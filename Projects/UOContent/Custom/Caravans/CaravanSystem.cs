using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Logging;

namespace Server.Custom.Caravans;

public static class CaravanSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CaravanSystem));
    private static readonly List<CaravanController> _activeCaravans = new();
    private static Timer _spawnTimer;
    private static int _maxConcurrentCaravans = 3;

    public static void Configure()
    {
        EventSink.ServerStarted += OnServerStarted;
        EventSink.WorldSave += OnWorldSave;
    }

    private static void OnServerStarted()
    {
        logger.Information("CaravanSystem: Starting. Max concurrent caravans: {Max}.", _maxConcurrentCaravans);

        _spawnTimer = Timer.DelayCall(
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(3),
            TrySpawnCaravan
        );
        _spawnTimer.Start();

        Timer.DelayCall(TimeSpan.FromSeconds(10), TrySpawnCaravan);
    }

    private static void OnWorldSave()
    {
        _activeCaravans.RemoveAll(c =>
            c.State == CaravanState.Arrived || c.State == CaravanState.Destroyed
        );
    }

    private static void TrySpawnCaravan()
    {
        _activeCaravans.RemoveAll(c =>
            c.State == CaravanState.Arrived || c.State == CaravanState.Destroyed
        );

        if (_activeCaravans.Count >= _maxConcurrentCaravans)
        {
            return;
        }

        var routes = CaravanRoute.CreateDefaultRoutes();
        var route = routes[Utility.Random(routes.Count)];
        var map = Map.Felucca;

        try
        {
            var controller = new CaravanController(route, map);
            controller.Start();
            _activeCaravans.Add(controller);

            logger.Information("CaravanSystem: Spawned caravan '{Route}'. Active: {Active}/{Max}.",
                route.Name, _activeCaravans.Count, _maxConcurrentCaravans);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "CaravanSystem: Failed to spawn caravan '{Route}'", route.Name);
        }
    }

    public static int ActiveCount => _activeCaravans.Count;
    public static IReadOnlyList<CaravanController> ActiveCaravans => _activeCaravans;
}
