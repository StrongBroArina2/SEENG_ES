using System;
using System.Buffers.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;
using static SEENG_ES.SpeedManager;

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
        private MyEntity3DSoundEmitter _mThrustersEmitter;


        private void UpdateEmitter3D(MyEntity3DSoundEmitter emitter, IMyCockpit cockpit)
        {
            if (emitter == null || cockpit == null) return;
            emitter.Update();
        }
        public void UpdateAllSounds(IMyCockpit cockpit, string prefix, ThrustManager thrustManager, SpeedManager speedManager, RotationManager rotationManager)
        {
            if (cockpit == null) return;
            string name = cockpit.DisplayNameText ?? "Unnamed";
            float normalizedSpeed = speedManager.NormalizedSpeed;

            // ──────────────────────────────────────────────────────────────
            // IM gonna kill myself today ╨ ∙
            // ──────────────────────────────────────────────────────────────
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
                EnsureEmitterStarted(ref _mThrustersEmitter, () => SND_mThrustersHandler.Start(ref _mThrustersEmitter, cockpit, prefix), "mThrusters");

                SND_mThrustersHandler.Update(_mThrustersEmitter, rotationManager, speedManager);
                SND_ManeuverThrustersHandler.Update(cockpit, rotationManager, speedManager);
                SND_MainThrusterHandler.Update(cockpit, thrustManager);
                SND_C_EngineHandler.Update(thrustManager, speedManager);
                SND_CT_EngineHandler.Update(thrustManager, speedManager);
                SND_C_TracksHandler.Update(speedManager);

                bool shouldAccelStart = (normalizedSpeed > 0f && speedManager.Acceleration > 0.1f);
                if (shouldAccelStart && (_acceleration0Emitter == null || !_acceleration0Emitter.Sound.IsPlaying))
                {
                    SND_Acceleration0Handler.StartAcceleration0Sound(ref _acceleration0Emitter, cockpit, name, speedManager, prefix);
                }
                SEENG_enginesParametrs.UpdateAcceleration0Sound(ref _acceleration0Emitter, cockpit, name, speedManager, prefix);

                if (thrustManager.IsThrusting && (_pushEmitter == null || !_pushEmitter.Sound?.IsPlaying == true))
                {
                    SEENG_enginesParametrs.StartPushSound(ref _pushEmitter, cockpit, name, thrustManager, prefix);
                }
                if (_pushEmitter != null)
                    SEENG_enginesParametrs.UpdatePushVolume(_pushEmitter, thrustManager);

                ForceUpdatePitch(_engineLoopEmitter, normalizedSpeed);
                ForceUpdatePitch(_engineLoop50Emitter, normalizedSpeed);

                UpdateEmitter3D(_engineLoopEmitter, cockpit);
                UpdateEmitter3D(_engineLoop50Emitter, cockpit);
                UpdateEmitter3D(_acdcEmitter, cockpit);
                UpdateEmitter3D(_pushEmitter, cockpit);
                UpdateEmitter3D(_acceleration0Emitter, cockpit);
                UpdateEmitter3D(_moveAmbienceEmitter, cockpit);
                UpdateEmitter3D(_stationaryAmbienceEmitter, cockpit);
                UpdateEmitter3D(_constantAmbienceEmitter, cockpit);

                if (_acdcEmitter != null) SEENG_enginesParametrs.UpdateAcdcVolume(_acdcEmitter, speedManager);
                if (_moveAmbienceEmitter != null) SEENG_enginesParametrs.UpdateMoveAmbienceVolume(_moveAmbienceEmitter, normalizedSpeed);
                if (_stationaryAmbienceEmitter != null) SEENG_enginesParametrs.UpdateStationaryAmbienceVolume(_stationaryAmbienceEmitter, normalizedSpeed);

                ApplySeengGlobalVolume(); //volume
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine($"SEENG_ES UpdateAllSounds error: {e}");
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
            StopEmitter(ref _mThrustersEmitter);
            SND_ManeuverThrustersHandler.StopAll();
            SND_MainThrusterHandler.StopAll();
            SND_C_EngineHandler.Stop();
            SND_CT_EngineHandler.Stop();
            SND_C_TracksHandler.Stop();

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
                UpdateEmitter3D(_engineLoopEmitter, cockpit);
                UpdateEmitter3D(_engineLoop50Emitter, cockpit);
                ForceUpdatePitch(_engineLoopEmitter, normalizedSpeed);
                ForceUpdatePitch(_engineLoop50Emitter, normalizedSpeed);
                if (_acdcEmitter != null) SEENG_enginesParametrs.UpdateAcdcVolume(_acdcEmitter, speedManager);
                if (_moveAmbienceEmitter != null) SEENG_enginesParametrs.UpdateMoveAmbienceVolume(_moveAmbienceEmitter, normalizedSpeed);
                if (_stationaryAmbienceEmitter != null) SEENG_enginesParametrs.UpdateStationaryAmbienceVolume(_stationaryAmbienceEmitter, normalizedSpeed);
                SND_ManeuverThrustersHandler.Restart(cockpit, prefix);
                SND_MainThrusterHandler.Restart(cockpit, prefix);
                SND_C_EngineHandler.Start(cockpit, prefix);
                SND_CT_EngineHandler.Start(cockpit, prefix);
                // SND_mThrustersHandler.Update(_mThrustersEmitter, rotationManager);
                SND_C_TracksHandler.Start(cockpit, prefix);

                ApplySeengGlobalVolume();
            }
            catch (Exception e)
            {
            }
        }

        private void ApplySeengGlobalVolume()
        {
            float mult = SEENG_VolumeManager.GlobalMultiplier;

            if (mult <= 0.0001f || Math.Abs(mult - 1f) < 0.0001f) return;

            void Apply(MyEntity3DSoundEmitter emitter)
            {
                if (emitter?.Sound != null && emitter.Sound.IsPlaying)
                {
                    emitter.Sound.VolumeMultiplier *= mult;

                    emitter.Sound.VolumeMultiplier = MathHelper.Clamp(emitter.Sound.VolumeMultiplier, 0f, 10f);
                }
            }

            Apply(_engineLoopEmitter);
            Apply(_acdcEmitter);
            Apply(_pushEmitter);
            Apply(_acceleration0Emitter);
            Apply(_engineLoop50Emitter);
            Apply(_moveAmbienceEmitter);
            Apply(_stationaryAmbienceEmitter);
            Apply(_constantAmbienceEmitter);

            Apply(SND_C_EngineHandler._idle);
            Apply(SND_C_EngineHandler._base33);
            Apply(SND_C_EngineHandler._base66);
            Apply(SND_C_EngineHandler._base99);
            Apply(SND_C_EngineHandler._load33);
            Apply(SND_C_EngineHandler._load66);
            Apply(SND_C_EngineHandler._load99);

            Apply(SND_C_EngineHandler._idle);
            Apply(SND_C_EngineHandler._base33);
            Apply(SND_C_EngineHandler._base66);
            Apply(SND_C_EngineHandler._base99);
            Apply(SND_C_EngineHandler._load33);
            Apply(SND_C_EngineHandler._load66);
            Apply(SND_C_EngineHandler._load99);

            Apply(SND_CT_EngineHandler._idleT);
            Apply(SND_CT_EngineHandler._base33T);
            Apply(SND_CT_EngineHandler._base66T);
            Apply(SND_CT_EngineHandler._base99T);
            Apply(SND_CT_EngineHandler._load33T);
            Apply(SND_CT_EngineHandler._load66T);
            Apply(SND_CT_EngineHandler._load99T);

            Apply(SND_C_TracksHandler._track33);
            Apply(SND_C_TracksHandler._track66);
            Apply(SND_C_TracksHandler._track99);

            Apply(SND_MainThrusterHandler._loopEmitter);
            Apply(SND_MainThrusterHandler._startEmitter);
            Apply(SND_MainThrusterHandler._endEmitter);
            Apply(SND_mThrustersHandler._emitter);
            foreach (var thruster in SND_ManeuverThrustersHandler._thrusters)
            {
                Apply(thruster.Emitter);
            }
        }

        public void Dispose()
        {
            StopAll();
        }
    }
}