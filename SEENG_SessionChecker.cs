using System;
using Sandbox.ModAPI;
using System.Globalization;

namespace SEENG_ES
{
    public class SessionChecker
    {
        private IMyCockpit _currentCockpit;
        public IMyCockpit CurrentCockpit => _currentCockpit;
        private int _debugCounter = 0;
        private const int DEBUG_INTERVAL = 60;
        private bool _debugEnabled = true;

        public IMyCockpit CheckAndUpdateCockpit(out bool hasSEENGTag, out bool isOccupied)
        {
            hasSEENGTag = false;
            isOccupied = false;

            var localPlayer = MyAPIGateway.Session?.Player;
            if (localPlayer == null)
            {
                if (_debugEnabled && _debugCounter % DEBUG_INTERVAL == 0)
                {

                }
                if (_currentCockpit != null)
                {
                    HandleCockpitChange(_currentCockpit, null);
                    _currentCockpit = null;
                }
                _debugCounter++;
                return null;
            }

            var controller = localPlayer.Controller;
            if (controller == null)
            {
                if (_debugEnabled && _debugCounter % DEBUG_INTERVAL == 0)
                {
 
                }
                if (_currentCockpit != null)
                {
                    HandleCockpitChange(_currentCockpit, null);
                    _currentCockpit = null;
                }
                _debugCounter++;
                return null;
            }

            var controlledEntity = controller.ControlledEntity;
            if (controlledEntity == null)
            {
                if (_currentCockpit != null)
                {
                    HandleCockpitChange(_currentCockpit, null);
                    _currentCockpit = null;
                }
                _debugCounter++;
                return null;
            }

            var cockpit = controlledEntity as IMyCockpit;
            if (cockpit == null)
            {
                if (_debugEnabled && _debugCounter % DEBUG_INTERVAL == 0)
                {

                }
                if (_currentCockpit != null)
                {
                    HandleCockpitChange(_currentCockpit, null);
                    _currentCockpit = null;
                }
                _debugCounter++;
                return null;
            }

            if (cockpit != _currentCockpit)
            {
                HandleCockpitChange(_currentCockpit, cockpit);
                _currentCockpit = cockpit;
            }

            string name = cockpit.DisplayNameText ?? "Unnamed";
            hasSEENGTag = name.Contains("[SEENG]");
            isOccupied = cockpit.IsOccupied;

            if (_debugEnabled && _debugCounter % DEBUG_INTERVAL == 0)
            {
                string controllerType = controller.GetType().Name;
                string controlledType = controlledEntity.GetType().Name;
            }

            _debugCounter++;
            return cockpit;
        }

        private void HandleCockpitChange(IMyCockpit oldCockpit, IMyCockpit newCockpit)
        {
            string oldName = oldCockpit?.DisplayNameText ?? "None";
            string newName = newCockpit?.DisplayNameText ?? "None";
            if (newCockpit != null)
            {
                float maxSpeed = ParseMaxSpeedFromCustomData(newCockpit);
            }
        }

        public static float ParseMaxSpeedFromCustomData(IMyCockpit cockpit)
        {
            if (cockpit == null) return 120f;

            string customData = cockpit.CustomData;
            if (string.IsNullOrEmpty(customData)) return 120f;

            string[] lines = customData.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("seeng_maxspeed", StringComparison.OrdinalIgnoreCase))
                {
                    string valueStr = trimmed.Substring("seeng_maxspeed".Length).Trim();
                    if (valueStr.StartsWith("="))
                    {
                        valueStr = valueStr.Substring(1).Trim();
                        if (float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value > 0f)
                        {
                            float effectiveMax = value * 1.2f;
                            return effectiveMax;
                        }
                    }
                }
            }
            return 120f;
        }

        public bool HasSEENGTag(IMyCockpit cockpit)
        {
            return (cockpit.DisplayNameText ?? "").Contains("[SEENG]");
        }

        public void Reset()
        {
            _currentCockpit = null;
            _debugCounter = 0;
        }
    }
}