using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace SEENG_ES
{
    public class SLogic
    {
        private readonly SessionChecker _sessionChecker = new SessionChecker();
        private readonly Dictionary<long, ShipSoundSession> _activeSessions = new Dictionary<long, ShipSoundSession>();
        private readonly HashSet<IMyEntity> _entitiesBuffer = new HashSet<IMyEntity>();
        private int _scanCounter = 0;

        public void Init(SEENG_modManager modManager)
        {
        }

        public void Update(SEENG_modManager modManager)
        {
            if (MyAPIGateway.Session == null) return;

            var listenerPos = MyAPIGateway.Session.Camera?.WorldMatrix.Translation ?? Vector3D.Zero;

            if (_scanCounter++ % 60 == 0) //scan time
            {
                ScanNearbyShips(modManager);
            }

            List<long> toRemove = new List<long>();
            foreach (var kvp in _activeSessions)
            {
                var session = kvp.Value;

                if (session.Cockpit == null ||
                    session.Cockpit.Closed ||
                    !_sessionChecker.HasSEENGTag(session.Cockpit) ||
                    Vector3D.DistanceSquared(session.Cockpit.GetPosition(), listenerPos) > 950 * 950) //sound sync dist!!!!!!!
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                session.Update(modManager);
            }

            foreach (var id in toRemove)
            {
                _activeSessions[id].Dispose();
                _activeSessions.Remove(id);
            }
        }

        private void ScanNearbyShips(SEENG_modManager modManager)
        {
            _entitiesBuffer.Clear();
            MyAPIGateway.Entities.GetEntities(_entitiesBuffer);
            var listenerPos = MyAPIGateway.Session.Camera?.WorldMatrix.Translation ?? Vector3D.Zero;

            foreach (var entity in _entitiesBuffer)
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null || grid.Physics == null) continue;

                if (Vector3D.DistanceSquared(grid.GetPosition(), listenerPos) > 600 * 600) //sound sync dist !!!!!!
                    continue;

                var slimBlocks = new List<IMySlimBlock>();
                grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyCockpit);

                foreach (var slim in slimBlocks)
                {
                    var cockpit = slim.FatBlock as IMyCockpit;
                    if (cockpit == null) continue;

                    if (_sessionChecker.HasSEENGTag(cockpit))
                    {
                        if (!_activeSessions.ContainsKey(cockpit.EntityId))
                        {
                            string prefix = SEENG_aConfig.GetPackPrefixFromCustomData(cockpit, modManager.CurrentPackConfig.Prefix);
                            var session = new ShipSoundSession(cockpit, prefix);
                            session.Handler.RestartAll(cockpit, prefix, session.Managers.ThrustManager, session.Managers.SpeedManager, session.Managers.RotationManager);
                            _activeSessions.Add(cockpit.EntityId, session);
                        }
                    }
                }
            }
        }

        public void RestartSoundsWithNewPack(SEENG_modManager modManager, string newPrefix)
        {
            var config = modManager.AvailablePacks.ContainsKey(newPrefix)
                         ? modManager.AvailablePacks[newPrefix]
                         : modManager.CurrentPackConfig;

            SEENG_enginesParametrs.MaxEnginePitchShift = config.MaxEnginePitchShift;
            SEENG_enginesParametrs.MaxEngine50PitchShift = config.MaxEngine50PitchShift;
            SEENG_enginesParametrs.EngineVolumes = config.EngineVolumes;
            SEENG_enginesParametrs.Engine50Volumes = config.Engine50Volumes;

            //full reaload
            foreach (var session in _activeSessions.Values) session.Dispose();
            _activeSessions.Clear();
            ScanNearbyShips(modManager);
            _scanCounter = 60;

            if (string.IsNullOrEmpty(newPrefix) || !modManager._availablePacks.ContainsKey(newPrefix))
            {
                newPrefix = "ImprovedVanilla";
            }
        }

        public void Dispose()
        {
            foreach (var session in _activeSessions.Values)
            {
                session.Dispose();
            }
            _activeSessions.Clear();
        }
    }
}