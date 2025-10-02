using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace SEENG_ES
{
    public static class SND_MoveAmbienceHandler
    {
        public static void StartMoveAmbienceSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            if (cockpit == null)
            {
                emitter = null;
                return;
            }

            var myEntity = (MyEntity)(IMyEntity)cockpit;
            emitter = new MyEntity3DSoundEmitter(myEntity, true, 1f);
            emitter.Force3D = true;
            string cueName = string.IsNullOrEmpty(prefix) ? "SeengMoveAmbience" : "SeengMoveAmbience_" + prefix;
            var soundPair = new MySoundPair(cueName);
            emitter.PlaySound(soundPair);
            if (emitter.Sound != null && emitter.Sound.IsPlaying)
            {
                emitter.Sound.VolumeMultiplier = 0f;
            }
            else
            {
                emitter = null;
            }
        }

        public static void UpdateMoveAmbienceVolume(MyEntity3DSoundEmitter emitter, float normalizedSpeed)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying) return;
            float volume = MathHelper.Clamp((normalizedSpeed - 0.05f) / 0.15f, 0f, 1f);
            emitter.Sound.VolumeMultiplier = volume;
        }
    }
}