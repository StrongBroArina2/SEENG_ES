using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Data.Audio;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using System;
using SEENG_SElauncher.SEENG_Managers;

namespace SEENG_ES
{
    public class SND_MainThrusterHandler
    {
        public MyEntity3DSoundEmitter _loopEmitter;
        public MyEntity3DSoundEmitter _startEmitter;
        public MyEntity3DSoundEmitter _endEmitter;

        private string _prefix = "";
        private bool _wasThrusting = false;
        private float _loopVolume = 0f;
        private float _rampTimer = 0f;
        private bool _isStartSoundPlaying = false;

        private const float RAMP_UP_1 = 0.5f;
        private const float RAMP_UP_2 = 3.0f;
        private const float FADE_OUT_TIME = 1.5f;

        public void Start(IMyCockpit cockpit, string prefix)
        {
            Restart(cockpit, prefix);
        }

        public void Restart(IMyCockpit cockpit, string prefix)
        {
            StopAll();

            if (cockpit == null) return;
            _prefix = prefix;

            var entity = (MyEntity)(IMyEntity)cockpit;

            // LOOP
            _loopEmitter = new MyEntity3DSoundEmitter(entity, true, 0f);
            _loopEmitter.Force3D = true;
            PlayLoop();

            // START
            _startEmitter = new MyEntity3DSoundEmitter(entity, false, 1f);
            _startEmitter.Force3D = true;

            // END
            _endEmitter = new MyEntity3DSoundEmitter(entity, false, 1f);
            _endEmitter.Force3D = true;

            _wasThrusting = false;
            _loopVolume = 0f;
            _rampTimer = 0f;
        }

        private void PlayLoop()
        {
            if (_loopEmitter == null) return;
            string cue = string.IsNullOrEmpty(_prefix) ? "SeengMainThrusterLoop" : $"SeengMainThrusterLoop_{_prefix}";
            var pair = new MySoundPair(cue);
            bool success = _loopEmitter.PlaySound(pair);
        }

        public void Update(IMyCockpit cockpit, ThrustManager thrustManager)
        {
            if (cockpit == null) return;

            bool isThrusting = thrustManager.IsThrusting;

            //deb
            string debug = $"[Main Thruster DEBUG]\n" +
                           $"IsThrusting: {isThrusting} (was: {_wasThrusting})\n" +
                           $"Loop Volume: {_loopVolume:F3}\n" +
                           $"Ramp Timer: {_rampTimer:F2}s\n" +
                           $"Loop Playing: {_loopEmitter?.IsPlaying}\n" +
                           $"Start Playing: {_isStartSoundPlaying}\n" +
                           $"Loop Emitter: {(_loopEmitter != null ? "EXISTS" : "NULL")}\n" +
                           $"Cue: SeengMainThrusterLoop{(_prefix == "" ? "" : "_" + _prefix)}";

            // MyAPIGateway.Utilities.ShowNotification(debug, 16, isThrusting ? "White" : "Gray");

            if (_isStartSoundPlaying && _startEmitter != null && !_startEmitter.IsPlaying)
            {
                _isStartSoundPlaying = false;
            }

            if (isThrusting && !_wasThrusting && !_isStartSoundPlaying)
            {
                PlaySound(_startEmitter, "SeengMainThrusterStart");
                _isStartSoundPlaying = true;
                _rampTimer = 0f;

                if (_loopEmitter != null && !_loopEmitter.IsPlaying)
                {
                    PlayLoop();
                }
            }

            if (isThrusting)
            {
                _rampTimer += 1f / 60f;

                float volume;
                if (_rampTimer <= RAMP_UP_1)
                    volume = MathHelper.Lerp(0f, 0.7f, _rampTimer / RAMP_UP_1);
                else
                    volume = MathHelper.Lerp(0.7f, 1.0f, (_rampTimer - RAMP_UP_1) / RAMP_UP_2);

                _loopVolume = MathHelper.Clamp(volume, 0f, 1f);
            }

            else if (!isThrusting && _wasThrusting)
            {
                _rampTimer = 0f;
                PlaySound(_endEmitter, "SeengMainThrusterEnd");
            }
            else if (!isThrusting)
            {
                _rampTimer += 1f / 60f;
                float t = MathHelper.Clamp(_rampTimer / FADE_OUT_TIME, 0f, 1f);
                _loopVolume = MathHelper.Lerp(_loopVolume, 0f, t);
            }

            if (_loopEmitter?.Sound != null)
            {
                _loopEmitter.Sound.VolumeMultiplier = _loopVolume;
            }

            if (_loopVolume < 0.01f && !isThrusting)
            {
                _loopEmitter?.StopSound(true);
            }

            else if (_loopVolume > 0.01f && _loopEmitter != null && !_loopEmitter.IsPlaying)
            {
                PlayLoop();
            }

            _wasThrusting = isThrusting;
        }

        private void PlaySound(MyEntity3DSoundEmitter emitter, string cueName)
        {
            if (emitter == null) return;
            string fullCue = string.IsNullOrEmpty(_prefix) ? cueName : $"{cueName}_{_prefix}";
            var pair = new MySoundPair(fullCue);
            emitter.PlaySound(pair);
        }

        public void StopAll()
        {
            _loopEmitter?.StopSound(true);
            _startEmitter?.StopSound(true);
            _endEmitter?.StopSound(true);

            _loopEmitter = null;
            _startEmitter = null;
            _endEmitter = null;

            _loopVolume = 0f;
            _wasThrusting = false;
            _isStartSoundPlaying = false;
        }
    }
}