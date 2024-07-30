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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/SavageRider.cs
 * ChangeLog
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
 *  03/14/05, Lego
 *          added the ability to bandage.
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
 *		Set BearMask drop to set mask to IOBAlignment Savage.
 *	8/23/04, Adam
 *		Increase gold to 125-175
 *		Add berry drop of 20%
 *		decrease the drop of the tribal spears 10%
 *		decrease bola drop to 5%
 *	7/29/04 smerX
 *		Set BearMask drop to 5%
 *	7/29/04, mith
 *		Included BearMask()
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	6/11/04, mith
 *		Moved the equippable combat items out of OnBeforeDeath()
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a savage corpse")]
    public class SavageRider : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Savage }); } }

        [Constructable]
        public SavageRider()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {

            IOBAlignment = IOBAlignment.Savage;
            ControlSlots = 3;

            SetStr(151, 170);
            SetDex(92, 130);
            SetInt(51, 65);

            SetDamage(29, 34);


            SetSkill(SkillName.Fencing, 72.5, 95.0);
            SetSkill(SkillName.Healing, 60.3, 90.0);
            SetSkill(SkillName.Macing, 72.5, 95.0);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 72.5, 95.0);
            SetSkill(SkillName.Swords, 72.5, 95.0);
            SetSkill(SkillName.Tactics, 72.5, 95.0);

            Fame = 1000;
            Karma = -1000;

            InitBody();
            InitOutfit();

            new SavageRidgeback().Rider = this;

            if (Core.RuleSets.AngelIslandRules())
                PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
        }

        public override int Meat { get { return 1; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }

        public override bool CanBandage { get { return Core.RuleSets.AngelIslandRules() ? true : base.CanBandage; } }
        public override TimeSpan BandageDelay { get { return Core.RuleSets.AngelIslandRules() ? TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)) : base.BandageDelay; } }

        public override void InitBody()
        {
            Name = NameList.RandomName("savage rider");

            if (Female = Utility.RandomBool())
                Body = 186;
            else
                Body = 185;
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            if (Core.RuleSets.AngelIslandRules())
            {
                AddItem(new BoneArms());
                AddItem(new BoneLegs());
                AddItem(new BearMask(), 1);         // always newbie on AI (they will need to get the equiv IOB)
                AddItem(new TribalSpear(), 0.90);   // newbie 90% of the time (10% drop)
            }
            else
            {
                AddItem(new BoneArms());
                AddItem(new BoneLegs());
                AddItem(new TribalSpear(), 0.90);   // TODO: newbie 90% of the time (10% drop)
                AddItem(new BearMask(), 0.90);      // TODO: newbie 90% of the time (10% drop)
            }
        }
        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(125, 175);

                if (Utility.RandomDouble() < 0.05)
                    PackItem(new BolaBall());

                if (Utility.RandomDouble() < 0.30)
                    PackItem(new TribalBerry());

                // Froste: 12% random IOB drop
                if (0.12 > Utility.RandomDouble())
                {
                    Item iob = Loot.RandomIOB();
                    PackItem(iob);
                }

                // Category 2 MID
                PackMagicItem(1, 1, 0.05);

                if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                {
                    // 30% boost to gold
                    PackGold(base.GetGold() / 3);
                }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020606081839/uo.stratics.com/hunters/savage_rider.shtml
                    // 20-40 Gold, bandages, bone arms, bone legs, bear mask, bola balls, tribal spear.
                    if (Spawning)
                    {
                        PackGold(20, 40);
                    }
                    else
                    {
                        PackItem(new Bandage(Utility.Random(3, 12)));
                        // arms dropped as dress
                        // legs dropped as dress
                        // bear mask dropped as dress

                        // http://www.uoguide.com/Savage_Empire
                        // http://uo.stratics.com/secrets/archive/orcsavage.shtml
                        // Bola balls have appeared as loot on Orc Bombers. Balls on Bombers are rather common, around a 50/50% chance of getting a ball or not. They are only appearing as loot on bombers.
                        if (PublishInfo.PublishDate >= Core.EraSAVE)
                            if (0.2 > Utility.RandomDouble())
                                PackItem(new BolaBall());

                        // spear dropped as dress
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackItem(new Bandage(Utility.RandomMinMax(1, 15)));

                        // arms dropped as dress
                        // legs dropped as dress
                        // bear mask dropped as dress

                        // http://www.uoguide.com/Savage_Empire
                        // http://uo.stratics.com/secrets/archive/orcsavage.shtml
                        // Bola balls have appeared as loot on Orc Bombers. Balls on Bombers are rather common, around a 50/50% chance of getting a ball or not. They are only appearing as loot on bombers.
                        if (PublishInfo.PublishDate >= Core.EraSAVE)
                            if (0.2 > Utility.RandomDouble())
                                PackItem(new BolaBall());

                        // spear dropped as dress
                    }

                    AddLoot(LootPack.Average);
                }
            }
        }

        public override bool OnBeforeDeath()
        {
            IMount mount = this.Mount;

            if (mount != null)
                mount.Rider = null;

            if (mount is Mobile)
                ((Mobile)mount).Delete();

            return base.OnBeforeDeath();
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.SavagesAndOrcs; }
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
            {
                // Ai uses HUE value and not the BodyMod as there is no sitting graphic
                if ((m.BodyMod == 183 || m.BodyMod == 184) || (m.BodyMod == 0 && m.HueMod == 0))
                    return false;
            }

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

        public override void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
            if (!Core.RuleSets.AngelIslandRules())
                if (to is Dragon || to is WhiteWyrm || to is SwampDragon || to is Drake || to is Nightmare || to is Daemon)
                    damage *= 3;
        }

        public SavageRider(Serial serial)
            : base(serial)
        {
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