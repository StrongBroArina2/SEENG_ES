using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRage.Utils;
using SEENG_ES;

namespace SEENG_SElauncher.IMGUI
{
    public class SEENG_News
    {
        private readonly SEENG_modManager _modManager;
        private List<string> _imagePaths = new List<string>();
        private int _currentIndex = 0;
        private DateTime _lastSwitchTime = DateTime.MinValue;
        private readonly TimeSpan _switchInterval = TimeSpan.FromSeconds(5);

        public SEENG_News(SEENG_modManager modManager)
        {
            _modManager = modManager;
            RefreshImages();
        }

        public void RefreshImages()
        {
            _imagePaths.Clear();
            if (_modManager.AvailablePacks.TryGetValue("ImprovedVanilla", out var config))
            {
                string newsPath = Path.Combine(config.ModPath, "SEENG_NEWS");
                MyLog.Default.WriteLine($"SEENG_News: Looking in path: {newsPath}");

                if (Directory.Exists(newsPath))
                {
                    var files = Directory.GetFiles(newsPath)
                        .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    _imagePaths.AddRange(files);
                    MyLog.Default.WriteLine($"SEENG_News: Found {files.Count} images.");
                }
                else
                {
                }
            }
            else
            {
            }
        }

        public string GetCurrentImagePath()
        {
            if (_imagePaths.Count == 0) return null;

            if (DateTime.Now - _lastSwitchTime > _switchInterval)
            {
                _currentIndex = (_currentIndex + 1) % _imagePaths.Count;
                _lastSwitchTime = DateTime.Now;
            }

            return _imagePaths[_currentIndex];
        }
    }
}
