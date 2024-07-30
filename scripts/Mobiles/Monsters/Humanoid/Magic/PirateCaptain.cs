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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/PirateCaptain.cs	
 * ChangeLog:
 *	7/9/10, adam
 *		o Merge pirate class hierarchy (all pirates are now derived from class Pirate)
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	4/9/05, Adam
 *		Upgrade treasure map level to 5 from 3
 *	1/2/05, Adam
 *		First version based on pirate.cs
 *		1. ControlSlots = 5
 *		2. lichlord stats
 *		3. Better magic equipemnt, gold, and Category 3 MID
 *		4. small chance at a black pirate captains hat
 */

using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("corpse of a salty seadog")]
    public class PirateCaptain : Pirate
    {
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }

        [Constructable]
        public PirateCaptain()
            : base(AIType.AI_Hybrid)
        {
        }

        public override void InitClass()
        {
            ControlSlots = 5;

            SetStr(416, 505);
            SetDex(146, 165);
            SetInt(566, 655);

            SetHits(250, 303);

            VirtualArmor = 40;

            SetDamage(16, 22);

            SetSkill(SkillName.EvalInt, 80.1, 90.1);
            SetSkill(SkillName.Magery, 80.1, 90.1);
            SetSkill(SkillName.MagicResist, 150.5, 200.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);
            SetSkill(SkillName.Tactics, 85.1, 98.1);
            SetSkill(SkillName.Swords, 85.1, 98.1);
            SetSkill(SkillName.Healing, 85.1, 98.1);

            Fame = 15000;
            Karma = -15000;
        }

        public override void InitBody()
        {
            base.InitBody();
            Title = "the pirate captain";
        }
        public override void InitOutfit()
        {
            base.InitOutfit();
            Item hat = FindItemOnLayer(Layer.Helm);
            if (hat != null)
                hat.Delete();

            hat = CaptainsHat("captain's hat");
            hat.LootType = LootType.Newbied;
            AddItem(hat);
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGem();
                PackMagicEquipment(1, 3, 0.80, 0.80);
                PackMagicEquipment(1, 3, 0.10, 0.10);
                PackGold(600, 700);

                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);

                // Froste: 12% random IOB drop
                if (0.12 > Utility.RandomDouble())
                {
                    Item iob = null;
                    if (0.75 > Utility.RandomDouble())
                        iob = Loot.RandomIOB();
                    else
                        iob = CaptainsHat("a pirate captain's hat");
                    PackItem(iob);
                }

                // pack bulk reg
                PackItem(new MandrakeRoot(Utility.RandomMinMax(10, 20)));

                if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                {
                    // 30% boost to gold
                    PackGold(base.GetGold() / 3);
                }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {
                    // ai special
                }
            }
        }

        public PirateCaptain(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            if (base.Version == 0)
                return;

            int version = reader.ReadInt();
        }
    }
}