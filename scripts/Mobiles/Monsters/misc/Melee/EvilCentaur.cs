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

/* Scripts/Mobiles/Monsters/Misc/Melee/EvilCentaur.cs
 * ChangeLog
 *  07/02/06, Kit
 *		InitBody/InitOutfit additions, changed rangefight to 6
 *  08/29/05 TK
 *		Changed AIType to Archer
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	3/5/05, Adam
 *		1. First time checkin - based on centaur.cs
 *		2. Add healing
 *		3. Make evil (red)
 *		4. set FightMode to "Weakest". This is anti-bard code :)
 *		5. Add neg karma for kill
 *		6. reduce arrows from 80-90 to 20-30
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a centaur corpse")]
    public class EvilCentaur : BaseCreature
    {
        [Constructable]
        public EvilCentaur()
            : base(AIType.AI_Archer, FightMode.All | FightMode.Weakest, 10, 6, 0.2, 0.4)
        {

            BaseSoundID = 679;

            SetStr(202, 300);
            SetDex(104, 260);
            SetInt(91, 100);

            SetHits(130, 172);

            SetDamage(13, 24);

            SetSkill(SkillName.Anatomy, 95.1, 115.0);
            SetSkill(SkillName.Archery, 95.1, 100.0);
            SetSkill(SkillName.MagicResist, 50.3, 80.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 95.1, 100.0);

            Fame = 6500;
            Karma = -6500;

            InitBody();
            InitOutfit();

            VirtualArmor = 50;


            PackItem(new Arrow(Utility.RandomMinMax(20, 30)), lootType: LootType.UnStealable);
            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
        }

        public override int Meat { get { return 1; } }
        public override int Hides { get { return 8; } }
        public override HideType HideType { get { return HideType.Spined; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)); } }

        public override void InitBody()
        {
            Name = NameList.RandomName("centaur");
            Body = 101;
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            AddItem(new Bow());

        }
        public EvilCentaur(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(180, 250);
                PackGem();
                PackMagicEquipment(1, 2, 0.15, 0.15);
                // Category 2 MID
                PackMagicItem(1, 1, 0.05);
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
                {   // Standard RunUO
                    // ai special
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

            if (BaseSoundID == 678)
                BaseSoundID = 679;
        }
    }
}