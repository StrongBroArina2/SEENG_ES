using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Audio;
using VRageMath;
using System.Diagnostics;
using VRage.ModAPI;
using SEENG_SElauncher.SEENG_Managers;

namespace SEENG_ES
{
    public class SND_CT_EngineHandler
    {
        public MyEntity3DSoundEmitter _idleT;
        public MyEntity3DSoundEmitter _base33T;
        public MyEntity3DSoundEmitter _base66T;
        public MyEntity3DSoundEmitter _base99T;
        public MyEntity3DSoundEmitter _load33T;
        public MyEntity3DSoundEmitter _load66T;
        public MyEntity3DSoundEmitter _load99T;
        public MyEntity3DSoundEmitter _gearShiftUpEmitter;
        public MyEntity3DSoundEmitter _gearShiftDownEmitter;
        public MyEntity3DSoundEmitter _revEmitter;
        public MyEntity3DSoundEmitter _releaseEmitter;
        private readonly Stopwatch _revCooldown = new Stopwatch();
        private const float REV_COOLDOWN_TIME = 6.0f;
        private readonly Stopwatch _relCooldown = new Stopwatch();
        private const float REL_COOLDOWN_TIME = 6.0f;
        private bool _wasThrottleActive = false;
        private bool _isTrackedVehicle = false;

        private string _prefix = "";



        private int _currentGear = 1;
        private float _virtualRpm = 800f;
        private float _smoothedRpmNorm = 0f;
        private float _shiftTimer = 0f;
        private bool _isShifting = false;
        private bool _shiftUp = false;

        private const float IDLE_RPM = 800f;
        private const float REDLINE_RPM = 4500f;
        private const float SHIFT_TIME = 0.4f;

        private const float RPM_BLEND_WEIGHT = 0.4f;

        private float[] GearRatios => _config.GearRatios.Count > 1 ? _config.GearRatios.ToArray() : new float[] { 0f, 3.0f, 2.2f, 1.5f, 1.1f, 0.85f };
        private float[] UpshiftRpm => _config.UpshiftRPM.Count > 1 ? _config.UpshiftRPM.ToArray() : new float[] { 0f, 4000f, 4100f, 4200f, 4300f, 4400f };
        private float[] DownshiftRpm => _config.DownshiftRPM.Count > 1 ? _config.DownshiftRPM.ToArray() : new float[] { 0f, 3400f, 3300f, 3200f, 3100f, 3000f };
        private int MaxGear => GearRatios.Length - 1;
        private SEENG_TransmissionConfig _config = SEENG_TransmissionConfig.Default;
        private float _loadIndicator = 0f;

        public void Start(IMyCockpit cockpit, string prefix, SEENG_TransmissionConfig transmissionConfig = null)
        {
            if (cockpit == null) return;

            _prefix = prefix;
            _config = transmissionConfig ?? SEENG_TransmissionConfig.Default;
            _isTrackedVehicle = _config.SkidSteering;

            var entity = (MyEntity)(IMyEntity)cockpit;

            CreateEmitter(ref _idleT, entity, GetFullCue("ctSeengEngineIdle"), true);
            CreateEmitter(ref _base33T, entity, GetFullCue("ctSeengEngine33"), true);
            CreateEmitter(ref _base66T, entity, GetFullCue("ctSeengEngine66"), true);
            CreateEmitter(ref _base99T, entity, GetFullCue("ctSeengEngine99"), true);
            CreateEmitter(ref _load33T, entity, GetFullCue("ctSeengEngineLoad33"), true);
            CreateEmitter(ref _load66T, entity, GetFullCue("ctSeengEngineLoad66"), true);
            CreateEmitter(ref _load99T, entity, GetFullCue("ctSeengEngineLoad99"), true);

            CreateEmitter(ref _gearShiftUpEmitter, entity, GetFullCue("ctSeengGearShiftUp"), false);
            CreateEmitter(ref _gearShiftDownEmitter, entity, GetFullCue("ctSeengGearShiftDown"), false);
            CreateEmitter(ref _revEmitter, entity, GetFullCue("ctSeengEngineRev"), false);
            CreateEmitter(ref _releaseEmitter, entity, GetFullCue("ctSeengEngineRelease"), false);

            ResetState();
            _revCooldown.Reset();
            _wasThrottleActive = false;
        }

        public void Update(ThrustManager thrustManager, SpeedManager speedManager, ThrottleThrusterManager throttleManager)
        {
            if (_idleT == null) return;

            float speed = speedManager.NormalizedSpeed;
            bool isThrusting = thrustManager.IsThrusting;
            HandleRevAndRelease(throttleManager);
            float ratePerSecond = 50f;
            bool shouldApplyLoad = false;


            if (_isTrackedVehicle)
            {
                shouldApplyLoad = throttleManager.IsForwardThrottling ||
                                  throttleManager.IsReverseThrottling ||
                                  throttleManager.IsSkidSteering;
            }
            else
            {
                shouldApplyLoad = throttleManager.IsForwardThrottling || throttleManager.IsReverseThrottling;
            }

            float deltaLoad = (shouldApplyLoad ? ratePerSecond : -ratePerSecond) * (1f / 60f);
            _loadIndicator = MathHelper.Clamp(_loadIndicator + deltaLoad, 0f, 100f);

            float loadMix = _loadIndicator / 100f;
            float effectiveLoadMix = _isShifting ? MathHelper.Lerp(loadMix, 0f, _shiftTimer / SHIFT_TIME) : loadMix;

            float targetRpm = IDLE_RPM;
            if (speed > 0.02f)
            {
                float gearRatio = GearRatios[_currentGear];
                targetRpm = IDLE_RPM + (speed * (REDLINE_RPM - IDLE_RPM) * gearRatio * 1.3f);
                targetRpm = MathHelper.Clamp(targetRpm, IDLE_RPM, REDLINE_RPM + 200f);
            }

            float rpmDiff = Math.Abs(targetRpm - _virtualRpm);
            float adaptiveMultiplier = 1f + (rpmDiff / 500f);
            float virtualLerpSpeed = (_isShifting ? 20f : (isThrusting ? 12f : 6f)) * adaptiveMultiplier;

            _virtualRpm = MathHelper.Lerp(_virtualRpm, targetRpm, virtualLerpSpeed * (1f / 60f));

            if (!_isShifting && speed > 0.05f)
            {
                if (_currentGear < GearRatios.Length)
                {
                    float upshiftThreshold = UpshiftRpm[_currentGear];
                    float downshiftThreshold = DownshiftRpm[_currentGear] * 0.9f;
                    if (_virtualRpm >= upshiftThreshold && _currentGear < MaxGear)
                        ShiftGear(true);
                    else if (_virtualRpm <= downshiftThreshold && _currentGear > 1)
                        ShiftGear(false);
                }
            }

            if (_isShifting)
            {
                _shiftTimer += 1f / 60f;
                if (_shiftTimer >= SHIFT_TIME)
                {
                    _isShifting = false;
                    _shiftTimer = 0f;
                    if (_shiftUp) _virtualRpm *= 0.7f;
                    else _virtualRpm *= 1.3f;
                    _virtualRpm = MathHelper.Clamp(_virtualRpm, IDLE_RPM, REDLINE_RPM + 500f);
                }
            }

            if (speed < 0.05f) _currentGear = 1;


            float rawRpmNorm = (_virtualRpm - IDLE_RPM) / (REDLINE_RPM - IDLE_RPM);
            float smoothLerpSpeed = _isShifting ? 2f : (isThrusting ? 4f : 3f);
            _smoothedRpmNorm = MathHelper.Lerp(_smoothedRpmNorm, rawRpmNorm, smoothLerpSpeed * (1f / 60f));
            float blendNorm = MathHelper.Lerp(speed, _smoothedRpmNorm, RPM_BLEND_WEIGHT);

            float base33Vol = CalcLayerVolume(blendNorm, 0.0f, 0.1f, 0.3f, 0.5f);
            float base66Vol = CalcLayerVolume(blendNorm, 0.3f, 0.5f, 0.65f, 0.85f);
            float base99Vol = CalcLayerVolume(blendNorm, 0.65f, 0.85f, 1.05f, 1.2f);
            float idleVol = speed <= 0.1f ? MathHelper.SmoothStep(1f, 0f, speed / 0.1f) * (1f - _smoothedRpmNorm * 0.4f) : 0f;

            SetVolume(_idleT, idleVol);
            SetVolume(_base33T, base33Vol * (1f - effectiveLoadMix));
            SetVolume(_base66T, base66Vol * (1f - effectiveLoadMix));
            SetVolume(_base99T, base99Vol * (1f - effectiveLoadMix));
            SetVolume(_load33T, base33Vol * effectiveLoadMix);
            SetVolume(_load66T, base66Vol * effectiveLoadMix);
            SetVolume(_load99T, base99Vol * effectiveLoadMix);

            ApplyLayerPitch(_base33T, _smoothedRpmNorm, 0.33f);
            ApplyLayerPitch(_base66T, _smoothedRpmNorm, 0.66f);
            ApplyLayerPitch(_base99T, _smoothedRpmNorm, 0.99f);
            ApplyLayerPitch(_load33T, _smoothedRpmNorm, 0.33f);
            ApplyLayerPitch(_load66T, _smoothedRpmNorm, 0.66f);
            ApplyLayerPitch(_load99T, _smoothedRpmNorm, 0.99f);
        }

        private void HandleRevAndRelease(ThrottleThrusterManager throttle)
        {
            bool currentThrottleActive = throttle.IsForwardThrottling ||
                                         throttle.IsReverseThrottling ||
                                         (_isTrackedVehicle && throttle.IsSkidSteering);

            if (currentThrottleActive && !_wasThrottleActive)
            {
                if (!_revCooldown.IsRunning || _revCooldown.Elapsed.TotalSeconds >= REV_COOLDOWN_TIME)
                {
                    PlayOneShot(_revEmitter, "ctSeengEngineRev");
                    _revCooldown.Restart();
                }
            }

            if (!currentThrottleActive && _wasThrottleActive)
            {
                if (!_relCooldown.IsRunning || _relCooldown.Elapsed.TotalSeconds >= REL_COOLDOWN_TIME)
                {
                    PlayOneShot(_releaseEmitter, "ctSeengEngineRelease");
                    _relCooldown.Restart();
                }
            }

            _wasThrottleActive = currentThrottleActive;
        }

        public void SetTrackedVehicleMode(bool isTracked)
        {
            _isTrackedVehicle = isTracked;
        }

        private void ShiftGear(bool up)
        {
            _currentGear += up ? 1 : -1;
            _currentGear = MathHelper.Clamp(_currentGear, 1, MaxGear);

            _isShifting = true;
            _shiftTimer = 0f;
            _shiftUp = up;

            if (up)
                PlayOneShot(_gearShiftUpEmitter, "ctSeengGearShiftUp");
            else
                PlayOneShot(_gearShiftDownEmitter, "ctSeengGearShiftDown");
        }

        private void PlayOneShot(MyEntity3DSoundEmitter emitter, string baseCueName)
        {
            if (emitter == null) return;

            string fullCue = GetFullCue(baseCueName);
            var soundPair = new MySoundPair(fullCue);
            emitter.PlaySound(soundPair);
        }

        // ==================== bullshit ====================

        private string GetFullCue(string baseName)
        {
            return string.IsNullOrEmpty(_prefix)
                ? baseName
                : $"{baseName}_{_prefix}";
        }

        private void CreateEmitter(ref MyEntity3DSoundEmitter emitter, MyEntity entity, string cueName, bool looped)
        {
            if (emitter != null)
            {
                emitter.StopSound(true);
                emitter = null;
            }

            emitter = new MyEntity3DSoundEmitter(entity, looped, 0f) { Force3D = true };
            var soundPair = new MySoundPair(cueName);

            if (!emitter.PlaySound(soundPair))
            {
                emitter = null;
            }
        }

        private void SetVolume(MyEntity3DSoundEmitter emitter, float volume)
        {
            if (emitter?.Sound != null)
                emitter.Sound.VolumeMultiplier = MathHelper.Clamp(volume, 0f, 1f);
        }

        private void ApplyLayerPitch(MyEntity3DSoundEmitter emitter, float rpmNorm, float layerRpmNorm)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying) return;

            float ratio = Math.Max(rpmNorm / Math.Max(layerRpmNorm, 0.01f), 0.1f);
            float semitones = (float)(12 * Math.Log(ratio, 2));
            semitones = MathHelper.Clamp(semitones, -13f, 13f);
            emitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
        }

        private float CalcLayerVolume(float norm, float fadeInStart, float fadeInEnd, float fadeOutStart, float fadeOutEnd)
        {
            if (norm < fadeInStart) return 0f;
            if (norm > fadeOutEnd) return 0f;
            if (norm <= fadeInEnd)
                return MathHelper.SmoothStep(0f, 1f, (norm - fadeInStart) / (fadeInEnd - fadeInStart));
            if (norm >= fadeOutStart)
                return MathHelper.SmoothStep(1f, 0f, (norm - fadeOutStart) / (fadeOutEnd - fadeOutStart));
            return 1f;
        }

        private void ResetState()
        {
            _virtualRpm = IDLE_RPM;
            _smoothedRpmNorm = 0f;
            _currentGear = 1;
            _loadIndicator = 0f;
            _isShifting = false;
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
            StopEmitter(ref _idleT);
            StopEmitter(ref _base33T);
            StopEmitter(ref _base66T);
            StopEmitter(ref _base99T);
            StopEmitter(ref _load33T);
            StopEmitter(ref _load66T);
            StopEmitter(ref _load99T);

            StopEmitter(ref _gearShiftUpEmitter);
            StopEmitter(ref _gearShiftDownEmitter);
            StopEmitter(ref _revEmitter);
            StopEmitter(ref _releaseEmitter);

            ResetState();
            _revCooldown.Reset();
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