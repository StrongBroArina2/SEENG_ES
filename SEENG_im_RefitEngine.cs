using Sandbox.Game;
using VRage.Audio;

namespace SEENG_ES
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

        public RefitResult HandleRefitClick(string selectedPack)
        {
            MyVisualScriptLogicProvider.PlayHudSound(MyGuiSounds.HudAntennaOn);

            if (string.IsNullOrEmpty(selectedPack) || !_modManager.AvailablePacks.ContainsKey(selectedPack))
            {
                return new RefitResult
                {
                    Success = false,
                    Message = "o do u like kissen bois."
                };
            }

            _modManager.CurrentPackConfig = _modManager.AvailablePacks[selectedPack];
            _logic.RestartSoundsWithNewPack(_modManager, selectedPack);

            return new RefitResult
            {
                Success = true,
                Message = $"Addon '{selectedPack}' applied."
            };
        }
    }
}