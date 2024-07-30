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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/OrcCaptain.cs
 * ChangeLog
 *	2/8/11, adam
 *		UOSI: the orc captain should have classic loot.. I don't know wtf RunUO is giving
 *		drop UOSP style loot UOSI.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/10/04, Froste
 *      Implemented new random IOB drop system and changed drop change to 12%
 *	11/05/04, Pigpen
 *		Made changes for Implementation of IOBSystem. Changes include:
 *		Removed IsEnemy and Aggressive Action Checks. These are now handled in BaseCreature.cs
 *		Set Creature IOBAlignment to Orcish.
 *	9/19/04, Adam
 *		Add IOB drop 5%
 *  9/16/04, Pigpen
 * 		Added IOB Functionality to item OrcishKinHelm
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles
{
    [CorpseName("an orcish corpse")]
    public class OrcCaptain : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Orc }); } }

        public override InhumanSpeech SpeechType { get { return InhumanSpeech.Orc; } }

        [Constructable]
        public OrcCaptain()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            BaseSoundID = 0x45A;
            IOBAlignment = IOBAlignment.Orcish;
            ControlSlots = 1;

            SetStr(111, 145);
            SetDex(101, 135);
            SetInt(86, 110);

            SetHits(67, 87);

            SetDamage(5, 15);

            SetSkill(SkillName.MagicResist, 70.1, 85.0);
            SetSkill(SkillName.Swords, 70.1, 95.0);
            SetSkill(SkillName.Tactics, 85.1, 100.0);

            Fame = 2500;
            Karma = -2500;

            InitBody();

            VirtualArmor = 34;
        }

        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override int Meat { get { return 1; } }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.SavagesAndOrcs; }
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
                if (m.Player && m.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
                    return false;

            return base.IsEnemy(m, filter);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            base.AggressiveAction(aggressor, criminal);

            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
            {
                Item item = aggressor.FindItemOnLayer(Layer.Helm);

                if (item is OrcishKinMask)
                {
                    AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0, item);
                    item.Delete();
                    aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                    aggressor.PlaySound(0x307);
                }
            }
        }

        public override void InitBody()
        {
            Name = NameList.RandomName("orc");
            Body = 7;
        }
        public override void InitOutfit()
        {
        }

        public OrcCaptain(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                // TODO: Skull?
                switch (Utility.Random(7))
                {
                    case 0: PackItem(new Arrow()); break;
                    case 1: PackItem(new Lockpick()); break;
                    case 2: PackItem(new Shaft()); break;
                    case 3: PackItem(new Ribs()); break;
                    case 4: PackItem(new Bandage()); break;
                    case 5: PackItem(new BeverageBottle(BeverageType.Wine)); break;
                    case 6: PackItem(new Jug(BeverageType.Cider)); break;
                }

                // Froste: 12% random IOB drop
                if (0.12 > Utility.RandomDouble())
                {
                    Item iob = Loot.RandomIOB();
                    PackItem(iob);
                }

                PackGold(50, 100);

                // Category 2 MID
                PackMagicItem(1, 1, 0.05);

                if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                {
                    // 30% boost to gold
                    PackGold(base.GetGold() / 3);
                }
            }
            else
            {   // Adam: the orc captain should have classic loot.. I don't know wtf RunUO is giving
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020607073208/uo.stratics.com/hunters/orccap.shtml
                    // 	50 to 150 Gold, Gems, Two-Handed Axe, Ringmail Tunic, Orc Helm, Thigh Boots, 1 Raw Ribs (carved)
                    if (Spawning)
                    {
                        PackGold(50, 150);
                    }
                    else
                    {
                        PackGem(1, .9);
                        PackGem(1, .05);
                        PackItem(new TwoHandedAxe());
                        PackItem(new RingmailChest());
                        PackItem(new OrcHelm());
                        PackItem(new ThighBoots());
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        // TODO: Skull?
                        switch (Utility.Random(7))
                        {
                            case 0: PackItem(new Arrow()); break;
                            case 1: PackItem(new Lockpick()); break;
                            case 2: PackItem(new Shaft()); break;
                            case 3: PackItem(new Ribs()); break;
                            case 4: PackItem(new Bandage()); break;
                            case 5: PackItem(new BeverageBottle(BeverageType.Wine)); break;
                            case 6: PackItem(new Jug(BeverageType.Cider)); break;
                        }

                        if (Core.RuleSets.AOSRules())
                            PackItem(Loot.RandomNecromancyReagent());
                    }

                    AddLoot(LootPack.Meager, 2);
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