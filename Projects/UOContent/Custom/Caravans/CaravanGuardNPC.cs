using System;
using ModernUO.Serialization;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Caravans;

[SerializationGenerator(0, false)]
public partial class CaravanGuardNPC : BaseCreature
{
    private CaravanController _controller;

    [Constructible]
    public CaravanGuardNPC() : base(AIType.AI_Melee, FightMode.Evil)
    {
        Name = "Caravan Guard";
        Title = "the guard";
        Hue = Race.Human.RandomSkinHue();

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
        }

        SetStr(80, 100);
        SetDex(70, 90);
        SetInt(30, 50);

        SetHits(90, 120);
        SetDamage(8, 16);

        SetSkill(SkillName.Swords, 60.0, 80.0);
        SetSkill(SkillName.Tactics, 60.0, 80.0);
        SetSkill(SkillName.MagicResist, 40.0, 60.0);
        SetSkill(SkillName.Parry, 50.0, 70.0);

        Fame = 300;
        Karma = 300;

        VirtualArmor = 30;

        AddItem(new ChainChest());
        AddItem(new ChainLegs());
        AddItem(new PlateArms());
        AddItem(new Helmet());
        AddItem(new Boots());
        AddItem(new Longsword());

        var pack = new Backpack();
        AddItem(pack);
    }

    public CaravanGuardNPC(CaravanController controller) : this()
    {
        _controller = controller;
    }

    public void SetController(CaravanController controller) => _controller = controller;

    public override string DefaultName => "a caravan guard";
}
