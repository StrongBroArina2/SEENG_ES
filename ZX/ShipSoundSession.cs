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
        public PackConfig Config;

        public ShipSoundSession(IMyCockpit cockpit, PackConfig config)
        {
            Cockpit = cockpit;
            ActivePrefix = config.Prefix;
            Config = config;
            Handler = new SoundHandler();

            float maxSpeed = SEENG_aConfig.GetCurrentMaxSpeedFromCustomData(cockpit);
            Managers = new ManagersUpdater(new SpeedManager(maxSpeed), new ThrustManager());

            string dataPrefix = SEENG_aConfig.GetPackPrefixFromCustomData(cockpit, null);
            if (string.IsNullOrEmpty(dataPrefix))
            {
                dataPrefix = "ImprovedVanilla";
            }
        }

        public void Update(SEENG_modManager modManager)
        {
            if (Cockpit == null || Cockpit.Closed) return;

            if (_logicTick++ % 100 == 0)
            {
                string currentDataPrefix = SEENG_aConfig.GetPackPrefixFromCustomData(Cockpit, null);
                if (string.IsNullOrEmpty(currentDataPrefix))
                {
                    currentDataPrefix = "ImprovedVanilla";
                }

                if (currentDataPrefix != ActivePrefix)
                {
                    ActivePrefix = currentDataPrefix;
                    Handler.RestartAll(Cockpit, ActivePrefix, Managers.ThrustManager, Managers.SpeedManager, Managers.RotationManager);
                }
            }

            PackConfig shipConfig = modManager.AvailablePacks.ContainsKey(ActivePrefix)
                            ? modManager.AvailablePacks[ActivePrefix]
                            : modManager.CurrentPackConfig;
            Managers.Update(Cockpit);
            Handler.UpdateAllSounds(Cockpit, ActivePrefix, Managers.ThrustManager, Managers.SpeedManager, Managers.RotationManager, shipConfig);
        }


        public void Dispose()
        {
            Handler.Dispose();
        }
    }
}