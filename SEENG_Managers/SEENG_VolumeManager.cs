using VRage.Utils;
using VRageMath;

namespace SEENG_ES
{
    public static class SEENG_VolumeManager
    {
        private static float _globalMultiplier = 1f;

        public static float GlobalMultiplier => _globalMultiplier;
        public static void SetVolume(float percent)
        {
            float clamped = MathHelper.Clamp(percent, -100f, 100f);
            _globalMultiplier = (clamped + 100f) / 100f;
            MyLog.Default.WriteLine($"SEENG_ES: Global SEENG volume set to {clamped:+0;-0;0}% ({_globalMultiplier:F2}x)");
        }
        public static float GetCurrentPercent()
        {
            return (_globalMultiplier * 100f) - 100f;
        }
    }
}