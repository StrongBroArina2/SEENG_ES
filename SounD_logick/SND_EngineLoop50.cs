using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Audio;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace SEENG_ES
{
    public  class SND_EngineLoop50Handler
    {
        public  void Start(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string prefix)
        {
            var myEntity = (MyEntity)(IMyEntity)cockpit;
            emitter = new MyEntity3DSoundEmitter(myEntity, true, 1f);
            emitter.Force3D = true;
            string cueName = string.IsNullOrEmpty(prefix) ? "SeengEngineLoop50" : "SeengEngineLoop50_" + prefix;
            var soundPair = new MySoundPair(cueName);
            emitter.PlaySound(soundPair);
        }

        public void UpdatePitchForLoop50(MyEntity3DSoundEmitter emitter, float normalizedSpeed, float maxPitch50)
        {
            if (emitter?.Sound != null && emitter.Sound.IsPlaying)
            {
                float adjustedSpeed = normalizedSpeed - 0.5f;
                float semitones = maxPitch50 * adjustedSpeed;
                emitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
            }
        }
    }
}