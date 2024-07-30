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

/* Scripts/Engines/AngelIsland/BaseAIGuard.cs
 
 *	ChangeLog
 *	8/6/2021, Adam
 *	    Gards were too slow. Speed them up a bit. 
 *	5/17/04, mith
 *		Cleaned up the way we delete armor in OnBeforeDeath().
 *	4/10/04 changes by mith
 *		Added chance to get a bow along with the other weapons. 
 *		Changed Mace to WarMace.
 *	4/7/04, changes by mith
 *		Changed FightMode to Aggressor instead of None.
 *		Removed tunic/doublet/body sash creation so these items don't drop to corpse.
 *		Replaced halberd with Broadsword so they don't deal quite so much damage as quick.
 *	Created 4/3/04 by mith
 */
using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class BaseAIGuard : BaseCreature
    {
        private static BaseDoor m_after_hours_club = null;
        public static BaseDoor AfterHoursClub { get { return m_after_hours_club; } set { m_after_hours_club = value; } }

        [Constructable]
        public BaseAIGuard()
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            InitStats(400, 100, 100);
            Title = "the guard";

            SpeechHue = Utility.RandomSpeechHue();

            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");

                switch (Utility.Random(2))
                {
                    case 0: AddItem(new LeatherSkirt()); break;
                    case 1: AddItem(new LeatherShorts()); break;
                }

                switch (Utility.Random(5))
                {
                    case 0: AddItem(new FemaleLeatherChest()); break;
                    case 1: AddItem(new FemaleStuddedChest()); break;
                    case 2: AddItem(new LeatherBustierArms()); break;
                    case 3: AddItem(new StuddedBustierArms()); break;
                    case 4: AddItem(new FemalePlateChest()); break;
                }
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");

                AddItem(new PlateChest());
                AddItem(new PlateArms());
                AddItem(new PlateGorget());
                AddItem(new PlateLegs());
            }

            Item hair = new Item(Utility.RandomList(0x203B, 0x203C, 0x203D, 0x2044, 0x2045, 0x2047, 0x2049, 0x204A));

            hair.Hue = Utility.RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;

            AddItem(hair);

            if (Utility.RandomBool() && !this.Female)
            {
                Item beard = new Item(Utility.RandomList(0x203E, 0x203F, 0x2040, 0x2041, 0x204B, 0x204C, 0x204D));

                beard.Hue = hair.Hue;
                beard.Layer = Layer.FacialHair;
                beard.Movable = false;

                AddItem(beard);
            }

            Broadsword weapon = new Broadsword();

            weapon.Movable = false;
            weapon.Crafter = this;
            weapon.Quality = WeaponQuality.Exceptional;

            AddItem(weapon);

            Container pack = new Backpack();

            pack.Movable = false;

            pack.DropItem(new Gold(10, 25));

            AddItem(pack);

            SetSkill(SkillName.Anatomy, 70.1, 80.0);
            SetSkill(SkillName.Tactics, 70.1, 80.0);
            SetSkill(SkillName.Swords, 70.1, 80.0);
            SetSkill(SkillName.MagicResist, 70.1, 80.0);
        }

        public BaseAIGuard(Serial serial)
            : base(serial)
        {
        }

        public override bool OnBeforeDeath()
        {
            ArrayList items = new ArrayList(this.Items);

            foreach (Item i in items)
            {
                if (i is BaseArmor || i is BaseClothing)
                    i.Delete();
            }

            return base.OnBeforeDeath();
        }

        public void DropItem(Item item)
        {
            if (Summoned || item == null)
            {
                if (item != null)
                    item.Delete();

                return;
            }

            Container pack = Backpack;

            if (pack == null)
            {
                pack = new Backpack();

                pack.Movable = false;

                AddItem(pack);
            }

            pack.DropItem(item);
        }

        public bool DropWeapon(int minLevel, int maxLevel)
        {
            if (1.0 <= Utility.RandomDouble())
                return false;

            if (maxLevel > 2)
                maxLevel = 2;

            Cap(ref minLevel, 0, 2);
            Cap(ref maxLevel, 0, 2);

            BaseWeapon weapon = new Broadsword();

            double random = Utility.RandomDouble();
            if (random >= .75)
            { weapon = new WarFork(); }
            else if (random >= .50)
            { weapon = new WarMace(); }
            else if (random >= .25)
            {
                weapon = new Bow();
                Arrow arrows = new Arrow();
                arrows.Amount = 25;
                DropItem(arrows);
            }

            if (weapon == null)
                return false;

            Item item = Loot.ImbueWeaponOrArmor((Item)weapon, minLevel, maxLevel);

            DropItem(item);

            return true;
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