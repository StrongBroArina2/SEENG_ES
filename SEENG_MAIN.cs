using System;
using System.Reflection;
using HarmonyLib;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Plugins;
using VRage.Utils;

namespace SEENG_ES
{
    public class SEENG_ES : IPlugin
    {
        private SLogic _logic;
        private SEENG_modManager _modManager;
        private DIGI_DisableShipSounds _disableSoundsComponent;
        private bool _isInitialized = false;

        public void Init(object gameInstance)
        {
            var harmony = new Harmony("SEENG_ES");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _logic = new SLogic();
            _modManager = new SEENG_modManager();
            _modManager.Init();
            _disableSoundsComponent = new DIGI_DisableShipSounds();
        }


        public void Update()
        {
            try
            {
                if (!_isInitialized)
                {
                    if (_modManager.CurrentPack == "")
                    {
                        _modManager.ScanMods();
                    }
                    if (MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null)
                        return;
                    if (MyAPIGateway.Session != null)
                    {
                        MyAPIGateway.Session.RegisterComponent(_disableSoundsComponent, MyUpdateOrder.NoUpdate, 0);
                    }

                    _isInitialized = true;
                    _logic.Init(_modManager);
                    _modManager.SubscribeToChat(_logic);
                }

                _logic.Update(_modManager);
            }
            catch (Exception e)
            {

            }
        }

        public void Dispose()
        {
            try
            {
                if (MyAPIGateway.Session != null)
                {
                    MyAPIGateway.Session.UnregisterComponent(_disableSoundsComponent);
                }
                _modManager?.UnsubscribeFromChat();
                _modManager?.Dispose();
                _logic?.Dispose();
                _isInitialized = false;
                _disableSoundsComponent = null;
            }
            catch (Exception e)
            {
            }
        }
    }
}