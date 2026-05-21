using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using SEENG_SElauncher;
using SEENG_SElauncher.SEENG_CFG_SYS;
using SEENG_SElauncher.SEENG_Managers;
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
            if (_scanCounter++ % 60 == 0)
            {
                ScanNearbyShips(modManager);
            }

            _toRemove.Clear();

            foreach (var kvp in _activeSessions)
            {
                var session = kvp.Value;

                if (session.Cockpit == null || session.Cockpit.Closed || !_sessionChecker.HasSEENGTag(session.Cockpit) )
                {
                    _toRemove.Add(kvp.Key);
                    continue;
                }
                session.Update(modManager);
            }

            foreach (var id in _toRemove)
            {
                if (_activeSessions.ContainsKey(id))
                {
                    _activeSessions[id].Dispose();
                    _activeSessions.Remove(id);
                }
            }
        }
        private readonly List<long> _toRemove = new List<long>();

        private void ScanNearbyShips(SEENG_modManager modManager)
        {
            _entitiesBuffer.Clear();
            MyAPIGateway.Entities.GetEntities(_entitiesBuffer);
            var listenerPos = MyAPIGateway.Session.Camera?.WorldMatrix.Translation ?? Vector3D.Zero;

            foreach (var entity in _entitiesBuffer)
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null || grid.Physics == null) continue;

                if (Vector3D.DistanceSquared(grid.GetPosition(), listenerPos) > 6500 * 6500) //sound sync dist !!!!!!
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
                            PackConfig config = modManager.AvailablePacks.ContainsKey(prefix)
                        ? modManager.AvailablePacks[prefix]
                        : modManager.CurrentPackConfig;
                            var session = new ShipSoundSession(cockpit, config);
                            session.Handler.RestartAll(cockpit, prefix, session.Managers.ThrustManager, session.Managers.SpeedManager, session.Managers.RotationManager, session.Managers.ThrottleThrusterManager, session.TransmissionConfig);
                            _activeSessions.Add(cockpit.EntityId, session);
                        }
                    }
                }
            }
        }

        public void RestartSoundsWithNewPack(SEENG_modManager modManager, string newPrefix)
        {
            if (string.IsNullOrEmpty(newPrefix) || !modManager.AvailablePacks.ContainsKey(newPrefix))
            {
                newPrefix = "ImprovedVanilla";
            }
            modManager.CurrentPackConfig = modManager.AvailablePacks[newPrefix];
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                var validCockpits = new List<IMyCockpit>();
                foreach (var session in _activeSessions.Values)
                {
                    if (session.Cockpit != null && !session.Cockpit.Closed && !session.Cockpit.MarkedForClose)
                    {
                        validCockpits.Add(session.Cockpit);
                    }
                    session.Handler.StopAll();
                    session.Dispose();
                }

                _activeSessions.Clear();
                _entitiesBuffer.Clear();

                foreach (var cockpit in validCockpits)
                {
                    string shipPrefix = SEENG_aConfig.GetPackPrefixFromCustomData(cockpit, newPrefix);
                    if (!modManager.AvailablePacks.TryGetValue(shipPrefix, out PackConfig shipConfig))
                    {
                        if (!modManager.AvailablePacks.TryGetValue("ImprovedVanilla", out shipConfig))
                        {
                            if (modManager.AvailablePacks.Count > 0)
                            {
                                var firstKey = new List<string>(modManager.AvailablePacks.Keys)[0];
                                shipConfig = modManager.AvailablePacks[firstKey];
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    
                    var newSession = new ShipSoundSession(cockpit, shipConfig);
                    newSession.Handler.RestartAll(
                 cockpit,
                 shipPrefix,
                 newSession.Managers.ThrustManager,
                 newSession.Managers.SpeedManager,
                 newSession.Managers.RotationManager,
                 newSession.Managers.ThrottleThrusterManager,
                 newSession.TransmissionConfig
             );
                    _activeSessions.Add(cockpit.EntityId, newSession);
                }
                ScanNearbyShips(modManager);
                _scanCounter = 60;
            });
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