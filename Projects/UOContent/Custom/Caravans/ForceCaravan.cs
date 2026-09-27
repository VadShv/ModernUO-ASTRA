using System;
using System.Linq;
using Server;
using Server.Custom.Caravans;
using Server.Network;

namespace Server.Custom.Caravans;

public static class ForceCaravan
{
    public static void Configure()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted()
    {
        Timer.DelayCall(TimeSpan.FromSeconds(5), () =>
        {
            var routes = CaravanRoute.CreateDefaultRoutes();
            var route = routes.FirstOrDefault(r => r.Name == "Britain-Yew") ?? routes[0];
            var controller = new CaravanController(route, Map.Felucca);
            controller.Start();

            var msg = $"[Caravan] A caravan is departing from Britain to Yew! Reward: {route.GuardReward} gold. Say 'guard' near the merchant to join.";
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile != null && ns.Mobile.Alive)
                    ns.Mobile.SendMessage(0x3B, msg);
            }
        });
    }
}
