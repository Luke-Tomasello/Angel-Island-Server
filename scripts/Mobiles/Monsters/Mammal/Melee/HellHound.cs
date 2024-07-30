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

/* Scripts/Mobiles/Monsters/Mammal/Melee/HellHound.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  06/27/06, Kit
 *		Added in missing skills, to be equivalent to standered OSI Min/Max HellHound skill values.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  9/26/04, Jade
 *      Increased gold from (1, 150) to (100, 200).
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a hell hound corpse")]
    public class HellHound : BaseCreature
    {
        [Constructable]
        public HellHound()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a hell hound";
            Body = 98;
            BaseSoundID = 229;

            SetStr(102, 150);
            SetDex(81, 105);
            SetInt(36, 60);

            SetHits(66, 125);

            SetDamage(11, 17);

            SetSkill(SkillName.MagicResist, 57.6, 75);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 3400;
            Karma = -3400;

            VirtualArmor = 30;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 85.5;
        }

        public override bool HasBreath { get { return true; } } // fire breath enabled
        public override int Meat { get { return 1; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public override PackInstinct PackInstinct { get { return PackInstinct.Canine; } }

        public HellHound(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackItem(new SulfurousAsh(5));
                PackGold(100, 200);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20021015001544/uo.stratics.com/hunters/hell_hound.shtml
                    // 0-150 gp, 5 Sulphurous Ash, 1 Raw Ribs (carved)
                    if (Spawning)
                    {
                        PackGold(0, 150);
                    }
                    else
                    {
                        PackItem(new SulfurousAsh(5));
                    }
                }
                else
                {
                    if (Spawning)
                        PackItem(new SulfurousAsh(5));

                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Meager);
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