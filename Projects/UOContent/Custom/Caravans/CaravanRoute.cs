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
            new Point3D(1270, 1600, 0),
            new Point3D(527, 1093, 0),
            500,
            new Point3D(1200, 1580, 0),
            new Point3D(1150, 1550, 0),
            new Point3D(1100, 1520, 0),
            new Point3D(1050, 1500, 0),
            new Point3D(1000, 1450, 0),
            new Point3D(950, 1400, 0),
            new Point3D(900, 1350, 0),
            new Point3D(850, 1300, 0),
            new Point3D(800, 1250, 0),
            new Point3D(750, 1200, 0),
            new Point3D(700, 1150, 0),
            new Point3D(650, 1120, 0),
            new Point3D(600, 1110, 0),
            new Point3D(550, 1100, 0)
        ));

        routes.Add(new CaravanRoute("Britain-Trinsic",
            new Point3D(1270, 1620, 0),
            new Point3D(1823, 2821, 0),
            600,
            GenerateWaypoints(new Point3D(1270, 1620, 0), new Point3D(1823, 2821, 0), 50)
        ));

        routes.Add(new CaravanRoute("Britain-Minoc",
            new Point3D(1270, 1600, 0),
            new Point3D(2449, 417, 5),
            700,
            GenerateWaypoints(new Point3D(1270, 1600, 0), new Point3D(2449, 417, 5), 50)
        ));

        routes.Add(new CaravanRoute("Yew-Vesper",
            new Point3D(527, 1093, 0),
            new Point3D(2895, 678, 0),
            800,
            GenerateWaypoints(new Point3D(527, 1093, 0), new Point3D(2895, 678, 0), 50)
        ));

        routes.Add(new CaravanRoute("Trinsic-Vesper",
            new Point3D(1850, 2745, 0),
            new Point3D(2895, 678, 0),
            750,
            GenerateWaypoints(new Point3D(1850, 2745, 0), new Point3D(2895, 678, 0), 50)
        ));

        return routes;
    }

    private static Point3D[] GenerateWaypoints(Point3D start, Point3D end, int step)
    {
        var points = new List<Point3D>();
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
        var count = Math.Max(1, dist / step);

        for (var i = 1; i < count; i++)
        {
            var t = (double)i / count;
            var x = (int)(start.X + dx * t);
            var y = (int)(start.Y + dy * t);
            var z = (int)(start.Z + (end.Z - start.Z) * t);
            points.Add(new Point3D(x, y, z));
        }

        return points.ToArray();
    }
}
