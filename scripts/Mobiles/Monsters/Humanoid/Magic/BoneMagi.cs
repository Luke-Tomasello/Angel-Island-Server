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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/BoneMagi.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/16/04, Froste
 *      Added IOBAlignment=IOBAlignment.Undead, added the random IOB drop to loot
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a bone magi corpse")]
    public class BoneMagi : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Undead }); } }

        [Constructable]
        public BoneMagi()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a bone magi";
            Body = 148;
            BaseSoundID = 451;
            IOBAlignment = IOBAlignment.Undead;
            ControlSlots = 2;

            SetStr(76, 100);
            SetDex(56, 75);
            SetInt(80, 100);

            SetHits(46, 60);

            SetDamage(3, 7);

            SetSkill(SkillName.EvalInt, 60.1, 70.0);
            SetSkill(SkillName.Magery, 60.1, 70.0);
            SetSkill(SkillName.MagicResist, 55.1, 70.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 55.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 38;
        }

        public override Poison PoisonImmune { get { return Poison.Regular; } }

        public BoneMagi(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(60, 90);
                PackScroll(1, 5);
                PackReg(3);
                PackItem(new Bone(Utility.Random(10, 12)));
                // Category 2 MID
                PackMagicItem(1, 1, 0.05);

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
                {   // http://web.archive.org/web/20020212101724/uo.stratics.com/hunters/bonemagi.shtml
                    // 50 to 200 Gold, Magic items, Gems, Scrolls, Bone reagent

                    if (Spawning)
                    {
                        PackGold(50, 200);
                    }
                    else
                    {
                        PackMagicItem(1, 1, 0.05);
                        PackGem(1, .9);
                        PackGem(1, .05);
                        PackScroll(1, 5);
                        PackItem(new Bone());

                        // 6/23/2023, Adam: NON STANDARD LOOT
                        // 10% drop, similar to bear masks
                        if (Utility.RandomChance(10))
                            // medible, no AR, no StrReq
                            PackItem(new BoneMagiHelm(armorBase: 1, strReq: 0));
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackReg(3);
                        if (Core.RuleSets.AOSRules())
                            PackNecroReg(3, 10);
                        PackItem(new Bone());
                    }

                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.LowScrolls);
                    AddLoot(LootPack.Potions);
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