using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace SEENG_ES
{
    public static class SND_StationaryAmbienceHandler
    {
        public static void StartStationaryAmbienceSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            var myEntity = (MyEntity)(IMyEntity)cockpit;
            emitter = new MyEntity3DSoundEmitter(myEntity, true, 1f);
            emitter.Force3D = true;
            string cueName = string.IsNullOrEmpty(prefix) ? "SeengStationaryAmbience" : "SeengStationaryAmbience_" + prefix;
            var soundPair = new MySoundPair(cueName);
            emitter.PlaySound(soundPair);
            emitter.Sound.VolumeMultiplier = 1f;
        }

        public static void UpdateStationaryAmbienceVolume(MyEntity3DSoundEmitter emitter, float normalizedSpeed)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying) return;
            float volume = MathHelper.Clamp(1f - (normalizedSpeed / 0.1f), 0f, 1f);
            emitter.Sound.VolumeMultiplier = volume;
        }
    }
}