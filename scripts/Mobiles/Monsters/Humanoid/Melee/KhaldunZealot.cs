/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

/* Scripts/Mobiles/Monsters/Humanoid/Melee/KhaldunZealot.cs
 * ChangeLog
 *  12/03/06 Taran Kain
 *      Set Female = false. No trannies!
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    public class KhaldunZealot : BaseCreature
    {
        public override bool ShowFameTitle { get { return false; } }

        [Constructable]
        public KhaldunZealot()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "Khaldun Zealot";
            Hue = 0;

            this.InitStats(Utility.Random(359, 399), Utility.Random(138, 151), Utility.Random(76, 97));

            this.Skills[SkillName.Wrestling].Base = Utility.Random(74, 80);
            this.Skills[SkillName.Swords].Base = Utility.Random(90, 95);
            this.Skills[SkillName.Anatomy].Base = Utility.Random(120, 125);
            this.Skills[SkillName.MagicResist].Base = Utility.Random(90, 94);
            this.Skills[SkillName.Tactics].Base = Utility.Random(90, 95);

            InitBody();
            InitOutfit();

            this.Fame = Utility.Random(5000, 9999);
            this.Karma = Utility.Random(-5000, -9999);
            this.VirtualArmor = 40;

        }

        public override bool AlwaysMurderer { get { return true; } }
        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax { get { return 769; } }

        public KhaldunZealot(Serial serial)
            : base(serial)
        {
        }
        public override void InitBody()
        {
            Female = false;
            Body = 0x190;
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            BoneArms arms = new BoneArms();
            arms.Hue = 0x3A8;
            arms.LootType = LootType.Blessed;
            AddItem(arms);

            BoneGloves gloves = new BoneGloves();
            gloves.Hue = 0x3A8;
            gloves.LootType = LootType.Blessed;
            AddItem(gloves);

            BoneChest tunic = new BoneChest();
            tunic.Hue = 0x3A8;
            tunic.LootType = LootType.Blessed;
            AddItem(tunic);
            BoneLegs legs = new BoneLegs();
            legs.Hue = 0x3A8;
            legs.LootType = LootType.Blessed;
            AddItem(legs);

            BoneHelm helm = new BoneHelm();
            helm.Hue = 0x3A8;
            helm.LootType = LootType.Blessed;
            AddItem(helm);

            AddItem(new Shoes());
            AddItem(new Buckler());

            VikingSword weapon = new VikingSword();

            weapon.Movable = true;

            AddItem(weapon);
        }

        // adam: GenerateLoot is never called.
        // The loot generation for this creature is special in that a SkeletalKnight is spawned in his place that carries all the loot.
        //	this is all done in OnBeforeDeath
        public override void GenerateLoot()
        {
        }

        public override bool OnBeforeDeath()
        {
            SkeletalKnight rm = new SkeletalKnight();

            rm.Team = this.Team;
            rm.MoveToWorld(this.Location, this.Map);

            Effects.SendLocationEffect(Location, Map, 0x3709, 13, 0x3B2, 0);

            Container bag = new Bag();
            switch (Utility.Random(9))
            {
                case 0: bag.DropItem(new Amber()); break;
                case 1: bag.DropItem(new Amethyst()); break;
                case 2: bag.DropItem(new Citrine()); break;
                case 3: bag.DropItem(new Diamond()); break;
                case 4: bag.DropItem(new Emerald()); break;
                case 5: bag.DropItem(new Ruby()); break;
                case 6: bag.DropItem(new Sapphire()); break;
                case 7: bag.DropItem(new StarSapphire()); break;
                case 8: bag.DropItem(new Tourmaline()); break;
            }

            switch (Utility.Random(8))
            {
                case 0: bag.DropItem(new SpidersSilk(3)); break;
                case 1: bag.DropItem(new BlackPearl(3)); break;
                case 2: bag.DropItem(new Bloodmoss(3)); break;
                case 3: bag.DropItem(new Garlic(3)); break;
                case 4: bag.DropItem(new MandrakeRoot(3)); break;
                case 5: bag.DropItem(new Nightshade(3)); break;
                case 6: bag.DropItem(new SulfurousAsh(3)); break;
                case 7: bag.DropItem(new Ginseng(3)); break;
            }

            bag.DropItem(new Gold(1000, 1500));
            rm.AddItem(bag);

            // TODO: need to drop thie 3 MID loot here too
            // Category 3 MID
            //PackMagicItem( 1, 2, 0.10 );
            //PackMagicItem( 1, 2, 0.05 );

            this.Delete();

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}