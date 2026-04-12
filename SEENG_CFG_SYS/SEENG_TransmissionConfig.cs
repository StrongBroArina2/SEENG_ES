using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace SEENG_ES
{
    public class SEENG_TransmissionConfig
    {
        // SND_CT
        public List<float> GearRatios { get; set; } = new List<float> { 0f, 3.8f, 2.2f, 1.5f, 1.1f, 0.85f };
        public List<float> UpshiftRPM { get; set; } = new List<float> { 0f, 4000f, 4100f, 4200f, 4300f, 4400f };
        public List<float> DownshiftRPM { get; set; } = new List<float> { 0f, 1800f, 1700f, 1600f, 1500f, 1400f };

        // SND_CTS
        public List<float> GearRatiosS { get; set; } = new List<float> { 0f, 3.5f, 2.4f, 1.7f, 1.2f, 0.9f };
        public List<float> UpshiftSpeedThresholds { get; set; } = new List<float> { 0f, 0.20f, 0.45f, 0.65f, 0.85f };
        public List<float> DownshiftSpeedThresholds { get; set; } = new List<float> { 0f, 0.12f, 0.35f, 0.55f, 0.75f };

       // public float RedlineRPM { get; set; } = 4500f;
       // public float IdleRPM { get; set; } = 800f;
        public bool SkidSteering { get; set; } = false;

        private const string SECTION = "SEENG_CAR";

        public static SEENG_TransmissionConfig Default => new SEENG_TransmissionConfig();

        public void ParseFromSBC(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element) continue;

                switch (reader.Name)
                {
                    case "GearRatios": GearRatios = ParseFloatList(reader.ReadElementContentAsString()); break;
                    case "UpshiftRPM": UpshiftRPM = ParseFloatList(reader.ReadElementContentAsString()); break;
                    case "DownshiftRPM": DownshiftRPM = ParseFloatList(reader.ReadElementContentAsString()); break;
                    case "GearRatiosS": GearRatiosS = ParseFloatList(reader.ReadElementContentAsString()); break;
                    case "UpshiftSpeedThresholds": UpshiftSpeedThresholds = ParseFloatList(reader.ReadElementContentAsString()); break;
                    case "DownshiftSpeedThresholds": DownshiftSpeedThresholds = ParseFloatList(reader.ReadElementContentAsString()); break;
                 //   case "RedlineRPM": float.TryParse(reader.ReadElementContentAsString(), out float r); RedlineRPM = r; break;
                 //   case "IdleRPM": float.TryParse(reader.ReadElementContentAsString(), out float i); IdleRPM = i; break;
                    case "SkidSteering": bool.TryParse(reader.ReadElementContentAsString(), out bool s); SkidSteering = s; break;
                }
            }
        }

        public void ParseFromCustomData(MyIni ini)
        {
            if (!ini.ContainsSection(SECTION)) return;

            if (ini.ContainsKey(SECTION, "GearRatios")) GearRatios = ParseFloatList(ini.Get(SECTION, "GearRatios").ToString());
            if (ini.ContainsKey(SECTION, "UpshiftRPM")) UpshiftRPM = ParseFloatList(ini.Get(SECTION, "UpshiftRPM").ToString());
            if (ini.ContainsKey(SECTION, "DownshiftRPM")) DownshiftRPM = ParseFloatList(ini.Get(SECTION, "DownshiftRPM").ToString());

            if (ini.ContainsKey(SECTION, "GearRatiosS")) GearRatiosS = ParseFloatList(ini.Get(SECTION, "GearRatiosS").ToString());
            if (ini.ContainsKey(SECTION, "UpshiftSpeedThresholds")) UpshiftSpeedThresholds = ParseFloatList(ini.Get(SECTION, "UpshiftSpeedThresholds").ToString());
            if (ini.ContainsKey(SECTION, "DownshiftSpeedThresholds")) DownshiftSpeedThresholds = ParseFloatList(ini.Get(SECTION, "DownshiftSpeedThresholds").ToString());

          //  if (ini.ContainsKey(SECTION, "RedlineRPM")) RedlineRPM = ini.Get(SECTION, "RedlineRPM").ToSingle(RedlineRPM);
          //  if (ini.ContainsKey(SECTION, "IdleRPM")) IdleRPM = ini.Get(SECTION, "IdleRPM").ToSingle(IdleRPM);
            if (ini.ContainsKey(SECTION, "SkidSteering")) SkidSteering = ini.Get(SECTION, "SkidSteering").ToBoolean(SkidSteering);
        }

        private List<float> ParseFloatList(string input)
        {
            var list = new List<float>();
            var parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                    list.Add(val);
            }
            return list.Count > 0 ? list : new List<float> { 0f };
        }
    }
}