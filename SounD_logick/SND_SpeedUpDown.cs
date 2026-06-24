using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;
using System;
using Sandbox.Game.Entities;
using VRage.Audio;
using Sandbox.Definitions;
using SEENG_SElauncher.SEENG_Managers;

namespace SEENG_ES
{
    public class SND_SpeedUpDown
    {
        public MyEntity3DSoundEmitter _emitter;
        private string _prefix = "";

        private float _lastSpeed = 0f;
        private float _cooldownTimer = 0f;
        private bool _isReady = true;
        private bool _isSoundPlaying = false;

        private const float COOLDOWN_TARGET = 2.0f;
        private const float STABILITY_THRESHOLD = 1.0f;
        private const float TRIGGER_THRESHOLD = 1.2f;

        public void Start(IMyCockpit cockpit, string prefix)
        {
            StopAll();
            if (cockpit == null) return;
            _prefix = prefix;
            var entity = (MyEntity)(IMyEntity)cockpit;
            _emitter = new MyEntity3DSoundEmitter(entity, true);
            _emitter.Force3D = true;

            _cooldownTimer = COOLDOWN_TARGET;
            _isReady = true;
        }

        public void Update(IMyCockpit cockpit, SpeedManager speedManager)
        {
            if (cockpit?.CubeGrid?.Physics == null || _emitter == null) return;

            float currentSpeed = (float)cockpit.CubeGrid.Physics.LinearVelocity.Length();
            float deltaSpeed = currentSpeed - _lastSpeed;

            if (_isSoundPlaying && _emitter != null && !_emitter.IsPlaying)
            {
                _isSoundPlaying = false;
            }

            bool isStable = Math.Abs(deltaSpeed) < (STABILITY_THRESHOLD / 60f);
            if (isStable)
            {
                if (_cooldownTimer < COOLDOWN_TARGET)
                    _cooldownTimer += 1f / 60f;
                else
                    _isReady = true;
            }

            if (_isReady && !_isSoundPlaying)
            {
                if (deltaSpeed > (TRIGGER_THRESHOLD / 60f))
                    PlaySpeed("SeengSpeedUP");
                else if (deltaSpeed < -(TRIGGER_THRESHOLD / 60f))
                    PlaySpeed("SeengSpeedDown");
            }

            _lastSpeed = currentSpeed;
            _emitter.Update();
        }

        private void PlaySpeed(string baseCue)
        {
            string fullCue = string.IsNullOrEmpty(_prefix) ? baseCue : $"{baseCue}_{_prefix}";
            var pair = new MySoundPair(fullCue);

            _emitter.PlaySound(pair, stopPrevious: true);

            _isSoundPlaying = true;
            _isReady = false;
            _cooldownTimer = 0f;
        }

        public void StopAll()
        {
            _emitter?.StopSound(true);
            _isReady = false;
            _isSoundPlaying = false;
            _cooldownTimer = 0f;
        }
    }
}