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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/Savage.cs
 *	ChangeLog:
 * 	3/23/23, Yoar
 * 		IsEnemy, AggressiveAction: Added 'BodyMod == 0' check so that only human body values can be considered savage kin.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 1 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	1/15/05, Adam
 *		Remove drop of 'plain' mask
 *	12/15/04, Pix
 *		Removed damage mod to big pets.
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/10/04, Froste
 *      Implemented new random IOB drop system and changed drop change to 12%
 *	11/05/04, Pigpen
 *		Made changes for Implementation of IOBSystem. Changes include:
 *		Removed IsEnemy and Aggressive Action Checks. These are now handled in BaseCreature.cs
 *		Set Creature IOBAlignment to Savage.
 *	10/1/04, Adam
 *		Change BandageDelay from 10 to 10-13
 *	0/19/04, Adam
 *		Have Savages Rummage Corpses
 *	8/27/04, Adam
 *		Have all savages wear masks, but only 5% drop
 *	8/23/04, Adam
 *		Increase gold to 100-150
 *		Increase berry drop to 20% OR bola to 5%
 *		Decrease tribal mask drop to 5% OR orkish mask 5%
 *	6/11/04, mith
 *		Moved the equippable combat items out of OnBeforeDeath()
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/23/04 smerX
 *		Enabled healing
 *
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a savage corpse")]
    public class Savage : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Savage }); } }

        [Constructable]
        public Savage()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {

            IOBAlignment = IOBAlignment.Savage;
            ControlSlots = 2;

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(23, 27);


            SetSkill(SkillName.Fencing, 60.0, 82.5);
            SetSkill(SkillName.Macing, 60.0, 82.5);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 57.5, 80.0);
            SetSkill(SkillName.Swords, 60.0, 82.5);
            SetSkill(SkillName.Tactics, 60.0, 82.5);

            Fame = 1000;
            Karma = -1000;

            InitBody();
            InitOutfit();

            if (Core.RuleSets.AngelIslandRules())
                PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
        }

        public override int Meat { get { return 1; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : false; } }

        public override bool CanBandage { get { return Core.RuleSets.AngelIslandRules() ? true : base.CanBandage; } }
        public override TimeSpan BandageDelay { get { return Core.RuleSets.AngelIslandRules() ? TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)) : base.BandageDelay; } }

        public override void InitBody()
        {
            Name = NameList.RandomName("savage");

            if (Female = Utility.RandomBool())
                Body = 184;
            else
                Body = 183;
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            AddItem(new Spear());
            AddItem(new BoneArms());
            AddItem(new BoneLegs());

            if (Core.RuleSets.AngelIslandRules())
            {
                // all savages wear a mask, but won't drop this one
                //	see GenerateLoot for the mask they do drop
                AddItem(new SavageMask(), 1.0);
            }
            else
            {
                if (0.5 > Utility.RandomDouble())
                    AddItem(new SavageMask());
                else
                {
                    // http://www.uoguide.com/Savage_Empire
                    // http://uo.stratics.com/secrets/archive/orcsavage.shtml
                    if (PublishInfo.PublishDate >= Core.EraSAVE)
                        if (0.1 > Utility.RandomDouble())
                            AddItem(new OrcishKinMask());   // TODO: Adam. Is this right? the savage will 'wear' the orc mask?
                                                            // if not we need to move this to GenerateLoot()
                }
            }

        }

        public Savage(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(100, 150);

                // berry or bola
                if (Female)
                {
                    if (Utility.RandomDouble() < 0.30)
                        PackItem(new TribalBerry());
                }
                else
                {
                    if (Utility.RandomDouble() < 0.05)
                        PackItem(new BolaBall());
                }

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
                {   // http://web.archive.org/web/20020606081710/uo.stratics.com/hunters/savage.shtml
                    // 20-40 Gold, bandages, bone arms, bone legs, spear, bola balls, savage tribal mask, mask of orcish kin, tribal berry.
                    if (Spawning)
                    {
                        PackGold(20, 40);
                    }
                    else
                    {
                        PackItem(new Bandage(Utility.RandomMinMax(1, 15)));

                        // bone arms dropped as part of dress
                        // bone legs dropped as part of dress
                        // spear dropped as part of dress

                        // http://www.uoguide.com/Savage_Empire
                        // http://uo.stratics.com/secrets/archive/orcsavage.shtml
                        // Bola balls have appeared as loot on Orc Bombers. Balls on Bombers are rather common, around a 50/50% chance of getting a ball or not. They are only appearing as loot on bombers.
                        if (PublishInfo.PublishDate >= Core.EraSAVE)
                        {
                            if (Female && 0.1 > Utility.RandomDouble())
                                PackItem(new TribalBerry());
                            else if (!Female && 0.1 > Utility.RandomDouble())
                                PackItem(new BolaBall());

                        }

                        // savage tribal mask dropped as part of dress
                        // mask of orish kin dropped as part of dress
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        // bone arms dropped as part of dress
                        // bone legs dropped as part of dress
                        // spear dropped as part of dress

                        // http://www.uoguide.com/Savage_Empire
                        // http://uo.stratics.com/secrets/archive/orcsavage.shtml
                        // Bola balls have appeared as loot on Orc Bombers. Balls on Bombers are rather common, around a 50/50% chance of getting a ball or not. They are only appearing as loot on bombers.
                        if (PublishInfo.PublishDate >= Core.EraSAVE)
                        {
                            if (Female && 0.1 > Utility.RandomDouble())
                                PackItem(new TribalBerry());
                            else if (!Female && 0.1 > Utility.RandomDouble())
                                PackItem(new BolaBall());

                        }

                        // savage tribal mask dropped as part of dress
                        // mask of orish kin dropped as part of dress

                        PackItem(new Bandage(Utility.RandomMinMax(1, 15)));
                    }

                    AddLoot(LootPack.Meager);
                }
            }
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
                // Ai uses HUE value and not the BodyMod as there is no sitting graphic
                if ((m.BodyMod == 183 || m.BodyMod == 184) || (m.BodyMod == 0 && m.HueMod == 0))
                    return false;

            return base.IsEnemy(m, filter);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            base.AggressiveAction(aggressor, criminal);

            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
            {   // Ai uses HUE value and not the BodyMod as there is no sitting graphic
                if ((aggressor.BodyMod == 183 || aggressor.BodyMod == 184) || (aggressor.BodyMod == 0 && aggressor.HueMod == 0))
                {
                    AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0, aggressor);
                    aggressor.BodyMod = 0;
                    aggressor.HueMod = -1;
                    aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                    aggressor.PlaySound(0x307);
                    aggressor.SendLocalizedMessage(1040008); // Your skin is scorched as the tribal paint burns away!

                    if (aggressor is PlayerMobile)
                        ((PlayerMobile)aggressor).SavagePaintExpiration = TimeSpan.Zero;
                }
            }
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.SavagesAndOrcs; }
        }

        public override void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
            if (!Core.RuleSets.AngelIslandRules())
                if (to is Dragon || to is WhiteWyrm || to is SwampDragon || to is Drake || to is Nightmare || to is Daemon)
                    damage *= 3;
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