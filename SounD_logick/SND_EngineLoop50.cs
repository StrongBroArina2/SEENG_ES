using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Audio;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace SEENG_ES
{
    public static class SND_EngineLoop50Handler
    {
        public static void StartEngineLoop50Sound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            var myEntity = (MyEntity)(IMyEntity)cockpit;
            emitter = new MyEntity3DSoundEmitter(myEntity, true, 1f);
            emitter.Force3D = true;
            string cueName = string.IsNullOrEmpty(prefix) ? "SeengEngineLoop50" : "SeengEngineLoop50_" + prefix;
            var soundPair = new MySoundPair(cueName);
            emitter.PlaySound(soundPair);
        }

        public static void UpdatePitchForLoop50(MyEntity3DSoundEmitter emitter, float normalizedSpeed)
        {
            if (emitter?.Sound != null && emitter.Sound.IsPlaying)
            {
                float adjustedSpeed = normalizedSpeed - 0.5f;
                float semitones = 15f * adjustedSpeed;
                emitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
            }
        }
    }
}