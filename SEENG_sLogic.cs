using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using SEENG_SElauncher;
using SEENG_SElauncher.SEENG_CFG_SYS;
using SEENG_SElauncher.SEENG_Managers;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using System.Collections.Generic;

namespace SEENG_ES
{
    public class SLogic
    {
        private readonly SessionChecker _sessionChecker = new SessionChecker();
        private readonly Dictionary<long, ShipSoundSession> _activeSessions = new Dictionary<long, ShipSoundSession>();
        private readonly HashSet<IMyEntity> _entitiesBuffer = new HashSet<IMyEntity>();
        private readonly Dictionary<long, long> _defaultSessionGrid = new Dictionary<long, long>();
        private int _scanCounter = 0;

        public void Init(SEENG_modManager modManager)
        {
        }

        public void Update(SEENG_modManager modManager)
        {
            if (MyAPIGateway.Session == null) return;
            if (_scanCounter++ % 160 == 0)
            {
                ScanNearbyShips(modManager);
            }

            _toRemove.Clear();

            foreach (var kvp in _activeSessions)
            {
                var session = kvp.Value;
                long sessionId = kvp.Key;
                if (session.Cockpit == null || session.Cockpit.Closed)
                {
                    _toRemove.Add(sessionId);
                    continue;
                }

                if (!session.IsDefaultSession && !_sessionChecker.HasSEENGTag(session.Cockpit))
                {
                    _toRemove.Add(sessionId);
                    continue;
                }

                if (session.IsDefaultSession)
                {
                    long gridId = session.Cockpit.CubeGrid?.EntityId ?? 0;
                    if (gridId == 0 || !_defaultSessionGrid.ContainsKey(gridId) || _defaultSessionGrid[gridId] != sessionId)
                    {
                        _toRemove.Add(sessionId);
                        continue;
                    }
                }

                session.Update(modManager);
            }

            foreach (var id in _toRemove)
            {
                if (_activeSessions.ContainsKey(id))
                {
                    _activeSessions[id].Dispose();
                    _activeSessions.Remove(id);
                    long gridToRemove = -1;
                    foreach (var kvp in _defaultSessionGrid)
                    {
                        if (kvp.Value == id)
                        {
                            gridToRemove = kvp.Key;
                            break;
                        }
                    }
                    if (gridToRemove != -1)
                        _defaultSessionGrid.Remove(gridToRemove);
                }
            }
        }
        private readonly List<long> _toRemove = new List<long>();

        private void ScanNearbyShips(SEENG_modManager modManager)
        {
            _entitiesBuffer.Clear();
            MyAPIGateway.Entities.GetEntities(_entitiesBuffer);
            var listenerPos = MyAPIGateway.Session.Camera?.WorldMatrix.Translation ?? Vector3D.Zero;

            var taggedGridIds = new HashSet<long>();
            foreach (var kvp in _activeSessions)
            {
                if (!kvp.Value.IsDefaultSession && kvp.Value.Cockpit?.CubeGrid != null && !kvp.Value.Cockpit.Closed)
                {
                    taggedGridIds.Add(kvp.Value.Cockpit.CubeGrid.EntityId);
                }
            }

            foreach (var entity in _entitiesBuffer)
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null || grid.Physics == null) continue;

                if (Vector3D.DistanceSquared(grid.GetPosition(), listenerPos) > 6500 * 6500)
                    continue;

                long gridId = grid.EntityId;
                var slimBlocks = new List<IMySlimBlock>();
                grid.GetBlocks(slimBlocks, b => b.FatBlock is IMyCockpit);

                if (slimBlocks.Count == 0) continue;

                bool hasTaggedCockpit = false;
                foreach (var slim in slimBlocks)
                {
                    var cockpit = slim.FatBlock as IMyCockpit;
                    if (cockpit == null) continue;

                    if (_sessionChecker.HasSEENGTag(cockpit))
                    {
                        hasTaggedCockpit = true;

                        if (!_activeSessions.ContainsKey(cockpit.EntityId))
                        {
                            string prefix = SEENG_aConfig.GetPackPrefixFromCustomData(cockpit, modManager.CurrentPackConfig.Prefix);
                            PackConfig packConfig = modManager.AvailablePacks.ContainsKey(prefix)
                                ? modManager.AvailablePacks[prefix]
                                : modManager.CurrentPackConfig;
                            var newSession = new ShipSoundSession(cockpit, packConfig);
                            newSession.Handler.RestartAll(cockpit, prefix, newSession.Managers, newSession.TransmissionConfig);
                            _activeSessions.Add(cockpit.EntityId, newSession);
                        }
                    }
                }

                if (hasTaggedCockpit)
                {
                    if (_defaultSessionGrid.ContainsKey(gridId))
                    {
                        long oldSessionId = _defaultSessionGrid[gridId];
                        _defaultSessionGrid.Remove(gridId);
                        if (_activeSessions.ContainsKey(oldSessionId))
                        {
                            _activeSessions[oldSessionId].Dispose();
                            _activeSessions.Remove(oldSessionId);
                        }
                    }
                    continue;
                }

                List<IMyThrust> thrusters;
                List<IMyMotorSuspension> suspensions;
                VehicleClass vehicleClass = VehicleClassifier.Classify(grid, out thrusters, out suspensions);

                if (vehicleClass == VehicleClass.Unknown)
                {
                    if (_defaultSessionGrid.ContainsKey(gridId))
                    {
                        long oldSessionId = _defaultSessionGrid[gridId];
                        _defaultSessionGrid.Remove(gridId);
                        if (_activeSessions.ContainsKey(oldSessionId))
                        {
                            _activeSessions[oldSessionId].Dispose();
                            _activeSessions.Remove(oldSessionId);
                        }
                    }
                    continue;
                }

                if (_defaultSessionGrid.ContainsKey(gridId))
                {
                    long existingSessionId = _defaultSessionGrid[gridId];
                    if (_activeSessions.ContainsKey(existingSessionId))
                    {
                        var existingSession = _activeSessions[existingSessionId];
                        if (existingSession.Cockpit != null && !existingSession.Cockpit.Closed && !existingSession.Cockpit.MarkedForClose)
                        {
                            existingSession.SetGridBlocks(thrusters, suspensions);
                            continue;
                        }
                        else
                        {
                            _activeSessions[existingSessionId].Dispose();
                            _activeSessions.Remove(existingSessionId);
                            _defaultSessionGrid.Remove(gridId);
                        }
                    }
                    else
                    {
                        _defaultSessionGrid.Remove(gridId);
                    }
                }
                IMyCockpit primaryCockpit = null;
                foreach (var slim in slimBlocks)
                {
                    var cockpit = slim.FatBlock as IMyCockpit;
                    if (cockpit != null && !cockpit.Closed && !cockpit.MarkedForClose)
                    {
                        primaryCockpit = cockpit;
                        break;
                    }
                }

                if (primaryCockpit == null) continue;

                string defaultPrefix = VehicleClassifier.GetDefaultPackPrefix(vehicleClass);
                PackConfig defaultPackConfig = modManager.AvailablePacks.ContainsKey(defaultPrefix)
                    ? modManager.AvailablePacks[defaultPrefix]
                    : modManager.CurrentPackConfig;

                var defaultSession = new ShipSoundSession(primaryCockpit, defaultPackConfig, vehicleClass, isDefault: true);
                defaultSession.SetGridBlocks(thrusters, suspensions);
                defaultSession.Handler.RestartAll(primaryCockpit, defaultPrefix, defaultSession.Managers, defaultSession.TransmissionConfig);
                _activeSessions.Add(primaryCockpit.EntityId, defaultSession);
                _defaultSessionGrid[gridId] = primaryCockpit.EntityId;
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
                var taggedCockpits = new List<IMyCockpit>();
                foreach (var session in _activeSessions.Values)
                {
                    if (!session.IsDefaultSession && session.Cockpit != null && !session.Cockpit.Closed && !session.Cockpit.MarkedForClose)
                    {
                        taggedCockpits.Add(session.Cockpit);
                    }
                    session.Handler.StopAll();
                    session.Dispose();
                }

                _activeSessions.Clear();
                _defaultSessionGrid.Clear();
                _entitiesBuffer.Clear();

                foreach (var cockpit in taggedCockpits)
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
                    newSession.Handler.RestartAll(cockpit, shipPrefix, newSession.Managers, newSession.TransmissionConfig);
                    _activeSessions.Add(cockpit.EntityId, newSession);
                }

                ScanNearbyShips(modManager);
                _scanCounter = 160;
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