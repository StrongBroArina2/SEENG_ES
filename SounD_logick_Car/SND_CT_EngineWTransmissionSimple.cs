using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Audio;
using VRageMath;
using System.Diagnostics;
using VRage.ModAPI;
using SEENG_SElauncher.SEENG_Managers;
using System;

namespace SEENG_ES
{
    public class SND_CTS_EngineHandler
    {
        public MyEntity3DSoundEmitter _idleT;
        public MyEntity3DSoundEmitter _base33T, _base66T, _base99T;
        public MyEntity3DSoundEmitter _load33T, _load66T, _load99T;

        public MyEntity3DSoundEmitter _gearShiftUpEmitter;
        public MyEntity3DSoundEmitter _gearShiftDownEmitter;
        public MyEntity3DSoundEmitter _revEmitter;
        public MyEntity3DSoundEmitter _releaseEmitter;

        private string _prefix = "";
        private bool _wasThrottleActive = false;
        private bool _isTrackedVehicle = false;
        private readonly Stopwatch _revCooldown = new Stopwatch();
        private const float REV_COOLDOWN_TIME = 6.0f;
        private readonly Stopwatch _relCooldown = new Stopwatch();
        private const float REL_COOLDOWN_TIME = 6.0f;

        private int _currentGear = 1;
        private float _virtualRpm = 800f;
        private float _smoothedRpmNorm = 0f;
        private float _shiftTimer = 0f;
        private bool _isShifting = false;
        private bool _shiftUp = false;
        private float _loadIndicator = 0f;

        private const float IDLE_RPM = 800f;
        private const float REDLINE_RPM = 4500f;
        private const float SHIFT_TIME = 0.4f;
        private const float RPM_BLEND_WEIGHT = 0.4f;
        private SEENG_TransmissionConfig _config = SEENG_TransmissionConfig.Default;

        private float[] GearRatios => _config.GearRatiosS.Count > 1 ? _config.GearRatiosS.ToArray() : new float[] { 0f, 3.5f, 2.4f, 1.7f, 1.2f, 0.9f };
        private float[] UpshiftSpeedThresholds => _config.UpshiftSpeedThresholds.Count > 1 ? _config.UpshiftSpeedThresholds.ToArray() : new float[] { 0f, 0.20f, 0.45f, 0.65f, 0.85f };
        private float[] DownshiftSpeedThresholds => _config.DownshiftSpeedThresholds.Count > 1 ? _config.DownshiftSpeedThresholds.ToArray() : new float[] { 0f, 0.12f, 0.35f, 0.55f, 0.75f };

        private int MaxGear => GearRatios.Length - 1;
        public void Start(IMyCockpit cockpit, string prefix, SEENG_TransmissionConfig transmissionConfig = null)
        {
            if (cockpit == null) return;

            _prefix = prefix;
            _config = transmissionConfig ?? SEENG_TransmissionConfig.Default;
            _isTrackedVehicle = _config.SkidSteering;
            var entity = (MyEntity)(IMyEntity)cockpit;

            CreateEmitter(ref _idleT, entity, GetFullCue("ctsSeengEngineIdle"), true);
            CreateEmitter(ref _base33T, entity, GetFullCue("ctsSeengEngine33"), true);
            CreateEmitter(ref _base66T, entity, GetFullCue("ctsSeengEngine66"), true);
            CreateEmitter(ref _base99T, entity, GetFullCue("ctsSeengEngine99"), true);
            CreateEmitter(ref _load33T, entity, GetFullCue("ctsSeengEngineLoad33"), true);
            CreateEmitter(ref _load66T, entity, GetFullCue("ctsSeengEngineLoad66"), true);
            CreateEmitter(ref _load99T, entity, GetFullCue("ctsSeengEngineLoad99"), true);

            CreateEmitter(ref _gearShiftUpEmitter, entity, GetFullCue("ctsSeengGearShiftUp"), false);
            CreateEmitter(ref _gearShiftDownEmitter, entity, GetFullCue("ctsSeengGearShiftDown"), false);
            CreateEmitter(ref _revEmitter, entity, GetFullCue("ctsSeengEngineRev"), false);
            CreateEmitter(ref _releaseEmitter, entity, GetFullCue("ctsSeengEngineRelease"), false);

            ResetState();
        }
        public void Update(ThrustManager thrustManager, SpeedManager speedManager, ThrottleThrusterManager throttleManager)
        {
            if (_idleT == null) return;

            float speed = speedManager.NormalizedSpeed;
            bool isThrusting = thrustManager.IsThrusting;

            HandleRevAndRelease(throttleManager);

            bool shouldApplyLoad = _isTrackedVehicle
                ? (throttleManager.IsForwardThrottling || throttleManager.IsReverseThrottling || throttleManager.IsSkidSteering)
                : (throttleManager.IsForwardThrottling || throttleManager.IsReverseThrottling);

            float deltaLoad = (shouldApplyLoad ? 50f : -50f) * (1f / 60f);
            _loadIndicator = MathHelper.Clamp(_loadIndicator + deltaLoad, 0f, 100f);
            float loadMix = _loadIndicator / 100f;
            float effectiveLoadMix = _isShifting ? MathHelper.Lerp(loadMix, 0f, _shiftTimer / SHIFT_TIME) : loadMix;

            //
            if (!_isShifting && speed > 0.02f)
            {
                if (_currentGear < MaxGear && speed > UpshiftSpeedThresholds[_currentGear])
                {
                    ShiftGear(true);
                }

                else if (_currentGear > 1 && speed < DownshiftSpeedThresholds[_currentGear - 1])
                {
                    ShiftGear(false);
                }
            }

            if (speed < 0.01f) _currentGear = 1;

            float targetRpm = IDLE_RPM;
            if (speed > 0.001f)
            {
                float gearRatio = GearRatios[_currentGear];
                targetRpm = IDLE_RPM + (speed * (REDLINE_RPM - IDLE_RPM) * gearRatio);
                targetRpm = MathHelper.Clamp(targetRpm, IDLE_RPM, REDLINE_RPM + 200f);
            }

            float rpmChangeSpeed = _isShifting ? 5f : (isThrusting ? 15f : 8f);

            _virtualRpm = MathHelper.Lerp(_virtualRpm, targetRpm, rpmChangeSpeed * (1f / 60f));

            if (_isShifting)
            {
                _shiftTimer += 1f / 60f;
                if (_shiftTimer >= SHIFT_TIME)
                {
                    _isShifting = false;
                    _shiftTimer = 0f;
                }
            }

            float rawRpmNorm = (_virtualRpm - IDLE_RPM) / (REDLINE_RPM - IDLE_RPM);
            _smoothedRpmNorm = MathHelper.Lerp(_smoothedRpmNorm, rawRpmNorm, 5f * (1f / 60f));

            float blendNorm = MathHelper.Lerp(speed, _smoothedRpmNorm, RPM_BLEND_WEIGHT);

            UpdateLayers(blendNorm, effectiveLoadMix);
        }

        private void ShiftGear(bool up)
        {
            _currentGear = MathHelper.Clamp(up ? _currentGear + 1 : _currentGear - 1, 1, MaxGear);
            _isShifting = true;
            _shiftTimer = 0f;
            _shiftUp = up;

            if (up)
                _virtualRpm *= 0.65f;
            else
                _virtualRpm *= 1.35f;

            _virtualRpm = MathHelper.Clamp(_virtualRpm, IDLE_RPM, REDLINE_RPM + 500f);

            PlayOneShot(up ? _gearShiftUpEmitter : _gearShiftDownEmitter, up ? "ctsSeengGearShiftUp" : "ctsSeengGearShiftDown");
        }

        private void UpdateLayers(float blendNorm, float loadMix)
        {
            float b33 = CalcLayerVolume(blendNorm, 0.0f, 0.1f, 0.3f, 0.5f);
            float b66 = CalcLayerVolume(blendNorm, 0.3f, 0.5f, 0.65f, 0.85f);
            float b99 = CalcLayerVolume(blendNorm, 0.65f, 0.85f, 1.05f, 1.2f);
            float idle = blendNorm <= 0.1f ? MathHelper.SmoothStep(1f, 0f, blendNorm / 0.1f) : 0f;

            SetVolume(_idleT, idle);
            SetVolume(_base33T, b33 * (1f - loadMix));
            SetVolume(_base66T, b66 * (1f - loadMix));
            SetVolume(_base99T, b99 * (1f - loadMix));
            SetVolume(_load33T, b33 * loadMix);
            SetVolume(_load66T, b66 * loadMix);
            SetVolume(_load99T, b99 * loadMix);

            ApplyLayerPitch(_idleT, _smoothedRpmNorm, 0.2f);
            ApplyLayerPitch(_base33T, _smoothedRpmNorm, 0.33f);
            ApplyLayerPitch(_base66T, _smoothedRpmNorm, 0.66f);
            ApplyLayerPitch(_base99T, _smoothedRpmNorm, 0.99f);
            ApplyLayerPitch(_load33T, _smoothedRpmNorm, 0.33f);
            ApplyLayerPitch(_load66T, _smoothedRpmNorm, 0.66f);
            ApplyLayerPitch(_load99T, _smoothedRpmNorm, 0.99f);
        }

        // ---

        private void HandleRevAndRelease(ThrottleThrusterManager throttle)
        {
            bool currentActive = throttle.IsForwardThrottling || throttle.IsReverseThrottling || (_isTrackedVehicle && throttle.IsSkidSteering);
            if (currentActive && !_wasThrottleActive)
            {
                if (!_revCooldown.IsRunning || _revCooldown.Elapsed.TotalSeconds >= REV_COOLDOWN_TIME)
                {
                    PlayOneShot(_revEmitter, "ctsSeengEngineRev");
                    _revCooldown.Restart();
                }
            }

            if (!currentActive && _wasThrottleActive)
            {
                if (!_relCooldown.IsRunning || _relCooldown.Elapsed.TotalSeconds >= REL_COOLDOWN_TIME)
                {
                    PlayOneShot(_releaseEmitter, "ctSeengEngineRelease");
                    _relCooldown.Restart();
                }
            }
            _wasThrottleActive = currentActive;
        }

        private void PlayOneShot(MyEntity3DSoundEmitter emitter, string baseCueName)
        {
            if (emitter == null) return;
            emitter.PlaySound(new MySoundPair(GetFullCue(baseCueName)));
        }

        private void CreateEmitter(ref MyEntity3DSoundEmitter emitter, MyEntity entity, string cueName, bool looped)
        {
            if (emitter != null) emitter.StopSound(true);
            emitter = new MyEntity3DSoundEmitter(entity, looped, 0f) { Force3D = true };
            emitter.PlaySound(new MySoundPair(cueName));
        }

        private void SetVolume(MyEntity3DSoundEmitter emitter, float vol)
        {
            if (emitter?.Sound != null) emitter.Sound.VolumeMultiplier = MathHelper.Clamp(vol, 0f, 1f);
        }

        private void ApplyLayerPitch(MyEntity3DSoundEmitter emitter, float rpmNorm, float layerRpmNorm)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying) return;
            float ratio = Math.Max(rpmNorm / Math.Max(layerRpmNorm, 0.01f), 0.1f);
            float semitones = (float)(12 * Math.Log(ratio, 2));
            emitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(MathHelper.Clamp(semitones, -6f, 6f));
        }

        private float CalcLayerVolume(float norm, float fiS, float fiE, float foS, float foE)
        {
            if (norm < fiS || norm > foE) return 0f;
            if (norm <= fiE) return MathHelper.SmoothStep(0f, 1f, (norm - fiS) / (fiE - fiS));
            if (norm >= foS) return MathHelper.SmoothStep(1f, 0f, (norm - foS) / (foE - foS));
            return 1f;
        }

        private string GetFullCue(string baseName) => string.IsNullOrEmpty(_prefix) ? baseName : $"{baseName}_{_prefix}";

        private void ResetState()
        {
            _virtualRpm = IDLE_RPM;
            _currentGear = 1;
            _loadIndicator = 0f;
            _isShifting = false;
            _revCooldown.Reset();
        }

        public void StopAll()
        {
            StopEmitter(ref _idleT); StopEmitter(ref _base33T); StopEmitter(ref _base66T);
            StopEmitter(ref _base99T); StopEmitter(ref _load33T); StopEmitter(ref _load66T); StopEmitter(ref _load99T); StopEmitter(ref _gearShiftUpEmitter);
            StopEmitter(ref _gearShiftDownEmitter);

            StopEmitter(ref _revEmitter);
            StopEmitter(ref _releaseEmitter);

            ResetState();
            _revCooldown.Reset();
        }
        public void Stop()
        {
            StopEmitter(ref _idleT); StopEmitter(ref _base33T); StopEmitter(ref _base66T); StopEmitter(ref _base99T);
            StopEmitter(ref _load33T); StopEmitter(ref _load66T); StopEmitter(ref _load99T);
            StopEmitter(ref _gearShiftUpEmitter); StopEmitter(ref _gearShiftDownEmitter);
            StopEmitter(ref _revEmitter); StopEmitter(ref _releaseEmitter);
            ResetState();
        }

        private void StopEmitter(ref MyEntity3DSoundEmitter emitter)
        {
            if (emitter != null)
            {
                emitter.StopSound(true);
                emitter = null;
            }
        }
    }
}