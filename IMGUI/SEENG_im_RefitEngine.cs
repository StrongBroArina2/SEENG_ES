using Sandbox.Game;
using Sandbox.ModAPI;
using SEENG_ES;
using SEENG_SElauncher.SEENG_CFG_SYS;
using VRage.Audio;

namespace SEENG_SElauncher.IMGUI
{
    public struct RefitResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class SEENG_im_RefitEngine
    {
        private readonly SEENG_modManager _modManager;
        private readonly SLogic _logic;

        public SEENG_im_RefitEngine(SEENG_modManager modManager, SLogic logic)
        {
            _modManager = modManager;
            _logic = logic;
        }

        public RefitResult HandleRefitClick(string selectedPack, Action onSuccessCloseMenu = null)
        {
            MyVisualScriptLogicProvider.PlayHudSound(MyGuiSounds.HudAntennaOn);

            if (string.IsNullOrEmpty(selectedPack) || !_modManager.AvailablePacks.ContainsKey(selectedPack))
            {
                return new RefitResult { Success = false, Message = "Invalid Pack." };
            }

            var cockpit = MyAPIGateway.Session.Player?.Controller?.ControlledEntity as IMyCockpit;
            if (cockpit == null)
            {
                return new RefitResult { Success = false, Message = "Must be in cockpit!" };
            }

            SEENG_aConfig.UpdatePackPrefixInCustomData(cockpit, selectedPack);
            _modManager.CurrentPackConfig = _modManager.AvailablePacks[selectedPack];
            _logic.RestartSoundsWithNewPack(_modManager, selectedPack);
            onSuccessCloseMenu?.Invoke();

            return new RefitResult
            {
                Success = true,
                Message = $"Addon '{selectedPack}' applied to ship!"
            };
        }
    }
}