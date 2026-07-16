using System;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SEENG_SElauncher.SEENG_CFG_SYS;
using SEENG_SElauncher.SEENG_Managers;
using VRage.Utils;
using VRageMath;
using static SEENG_SElauncher.SEENG_Managers.SpeedManager;

namespace SEENG_ES
{
    public class SoundHandler
    {
        private MyEntity3DSoundEmitter _engineLoopEmitter;
        private MyEntity3DSoundEmitter _acdcEmitter;
        private MyEntity3DSoundEmitter _engineLoop50Emitter;
        private MyEntity3DSoundEmitter _moveAmbienceEmitter;
        private MyEntity3DSoundEmitter _stationaryAmbienceEmitter;
        private MyEntity3DSoundEmitter _constantAmbienceEmitter;
        private MyEntity3DSoundEmitter _mThrustersEmitter;
        private SND_ACDC_ADV _acdcAdvHandler = new SND_ACDC_ADV();
        private SND_SpeedUpDown _speedUpDown = new SND_SpeedUpDown();

        private readonly SND_EngineLoopHandler _engineLoopHandler = new SND_EngineLoopHandler();
        private readonly SND_EngineLoop50Handler _engineLoop50Handler = new SND_EngineLoop50Handler();
        //private readonly SND_acdcHandler _acdcHandler = new SND_acdcHandler();
        private readonly SND_MoveAmbienceHandler _moveAmbienceHandler = new SND_MoveAmbienceHandler();
        private readonly SND_StationaryAmbienceHandler _stationaryAmbienceHandler = new SND_StationaryAmbienceHandler();
        private readonly SND_ConstantAmbienceHandler _constantAmbienceHandler = new SND_ConstantAmbienceHandler();
        private readonly SND_mThrustersHandler _mThrustersHandler = new SND_mThrustersHandler();

        private readonly SND_C_EngineHandler _cEngineHandler = new SND_C_EngineHandler();
        private readonly SND_CT_EngineHandler _ctEngineHandler = new SND_CT_EngineHandler();
        private readonly SND_CTS_EngineHandler _ctsEngineHandler = new SND_CTS_EngineHandler();
        private readonly SND_C_TracksHandler _cTracksHandler = new SND_C_TracksHandler();
        private readonly SND_C_WheelsHandler _cWheelsHandler = new SND_C_WheelsHandler();

        private readonly SND_MainThrusterHandler _mainThrusterHandler = new SND_MainThrusterHandler();
        private readonly SND_ManeuverThrustersHandler _maneuverThrustersHandler = new SND_ManeuverThrustersHandler();

        private readonly SND_EngineLoopPowerHandler _engineLoopPowerHandler = new SND_EngineLoopPowerHandler();
        private MyEntity3DSoundEmitter _engineLoopPowerEmitter;

        //private readonly SND_MagLockHandler _magLockHandler = new SND_MagLockHandler();
        //private readonly SND_RotorHandler _RotorHandler = new SND_RotorHandler();
        //private readonly SND_PistonHandler _PistonLockHandler = new SND_PistonHandler();
        //private readonly SND_InventoryHandler _InventoryHandler = new SND_InventoryHandler();

        public SND_CT_EngineHandler GetCTEngineHandler() => _ctEngineHandler;
        public SND_CTS_EngineHandler GetCTSEngineHandler() => _ctsEngineHandler;
        public void UpdateAllSounds(IMyCockpit cockpit, string prefix, ManagersUpdater managers, PackConfig config)
        {
            if (cockpit == null || managers == null) return;
            float normalizedSpeed = managers.SpeedManager.NormalizedSpeed;

            //1.0
            SEENG_enginesParametrs.UpdatePitchForEmitter(_engineLoopEmitter, normalizedSpeed, config.MaxEnginePitchShift);
            SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoopEmitter, normalizedSpeed, config.EngineVolumes);
            _engineLoop50Handler.UpdatePitchForLoop50(_engineLoop50Emitter, normalizedSpeed, config.MaxEngine50PitchShift);
            SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoop50Emitter, normalizedSpeed, config.Engine50Volumes);
           // _acdcHandler.UpdateAcdcVolume(_acdcEmitter, speedManager);
            _moveAmbienceHandler.UpdateMoveAmbienceVolume(_moveAmbienceEmitter, normalizedSpeed);
            _stationaryAmbienceHandler.UpdateStationaryAmbienceVolume(_stationaryAmbienceEmitter, normalizedSpeed);

            //1.2
            _mThrustersHandler.Update(_mThrustersEmitter, managers.RotationManager, managers.SpeedManager);
            _mainThrusterHandler.Update(cockpit, managers.ThrustManager);
            _maneuverThrustersHandler.Update(cockpit, managers.RotationManager, managers.SpeedManager);
            _acdcAdvHandler.Update(cockpit, managers.SpeedManager, config.MaxACDCAdvPitchSemitones);
            _speedUpDown.Update(cockpit, managers.SpeedManager);

            //1.3
            _cEngineHandler.Update(managers.ThrustManager, managers.SpeedManager);
            _ctEngineHandler.Update(managers.ThrustManager, managers.SpeedManager, managers.ThrottleThrusterManager);
            _ctsEngineHandler.Update(managers.ThrustManager, managers.SpeedManager, managers.ThrottleThrusterManager);
            _cTracksHandler.Update(managers.SpeedManager);
            _cWheelsHandler.Update(managers.SpeedManager);

            
             //_magLockHandler.Update(blockManager);
           // _RotorHandler.Update(blockManager);
           // _PistonLockHandler.Update(blockManager);
            //_InventoryHandler.Update(blockManager);


            UpdateEmitter3D(_engineLoopEmitter, cockpit);
            UpdateEmitter3D(_engineLoop50Emitter, cockpit);
            UpdateEmitter3D(_moveAmbienceEmitter, cockpit);
            UpdateEmitter3D(_stationaryAmbienceEmitter, cockpit);
            UpdateEmitter3D(_constantAmbienceEmitter, cockpit);
            UpdateEmitter3D(_mThrustersEmitter, cockpit);

            _engineLoopPowerHandler.Update(_engineLoopPowerEmitter, managers.PowerManager.PowerLoadPercent, 12f);

            var emitters = new[]
            {
    _engineLoopEmitter,   _engineLoop50Emitter,    _moveAmbienceEmitter,    _stationaryAmbienceEmitter,    _constantAmbienceEmitter,
    _mThrustersEmitter,  _engineLoopPowerEmitter,   _acdcAdvHandler._accelEmitter,   _acdcAdvHandler._deccelEmitter,    _speedUpDown._emitter,   _mainThrusterHandler._loopEmitter,   _mainThrusterHandler._startEmitter,   _mainThrusterHandler._endEmitter,
    _cTracksHandler._track33,    _cTracksHandler._track66,    _cTracksHandler._track99,
    _cWheelsHandler._wheels33,    _cWheelsHandler._wheels66,    _cWheelsHandler._wheels99,
    _cEngineHandler._base33,    _cEngineHandler._base66,    _cEngineHandler._base99, _cEngineHandler._load33,    _cEngineHandler._load66,    _cEngineHandler._load99, _cEngineHandler._idle,
    _ctEngineHandler._base33T,    _ctEngineHandler._base66T,    _ctEngineHandler._base99T, _ctEngineHandler._load33T,    _ctEngineHandler._load66T,    _ctEngineHandler._load99T, _ctEngineHandler._idleT,
    _ctEngineHandler._gearShiftUpEmitter,    _ctEngineHandler._gearShiftDownEmitter,    _ctEngineHandler._revEmitter, _ctEngineHandler._releaseEmitter,
    _ctsEngineHandler._base33T,    _ctsEngineHandler._base66T,    _ctsEngineHandler._base99T, _ctsEngineHandler._load33T,    _ctsEngineHandler._load66T,    _ctsEngineHandler._load99T, _ctsEngineHandler._idleT,
    _ctsEngineHandler._gearShiftUpEmitter,    _ctsEngineHandler._gearShiftDownEmitter,    _ctsEngineHandler._revEmitter, _ctsEngineHandler._releaseEmitter,
            };

            foreach (var emitter in emitters)
                ApplyGlobalVolume(emitter);

        }

        public void RestartAll(IMyCockpit cockpit, string prefix, ManagersUpdater managers, SEENG_TransmissionConfig transmissionConfig)
        {
            StopAll();Dispose();if (cockpit == null || cockpit.MarkedForClose || cockpit.Closed) return;
            void DIENT(string baseName, Action startAction) // DO I EVEN NEED this handler on my pack
            {string fullName = string.IsNullOrEmpty(prefix) ? baseName : $"{baseName}_{prefix}";
            if (MySoundPair.GetCueId(fullName).IsNull) return;try{startAction();}
            catch (Exception e){ MyLog.Default.WriteLine($"SEENG Error {baseName}: {e.Message}");}}
            // 1.0
            DIENT("SeengEngineLoop", () => _engineLoopHandler.Start(ref _engineLoopEmitter, cockpit, prefix));
            DIENT("SeengEngineLoop50", () => _engineLoop50Handler.Start(ref _engineLoop50Emitter, cockpit, prefix));
            //DIENT("SeengEngineAcDc", () => _acdcHandler.Start(ref _acdcEmitter, cockpit, prefix));
            DIENT("SeengMoveAmbience", () => _moveAmbienceHandler.Start(ref _moveAmbienceEmitter, cockpit, prefix));
            DIENT("SeengStationaryAmbience", () => _stationaryAmbienceHandler.Start(ref _stationaryAmbienceEmitter, cockpit, prefix));
            DIENT("SeengAmbienceConstant", () => _constantAmbienceHandler.Start(ref _constantAmbienceEmitter, cockpit, prefix));

            // 1.1
            DIENT("SeengmThrusters", () => _mThrustersHandler.Start(ref _mThrustersEmitter, cockpit, prefix));
            DIENT("SeengMainThrusterLoop", () => _mainThrusterHandler.Start(cockpit, prefix));
            DIENT("SeengACDCacc", () => _acdcAdvHandler.Start(cockpit, prefix));
            DIENT("SeengSpeedUP", () => _speedUpDown.Start(cockpit, prefix));
            DIENT("SeengManeuverThrusters", () => _maneuverThrustersHandler.Restart(cockpit, prefix));

            // 1.3
            DIENT("cSeengEngineIdle", () => _cEngineHandler.Start(cockpit, prefix));
            DIENT("ctSeengEngineIdle", () => _ctEngineHandler.Start(cockpit, prefix, transmissionConfig));
            DIENT("ctsSeengEngineIdle", () => _ctsEngineHandler.Start(cockpit, prefix, transmissionConfig));
            DIENT("cSeengTrack33", () => _cTracksHandler.Start(cockpit, prefix));
            DIENT("cSeengWheel33", () => _cWheelsHandler.Start(cockpit, prefix));

            /// 1.4 wip
            //DIENT("bSeengMagLock", () => _magLockHandler.Start(cockpit, prefix));
            //DIENT("bSeengRotorLoop", () => _RotorHandler.Start(cockpit, prefix));
            //DIENT("bSeengPistonLoop", () => _PistonLockHandler.Start(cockpit, prefix));
            //DIENT("bSeengInventoryAdd", () => _InventoryHandler.Start(cockpit, prefix));

            DIENT("SeengEngineLoopPower", () => _engineLoopPowerHandler.Start(ref _engineLoopPowerEmitter, cockpit, prefix));
        }

        private void UpdateEmitter3D(MyEntity3DSoundEmitter emitter, IMyCockpit cockpit)
        {
            if (emitter == null || cockpit == null) return;
            emitter.Update();
        }

        private void ApplyGlobalVolume(MyEntity3DSoundEmitter emitter)
        {
            if (emitter?.Sound != null && emitter.Sound.IsPlaying)
            {
                float newVolume = emitter.Sound.VolumeMultiplier * SEENG_VolumeManager1.GetMultiplier();
                emitter.Sound.VolumeMultiplier = MathHelper.Clamp(newVolume, 0f, 2f);
            }
        }

        public void StopAll()
        {   
            //1.0
            _engineLoopEmitter?.StopSound(true);
            _acdcEmitter?.StopSound(true);
            _engineLoop50Emitter?.StopSound(true);
            _moveAmbienceEmitter?.StopSound(true);
            _stationaryAmbienceEmitter?.StopSound(true);
            _constantAmbienceEmitter?.StopSound(true);
            //1.1
            _mThrustersEmitter?.StopSound(true);
            _mainThrusterHandler.StopAll();
            _maneuverThrustersHandler.StopAll();
            _acdcAdvHandler.StopAll();
            _speedUpDown.StopAll();
            //1.3
            _cEngineHandler.StopAll();
            _ctEngineHandler.StopAll();
            _ctsEngineHandler.StopAll();
            _cTracksHandler.StopAll();
            _cWheelsHandler.StopAll();

            _engineLoopPowerEmitter?.StopSound(true);

            //_magLockHandler.StopAll();
            //_RotorHandler.StopAll();
            //_PistonLockHandler.StopAll();
            //_InventoryHandler.StopAll();
        }

        public void Dispose()
        {
            StopAll();
            _engineLoopEmitter = null;
            _acdcEmitter = null;
            _engineLoop50Emitter = null;
            _moveAmbienceEmitter = null;
            _stationaryAmbienceEmitter = null;
            _constantAmbienceEmitter = null;
            _mThrustersEmitter = null;
            _engineLoopPowerEmitter = null;

            //_magLockHandler.Dispose();

        }
    }
}