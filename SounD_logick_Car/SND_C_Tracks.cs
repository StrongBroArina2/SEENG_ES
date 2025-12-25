using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace SEENG_ES
{
    public static class SND_C_TracksHandler
    {
        public static MyEntity3DSoundEmitter _track33;
        public static MyEntity3DSoundEmitter _track66;
        public static MyEntity3DSoundEmitter _track99;

        public static void Start(IMyCockpit cockpit, string prefix)
        {
            if (cockpit == null) return;

            var entity = (MyEntity)(IMyEntity)cockpit;

            CreateEmitter(ref _track33, entity, $"cSeengTrack33_{prefix}");
            CreateEmitter(ref _track66, entity, $"cSeengTrack66_{prefix}");
            CreateEmitter(ref _track99, entity, $"cSeengTrack99_{prefix}");
        }

        public static void Update(SpeedManager speedManager)
        {
            if (_track33 == null) return; 
            float speed = speedManager.NormalizedSpeed;
            float vol33 = CalcLayerVolume(speed, 0.00f, 0.15f, 0.40f, 0.60f);
            float vol66 = CalcLayerVolume(speed, 0.40f, 0.60f, 0.75f, 0.95f);
            float vol99 = CalcLayerVolume(speed, 0.75f, 0.95f, 1.00f, 1.00f);

            SetVolume(_track33, vol33);
            SetVolume(_track66, vol66);
            SetVolume(_track99, vol99);
        }

        public static void Stop()
        {
            StopEmitter(ref _track33);
            StopEmitter(ref _track66);
            StopEmitter(ref _track99);
        }

        private static void CreateEmitter(ref MyEntity3DSoundEmitter emitter, MyEntity entity, string cueName)
        {
            if (emitter != null && emitter.Sound?.IsPlaying == true)
                return;

            if (emitter != null)
            {
                emitter.StopSound(true);
                emitter = null;
            }

            emitter = new MyEntity3DSoundEmitter(entity, true, 0f);
            emitter.Force3D = true;

            var soundPair = new MySoundPair(cueName);
            bool success = emitter.PlaySound(soundPair);

            if (success && emitter.Sound?.IsPlaying == true)
            {
            }
            else
            {
                emitter = null;
            }
        }

        private static void SetVolume(MyEntity3DSoundEmitter emitter, float volume)
        {
            if (emitter?.Sound != null)
                emitter.Sound.VolumeMultiplier = MathHelper.Clamp(volume, 0f, 1f);
        }

        private static void StopEmitter(ref MyEntity3DSoundEmitter emitter)
        {
            emitter?.StopSound(true);
            emitter = null;
        }
        private static float CalcLayerVolume(float speed, float fadeInStart, float fadeInEnd, float fadeOutStart, float fadeOutEnd)
        {
            if (speed < fadeInStart) return 0f;
            if (speed > fadeOutEnd) return 0f;

            if (speed <= fadeInEnd)
            {
                float t = (speed - fadeInStart) / (fadeInEnd - fadeInStart);
                return MathHelper.SmoothStep(0f, 1f, t);
            }

            if (speed >= fadeOutStart)
            {
                float t = (speed - fadeOutStart) / (fadeOutEnd - fadeOutStart);
                return MathHelper.SmoothStep(1f, 0f, t);
            }

            return 1f;
        }
    }
}