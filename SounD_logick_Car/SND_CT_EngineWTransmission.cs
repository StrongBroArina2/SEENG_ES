using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Audio;
using VRageMath;
using System.Diagnostics;
using VRage.ModAPI;

namespace SEENG_ES
{
    public  class SND_CT_EngineHandler
    {
        public  MyEntity3DSoundEmitter _idleT;
        public  MyEntity3DSoundEmitter _base33T;
        public  MyEntity3DSoundEmitter _base66T;
        public  MyEntity3DSoundEmitter _base99T;
        public  MyEntity3DSoundEmitter _load33T;
        public  MyEntity3DSoundEmitter _load66T;
        public  MyEntity3DSoundEmitter _load99T;

        private  int _currentGear = 1;
        private  float _virtualRpm = 800f;
        private  float _smoothedRpmNorm = 0f;
        private  float _shiftTimer = 0f;
        private  bool _isShifting = false;
        private  bool _shiftUp = false;

        private const float IDLE_RPM = 800f;
        private const float REDLINE_RPM = 4500f;
        private const float SHIFT_TIME = 0.4f;

// mix
        private const float RPM_BLEND_WEIGHT = 0.4f;

        private  readonly float[] GearRatios = { 0f, 3.8f, 2.2f, 1.5f, 1.1f, 0.85f };
        private  readonly float[] UpshiftRpm = { 0f, 4000f, 4100f, 4200f, 4300f, 4400f };
        private  readonly float[] DownshiftRpm = { 0f, 1800f, 1700f, 1600f, 1500f, 1400f };

        private  float _loadIndicator = 0f;
        private  readonly Stopwatch _loadTimer = new Stopwatch();

        public  void Start(IMyCockpit cockpit, string prefix)
        {
            if (cockpit == null) return;

            var entity = (MyEntity)(IMyEntity)cockpit;

            CreateEmitter(ref _idleT, entity, $"ctSeengEngineIdle_{prefix}", true);
            CreateEmitter(ref _base33T, entity, $"ctSeengEngine33_{prefix}", true);
            CreateEmitter(ref _base66T, entity, $"ctSeengEngine66_{prefix}", true);
            CreateEmitter(ref _base99T, entity, $"ctSeengEngine99_{prefix}", true);
            CreateEmitter(ref _load33T, entity, $"ctSeengEngineLoad33_{prefix}", true);
            CreateEmitter(ref _load66T, entity, $"ctSeengEngineLoad66_{prefix}", true);
            CreateEmitter(ref _load99T, entity, $"ctSeengEngineLoad99_{prefix}", true);

            _virtualRpm = IDLE_RPM;
            _smoothedRpmNorm = 0f;
            _currentGear = 1;
            _loadTimer.Start();
        }

        public  void Update(ThrustManager thrustManager, SpeedManager speedManager)
        {
            if (_idleT == null) return;

            float speed = speedManager.NormalizedSpeed; 
            bool isThrusting = thrustManager.IsThrusting;

            // Load
            float ratePerSecond = 50f;
            float deltaLoad = (isThrusting ? ratePerSecond : -ratePerSecond) * (1f / 60f);
            _loadIndicator = MathHelper.Clamp(_loadIndicator + deltaLoad, 0f, 100f);
            float loadMix = _loadIndicator / 100f;
            float effectiveLoadMix = _isShifting ? MathHelper.Lerp(loadMix, 0f, _shiftTimer / SHIFT_TIME) : loadMix;

            //Virtual RPM
            float targetRpm = IDLE_RPM;
            if (speed > 0.02f)
            {
                float gearRatio = GearRatios[_currentGear];
                targetRpm = IDLE_RPM + (speed * (REDLINE_RPM - IDLE_RPM) * gearRatio * 1.3f);
                targetRpm = MathHelper.Clamp(targetRpm, IDLE_RPM, REDLINE_RPM + 200f);
            }

            float virtualLerpSpeed = _isShifting ? 20f : (isThrusting ? 12f : 6f);
            _virtualRpm = MathHelper.Lerp(_virtualRpm, targetRpm, virtualLerpSpeed * (1f / 60f));

            if (!_isShifting && speed > 0.05f)
            {
                if (_virtualRpm >= UpshiftRpm[_currentGear] && _currentGear < 5)
                    ShiftGear(true);
                else if (_virtualRpm <= DownshiftRpm[_currentGear] && _currentGear > 1)
                    ShiftGear(false);
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

            ApplyLayerPitch(_idleT, _smoothedRpmNorm, 0.2f);
            ApplyLayerPitch(_base33T, _smoothedRpmNorm, 0.33f);
            ApplyLayerPitch(_base66T, _smoothedRpmNorm, 0.66f);
            ApplyLayerPitch(_base99T, _smoothedRpmNorm, 0.99f);
            ApplyLayerPitch(_load33T, _smoothedRpmNorm, 0.33f);
            ApplyLayerPitch(_load66T, _smoothedRpmNorm, 0.66f);
            ApplyLayerPitch(_load99T, _smoothedRpmNorm, 0.99f);

            // deb
            if (MyAPIGateway.Session?.Player != null)
            {
                string shiftInfo = _isShifting ? $"SHIFT {(_shiftUp ? "UP" : "DN")} ({(_shiftTimer * 100f / SHIFT_TIME):F0}%)" : $"Gear {_currentGear}";
                string debugText = $"[Transmission + BLEND]\n" +
                                   $"{shiftInfo}\n" +
                                   $"Speed: {(speed * 100f):F1}% | RPM: {(_smoothedRpmNorm * 100f):F1}% | BLEND: {(blendNorm * 100f):F1}%\n" +
                                   $"Load: {_loadIndicator:F0}% (eff: {effectiveLoadMix:F2})\n" +
                                   $"Vols: 33:{base33Vol:F2} 66:{base66Vol:F2} 99:{base99Vol:F2}";

                string color = isThrusting ? "Green" : (_loadIndicator > 10 ? "Yellow" : "White");
               // MyAPIGateway.Utilities.ShowNotification(debugText, 16, color);
            }
        }
        private  void ShiftGear(bool up)
        {
            _currentGear += up ? 1 : -1;
            _currentGear = MathHelper.Clamp(_currentGear, 1, 5);
            _isShifting = true;
            _shiftTimer = 0f;
            _shiftUp = up;
        }

        public  void Stop()
        {
            StopEmitter(ref _idleT);
            StopEmitter(ref _base33T);
            StopEmitter(ref _base66T);
            StopEmitter(ref _base99T);
            StopEmitter(ref _load33T);
            StopEmitter(ref _load66T);
            StopEmitter(ref _load99T);

            _virtualRpm = IDLE_RPM;
            _smoothedRpmNorm = 0f;
            _currentGear = 1;
            _loadIndicator = 0f;
            _isShifting = false;
        }

        private  void CreateEmitter(ref MyEntity3DSoundEmitter emitter, MyEntity entity, string cueName, bool looped)
        {
            if (emitter != null && emitter.Sound?.IsPlaying == true) return;
            if (emitter != null) { emitter.StopSound(true); emitter = null; }

            emitter = new MyEntity3DSoundEmitter(entity, looped, 0f) { Force3D = true };
            var soundPair = new MySoundPair(cueName);
            if (!emitter.PlaySound(soundPair))
            {
                emitter = null;
            }
        }

        private  void SetVolume(MyEntity3DSoundEmitter emitter, float volume)
        {
            if (emitter?.Sound != null)
                emitter.Sound.VolumeMultiplier = MathHelper.Clamp(volume, 0f, 1f);
        }

        private  void ApplyLayerPitch(MyEntity3DSoundEmitter emitter, float rpmNorm, float layerRpmNorm)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying) return;
            float ratio = Math.Max(rpmNorm / Math.Max(layerRpmNorm, 0.01f), 0.1f);
            float semitones = (float)(12 * Math.Log(ratio, 2));
            semitones = MathHelper.Clamp(semitones, -6f, 6f);
            emitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
        }

        public void StopAll()
        {
            StopEmitter(ref _idleT); StopEmitter(ref _base33T); StopEmitter(ref _base66T);
            StopEmitter(ref _base99T); StopEmitter(ref _load33T); StopEmitter(ref _load66T); StopEmitter(ref _load99T);
        }
        private  void StopEmitter(ref MyEntity3DSoundEmitter emitter)
        {
            emitter?.StopSound(true);
            emitter = null;
        }

        private  float CalcLayerVolume(float norm, float fadeInStart, float fadeInEnd, float fadeOutStart, float fadeOutEnd)
        {
            if (norm < fadeInStart) return 0f;
            if (norm > fadeOutEnd) return 0f;
            if (norm <= fadeInEnd)
                return MathHelper.SmoothStep(0f, 1f, (norm - fadeInStart) / (fadeInEnd - fadeInStart));
            if (norm >= fadeOutStart)
                return MathHelper.SmoothStep(1f, 0f, (norm - fadeOutStart) / (fadeOutEnd - fadeOutStart));
            return 1f;
        }
    }
}