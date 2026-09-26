using System;
using ModernUO.Serialization;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Caravans;

[SerializationGenerator(0, false)]
public partial class CaravanMerchant : BaseCreature
{
    private CaravanController _controller;

    [Constructible]
    public CaravanMerchant() : base(AIType.AI_Melee, FightMode.Aggressor)
    {
        Name = "Caravan Merchant";
        Title = "the trader";
        Body = 0x190;
        Hue = Race.Human.RandomSkinHue();

        SetStr(60, 80);
        SetDex(50, 70);
        SetInt(40, 60);

        SetHits(80, 100);
        SetDamage(5, 10);

        SetSkill(SkillName.Swords, 40.0, 60.0);
        SetSkill(SkillName.Tactics, 40.0, 60.0);
        SetSkill(SkillName.MagicResist, 30.0, 50.0);

        Fame = 500;
        Karma = 500;

        VirtualArmor = 20;

        AddItem(new FancyShirt(Utility.RandomNeutralHue()));
        AddItem(new LongPants(Utility.RandomNeutralHue()));
        AddItem(new Boots());
        AddItem(new Cutlass());

        var pack = new Backpack();
        AddItem(pack);
    }

    public CaravanMerchant(CaravanController controller) : this()
    {
        _controller = controller;
    }

    public void SetController(CaravanController controller) => _controller = controller;

    public override string DefaultName => "a caravan merchant";

    public override void OnDoubleClick(Mobile from)
    {
        if (_controller == null || from is not PlayerMobile player)
        {
            base.OnDoubleClick(from);
            return;
        }

        if (from.InRange(this, 3))
        {
            if (_controller.State == CaravanState.Traveling || _controller.State == CaravanState.UnderAttack)
            {
                var dest = _controller.RouteName.Contains("-") ? _controller.RouteName.Split('-')[1] : _controller.RouteName;
                from.SendMessage($"This caravan travels to {dest}. Guard reward: {_controller.Route.GuardReward} gold.");
                from.SendMessage("Say 'guard' to join as a caravan guard.");
            }
            else
            {
                from.SendMessage("This caravan is not traveling right now.");
            }
        }
        else
        {
            from.SendMessage("You are too far away.");
        }
    }

    public override bool HandlesOnSpeech(Mobile from) => from is PlayerMobile && from.InRange(this, 5);

    public override void OnSpeech(SpeechEventArgs e)
    {
        if (_controller == null) return;

        var speech = e.Speech.ToLowerInvariant();

        if (speech.Contains("guard") || speech.Contains("охрана") || speech.Contains("защита"))
        {
            _controller.AddPlayerGuard(e.Mobile);
            e.Handled = true;
        }
        else if (speech.Contains("leave") || speech.Contains("уйти"))
        {
            _controller.RemovePlayerGuard(e.Mobile);
            e.Mobile.SendMessage("You left the caravan guard.");
            e.Handled = true;
        }
    }
}
