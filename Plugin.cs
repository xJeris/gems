using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ErenshorGems
{
    [BepInPlugin("com.erenshor.gems", "Erenshor Gems", "0.3.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance;
        internal static BepInEx.Logging.ManualLogSource Log;

        internal static ConfigEntry<float> WindowX;
        internal static ConfigEntry<float> WindowY;

        private Harmony _harmony;
        private GemsWindow _window;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            WindowX = Config.Bind("Window", "X", -1f, "Window X position (-1 = center)");
            WindowY = Config.Bind("Window", "Y", -1f, "Window Y position (-1 = center)");

            _window = new GemsWindow();

            _harmony = new Harmony("com.erenshor.gems");
            _harmony.PatchAll();

            Logger.LogInfo("Erenshor Gems loaded. Type /icons to play!");
        }

        private void Update()
        {
            if (_window != null && _window.IsVisible)
            {
                _window.GameTick();
            }
        }

        private void OnGUI()
        {
            if (_window != null && _window.IsVisible)
            {
                _window.Draw();
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        internal void ToggleWindow()
        {
            if (_window == null)
                _window = new GemsWindow();

            _window.Toggle();
        }

        internal bool IsWindowVisible => _window != null && _window.IsVisible;
    }
}
