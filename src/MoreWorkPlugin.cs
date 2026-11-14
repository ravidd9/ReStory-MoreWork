using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ReStoryMoreWork;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class MoreWorkPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "restory.morework";
    internal const string PluginName = "ReStory More Work";
    internal const string PluginVersion = "1.0.0";

    private void Awake()
    {
        Log.Source = Logger;

        Settings.Bind(Config);

        // A game update that renamed something this mod relies on turns the whole mod off, rather than half-working
        // on top of a game it no longer understands.
        _isCompatible = GameAccess.Missing.Count == 0;
        int failedPatches = _isCompatible ? ApplyPatches(new Harmony(PluginGuid)) : 0;

        RunSelfCheck(failedPatches);
    }

    private static bool _isCompatible;

    private void Update()
    {
        try
        {
            HandleHotkeys();
        }
        catch (Exception exception)
        {
            Log.Error($"Hotkey failed: {exception}");
            Toast.Show("More Work: that did not work. See LogOutput.log.");
        }
    }

    private void OnGUI()
    {
        Toast.Draw();
    }

    private static void HandleHotkeys()
    {
        if (!_isCompatible || IsTypingInTextField())
            return;

        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Settings.OrdersEnabled.Value && Settings.AddOrderKey != KeyCode.None &&
            Input.GetKeyDown(Settings.AddOrderKey))
        {
            if (isShiftHeld)
            {
                Orders.ReceiveScheduledOrdersNow();
            }
            else
            {
                Orders.AddOrderNow();
            }
        }

        if (Settings.MarketplaceEnabled.Value && Settings.RestockKey != KeyCode.None &&
            Input.GetKeyDown(Settings.RestockKey))
        {
            if (isShiftHeld)
            {
                Listings.Refresh();
            }
            else
            {
                Listings.Restock();
            }
        }
    }

    /// <summary>True while a text box (e.g. a search field on the in-game PC) has the keyboard.</summary>
    private static bool IsTypingInTextField()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        if (selected == null)
            return false;

        TMP_InputField tmpField = selected.GetComponent<TMP_InputField>();

        if (tmpField != null && tmpField.isFocused)
            return true;

        UnityEngine.UI.InputField legacyField = selected.GetComponent<UnityEngine.UI.InputField>();

        return legacyField != null && legacyField.isFocused;
    }

    // Patching class by class keeps one feature a game update broke from taking the others down with it.
    private static int ApplyPatches(Harmony harmony)
    {
        int failed = 0;

        foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(MoreWorkPlugin).Assembly))
        {
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception exception)
            {
                failed++;
                Log.Error($"{type.Name} is disabled: it no longer matches the game, most likely after a game "
                          + $"update. {exception.GetBaseException().Message}");
            }
        }

        return failed;
    }

    private static void RunSelfCheck(int failedPatches)
    {
        IReadOnlyList<string> missing = GameAccess.Missing;

        if (missing.Count == 0 && failedPatches == 0)
        {
            Log.Info($"Loaded. Self-check passed. Orders: {Describe(Settings.OrdersEnabled.Value)}, key "
                     + $"{Settings.AddOrderKey}, {Settings.ExtraOrdersPerDay.Value} extra/day between "
                     + $"{Settings.ArrivalStartHour.Value}:00 and {Settings.ArrivalEndHour.Value}:00. Marketplace: "
                     + $"{Describe(Settings.MarketplaceEnabled.Value)}, key {Settings.RestockKey}, "
                     + $"{Settings.RestockAmount.Value} per restock, {Settings.ExtraListingsEachMorning.Value} "
                     + "extra each morning.");
            return;
        }

        Log.Error("Loaded. SELF-CHECK FAILED. "
                  + (missing.Count > 0
                      ? $"The game changed since this mod was built, so the mod turned itself off. Missing: {string.Join(", ", missing)}."
                      : $"{failedPatches} daily hook(s) could not be installed; the hotkeys still work."));
    }

    private static string Describe(bool isEnabled)
    {
        return isEnabled ? "on" : "off";
    }
}
