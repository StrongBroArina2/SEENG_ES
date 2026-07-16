using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace SEENG_SElauncher.SEENG_Managers
{
    public class SEENG_AtmosphereManager
    {
        public float AtmosphereLevel { get; private set; } = 0f;
        public bool IsInAtmosphere { get; private set; } = false;

        private int _nextRecalculation = 0;

        public void Update(IMyCockpit cockpit)
        {
            if (cockpit?.CubeGrid == null)
            {
                AtmosphereLevel = 0f;
                IsInAtmosphere = false;
                return;
            }

            if (MyAPIGateway.Session.GameplayFrameCounter < _nextRecalculation)
                return;

            RecalculateAtmosphere(cockpit.CubeGrid);

            _nextRecalculation = MyAPIGateway.Session.GameplayFrameCounter + 12;
        }

        private void RecalculateAtmosphere(IMyCubeGrid grid)
        {
            if (grid.Physics == null)
            {
                AtmosphereLevel = 0f;
                IsInAtmosphere = false;
                return;
            }

            Vector3D center = grid.PositionComp.WorldAABB.Center;


            var closestPlanet = MyGamePruningStructure.GetClosestPlanet(center);

            if (closestPlanet == null)
            {
                AtmosphereLevel = 0f;
                IsInAtmosphere = false;
                return;
            }

            AtmosphereLevel = closestPlanet.GetAirDensity(center);
            IsInAtmosphere = closestPlanet.HasAtmosphere && AtmosphereLevel > 0.001f;
        }

        public void Reset()
        {
            AtmosphereLevel = 0f;
            IsInAtmosphere = false;
            _nextRecalculation = 0;
        }
    }
}