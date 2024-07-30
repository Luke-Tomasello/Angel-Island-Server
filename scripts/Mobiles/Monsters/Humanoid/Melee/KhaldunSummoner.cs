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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/KhaldunSummoner.cs
 * ChangeLog
 *  12/03/06 Taran Kain
 *      Set Female = false. No trannies!
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    public class KhaldunSummoner : BaseCreature
    {
        public override bool ShowFameTitle { get { return false; } }

        [Constructable]
        public KhaldunSummoner()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {

            Name = "Khaldun Zealot";

            SetStr(356, 396);
            SetDex(105, 135);
            SetInt(530, 653);
            SetSkill(SkillName.Wrestling, 91.3, 97.8);
            SetSkill(SkillName.Tactics, 91.5, 99.0);
            SetSkill(SkillName.MagicResist, 90.6, 96.8);
            SetSkill(SkillName.Magery, 91.7, 99.0);
            SetSkill(SkillName.EvalInt, 100.1, 100.1);
            SetSkill(SkillName.Meditation, 121.1, 128.1);

            InitBody();
            InitOutfit();

            VirtualArmor = 36;
            SetFameLevel(4);
            SetKarmaLevel(4);
        }

        public override bool AlwaysMurderer { get { return true; } }
        public override bool Unprovokable { get { return true; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax { get { return 753; } }

        public KhaldunSummoner(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            Female = false;
            Body = 0x190;
        }

        // adam: GenerateLoot is never called.
        // The loot generation for this creature is special in that a BoneMagi is spawned in his place that carries all the loot.
        //	this is all done in OnBeforeDeath
        public override void GenerateLoot()
        {
        }

        public override bool OnBeforeDeath()
        {
            BoneMagi rm = new BoneMagi();

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

            LeatherGloves gloves = new LeatherGloves();
            gloves.Hue = 32;
            AddItem(gloves);

            BoneHelm helm = new BoneHelm();
            helm.Hue = 0x3A8;
            helm.LootType = LootType.Blessed;
            AddItem(helm);

            Cloak cloak = new Cloak();
            cloak.Hue = 32;
            AddItem(cloak);

            Kilt kilt = new Kilt();
            kilt.Hue = 32;
            AddItem(kilt);

            Sandals sandals = new Sandals();
            sandals.Hue = 32;
            AddItem(sandals);

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