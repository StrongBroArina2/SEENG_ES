using System;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace SEENG_ES
{
    public static class SND_acdcHandler
    {
        public static void StartAcdcSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            var myEntity = (MyEntity)(IMyEntity)cockpit;
            emitter = new MyEntity3DSoundEmitter(myEntity, true, 1f);
            emitter.Force3D = true;
            string cueName = string.IsNullOrEmpty(prefix) ? "SeengEngineAcDc" : "SeengEngineAcDc_" + prefix;
            var soundPair = new MySoundPair(cueName);
            emitter.PlaySound(soundPair);
        }

        public static void UpdateAcdcVolume(MyEntity3DSoundEmitter emitter, SpeedManager speedManager)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying) return;

            float currentAcc = speedManager.Acceleration;
            float absAcc = Math.Abs(currentAcc);
            const float ACC_THRESHOLD = 0.1f;

            float elapsed = (float)speedManager.AccelerationStartTime.Elapsed.TotalSeconds;
            float targetVolume = 0f;

            if (absAcc < ACC_THRESHOLD)
            {
                emitter.Sound.VolumeMultiplier = Math.Max(0f, emitter.Sound.VolumeMultiplier - (1f / 120f));
                return;
            }

            if (elapsed < 3f)
            {
                targetVolume = 0.35f * (elapsed / 3f);
            }
            else if (elapsed < 6f)
            {
                targetVolume = 0.35f + 0.45f * ((elapsed - 3f) / 3f);
            }
            else if (elapsed < 10f)
            {
                targetVolume = 0.80f + 0.20f * ((elapsed - 6f) / 4f);
            }
            else
            {
                targetVolume = 1f;
            }

            float mul = 0.1f;
            emitter.Sound.VolumeMultiplier = Math.Min(1f, emitter.Sound.VolumeMultiplier + (targetVolume - emitter.Sound.VolumeMultiplier) * mul);
        }

    }
}