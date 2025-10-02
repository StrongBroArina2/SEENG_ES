using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Audio;
using VRageMath;
using System.Collections.Generic;

namespace SEENG_ES
{
    public struct VolumePoint
    {
        public float Speed { get; set; }
        public float Volume { get; set; }

        public VolumePoint(float speed = 0f, float volume = 0f)
        {
            Speed = speed;
            Volume = volume;
        }
    }

    public struct PackConfig
    {
        public string Prefix { get; set; }
        public float MaxEnginePitchShift { get; set; }
        public float MaxEngine50PitchShift { get; set; }
        public List<VolumePoint> EngineVolumes { get; set; }
        public List<VolumePoint> Engine50Volumes { get; set; }

        public PackConfig(string prefix = "", float maxEnginePitchShift = 15f, float maxEngine50PitchShift = 15f, List<VolumePoint> engineVolumes = null, List<VolumePoint> engine50Volumes = null)
        {
            Prefix = prefix;
            MaxEnginePitchShift = maxEnginePitchShift;
            MaxEngine50PitchShift = maxEngine50PitchShift;
            EngineVolumes = engineVolumes ?? new List<VolumePoint>();
            Engine50Volumes = engine50Volumes ?? new List<VolumePoint>();
        }
    }
    public static class SEENG_enginesParametrs
    {
        public static float MaxEnginePitchShift { get; set; } = 15f;
        public static float MaxEngine50PitchShift { get; set; } = 15f;
        public static List<VolumePoint> EngineVolumes { get; set; } = new List<VolumePoint>();
        public static List<VolumePoint> Engine50Volumes { get; set; } = new List<VolumePoint>();

        public static void UpdateVolumeForEmitter(MyEntity3DSoundEmitter emitter, float normalizedSpeed, List<VolumePoint> volumes)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying) return;

            if (volumes.Count == 0)
            {
                emitter.Sound.VolumeMultiplier = 1f;
                return;
            }

            float speedPercent = normalizedSpeed * 100f;
            VolumePoint low = volumes[0];
            VolumePoint high = volumes[volumes.Count - 1];

            if (speedPercent <= low.Speed)
            {
                emitter.Sound.VolumeMultiplier = low.Volume;
                return;
            }
            if (speedPercent >= high.Speed)
            {
                emitter.Sound.VolumeMultiplier = high.Volume;
                return;
            }

            for (int i = 0; i < volumes.Count - 1; i++)
            {
                if (speedPercent >= volumes[i].Speed && speedPercent <= volumes[i + 1].Speed)
                {
                    float t = (speedPercent - volumes[i].Speed) / (volumes[i + 1].Speed - volumes[i].Speed);
                    float volume = MathHelper.Lerp(volumes[i].Volume, volumes[i + 1].Volume, t);
                    emitter.Sound.VolumeMultiplier = volume;
                    return;
                }
            }

            emitter.Sound.VolumeMultiplier = 1f;
        }
        public static void StartEngineLoopSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            SND_EngineLoopHandler.StartEngineLoopSound(ref emitter, cockpit, name, prefix);
        }
        public static void UpdateAcceleration0Sound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, SpeedManager speedManager, string prefix)
        {
            SND_Acceleration0Handler.UpdateAcceleration0Sound(ref emitter, cockpit, name, speedManager, prefix);
        }
        public static void UpdatePitchForEmitter(MyEntity3DSoundEmitter emitter, float normalizedSpeed)
        {
            if (emitter?.Sound != null && emitter.Sound.IsPlaying)
            {
                float semitones = MaxEnginePitchShift * normalizedSpeed;
                emitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
            }
        }

        public static void UpdatePitchForLoop50(MyEntity3DSoundEmitter emitter, float normalizedSpeed)
        {
            if (emitter?.Sound != null && emitter.Sound.IsPlaying)
            {
                float adjustedSpeed = normalizedSpeed - 0.5f;
                float semitones = MaxEngine50PitchShift * adjustedSpeed;
                emitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
            }
        }

        public static void StartEngineLoop50Sound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            SND_EngineLoop50Handler.StartEngineLoop50Sound(ref emitter, cockpit, name, prefix);
        }
        public static void StartMoveAmbienceSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            SND_MoveAmbienceHandler.StartMoveAmbienceSound(ref emitter, cockpit, name, prefix);
        }

        public static void UpdateMoveAmbienceVolume(MyEntity3DSoundEmitter emitter, float normalizedSpeed)
        {
            SND_MoveAmbienceHandler.UpdateMoveAmbienceVolume(emitter, normalizedSpeed);
        }
        public static void StartStationaryAmbienceSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            SND_StationaryAmbienceHandler.StartStationaryAmbienceSound(ref emitter, cockpit, name, prefix);
        }

        public static void UpdateStationaryAmbienceVolume(MyEntity3DSoundEmitter emitter, float normalizedSpeed)
        {
            SND_StationaryAmbienceHandler.UpdateStationaryAmbienceVolume(emitter, normalizedSpeed);
        }
        public static void StartConstantAmbienceSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            SND_ConstantAmbienceHandler.StartConstantAmbienceSound(ref emitter, cockpit, name, prefix);
        }

        public static void StartAcdcSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            SND_acdcHandler.StartAcdcSound(ref emitter, cockpit, name, prefix);
        }

        public static void UpdateAcdcVolume(MyEntity3DSoundEmitter emitter, SpeedManager speedManager)
        {
            SND_acdcHandler.UpdateAcdcVolume(emitter, speedManager);
        }
        public static void StopEmitter(ref MyEntity3DSoundEmitter emitter)
        {
            if (emitter != null)
            {
                emitter.StopSound(true);
                emitter = null;
            }
        }
    }
}