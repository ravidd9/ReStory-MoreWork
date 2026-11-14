using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using UnityEngine;

namespace ReStoryMoreWork;

/// <summary>Every setting of the mod, bound to BepInEx\config\restory.morework.cfg.</summary>
internal static class Settings
{
    private const string GeneralSection = "General";
    private const string OrdersSection = "Orders";
    private const string OrderTypesSection = "Orders - Types";
    private const string MarketplaceSection = "Marketplace";
    private const string SectionSeparator = "# ----------------------------------------------------------------";

    internal const int GameWorkDayStartHour = 8;
    internal const int GameWorkDayEndHour = 22;

    internal static ConfigEntry<bool> ShowMessages;

    internal static ConfigEntry<bool> OrdersEnabled;
    internal static KeyCode AddOrderKey;
    internal static ConfigEntry<int> ExtraOrdersPerDay;
    internal static ConfigEntry<int> ArrivalStartHour;
    internal static ConfigEntry<int> ArrivalEndHour;
    internal static ConfigEntry<bool> MoveGameOrdersIntoHours;

    private static readonly Dictionary<OrderKind, ConfigEntry<int>> OrderKindWeights =
        new Dictionary<OrderKind, ConfigEntry<int>>();

    internal static ConfigEntry<bool> MarketplaceEnabled;
    internal static KeyCode RestockKey;
    internal static ConfigEntry<int> RestockAmount;
    internal static ConfigEntry<int> ExtraListingsEachMorning;
    internal static ConfigEntry<int> MaxListings;

    internal static int WeightOf(OrderKind kind)
    {
        return OrderKindWeights.TryGetValue(kind, out ConfigEntry<int> entry) ? Math.Max(0, entry.Value) : 0;
    }

    internal static void Bind(ConfigFile config)
    {
        ShowMessages = config.Bind(GeneralSection, "ShowMessages", true,
            "Shows a short message at the top of the screen when one of this mod's hotkeys does something.");

        BindOrders(config);
        BindOrderTypes(config);
        BindMarketplace(config);

        AddSectionSeparators(config);
    }

    private static void BindOrders(ConfigFile config)
    {
        OrdersEnabled = config.Bind(OrdersSection, "Enabled", true,
            "Turns every Outlink Express order feature of this mod on (true) or off (false).");

        AddOrderKey = BindHotkey(config, OrdersSection, "AddOrderKey", KeyCode.F2,
            "Adds one new order to the Outlink Express inbox right away. Works with the clock paused. "
            + "Hold Shift while pressing it to instead receive, right now, every order the game has already "
            + "scheduled to arrive later today (handy while the clock is paused, because scheduled orders "
            + "only arrive while it runs).");

        ExtraOrdersPerDay = config.Bind(OrdersSection, "ExtraOrdersPerDay", 0, new ConfigDescription(
            "Extra orders the mod schedules every morning, on top of the game's own daily orders. "
            + "They arrive between ArrivalStartHour and ArrivalEndHour. 0 = only the game's own orders.",
            new AcceptableValueRange<int>(0, 30)));

        ArrivalStartHour = config.Bind(OrdersSection, "ArrivalStartHour", GameWorkDayStartHour,
            new ConfigDescription(
                "Earliest in-game hour (24-hour clock) extra daily orders can arrive. The shop opens at 8.",
                new AcceptableValueRange<int>(0, 23)));

        ArrivalEndHour = config.Bind(OrdersSection, "ArrivalEndHour", GameWorkDayEndHour,
            new ConfigDescription(
                "Latest in-game hour (24-hour clock) extra daily orders can arrive. Work ends at 22. "
                + "A window that crosses midnight works too, e.g. ArrivalStartHour 20 and ArrivalEndHour 2. 0 and 24 = any time of day.",
                new AcceptableValueRange<int>(0, 24)));

        MoveGameOrdersIntoHours = config.Bind(OrdersSection, "MoveGameOrdersIntoHours", false,
            "Also moves the game's own daily orders so they arrive between ArrivalStartHour and "
            + "ArrivalEndHour. false = the game's orders keep their normal arrival times.");
    }

    private static void BindOrderTypes(ConfigFile config)
    {
        const string sharedNote = " Relative chance compared to the other types in this section, 0 = never. "
                                  + "Only applies to orders this mod adds (hotkey and ExtraOrdersPerDay); the "
                                  + "game's own orders keep their normal mix. Job types you have not unlocked yet "
                                  + "in the game are skipped.";

        const string cleaningAddedNote = " The game always adds a cleaning task to painting, hacking and sticker "
                                         + "orders, because every order needs a cleaning or repair job.";

        BindWeight(config, OrderKind.Normal, "Normal", 100,
            "The game's usual mix: cleaning and repairs, sometimes painting, stickers or hacking on top.");
        BindWeight(config, OrderKind.CleaningOnly, "CleaningOnly", 0,
            "Cleaning only: one kind of dirt to clean, nothing broken.");
        BindWeight(config, OrderKind.RepairOnly, "RepairOnly", 0,
            "Repair only: broken parts to replace, nothing to clean.");
        BindWeight(config, OrderKind.Painting, "Painting", 0, "A painting job." + cleaningAddedNote);
        BindWeight(config, OrderKind.Hacking, "Hacking", 0, "A hacking (software update) job." + cleaningAddedNote);
        BindWeight(config, OrderKind.Stickers, "Stickers", 0, "A sticker job." + cleaningAddedNote);

        void BindWeight(ConfigFile file, OrderKind kind, string key, int defaultWeight, string purpose)
        {
            OrderKindWeights[kind] = file.Bind(OrderTypesSection, key, defaultWeight, new ConfigDescription(
                purpose + sharedNote, new AcceptableValueRange<int>(0, 100)));
        }
    }

    private static void BindMarketplace(ConfigFile config)
    {
        MarketplaceEnabled = config.Bind(MarketplaceSection, "Enabled", true,
            "Turns every second-hand Marketplace feature of this mod on (true) or off (false).");

        RestockKey = BindHotkey(config, MarketplaceSection, "RestockKey", KeyCode.F3,
            "Adds RestockAmount new second-hand listings right away. Works with the clock paused. Hold Shift "
            + "while pressing it to instead replace every random listing that is not in your cart with new ones.");

        RestockAmount = config.Bind(MarketplaceSection, "RestockAmount", 5, new ConfigDescription(
            "How many listings one press of RestockKey adds.", new AcceptableValueRange<int>(1, 30)));

        ExtraListingsEachMorning = config.Bind(MarketplaceSection, "ExtraListingsEachMorning", 0,
            new ConfigDescription(
                "Extra listings the mod posts every morning, on top of the game's own. 0 = only the game's own.",
                new AcceptableValueRange<int>(0, 30)));

        MaxListings = config.Bind(MarketplaceSection, "MaxListings", 40, new ConfigDescription(
            "The mod never adds a listing while the Marketplace already shows this many devices. Note: the game "
            + "itself skips its own daily batch while 15 or more devices are listed.",
            new AcceptableValueRange<int>(5, 100)));
    }

    private static KeyCode BindHotkey(ConfigFile config, string section, string key, KeyCode defaultKey,
        string purpose)
    {
        ConfigEntry<string> entry = config.Bind(section, key, defaultKey.ToString(),
            $"{purpose} Takes any Unity KeyCode name, e.g. F2, F3, Insert, Keypad5. None turns the hotkey off.");

        bool isKnownKeyName = Enum.TryParse(entry.Value, true, out KeyCode parsed)
                              && Enum.IsDefined(typeof(KeyCode), parsed);

        if (isKnownKeyName)
            return parsed;

        Log.Warning($"{key} \"{entry.Value}\" is not a key name; falling back to {defaultKey}.");

        return defaultKey;
    }

    private static void AddSectionSeparators(ConfigFile config)
    {
        // Cosmetic only: never let a locked or unreadable config file stop the mod from loading.
        try
        {
            InsertSeparators(config.ConfigFilePath);
        }
        catch (Exception exception)
        {
            Log.Warning($"Could not tidy the config file ({exception.Message}); settings still apply.");
        }
    }

    private static void InsertSeparators(string configFilePath)
    {
        if (!File.Exists(configFilePath))
            return;

        List<string> lines = new List<string>(File.ReadAllLines(configFilePath));
        bool hasChanged = false;

        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            bool isSectionHeader = lines[lineIndex].StartsWith("[");
            bool hasSeparatorAbove = lineIndex > 0 && lines[lineIndex - 1] == SectionSeparator;

            if (!isSectionHeader || hasSeparatorAbove)
                continue;

            lines.Insert(lineIndex, SectionSeparator);
            lineIndex++;
            hasChanged = true;
        }

        if (hasChanged)
        {
            File.WriteAllLines(configFilePath, lines);
        }
    }
}
