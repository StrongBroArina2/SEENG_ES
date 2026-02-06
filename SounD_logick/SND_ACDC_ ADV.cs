using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using System;
using Sandbox.Game.Entities;

namespace SEENG_ES
{
    public class SND_ACDC_ADV
    {
        private MyEntity3DSoundEmitter _accelEmitter;
        private MyEntity3DSoundEmitter _deccelEmitter;

        private string _prefix = "";
        private float _lastSpeed = 0f;
        private float _accelVolume = 0f;
        private float _deccelVolume = 0f;

        private const float FADE_STEP = 1f / (60f * 1.5f);
        private const float MAX_PITCH_SEMITONES = 10f; 

        public void Start(IMyCockpit cockpit, string prefix)
        {
            StopAll();
            if (cockpit == null) return;
            _prefix = prefix;

            var entity = (MyEntity)(IMyEntity)cockpit;

            _accelEmitter = new MyEntity3DSoundEmitter(entity, true);
            _accelEmitter.Force3D = true;

            _deccelEmitter = new MyEntity3DSoundEmitter(entity, true);
            _deccelEmitter.Force3D = true;
        }

        public void Update(IMyCockpit cockpit, SpeedManager speedManager)
        {
            if (cockpit?.CubeGrid?.Physics == null || _accelEmitter == null) return;

            float currentSpeed = (float)cockpit.CubeGrid.Physics.LinearVelocity.Length();
            float acceleration = currentSpeed - _lastSpeed;
            float normSpeed = speedManager.NormalizedSpeed;

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
            float targetPitch = (float)Math.Pow(2, (normSpeed * MAX_PITCH_SEMITONES) / 12.0);

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
        }
    }
}