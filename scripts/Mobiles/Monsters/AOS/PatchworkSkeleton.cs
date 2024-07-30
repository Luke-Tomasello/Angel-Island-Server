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

/* Scripts/Mobiles/Monsters/AOS/PatchworkSkeleton.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *	11/19/04, Adam
 *		IOBAlignment = IOBAlignment.Undead;
 *		Bumped the min gold to 100 from 20. Max is still 150
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a patchwork skeleton corpse")] // TODO: Corpse name?
    public class PatchworkSkeleton : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Undead }); } }

        public override WeaponAbility GetWeaponAbility()
        {
            return WeaponAbility.Dismount;
        }

        [Constructable]
        public PatchworkSkeleton()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a patchwork skeleton";
            Body = 309;
            BaseSoundID = 0x48D;
            IOBAlignment = IOBAlignment.Undead;
            ControlSlots = 1;

            SetStr(96, 120);
            SetDex(71, 95);
            SetInt(16, 40);

            SetHits(58, 72);

            SetDamage(18, 22);

            SetSkill(SkillName.MagicResist, 70.1, 95.0);
            SetSkill(SkillName.Tactics, 55.1, 80.0);
            SetSkill(SkillName.Wrestling, 50.1, 70.0);

            Fame = 500;
            Karma = -500;

            VirtualArmor = 54;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }

        public PatchworkSkeleton(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                PackGem();
                PackGold(100, 150);

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
            else if (Core.RuleSets.AllShards)
            {   // https://web.archive.org/web/20211026173029/https://uo.stratics.com/database/view.php?db_content=hunters&id=683
                //  50-100 gold
                PackGold(50, 100);
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