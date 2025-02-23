using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HK8YPlando.Util;

public delegate bool CoroutinePercentUpdate(float pct);

public delegate bool CoroutineTimeUpdate(float deltaTime);

public record CoroutineUpdate
{
    public CoroutineUpdate(bool done, float extraTime)
    {
        this.done = done;
        this.extraTime = extraTime;
    }

    public bool done;
    public float extraTime;
}

public abstract class CoroutineElement
{
    // Returns false when incomplete.
    // Returns (true, remainingTime) when complete.
    public abstract CoroutineUpdate Update(float deltaTime);
}

internal class SleepSeconds : CoroutineElement
{
    private readonly float orig;
    private float remaining;
    private readonly CoroutinePercentUpdate? percentUpdate;
    private readonly CoroutineTimeUpdate? timeUpdate;

    public SleepSeconds(float remaining)
    {
        this.orig = remaining;
        this.remaining = remaining;
    }

    public SleepSeconds(float remaining, CoroutinePercentUpdate percentUpdate) : this(remaining) => this.percentUpdate = percentUpdate;

    public SleepSeconds(float remaining, CoroutineTimeUpdate timeUpdate) : this(remaining) => this.timeUpdate = timeUpdate;

    public override CoroutineUpdate Update(float deltaTime)
    {
        if (remaining <= deltaTime)
        {
            percentUpdate?.Invoke(1.0f);
            timeUpdate?.Invoke(remaining);
            return new(true, deltaTime - remaining);
        }
        remaining -= deltaTime;

        bool done = false;
        if (percentUpdate != null) done = percentUpdate.Invoke(1.0f - (remaining / orig));
        if (timeUpdate != null) done = timeUpdate.Invoke(deltaTime);
        return new(done, 0);
    }
}

internal class SleepFrames : CoroutineElement
{
    private int remaining;

    public SleepFrames(int remaining)
    {
        this.remaining = remaining;
    }

    public override CoroutineUpdate Update(float deltaTime)
    {
        if (remaining <= 0) return new(true, deltaTime);

        --remaining;
        return new(false, 0);
    }
}

internal class SleepUntil : CoroutineElement
{
    private Func<bool> condition;

    public SleepUntil(Func<bool> condition) => this.condition = condition;

    public override CoroutineUpdate Update(float deltaTime)
    {
        bool cond = condition();
        return new(cond, cond ? deltaTime : 0);
    }
}

internal class SleepUntilTimeout : CoroutineElement
{
    private readonly CoroutineOneOf choice;

    public bool TimedOut { get; private set; }

    public SleepUntilTimeout(SleepUntil sleepUntil, SleepSeconds timeout) => choice = new([sleepUntil, timeout]);

    public override CoroutineUpdate Update(float deltaTime)
    {
        var update = choice.Update(deltaTime);
        TimedOut = update.done && choice.Choice == 1;
        return update;
    }
}

internal class SleepUntilCondHolds : CoroutineElement
{
    private readonly Func<bool> condition;
    private float time;

    public SleepUntilCondHolds(Func<bool> condition, float time)
    {
        this.condition = condition;
        this.time = time;
    }

    private SleepSeconds? timer;

    public override CoroutineUpdate Update(float deltaTime)
    {
        if (condition())
        {
            timer ??= new(time);
            return timer.Update(deltaTime);
        }

        timer = null;
        return new(false, 0);
    }
}

internal class CoroutineSequence : CoroutineElement
{
    public delegate bool StopCondition();

    private readonly IEnumerator<CoroutineElement> coroutine;
    private readonly StopCondition? stopCondition;
    private CoroutineElement? current;

    public CoroutineSequence(IEnumerator<CoroutineElement> coroutine, StopCondition? stopCondition = null)
    {
        this.coroutine = coroutine;
        this.stopCondition = stopCondition;
    }

    public static CoroutineSequence Create(IEnumerator<CoroutineElement> coroutine, CoroutineSequence.StopCondition? stopCondition = null) => new(coroutine, stopCondition);

    public override CoroutineUpdate Update(float deltaTime)
    {
        if (stopCondition?.Invoke() ?? false) return new(true, deltaTime);

        if (current == null)
        {
            current = coroutine.MaybeMoveNext();
            if (current == null) return new(true, deltaTime);
        }

        while (current != null && deltaTime > 0)
        {
            var update = current.Update(deltaTime);
            if (update.done)
            {
                current = coroutine.MaybeMoveNext();
                deltaTime = update.extraTime;
            }
            else break;
        }

        if (current == null) return new(true, deltaTime);
        else return new(false, 0);
    }
}

internal class CoroutineOneOf : CoroutineElement
{
    private readonly List<CoroutineElement> choices;

    public int Choice { get; private set; } = -1;

    public CoroutineOneOf(List<CoroutineElement> choices) => this.choices = choices;

    public override CoroutineUpdate Update(float deltaTime) => choices.Select(c => c.Update(deltaTime)).OrderBy(c => c.done ? -c.extraTime : 1).First();
}

internal class CoroutineAllOf : CoroutineElement
{
    private readonly List<CoroutineElement> requirements;

    public CoroutineAllOf(List<CoroutineElement> requirements) => this.requirements = requirements;

    public override CoroutineUpdate Update(float deltaTime)
    {
        List<CoroutineElement> remaining = [];
        float minRemaining = Mathf.Infinity;

        foreach (var requirement in requirements)
        {
            var update = requirement.Update(deltaTime);
            if (update.done) minRemaining = Mathf.Min(minRemaining, update.extraTime);
            else remaining.Add(requirement);
        }

        if (remaining.Count == 0)
        {
            requirements.Clear();
            return new(true, minRemaining);
        }
        else if (remaining.Count != requirements.Count)
        {
            requirements.Clear();
            requirements.AddRange(remaining);
        }

        return new(false, 0);
    }
}

internal static class Coroutines
{
    public static CoroutineSequence Sequence(IEnumerator<CoroutineElement> enumerator, CoroutineSequence.StopCondition? stopCondition = null)
        => new(enumerator, stopCondition);

    public static CoroutineOneOf OneOf(params CoroutineElement[] choices) => new(choices.ToList());

    public static CoroutineAllOf AllOf(params CoroutineElement[] choices) => new(choices.ToList());

    // Sleep N frames
    public static SleepFrames SleepFrames(int frames) => new(frames);

    // Sleep one frame
    public static SleepFrames SleepFrame() => SleepFrames(1);

    // Sleep the specified number of seconds
    public static SleepSeconds SleepSeconds(float seconds) => new(seconds);

    // Sleep until condition() holds
    public static SleepUntil SleepUntil(Func<bool> condition) => new(condition);

    // Sleep until condition(), or 
    public static SleepUntilTimeout SleepUntilTimeout(Func<bool> condition, float seconds) => new(new(condition), new(seconds));

    public static SleepUntilCondHolds SleepUntilCondHolds(Func<bool> condition, float seconds) => new(condition, seconds);

    public static SleepSeconds Noop() => SleepSeconds(0);

    public static SleepSeconds SleepSecondsUpdatePercent(float seconds, CoroutinePercentUpdate update) => new(seconds, update);

    public static SleepSeconds SleepSecondsUpdateDelta(float seconds, CoroutineTimeUpdate update) => new(seconds, update);
}