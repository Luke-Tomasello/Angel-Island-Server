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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/OverlandBandit.cs
 * ChangeLog
 *	10/21/08, Adam
 *		Turn off rare factory drop until we reload the rare factory
 *  6/1/07, Adam
 *      -
 *	9/10/06, Adam
 *		- Update paramaters to new (public) DescribeLocation.
 *	1/25/06, Adam
 *		Fix a string format error (missing argument).
 *	1/24/06, Adam
 *		Add a filter to prevent queuing redundant town crier messages.
 *	1/18/06, Adam
 *		Call base.OverlandSystemMessage(state, mob) on exit.
 *			This call ensures the base class knows the message context.
 *	1/16/06, Adam
 *		Make the black clothes drop based on the 'Announced' variable
 *		(you don't deserve black cloth unless there is some competition!)
 *	1/13/06, Adam
 *		First time checkin
 */

using Server.Diagnostics;
using Server.Items;
using System;

namespace Server.Mobiles
{
    public class OverlandBandit : BaseOverland
    {

        [Constructable]
        public OverlandBandit()
        {
            SpeechHue = Utility.RandomSpeechHue();
            Title = "the bandit";

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(23, 27);

            SetSkill(SkillName.Anatomy, 60.0, 82.5);
            SetSkill(SkillName.Fencing, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 88.5, 100.0);
            SetSkill(SkillName.Tactics, 60.0, 82.5);

            Fame = 2500;
            Karma = -2500;

            new Destrier().Rider = this;

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
        }

        public override bool AlwaysAttackable { get { return true; } }
        public override bool ClickTitle { get { return false; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)); } }

        public OverlandBandit(Serial serial)
            : base(serial)
        {
        }

        public override void InitOutfit()
        {
            int lowHue = GetRandomHue();
            int hairHue = Utility.RandomHairHue();
            int cloakHue = 1;   // black

            if (Female)
                AddItem(new PlainDress(GetRandomHue()));
            else
            {
                AddItem(new Shirt(GetRandomHue()));
                AddItem(new ShortPants(lowHue));
            }

            AddItem(new Shoes(lowHue));

            if (Female == false)
                if (Utility.RandomBool())
                    AddItem(new Mustache(hairHue));
                else
                    AddItem(new Goatee(hairHue));

            // they are color coordinated :P
            Cloak cloak = new Cloak(cloakHue);
            FloppyHat hat = new FloppyHat(cloakHue);
            hat.LootType = LootType.Newbied;
            cloak.LootType = LootType.Newbied;
            AddItem(cloak);
            AddItem(hat);

            AddItem(new Kryss());

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(hairHue)); break;
                case 1: AddItem(new LongHair(hairHue)); break;
                case 2: AddItem(new ReceedingHair(hairHue)); break;
                case 3: AddItem(new PonyTail(hairHue)); break;
            }

        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 3))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (!e.Handled && e.Mobile.InRange(this.Location, 3))
            {
                if (e.HasKeyword(0x1D)) // *destination*
                {
                    switch (Utility.Random(3))
                    {
                        case 0: e.Mobile.Say("Destination? Go away. I am exactly where I want to be."); break;
                        case 1: e.Mobile.Say("You again. Please leave me alone. I have business to attend to."); break;
                        case 2: e.Mobile.Say("Can you not see I am busy?"); break;
                    }
                    e.Handled = true;
                }
                else if (e.HasKeyword(0x1E)) // *i will take thee*
                {
                    switch (Utility.Random(3))
                    {
                        case 0: e.Mobile.Say("You will take me where exactly? Go away!"); break;
                        case 1: e.Mobile.Say("You're still here? Away with thee, lest I lose my patience."); break;
                        case 2: e.Mobile.Say("Why are you still here?"); break;
                    }
                    e.Handled = true;
                }
            }
        }

        private string RandomAdjective()
        {
            switch (Utility.Random(5))
            {
                case 0: return "despicable";
                case 1: return "loathsome";
                case 2: return "wretched";
                case 3: return "nefarious";
                case 4: return "vile";
                default: return "error";
            }
        }

        public override bool OverlandSystemMessage(MsgState state, Mobile mob)
        {
            //	ignore redundant queue requests
            if (Announce == true && RedundantTCEntry(state) == false)
            {
                try
                {
                    switch (state)
                    {
                        // initial/default message
                        case MsgState.InitialMsg:
                            {
                                string[] lines = new string[2];
                                lines[0] = string.Format(
                                    "The {3} bandit {0} was last seen near {1}. {2} is not to be trusted.",
                                    Name,
                                    DescribeLocation(this),
                                    Female == true ? "She" : "He",
                                    RandomAdjective());

                                lines[1] = string.Format(
                                    "Do what you will with {0}, but just see that {1} does not enter our fair city.",
                                    Female == true ? "her" : "him",
                                    Female == true ? "she" : "he");

                                AddTCEntry(lines, 5);
                                break;
                            }

                        // under attack
                        case MsgState.UnderAttackMsg:
                            {
                                string[] lines = new string[2];

                                switch (Utility.Random(2))
                                {
                                    case 0:
                                        lines[0] = string.Format(
                                            "Hurrah! {1} has stepped in to beat down that {2} bandit {0}.",
                                            Name,
                                            (Hero(mob) == null) ? "Someone" : Hero(mob).Name,
                                            RandomAdjective());

                                        lines[1] = string.Format(
                                            "{0} may need some asistance. {2} was last seen near {1}",
                                            (Hero(mob) == null) ? "Someone" : Hero(mob).Name,
                                            DescribeLocation(this),
                                            (Hero(mob) == null) ? "It" :
                                            Hero(mob).Female == true ? "She" : "He"
                                            );

                                        break;

                                    case 1:
                                        lines[0] = string.Format(
                                            "The brave {0} is battling that {2} bandit {1}.",
                                            (Hero(mob) == null) ? "Someone" : Hero(mob).Name,
                                            Name,
                                            RandomAdjective());

                                        lines[1] = string.Format(
                                            "Hurry now, and give your aid to {0}." + " " +
                                            "We have heard they are still fighting near {1}.",
                                            (Hero(mob) == null) ? "Someone" : Hero(mob).Name,
                                            DescribeLocation(this));
                                        break;
                                }

                                // 2 minute attack message
                                AddTCEntry(lines, 2);
                                break;
                            }

                        // OnDeath
                        case MsgState.OnDeathMsg:
                            {
                                string[] lines = new string[1];
                                switch (Utility.Random(2))
                                {
                                    case 0:
                                        lines[0] = string.Format("Huzzah! that {0} bandit {1} has been defeated!", RandomAdjective(), Name);
                                        break;

                                    case 1:
                                        lines[0] = string.Format("The {0} bandit {1} has been killed! Rejoice!", RandomAdjective(), Name);
                                        break;
                                }

                                // 2 minute death message
                                AddTCEntry(lines, 2);
                                break;
                            }
                    }
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("Caught Exception{0}", exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
            }

            // we must call the base to record the 'last message'
            return base.OverlandSystemMessage(state, mob);
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                // if it's being announced, it's better loot because more risk
#if DEBUG
                if (true)
#else
                if (Announce == true)
#endif
                {
                    PackGold(1200, 1600);
                    PackScroll(6, 8);
                    PackScroll(6, 8);
                    PackReg(10);
                    PackReg(10);

                    // Use our unevenly weighted table for chance resolution
                    Item item;
                    item = Loot.RandomArmorOrShieldOrWeapon();
                    PackItem(Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level6 /*6*/, 0, false));

                    // maybe some black cloth - 2 chances
                    int black = 1;
                    if ((Utility.RandomDouble() < .25))
                        PackItem(new Cloak(black));

                    if ((Utility.RandomDouble() < .25))
                        PackItem(new FloppyHat(black));

#if false
                    // level 2 rare sword
                    if (Utility.RandomChance(10))
                    {
                        //PackItem(Server.Engines.RareFactory.AcquireRare(2, "Swords"), false);
                    }
#endif
                    if (Core.RuleSets.SiegeStyleRules())
                        PackItem(Loot.RareFactoryItem(.3, Loot.RareType.DungeonChestDropL6));
                }
                else
                {   // crappy loot if manually spawned 
                    PackGold(200, 250);
                }
            }
            else if (Core.RuleSets.SiegeStyleRules())
            {
                // if it's being announced, it's better loot because more risk
#if DEBUG
                if (true)
#else
                if (Announce == true)
#endif
                {
                    if (Spawning)
                    {
                        PackGold(1200, 1600);
                        PackScroll(6, 8);
                        PackScroll(6, 8);
                        PackReg(10);
                        PackReg(10);
                    }
                    else
                    {
                        // Use our unevenly weighted table for chance resolution
                        Item item;
                        item = Loot.RandomArmorOrShieldOrWeapon();
                        PackItem(Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level6 /*6*/, 0, false));

                        // maybe some black cloth - 2 chances
                        int black = 1;
                        if ((Utility.RandomDouble() < .25))
                            PackItem(new Cloak(black));

                        if ((Utility.RandomDouble() < .25))
                            PackItem(new FloppyHat(black));

#if false
                    // level 2 rare sword
                    if (Utility.RandomChance(10))
                    {
                        //PackItem(Server.Engines.RareFactory.AcquireRare(2, "Swords"), false);
                    }
#endif
                        PackItem(Loot.RareFactoryItem(.3, Loot.RareType.DungeonChestDropL6));
                    }
                }
                else
                {   // crappy loot if manually spawned 
                    PackGold(200, 250);
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
                {   // Standard RunUO
                    // ai special
                }
            }

            return;
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