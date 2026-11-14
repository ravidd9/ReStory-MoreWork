using System;
using System.Collections.Generic;
using System.Linq;
using Restory.Data.Devices;
using Restory.Data.Devices.DeviceWorkTypes;
using Restory.Data.Elements.Condition;
using Restory.Data.Email;
using Restory.Gameplay.Elements;
using Restory.Gameplay.EmailSystems;
using Restory.Gameplay.TimeSystems;
using Restory.Gameplay.WorkOrders.EmailOrders;
using Random = UnityEngine.Random;

namespace ReStoryMoreWork;

/// <summary>
/// Makes Outlink Express orders with the game's own order generator, prices, sender names, comments and deadline,
/// and puts them in the game's own inbox lists. Nothing mod-specific is stored on an order, so the game saves,
/// loads, pays and rates them exactly like its own, with or without the mod installed.
/// </summary>
internal static class Orders
{
    private const int MaxDevicesPerOrder = 3;
    private const int MaxRollsPerDevice = 8;

    // The game stamps its first morning order up to one mail check (5 minutes) before the day starts.
    private static readonly TimeSpan TodayGrace = TimeSpan.FromMinutes(10);

    /// <summary>Adds one order to the inbox right now. Works with the clock paused.</summary>
    internal static void AddOrderNow()
    {
        EmailService emailService = GameAccess.EmailService;

        if (emailService == null)
        {
            Toast.Show("Load a save first.");
            return;
        }

        GameCalendar calendar = GameAccess.EmailCalendar(emailService);
        HashSet<DeviceInfo> todaysDevices = DevicesInTodaysOrders(emailService, calendar);

        if (!TryMakeOrder(emailService, PickKind(emailService), calendar.CurrentDateTime - TimeSpan.FromSeconds(1),
                todaysDevices, out EmailLetterOrderRecord order, out OrderKind madeKind))
        {
            Toast.Show("No order could be made: no licensed device fits. See LogOutput.log.");
            return;
        }

        GameAccess.ReceivedEmails(emailService).Add(new EmailLetterRecordInFolder
        {
            Email = order,
            Folder = EmailFolders.OrdersInbox
        });

        GameAccess.RaiseNewEmailReceived(emailService);

        Log.Info($"Added order now: {Describe(order, madeKind)}.");
        Toast.Show($"New order in the inbox: {order.DeviceCondition.DeviceInfo.ID} ({madeKind}, {order.Payment}).");
    }

    /// <summary>
    /// Receives, right now, every order already scheduled to arrive later today. Scheduled mail only arrives while
    /// the clock runs, so this is how to get today's orders while it is paused.
    /// </summary>
    internal static void ReceiveScheduledOrdersNow()
    {
        EmailService emailService = GameAccess.EmailService;

        if (emailService == null)
        {
            Toast.Show("Load a save first.");
            return;
        }

        DateTime now = GameAccess.EmailCalendar(emailService).CurrentDateTime;
        List<EmailLetterRecordInFolder> upcoming = GameAccess.UpcomingEmails(emailService);

        List<EmailLetterRecordInFolder> scheduledOrders = upcoming
            .Where(entry => entry.Folder == EmailFolders.OrdersInbox && entry.Email is EmailLetterOrderRecord)
            .OrderBy(entry => entry.Email.ReceivedDateTime)
            .ToList();

        if (scheduledOrders.Count == 0)
        {
            Toast.Show("No orders are scheduled for later today.");
            return;
        }

        List<EmailLetterRecordInFolder> received = GameAccess.ReceivedEmails(emailService);

        for (int index = 0; index < scheduledOrders.Count; index++)
        {
            EmailLetterRecordInFolder entry = scheduledOrders[index];
            EmailLetterOrderRecord order = (EmailLetterOrderRecord)entry.Email;

            // Keep their order in the inbox, one second apart, all just before "now".
            DateTime arrival = now - TimeSpan.FromSeconds(scheduledOrders.Count - index);

            if (order.ReceivedDateTime > arrival)
            {
                order.ReceivedDateTime = arrival;
            }

            upcoming.Remove(entry);
            received.Add(new EmailLetterRecordInFolder { Email = entry.Email, Folder = entry.Folder });
        }

        GameAccess.RaiseNewEmailReceived(emailService);

        Log.Info($"Received {scheduledOrders.Count} scheduled order(s) now.");
        Toast.Show($"Received {scheduledOrders.Count} scheduled order(s).");
    }

    /// <summary>
    /// Runs right after the game has scheduled its own orders for the new day. Adds the extra daily orders, and
    /// moves the game's own new orders into the arrival hours when asked to.
    /// </summary>
    internal static void OnGameScheduledTodaysEmails(EmailService emailService, int upcomingCountBefore,
        bool isStoryDay)
    {
        if (!Settings.OrdersEnabled.Value)
            return;

        GameCalendar calendar = GameAccess.EmailCalendar(emailService);
        List<EmailLetterRecordInFolder> upcoming = GameAccess.UpcomingEmails(emailService);
        ArrivalWindow window = ArrivalWindow.ForToday(calendar);

        int movedCount = 0;

        // On the first days the game sends scripted story orders, the first of which must be there right away;
        // only the game's random daily orders are moved.
        if (Settings.MoveGameOrdersIntoHours.Value && !isStoryDay)
        {
            for (int index = upcomingCountBefore; index < upcoming.Count; index++)
            {
                if (upcoming[index].Folder != EmailFolders.OrdersInbox ||
                    !(upcoming[index].Email is EmailLetterOrderRecord gameOrder))
                    continue;

                gameOrder.ReceivedDateTime = window.RandomTime(calendar.CurrentDateTime);
                movedCount++;
            }
        }

        int wantedExtras = Settings.ExtraOrdersPerDay.Value;
        int addedExtras = 0;
        HashSet<DeviceInfo> todaysDevices = DevicesInTodaysOrders(emailService, calendar);

        for (int extra = 0; extra < wantedExtras; extra++)
        {
            DateTime arrival = window.RandomTime(calendar.CurrentDateTime);

            if (!TryMakeOrder(emailService, PickKind(emailService), arrival, todaysDevices,
                    out EmailLetterOrderRecord order, out OrderKind madeKind))
                break;

            upcoming.Add(new EmailLetterRecordInFolder { Email = order, Folder = EmailFolders.OrdersInbox });
            addedExtras++;

            Log.Info($"Scheduled extra order for {arrival:HH:mm}: {Describe(order, madeKind)}.");
        }

        if (wantedExtras > 0 || movedCount > 0)
        {
            Log.Info($"Day {calendar.CurrentDayNumber}: scheduled {addedExtras}/{wantedExtras} extra order(s) "
                     + $"for {window}" + (movedCount > 0 ? $", moved {movedCount} game order(s) there" : "")
                     + ".");
        }
    }

    private static bool TryMakeOrder(EmailService emailService, OrderKind wantedKind, DateTime receivedAt,
        HashSet<DeviceInfo> todaysDevices, out EmailLetterOrderRecord order, out OrderKind madeKind)
    {
        order = null;
        madeKind = wantedKind;

        if (!TryGenerateDevice(emailService, wantedKind, todaysDevices,
                out RandomlyGeneratedDeviceCondition device, out DeviceWorkType[] jobs))
        {
            if (wantedKind == OrderKind.Normal)
            {
                Log.Warning("The game's order generator found no licensed device to make an order for.");
                return false;
            }

            Log.Warning($"Could not make a {wantedKind} order (no fitting licensed device); "
                        + "making a Normal one instead.");

            madeKind = OrderKind.Normal;

            if (!TryGenerateDevice(emailService, OrderKind.Normal, todaysDevices, out device, out jobs))
            {
                Log.Warning("The game's order generator found no licensed device to make an order for.");
                return false;
            }
        }

        todaysDevices.Add(device.DeviceInfo);

        EmailSettings mailSettings = emailService.Settings;
        GameAccess.EmailComments(emailService).TryToGetRandomEmailComment(out EmailComment comment);

        // Field for field what EmailService.GenerateEmailsForTheDay fills in for the game's own orders.
        order = new EmailLetterOrderRecord
        {
            SenderContactInfo = GameAccess.EmailNames(emailService).GetRandomEmailContact(),
            ReceivedDateTime = receivedAt,
            DeviceCondition = device,
            WorkTypes = jobs,
            EmailComment = comment,
            SubjectLocalizationKey = mailSettings.OrderSubjectLocalizationKey,
            Payment = GameAccess.PriceEstimation(emailService).EstimateEmailOrderPayment(device, jobs),
            NumberDaysToComplete = mailSettings.NumberDaysToComplete
        };

        return true;
    }

    private static bool TryGenerateDevice(EmailService emailService, OrderKind kind,
        HashSet<DeviceInfo> todaysDevices, out RandomlyGeneratedDeviceCondition device, out DeviceWorkType[] jobs)
    {
        RandomDevicesGenerationService generator = GameAccess.OrderDevices(emailService);

        if (kind == OrderKind.Normal)
        {
            // Same call the game makes for each of its daily orders. Prefer a device nobody sent in today, like
            // the game does, but allow a repeat rather than no order at all.
            return generator.TryGetRandomDeviceConditionForEmailOrderFromAvailableDevices(todaysDevices,
                       out device, out jobs)
                   || generator.TryGetRandomDeviceConditionForEmailOrderFromAvailableDevices(null,
                       out device, out jobs);
        }

        List<DeviceWorkType> candidates = UnlockedJobsFor(emailService, kind);
        bool isCleaningOrRepair = kind == OrderKind.CleaningOnly || kind == OrderKind.RepairOnly;

        for (int deviceTry = 0; deviceTry < MaxDevicesPerOrder; deviceTry++)
        {
            DeviceWorkType job = PickJob(candidates);

            if (job == null)
                break;

            if (!isCleaningOrRepair)
            {
                // The game's own "order with this one job type" generator, which also adds the cleaning task the
                // game requires and already falls back to repeat devices.
                if (generator.TryGetSingleWorkTypeDeviceConditionForEmailOrderFromAvailableDevices(todaysDevices,
                        job, out device, out jobs))
                    return true;

                continue;
            }

            // Cleaning and repair: the same two steps the single-job generator takes, but the condition is re-rolled
            // on the same device until something is actually dirty or broken, so a retry doesn't draw another
            // device from the game's saved device rotation.
            if (!generator.TryGetRandomAvailableDeviceForEmailOrder(todaysDevices, job, out DeviceInfo deviceInfo) &&
                !generator.TryGetRandomAvailableDeviceForEmailOrder(new HashSet<DeviceInfo>(), job, out deviceInfo))
                continue;

            for (int roll = 0; roll < MaxRollsPerDevice; roll++)
            {
                if (generator.TryGenerateRandomConditionForDevice(deviceInfo, job, out device,
                        out DeviceWorkType finalJob) && HasWorkToDo(kind, device))
                {
                    jobs = new[] { finalJob };
                    return true;
                }
            }
        }

        device = null;
        jobs = null;
        return false;
    }

    private static bool HasWorkToDo(OrderKind kind, RandomlyGeneratedDeviceCondition device)
    {
        List<ElementData> parts = device.GetElementsCondition();

        switch (kind)
        {
            case OrderKind.CleaningOnly:
                return parts.Any(part => part.Condition is DirtyElementCondition);
            case OrderKind.RepairOnly:
                return parts.Any(part => part.Condition is DamagedElementCondition);
            default:
                return true;
        }
    }

    private static OrderKind PickKind(EmailService emailService)
    {
        List<(OrderKind kind, int weight)> pool = new List<(OrderKind, int)>();

        foreach (OrderKind kind in Enum.GetValues(typeof(OrderKind)))
        {
            int weight = Settings.WeightOf(kind);

            if (weight <= 0)
                continue;

            if (kind != OrderKind.Normal && UnlockedJobsFor(emailService, kind).Count == 0)
                continue;

            pool.Add((kind, weight));
        }

        if (pool.Count == 0)
            return OrderKind.Normal;

        int roll = Random.Range(0, pool.Sum(entry => entry.weight));

        foreach ((OrderKind kind, int weight) in pool)
        {
            if (roll < weight)
                return kind;

            roll -= weight;
        }

        return pool[pool.Count - 1].kind;
    }

    private static List<DeviceWorkType> UnlockedJobsFor(EmailService emailService, OrderKind kind)
    {
        return GameAccess.WorkTypes(emailService).GetAvailableWorkTypesList()
            .Where(job => job != null && Matches(kind, job))
            .ToList();
    }

    private static bool Matches(OrderKind kind, DeviceWorkType job)
    {
        switch (kind)
        {
            case OrderKind.CleaningOnly:
                return job is DeviceWorkTypeClean;
            case OrderKind.RepairOnly:
                return job is DeviceWorkTypeRepair;
            case OrderKind.Painting:
                return job is DeviceWorkTypePaintBase;
            case OrderKind.Hacking:
                return job is DeviceWorkTypeHacking;
            case OrderKind.Stickers:
                return job is DeviceWorkTypeApplyStickers;
            default:
                return false;
        }
    }

    private static DeviceWorkType PickJob(List<DeviceWorkType> candidates)
    {
        if (candidates.Count == 0)
            return null;

        // The game's own weighted pick (each job type carries a randomness weight); uniform if all weigh 0.
        if (RandomGeneratorFromWeights.TryToGetRandomObject(candidates, out DeviceWorkType job) && job != null)
            return job;

        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>Devices already in an order today, so new orders prefer other devices, like the game's do.</summary>
    private static HashSet<DeviceInfo> DevicesInTodaysOrders(EmailService emailService, GameCalendar calendar)
    {
        HashSet<DeviceInfo> devices = new HashSet<DeviceInfo>();

        foreach (EmailLetterRecordInFolder entry in GameAccess.UpcomingEmails(emailService))
        {
            if (entry.Email is EmailLetterOrderRecord order && order.DeviceCondition?.DeviceInfo != null)
            {
                devices.Add(order.DeviceCondition.DeviceInfo);
            }
        }

        foreach (EmailLetterRecordInFolder entry in GameAccess.ReceivedEmails(emailService))
        {
            if (entry.Email is EmailLetterOrderRecord order && order.DeviceCondition?.DeviceInfo != null &&
                order.ReceivedDateTime >= calendar.CurrentDayStartTime - TodayGrace)
            {
                devices.Add(order.DeviceCondition.DeviceInfo);
            }
        }

        return devices;
    }

    private static string Describe(EmailLetterOrderRecord order, OrderKind kind)
    {
        string jobs = string.Join(", ", order.WorkTypes.Where(job => job != null).Select(job => job.GetType().Name
            .Replace("DeviceWorkType", "")));

        return $"{order.DeviceCondition.DeviceInfo.ID} [{kind}: {jobs}], pays {order.Payment}, "
               + $"{order.NumberDaysToComplete} day(s), from {order.SenderContactInfo?.EmailAddress}";
    }
}
