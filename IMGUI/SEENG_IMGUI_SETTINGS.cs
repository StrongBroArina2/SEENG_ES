using ImGuiNET;
using Sandbox.ModAPI;
using SEENG_SElauncher.SEENG_CFG_SYS;
using SEENG_ES;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Utils;
using VRage.Game;

namespace SEENG_SElauncher.IMGUI
{
    public class SEENG_IMGUI_SETTINGS
    {
        private SEENG_modManager _modManager;
        private SLogic _logic;
        private bool _showWindow = false;
        private int _selectedProfileIndex = 0;
        private Dictionary<VehicleClass, string> _tempPackMapping = new Dictionary<VehicleClass, string>();
        private Dictionary<VehicleClass, float> _tempSpeedMapping = new Dictionary<VehicleClass, float>();
        private float _tempVolume = 100f;
        private bool _tempUseWorldSpeed = false;

        public bool IsVisible => _showWindow;

        public SEENG_IMGUI_SETTINGS(SEENG_modManager modManager, SLogic logic)
        {
            _modManager = modManager;
            _logic = logic;
        }

        public void Toggle()
        {
            _showWindow = !_showWindow;
            if (_showWindow)
                InitializeFromCurrentProfile();
        }

        public void Show()
        {
            _showWindow = true;
            InitializeFromCurrentProfile();
        }

        public void Hide()
        {
            _showWindow = false;
        }

        private void InitializeFromCurrentProfile()
        {
            _selectedProfileIndex = SEENG_CFG_Settings.CurrentProfileIndex;
            var profile = SEENG_CFG_Settings.CurrentProfile;
            _tempPackMapping = new Dictionary<VehicleClass, string>(profile.PackMapping);
            _tempSpeedMapping = new Dictionary<VehicleClass, float>(profile.SpeedMapping);
            _tempVolume = profile.Volume;
            _tempUseWorldSpeed = profile.UseWorldSpeed;
        }

        public void OnFrame()
        {
            if (!_showWindow) return;

            var displaySize = ImGui.GetIO().DisplaySize;
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(displaySize.X * 0.5f, displaySize.Y * 0.5f), ImGuiCond.Always, new System.Numerics.Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(750, 900), ImGuiCond.Once);

            bool windowOpen = true;
            if (!ImGui.Begin("Settings", ref windowOpen, ImGuiWindowFlags.NoResize))
            {
                ImGui.End();
                return;
            }

            try
            {
                ImGui.Text("Profiles:");
                ImGui.Separator();
                ImGui.Spacing();

                float profileButtonWidth = (ImGui.GetWindowWidth() - 40f) / 3f;
                float profileButtonHeight = 40f;

                for (int i = 0; i < 3; i++)
                {
                    if (i > 0) ImGui.SameLine();

                    string profileName = i < SEENG_CFG_Settings.Profiles.Count
                        ? SEENG_CFG_Settings.Profiles[i].Name
                        : $"Profile {i + 1}";

                    bool isSelected = _selectedProfileIndex == i;
                    if (isSelected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.2f, 0.5f, 0.2f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.3f, 0.6f, 0.3f, 1.0f));
                    }

                    if (ImGui.Button(profileName, new System.Numerics.Vector2(profileButtonWidth, profileButtonHeight)))
                    {
                        SaveTempToProfile(_selectedProfileIndex);
                        _selectedProfileIndex = i;
                        LoadProfileToTemp(i);
                    }

                    if (isSelected)
                    {
                        ImGui.PopStyleColor(2);
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // VOLUME SLIDER
                ImGui.Text("Volume:");
                ImGui.SetNextItemWidth(-1f);
                if (ImGui.SliderFloat("##settingsVolume", ref _tempVolume, 0f, 200f, "%.0f %%"))
                {
                    SEENG_VolumeManager1.MasterVolume = _tempVolume;
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // WORLD SPEED
                if (ImGui.Checkbox("Use World Speed Settings (Max speed for grids in this world/server)", ref _tempUseWorldSpeed))
                {
                }

                if (_tempUseWorldSpeed)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f), "Using world ships speed");
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // VEHICLE CLASS
                ImGui.Text("Vehicle Class Configuration");
                ImGui.Separator();
                ImGui.Spacing();

                var packKeys = _modManager.AvailablePacks.Keys.ToArray();

                // S_Ship
                DrawVehicleClassRow("Small Ship:", VehicleClass.S_Ship, "##smallShip", packKeys); 

                ImGui.Spacing();

                // L_Ship
                DrawVehicleClassRow("Large Ship:", VehicleClass.L_Ship, "##largeShip", packKeys);

                ImGui.Spacing();

                // S_Rover
                DrawVehicleClassRow("Small Rover:", VehicleClass.S_Rover, "##smallRover", packKeys);

                ImGui.Spacing();

                // L_Rover
                DrawVehicleClassRow("Large Rover:", VehicleClass.L_Rover, "##largeRover", packKeys);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1f), "Version: 1.4.2");
                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1f), "SEENG Engine Sounds");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // APPLY / CANCEL
                float buttonWidth = 120f;
                float buttonSpacing = (ImGui.GetWindowWidth() - buttonWidth * 2) * 0.5f;

                ImGui.SetCursorPosX(buttonSpacing);
                if (ImGui.Button("Apply", new System.Numerics.Vector2(buttonWidth, 35)))
                {
                    ApplySettings();
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - buttonWidth - buttonSpacing);
                if (ImGui.Button("Cancel", new System.Numerics.Vector2(buttonWidth, 35)))
                {
                    InitializeFromCurrentProfile();
                    _showWindow = false;
                }
            }
            finally
            {
                ImGui.End();
            }

            if (!windowOpen)
                _showWindow = false;
        }

        private void DrawVehicleClassRow(string label, VehicleClass vc, string comboId, string[] packKeys)
        {
            ImGui.Text(label);
            ImGui.SetNextItemWidth(-1f);

            string currentPack = _tempPackMapping.ContainsKey(vc) ? _tempPackMapping[vc] : "ImprovedVanilla";
            int currentIndex = Array.IndexOf(packKeys, currentPack);
            if (currentIndex < 0) currentIndex = 0;

            if (ImGui.Combo(comboId, ref currentIndex, packKeys, packKeys.Length))
            {
                _tempPackMapping[vc] = packKeys[currentIndex];
            }

            ImGui.SetNextItemWidth(-1f);

            float speedToDisplay;
            if (_tempUseWorldSpeed)
            {
                speedToDisplay = VehicleClassifier.GetWorldMaxSpeed(vc);
            }
            else
            {
                speedToDisplay = _tempSpeedMapping.ContainsKey(vc) ? _tempSpeedMapping[vc] : VehicleClassifier.GetDefaultMaxSpeed(vc);
            }

            string speedLabel = $"{vc.ToString()} Speed: %.0f m/s";

            if (_tempUseWorldSpeed)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 0.5f));
                ImGui.PushStyleColor(ImGuiCol.SliderGrab, new System.Numerics.Vector4(0.4f, 0.4f, 0.4f, 0.5f));
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f));
                ImGui.SliderFloat($"##{comboId}Speed", ref speedToDisplay, 10f, 500f, speedLabel);

                ImGui.PopStyleColor(3);
            }
            else
            {
                if (ImGui.SliderFloat($"##{comboId}Speed", ref speedToDisplay, 10f, 500f, speedLabel))
                {
                    _tempSpeedMapping[vc] = speedToDisplay;
                }
            }
        }

        private void SaveTempToProfile(int profileIndex)
        {
            if (profileIndex < 0 || profileIndex >= SEENG_CFG_Settings.Profiles.Count) return;
            var profile = SEENG_CFG_Settings.Profiles[profileIndex];
            profile.PackMapping = new Dictionary<VehicleClass, string>(_tempPackMapping);
            profile.SpeedMapping = new Dictionary<VehicleClass, float>(_tempSpeedMapping);
            profile.Volume = _tempVolume;
            profile.UseWorldSpeed = _tempUseWorldSpeed;
        }

        private void LoadProfileToTemp(int profileIndex)
        {
            if (profileIndex < 0 || profileIndex >= SEENG_CFG_Settings.Profiles.Count) return;
            var profile = SEENG_CFG_Settings.Profiles[profileIndex];
            _tempPackMapping = new Dictionary<VehicleClass, string>(profile.PackMapping);
            _tempSpeedMapping = new Dictionary<VehicleClass, float>(profile.SpeedMapping);
            _tempVolume = profile.Volume;
            _tempUseWorldSpeed = profile.UseWorldSpeed;
        }

        private void ApplySettings()
        {
            SaveTempToProfile(_selectedProfileIndex);
            SEENG_CFG_Settings.CurrentProfileIndex = _selectedProfileIndex;
            var activeSpeedMapping = new Dictionary<VehicleClass, float>();
            foreach (VehicleClass vc in Enum.GetValues(typeof(VehicleClass)))
            {
                if (vc == VehicleClass.Unknown) continue;

                if (_tempUseWorldSpeed)
                {
                    activeSpeedMapping[vc] = VehicleClassifier.GetWorldMaxSpeed(vc);
                }
                else
                {
                    activeSpeedMapping[vc] = _tempSpeedMapping.ContainsKey(vc)
                        ? _tempSpeedMapping[vc]
                        : VehicleClassifier.GetDefaultMaxSpeed(vc);
                }
            }
            VehicleClassifier.SetPackMapping(_tempPackMapping);
            VehicleClassifier.SetSpeedMapping(activeSpeedMapping);
            SEENG_CFG_Settings.Save();

            MyAPIGateway.Utilities.ShowNotification("Settings applied and saved!", 3000, MyFontEnum.Green);
            _logic.RestartSoundsWithNewPack(_modManager, _modManager.CurrentPackConfig.Prefix);
            _showWindow = false;
        }
    }
}