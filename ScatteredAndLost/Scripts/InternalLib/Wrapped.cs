using System.Threading;

namespace HK8YPlando.Scripts.InternalLib;

// Simple class for captures that wraps a primitive.
internal class Wrapped<T>
{
    internal T Value;

    internal Wrapped(T value) => Value = value;
}

internal class Synchronized<T>
{
    private Mutex mutex = new();
    private T Value;

    internal Synchronized(T init) => Value = init;

    internal T Get()
    {
        mutex.WaitOne();
        var ret = Value;
        mutex.ReleaseMutex();
        return ret;
    }

    internal void Set(T value)
    {
        mutex.WaitOne();
        Value = value;
        mutex.ReleaseMutex();
    }
}
