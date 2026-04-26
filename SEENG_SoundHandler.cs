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
        private readonly SND_acdcHandler _acdcHandler = new SND_acdcHandler();
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

        public SND_CT_EngineHandler GetCTEngineHandler() => _ctEngineHandler;
        public SND_CTS_EngineHandler GetCTSEngineHandler() => _ctsEngineHandler;

        public void UpdateAllSounds(IMyCockpit cockpit, string prefix, ThrustManager thrustManager, SpeedManager speedManager, RotationManager rotationManager, ThrottleThrusterManager throttleManager, PackConfig config, SEENG_TransmissionConfig transmissionConfig)
        {
            if (cockpit == null) return;
            float normalizedSpeed = speedManager.NormalizedSpeed;
            //1.0
            SEENG_enginesParametrs.UpdatePitchForEmitter(_engineLoopEmitter, normalizedSpeed, config.MaxEnginePitchShift);
            SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoopEmitter, normalizedSpeed, config.EngineVolumes);
            _engineLoop50Handler.UpdatePitchForLoop50(_engineLoop50Emitter, normalizedSpeed, config.MaxEngine50PitchShift);
            SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoop50Emitter, normalizedSpeed, config.Engine50Volumes);
            _acdcHandler.UpdateAcdcVolume(_acdcEmitter, speedManager);
            _moveAmbienceHandler.UpdateMoveAmbienceVolume(_moveAmbienceEmitter, normalizedSpeed);
            _stationaryAmbienceHandler.UpdateStationaryAmbienceVolume(_stationaryAmbienceEmitter, normalizedSpeed);

            //1.2
            _mThrustersHandler.Update(_mThrustersEmitter, rotationManager, speedManager);
            _mainThrusterHandler.Update(cockpit, thrustManager);
            _maneuverThrustersHandler.Update(cockpit, rotationManager, speedManager);
            _acdcAdvHandler.Update(cockpit, speedManager);
            _speedUpDown.Update(cockpit, speedManager);

            //1.3
            _cEngineHandler.Update(thrustManager, speedManager);
            _ctEngineHandler.Update(thrustManager, speedManager, throttleManager);
            _ctsEngineHandler.Update(thrustManager, speedManager, throttleManager);
            _cTracksHandler.Update(speedManager);
            _cWheelsHandler.Update(speedManager);
            

            UpdateEmitter3D(_engineLoopEmitter, cockpit);
            UpdateEmitter3D(_acdcEmitter, cockpit);
            UpdateEmitter3D(_engineLoop50Emitter, cockpit);
            UpdateEmitter3D(_moveAmbienceEmitter, cockpit);
            UpdateEmitter3D(_stationaryAmbienceEmitter, cockpit);
            UpdateEmitter3D(_constantAmbienceEmitter, cockpit);
            UpdateEmitter3D(_mThrustersEmitter, cockpit);
        }

        public void RestartAll(IMyCockpit cockpit, string prefix, ThrustManager thrustManager, SpeedManager speedManager, RotationManager rotationManager, ThrottleThrusterManager throttleManager, SEENG_TransmissionConfig transmissionConfig)
        {
            StopAll();
            Dispose();
            if (cockpit == null || cockpit.MarkedForClose || cockpit.Closed) return;

            Func<string, bool> SoundExists = (baseName) => {
                string fullName = string.IsNullOrEmpty(prefix) ? baseName : $"{baseName}_{prefix}";
                return !MySoundPair.GetCueId(fullName).IsNull;
            };
            // 1.0
            try { if (SoundExists("SeengEngineLoop")) _engineLoopHandler.Start(ref _engineLoopEmitter, cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG_ERR ENG_LOOP" + e.Message); }
            try { if (SoundExists("SeengEngineLoop50")) _engineLoop50Handler.Start(ref _engineLoop50Emitter, cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error Loop50: " + e.Message); }
            try { if (SoundExists("SeengEngineAcDc")) _acdcHandler.Start(ref _acdcEmitter, cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error AcDc: " + e.Message); }
            try { if (SoundExists("SeengMoveAmbience")) _moveAmbienceHandler.Start(ref _moveAmbienceEmitter, cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error MoveAmbience: " + e.Message); }
            try { if (SoundExists("SeengStationaryAmbience")) _stationaryAmbienceHandler.Start(ref _stationaryAmbienceEmitter, cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error StationaryAmbience: " + e.Message); }
            try { if (SoundExists("SeengAmbienceConstant")) _constantAmbienceHandler.Start(ref _constantAmbienceEmitter, cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error Constant: " + e.Message); }
            //1.1
            try { if (SoundExists("SeengmThrusters")) _mThrustersHandler.Start(ref _mThrustersEmitter, cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error mThrustersGYRO: " + e.Message); }
            try { if (SoundExists("SeengMainThrusterLoop")) _mainThrusterHandler.Start(cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error MainThrust: " + e.Message); }
            try { if (SoundExists("SeengACDCacc")) _acdcAdvHandler.Start(cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error ACDC_ADV: " + e.Message); }
            try { if (SoundExists("SeengSpeedUP")) _speedUpDown.Start(cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error SpeedUPDOWN: " + e.Message); }
            try { if (SoundExists("SeengManeuverThrusters")) _maneuverThrustersHandler.Restart(cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error ManeuvrThrust: " + e.Message); }
            //1.3
            try { if (SoundExists("cSeengEngineIdle")) _cEngineHandler.Start(cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error cEngine: " + e.Message); }
            try { if (SoundExists("ctSeengEngineIdle")) _ctEngineHandler.Start(cockpit, prefix, transmissionConfig); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error ctEngine: " + e.Message); }
            try { if (SoundExists("ctsSeengEngineIdle")) _ctsEngineHandler.Start(cockpit, prefix, transmissionConfig); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error ctsEngine: " + e.Message); }
            try { if (SoundExists("cSeengTrack33")) _cTracksHandler.Start(cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error Track: " + e.Message); }
            try { if (SoundExists("cSeengWheel33")) _cWheelsHandler.Start(cockpit, prefix); } catch (Exception e) { MyLog.Default.WriteLine("SEENG Error Wheel: " + e.Message); }

        }

        private void UpdateEmitter3D(MyEntity3DSoundEmitter emitter, IMyCockpit cockpit)
        {
            if (emitter == null || cockpit == null) return;
            emitter.Update();
        }

        public void StopAll()
        {    //1.0
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


        }


    }
}
