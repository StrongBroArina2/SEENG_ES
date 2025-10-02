using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Sandbox.ModAPI;
using VRage.Utils;

namespace SEENG_ES
{
    public class SEENG_modManager
    {
        private Dictionary<string, PackConfig> _availablePacks = new Dictionary<string, PackConfig>();
        public PackConfig CurrentPackConfig { get; private set; } = new PackConfig { Prefix = "", MaxEnginePitchShift = 15f, MaxEngine50PitchShift = 15f };
        public string CurrentPack { get; private set; } = "";

        public void Init()
        {
            if (MyAPIGateway.Session != null)
            {
                ScanMods();
            }
            else
            {
            }
        }

        public void SubscribeToChat(SLogic logic)
        {
            if (MyAPIGateway.Utilities != null)
            {
                MyAPIGateway.Utilities.MessageEntered += (string msg, ref bool send) => OnChatMessage(msg, ref send, logic);
            }
        }

        public void UnsubscribeFromChat()
        {
            if (MyAPIGateway.Utilities != null)
            {
                MyAPIGateway.Utilities.MessageEntered -= (string msg, ref bool send) => OnChatMessage(msg, ref send, null);
            }
        }

        public void ScanMods()
        {
            if (MyAPIGateway.Session?.Mods == null)
            {
                return;
            }

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(appDataPath))
            {
                return;
            }

            string modsPath = Path.Combine(appDataPath, "SpaceEngineers", "Mods");
            if (!Directory.Exists(modsPath))
            {
                return;
            }

            _availablePacks.Clear();

            foreach (var modItem in MyAPIGateway.Session.Mods)
            {
                if (string.IsNullOrEmpty(modItem.Name)) continue;

                string modFolder = modItem.Name;
                string configPath = Path.Combine(modsPath, modFolder, "SEENG_Config.sbc");

                if (File.Exists(configPath))
                {
                    var config = ParseConfig(configPath);
                    if (!string.IsNullOrEmpty(config.Prefix) && !_availablePacks.ContainsKey(config.Prefix))
                    {
                        _availablePacks[config.Prefix] = config;
                    }
                }
                else
                {
                    if (modItem.PublishedFileId > 0)
                    {
                        string steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "workshop", "content", "244850");
                        string idFolder = modItem.PublishedFileId.ToString();
                        string altConfigPath = Path.Combine(steamPath, idFolder, "SEENG_Config.sbc");

                        if (File.Exists(altConfigPath))
                        {
                            var config = ParseConfig(altConfigPath);
                            if (!string.IsNullOrEmpty(config.Prefix) && !_availablePacks.ContainsKey(config.Prefix))
                            {
                                _availablePacks[config.Prefix] = config;
                            }
                        }
                    }
                }
            }


        }

        private PackConfig ParseConfig(string configPath)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(configPath))
                {
                    string prefix = "";
                    float maxPitchShift = 15f;
                    float max50PitchShift = 15f;
                    List<VolumePoint> engineVolumes = new List<VolumePoint>();
                    List<VolumePoint> engine50Volumes = new List<VolumePoint>();
                    string currentElement = "";

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            currentElement = reader.Name;

                            if (currentElement == "Prefix")
                            {
                                reader.Read();
                                prefix = reader.Value.Trim();
                            }
                            else if (currentElement == "MaxEnginePitchShift")
                            {
                                reader.Read();
                                if (float.TryParse(reader.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                                {
                                    maxPitchShift = value;
                                }
                            }
                            else if (currentElement == "MaxEngine50PitchShift")
                            {
                                reader.Read();
                                if (float.TryParse(reader.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                                {
                                    max50PitchShift = value;
                                }
                            }
                            else if (currentElement == "SeengEngineVolume")
                            {
                                float speed = 0f;
                                float volume = 0f;
                                if (reader.MoveToAttribute("Speed") && float.TryParse(reader.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out speed))
                                {
                                }
                                if (reader.MoveToAttribute("Volume") && float.TryParse(reader.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out volume))
                                {
                                }
                                if (currentElement == "SeengEngineVolume" && speed >= 0f && speed <= 100f && volume >= 0f && volume <= 1f)
                                {
                                    engineVolumes.Add(new VolumePoint { Speed = speed, Volume = volume });
                                }
                            }
                            else if (currentElement == "SeengEngine50Volume")
                            {
                                float speed = 0f;
                                float volume = 0f;
                                if (reader.MoveToAttribute("Speed") && float.TryParse(reader.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out speed))
                                {
                                }
                                if (reader.MoveToAttribute("Volume") && float.TryParse(reader.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out volume))
                                {
                                }
                                if (currentElement == "SeengEngine50Volume" && speed >= 0f && speed <= 100f && volume >= 0f && volume <= 1f)
                                {
                                    engine50Volumes.Add(new VolumePoint { Speed = speed, Volume = volume });
                                }
                            }
                        }
                    }
                    engineVolumes.Sort((a, b) => a.Speed.CompareTo(b.Speed));
                    engine50Volumes.Sort((a, b) => a.Speed.CompareTo(b.Speed));

                    return new PackConfig
                    {
                        Prefix = prefix,
                        MaxEnginePitchShift = maxPitchShift,
                        MaxEngine50PitchShift = max50PitchShift,
                        EngineVolumes = engineVolumes,
                        Engine50Volumes = engine50Volumes
                    };
                }
            }
            catch (Exception e)
            {
                return new PackConfig { Prefix = "", MaxEnginePitchShift = 15f, MaxEngine50PitchShift = 15f };
            }
        }

        private void OnChatMessage(string message, ref bool sendToOthers, SLogic logic)
        {
            if (!message.StartsWith("/seeng", StringComparison.OrdinalIgnoreCase)) return;

            sendToOthers = false;
            string[] args = message.Split(' ');
            if (args.Length == 1)
            {
                string packList = "Available addons:\n";
                foreach (var pack in _availablePacks)
                {
                    packList += pack.Key + "\n";
                }
                MyAPIGateway.Utilities.ShowMessage("SEENG_ES", packList);
            }
            else if (args.Length == 2)
            {
                string requestedPrefix = args[1];
                if (_availablePacks.ContainsKey(requestedPrefix))
                {
                    CurrentPackConfig = _availablePacks[requestedPrefix];
                    MyAPIGateway.Utilities.ShowMessage("SEENG_ES", $"Addon '{requestedPrefix}' loaded.");
                    MyLog.Default.WriteLine($"SEENG_ES: Addon '{requestedPrefix}' loaded.");
                    logic?.RestartSoundsWithNewPack(this, requestedPrefix);
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("SEENG_ES", $"Addon '{requestedPrefix}' not loaded as a mod or just not.");
                }
            }
        }

        public void Dispose()
        {
            UnsubscribeFromChat();
        }
    }
}