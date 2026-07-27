using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using VRage.Filesystem;
using VRage.FileSystem;
using VRage.Utils;

namespace SEENG_SElauncher.SEENG_CFG_SYS
{
    public class SettingsProfile
    {
        public string Name = "Profile 1";
        public float Volume = 100f;
        public bool UseWorldSpeed = false;

        [XmlElement("PackMapping")]
        public List<PackMappingEntry> PackMappingList = new List<PackMappingEntry>();

        [XmlElement("SpeedMapping")]
        public List<SpeedMappingEntry> SpeedMappingList = new List<SpeedMappingEntry>();

        [XmlIgnore]
        public Dictionary<VehicleClass, string> PackMapping
        {
            get
            {
                var dict = new Dictionary<VehicleClass, string>();
                foreach (var entry in PackMappingList)
                    dict[entry.VehicleClass] = entry.PackPrefix;
                return dict;
            }
            set
            {
                PackMappingList.Clear();
                foreach (var kvp in value)
                    PackMappingList.Add(new PackMappingEntry { VehicleClass = kvp.Key, PackPrefix = kvp.Value });
            }
        }

        [XmlIgnore]
        public Dictionary<VehicleClass, float> SpeedMapping
        {
            get
            {
                var dict = new Dictionary<VehicleClass, float>();
                foreach (var entry in SpeedMappingList)
                    dict[entry.VehicleClass] = entry.Speed;
                return dict;
            }
            set
            {
                SpeedMappingList.Clear();
                foreach (var kvp in value)
                    SpeedMappingList.Add(new SpeedMappingEntry { VehicleClass = kvp.Key, Speed = kvp.Value });
            }
        }
    }

    public class PackMappingEntry
    {
        public VehicleClass VehicleClass = VehicleClass.Unknown;
        public string PackPrefix = "ImprovedVanilla";
    }

    public class SpeedMappingEntry
    {
        public VehicleClass VehicleClass = VehicleClass.Unknown;
        public float Speed = 100f;
    }

    public static class SEENG_CFG_Settings
    {
        private const string FileName = "SEENG_Settings.xml";
        private static string FilePath => Path.Combine(MyFileSystem.UserDataPath, FileName);

        public static List<SettingsProfile> Profiles { get; private set; } = new List<SettingsProfile>();
        public static int CurrentProfileIndex { get; set; } = 0;

        public static SettingsProfile CurrentProfile
        {
            get
            {
                if (Profiles == null || Profiles.Count == 0)
                {
                    Profiles = new List<SettingsProfile>();
                    for (int i = 0; i < 3; i++)
                        Profiles.Add(new SettingsProfile { Name = $"Profile {i + 1}" });
                }
                if (CurrentProfileIndex < 0 || CurrentProfileIndex >= Profiles.Count)
                    CurrentProfileIndex = 0;
                return Profiles[CurrentProfileIndex];
            }
        }

        public static void Load()
        {
            try
            {
                string file = FilePath;
                if (!File.Exists(file))
                {
                    CreateDefaults();
                    return;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(SEENG_CFG_SettingsData));
                using (XmlReader xml = XmlReader.Create(file))
                {
                    var data = (SEENG_CFG_SettingsData)serializer.Deserialize(xml);
                    if (data != null)
                    {
                        Profiles = data.Profiles ?? new List<SettingsProfile>();
                        CurrentProfileIndex = data.CurrentProfileIndex;
                    }
                }

                while (Profiles.Count < 3)
                    Profiles.Add(new SettingsProfile { Name = $"Profile {Profiles.Count + 1}" });

                MyLog.Default.WriteLine($"SEENG_CFG_Settings: Loaded {Profiles.Count} profiles, current index: {CurrentProfileIndex}");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"SEENG_CFG_Settings: Failed to load settings: {ex.Message}");
                CreateDefaults();
            }

            ApplyCurrentProfile();
        }

        public static void Save()
        {
            try
            {
                string file = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                XmlSerializer serializer = new XmlSerializer(typeof(SEENG_CFG_SettingsData));
                var data = new SEENG_CFG_SettingsData
                {
                    Profiles = Profiles,
                    CurrentProfileIndex = CurrentProfileIndex
                };
                using (StreamWriter stream = File.CreateText(file))
                {
                    serializer.Serialize(stream, data);
                }
                MyLog.Default.WriteLine("SEENG_CFG_Settings: Settings saved successfully.");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine($"SEENG_CFG_Settings: Failed to save settings: {ex.Message}");
            }
        }

        public static void ApplyCurrentProfile()
        {
            var profile = CurrentProfile;
            VehicleClassifier.SetPackMapping(profile.PackMapping);
            VehicleClassifier.SetSpeedMapping(profile.SpeedMapping);
            SEENG_ES.SEENG_VolumeManager1.SetVolume(profile.Volume);
        }

        private static void CreateDefaults()
        {
            Profiles = new List<SettingsProfile>();
            for (int i = 0; i < 3; i++)
            {
                var profile = new SettingsProfile { Name = $"Profile {i + 1}", Volume = 100f };
                profile.PackMapping = new Dictionary<VehicleClass, string>
                {
                    { VehicleClass.S_Ship, "ImprovedVanilla" },
                    { VehicleClass.L_Ship, "ImprovedVanilla" },
                    { VehicleClass.S_Rover, "ImprovedVanilla" },
                    { VehicleClass.L_Rover, "ImprovedVanilla" }
                };
                profile.SpeedMapping = new Dictionary<VehicleClass, float>
                {
                    { VehicleClass.S_Ship, VehicleClassifier.GetDefaultMaxSpeed(VehicleClass.S_Ship) },
                    { VehicleClass.L_Ship, VehicleClassifier.GetDefaultMaxSpeed(VehicleClass.L_Ship) },
                    { VehicleClass.S_Rover, VehicleClassifier.GetDefaultMaxSpeed(VehicleClass.S_Rover) },
                    { VehicleClass.L_Rover, VehicleClassifier.GetDefaultMaxSpeed(VehicleClass.L_Rover) }
                };
                Profiles.Add(profile);
            }
            CurrentProfileIndex = 0;
            Save();
        }
    }
    public class SEENG_CFG_SettingsData
    {
        public List<SettingsProfile> Profiles = new List<SettingsProfile>();
        public int CurrentProfileIndex = 0;
    }
}