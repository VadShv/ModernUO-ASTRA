using System;
using System.Linq;
using Server;
using Server.Custom.Caravans;
using Server.Network;

namespace Server.Custom.Caravans;

public static class TestCaravan2
{
    public static void Configure()
    {
        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnServerStarted()
    {
        Timer.DelayCall(TimeSpan.FromSeconds(3), () =>
        {
            var routes = CaravanRoute.CreateDefaultRoutes();
            var route = routes.First(r => r.Name == "Britain-Yew");
            var controller = new CaravanController(route, Map.Felucca);
            controller.Start();

            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile != null && ns.Mobile.Alive)
                    ns.Mobile.SendMessage(0x3B, "[Caravan] TEST caravan at Britain west gate (1330, 1597)! Come see it!");
            }
        });
    }
}
