using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace SEENG_SElauncher.SEENG_CFG_SYS
{
    public enum VehicleClass
    {
        Unknown,
        S_Ship,
        L_Ship,
        S_Rover,
        L_Rover
    }

    public static class VehicleClassifier
    {
        private static Dictionary<VehicleClass, string> _customPackMapping = new Dictionary<VehicleClass, string>();

        public static VehicleClass Classify(IMyCubeGrid grid, out List<IMyThrust> thrusters, out List<IMyMotorSuspension> suspensions)
        {
            thrusters = new List<IMyThrust>();
            suspensions = new List<IMyMotorSuspension>();

            if (grid == null || grid.Physics == null)
                return VehicleClass.Unknown;

            bool isSmall = grid.GridSizeEnum == VRage.Game.MyCubeSize.Small;

            thrusters = grid.GetFatBlocks<IMyThrust>().ToList();
            suspensions = grid.GetFatBlocks<IMyMotorSuspension>().ToList();

            bool hasWorkingThrust = false;
            for (int i = thrusters.Count - 1; i >= 0; i--)
            {
                if (thrusters[i]?.IsWorking == true)
                    hasWorkingThrust = true;
                else
                    thrusters.RemoveAtFast(i);
            }

            bool hasWorkingSuspension = false;
            for (int i = suspensions.Count - 1; i >= 0; i--)
            {
                if (suspensions[i]?.IsWorking == true)
                    hasWorkingSuspension = true;
                else
                    suspensions.RemoveAtFast(i);
            }

            if (hasWorkingThrust)
                return isSmall ? VehicleClass.S_Ship : VehicleClass.L_Ship;

            if (hasWorkingSuspension)
                return isSmall ? VehicleClass.S_Rover : VehicleClass.L_Rover;

            return VehicleClass.Unknown;
        }

        public static string GetDefaultPackPrefix(VehicleClass vehicleClass)
        {
            if (_customPackMapping.ContainsKey(vehicleClass))
            {
                return _customPackMapping[vehicleClass];
            }

            switch (vehicleClass)
            {
                case VehicleClass.S_Ship: return "VSMALL";
                case VehicleClass.L_Ship: return "VLARGE";
                case VehicleClass.S_Rover: return "VRover";
                case VehicleClass.L_Rover: return "VRover";
                default: return "ImprovedVanilla";
            }
        }

        public static void SetPackMapping(Dictionary<VehicleClass, string> mapping)
        {
            _customPackMapping = new Dictionary<VehicleClass, string>(mapping);
        }

        public static void ClearPackMapping()
        {
            _customPackMapping.Clear();
        }
    }
}
