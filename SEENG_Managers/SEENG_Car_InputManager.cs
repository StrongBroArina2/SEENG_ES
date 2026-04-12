using Sandbox.ModAPI;
using VRageMath;

namespace SEENG_SElauncher.SEENG_Managers
{
    public class ThrottleThrusterManager
    {
        public bool IsForwardThrottling { get; private set; } = false;
        public bool IsReverseThrottling { get; private set; } = false;

        public bool WasForwardThrottling { get; private set; } = false;
        public bool WasReverseThrottling { get; private set; } = false;

        public bool IsSkidSteering { get; private set; } = false;

        private bool _isTrackedVehicle = false;

        public void SetTrackedVehicle(bool isTracked)
        {
            _isTrackedVehicle = isTracked;
        }   

        public void Update(IMyCockpit cockpit)
        {
            WasForwardThrottling = IsForwardThrottling;
            WasReverseThrottling = IsReverseThrottling;

            if (cockpit == null)
            {
                Reset();
                return;
            }

            var move = cockpit.MoveIndicator;

            IsForwardThrottling = move.Z > 0.12f;   // W
            IsReverseThrottling = move.Z < -0.12f;  // S

            // Skid Steering
            if (_isTrackedVehicle)
            {
                IsSkidSteering = Math.Abs(move.X) > 0.35f && Math.Abs(move.Z) < 0.25f;
            }
            else
            {
                IsSkidSteering = false;
            }
        }

        public void Reset()
        {
            IsForwardThrottling = false;
            IsReverseThrottling = false;
            WasForwardThrottling = false;
            WasReverseThrottling = false;
            IsSkidSteering = false;
        }
    }
}