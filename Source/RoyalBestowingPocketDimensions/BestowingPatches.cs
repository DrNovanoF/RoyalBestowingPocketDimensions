using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RoyalBestowingPocketDimensions
{
    internal static class PatchTools
    {
        private static readonly MethodInfo IsPlayerHomeGetter =
            AccessTools.PropertyGetter(typeof(Map), nameof(Map.IsPlayerHome));

        private static readonly MethodInfo Replacement =
            AccessTools.Method(typeof(SupportedPocketMap), nameof(SupportedPocketMap.IsPlayerHomeOrSupportedPocket));

        internal static IEnumerable<CodeInstruction> ReplacePlayerHomeCheck(
            IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            int replacements = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(IsPlayerHomeGetter))
                {
                    replacements++;
                    yield return new CodeInstruction(OpCodes.Call, Replacement).MoveLabelsFrom(instruction);
                }
                else
                {
                    yield return instruction;
                }
            }

            if (replacements != 1)
                Log.Error($"[Royal Bestowing Pocket Dimensions] Expected one IsPlayerHome check in {original}, found {replacements}.");
        }
    }

    [HarmonyPatch(typeof(QuestPart_RequirementsToAcceptPawnOnColonyMap), nameof(QuestPart_RequirementsToAcceptPawnOnColonyMap.CanAccept))]
    internal static class AcceptPawnOnPocketMapPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return PatchTools.ReplacePlayerHomeCheck(instructions, original);
        }
    }

    [HarmonyPatch(typeof(QuestPart_BestowingCeremony), nameof(QuestPart_BestowingCeremony.TryGetCeremonySpot))]
    internal static class CeremonySpotOnPocketMapPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // This replaces only the fallback's IsPlayerHome call. The throne
            // branch contains no such call and remains byte-for-byte vanilla.
            return PatchTools.ReplacePlayerHomeCheck(instructions, original);
        }
    }

    [HarmonyPatch(typeof(ShipJob_Arrive), nameof(ShipJob_Arrive.TryStart))]
    internal static class BestowingShuttleArrivalPatch
    {
        private static readonly MethodInfo Filter =
            AccessTools.Method(typeof(SupportedPocketMap), nameof(SupportedPocketMap.FilterArrivalRedirect));

        private static bool Prefix(ShipJob_Arrive __instance, ref bool __result)
        {
            if (SupportedPocketMap.PrepareBestowingLanding(__instance))
                return true;

            // TryStart(false) keeps this job at the head of TransportShip's queue.
            // It will be retried automatically once a compatible dock is opened.
            __result = false;
            return false;
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase original)
        {
            int replacements = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Isinst && instruction.operand as System.Type == typeof(PocketMapParent))
                {
                    // Stack after isinst: PocketMapParent. Add this ShipJob and
                    // filter the cast only for a tagged Bestowing arrival to DR.
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, Filter);
                    replacements++;
                }
            }

            if (replacements != 1)
                Log.Error($"[Royal Bestowing Pocket Dimensions] Expected one PocketMapParent redirect in {original}, found {replacements}.");
        }
    }
}
