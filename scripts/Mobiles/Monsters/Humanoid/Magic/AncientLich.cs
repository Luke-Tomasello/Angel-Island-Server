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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/AncientLich.cs
 * ChangeLog
 *	4/12/09, Adam
 *		Update special armor drop to not use SDrop system
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  7/08/06, Kit
 *		Added CorpseSkin Bustier/shorts/skirt
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 8 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 * 	4/11/05, Adam
 *		Update to use new version of Loot.ImbueWeaponOrArmor()
 *	3/28/05, Adam 
 *		Use weighted table selection code for weapon/armor attr in Loot.cs
 *	3/21/05, Adam
 *		Cleaned up weighted table selection code for weapon/armor attr
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
 *  9/14/04, Pigpen
 *		Removed Treasure Map as loot.
 *  7/30/04
 *		Add the Corpse Skin (rare) armor to this mini-boss.
 *		10% chance to drop a piece
 *		5% chance to drop a helm
 *  7/24/04, Adam
 *		add 25% chance to get a Random Slayer Instrument
 *	7/21/04, mith
 *		IsEnemy() and AggressiveAction() code added to support Brethren property of BloodDrenchedBandana.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	6/26/04 Adam: liches carry IDWands. It's historical man!
 *		20% chance to get an IDWand
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an ancient lich corpse")] // TODO: Corpse name?
    public class AncientLich : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Council }); } }

        private Mobile m_Jadey;
        private Mobile m_Adam;
        private bool m_SpawnedGuardians;

        [Constructable]
        public AncientLich()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = NameList.RandomName("ancient lich");
            Body = 78;
            BaseSoundID = 412;
            IOBAlignment = IOBAlignment.Council;
            ControlSlots = 9;

            SetStr(216, 305);
            SetDex(96, 115);
            SetInt(966, 1045);

            SetHits(560, 595);

            SetDamage(15, 27);

            SetSkill(SkillName.EvalInt, 120.1, 130.0);
            SetSkill(SkillName.Magery, 120.1, 130.0);
            SetSkill(SkillName.Meditation, 100.1, 101.0);
            SetSkill(SkillName.Poisoning, 100.1, 101.0);
            SetSkill(SkillName.MagicResist, 175.2, 200.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 75.1, 100.0);

            Fame = 23000;
            Karma = -23000;

            VirtualArmor = 60;
        }

        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public AncientLich(Serial serial)
            : base(serial)
        {
        }

        public void CheckGuardians()
        {
            if (!m_SpawnedGuardians)
            {
                // not really necessary for now, i guess?
                //
                /*switch( Utility.Random( 2 )
				{
					case 0: this.Say( "You evil defiler! You shall not escape my minions!" ); break;
					case 1: this.Say( " Foul fiend, my guardians will show you the meaning of respect!" ); break
					case 2: this.Say( "Be gone, fool, or your soul is mine!" ); break;
				}*/

                m_Jadey = new LadyGuardian();
                m_Adam = new LordGuardian();

                ((BaseCreature)m_Jadey).Team = this.Team;
                ((BaseCreature)m_Adam).Team = this.Team;

                m_Jadey.RawStr = Utility.Random(90, 110);
                m_Adam.RawStr = Utility.Random(120, 150);

                m_Jadey.MoveToWorld(this.Location, this.Map);
                m_Adam.MoveToWorld(this.Location, this.Map);

                m_Jadey.Combatant = this.Combatant;
                m_Adam.Combatant = this.Combatant;

                m_SpawnedGuardians = true;
            }
            else if (m_Jadey != null && m_Adam != null && m_Jadey.Deleted && m_Adam.Deleted)
            {
                m_Jadey = null;
                m_Adam = null;
            }
        }

        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                CheckGuardians();

                if (m_Jadey != null && m_Adam != null)
                    amount = 1;
            }

            base.Damage(amount, from, source_weapon);
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(1200, 1600);
                PackScroll(6, 8);
                PackScroll(6, 8);
                PackReg(10);
                PackReg(10);

                PackItem(new GnarledStaff());

                // Adam: liches carry IDWands. It's historical man!
                //	20% chance to get an IDWands
                if (Utility.RandomDouble() < 0.20)
                    PackItem(Loot.IDWand());

                // Froste: 12% random IOB drop
                if (0.12 > Utility.RandomDouble())
                {
                    Item iob = Loot.RandomIOB();
                    PackItem(iob);
                }

                // adam: add 25% chance to get a Random Slayer Instrument
                PackSlayerInstrument(.25);

                // Pigpen: Add the CorpseSkin (rare) armor to this mini-boss.
                if (Core.RuleSets.MiniBossArmor())
                    DoMiniBossArmor();

                // Use our unevenly weighted table for chance resolution
                Item item;
                item = Loot.RandomArmorOrShieldOrWeapon();
                PackItem(Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level6 /*6*/, 0, false));

                if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                {
                    // 30% boost to gold
                    PackGold(base.GetGold() / 3);
                }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20011218043901/uo.stratics.com/hunters/ancientlich.shtml
                    // 1500 Gold, Reagents, Magic Items, Bone Armor
                    if (Spawning)
                    {
                        PackGold(1500);
                    }
                    else
                    {
                        // Pigpen: Add the CorpseSkin (rare) armor to this mini-boss.
                        if (Core.RuleSets.MiniBossArmor())
                            DoMiniBossArmor();

                        PackReg(30, 275);
                        if (Utility.RandomBool())
                            PackMagicEquipment(2, 3);
                        else
                            PackMagicItem(2, 3, 0.80);

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
                        if (Core.RuleSets.AOSRules())
                            PackNecroReg(30, 275);

                        PackItem(new GnarledStaff());
                    }

                    AddLoot(LootPack.FilthyRich, 3);
                    AddLoot(LootPack.MedScrolls, 2);
                }
            }
        }
        private void DoMiniBossArmor()
        {
            if (Utility.Chance(0.30))
            {
                switch (Utility.Random(10))
                {
                    case 0: PackItem(new CorpseSkinArmor(), no_scroll: true); break;     // female chest
                    case 1: PackItem(new CorpseSkinArms(), no_scroll: true); break;      // arms
                    case 2: PackItem(new CorpseSkinTunic(), no_scroll: true); break;     // male chest
                    case 3: PackItem(new CorpseSkinGloves(), no_scroll: true); break;    // gloves
                    case 4: PackItem(new CorpseSkinGorget(), no_scroll: true); break;    // gorget
                    case 5: PackItem(new CorpseSkinLeggings(), no_scroll: true); break;  // legs
                    case 6: PackItem(new CorpseSkinHelm(), no_scroll: true); break;      // helm
                    case 7: PackItem(new CorpseSkinBustier(), no_scroll: true); break;   //bustier
                    case 8: PackItem(new CorpseSkinShorts(), no_scroll: true); break;    //shorts
                    case 9: PackItem(new CorpseSkinSkirt(), no_scroll: true); break;     //skirt
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