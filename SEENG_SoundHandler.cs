using System;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace SEENG_ES
{
    public class SoundHandler
    {
        private MyEntity3DSoundEmitter _engineLoopEmitter;
        private MyEntity3DSoundEmitter _acdcEmitter;
        private MyEntity3DSoundEmitter _pushEmitter;
        private MyEntity3DSoundEmitter _acceleration0Emitter;
        private MyEntity3DSoundEmitter _engineLoop50Emitter;
        private MyEntity3DSoundEmitter _moveAmbienceEmitter;
        private MyEntity3DSoundEmitter _stationaryAmbienceEmitter;
        private MyEntity3DSoundEmitter _constantAmbienceEmitter;

        public void UpdateAllSounds(IMyCockpit cockpit, string prefix, ThrustManager thrustManager, SpeedManager speedManager)
        {
            if (cockpit == null) return;

            string name = cockpit.DisplayNameText ?? "Unnamed";
            float normalizedSpeed = speedManager.NormalizedSpeed;
            SEENG_enginesParametrs.UpdatePitchForEmitter(_engineLoopEmitter, normalizedSpeed);
            SEENG_enginesParametrs.UpdatePitchForLoop50(_engineLoop50Emitter, normalizedSpeed);
            SEENG_enginesParametrs.UpdatePitchForEmitter(_acdcEmitter, normalizedSpeed);
            SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoopEmitter, normalizedSpeed, SEENG_enginesParametrs.EngineVolumes);
            SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoop50Emitter, normalizedSpeed, SEENG_enginesParametrs.Engine50Volumes);

            try
            {
                EnsureEmitterStarted(ref _engineLoopEmitter, () => SEENG_enginesParametrs.StartEngineLoopSound(ref _engineLoopEmitter, cockpit, name, prefix), "EngineLoop");
                EnsureEmitterStarted(ref _acdcEmitter, () => SEENG_enginesParametrs.StartAcdcSound(ref _acdcEmitter, cockpit, name, prefix), "ACDC");
                EnsureEmitterStarted(ref _engineLoop50Emitter, () => SEENG_enginesParametrs.StartEngineLoop50Sound(ref _engineLoop50Emitter, cockpit, name, prefix), "EngineLoop50");
                EnsureEmitterStarted(ref _moveAmbienceEmitter, () => SEENG_enginesParametrs.StartMoveAmbienceSound(ref _moveAmbienceEmitter, cockpit, name, prefix), "MoveAmbience");
                EnsureEmitterStarted(ref _stationaryAmbienceEmitter, () => SEENG_enginesParametrs.StartStationaryAmbienceSound(ref _stationaryAmbienceEmitter, cockpit, name, prefix), "StationaryAmbience");
                EnsureEmitterStarted(ref _constantAmbienceEmitter, () => SEENG_enginesParametrs.StartConstantAmbienceSound(ref _constantAmbienceEmitter, cockpit, name, prefix), "ConstantAmbience");
                bool shouldAccelStart = (normalizedSpeed > 0f && speedManager.Acceleration > 0.1f);
                if (shouldAccelStart && (_acceleration0Emitter == null || !_acceleration0Emitter.Sound.IsPlaying))
                {
                    SND_Acceleration0Handler.StartAcceleration0Sound(ref _acceleration0Emitter, cockpit, name, speedManager, prefix);
                }
                SEENG_enginesParametrs.UpdateAcceleration0Sound(ref _acceleration0Emitter, cockpit, name, speedManager, prefix);
                ForceUpdatePitch(_engineLoopEmitter, normalizedSpeed);
                ForceUpdatePitch(_engineLoop50Emitter, normalizedSpeed);
                if (_acdcEmitter != null) SEENG_enginesParametrs.UpdateAcdcVolume(_acdcEmitter, speedManager);
                if (_moveAmbienceEmitter != null) SEENG_enginesParametrs.UpdateMoveAmbienceVolume(_moveAmbienceEmitter, normalizedSpeed);
                if (_stationaryAmbienceEmitter != null) SEENG_enginesParametrs.UpdateStationaryAmbienceVolume(_stationaryAmbienceEmitter, normalizedSpeed);
            }
            catch (Exception e)
            {
            }
        }



        private void EnsureEmitterStarted(ref MyEntity3DSoundEmitter emitter, System.Action startAction, string soundType)
        {
            bool needsStart = (emitter == null || emitter.Sound == null || !emitter.Sound.IsPlaying);
            if (needsStart)
            {
                try
                {
                    startAction();
                    if (emitter?.Sound?.IsPlaying == true)
                    {
                    }
                    else
                    {
                        if (emitter != null)
                        {
                            emitter.StopSound(true);
                            emitter = null;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (emitter != null)
                    {
                        emitter.StopSound(true);
                        emitter = null;
                    }
                }
            }
        }

        private void ForceUpdatePitch(MyEntity3DSoundEmitter emitter, float normalizedSpeed)
        {
            if (emitter == null || emitter.Sound == null) return;
            SEENG_enginesParametrs.UpdatePitchForEmitter(_engineLoopEmitter, normalizedSpeed);
            SEENG_enginesParametrs.UpdatePitchForLoop50(_engineLoop50Emitter, normalizedSpeed);
            SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoopEmitter, normalizedSpeed, SEENG_enginesParametrs.EngineVolumes);
            SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoop50Emitter, normalizedSpeed, SEENG_enginesParametrs.Engine50Volumes);
            if (emitter == _engineLoop50Emitter)
            {
                SEENG_enginesParametrs.UpdatePitchForLoop50(emitter, normalizedSpeed);
            }
        }

        public void StopAll()
        {
            StopEmitter(ref _engineLoopEmitter);
            StopEmitter(ref _acdcEmitter);
            StopEmitter(ref _pushEmitter);
            StopEmitter(ref _acceleration0Emitter);
            StopEmitter(ref _engineLoop50Emitter);
            StopEmitter(ref _moveAmbienceEmitter);
            StopEmitter(ref _stationaryAmbienceEmitter);
            StopEmitter(ref _constantAmbienceEmitter);

        }

        private void StopEmitter(ref MyEntity3DSoundEmitter emitter)
        {
            if (emitter != null)
            {
                emitter.StopSound(true);
                emitter.StopSound(true);
                emitter = null;
            }
        }

        public void RestartAll(IMyCockpit cockpit, string prefix, SpeedManager speedManager)
        {
            if (cockpit == null) return;
            StopAll();
            string name = cockpit.DisplayNameText ?? "Unnamed";


            try
            {
                try { SEENG_enginesParametrs.StartEngineLoopSound(ref _engineLoopEmitter, cockpit, name, prefix); } catch (Exception e) { _engineLoopEmitter = null; }
                try { SEENG_enginesParametrs.StartAcdcSound(ref _acdcEmitter, cockpit, name, prefix); } catch (Exception e) { _acdcEmitter = null; }
                try { SEENG_enginesParametrs.StartEngineLoop50Sound(ref _engineLoop50Emitter, cockpit, name, prefix); } catch (Exception e) { _engineLoop50Emitter = null; }
                try { SEENG_enginesParametrs.StartMoveAmbienceSound(ref _moveAmbienceEmitter, cockpit, name, prefix); } catch (Exception e) { _moveAmbienceEmitter = null; }
                try { SEENG_enginesParametrs.StartStationaryAmbienceSound(ref _stationaryAmbienceEmitter, cockpit, name, prefix); } catch (Exception e) { _stationaryAmbienceEmitter = null; }
                try { SEENG_enginesParametrs.StartConstantAmbienceSound(ref _constantAmbienceEmitter, cockpit, name, prefix); } catch (Exception e) { _constantAmbienceEmitter = null; }
                float normalizedSpeed = speedManager.NormalizedSpeed;
                SEENG_enginesParametrs.UpdatePitchForEmitter(_engineLoopEmitter, normalizedSpeed);
                SEENG_enginesParametrs.UpdatePitchForLoop50(_engineLoop50Emitter, normalizedSpeed);
                SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoopEmitter, normalizedSpeed, SEENG_enginesParametrs.EngineVolumes);
                SEENG_enginesParametrs.UpdateVolumeForEmitter(_engineLoop50Emitter, normalizedSpeed, SEENG_enginesParametrs.Engine50Volumes);
                ForceUpdatePitch(_engineLoopEmitter, normalizedSpeed);
                ForceUpdatePitch(_engineLoop50Emitter, normalizedSpeed);
                if (_acdcEmitter != null) SEENG_enginesParametrs.UpdateAcdcVolume(_acdcEmitter, speedManager);
                if (_moveAmbienceEmitter != null) SEENG_enginesParametrs.UpdateMoveAmbienceVolume(_moveAmbienceEmitter, normalizedSpeed);
                if (_stationaryAmbienceEmitter != null) SEENG_enginesParametrs.UpdateStationaryAmbienceVolume(_stationaryAmbienceEmitter, normalizedSpeed);
                UpdateAllSounds(cockpit, prefix, new ThrustManager(), speedManager);
            }
            catch (Exception e)
            {
            }
        }

        public void Dispose()
        {
            StopAll();
        }
    }
}