using HutongGames.PlayMaker.Actions;

namespace HK8YPlando.Util;

internal static class FSMExtensions
{
    internal static void SetMinMax(this WaitRandom self, float min, float max)
    {
        self.timeMin.Value = min;
        self.timeMax.Value = max;
    }
}
