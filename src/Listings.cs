using System.Collections.Generic;
using System.Linq;
using Restory.Data.Devices;
using Restory.Gameplay.Licenses;
using Restory.Gameplay.Shops;
using Restory.Gameplay.Shops.Devices;
using Restory.Gameplay.WorkOrders.EmailOrders;
using Random = UnityEngine.Random;

namespace ReStoryMoreWork;

/// <summary>
/// Adds second-hand Marketplace listings with the game's own listing generator, so price, condition, seller,
/// seller rating, description and how long the listing stays up are all rolled exactly as for the game's own.
/// Only devices you hold the licence for are listed. Listings are stored as the game's own type, so the game
/// saves and loads them with or without the mod.
/// </summary>
internal static class Listings
{
    /// <summary>Adds RestockAmount listings right now. Works with the clock paused.</summary>
    internal static void Restock()
    {
        DeviceShopSupplier supplier = GameAccess.ShopSupplier;

        if (supplier == null)
        {
            Toast.Show("Load a save first.");
            return;
        }

        int added = AddListings(supplier, Settings.RestockAmount.Value, out string problem);

        if (added > 0)
        {
            GameAccess.RefreshOpenMarketplacePage();
        }

        Log.Info($"Restock: added {added} listing(s)." + (problem != null ? $" {problem}" : ""));
        Toast.Show(added > 0 ? $"Marketplace: {added} new listing(s)." : $"Marketplace: nothing added. {problem}");
    }

    /// <summary>
    /// Replaces every random listing of a licensed device that is not in the cart with fresh ones. The game's
    /// scripted (story) listings, its unlicensed teaser listings and anything in the cart are kept.
    /// </summary>
    internal static void Refresh()
    {
        DeviceShopSupplier supplier = GameAccess.ShopSupplier;

        if (supplier == null)
        {
            Toast.Show("Load a save first.");
            return;
        }

        ShopsService shops = GameAccess.Shops(supplier);
        DeviceShoppingCart cart = GameAccess.ShoppingCart(supplier);
        DeviceShopRandomDevicesGenerationService generator = GameAccess.ListingGenerator(supplier);
        LicensesService licenses = GameAccess.Licenses(generator);

        // Check there is something to list before taking anything away.
        if (LicensedPool(generator, licenses).Count == 0)
        {
            Log.Info("Refresh: nothing changed, no licensed device is unlocked yet.");
            Toast.Show("Marketplace: nothing changed. No licensed device is unlocked yet.");
            return;
        }

        List<RandomlyGeneratedDeviceShopLot> replaceable = shops.Lots
            .OfType<RandomlyGeneratedDeviceShopLot>()
            .Where(lot => !cart.LotsInCart.Contains(lot) && licenses.IsLicensed(lot.Device.DeviceInfo))
            .ToList();

        foreach (RandomlyGeneratedDeviceShopLot lot in replaceable)
        {
            shops.RemoveDeviceFromShop(lot);
        }

        int wanted = System.Math.Max(replaceable.Count, Settings.RestockAmount.Value);
        int added = AddListings(supplier, wanted, out string problem);
        GameAccess.RefreshOpenMarketplacePage();

        Log.Info($"Refresh: removed {replaceable.Count} listing(s), added {added}." +
                 (problem != null ? $" {problem}" : ""));
        Toast.Show($"Marketplace refreshed: {replaceable.Count} removed, {added} new.");
    }

    /// <summary>Runs right after the game has prepared its own Marketplace batch for the new day.</summary>
    internal static void OnGameSuppliedTodaysBatch(DeviceShopSupplier supplier)
    {
        if (!Settings.MarketplaceEnabled.Value || Settings.ExtraListingsEachMorning.Value <= 0)
            return;

        int added = AddListings(supplier, Settings.ExtraListingsEachMorning.Value, out string problem);

        Log.Info($"Morning: added {added}/{Settings.ExtraListingsEachMorning.Value} extra listing(s)." +
                 (problem != null ? $" {problem}" : ""));
    }

    private static int AddListings(DeviceShopSupplier supplier, int wanted, out string problem)
    {
        problem = null;

        ShopsService shops = GameAccess.Shops(supplier);
        DeviceShopRandomDevicesGenerationService generator = GameAccess.ListingGenerator(supplier);
        LicensesService licenses = GameAccess.Licenses(generator);

        int listedDevices = shops.Lots.Count(lot => lot is IDeviceShopLot);
        int room = Settings.MaxListings.Value - listedDevices;

        if (room <= 0)
        {
            problem = $"The Marketplace already lists {listedDevices} devices (MaxListings is {Settings.MaxListings.Value}).";
            return 0;
        }

        List<AvailableDevicesListEntry> pool = LicensedPool(generator, licenses);

        if (pool.Count == 0)
        {
            problem = "No licensed device is unlocked yet.";
            return 0;
        }

        HashSet<string> listedIds = new HashSet<string>(shops.Lots
            .OfType<IDeviceShopLot>()
            .Select(lot => lot.Device?.DeviceInfo?.ID)
            .Where(id => id != null));

        int toAdd = System.Math.Min(wanted, room);

        if (toAdd < wanted)
        {
            problem = $"Stopped at MaxListings ({Settings.MaxListings.Value}).";
        }

        for (int index = 0; index < toAdd; index++)
        {
            DeviceInfo device = PickDevice(pool, listedIds);

            // postImmediately: posted as of this morning, so it shows up now instead of waiting on the clock.
            IDeviceShopLot lot = generator.GetRandomlyGeneratedDeviceShopLot(device, true);
            shops.SupplyDeviceLot(lot);
            listedIds.Add(device.ID);

            Log.Info($"Listed {device.ID}: {lot.Price}, {lot.Quality?.GetType().Name}, seller {lot.SellerNameKey}, "
                      + $"up for {lot.DaysBeforeRemoving} day(s).");
        }

        return toAdd;
    }

    /// <summary>The same pool and weights the game draws its own listings from, minus anything you aren't licensed for.</summary>
    private static List<AvailableDevicesListEntry> LicensedPool(DeviceShopRandomDevicesGenerationService generator,
        LicensesService licenses)
    {
        return GameAccess.AvailableDevices(generator).GetAvailableDevicesList()
            .Where(entry => entry.Device != null && entry.RandomnessWeight > 0 && licenses.IsLicensed(entry.Device))
            .ToList();
    }

    /// <summary>Weighted like the game's own pick, preferring devices not listed yet.</summary>
    private static DeviceInfo PickDevice(List<AvailableDevicesListEntry> pool, HashSet<string> listedIds)
    {
        List<AvailableDevicesListEntry> unlisted = pool.Where(entry => !listedIds.Contains(entry.Device.ID)).ToList();
        List<AvailableDevicesListEntry> choices = unlisted.Count > 0 ? unlisted : pool;

        int roll = Random.Range(0, choices.Sum(entry => entry.RandomnessWeight));

        foreach (AvailableDevicesListEntry entry in choices)
        {
            if (roll < entry.RandomnessWeight)
                return entry.Device;

            roll -= entry.RandomnessWeight;
        }

        return choices[choices.Count - 1].Device;
    }
}
