using Sandbox.ModAPI;
using VRage.Data.Audio;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using System.Diagnostics;
using VRageMath;
using Sandbox.Game.Entities;
using System;
using VRage.Game.Entity;

namespace SEENG_ES
{
    public static class SND_Acceleration0Handler
    {
        private static Stopwatch _delayTimer = new Stopwatch();
        private static float _prevNormalizedSpeed = 0f;

        public static void UpdateAcceleration0Sound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, SpeedManager speedManager, string prefix)
        {
            float currentNormalized = speedManager.NormalizedSpeed;
            float currentAcc = speedManager.Acceleration;
            bool isAccelerating = currentAcc > 0.1f && currentNormalized > 0f;
            bool shouldStart = (_prevNormalizedSpeed <= 0f && currentNormalized >= 0.01f) || isAccelerating;

            if (emitter == null)
            {
                if (shouldStart)
                {
                    _delayTimer.Restart();
                    _prevNormalizedSpeed = currentNormalized;
                    return;
                }
            }
            else
            {
                if (!isAccelerating)
                {
                    float deltaVolume = -1f / 150f;
                    float newVolume = Math.Max(0f, emitter.Sound.VolumeMultiplier + deltaVolume);
                    emitter.Sound.VolumeMultiplier = newVolume;

                    if (newVolume <= 0f)
                    {
                        emitter.StopSound(true);
                        emitter = null;
                        _delayTimer.Reset();
                    }
                    return;
                }
                else
                {
                    emitter.Sound.VolumeMultiplier = 1f;
                }
            }
            if (_delayTimer.IsRunning && _delayTimer.Elapsed.TotalSeconds >= 0.3f)
            {
                if (currentNormalized > 0f && isAccelerating)
                {
                    StartAcceleration0Sound(ref emitter, cockpit, name, speedManager, prefix);
                }
                else
                {
                    _delayTimer.Reset();
                }
                _prevNormalizedSpeed = currentNormalized;
            }
        }

        public static void StartAcceleration0Sound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, SpeedManager speedManager, string prefix)
        {
            _delayTimer.Reset();
            _prevNormalizedSpeed = speedManager.NormalizedSpeed;
        }
        public static void ResetTimers()
        {
            _delayTimer.Reset();
            _prevNormalizedSpeed = 0f;
        }
    }
}