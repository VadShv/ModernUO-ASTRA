using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.Caravans;

public class CaravanController
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CaravanController));

    private readonly CaravanRoute _route;
    private readonly Map _map;
    private readonly List<Mobile> _members = new();
    private readonly List<Mobile> _playerGuards = new();
    private readonly Timer _movementTimer;
    private readonly Timer _ambushTimer;

    private int _currentWaypoint = 0;
    private CaravanState _state = CaravanState.Forming;
    private DateTime _lastAmbush;
    private Mobile _merchant;

    public CaravanState State => _state;
    public CaravanRoute Route => _route;
    public Mobile Merchant => _merchant;
    public int PlayerGuardCount => _playerGuards.Count;
    public string RouteName => _route.Name;

    public CaravanController(CaravanRoute route, Map map)
    {
        _route = route;
        _map = map;
        _movementTimer = Timer.DelayCall(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), OnMoveTick);
        _ambushTimer = Timer.DelayCall(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), CheckAmbush);
    }

    public void Start()
    {
        try
        {
            _merchant = new CaravanMerchant(this);
            _merchant.MoveToWorld(_route.Start, _map);
            _members.Add(_merchant);

            for (var i = 0; i < 2; i++)
            {
                var horse = new PackHorse();
                horse.MoveToWorld(GetNearbyLocation(_route.Start, 2), _map);
                _members.Add(horse);
            }

            for (var i = 0; i < 3; i++)
            {
                var guard = new CaravanGuardNPC(this);
                guard.MoveToWorld(GetNearbyLocation(_route.Start, 3), _map);
                _members.Add(guard);
            }

            FillCargo();

            _state = CaravanState.Traveling;
            _movementTimer.Start();
            _ambushTimer.Start();
            _lastAmbush = Core.Now;

            logger.Information("Caravan '{Route}' started at {Start} with {Count} members.",
                _route.Name, _route.Start, _members.Count);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to start caravan '{Route}'", _route.Name);
            Cleanup();
        }
    }

    private void FillCargo()
    {
        foreach (var member in _members.OfType<PackHorse>())
        {
            var pack = member.Backpack;
            if (pack == null) continue;

            for (var i = 0; i < 5; i++)
            {
                var item = Utility.Random(4) switch
                {
                    0 => new Gold(100 + Utility.Random(200)),
                    1 => new IronIngot(20 + Utility.Random(30)),
                    2 => new BoltOfCloth(),
                    3 => (Item)new Log(20 + Utility.Random(30)),
                    _ => new Gold(50)
                };
                pack.DropItem(item);
            }
        }
    }

    private void OnMoveTick()
    {
        if (_state != CaravanState.Traveling && _state != CaravanState.UnderAttack)
        {
            return;
        }

        if (_merchant == null || _merchant.Deleted || !_merchant.Alive)
        {
            OnCaravanDestroyed();
            return;
        }

        if (_currentWaypoint >= _route.Waypoints.Count)
        {
            OnArrived();
            return;
        }

        var target = _route.Waypoints[_currentWaypoint];
        var merchantLoc = _merchant.Location;

        if (GetDistance(merchantLoc, target) <= 5)
        {
            _currentWaypoint++;
            if (_currentWaypoint < _route.Waypoints.Count)
            {
                logger.Information("Caravan '{Route}' reached waypoint {Index}/{Total}.",
                    _route.Name, _currentWaypoint, _route.Waypoints.Count);
            }
            return;
        }

        MoveMember(_merchant, target, 0);
        for (var i = 0; i < _members.Count; i++)
        {
            var m = _members[i];
            if (m == _merchant || m.Deleted || !m.Alive) continue;
            MoveMember(m, merchantLoc, i + 2);
        }

        foreach (var guard in _playerGuards)
        {
            if (guard.Deleted || !guard.Alive) continue;
            if (GetDistance(guard.Location, merchantLoc) > 15)
            {
                guard.MoveToWorld(GetNearbyLocation(merchantLoc, 3), _map);
            }
        }
    }

    private void MoveMember(Mobile m, Point3D target, int offset)
    {
        if (m.Deleted || !m.Alive) return;

        var loc = m.Location;
        var dx = target.X - loc.X;
        var dy = target.Y - loc.Y;
        var dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (dist == 0) return;

        var dir = m.GetDirectionTo(target.X, target.Y);

        if (!m.Move(dir))
        {
            var left = (Direction)(((int)dir - 1) & 0x07);
            if (!m.Move(left))
            {
                var right = (Direction)(((int)dir + 1) & 0x07);
                if (!m.Move(right))
                {
                    var around = (Direction)(((int)dir + 2) & 0x07);
                    m.Move(around);
                }
            }
        }
    }

    private void CheckAmbush()
    {
        if (_state != CaravanState.Traveling) return;

        var timeSinceLastAmbush = Core.Now - _lastAmbush;
        if (timeSinceLastAmbush < TimeSpan.FromMinutes(2)) return;

        if (Utility.RandomDouble() > 0.4) return;

        SpawnAmbush();
    }

    private void SpawnAmbush()
    {
        _state = CaravanState.UnderAttack;
        _lastAmbush = Core.Now;

        var banditCount = 2 + Utility.Random(3) + Math.Max(0, _playerGuards.Count - 1);
        var loc = _merchant.Location;

        for (var i = 0; i < banditCount; i++)
        {
            var bandit = new Brigand();
            bandit.MoveToWorld(GetNearbyLocation(loc, 8 + Utility.Random(5)), _map);

            if (_merchant != null && _merchant.Alive)
            {
                bandit.Combatant = _merchant;
            }
        }

        _merchant?.Say("We're under attack! Protect the caravan!");

        logger.Information("Caravan '{Route}' ambushed by {Count} bandits at {Loc}.",
            _route.Name, banditCount, loc);

        Timer.DelayCall(TimeSpan.FromSeconds(20), () =>
        {
            if (_state == CaravanState.UnderAttack)
            {
                _state = CaravanState.Traveling;
                _merchant?.Say("The attack is over. Moving on.");
            }
        });
    }

    public bool AddPlayerGuard(Mobile player)
    {
        if (_state != CaravanState.Traveling && _state != CaravanState.UnderAttack)
        {
            player.SendMessage("This caravan is not accepting guards right now.");
            return false;
        }

        if (_playerGuards.Contains(player))
        {
            player.SendMessage("You are already guarding this caravan.");
            return false;
        }

        if (GetDistance(player.Location, _merchant.Location) > 20)
        {
            player.SendMessage("You are too far from the caravan merchant.");
            return false;
        }

        _playerGuards.Add(player);
        player.SendMessage($"You joined the caravan guard! Protect the merchant to {_route.Name.Split('-')[1]} for {_route.GuardReward} gold.");
        _merchant?.Say($"{player.Name} has joined our guards!");

        logger.Information("Player {Player} joined caravan '{Route}' as guard.", player.Name, _route.Name);
        return true;
    }

    public void RemovePlayerGuard(Mobile player)
    {
        _playerGuards.Remove(player);
    }

    private void OnArrived()
    {
        _state = CaravanState.Arrived;
        _movementTimer.Stop();
        _ambushTimer.Stop();

        _merchant?.Say("We have arrived! Thank you for the protection.");

        foreach (var guard in _playerGuards)
        {
            if (guard.Deleted || !guard.Alive) continue;

            var gold = new Gold(_route.GuardReward);
            guard.Backpack?.DropItem(gold);
            guard.SendMessage($"Caravan arrived! You received {_route.GuardReward} gold for your service.");
        }

        logger.Information("Caravan '{Route}' arrived at destination. {GuardCount} player guards rewarded.",
            _route.Name, _playerGuards.Count);

        Timer.DelayCall(TimeSpan.FromSeconds(10), Cleanup);
    }

    private void OnCaravanDestroyed()
    {
        _state = CaravanState.Destroyed;
        _movementTimer.Stop();
        _ambushTimer.Stop();

        logger.Information("Caravan '{Route}' destroyed. Merchant killed.", _route.Name);

        foreach (var guard in _playerGuards)
        {
            if (!guard.Deleted && guard.Alive)
            {
                guard.SendMessage("The caravan was destroyed! You failed to protect the merchant.");
            }
        }

        Timer.DelayCall(TimeSpan.FromSeconds(30), Cleanup);
    }

    private void Cleanup()
    {
        foreach (var m in _members)
        {
            if (m != null && !m.Deleted && m.Alive)
            {
                m.Delete();
            }
        }
        _members.Clear();
        _playerGuards.Clear();
    }

    private Point3D GetNearbyLocation(Point3D center, int range)
    {
        for (var i = 0; i < 10; i++)
        {
            var x = center.X + Utility.Random(-range, range * 2 + 1);
            var y = center.Y + Utility.Random(-range, range * 2 + 1);
            var z = _map.GetAverageZ(x, y);
            if (_map.CanSpawnMobile(x, y, z))
            {
                return new Point3D(x, y, z);
            }
        }
        return center;
    }

    private static int GetDistance(Point3D a, Point3D b)
    {
        return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    public bool IsMember(Mobile m) => _members.Contains(m);
}
