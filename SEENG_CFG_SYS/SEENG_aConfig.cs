using Sandbox.ModAPI;
using VRage.Utils;
using System.Globalization;
using VRage.Game.ModAPI.Ingame.Utilities;
using SEENG_ES;

namespace SEENG_SElauncher.SEENG_CFG_SYS
{
    public class SEENG_aConfig
    {
        public static void UpdateMaxSpeedInCustomData(IMyCockpit cockpit, float newMaxSpeed)
        {
            if (cockpit == null) return;

            string customData = cockpit.CustomData;
            MyIni fullIni = new MyIni();
            MyIniParseResult parseResult;
            if (!string.IsNullOrEmpty(customData))
            {
                if (!fullIni.TryParse(customData, out parseResult))
                {
                    return;
                }
            }

            fullIni.Set("SEENG", "seeng_maxspeed", newMaxSpeed.ToString(CultureInfo.InvariantCulture));
            cockpit.CustomData = fullIni.ToString();
        }

        public static float GetCurrentMaxSpeedFromCustomData(IMyCockpit cockpit)
        {
            if (cockpit == null) return 120f;

            string customData = cockpit.CustomData;
            if (string.IsNullOrEmpty(customData)) return 120f;

            MyIni fullIni = new MyIni();
            MyIniParseResult parseResult;
            if (!fullIni.TryParse(customData, out parseResult))
            {
                MyLog.Default.WriteLine("SEENG_ES: CustomData failed you.");
                return 120f;
            }
            string maxSpeedStr = fullIni.Get("SEENG", "seeng_maxspeed").ToString();
            if (float.TryParse(maxSpeedStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float maxSpeed) && maxSpeed > 0f)
            {
                return maxSpeed * 1.2f;
            }

            return 120f;
        }

        public static SEENG_TransmissionConfig GetTransmissionConfig(IMyCockpit cockpit, SEENG_TransmissionConfig packTransmission = null)
        {
            var config = new SEENG_TransmissionConfig();

            if (cockpit != null)
            {
                string customData = cockpit.CustomData;
                if (!string.IsNullOrEmpty(customData))
                {
                    MyIni ini = new MyIni();
                    if (ini.TryParse(customData))
                    {
                        config.ParseFromCustomData(ini);
                    }
                }
            }

            if (packTransmission != null && config.GearRatios.Count <= 1)
            {
                config = packTransmission;
            }

            return config;
        }
        public static bool GetSkidSteering(IMyCockpit cockpit, bool packDefault = false)
        {
            if (cockpit != null)
            {
                string customData = cockpit.CustomData;
                if (!string.IsNullOrEmpty(customData))
                {
                    MyIni ini = new MyIni();
                    if (ini.TryParse(customData) && ini.ContainsKey("SEENG", "SkidSteering"))
                    {
                        return ini.Get("SEENG", "SkidSteering").ToBoolean(false);
                    }
                }
            }

            return packDefault;
        }

        public static bool IsBlockApiEnabled(IMyCockpit cockpit)
        {
            if (cockpit == null) return false;
            string customData = cockpit.CustomData;
            if (string.IsNullOrEmpty(customData)) return false;

            MyIni ini = new MyIni();
            if (ini.TryParse(customData) && ini.ContainsKey("SEENG", "seeng_blockapi"))
            {
                return ini.Get("SEENG", "seeng_blockapi").ToBoolean(false);
            }
            return false;
        }
        public static void UpdatePackPrefixInCustomData(IMyCockpit cockpit, string prefix)
        {
            if (cockpit == null) return;
            MyIni ini = new MyIni();
            ini.TryParse(cockpit.CustomData);
            ini.Set("SEENG", "seeng_pack", prefix);
            cockpit.CustomData = ini.ToString();
        }
        public static string GetPackPrefixFromCustomData(IMyCockpit cockpit, string defaultPrefix)
        {
            if (cockpit == null) return defaultPrefix;

            string customData = cockpit.CustomData;
            if (string.IsNullOrEmpty(customData)) return defaultPrefix;

            MyIni fullIni = new MyIni();
            if (!fullIni.TryParse(customData)) return defaultPrefix;

            if (fullIni.ContainsSection("SEENG") && fullIni.ContainsKey("SEENG", "seeng_pack"))
            {
                string prefix = fullIni.Get("SEENG", "seeng_pack").ToString();
                if (!string.IsNullOrEmpty(prefix)) return prefix;
            }

            return defaultPrefix;
        }

    }
}