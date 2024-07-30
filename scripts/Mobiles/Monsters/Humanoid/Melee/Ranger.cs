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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/Ranger.cs
 * ChangeLog
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions, changed rangefight to 6
 *  9/20/05, Adam
 *		Make bard immune.
 *  9/19/05, Adam
 *		a. Change Karma loss to that for a 'good' aligned creature
 *		b. remove powder of transloacation
 *  9/16/05, Adam
 *		spawned from BrigandArcher.cs
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;
using System;

namespace Server.Mobiles
{
    public class Ranger : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Militia }); } }

        [Constructable]
        public Ranger()
            : base(AIType.AI_Archer, FightMode.Aggressor | FightMode.Criminal, 10, 6, 0.2, 0.4)
        {
            SpeechHue = Utility.RandomSpeechHue();
            Title = "the ranger";
            Hue = Utility.RandomSkinHue();
            IOBAlignment = IOBAlignment.Good;
            ControlSlots = 3;
            BardImmune = true;

            SetStr(146, 200);
            SetDex(130, 170);
            SetInt(51, 65);

            SetDamage(23, 27);

            SetSkill(SkillName.Archery, 90.0, 100.5);
            SetSkill(SkillName.Macing, 60.0, 82.5);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 57.5, 80.0);
            SetSkill(SkillName.Anatomy, 80.0, 92.5);
            SetSkill(SkillName.Tactics, 80.0, 92.5);

            Fame = 1500;
            Karma = 1500;

            InitBody();
            InitOutfit();
            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
            PackItem(new FancyShirt(Utility.RandomNeutralHue()));
            PackItem(new LongPants(Utility.RandomNeutralHue()));

        }

        public override bool AlwaysMurderer { get { return false; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanRummageCorpses { get { return false; } }
        public override bool ClickTitle { get { return true; } }

        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(Utility.RandomMinMax(10, 11)); } }

        public Ranger(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            if (this.Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            Item hair = new Item(Utility.RandomList(0x203B, 0x203C, 0x203D, 0x2048));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            double chance = 0.98;
            AddItem(new RangerArms(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new RangerChest(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new RangerGloves(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new RangerGorget(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new RangerLegs(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new Boots(0x5E4), LootType.Newbied); // never drop, you need the IOB version
            AddItem(new Bow());
        }


        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(170, 220);

                PackItem(new Arrow(Utility.RandomMinMax(20, 30)));

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
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {
                    // ai special
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}