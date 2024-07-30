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

/* Scripts/Mobiles/Townfolk/Thief.cs
 *
 * ChangeLog
 *  07/02/06, Kit
 *		InitOutfit/Body overrides
 *	5/26/04, Pixie
 *		Changed to use the new CollectBounty() call in BountyKeeper.
 *	5/23/04 created by smerX
 *
 */

using Server.BountySystem;
using Server.Diagnostics;
using Server.Items;
using System;

namespace Server.Mobiles
{
    public class Thief : BaseCreature
    {

        [Constructable]
        public Thief()
            : base(AIType.AI_Melee, FightMode.Aggressor, 22, 1, 0.2, 1.0)
        {
            InitBody();
            InitOutfit();
            Title = "the thief";

            SetSkill(SkillName.Fencing, 84.0, 90.1);
            SetSkill(SkillName.Tactics, 75.1, 85.0);
            SetSkill(SkillName.Anatomy, 75.1, 85.0);

            SetDamage(5, 7);

            Fame = 0;
            Karma = -100;
        }

        public override void InitBody()
        {
            SetStr(90, 100);
            SetDex(90, 100);
            SetInt(15, 25);

            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            AddItem(new Shirt(Utility.RandomNeutralHue()));
            AddItem(new ShortPants(Utility.RandomNeutralHue()));
            AddItem(new Boots(Utility.RandomNeutralHue()));

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new TwoPigTails(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new KrisnaHair(Utility.RandomHairHue())); break;
            }

            AddItem(new Kryss());
            PackGold(50, 60);

            Bandage aids = new Bandage();
            aids.Amount = Utility.Random(1, 3);
            AddItem(aids);

            Lockpick picks = new Lockpick();
            picks.Amount = Utility.Random(1, 3);
            AddItem(picks);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            bool bReturn = false;
            try
            {
                if (dropped is Head)
                {
                    int result = 0;
                    int goldGiven = 0;
                    bReturn = BountyKeeper.CollectBounty((Head)dropped, from, this, ref goldGiven, ref result);
                    switch (result)
                    {
                        case -2:
                            Say("Pft.. I don't want that.");
                            break;
                        case -3:
                            Say("Haha, yeah right..");
                            Say("I'll take that head, you just run along now.");
                            break;
                        case -4: //good, gold given
                            Say(string.Format("I thank you for the business.  Here's the reward of {0} gold!", goldGiven));
                            break;
                        case -5:
                            Say(string.Format("I thank you for the business, but the bounty has already been collected."));
                            break;
                        default:
                            if (bReturn)
                            {
                                Say("I'll take that.");
                            }
                            else
                            {
                                Say("I don't want that.");
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Error (nonfatal) in Thief.OnDragDrop(): " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            return bReturn;
        }

        public Thief(Serial serial)
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