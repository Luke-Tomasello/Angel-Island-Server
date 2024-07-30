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

/* ./Scripts/Mobiles/Animals/Town Critters/Rat.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 3 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a rat corpse")]
    public class Rat : BaseCreature
    {
        [Constructable]
        public Rat()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.3, 0.6)
        {
            Name = "a rat";
            Body = 238;
            BaseSoundID = 0xCC;

            SetStr(9);
            SetDex(35);
            SetInt(5);

            SetHits(6);
            SetMana(0);

            SetDamage(1, 2);

            SetSkill(SkillName.MagicResist, 4.0);
            SetSkill(SkillName.Tactics, 4.0);
            SetSkill(SkillName.Wrestling, 4.0);

            Fame = 150;
            Karma = -150;

            VirtualArmor = 6;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -0.9;
        }

        public override int Meat { get { return 1; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat | FoodType.Fish | FoodType.Eggs | FoodType.GrainsAndHay; } }

        public Rat(Serial serial)
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
                {   // http://web.archive.org/web/20020202091202/uo.stratics.com/hunters/rat.shtml
                    // 	1 Raw Ribs (carved)
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
}