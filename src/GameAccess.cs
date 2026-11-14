using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Restory.Data.Email;
using Restory.Gameplay.Devices;
using Restory.Gameplay.EmailSystems;
using Restory.Gameplay.Licenses;
using Restory.Gameplay.Shops;
using Restory.Gameplay.Shops.Devices;
using Restory.Gameplay.TimeSystems;
using Restory.Gameplay.WorkOrders.EmailOrders;
using Restory.TimeSystems;
using Restory.UI.Presenters.Shops.Devices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReStoryMoreWork;

/// <summary>
/// The game services this mod drives, and the private parts of them it needs. Every private member is looked up
/// by name once, so a game update that renames one shows up as a clear self-check failure at startup instead of
/// a crash mid-game.
/// </summary>
internal static class GameAccess
{
    private static readonly List<string> MissingMembers = new List<string>();

    // EmailService: the Outlink Express inbox.
    internal static readonly AccessTools.FieldRef<EmailService, List<EmailLetterRecordInFolder>> UpcomingEmails =
        Field<EmailService, List<EmailLetterRecordInFolder>>("upcomingEmails");

    internal static readonly AccessTools.FieldRef<EmailService, List<EmailLetterRecordInFolder>> ReceivedEmails =
        Field<EmailService, List<EmailLetterRecordInFolder>>("receivedEmails");

    internal static readonly AccessTools.FieldRef<EmailService, MainDayTimes> LastTrackedTime =
        Field<EmailService, MainDayTimes>("lastTrackedTime");

    internal static readonly AccessTools.FieldRef<EmailService, int> StoryOrdersBatchIndex =
        Field<EmailService, int>("lastInitialEmailsBatchIndex");

    internal static readonly AccessTools.FieldRef<EmailService, GameCalendar> EmailCalendar =
        Field<EmailService, GameCalendar>("gameCalendar");

    internal static readonly AccessTools.FieldRef<EmailService, RandomDevicesGenerationService> OrderDevices =
        Field<EmailService, RandomDevicesGenerationService>("randomDevicesGenerator");

    internal static readonly AccessTools.FieldRef<EmailService, EmailNamesService> EmailNames =
        Field<EmailService, EmailNamesService>("emailNamesProvider");

    internal static readonly AccessTools.FieldRef<EmailService, EmailCommentsService> EmailComments =
        Field<EmailService, EmailCommentsService>("emailCommentsService");

    internal static readonly AccessTools.FieldRef<EmailService, DevicePriceEstimationService> PriceEstimation =
        Field<EmailService, DevicePriceEstimationService>("devicePriceEstimationService");

    internal static readonly AccessTools.FieldRef<EmailService, AvailableDevicesWorkTypesTrackingService> WorkTypes =
        Field<EmailService, AvailableDevicesWorkTypesTrackingService>("availableWorkTypesService");

    private static readonly AccessTools.FieldRef<EmailService, Action> NewEmailReceivedEvent =
        Field<EmailService, Action>("OnNewEmailReceived");

    private static readonly AccessTools.FieldRef<EmailService, Action> ContentsChangedEvent =
        Field<EmailService, Action>("OnContentsChanged");

    // DeviceShopSupplier: the second-hand Marketplace.
    internal static readonly AccessTools.FieldRef<DeviceShopSupplier, ShopsService> Shops =
        Field<DeviceShopSupplier, ShopsService>("shopsService");

    internal static readonly AccessTools.FieldRef<DeviceShopSupplier, DeviceShopRandomDevicesGenerationService>
        ListingGenerator = Field<DeviceShopSupplier, DeviceShopRandomDevicesGenerationService>("randomDevicesGenerator");

    internal static readonly AccessTools.FieldRef<DeviceShopSupplier, DeviceShoppingCart> ShoppingCart =
        Field<DeviceShopSupplier, DeviceShoppingCart>("shoppingCart");

    internal static readonly AccessTools.FieldRef<DeviceShopRandomDevicesGenerationService, LicensesService>
        Licenses = Field<DeviceShopRandomDevicesGenerationService, LicensesService>("licensesService");

    internal static readonly AccessTools.FieldRef<DeviceShopRandomDevicesGenerationService,
            AvailableDevicesListTrackingService>
        AvailableDevices =
            Field<DeviceShopRandomDevicesGenerationService, AvailableDevicesListTrackingService>(
                "availableDevicesListTracker");

    // GUI_DeviceShopPanel: the Marketplace page on the in-game PC, refreshed when it is open.
    private static readonly AccessTools.FieldRef<GUI_DeviceShopPanel, bool> PanelIsShown =
        Field<GUI_DeviceShopPanel, bool>("isSubscribed");

    private static EmailService _emailService;
    private static DeviceShopSupplier _shopSupplier;

    /// <summary>The inbox of the loaded save, or null on the main menu.</summary>
    internal static EmailService EmailService =>
        _emailService != null ? _emailService : _emailService = Object.FindAnyObjectByType<EmailService>();

    /// <summary>The Marketplace of the loaded save, or null on the main menu.</summary>
    internal static DeviceShopSupplier ShopSupplier =>
        _shopSupplier != null ? _shopSupplier : _shopSupplier = Object.FindAnyObjectByType<DeviceShopSupplier>();

    internal static IReadOnlyList<string> Missing => MissingMembers;

    /// <summary>Tells the inbox and the PC's mail client that new mail is in, the same way the game does.</summary>
    internal static void RaiseNewEmailReceived(EmailService emailService)
    {
        NewEmailReceivedEvent(emailService)?.Invoke();
        ContentsChangedEvent(emailService)?.Invoke();
    }

    /// <summary>
    /// Redraws the Marketplace page if it is open on the PC right now, by closing and reopening it the way the game
    /// does when you switch to it. That also closes an opened listing card, whose list entry gets reused, and
    /// rebuilds the category filters. Does nothing when the page isn't open.
    /// </summary>
    internal static void RefreshOpenMarketplacePage()
    {
        GUI_DeviceShopPanel panel = Object.FindAnyObjectByType<GUI_DeviceShopPanel>();

        if (panel == null || !PanelIsShown(panel))
            return;

        panel.Hide();
        panel.Show();
    }

    private static AccessTools.FieldRef<TObject, TField> Field<TObject, TField>(string name)
    {
        FieldInfo field = AccessTools.Field(typeof(TObject), name);

        if (field == null || field.FieldType != typeof(TField))
        {
            MissingMembers.Add($"{typeof(TObject).Name}.{name}");
            return null;
        }

        return AccessTools.FieldRefAccess<TObject, TField>(field);
    }
}
