using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using System;
using Sandbox.Game.Entities;
using SEENG_SElauncher.SEENG_Managers;

namespace SEENG_ES
{
    public class SND_ACDC_ADV
    {
        public MyEntity3DSoundEmitter _accelEmitter;
        public MyEntity3DSoundEmitter _deccelEmitter;

        private string _prefix = "";
        private float _lastSpeed = 0f;
        private float _accelVolume = 0f;
        private float _deccelVolume = 0f;

        private const float FADE_STEP = 1f / (60f * 1.5f);
        private float _maxPitchSemitones = 10f;

        private float _forcedFadeTimer = 0f;
        private const float MIN_FADE_TIME = 5.3f;
        private const float SPEED_THRESHOLD = 0.05f;

        public void Start(IMyCockpit cockpit, string prefix)
        {
            StopAll();
            if (cockpit == null) return;
            _prefix = prefix;

            var entity = (MyEntity)(IMyEntity)cockpit;

            _accelEmitter = new MyEntity3DSoundEmitter(entity, true, 10f);
            _accelEmitter.Force3D = true;

            _deccelEmitter = new MyEntity3DSoundEmitter(entity, true, 10f);
            _deccelEmitter.Force3D = true;
        }

        public void Update(IMyCockpit cockpit, SpeedManager speedManager, float maxPitchSemitones)
        {
            _maxPitchSemitones = maxPitchSemitones;
            if (cockpit?.CubeGrid?.Physics == null || _accelEmitter == null) return;

            float currentSpeed = (float)cockpit.CubeGrid.Physics.LinearVelocity.Length();
            float acceleration = currentSpeed - _lastSpeed;
            float normSpeed = speedManager.NormalizedSpeed;
            float dt = 1f / 60f;

            if (acceleration > 0.01f)
            {
                _accelVolume = MathHelper.Clamp(_accelVolume + FADE_STEP, 0f, 1f);
                _deccelVolume = MathHelper.Clamp(_deccelVolume - FADE_STEP, 0f, 1f);
            }
            else if (acceleration < -0.01f)
            {
                _deccelVolume = MathHelper.Clamp(_deccelVolume + FADE_STEP, 0f, 1f);
                _accelVolume = MathHelper.Clamp(_accelVolume - FADE_STEP, 0f, 1f);
            }
            else
            {
                _accelVolume = MathHelper.Clamp(_accelVolume - FADE_STEP, 0f, 1f);
                _deccelVolume = MathHelper.Clamp(_deccelVolume - FADE_STEP, 0f, 1f);
            }
            if (normSpeed < SPEED_THRESHOLD)
            {
                _forcedFadeTimer = MathHelper.Clamp(_forcedFadeTimer + dt, 0f, MIN_FADE_TIME);
                float speedRatio = normSpeed / SPEED_THRESHOLD;
                float speedSquelch = speedRatio * speedRatio * (3 - 2 * speedRatio);
                float timeRatio = 1f - (_forcedFadeTimer / MIN_FADE_TIME);
                float finalSquelch = Math.Max(speedSquelch, timeRatio);
                _accelVolume *= finalSquelch;
                _deccelVolume *= finalSquelch;
            }
            else
            {
                _forcedFadeTimer = 0f;
            }

            float targetPitch = (float)Math.Pow(2, (normSpeed * _maxPitchSemitones) / 12.0);

            UpdateLayer(_accelEmitter, "SeengACDCacc", _accelVolume, targetPitch);
            UpdateLayer(_deccelEmitter, "SeengACDCdcc", _deccelVolume, targetPitch);

            _lastSpeed = currentSpeed;
        }

        private void UpdateLayer(MyEntity3DSoundEmitter emitter, string baseCue, float volume, float pitch)
        {
            if (volume > 0.001f)
            {
                if (!emitter.IsPlaying)
                {
                    string fullCue = string.IsNullOrEmpty(_prefix) ? baseCue : $"{baseCue}_{_prefix}";
                    emitter.PlaySound(new MySoundPair(fullCue));
                }

                if (emitter.Sound != null)
                {
                    emitter.Sound.VolumeMultiplier = volume;
                    emitter.Sound.FrequencyRatio = pitch;
                }
            }
            else if (emitter.IsPlaying)
            {
                emitter.StopSound(true);
            }
        }

        public void StopAll()
        {
            _accelEmitter?.StopSound(true);
            _deccelEmitter?.StopSound(true);

            _accelEmitter = null;
            _deccelEmitter = null;

            _accelVolume = 0f;
            _deccelVolume = 0f;
            _lastSpeed = 0f;

            _forcedFadeTimer = 0f;
        }
    }
}