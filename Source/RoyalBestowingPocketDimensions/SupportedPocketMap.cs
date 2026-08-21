using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RoyalBestowingPocketDimensions
{
    /// <summary>
    /// The only place that knows about Dimensions RePocketed. This deliberately
    /// uses vanilla base types plus stable DR type/def names, so DR is not a hard
    /// assembly reference and the compatibility rule is easy to update.
    /// </summary>
    internal static class SupportedPocketMap
    {
        private const string DrParentType = "KB_PocketDimension.MapParent_PocketDimension";
        private const string DrParentDef = "KB_WorldObject_PocketDimension";
        private const string DrGeneratorDef = "KB_PocketDimensionMapGenerator";
        private const string BestowingQuestTag = "Bestowing";
        private const string ShuttleDockPackageId = "Salvador143.ShuttleDock";
        private const string ShuttleDockType = "MThings.ShuttleDock.Building_ShuttleDock";

        private static readonly string[] ShuttleDockDefs =
        {
            "MThings_5x5ShuttleDock",
            "MThings_7x7ShuttleDock",
            "MThings_9x9ShuttleDock",
            "MThings_11x11ShuttleDock"
        };

        private static PropertyInfo shuttleDockRoofOpen;

        internal static bool IsPlayerHomeOrSupportedPocket(Map map)
        {
            return map != null && (map.IsPlayerHome || IsSupportedPlayerPocket(map));
        }

        internal static bool IsSupportedPlayerPocket(Map map)
        {
            if (map == null || !map.IsPocketMap || !(map.Parent is PocketMapParent))
                return false;

            bool isDimensionsRePocketed =
                map.Parent.GetType().FullName == DrParentType &&
                map.Parent.def != null && map.Parent.def.defName == DrParentDef &&
                map.generatorDef != null && map.generatorDef.defName == DrGeneratorDef;

            // A DR pocket becomes eligible only while it actually contains a
            // player pawn. For bestowing this is the title holder, preventing the
            // helper from turning arbitrary/non-player pocket maps into colonies.
            return isDimensionsRePocketed &&
                   map.mapPawns.AllPawnsSpawned.Any(p => p.Faction == Faction.OfPlayer);
        }

        internal static PocketMapParent FilterArrivalRedirect(
            PocketMapParent candidate,
            ShipJob_Arrive job)
        {
            if (candidate == null || job == null || !IsBestowingArrival(job))
                return candidate;

            Map targetMap = job.mapOfPawn?.MapHeld;
            if (targetMap != null && targetMap.Parent == candidate && IsSupportedPlayerPocket(targetMap))
            {
                Log.Message("[Royal Bestowing Pocket Dimensions] Bestowing shuttle redirected to the target pawn's pocket map.");
                return null; // Makes only vanilla's PocketMapParent redirect test fail.
            }

            return candidate;
        }

        /// <summary>
        /// When Shuttle Dock is installed, Bestowing arrivals in DR must wait for
        /// an open dock. Returning false from TryStart leaves the job queued;
        /// TransportShip retries it every tick, so opening the roof releases it.
        /// </summary>
        internal static bool PrepareBestowingLanding(ShipJob_Arrive job)
        {
            if (job == null || !IsBestowingArrival(job))
                return true;

            Map targetMap = job.mapOfPawn?.MapHeld;
            if (!IsSupportedPlayerPocket(targetMap) || !ModsConfig.IsActive(ShuttleDockPackageId))
                return true;

            if (!TryFindOpenShuttleDock(job, targetMap, out IntVec3 landingCell))
                return false;

            job.cell = landingCell;
            return true;
        }

        private static bool TryFindOpenShuttleDock(ShipJob_Arrive job, Map map, out IntVec3 landingCell)
        {
            IntVec2 shuttleSize = job.transportShip?.shipThing?.def?.size ?? ThingDefOf.Shuttle.size;

            foreach (string defName in ShuttleDockDefs)
            {
                ThingDef dockDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (dockDef == null || dockDef.size.x < shuttleSize.x || dockDef.size.z < shuttleSize.z)
                    continue;

                foreach (Thing dock in map.listerThings.ThingsOfDef(dockDef))
                {
                    if (dock == null || !dock.Spawned || dock.GetType().FullName != ShuttleDockType || !IsDockRoofOpen(dock))
                        continue;

                    CellRect shuttleRect = GenAdj.OccupiedRect(dock.Position, Rot4.North, shuttleSize);
                    if (shuttleRect.Cells.All(cell => cell.InBounds(map) && !cell.Roofed(map)))
                    {
                        landingCell = dock.Position;
                        return true;
                    }
                }
            }

            landingCell = IntVec3.Invalid;
            return false;
        }

        private static bool IsDockRoofOpen(Thing dock)
        {
            if (shuttleDockRoofOpen == null || shuttleDockRoofOpen.DeclaringType != dock.GetType())
                shuttleDockRoofOpen = AccessTools.Property(dock.GetType(), "RoofOpen");

            return shuttleDockRoofOpen != null &&
                   shuttleDockRoofOpen.PropertyType == typeof(bool) &&
                   (bool)shuttleDockRoofOpen.GetValue(dock, null);
        }

        internal static bool IsBestowingArrival(ShipJob_Arrive job)
        {
            // Vanilla adds the same hard-coded quest tag to both the transport
            // ship and the title holder. Checking both survives job creation or
            // serialization paths where one of the tag lists is unavailable.
            return HasBestowingTag(job.transportShip?.questTags) ||
                   HasBestowingTag(job.mapOfPawn?.questTags);
        }

        private static bool HasBestowingTag(System.Collections.Generic.List<string> tags)
        {
            return tags != null && tags.Any(tag =>
                tag != null &&
                (tag.Equals(BestowingQuestTag, StringComparison.Ordinal) ||
                 tag.EndsWith("." + BestowingQuestTag, StringComparison.Ordinal)));
        }
    }
}
