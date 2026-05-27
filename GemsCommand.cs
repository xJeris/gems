using HarmonyLib;
using UnityEngine.UI;

namespace ErenshorGems
{
    [HarmonyPatch(typeof(TypeText), "CheckCommands")]
    public static class GemsCommand
    {
        static bool Prefix(TypeText __instance)
        {
            Text typed = __instance.typed;
            if (typed == null || string.IsNullOrEmpty(typed.text))
                return true;

            string cmd = typed.text.Trim().ToLower();
            if (cmd == "/icons")
            {
                Plugin.Instance.ToggleWindow();
                return false; // skip original CheckCommands
            }

            return true; // let original handle it
        }
    }
}
