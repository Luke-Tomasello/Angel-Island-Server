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

/* Scripts/Mobiles/Wind Elders/CouncilElder.cs
 * ChangeLog
 *  12/03/06 Taran Kain
 *      Set Female to be false - no tranny council elders!
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 8 lines removed.
 *  6/05/05, Kit
 *		Switched to use new CouncilMemberAI based off of HumanMageAI
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *		Added code to make sure not dropping robe out of iobregion
 *	1/16/05, Adam
 *			Up Wrestling from 100.0, 110.0 to 160.0, 170.0
 *			This change allows the Council Member to avoid excessive interruptions
 *	1/15/05, Adam
 *		Fix the elder so he actually uses that staff 
 *		Remove staff.Layer = Layer.Earrings;
 *		Add staff.Movable = false
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/16/04, Froste
 *      Changed IOBAlignment to Council
 *  11/10/04, Froste
 *      Implemented new random IOB drop system and changed drop change to 12%
 *	11/05/04, Pigpen
 *		Made changes for Implementation of IOBSystem. Changes include:
 *		Removed IsEnemy and Aggressive Action Checks. These are now handled in BaseCreature.cs
 *		Set Creature IOBAlignment to Undead.
 *	9/26/04, Adam
 *		Add 5% IOB drop (BloodDrenchedBandana)
 *	8/3/04, Adam
 *		Update Stats, Skills, and Damage/Resist values to be more consistent.
 *	7/21/04, mith
 *		IsEnemy() and AggressiveAction() code added to support Brethren property of BloodDrenchedBandana.
 *	7/13/04 smerX
 *		Changed AIType to AIType.AI_Council (formerly AIType.AI_Mage)
 *	7/13/04, adam
 *		1. reduce chance to drop robes and sandies from 7% to 5%
 *	7/13/04 smerX
 *		Upgraded AI
 *		Changed FightMode to Strongest (formerly Closest)
 *	7/8/04 smerX
 *		Fixxed issue with shrouds being set LootType.Blessed by default
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 * Created 5/5/04 by mith
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a corpse of Kahn Anthias")]
    public class CouncilElder : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Council }); } }

        [Constructable]
        public CouncilElder()
            : base(AIType.AI_CouncilMember, FightMode.All | FightMode.Strongest, 10, 1, 0.2, 0.4)
        {
            Name = "Kahn Anthias, ";
            Title = "Elder of the Mystic Council";
            Female = false;
            Body = 0x190;
            IOBAlignment = IOBAlignment.Council;
            ControlSlots = 9;
            BardImmune = true;

            HoodedShroudOfShadows shroud = new HoodedShroudOfShadows();
            shroud.Hue = 0x4D3;
            shroud.Name = "Tattered Elder's Robe";

            // adam: reduce chance to 5% from 7% drop
            if (Utility.RandomDouble() <= 0.95)
            {
                shroud.LootType = LootType.Newbied;
            }
            else
            {
                shroud.LootType = LootType.Regular;
            }

            AddItem(shroud);

            Sandals sandals = new Sandals(0x1);

            // adam: reduce chance to 5% from 7% drop
            if (Utility.RandomDouble() <= 0.95)
                sandals.LootType = LootType.Newbied;

            AddItem(sandals);

            GnarledStaff staff = new GnarledStaff();
            staff.LootType = LootType.Newbied;
            staff.Movable = false;
            AddItem(staff);

            SetStr(216, 305);
            SetDex(96, 115);
            SetInt(966, 1045);

            SetHits(560, 595);
            SetDamage(15, 27);

            SetSkill(SkillName.EvalInt, 120.1, 130.0);
            SetSkill(SkillName.Magery, 120.1, 130.0);
            SetSkill(SkillName.MagicResist, 175.2, 200.0);
            SetSkill(SkillName.Meditation, 100.1, 101.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 160.0, 170.0);
            SetSkill(SkillName.Poisoning, 100.1, 101.0);
            SetSkill(SkillName.Macing, 75.1, 100.0);

            Fame = 20000;
            Karma = -20000;

            VirtualArmor = 60;
        }
        public override Characteristics MyCharacteristics { get { return base.MyCharacteristics & ~Characteristics.DamageSlows; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : false; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }

        public override bool Uncalmable
        {
            get
            {
                if (Hits > 1)
                    Say("Peace, is it? I'll give you peace!");

                return BardImmune;
            }
        }

        public CouncilElder(Serial serial)
            : base(serial)
        {
        }
        private void AICrossShardLoot()
        {   // Angel Island creatures that exist on multiple shards use common loot
            //  i.e., that loot doesn't change across different shard configs
            PackItem(new GnarledStaff());
            PackGold(1200, 1600);
            PackScroll(6, 8);
            PackScroll(6, 8);
            PackMagicEquipment(2, 3, 0.80, 0.80);
            PackMagicEquipment(2, 3, 0.50, 0.50);
            PackReg(10);
            PackReg(10);

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

            //Kit added if not in region check to make sure not dropping iobregion rares out of region
            if (IOBRegions.GetIOBStronghold(this) != IOBAlignment)
            {
                // Adam: Remove interesting colors if not in stronghold
                Item shoes = this.FindItemOnLayer(Layer.Shoes);
                if (shoes is Sandals)
                    shoes.Hue = 0;

                // make sure the robe does not drop
                Item shroud = FindItemOnLayer(Layer.OuterTorso);
                if (shroud != null)
                    shroud.LootType = LootType.Newbied;
            }
        }
        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {   // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                AICrossShardLoot();
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
                        AICrossShardLoot();
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
        }
    }
}