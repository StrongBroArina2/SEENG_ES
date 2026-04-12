using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Utils;
using VRage.Audio;
using VRageMath;
using System.Diagnostics;
using VRage.ModAPI;
using SEENG_SElauncher.SEENG_Managers;

namespace SEENG_ES
{
    public  class SND_C_EngineHandler
    {
        public  MyEntity3DSoundEmitter _idle;
        public  MyEntity3DSoundEmitter _base33;
        public  MyEntity3DSoundEmitter _base66;
        public  MyEntity3DSoundEmitter _base99;
        public  MyEntity3DSoundEmitter _load33;
        public  MyEntity3DSoundEmitter _load66;
        public  MyEntity3DSoundEmitter _load99;

        private  readonly Stopwatch _loadRampTimer = new Stopwatch();
        private  float _currentLoadRpm = 0f;

        private const float RPM33 = 0.33f;
        private const float RPM66 = 0.66f;
        private const float RPM99 = 0.99f;

        private const float MAX_PITCH_SEMITONES = 8f;
        private const float MIN_PITCH_SEMITONES = -2f;

        public  void Start(IMyCockpit cockpit, string prefix)
        {
            if (cockpit == null) return;
            var entity = (MyEntity)(IMyEntity)cockpit;

            CreateEmitter(ref _idle, entity, $"cSeengEngineIdle_{prefix}", true);
            CreateEmitter(ref _base33, entity, $"cSeengEngine33_{prefix}", true);
            CreateEmitter(ref _base66, entity, $"cSeengEngine66_{prefix}", true);
            CreateEmitter(ref _base99, entity, $"cSeengEngine99_{prefix}", true);
            CreateEmitter(ref _load33, entity, $"cSeengEngineLoad33_{prefix}", true);
            CreateEmitter(ref _load66, entity, $"cSeengEngineLoad66_{prefix}", true);
            CreateEmitter(ref _load99, entity, $"cSeengEngineLoad99_{prefix}", true);
        }

        public  void Update(ThrustManager thrustManager, SpeedManager speedManager)
        {
            if (_idle == null) return; 

            float speed = speedManager.NormalizedSpeed; 

            if (!_loadRampTimer.IsRunning)
                _loadRampTimer.Start();

            float secondsPerFullCycle = 2f;
            float ratePerSecond = 100f / secondsPerFullCycle;

            float deltaThisFrame = (thrustManager.IsThrusting ? ratePerSecond : -ratePerSecond) * (1f / 60f);
            _currentLoadRpm = MathHelper.Clamp(_currentLoadRpm + deltaThisFrame, 0f, 100f);

            float loadMix = _currentLoadRpm / 100f;

            // Idle
            float idleVol = speed <= 0.1f ? (1f - speed / 0.1f) : 0f;
            SetVolume(_idle, idleVol);

            float base33Vol = CalcLayerVolume(speed, 0.0f, 0.1f, 0.3f, 0.5f);
            float base66Vol = CalcLayerVolume(speed, 0.3f, 0.5f, 0.65f, 0.85f); 
            float base99Vol = CalcLayerVolume(speed, 0.65f, 0.85f, 1.0f, 1.0f);

            SetVolume(_base33, base33Vol * (1f - loadMix));
            SetVolume(_base66, base66Vol * (1f - loadMix));
            SetVolume(_base99, base99Vol * (1f - loadMix));

            SetVolume(_load33, base33Vol * loadMix);
            SetVolume(_load66, base66Vol * loadMix);
            SetVolume(_load99, base99Vol * loadMix);

            ApplyLayerPitch(_idle, speed, 1.0f);
            ApplyLayerPitch(_base33, speed, RPM33);
            ApplyLayerPitch(_base66, speed, RPM66);
            ApplyLayerPitch(_base99, speed, RPM99);
            ApplyLayerPitch(_load33, speed, RPM33);
            ApplyLayerPitch(_load66, speed, RPM66);
            ApplyLayerPitch(_load99, speed, RPM99);

            // debug my beloved
            if (MyAPIGateway.Session?.Player != null)
            {
                float debugIdleVol = speed <= 0.1f ? (1f - speed / 0.1f) : 0f;

                float debugBase33 = CalcLayerVolume(speed, 0.0f, 0.1f, 0.3f, 0.5f);
                float debugBase66 = CalcLayerVolume(speed, 0.3f, 0.5f, 0.65f, 0.85f);
                float debugBase99 = CalcLayerVolume(speed, 0.65f, 0.85f, 1.0f, 1.0f);

                string debugText = $"[C-Engine DEBUG]\n" +
                                   $"IsThrusting: {thrustManager.IsThrusting}\n" +
                                   $"Load RPM: {_currentLoadRpm:F1}%\n" +
                                   $"Load Mix: {loadMix:F3}\n" +
                                   $"Speed: {(speed * 100f):F1}%\n" +
                                   $"Idle Vol: {debugIdleVol:F2}\n" +
                                   $"Base33 → Load33: {debugBase33:F2} → {(debugBase33 * loadMix):F2}\n" +
                                   $"Base66 → Load66: {debugBase66:F2} → {(debugBase66 * loadMix):F2}\n" +
                                   $"Base99 → Load99: {debugBase99:F2} → {(debugBase99 * loadMix):F2}";

                string color = thrustManager.IsThrusting ? "Green" :
                               _currentLoadRpm > 10f ? "Yellow" : "White";

               // MyAPIGateway.Utilities.ShowNotification(debugText, 16, color);
            }
        }

        public  void Stop()
        {
            StopEmitter(ref _idle);
            StopEmitter(ref _base33);
            StopEmitter(ref _base66);
            StopEmitter(ref _base99);
            StopEmitter(ref _load33);
            StopEmitter(ref _load66);
            StopEmitter(ref _load99);

            _loadRampTimer.Reset();
            _currentLoadRpm = 0f;
        }

        private  void CreateEmitter(ref MyEntity3DSoundEmitter emitter, MyEntity entity, string cueName, bool looped)
        {
            if (emitter != null && emitter.Sound?.IsPlaying == true)
                return;

            if (emitter != null)
            {
                emitter.StopSound(true);
                emitter = null;
            }

            emitter = new MyEntity3DSoundEmitter(entity, looped, 0f);
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

        private  void SetVolume(MyEntity3DSoundEmitter emitter, float volume)
        {
            if (emitter?.Sound != null)
                emitter.Sound.VolumeMultiplier = MathHelper.Clamp(volume, 0f, 1f);
        }

        private  void ApplyLayerPitch(MyEntity3DSoundEmitter emitter, float speed, float layerRpm)
        {
            if (emitter?.Sound != null && emitter.Sound.IsPlaying)
            {
                float ratio = (speed > 0.01f && layerRpm > 0f) ? speed / layerRpm : 1f;
                float semitones = (float)(12 * Math.Log(ratio, 2));
                semitones = MathHelper.Clamp(semitones, MIN_PITCH_SEMITONES, MAX_PITCH_SEMITONES);
                emitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
            }
        }

        public void StopAll()
        {
            StopEmitter(ref _idle); StopEmitter(ref _base33); StopEmitter(ref _base66);
            StopEmitter(ref _base99); StopEmitter(ref _load33); StopEmitter(ref _load66); StopEmitter(ref _load99);
        }

        private  void StopEmitter(ref MyEntity3DSoundEmitter emitter)
        {
            emitter?.StopSound(true);
            emitter = null;
        }
        private  float CalcLayerVolume(float speed, float fadeInStart, float fadeInEnd, float fadeOutStart, float fadeOutEnd)
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