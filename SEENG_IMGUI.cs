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
        private readonly IImGuiImageService _imageService = MySandboxGame.Services.GetRequiredService<IImGuiImageService>();
        private readonly ConfigHandler _configHandler = MySandboxGame.Services.GetRequiredService<ConfigHandler>();

        public SEENGRenderComponent(SEENG_modManager modManager, SLogic logic, IImGuiImageService imageService)
        {
            _modManager = modManager;
            _logic = logic;
            _imageService = imageService;
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
                }
            }

            if (!_showMenu) return;

            // Main Back
            var displaySize = ImGui.GetIO().DisplaySize;
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(displaySize.X * 0.5f, displaySize.Y * 0.5f), ImGuiCond.FirstUseEver, new System.Numerics.Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(displaySize.X * 0.75f, displaySize.Y * 0.75f), ImGuiCond.Once);
            ImGui.Begin("SEENG Engine Sounds", ref _showMenu, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground);

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

            // Welcome welcome...
            var listboxCenterX = windowSize.X * 0.5f;
            var listboxCenterY = windowSize.Y * 0.4f;
            var captionY = listboxCenterY - 390 - 108;
            ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - ImGui.CalcTextSize("SEENG Engine Sounds! 1.0").X * 0.5f, captionY));
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 1, 1), "SEENG Engine Sounds! 1.0");

            // Listbox
            ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - ImGui.CalcTextSize("Engines list").X * 0.5f, listboxCenterY - 390));
            ImGui.Text("Engines list:");
            var packList = _modManager._availablePacks.Keys.ToList();
            packList.Insert(0, "None");
            var listboxSize = new System.Numerics.Vector2(720, 720);
            ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - listboxSize.X * 0.5f, listboxCenterY - listboxSize.Y * 0.5f));
            if (ImGui.BeginListBox("##KYS", listboxSize))
            {
                for (int i = 0; i < packList.Count; i++)
                {
                    bool isSelected = (_selectedIndex == i);
                    if (ImGui.Selectable(packList[i], isSelected))
                    {
                        _selectedIndex = i;
                        UpdateDescription(i);
                    }
                }
                ImGui.EndListBox();
            }

            // Apply
            var buttonPosY = listboxCenterY + listboxSize.Y * 0.5f + 30;
            ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - 360, buttonPosY));
            if (ImGui.Button("Refit Engine", new System.Numerics.Vector2(720, 126)))
            {
                MyVisualScriptLogicProvider.PlayHudSound(VRage.Audio.MyGuiSounds.HudAntennaOn);
                string selectedPack = _selectedIndex > 0 ? packList[_selectedIndex] : "";
                if (!string.IsNullOrEmpty(selectedPack) && _modManager._availablePacks.ContainsKey(selectedPack))
                {
                    _modManager.SetCurrentPack(selectedPack);
                    _logic.RestartSoundsWithNewPack(_modManager, selectedPack);
                    ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), $"Engine '{selectedPack}' instaled");
                }
                else
                {
                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "o do u like kissen bois");
                }
            }

            // Placeholder buttons
            var subButtonY = buttonPosY + 143;
            var subButtonWidth = 350f;
            var subButtonHeight = 126f;
            ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX - 720 * 0.5f, subButtonY));
            if (ImGui.Button("Sound Volume", new System.Numerics.Vector2(subButtonWidth, subButtonHeight)))
            {
            }
            ImGui.SetCursorPos(new System.Numerics.Vector2(listboxCenterX + subButtonWidth * 0.5f - subButtonWidth * 0.5f, subButtonY));
            if (ImGui.Button("Set Ship Speed", new System.Numerics.Vector2(subButtonWidth, subButtonHeight)))
            {
            }

            // Description text
            var leftTopX = windowSize.X * 0.15f;
            var leftTopY = windowSize.Y * 0.15f;
            ImGui.SetCursorPos(new System.Numerics.Vector2(leftTopX, leftTopY));
            ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), _descriptionText);

            // picture panel
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
                string thumbPath = Path.Combine(GetModPathForPack(selectedPack), "SEENG_thumb.jpg");
                if (File.Exists(thumbPath))
                {
                    var img = _imageService.GetFromPath(thumbPath);
                }
            }
            else
            {
                // Placeholder text
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

            // Right window buttons
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
                MyGuiSandbox.OpenUrlWithFallback("https://steamcommunity.com/workshop/filedetails/?id=12345", "kks");
            }
            ImGui.PopStyleColor(3);
            rightButtonY += buttonHeight + 30;
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.129f, 0.251f, 0.306f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.129f, 0.251f, 0.600f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.443f, 0.6f, 0.635f, 1.0f));
            ImGui.SetCursorPos(new System.Numerics.Vector2(rightButtonX, rightButtonY));
            if (ImGui.Button("Report a problem/suggestion", new System.Numerics.Vector2(buttonWidth, buttonHeight)))
            {
                MyGuiSandbox.OpenUrlWithFallback("https://steamcommunity.com/workshop/filedetails/?id=12345", "kks");
            }
            ImGui.PopStyleColor(3);
            rightButtonY += buttonHeight + 30;
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.129f, 0.251f, 0.306f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.129f, 0.251f, 0.600f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.443f, 0.6f, 0.635f, 1.0f));
            ImGui.SetCursorPos(new System.Numerics.Vector2(rightButtonX, rightButtonY));
            if (ImGui.Button("WIKI - How to make your own engine", new System.Numerics.Vector2(buttonWidth, buttonHeight)))
            {
                MyGuiSandbox.OpenUrlWithFallback("https://steamcommunity.com/workshop/filedetails/?id=12345", "kks");
            }
            ImGui.PopStyleColor(3);

            ImGui.End();
        }

        // Update description text
        private void UpdateDescription(int index)
        {
            var packList = _modManager._availablePacks.Keys.ToList();
            packList.Insert(0, "None");

            if (index == 0)
            {
                _descriptionText = "Select an engine...";
                _bigDescText = "Select an engine for details...";
                return;
            }

            string selectedPack = packList[index];
            string modPath = GetModPathForPack(selectedPack);
            string descPath = Path.Combine(modPath, "SEENG_desc.txt");
            _descriptionText = File.Exists(descPath) ? File.ReadAllText(descPath).Trim() : $"... {selectedPack}...";

            string bigDescPath = Path.Combine(modPath, "SEENG_descBIG.txt");
            _bigDescText = File.Exists(bigDescPath) ? File.ReadAllText(bigDescPath).Trim() : $"... {selectedPack}...";
        }

        // mod path
        private string GetModPathForPack(string packPrefix)
        {
            if (string.IsNullOrEmpty(packPrefix) || packPrefix == "VNTR") return "";

            if (_modManager._availablePacks.TryGetValue(packPrefix, out var config))
            {
                return config.ModPath;
            }

            return "";
        }
        public void Dispose()
        {
        }
    }
}