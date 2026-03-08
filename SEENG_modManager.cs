using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using Sandbox.ModAPI;
using VRage.Utils;

namespace SEENG_ES
{
    public class SEENG_modManager
    {
        public Dictionary<string, PackConfig> _availablePacks = new Dictionary<string, PackConfig>();
        public PackConfig CurrentPackConfig { get; set; } = new PackConfig { Prefix = "", MaxEnginePitchShift = 15f, MaxEngine50PitchShift = 15f };
        public string CurrentPack { get; private set; } = "";
        public Dictionary<string, PackConfig> _workshopPacks = new Dictionary<string, PackConfig>();
        public Dictionary<string, PackConfig> _debugPacks = new Dictionary<string, PackConfig>();
        private bool _showDebugPacks = false;

        public bool ShowDebugPacks
        {
            get => _showDebugPacks;
            set => _showDebugPacks = value;
        }

        public Dictionary<string, PackConfig> AvailablePacks
        {
            get
            {
                return _showDebugPacks ? _debugPacks : _workshopPacks;
            }
        }


        public void SetCurrentPack(string prefix)
        {
            if (_availablePacks.ContainsKey(prefix))
            {
                CurrentPackConfig = _availablePacks[prefix];
                MyLog.Default.WriteLine($"SEENG_ES: Addon '{prefix}'");
            }
            else
            {
            }
        }

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
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(appDataPath)) return;

            string modsPath = Path.Combine(appDataPath, "SpaceEngineers", "Mods");
            if (!Directory.Exists(modsPath)) return;

            _availablePacks.Clear();


            // Local
            foreach (string modDir in Directory.GetDirectories(modsPath))
            {
                string configPath = Path.Combine(modDir, "SEENG_Config.sbc");
                if (File.Exists(configPath))
                {
                    var config = ParseConfig(configPath);
                    config.ModPath = modDir;
                    config.DisplayName = "[DEBUG] " + config.Prefix;
                    if (!string.IsNullOrEmpty(config.Prefix) && !_debugPacks.ContainsKey(config.Prefix))
                    {
                        _debugPacks[config.Prefix] = config;
                        //MyLog.Default.WriteLine($"SEENG_ES: Allocated DEBUG Addon '{config.Prefix}' in {configPath} ({config.ModPath}).");
                    }
                }
            }

            // Workshop
            string workshopPath = Path.GetFullPath(@"..\..\..\workshop\content\244850\");
            if (!Directory.Exists(workshopPath))
            {
                MyLog.Default.WriteLine("Workshop path not found: " + workshopPath);
                return;
            }

            foreach (string idDir in Directory.GetDirectories(workshopPath))
            {
                string configPath = Path.Combine(idDir, "SEENG_Config.sbc");
                if (File.Exists(configPath))
                {
                    var config = ParseConfig(configPath);
                    config.ModPath = idDir;
                    config.DisplayName = config.Prefix;
                    if (!string.IsNullOrEmpty(config.Prefix) && !_workshopPacks.ContainsKey(config.Prefix))
                    {
                        _workshopPacks[config.Prefix] = config;
                        //MyLog.Default.WriteLine($"SEENG_ES: Allocated Workshop Addon '{config.Prefix}' in {configPath} ({config.ModPath}).");
                    }
                }
            }

            if (!_availablePacks.ContainsKey("ImprovedVanilla"))
            {
                MyLog.Default.WriteLine(
                    "SEENG_ES: CRITICAL: Required pack 'ImprovedVanilla' not found! " +
                    "Please install the SEENG Engine sounds mod or client mod."
                );
            }

            MyLog.Default.WriteLine($"SEENG_ES: Addons: {_workshopPacks.Count}, Debug addons: {_debugPacks.Count}.");
        }

        private PackConfig ParseConfig(string configPath)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(configPath))
                {
                    string prefix = "";
                    string friendlyName = "";
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
                            else if (reader.Name == "FriendlyName")
                            {
                                reader.Read();
                                friendlyName = reader.Value.Trim();
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
                        DisplayName = prefix,
                        FriendlyName = friendlyName,
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
                var currentPacks = AvailablePacks;
                string packList = $"Addons ({(_showDebugPacks ? "Debug" : "Workshop")}):\n";
                foreach (var pack in currentPacks.OrderBy(k => k.Key))
                {
                    packList += pack.Value.DisplayName + "\n";
                }
                MyAPIGateway.Utilities.ShowMessage("SEENG_ES", packList);
            }
            else if (args.Length == 2)
            {
                string cmd = args[1].ToLower();
                if (cmd == "debug")
                {
                    _showDebugPacks = !_showDebugPacks;
                    string mode = _showDebugPacks ? "Debug (AppData)" : "Workshop";
                    MyAPIGateway.Utilities.ShowMessage("SEENG_ES", $"Switched to {mode} addons.");
                    return;
                }
                else if (cmd == "reload")
                {
                    ScanMods();
                    string currentPrefix = CurrentPackConfig.Prefix;
                    var currentPacks = AvailablePacks;
                    if (!string.IsNullOrEmpty(currentPrefix) && currentPacks.ContainsKey(currentPrefix))
                    {
                        CurrentPackConfig = currentPacks[currentPrefix];
                        logic?.RestartSoundsWithNewPack(this, currentPrefix);
                        MyAPIGateway.Utilities.ShowMessage("SEENG_ES", "Configs reloaded.");
                    }
                    else
                    {
                        CurrentPackConfig = currentPacks["ImprovedVanilla"];
                        logic?.RestartSoundsWithNewPack(this, "ImprovedVanilla");
                        MyAPIGateway.Utilities.ShowMessage("SEENG_ES", "Configs reloaded. Fallback to ImprovedVanilla.");
                    }
                    return;
                }
                else // /seeng prefix
                {
                    var currentPacks = AvailablePacks;
                    string requestedPrefix = args[1];
                    if (currentPacks.ContainsKey(requestedPrefix))
                    {
                        CurrentPackConfig = currentPacks[requestedPrefix];
                        MyAPIGateway.Utilities.ShowMessage("SEENG_ES", $"Addon '{currentPacks[requestedPrefix].DisplayName}'.");
                        MyLog.Default.WriteLine($"SEENG_ES: Addon set '{requestedPrefix}'.");

                        logic?.RestartSoundsWithNewPack(this, requestedPrefix);
                    }
                    else
                    {
                        MyAPIGateway.Utilities.ShowMessage("SEENG_ES", $"Are u blind?!");
                    }
                }
            }
        }

        public void Dispose()
        {
            UnsubscribeFromChat();
        }
    }
}