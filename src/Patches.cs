using System;
using HarmonyLib;
using Restory.Gameplay.EmailSystems;
using Restory.Gameplay.Shops.Devices;
using Restory.TimeSystems;

namespace ReStoryMoreWork;

/// <summary>
/// Hooks the moment the game schedules a new day's orders (EmailService.EnqueueEmailsForTheDay). It runs once per
/// in-game morning, never on loading a save, so extras are added exactly when the game adds its own.
/// </summary>
[HarmonyPatch(typeof(EmailService), "EnqueueEmailsForTheDay")]
internal static class DailyOrdersPatch
{
    internal sealed class State
    {
        internal int UpcomingCountBefore;
        internal bool IsFirstDayOfSave;
        internal int StoryBatchIndexBefore;
    }

    private static void Prefix(EmailService __instance, out State __state)
    {
        __state = null;

        // Never let the mod stop the game's own method from running.
        try
        {
            __state = new State
            {
                UpcomingCountBefore = GameAccess.UpcomingEmails(__instance).Count,
                IsFirstDayOfSave = GameAccess.LastTrackedTime(__instance) == MainDayTimes.None,
                StoryBatchIndexBefore = GameAccess.StoryOrdersBatchIndex(__instance)
            };
        }
        catch (Exception exception)
        {
            Log.Error($"Could not read the inbox before the game's daily orders. {exception}");
        }
    }

    private static void Postfix(EmailService __instance, State __state)
    {
        if (__state == null)
            return;

        try
        {
            // The game sent a scripted story batch today instead of random orders.
            bool isStoryDay = __state.IsFirstDayOfSave ||
                              GameAccess.StoryOrdersBatchIndex(__instance) != __state.StoryBatchIndexBefore;

            Orders.OnGameScheduledTodaysEmails(__instance, __state.UpcomingCountBefore, isStoryDay);
        }
        catch (Exception exception)
        {
            Log.Error($"Adding today's extra orders failed; the game's own orders are unaffected. {exception}");
        }
    }
}

/// <summary>
/// Hooks the moment the game prepares a new day's Marketplace batch (DeviceShopSupplier.SupplyNextBatch). It runs
/// at most once per in-game day.
/// </summary>
[HarmonyPatch(typeof(DeviceShopSupplier), "SupplyNextBatch")]
internal static class DailyListingsPatch
{
    private static void Postfix(DeviceShopSupplier __instance)
    {
        try
        {
            Listings.OnGameSuppliedTodaysBatch(__instance);
        }
        catch (Exception exception)
        {
            Log.Error($"Adding today's extra listings failed; the game's own listings are unaffected. {exception}");
        }
    }
}
