using System;
using ModernUO.CodeGeneratedEvents;
using Server;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom;

public static class LoginDebug
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LoginDebug));

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile m)
    {
        if (m?.Map == null || m.NetState == null) return;

        var map = m.Map;
        var loc = m.Location;

        var mobilesInRange = 0;
        foreach (var mob in map.GetMobilesInRange(loc, Core.GlobalMaxUpdateRange))
        {
            if (mob != m) mobilesInRange++;
        }

        var itemsInRange = 0;
        foreach (var item in map.GetItemsInRange(loc, Core.GlobalMaxUpdateRange))
        {
            itemsInRange++;
        }

        logger.Information(
            "LoginDebug: Player {Name} at ({X}, {Y}, {Z}) on map {Map} ({MapW}x{MapH}). " +
            "Mobiles in range: {MobCount}, Items in range: {ItemCount}",
            m.Name, loc.X, loc.Y, loc.Z, map.Name, map.Width, map.Height,
            mobilesInRange, itemsInRange
        );

        var season = m.GetSeason();
        logger.Information("LoginDebug: Season={Season}, MapID={MapID}", season, map.MapID);

        var ns = m.NetState;
        logger.Information(
            "LoginDebug: NetState Version={Version}, StygianAbyss={SA}",
            ns.Version, ns.StygianAbyss
        );

        if (mobilesInRange == 0 && itemsInRange == 0 && loc.X > 5000)
        {
            var britain = new Point3D(1496, 1629, 10);
            logger.Information("LoginDebug: Moving {Name} from Green Acres to Britain ({X}, {Y}, {Z})",
                m.Name, britain.X, britain.Y, britain.Z);
            m.MoveToWorld(britain, map);
        }
    }
}
