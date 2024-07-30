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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/Zombie.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 4 lines removed.
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
    [CorpseName("a rotting corpse")]
    public class Zombie : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Undead }); } }

        [Constructable]
        public Zombie()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a zombie";
            Body = 3;
            BaseSoundID = 471;
            IOBAlignment = IOBAlignment.Undead;
            ControlSlots = 1;

            SetStr(46, 70);
            SetDex(31, 50);
            SetInt(26, 40);

            SetHits(28, 42);

            SetDamage(3, 7);

            SetSkill(SkillName.MagicResist, 15.1, 40.0);
            SetSkill(SkillName.Tactics, 35.1, 50.0);
            SetSkill(SkillName.Wrestling, 35.1, 50.0);

            Fame = 600;
            Karma = -600;

            VirtualArmor = 18;
        }

        public override Poison PoisonImmune { get { return Poison.Regular; } }

        public Zombie(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackItem(new Bone());
                PackGold(25, 50);
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
                {   // http://web.archive.org/web/20021014234922/uo.stratics.com/hunters/zombie.shtml
                    // Body Parts, Sometimes Bone Armor
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                        switch (Utility.Random(10))
                        {
                            case 0: PackItem(new LeftArm()); break;
                            case 1: PackItem(new RightArm()); break;
                            case 2: PackItem(new Torso()); break;
                            case 3: PackItem(new Bone()); break;
                            case 4: PackItem(new RibCage()); break;
                            case 5: PackItem(new RibCage()); break;
                            case 6: PackItem(new BonePile()); break;
                            case 7: PackItem(new BonePile()); break;
                            case 8: PackItem(new BonePile()); break;
                            case 9: PackItem(new BonePile()); break;
                        }

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
                }
                else
                {
                    if (Spawning)
                    {
                        switch (Utility.Random(10))
                        {
                            case 0: PackItem(new LeftArm()); break;
                            case 1: PackItem(new RightArm()); break;
                            case 2: PackItem(new Torso()); break;
                            case 3: PackItem(new Bone()); break;
                            case 4: PackItem(new RibCage()); break;
                            case 5: PackItem(new RibCage()); break;
                            case 6: PackItem(new BonePile()); break;
                            case 7: PackItem(new BonePile()); break;
                            case 8: PackItem(new BonePile()); break;
                            case 9: PackItem(new BonePile()); break;
                        }
                    }

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