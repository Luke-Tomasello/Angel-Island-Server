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

/* Scripts/Mobiles/Monsters/Reptile/Melee/Lizardman.cs
 * ChangeLog
 *  2/9/11, Adam
 *		spawnewd from orc camp.
 */

using Server.Items;
using Server.Misc;

namespace Server.Mobiles
{
    [CorpseName("a lizardman corpse")]
    public class Lizardman : BaseCreature
    {
        public override InhumanSpeech SpeechType { get { return InhumanSpeech.Lizardman; } }

        [Constructable]
        public Lizardman()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = NameList.RandomName("lizardman");
            Body = Utility.RandomList(35, 36);
            BaseSoundID = 417;

            SetStr(96, 120);
            SetDex(86, 105);
            SetInt(36, 60);

            SetHits(58, 72);

            SetDamage(5, 7);

            SetSkill(SkillName.MagicResist, 35.1, 60.0);
            SetSkill(SkillName.Tactics, 55.1, 80.0);
            SetSkill(SkillName.Wrestling, 50.1, 70.0);

            Fame = 1500;
            Karma = -1500;

            VirtualArmor = 28;
        }

        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override int Meat { get { return 1; } }
        public override int Hides { get { return 12; } }
        public override HideType HideType { get { return HideType.Spined; } }

        public Lizardman(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
                PackGold(25, 50);
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020213041111/uo.stratics.com/hunters/lizardman.shtml
                    // 0 to 50 Gold, Weapon Carried, 1 Raw Ribs (carved), 12 Hides (carved)

                    if (Spawning)
                    {
                        PackGold(0, 50);
                    }
                    else
                    {
                        // Weapon Carried
                        if (this.Body == 0x23)
                            PackItem(new ShortSpear());
                        else if (this.Body == 0x24)
                            PackItem(new WarMace());
                    }
                }
                else
                {
                    AddLoot(LootPack.Meager);
                    // TODO: weapon
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