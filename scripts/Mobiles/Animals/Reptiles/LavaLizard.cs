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

/* Scripts/Mobiles/Animals/Reptiles/LavaLizard.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a lava lizard corpse")]
    [TypeAlias("Server.Mobiles.Lavalizard")]
    public class LavaLizard : BaseCreature
    {
        [Constructable]
        public LavaLizard()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a lava lizard";
            Body = 0xCE;
            Hue = Utility.RandomList(0x647, 0x650, 0x659, 0x662, 0x66B, 0x674);
            BaseSoundID = 0x5A;

            SetStr(126, 150);
            SetDex(56, 75);
            SetInt(11, 20);

            SetHits(76, 90);
            SetMana(0);

            SetDamage(6, 24);

            SetSkill(SkillName.MagicResist, 55.1, 70.0);
            SetSkill(SkillName.Tactics, 60.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 40;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 80.7;
        }

        public override bool HasBreath { get { return true; } } // fire breath enabled
        public override int Hides { get { return 12; } }
        public override HideType HideType { get { return HideType.Spined; } }

        public LavaLizard(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
                PackGold(20, 50);
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020607034631/uo.stratics.com/hunters/lavalizard.shtml
                    // 20 - 50 Gold
                    if (Spawning)
                    {
                        PackGold(20, 50);
                    }
                    else
                    {
                    }
                }
                else
                {
                    if (Spawning)
                        PackItem(new SulfurousAsh(Utility.Random(4, 10)));

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