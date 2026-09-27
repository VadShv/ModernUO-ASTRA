using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Caravans;

public static class CaravanSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CaravanSystem));
    private static readonly List<CaravanController> _activeCaravans = new();

    public static void Configure()
    {
        EventSink.ServerStarted += OnServerStarted;
        EventSink.WorldSave += OnWorldSave;
    }

    private static void OnServerStarted()
    {
        CleanupOrphanedCaravanNpcs();

        logger.Information("CaravanSystem: Starting. Scheduled departures at 10:00 and 20:00 MSK.");

        ScheduleNextDeparture();
    }

    private static void OnWorldSave()
    {
        _activeCaravans.RemoveAll(c =>
            c.State == CaravanState.Arrived || c.State == CaravanState.Destroyed
        );
    }

    private static void CleanupOrphanedCaravanNpcs()
    {
        var count = 0;
        foreach (var m in World.Mobiles.Values.ToList())
        {
            if (m is CaravanMerchant or CaravanGuardNPC)
            {
                m.Delete();
                count++;
            }
        }
        if (count > 0)
        {
            logger.Information("CaravanSystem: Cleaned up {Count} orphaned caravan NPCs.", count);
        }
    }

    private static void ScheduleNextDeparture()
    {
        var nowUtc = DateTime.UtcNow;
        var msk = nowUtc.AddHours(3);

        int[] departHoursMsk = { 10, 20 };
        int[] returnHoursMsk = { 13, 23 };

        var nextDepart = FindNextTime(msk, departHoursMsk);
        var nextReturn = FindNextTime(msk, returnHoursMsk);

        if (nextDepart <= nextReturn)
        {
            var delay = nextDepart - msk;
            logger.Information("CaravanSystem: Next departure in {Delay} (at {Time} MSK).",
                delay, nextDepart.ToString("HH:mm"));
            Timer.DelayCall(delay, () => SpawnScheduledCaravan(isReturn: false));
        }
        else
        {
            var delay = nextReturn - msk;
            logger.Information("CaravanSystem: Next return caravan in {Delay} (at {Time} MSK).",
                delay, nextReturn.ToString("HH:mm"));
            Timer.DelayCall(delay, () => SpawnScheduledCaravan(isReturn: true));
        }
    }

    private static DateTime FindNextTime(DateTime now, int[] hours)
    {
        foreach (var h in hours.OrderBy(h => h))
        {
            var target = new DateTime(now.Year, now.Month, now.Day, h, 0, 0);
            if (target > now) return target;
        }
        return new DateTime(now.Year, now.Month, now.Day, hours[0], 0, 0).AddDays(1);
    }

    private static void SpawnScheduledCaravan(bool isReturn)
    {
        _activeCaravans.RemoveAll(c =>
            c.State == CaravanState.Arrived || c.State == CaravanState.Destroyed
        );

        var routes = CaravanRoute.CreateDefaultRoutes();
        var map = Map.Felucca;
        var mskNow = DateTime.UtcNow.AddHours(3);

        CaravanRoute route;
        if (isReturn)
        {
            var returnRoutes = routes.Where(r => r.Name.StartsWith("Britain-")).ToList();
            route = returnRoutes[Utility.Random(returnRoutes.Count)];
            var dest = route.End;
            route = new CaravanRoute($"Return-{route.Name.Split('-')[1]}-Britain",
                dest, route.Start, route.GuardReward,
                route.Waypoints.Skip(1).Take(route.Waypoints.Count - 2).Reverse().ToArray());
        }
        else
        {
            var departRoutes = routes.Where(r => r.Name.StartsWith("Britain-")).ToList();
            route = departRoutes[Utility.Random(departRoutes.Count)];
        }

        try
        {
            var controller = new CaravanController(route, map);
            controller.Start();
            _activeCaravans.Add(controller);

            var type = isReturn ? "returning to Britain" : "departing from Britain";
            logger.Information("CaravanSystem: Scheduled caravan '{Route}' {Type} at {Time} MSK.",
                route.Name, type, mskNow.ToString("HH:mm"));

            AnnounceCaravan(route, isReturn);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "CaravanSystem: Failed to spawn scheduled caravan '{Route}'", route.Name);
        }

        ScheduleNextDeparture();
    }

    private static void AnnounceCaravan(CaravanRoute route, bool isReturn)
    {
        var parts = route.Name.Split('-');
        var from = parts.Length > 0 ? parts[0] : "Unknown";
        var to = parts.Length > 1 ? parts[1] : "Unknown";
        string msg;

        if (isReturn)
        {
            msg = $"[Caravan] A caravan is returning from {from} to Britain! Guards needed. Say 'guard' near the merchant to join.";
        }
        else
        {
            msg = $"[Caravan] A caravan is departing from Britain to {to}! Reward: {route.GuardReward} gold. Say 'guard' near the merchant to join.";
        }

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile != null && ns.Mobile.Alive)
            {
                ns.Mobile.SendMessage(0x3B, msg);
            }
        }
    }

    public static void OnCaravanArrived(CaravanController controller)
    {
        var arrivalLoc = controller.Route.End;
        var map = Map.Felucca;

        var vendors = new List<BaseVendor>();
        foreach (var m in map.GetMobilesInRange<BaseVendor>(arrivalLoc, 100))
        {
            vendors.Add(m);
        }

        foreach (var vendor in vendors)
        {
            vendor.Restock();
            AddBonusStock(vendor);
        }

        var cityName = controller.RouteName.Split('-').Last();
        logger.Information("CaravanSystem: Caravan arrived at {City}. Restocked {Count} vendors.",
            cityName, vendors.Count);

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile != null && ns.Mobile.Alive)
            {
                ns.Mobile.SendMessage(0x44,
                    $"[Caravan] The caravan from {controller.RouteName.Split('-')[0]} has arrived in {cityName}! Local shops have been restocked with fresh goods.");
            }
        }
    }

    private static void AddBonusStock(BaseVendor vendor)
    {
        var pack = vendor.Backpack;
        if (pack == null) return;

        var bonusItems = new List<Item>();
        var roll = Utility.Random(4);
        switch (roll)
        {
            case 0:
                for (var i = 0; i < 3; i++) bonusItems.Add(new Longsword());
                for (var i = 0; i < 2; i++) bonusItems.Add(new Buckler());
                break;
            case 1:
                for (var i = 0; i < 3; i++) bonusItems.Add(new ChainChest());
                for (var i = 0; i < 2; i++) bonusItems.Add(new ChainLegs());
                break;
            case 2:
                for (var i = 0; i < 5; i++) bonusItems.Add(new IronIngot(20));
                for (var i = 0; i < 3; i++) bonusItems.Add(new Arrow(50));
                break;
            case 3:
                for (var i = 0; i < 3; i++) bonusItems.Add(new PlateChest());
                for (var i = 0; i < 2; i++) bonusItems.Add(new PlateArms());
                bonusItems.Add(new Helmet());
                break;
        }

        foreach (var item in bonusItems)
        {
            pack.DropItem(item);
        }
    }

    public static int ActiveCount => _activeCaravans.Count;
    public static IReadOnlyList<CaravanController> ActiveCaravans => _activeCaravans;
}
