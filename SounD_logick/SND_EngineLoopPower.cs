using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Audio;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace SEENG_ES
{
    public class SND_EngineLoopPowerHandler
    {
        public void Start(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string prefix)
        {
            var myEntity = (MyEntity)(IMyEntity)cockpit;
            emitter = new MyEntity3DSoundEmitter(myEntity, true, 1f);
            emitter.Force3D = true;

            string cueName = string.IsNullOrEmpty(prefix) ? "SeengEngineLoopPower" : "SeengEngineLoopPower_" + prefix;
            var soundPair = new MySoundPair(cueName);
            emitter.PlaySound(soundPair);
        }

        public void Update(MyEntity3DSoundEmitter emitter, float powerPercent, float maxPitchShift = 12f)
        {
            if (emitter?.Sound == null || !emitter.Sound.IsPlaying) return;
            float normalizedPower = powerPercent / 100f;
            float semitones = maxPitchShift * normalizedPower;
            emitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);

        }
    }
}