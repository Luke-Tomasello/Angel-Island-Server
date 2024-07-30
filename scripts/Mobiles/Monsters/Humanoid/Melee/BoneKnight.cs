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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/BoneKnight.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
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
    [CorpseName("a bone knight corpse")]
    public class BoneKnight : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Undead }); } }

        [Constructable]
        public BoneKnight()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a bone knight";
            Body = 57;
            BaseSoundID = 451;
            IOBAlignment = IOBAlignment.Undead;
            ControlSlots = 2;

            SetStr(196, 250);
            SetDex(76, 95);
            SetInt(36, 60);

            SetHits(118, 150);

            SetDamage(8, 18);

            SetSkill(SkillName.MagicResist, 65.1, 80.0);
            SetSkill(SkillName.Tactics, 85.1, 100.0);
            SetSkill(SkillName.Wrestling, 85.1, 95.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 40;
        }

        public BoneKnight(Serial serial)
            : base(serial)
        {
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
                PackGold(100, 130);
                PackItem(new WoodenShield());
                PackItem(new Bone(Utility.Random(9, 16)));

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
                {   // http://web.archive.org/web/20020210165441/uo.stratics.com/hunters/boneknight.shtml
                    // 0 to 150 Gold, Platemail Armor, Wooden Shield, Weapon Carried

                    if (Spawning)
                    {
                        PackGold(0, 150);
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
                {   // standard runuo loot
                    if (Spawning)
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

                        PackSlayer();
                        PackItem(new Scimitar());
                        PackItem(new WoodenShield());
                    }

                    AddLoot(LootPack.Average);
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