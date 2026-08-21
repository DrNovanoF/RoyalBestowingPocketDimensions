using HarmonyLib;
using Verse;

namespace RoyalBestowingPocketDimensions
{
    [StaticConstructorOnStartup]
    internal static class ModEntry
    {
        static ModEntry()
        {
            new Harmony("local.royalbestowing.pocketdimensions").PatchAll();
            Log.Message("[Royal Bestowing Pocket Dimensions] Patches loaded.");
        }
    }
}
