using ImGuiNET;
using CringePlugins.Abstractions;
using CringePlugins.Services;
using Sandbox.ModAPI;
using Sandbox.Graphics.GUI;
using VRage.Input;
using Sandbox.Game;
using Microsoft.Extensions.DependencyInjection;
using Sandbox;
using CringePlugins.Config;
using System.IO;
using VRage.Game.ModAPI;
using System;
using VRage.Utils;

namespace SEENG_ES
{
    public class SEENGRenderComponent : IRenderComponent
    {
        private bool _showMenu = false;
        private SEENG_modManager _modManager;
        private SLogic _logic;
        private int _selectedIndex = 0;
        private string _descriptionText = "...";
        private string _bigDescText = "...";
        private readonly IImGuiImageService _imageService;
        private bool _showSpeedWindow = false;
        private string _speedInputText = "120";
        private SessionChecker _sessionChecker = new SessionChecker();
        private RefitResult? _pendingRefitResult;
        private string _selectedPack = "";

        public SEENGRenderComponent(SEENG_modManager modManager, SLogic logic, IImGuiImageService imageService = null)
        {
            _modManager = modManager ?? throw new ArgumentNullException(nameof(modManager));
            _logic = logic ?? throw new ArgumentNullException(nameof(logic));
            _imageService = imageService ?? MySandboxGame.Services.GetRequiredService<IImGuiImageService>();
        }

        public void OnFrame()
        {
            if (MyAPIGateway.Input?.IsNewKeyPressed(MyKeys.F1) == true)
            {
                _showMenu = !_showMenu;
                if (_showMenu)
                {
                    _selectedIndex = 0;
                    UpdateDescription(0);
                    _selectedPack = "";
                }
            }

            if (!_showMenu) return;

            var displaySize = ImGui.GetIO().DisplaySize;
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(displaySize.X * 0.5f, displaySize.Y * 0.5f), ImGuiCond.FirstUseEver, new System.Numerics.Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(displaySize.X * 0.75f, displaySize.Y * 0.75f), ImGuiCond.Once);

            if (!ImGui.Begin("SEENG Engine Sounds", ref _showMenu, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground))
            {
                ImGui.End();
                return;
            }

            try
            {
                var windowSize = ImGui.GetWindowSize();

                // L window background
                var secondaryPos = new System.Numerics.Vector2(windowSize.X * 0.625f, windowSize.Y * 0.1f);
                var secondarySize = new System.Numerics.Vector2(windowSize.X * 0.35f, windowSize.Y * 0.8f);
                ImGui.SetCursorPos(secondaryPos);
                var drawListSecondary = ImGui.GetWindowDrawList();
                var secondaryPanelPos = ImGui.GetCursorScreenPos();
                drawListSecondary.AddRectFilled(secondaryPanelPos, new System.Numerics.Vector2(secondaryPanelPos.X + secondarySize.X, secondaryPanelPos.Y + secondarySize.Y), 0x66000000, 0, 0);
                ImGui.Dummy(secondarySize);

                // R background
                var tertiaryPos = new System.Numerics.Vector2(windowSize.X * 0.025f, windowSize.Y * 0.1f);
                var tertiarySize = new System.Numerics.Vector2(windowSize.X * 0.35f, windowSize.Y * 0.8f);
                ImGui.SetCursorPos(tertiaryPos);
                var drawListTertiary = ImGui.GetWindowDrawList();
                var tertiaryPanelPos = ImGui.GetCursorScreenPos();
                drawListTertiary.AddRectFilled(tertiaryPanelPos, new System.Numerics.Vector2(tertiaryPanelPos.X + tertiarySize.X, tertiaryPanelPos.Y + tertiarySize.Y), 0x66000000, 0, 0);
                ImGui.Dummy(tertiarySize);

                // Text
                var listboxCenterX = windowSize.X * 0.5f;
                var listboxCenterY = windowSize.Y * 0.4f;
                var captionY = listboxCenterY - 390 - 108;
                ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - ImGui.CalcTextSize("SEENG Engine Sounds! 1.0").X * 0.5f, captionY));
                ImGui.TextColored(new System.Numerics.Vector4(1, 1, 1, 1), "SEENG Engine Sounds! 1.0");

                // Listbox
                ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - ImGui.CalcTextSize("Engines list").X * 0.5f, listboxCenterY - 390));
                ImGui.Text("Engines list:");
                var currentPacks = _modManager.AvailablePacks;
                var packList = currentPacks.Keys.ToList();
                packList.Insert(0, "None");
                var listboxSize = new System.Numerics.Vector2(720, 720);
                ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - listboxSize.X * 0.5f, listboxCenterY - listboxSize.Y * 0.5f));
                if (ImGui.BeginListBox("##Packs", listboxSize))
                {
                    try
                    {
                        for (int i = 0; i < packList.Count; i++)
                        {
                            bool isSelected = (_selectedIndex == i);
                            string displayText = i == 0 ? "None" : currentPacks[packList[i]].DisplayName; // DisplayName с [DEBUG]
                            if (ImGui.Selectable(displayText, isSelected))
                            {
                                _selectedIndex = i;
                                _selectedPack = i == 0 ? "" : packList[i];
                                UpdateDescription(i);
                            }
                        }
                    }
                    finally
                    {
                        ImGui.EndListBox();
                    }
                }


                // Apply
                var buttonPosY = listboxCenterY + listboxSize.Y * 0.5f + 30;
                ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - 360, buttonPosY));
                if (ImGui.Button("Refit Engine", new System.Numerics.Vector2(720, 126)))
                {
                    string selectedPack = _selectedIndex > 0 ? packList[_selectedIndex] : "";
                    var refitEngine = new SEENG_im_RefitEngine(_modManager, _logic);
                    var result = refitEngine.HandleRefitClick(selectedPack);

                    if (result.Success)
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), $"Engine '{selectedPack}' instaled");
                    }
                    else
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), result.Message ?? "o do u like kissen bois");
                    }
                }

                // Buttons Config
                var subButtonY = buttonPosY + 143;
                var subButtonWidth = 350f;
                var subButtonHeight = 126f;
                ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - 720 * 0.5f, subButtonY));
                if (ImGui.Button("Sound Volume(WIP)", new System.Numerics.Vector2(subButtonWidth, subButtonHeight)))
                {
                    // sound volume
                }
                ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX + subButtonWidth * 0.5f - subButtonWidth * 0.5f, subButtonY));
                if (ImGui.Button("Set Ship Speed", new System.Numerics.Vector2(subButtonWidth, subButtonHeight)))
                {
                    var cockpit = _sessionChecker.CheckAndUpdateCockpit(out _, out _);
                    if (cockpit != null)
                    {
                        float currentMaxSpeed = SEENG_aConfig.GetCurrentMaxSpeedFromCustomData(cockpit);
                        _speedInputText = (currentMaxSpeed / 1.2f).ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
                        _showSpeedWindow = true;
                    }
                    else
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "No cockpit occupied!");
                    }
                }

                // Description text
                var leftTopX = windowSize.X * 0.15f;
                var leftTopY = windowSize.Y * 0.15f;
                ImGui.SetCursorPos(new System.Numerics.Vector2(leftTopX, leftTopY));
                ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), _descriptionText);

                // Picture panel
                var panelLeftX = windowSize.X * 0.05f;
                var panelLeftY = windowSize.Y * 0.35f;
                ImGui.SetCursorPos(new System.Numerics.Vector2(panelLeftX, panelLeftY));
                var drawList = ImGui.GetWindowDrawList();
                var panelPos = ImGui.GetCursorScreenPos();
                var panelSize = new System.Numerics.Vector2(900, 506);
                drawList.AddRectFilled(panelPos, new System.Numerics.Vector2(panelPos.X + panelSize.X, panelPos.Y + panelSize.Y), 0x80000000, 0, 0);
                ImGui.Dummy(panelSize);

                if (_selectedIndex > 0)
                {
                    string selectedPack = packList[_selectedIndex];
                    string modPath = GetModPathForPack(selectedPack);
                    string thumbPathJpg = Path.Combine(modPath, "SEENG_thumb.jpg");
                    string thumbPathPng = Path.Combine(modPath, "SEENG_thumb.png");
                    string thumbPath = File.Exists(thumbPathJpg) ? thumbPathJpg : File.Exists(thumbPathPng) ? thumbPathPng : "";

                    if (!string.IsNullOrEmpty(thumbPath) && _imageService != null)
                    {
                        try
                        {
                            var img = _imageService.GetFromPath(thumbPath);
                            var size = img.Size;

                            if (size.X > panelSize.X || size.Y > panelSize.Y)
                            {
                                var scale = Math.Min(panelSize.X / size.X, panelSize.Y / size.Y);
                                size = new System.Numerics.Vector2(size.X * scale, size.Y * scale);
                            }

                            ImGui.SetCursorPos(new System.Numerics.Vector2(
                                panelLeftX + (panelSize.X - size.X) * 0.5f,
                                panelLeftY + (panelSize.Y - size.Y) * 0.5f));
                            ImGui.Image(img, size);
                        }
                        catch (Exception ex)
                        {
                            MyLog.Default.WriteLine($"SEENG_ES: Failed to load image {thumbPath}: {ex.Message}");
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                    ImGui.SetCursorPos(new System.Numerics.Vector2(
                        panelLeftX + (panelSize.X - ImGui.CalcTextSize("Related Ships Picture").X) * 0.5f,
                        panelLeftY + (panelSize.Y - ImGui.CalcTextSize("Related Ships Picture").Y) * 0.5f));
                    ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1), "Related Ships Picture");
                }

                // Detailed info text
                var leftBottomY = panelLeftY + panelSize.Y + 45;
                ImGui.SetCursorPos(new System.Numerics.Vector2(leftTopX, leftBottomY));
                ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), _bigDescText);

                // Right window
                var rightTopX = windowSize.X * 0.8f;
                var rightTopY = windowSize.Y * 0.12f;
                ImGui.SetCursorPos(new System.Numerics.Vector2(rightTopX - ImGui.CalcTextSize("Right").X * 0.5f, rightTopY));
                ImGui.TextColored(new System.Numerics.Vector4(1, 1, 1, 1), "Right");
                var rightSubY = rightTopY + 30;
                ImGui.SetCursorPos(new System.Numerics.Vector2(rightTopX - ImGui.CalcTextSize("text").X * 0.5f, rightSubY));
                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1), "text");

                // News box
                var newsBoxX = windowSize.X * 0.64f;
                var newsBoxY = windowSize.Y * 0.35f;
                ImGui.SetCursorPos(new System.Numerics.Vector2(newsBoxX, newsBoxY));
                var newsDrawList = ImGui.GetWindowDrawList();
                var newsPanelPos = ImGui.GetCursorScreenPos();
                var newsPanelSize = new System.Numerics.Vector2(900, 506);
                newsDrawList.AddRectFilled(newsPanelPos, new System.Numerics.Vector2(newsPanelPos.X + newsPanelSize.X, newsPanelPos.Y + newsPanelSize.Y), 0x66000000, 0, 0);
                ImGui.Dummy(newsPanelSize);
                ImGui.SetCursorPos(new System.Numerics.Vector2(newsBoxX + (newsPanelSize.X - ImGui.CalcTextSize("News").X) * 0.5f, newsBoxY + (newsPanelSize.Y - ImGui.CalcTextSize("News").Y) * 0.5f));
                ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1), "News");

                // Right window btns
                var rightButtonY = windowSize.Y * 0.675f;
                var rightButtonX = windowSize.X * 0.672f;
                var buttonWidth = 720f;
                var buttonHeight = 90f;

                ImGui.SetCursorPos(new System.Numerics.Vector2(rightButtonX, rightButtonY));
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.129f, 0.251f, 0.306f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.129f, 0.251f, 0.600f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.443f, 0.6f, 0.635f, 1.0f));
                if (ImGui.Button("Order BigMac(requaiers connection to MacApp)", new System.Numerics.Vector2(buttonWidth, buttonHeight)))
                {
                    MyGuiSandbox.OpenUrlWithFallback("https://youtu.be/dQw4w9WgXcQ", "kks");
                }
                ImGui.PopStyleColor(3);
                rightButtonY += buttonHeight + 30;
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.129f, 0.251f, 0.306f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.129f, 0.251f, 0.600f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.443f, 0.6f, 0.635f, 1.0f));
                ImGui.SetCursorPos(new System.Numerics.Vector2(rightButtonX, rightButtonY));
                if (ImGui.Button("Report a problem/suggestion", new System.Numerics.Vector2(buttonWidth, buttonHeight)))
                {
                    MyGuiSandbox.OpenUrlWithFallback("https://discord.gg/bvkhT6wvDm", "kks");
                }
                ImGui.PopStyleColor(3);
                rightButtonY += buttonHeight + 30;
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.129f, 0.251f, 0.306f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.129f, 0.251f, 0.600f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.443f, 0.6f, 0.635f, 1.0f));
                ImGui.SetCursorPos(new System.Numerics.Vector2(rightButtonX, rightButtonY));
                if (ImGui.Button("WIKI - How to make your own engine", new System.Numerics.Vector2(buttonWidth, buttonHeight)))
                {
                    MyGuiSandbox.OpenUrlWithFallback("https://discord.gg/bvkhT6wvDm", "kks");
                }
                ImGui.PopStyleColor(3);

                // Set Ship Speed
                if (_showSpeedWindow)
                {
                    var displaySizeModal = ImGui.GetIO().DisplaySize;
                    ImGui.SetNextWindowPos(new System.Numerics.Vector2(displaySizeModal.X * 0.5f, displaySizeModal.Y * 0.5f), ImGuiCond.Always, new System.Numerics.Vector2(0.5f, 0.5f));
                    ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 200), ImGuiCond.Once);

                    bool open = true;
                    if (ImGui.Begin("Set Ship Speed", ref open, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
                    {
                        ImGui.SetCursorPosY(ImGui.GetWindowHeight() * 0.3f);
                        ImGui.Text("Enter max speed in m/s:");
                        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 50f);
                        ImGui.InputText("##speedInput", ref _speedInputText, 10);

                        float buttonSizeX = 100f;
                        float buttonSizeY = 30f;
                        float buttonSpacing = 20f;
                        float totalWidth = (buttonSizeX * 2) + buttonSpacing;
                        float startX = (ImGui.GetWindowWidth() - totalWidth) * 0.5f;
                        ImGui.SetCursorPosY(ImGui.GetWindowHeight() * 0.6f);
                        ImGui.SetCursorPosX(startX);

                        if (ImGui.Button("Accept", new System.Numerics.Vector2(buttonSizeX, buttonSizeY)))
                        {
                            if (float.TryParse(_speedInputText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float newSpeed) && newSpeed > 0f)
                            {
                                var cockpit = _sessionChecker.CheckAndUpdateCockpit(out _, out _);
                                if (cockpit != null)
                                {
                                    SEENG_aConfig.UpdateMaxSpeedInCustomData(cockpit, newSpeed);
                                }
                                else
                                {
                                    MyAPIGateway.Utilities.ShowMessage("SEENG_ES", "No cockpit");
                                }
                            }
                            else
                            {
                                MyAPIGateway.Utilities.ShowMessage("SEENG_ES", "Invalid speed value");
                            }
                            _showSpeedWindow = false;
                        }

                        ImGui.SameLine();
                        ImGui.SetCursorPosX(startX + buttonSizeX + buttonSpacing);
                        if (ImGui.Button("Close", new System.Numerics.Vector2(buttonSizeX, buttonSizeY)))
                        {
                            _showSpeedWindow = false;
                        }
                    }
                    ImGui.End();

                    if (!open)
                    {
                        _showSpeedWindow = false;
                    }
                }
            }
            finally
            {
                ImGui.End();
            }
        }

        private void UpdateDescription(int index)
        {
            var packList = _modManager.AvailablePacks.Keys.ToList();
            packList.Insert(0, "None");

            if (index == 0 || index >= packList.Count)
            {
                _descriptionText = "Select an engine...";
                _bigDescText = "Select an engine...";
                return;
            }

            string selectedPack = packList[index];
            string modPath = GetModPathForPack(selectedPack);
            if (string.IsNullOrEmpty(modPath))
            {
                _descriptionText = "No description available.";
                _bigDescText = "No details available.";
                return;
            }

            string descPath = Path.Combine(modPath, "SEENG_desc.txt");
            _descriptionText = File.Exists(descPath) ? File.ReadAllText(descPath).Trim() : $"... {selectedPack}...";

            string bigDescPath = Path.Combine(modPath, "SEENG_descBIG.txt");
            _bigDescText = File.Exists(bigDescPath) ? File.ReadAllText(bigDescPath).Trim() : $"... {selectedPack}...";

        }

        private string GetModPathForPack(string packPrefix)
        {
            if (string.IsNullOrEmpty(packPrefix) || packPrefix == "None") return "";

            if (_modManager._workshopPacks.TryGetValue(packPrefix, out var workshopConfig))
            {
                return workshopConfig.ModPath;
            }
            if (_modManager._debugPacks.TryGetValue(packPrefix, out var debugConfig))
            {
                return debugConfig.ModPath;
            }

            return "";
        }

        public void Dispose()
        {
        }
    }
}
