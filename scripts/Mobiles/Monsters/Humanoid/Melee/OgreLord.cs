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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/OgreLord.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Factions;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an ogre lords corpse")]
    public class OgreLord : BaseCreature
    {
        public override Faction FactionAllegiance { get { return Minax.Instance; } }
        public override Ethics.Ethic EthicAllegiance { get { return Ethics.Ethic.Evil; } }

        [Constructable]
        public OgreLord()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "an ogre lord";
            Body = 83;
            BaseSoundID = 427;

            SetStr(767, 945);
            SetDex(66, 75);
            SetInt(46, 70);

            SetHits(476, 552);

            SetDamage(20, 25);

            SetSkill(SkillName.MagicResist, 125.1, 140.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 50;
        }

        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override Poison PoisonImmune { get { return Poison.Regular; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }
        public override int Meat { get { return 2; } }

        public OgreLord(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGem();

                PackItem(new Club());
                PackItem(new Arrow(10));
                PackGold(550, 700);
                PackMagicEquipment(2, 3, 0.40, 0.40);
                PackMagicEquipment(2, 3, 0.15, 0.15);
                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020202091246/uo.stratics.com/hunters/ogrelord.shtml
                    // 200 to 250 Gold, Magic items, Weapon Carried, 2 Raw Ribs (carved)
                    // https://web.archive.org/web/20010414040605/http://uo.stratics.com/hunters/
                    // 200 to 250 gold, Magic items, Weapon Carried, 2 Raw Ribs (carved)
                    if (Spawning)
                    {
                        PackGold(200, 250);
                    }
                    else
                    {
                        PackMagicEquipment(2, 3);
                        PackMagicItem(1, 2, 0.05);
                        // stratics says "Weapon Carried", I think they mean a club as the ogre lord doesn't carry a weapon
                        PackItem(new Club());
                    }
                }
                else
                {
                    if (Spawning)
                        PackItem(new Club());

                    AddLoot(LootPack.Rich, 2);
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