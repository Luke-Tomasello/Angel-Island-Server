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

/* Scripts/Mobiles/Animals/Rodents/GiantRat.cs
 * ChangeLog
 *  2/19/2024, Adam
 *      Add bilge rat
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 4 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("a giant rat corpse")]
    [TypeAlias("Server.Mobiles.Giantrat")]
    public class GiantRat : BaseCreature
    {
        [Constructable]
        public GiantRat()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a giant rat";
            Body = 0xD7;
            BaseSoundID = 0x188;

            SetStr(32, 74);
            SetDex(46, 65);
            SetInt(16, 30);

            SetHits(26, 39);
            SetMana(0);

            SetDamage(4, 8);

            SetSkill(SkillName.MagicResist, 25.1, 30.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
            SetSkill(SkillName.Wrestling, 29.3, 44.0);

            Fame = 300;
            Karma = -300;

            VirtualArmor = 18;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 29.1;
        }

        public override int Meat { get { return 1; } }
        public override int Hides { get { return Core.RuleSets.AngelIslandRules() ? 6 : Utility.Random(3) == 0 ? 6 : 0; } }
        public override FoodType FavoriteFood { get { return FoodType.Fish | FoodType.Meat | FoodType.FruitsAndVegies | FoodType.Eggs; } }

        public GiantRat(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
                PackGold(0, 25);
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020313115208/uo.stratics.com/hunters/giantrat.shtml
                    // 1 Raw Ribs (carved), 6 Hides (carved) (sometimes)
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
                    AddLoot(LootPack.Poor);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [CorpseName("a bilge rat corpse")]
    public class BilgeRat : GiantRat
    {
        [Constructable]
        public BilgeRat()
            : base()
        {
            Name = "a bilge rat";
            Body = 0xD7;
            BaseSoundID = 0x188;

            Utility.CopyStats(typeof(GiantRat), this, stat_multiplier: 1.5, damage_multiplier: 1.2, skill_multiplier: 1.2);

            Fame = 300;
            Karma = -300;

            Tamable = false;
        }

        public override int Meat { get { return 1; } }
        public override int Hides { get { return Core.RuleSets.AngelIslandRules() ? 6 : Utility.Random(3) == 0 ? 6 : 0; } }

        public BilgeRat(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
                PackGold(0, 25);
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020313115208/uo.stratics.com/hunters/giantrat.shtml
                    // 1 Raw Ribs (carved), 6 Hides (carved) (sometimes)
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
                    AddLoot(LootPack.Poor);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
    // 
}