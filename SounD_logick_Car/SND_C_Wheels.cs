using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace SEENG_ES
{
    public class SND_C_WheelsHandler
    {
        public MyEntity3DSoundEmitter _wheels33;
        public MyEntity3DSoundEmitter _wheels66;
        public MyEntity3DSoundEmitter _wheels99;

        public void Start(IMyCockpit cockpit, string prefix)
        {
            if (cockpit == null) return;

            var entity = (MyEntity)(IMyEntity)cockpit;

            CreateEmitter(ref _wheels33, entity, $"cSeengWheel33_{prefix}");
            CreateEmitter(ref _wheels66, entity, $"cSeengWheel66_{prefix}");
            CreateEmitter(ref _wheels99, entity, $"cSeengWheel99_{prefix}");
        }

        public void Update(SpeedManager speedManager)
        {
            if (_wheels33 == null) return;
            float speed = speedManager.NormalizedSpeed;
            float vol33 = CalcLayerVolume(speed, 0.00f, 0.15f, 0.40f, 0.60f);
            float vol66 = CalcLayerVolume(speed, 0.40f, 0.60f, 0.75f, 0.95f);
            float vol99 = CalcLayerVolume(speed, 0.75f, 0.95f, 1.00f, 1.00f);

            SetVolume(_wheels33, vol33);
            SetVolume(_wheels66, vol66);
            SetVolume(_wheels99, vol99);
        }

        public void Stop()
        {
            StopEmitter(ref _wheels33);
            StopEmitter(ref _wheels66);
            StopEmitter(ref _wheels99);
        }

        private void CreateEmitter(ref MyEntity3DSoundEmitter emitter, MyEntity entity, string cueName)
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

        private void SetVolume(MyEntity3DSoundEmitter emitter, float volume)
        {
            if (emitter?.Sound != null)
                emitter.Sound.VolumeMultiplier = MathHelper.Clamp(volume, 0f, 1f);
        }

        private void StopEmitter(ref MyEntity3DSoundEmitter emitter)
        {
            emitter?.StopSound(true);
            emitter = null;
        }

        public void StopAll()
        {
            StopEmitter(ref _wheels33); StopEmitter(ref _wheels66); StopEmitter(ref _wheels99);
        }
        private float CalcLayerVolume(float speed, float fadeInStart, float fadeInEnd, float fadeOutStart, float fadeOutEnd)
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