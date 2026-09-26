using System;
using System.Collections.Generic;
using Server;

namespace Server.Custom.Caravans;

public enum CaravanState
{
    Forming,
    Traveling,
    UnderAttack,
    Arrived,
    Destroyed
}

public class CaravanRoute
{
    public string Name { get; }
    public Point3D Start { get; }
    public Point3D End { get; }
    public List<Point3D> Waypoints { get; }
    public int GuardReward { get; }

    public CaravanRoute(string name, Point3D start, Point3D end, int guardReward, params Point3D[] waypoints)
    {
        Name = name;
        Start = start;
        End = end;
        GuardReward = guardReward;
        Waypoints = new List<Point3D> { start };
        Waypoints.AddRange(waypoints);
        Waypoints.Add(end);
    }

    public static List<CaravanRoute> CreateDefaultRoutes()
    {
        var routes = new List<CaravanRoute>();

        routes.Add(new CaravanRoute("Britain-Yew",
            new Point3D(1496, 1629, 10),
            new Point3D(527, 1093, 0),
            500,
            new Point3D(1200, 1500, 0),
            new Point3D(900, 1300, 0),
            new Point3D(700, 1200, 0)
        ));

        routes.Add(new CaravanRoute("Britain-Trinsic",
            new Point3D(1496, 1629, 10),
            new Point3D(1823, 2821, 0),
            600,
            new Point3D(1600, 1900, 0),
            new Point3D(1700, 2200, 0),
            new Point3D(1800, 2500, 0)
        ));

        routes.Add(new CaravanRoute("Britain-Minoc",
            new Point3D(1496, 1629, 10),
            new Point3D(2449, 417, 5),
            700,
            new Point3D(1700, 1200, 0),
            new Point3D(2000, 800, 0),
            new Point3D(2300, 500, 0)
        ));

        routes.Add(new CaravanRoute("Yew-Vesper",
            new Point3D(527, 1093, 0),
            new Point3D(2895, 678, 0),
            800,
            new Point3D(1000, 1000, 0),
            new Point3D(1800, 800, 0),
            new Point3D(2500, 700, 0)
        ));

        routes.Add(new CaravanRoute("Trinsic-Vesper",
            new Point3D(1823, 2821, 0),
            new Point3D(2895, 678, 0),
            750,
            new Point3D(2200, 2500, 0),
            new Point3D(2600, 1800, 0),
            new Point3D(2800, 1200, 0)
        ));

        return routes;
    }
}
