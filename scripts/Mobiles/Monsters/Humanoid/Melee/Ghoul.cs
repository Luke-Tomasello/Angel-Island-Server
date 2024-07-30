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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/Ghoul.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/16/04, Froste
 *      Added IOBAlignment=IOBAlignment.Undead, added the random IOB drop to loot
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a ghostly corpse")]
    public class Ghoul : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Undead }); } }

        [Constructable]
        public Ghoul()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a ghoul";
            Body = 153;
            BaseSoundID = 0x482;
            IOBAlignment = IOBAlignment.Undead;
            ControlSlots = 1;

            SetStr(76, 100);
            SetDex(76, 95);
            SetInt(36, 60);

            SetHits(46, 60);
            SetMana(0);

            SetDamage(7, 9);

            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 55.0);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 28;
        }

        public override Poison PoisonImmune { get { return Poison.Regular; } }

        public Ghoul(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackItem(new Bone());
                PackGold(10, 50);
                PackItem(Loot.RandomWeapon());
                // Froste: 12% random IOB drop
                if (0.12 > Utility.RandomDouble())
                {
                    Item iob = Loot.RandomIOB();
                    PackItem(iob);
                }

                if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                {
                    // 30% boost to gold
                    PackGold(base.GetGold() / 3);
                }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {
                    // http://web.archive.org/web/20020221204724/uo.stratics.com/hunters/ghoul.shtml
                    // 50 to 150 Gold, Magic items, Gems, Sometimes Bone Armor

                    if (Spawning)
                    {
                        PackGold(50, 150);
                    }
                    else
                    {
                        PackMagicItem(1, 1, 0.05);
                        PackGem();
                        if (0.12 > Utility.RandomDouble())
                            switch (Utility.Random(5))
                            {
                                case 0: PackItem(new BoneArms()); break;
                                case 1: PackItem(new BoneChest()); break;
                                case 2: PackItem(new BoneGloves()); break;
                                case 3: PackItem(new BoneLegs()); break;
                                case 4: PackItem(new BoneHelm()); break;
                            }
                    }

                    // Note: The loot is way different just a few months later
                    // http://web.archive.org/web/20020806210220/uo.stratics.com/hunters/ghoul.shtml
                    // 10 to 50 Gold, A Random Weapon, Bones
                }
                else
                {
                    if (Spawning)
                        PackItem(Loot.RandomWeapon());

                    AddLoot(LootPack.Meager);
                }
            }
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.FeyAndUndead; }
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