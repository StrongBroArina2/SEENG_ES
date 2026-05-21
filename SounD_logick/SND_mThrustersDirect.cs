using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SEENG_SElauncher.SEENG_Managers;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;
using static SEENG_SElauncher.SEENG_Managers.SpeedManager;

namespace SEENG_ES
{
    public  class SND_mThrustersHandler
    {
        public static MyEntity3DSoundEmitter _emitter;
        private static string _currentCue = "";

        private const float MAX_ANGULAR_SPEED = 3.293f; // peak rpm se lkmit
        private const float MIN_ANGULAR_THRESHOLD = 0.050f; // min rpm
        private const float LINEAR_SPEED_LIMIT = 0.25f; // max speed limit
        private const float FADE_RANGE = 0.15f;     
        private const float MIN_VOLUME = 0.5f;      

        private const float PITCH_MIN = 0.85f;  
        private const float PITCH_MAX = 1.65f;   

        private static float _targetVolume = 0f;
        private static float _currentVolume = 0f;

        public  void Start(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string prefix)
        {
            if (cockpit == null) return;

            var entity = (MyEntity)(IMyEntity)cockpit;
            emitter = new MyEntity3DSoundEmitter(entity, true, 0f);
            emitter.Force3D = true;

            _currentCue = string.IsNullOrEmpty(prefix) ? "SeengmThrusters" : $"SeengmThrusters_{prefix}";
            var pair = new MySoundPair(_currentCue);

            emitter.PlaySound(pair);

            if (emitter?.Sound?.IsPlaying == true)
            {
            }
            else
            {
                emitter?.StopSound(true);
                emitter = null;
                return;
            }

            _emitter = emitter;
            _currentVolume = 0f;
            _targetVolume = 0f;
        }

        public  void Update(MyEntity3DSoundEmitter emitter, RotationManager rotationManager, SpeedManager speedManager)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying)
                return;

            float angularSpeed = rotationManager.AngularSpeedRad;
            float linearSpeedNorm = speedManager.NormalizedSpeed;

            bool angularOk = angularSpeed >= MIN_ANGULAR_THRESHOLD;
            bool linearOk = linearSpeedNorm <= LINEAR_SPEED_LIMIT;

            bool shouldBeActive = angularOk && linearOk;

            if (angularSpeed >= MIN_ANGULAR_THRESHOLD)
            {
                float pitchFactor = MathHelper.Clamp(angularSpeed / MAX_ANGULAR_SPEED, 0f, 1f);
                float pitch = MathHelper.Lerp(PITCH_MIN, PITCH_MAX, pitchFactor);
                emitter.Sound.FrequencyRatio = pitch;

                if (linearSpeedNorm <= LINEAR_SPEED_LIMIT)
                {
                    _targetVolume = 1f;
                }
                else
                {
                    float fadeFactor = MathHelper.Clamp((linearSpeedNorm - LINEAR_SPEED_LIMIT) / FADE_RANGE, 0f, 1f);
                    _targetVolume = MathHelper.Lerp(1f, MIN_VOLUME, fadeFactor);
                }
            }
            else
            {
                _targetVolume = 0f;
            }

            float fadeSpeed = _targetVolume > _currentVolume ? 8f : 10f;
            _currentVolume = MathHelper.Lerp(_currentVolume, _targetVolume, fadeSpeed * (1f / 60f));

            emitter.Sound.VolumeMultiplier = _currentVolume;

            if (_currentVolume < 0.01f && _targetVolume == 0f)
            {
            }
        }

        public  void Stop()
        {
            if (_emitter != null)
            {
                _emitter.StopSound(true);
                _emitter = null;
            }
            _currentVolume = 0f;
            _targetVolume = 0f;
        }
    }
}