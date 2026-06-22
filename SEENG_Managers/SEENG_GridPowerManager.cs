using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace SEENG_SElauncher.SEENG_Managers
{
    public class SEENG_GridPowerManager
    {
        private IMyResourceDistributorComponent _distributor;
        private IMyCubeGrid _currentGrid;

        public float PowerLoadPercent { get; private set; } = 0f;
        private float _smoothedPower = 0f;
        private const float SMOOTH_FACTOR = 0.15f; 

        public void Update(IMyCockpit cockpit)
        {
            if (cockpit?.CubeGrid == null)
            {
                PowerLoadPercent = 0f;
                _smoothedPower = 0f;
                return;
            }

            if (_currentGrid != cockpit.CubeGrid)
            {
                _currentGrid = cockpit.CubeGrid;
                _distributor = _currentGrid.ResourceDistributor;
            }

            if (_distributor == null)
            {
                PowerLoadPercent = 0f;
                return;
            }

            var electricityId = MyResourceDistributorComponent.ElectricityId;

            float maxAvailable = _distributor.MaxAvailableResourceByType(electricityId, _currentGrid);
            float requiredInput = _distributor.TotalRequiredInputByType(electricityId, _currentGrid);

            float load = 0f;
            if (maxAvailable > 0.001f)
            {
                load = MathHelper.Clamp((requiredInput / maxAvailable) * 100f, 0f, 100f);
            }
            else if (requiredInput > 0.001f)
            {
                load = 100f;
            }

            _smoothedPower = MathHelper.Lerp(_smoothedPower, load, SMOOTH_FACTOR);
            PowerLoadPercent = _smoothedPower;
        }

        public void Reset()
        {
            PowerLoadPercent = 0f;
            _smoothedPower = 0f;
            _currentGrid = null;
            _distributor = null;
        }
    }
}