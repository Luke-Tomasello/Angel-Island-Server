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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/BoneKnightLord.cs
 * ChangeLog
 *  10/18/23, Yoar
 *      Buffed stats
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/19/04, Adam
 *		1. Create from BoneKnight.cs
 *		2. stats and loot based on a scaled down Executioner
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a bone knight lord corpse")]
    public class BoneKnightLord : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Undead }); } }

        [Constructable]
        public BoneKnightLord()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a bone knight lord";
            Body = 57;
            BaseSoundID = 451;
            IOBAlignment = IOBAlignment.Undead;
            ControlSlots = 3;

            SetStr(346, 370);
            SetDex(71, 90);
            SetInt(26, 40);

            SetHits(186, 200);

            SetDamage(13, 23);

            SetSkill(SkillName.MagicResist, 65.1, 80.0);
            SetSkill(SkillName.Tactics, 85.1, 100.0);
            SetSkill(SkillName.Wrestling, 85.1, 95.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 40;
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                switch (Utility.Random(6))
                {
                    case 0: PackItem(new PlateArms()); break;
                    case 1: PackItem(new PlateChest()); break;
                    case 2: PackItem(new PlateGloves()); break;
                    case 3: PackItem(new PlateGorget()); break;
                    case 4: PackItem(new PlateLegs()); break;
                    case 5: PackItem(new PlateHelm()); break;
                }

                PackItem(new Scimitar());
                PackItem(new WoodenShield());
                PackItem(new Bone(Utility.Random(9, 16)));

                PackGold(200, 400);
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
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(100, 250);
                    }
                    else
                    {
                        switch (Utility.Random(6))
                        {
                            case 0: PackItem(new PlateArms()); break;
                            case 1: PackItem(new PlateChest()); break;
                            case 2: PackItem(new PlateGloves()); break;
                            case 3: PackItem(new PlateGorget()); break;
                            case 4: PackItem(new PlateLegs()); break;
                            case 5: PackItem(new PlateHelm()); break;
                        }

                        PackItem(new Scimitar());
                        PackItem(new WoodenShield());
                    }
                }
                else
                {   // ai special
                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Meager);
                }
            }
        }

        public BoneKnightLord(Serial serial)
            : base(serial)
        {
        }

        public override bool OnBeforeDeath()
        {
            return base.OnBeforeDeath();
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