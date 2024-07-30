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

/* ./Scripts/Mobiles/Monsters/Misc/Melee/FrostOoze.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a frost ooze corpse")]
    public class FrostOoze : BaseCreature
    {
        [Constructable]
        public FrostOoze()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a frost ooze";
            Body = 94;
            BaseSoundID = 456;

            SetStr(18, 30);
            SetDex(16, 21);
            SetInt(16, 20);

            SetHits(13, 17);

            SetDamage(3, 9);

            SetSkill(SkillName.MagicResist, 5.1, 10.0);
            SetSkill(SkillName.Tactics, 19.3, 34.0);
            SetSkill(SkillName.Wrestling, 25.3, 40.0);

            Fame = 450;
            Karma = -450;

            VirtualArmor = 38;
        }

        public FrostOoze(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGem();

                if (Utility.RandomBool())
                    PackGem();
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020806203419/uo.stratics.com/hunters/frostooze.shtml
                    // 	Gems
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                        PackGem(1, .9);
                        PackGem(1, .05);
                    }
                }
                else
                {   // Standard RunUO
                    AddLoot(LootPack.Gems, Utility.RandomMinMax(1, 2));
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