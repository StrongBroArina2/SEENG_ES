using Sandbox.Game.EntityComponents;
using VRage.Game.Components;

namespace SEENG_ES
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DIGI_DisableShipSounds : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            MyShipSoundComponent.ClearShipSounds();
        }
    }
}