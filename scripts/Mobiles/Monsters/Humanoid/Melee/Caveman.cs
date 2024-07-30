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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/Caveman.cs
 * ChangeLog
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	1/6/06, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class Caveman : BaseCreature
    {
        [Constructable]
        public Caveman()
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.2, 0.4)
        {
            SpeechHue = Utility.RandomSpeechHue();
            Hue = 33770;    // best looking color - tribe should be same hue
            IOBAlignment = IOBAlignment.None;
            ControlSlots = 2;

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(23, 27);

            SetSkill(SkillName.Fencing, 80.0, 92.5);        // not used
            SetSkill(SkillName.Macing, 80.0, 92.5);
            SetSkill(SkillName.Tactics, 80.0, 92.5);
            SetSkill(SkillName.Wrestling, 99.9, 130.5); // for the womenz!
            SetSkill(SkillName.Poisoning, 60.0, 82.5);  // not used
            SetSkill(SkillName.MagicResist, 77.5, 96.0);    // not used

            Fame = 0;
            Karma = 0;

            InitBody();
            InitOutfit();
            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);

        }

        public override bool AlwaysAttackable { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : false; } }
        public override bool ClickTitle { get { return false; } }
        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(Utility.RandomMinMax(13, 16)); } }

        public Caveman(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            if (this.Female = Utility.RandomBool())
            {
                Body = 0x191;
                Title = "the cavewoman";
                Name = NameList.RandomName("caveman_female");

            }
            else
            {
                Body = 0x190;
                Title = "the caveman";
                Name = NameList.RandomName("caveman_male");
            }
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            if (this.Female)
            {

                AddItem(new Kilt(Utility.RandomList(1863, 1836, 1133, 1141)));
                int hairHue = Utility.RandomList(1140, 1108, 1140, 1147, 1149, 1175, 2412);
                Item hair = new LongHair();
                hair.Hue = hairHue;
                hair.Layer = Layer.Hair;
                hair.Movable = false;
                AddItem(hair);
            }
            else
            {
                AddItem(new Kilt(Utility.RandomList(1863, 1836, 1133, 1141)));
                int hairHue = Utility.RandomList(1140, 1108, 1140, 1147, 1149, 1175, 2412);
                Item hair = new LongHair();
                hair.Hue = hairHue;
                hair.Layer = Layer.Hair;
                hair.Movable = false;
                AddItem(hair);

                // MediumLongBeard seems to be a long beard + Mustache
                Item beard = new MediumLongBeard();
                beard.Hue = hairHue;
                beard.Movable = false;
                beard.Layer = Layer.FacialHair;
                AddItem(beard);

                // Adam: I'll probably get in trouble for this :0
                if (this.Female == false)
                {
                    // 10% staff, 90% club
                    if (Utility.RandomDouble() < 0.10)
                        AddItem(new GnarledStaff());
                    else
                        AddItem(new Club());
                }
            }
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                if (this.Female == true)
                    PackGold(60, 80);       // women have no weapons
                else
                    PackGold(100, 150);

                switch (Utility.Random(3))
                {
                    case 0: PackItem(new Fish()); break;
                    case 1: AddItem(new ChickenLeg()); break;
                    case 2: AddItem(new CookedBird()); break;
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