using Sandbox.ModAPI;

namespace SEENG_ES
{
    public class SLogic
    {

        private IMyCockpit _previousCockpit;
        private SessionChecker _sessionChecker;
        private ManagersUpdater _managersUpdater;
        private SoundHandler _soundHandler;
        private int _debugCounter = 0;
        private const int DEBUG_INTERVAL = 1060;


        public void Init(SEENG_modManager modManager)
        {
            _sessionChecker = new SessionChecker();
            var speedManager = new SpeedManager(120f);
            var thrustManager = new ThrustManager();
            _managersUpdater = new ManagersUpdater(speedManager, thrustManager);
            _soundHandler = new SoundHandler();
        }

        public void Update(SEENG_modManager modManager)
        {
            string currentPrefix = modManager.CurrentPackConfig.Prefix;

            bool hasSEENGTag;
            bool isOccupied;
            var cockpit = _sessionChecker.CheckAndUpdateCockpit(out hasSEENGTag, out isOccupied);

            if (cockpit == null)
            {
                _soundHandler.StopAll();
                _managersUpdater.Reset();
                SND_Acceleration0Handler.ResetTimers();
                _previousCockpit = null;
                return;
            }
            if (cockpit != _previousCockpit)
            {
                RestartSoundsWithNewPack(modManager, currentPrefix);
                _previousCockpit = cockpit;
            }

            float maxSpeed = SessionChecker.ParseMaxSpeedFromCustomData(cockpit);
            _managersUpdater.SpeedManager.MaxSpeed = maxSpeed;

            _managersUpdater.Update(cockpit);

            if (_debugCounter % DEBUG_INTERVAL == 0)
            {
                float normalizedSpeed = _managersUpdater.SpeedManager.NormalizedSpeed;
                bool isThirdPerson = !MyAPIGateway.Session.CameraController.IsInFirstPersonView;
            }
            _debugCounter++;

            if (hasSEENGTag && isOccupied)
            {
                _soundHandler.UpdateAllSounds(cockpit, currentPrefix, _managersUpdater.ThrustManager, _managersUpdater.SpeedManager);
            }
            else
            {
                _soundHandler.StopAll();
                _managersUpdater.Reset();
                SND_Acceleration0Handler.ResetTimers();
            }
        }

        public void RestartSoundsWithNewPack(SEENG_modManager modManager, string newPrefix)
        {
            var cockpit = _sessionChecker.CurrentCockpit;
            if (cockpit == null || !_sessionChecker.HasSEENGTag(cockpit)) return;

            var config = modManager.CurrentPackConfig;
            SEENG_enginesParametrs.MaxEnginePitchShift = config.MaxEnginePitchShift;
            SEENG_enginesParametrs.MaxEngine50PitchShift = config.MaxEngine50PitchShift;
            SEENG_enginesParametrs.EngineVolumes = config.EngineVolumes;
            SEENG_enginesParametrs.Engine50Volumes = config.Engine50Volumes;

            string name = cockpit.DisplayNameText ?? "Unnamed";
            _soundHandler.StopAll();

            _soundHandler.RestartAll(cockpit, config.Prefix, _managersUpdater.SpeedManager);
        }

        public void Dispose()
        {
            _soundHandler?.Dispose();
            _sessionChecker?.Reset();
            _managersUpdater?.Reset();
            _sessionChecker = null;
            _managersUpdater = null;
            _soundHandler = null;
        }
    }
}