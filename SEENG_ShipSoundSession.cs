using Sandbox;
using Sandbox.ModAPI;
using SEENG_ES;
using SEENG_SElauncher.SEENG_CFG_SYS;
using SEENG_SElauncher.SEENG_Managers;
using VRage.Utils;
using VRageMath;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace SEENG_SElauncher
{
    public class ShipSoundSession
    {
        public IMyCockpit Cockpit;
        public SoundHandler Handler;
        public ManagersUpdater Managers;
        public SEENG_BlockStateManager BlockState;
        public string ActivePrefix;
        private int _logicTick = 0;
        public bool NeedsRestart = false;
        public PackConfig Config;
        public SEENG_TransmissionConfig TransmissionConfig;
        private SEENG_TransmissionConfig _transmission = SEENG_TransmissionConfig.Default;
        public VehicleClass ClassType { get; private set; } = VehicleClass.Unknown;
        public bool IsDefaultSession { get; private set; } = false;
        public List<IMyThrust> GridThrusters { get; private set; } = new List<IMyThrust>();
        public List<IMyMotorSuspension> GridSuspensions { get; private set; } = new List<IMyMotorSuspension>();

        public ShipSoundSession(IMyCockpit cockpit, PackConfig packConfig, VehicleClass vehicleClass = VehicleClass.Unknown, bool isDefault = false)
        {
            Cockpit = cockpit;
            Config = packConfig;
            ActivePrefix = packConfig.Prefix;
            ClassType = vehicleClass;
            IsDefaultSession = isDefault;
            TransmissionConfig = SEENG_aConfig.GetTransmissionConfig(cockpit, packConfig.Transmission);
            Handler = new SoundHandler();
            BlockState = new SEENG_BlockStateManager();
            float maxSpeed = isDefault
                ? SEENG_CFG_SYS.VehicleClassifier.GetDefaultMaxSpeed(vehicleClass)
                : SEENG_aConfig.GetCurrentMaxSpeedFromCustomData(cockpit);
            Managers = new ManagersUpdater(new SpeedManager(maxSpeed), new ThrustManager());

            var ctHandler = Handler.GetCTEngineHandler();
            if (ctHandler != null)
            {
            }

            var ctsHandler = Handler.GetCTSEngineHandler();
            if (ctsHandler != null)
            {
            }
        }

        public void SetGridBlocks(List<IMyThrust> thrusters, List<IMyMotorSuspension> suspensions)
        {
            GridThrusters = thrusters ?? new List<IMyThrust>();
            GridSuspensions = suspensions ?? new List<IMyMotorSuspension>();
        }

        public void Update(SEENG_modManager modManager)
        {
            if (Cockpit == null || Cockpit.Closed) return;

            if (_logicTick++ % 200 == 0)
            {
                CheckAndUpdateTransmissionConfig();

                if (IsDefaultSession)
                {
                    if ((Cockpit.DisplayNameText ?? "").Contains("[SEENG]"))
                    {
                        IsDefaultSession = false;
                        string newPrefix = SEENG_aConfig.GetPackPrefixFromCustomData(Cockpit, ActivePrefix);
                        if (!string.IsNullOrEmpty(newPrefix) && newPrefix != ActivePrefix)
                        {
                            ActivePrefix = newPrefix;
                            Handler.RestartAll(Cockpit, ActivePrefix, Managers, TransmissionConfig);
                        }
                        return;
                    }
                }
                else
                {
                    string currentDataPrefix = SEENG_aConfig.GetPackPrefixFromCustomData(Cockpit, null);
                    if (string.IsNullOrEmpty(currentDataPrefix))
                    {
                        currentDataPrefix = "ImprovedVanilla";
                    }

                    if (currentDataPrefix != ActivePrefix)
                    {
                        ActivePrefix = currentDataPrefix;
                        Handler.RestartAll(Cockpit, ActivePrefix, Managers, TransmissionConfig);
                    }
                }
            }

            PackConfig shipConfig = modManager.AvailablePacks.ContainsKey(ActivePrefix)
                            ? modManager.AvailablePacks[ActivePrefix]
                            : modManager.CurrentPackConfig;

            BlockState.Update(Cockpit);
            Managers.Update(Cockpit, IsDefaultSession, ClassType);
            Handler.UpdateAllSounds(Cockpit, ActivePrefix, Managers, shipConfig);
        }

        private void CheckAndUpdateTransmissionConfig()
        {
            if (Cockpit == null) return;

            var newTransmission = SEENG_aConfig.GetTransmissionConfig(Cockpit, Config.Transmission);

            if (_transmission.SkidSteering != newTransmission.SkidSteering ||
                _transmission.GearRatios.Count != newTransmission.GearRatios.Count ||
                _transmission.UpshiftRPM.Count != newTransmission.UpshiftRPM.Count ||
                _transmission.DownshiftRPM.Count != newTransmission.DownshiftRPM.Count ||
                _transmission.UpshiftSpeedThresholds.Count != newTransmission.UpshiftSpeedThresholds.Count ||
                _transmission.DownshiftSpeedThresholds.Count != newTransmission.DownshiftSpeedThresholds.Count ||
                _transmission.GearRatiosS.Count != newTransmission.GearRatiosS.Count)
            {
                TransmissionConfig = newTransmission;
                _transmission = newTransmission;      
                var ctHandler = Handler.GetCTEngineHandler();
                if (ctHandler != null)
                {
                    ctHandler.Stop();
                    ctHandler.Start(Cockpit, ActivePrefix, TransmissionConfig);
                }

                var ctsHandler = Handler.GetCTSEngineHandler();
                if (ctsHandler != null)
                {
                    ctsHandler.Stop();
                    ctsHandler.Start(Cockpit, ActivePrefix, TransmissionConfig);
                }
            }
        }
        public void Dispose()
        {
            Handler.Dispose();
            BlockState?.Reset();
        }
    }
}
