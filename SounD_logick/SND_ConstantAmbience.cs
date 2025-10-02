using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;

namespace SEENG_ES
{
    public static class SND_ConstantAmbienceHandler
    {
        public static void StartConstantAmbienceSound(ref MyEntity3DSoundEmitter emitter, IMyCockpit cockpit, string name, string prefix)
        {
            var myEntity = (MyEntity)(IMyEntity)cockpit;
            emitter = new MyEntity3DSoundEmitter(myEntity, true, 1f);
            emitter.Force3D = true;
            string cueName = string.IsNullOrEmpty(prefix) ? "SeengAmbienceConstant" : "SeengAmbienceConstant_" + prefix;
            var soundPair = new MySoundPair(cueName);
            emitter.PlaySound(soundPair);
            emitter.Sound.VolumeMultiplier = 1f;
        }
    }
}