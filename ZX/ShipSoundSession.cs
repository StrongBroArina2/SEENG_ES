using Sandbox;
using Sandbox.ModAPI;
using VRageMath;

namespace SEENG_ES
{
    public class ShipSoundSession
    {
        public IMyCockpit Cockpit;
        public SoundHandler Handler;
        public ManagersUpdater Managers;
        public string ActivePrefix;
        private int _logicTick = 0;
        public bool NeedsRestart = false;

        public ShipSoundSession(IMyCockpit cockpit, string prefix)
        {
            Cockpit = cockpit;
            ActivePrefix = prefix;
            Handler = new SoundHandler();

            float maxSpeed = SEENG_aConfig.GetCurrentMaxSpeedFromCustomData(cockpit);
            Managers = new ManagersUpdater(new SpeedManager(maxSpeed), new ThrustManager());
        }

        public void Update(SEENG_modManager modManager)
        {
            if (Cockpit == null || Cockpit.Closed) return;

            var listenerPos = MyAPIGateway.Session.Camera?.WorldMatrix.Translation ?? Vector3D.Zero;

            // update time, addon
            if (_logicTick++ % 100 == 0)
            {
                string currentDataPrefix = SEENG_aConfig.GetPackPrefixFromCustomData(Cockpit, modManager.CurrentPackConfig.Prefix);
                if (currentDataPrefix != ActivePrefix)
                {
                    ActivePrefix = currentDataPrefix;
                    Handler.RestartAll(Cockpit, ActivePrefix, Managers.ThrustManager, Managers.SpeedManager, Managers.RotationManager);
                }
            }
            Managers.Update(Cockpit);
            Handler.UpdateAllSounds(Cockpit, ActivePrefix, Managers.ThrustManager, Managers.SpeedManager, Managers.RotationManager);
        }


        public void Dispose()
        {
            Handler.Dispose();
        }
    }
}