using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace SEENG_ES
{
    public static class SND_PushHandler
    {
        public static void StartPushSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, ThrustManager thrustManager, string prefix)
        {
            if (cockpit == null) return;

            var myEntity = (MyEntity)(IMyEntity)cockpit;
            emitter = new MyEntity3DSoundEmitter(myEntity, true, 1f);
            emitter.Force3D = true;
            string cueName = string.IsNullOrEmpty(prefix) ? "SeengEnginePush" : "SeengEnginePush_" + prefix;
            var soundPair = new MySoundPair(cueName);
            emitter.PlaySound(soundPair);

            if (emitter?.Sound != null && emitter.Sound.IsPlaying)
            {
                emitter.Sound.VolumeMultiplier = 1f;
                thrustManager.IsPushLooping = true;
            }
            else
            {
                emitter?.StopSound(true);
                emitter = null;

            }

        }

        private static float CubicEaseOut(float t)
        {
            return 1f - (float)Math.Pow(1f - t, 3f);
        }
        public static void UpdatePushVolume(MyEntity3DSoundEmitter emitter, ThrustManager thrustManager)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying) return;

            float elapsed = (float)thrustManager.DecayStartTime.Elapsed.TotalSeconds;

            if (thrustManager.IsThrusting)
            {
                emitter.Sound.VolumeMultiplier = 1f;
                thrustManager.StartDecay();
                return;
            }
            float eased = CubicEaseOut(1f - elapsed / 2f);
            emitter.Sound.VolumeMultiplier = MathHelper.Clamp(eased, 0f, 1f);

            if (emitter.Sound.VolumeMultiplier <= 0f)
            {
                emitter.StopSound(true);
                emitter = null;
                thrustManager.IsPushLooping = false;

            }
        }
    }
}