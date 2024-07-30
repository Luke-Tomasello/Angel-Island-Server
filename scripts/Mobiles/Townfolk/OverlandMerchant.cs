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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/OverlandMerchant.cs
 * ChangeLog
 *	10/21/08, Adam
 *		Turn off rare factory drop until we reload the rare factory 
 *	1/4/08, Adam
 *		Add new loot to drop
 *		SerpentBannerEastAddonDeed, SerpentBannerSouthAddonDeed, GoldenSerpentShieldSouthAddonDeed,
 *		GoldenSerpentShieldEastAddonDeed, SilverSerpentShieldEastAddonDeed, SilverSerpentShieldSouthAddonDeed
 *	9/10/06, Adam
 *		- Update paramaters to new (public) DescribeLocation.
 *	1/24/06, Adam
 *		Add a filter to prevent queuing redundant town crier messages.
 *	1/18/06, Adam
 *		Call base.OverlandSystemMessage(state, mob) on exit.
 *			This call ensures the base class knows the message context.
 *	1/15/06, Adam
 *		Make good loot based on whether the escort is being announced on the town crier
 *	1/13/06, Adam
 *		Now BaseEscortable
 *	1/11/06, Adam
 *		Working version of the Overland Spawn System
 *	1/10/06, Adam
 *		First time checkin
 */

using Server.Diagnostics;
using Server.Items;
using System;

namespace Server.Mobiles
{
    public class OverlandMerchant : BaseEscortable
    {

        // we wanted this to be false, but I couldn't get the escourt on my boat!
        public override bool GateTravel { get { return true; } }

        [Constructable]
        public OverlandMerchant()
        {
            SpeechHue = Utility.RandomSpeechHue();
            Title = "the traveling merchant";
            Destination = new Point3D(1495, 1629, 10); // our overland merchants always want to go to "Britain"

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(23, 27);

            SetSkill(SkillName.Anatomy, 60.0, 82.5);
            SetSkill(SkillName.Macing, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 88.5, 100.0);
            SetSkill(SkillName.Tactics, 60.0, 82.5);

            Fame = 2500;
            Karma = -2500;

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
        }

        public OverlandMerchant(Serial serial)
            : base(serial)
        {
        }

        public override bool ClickTitle { get { return false; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)); } }

        public override void InitOutfit()
        {
            int hairHue = Utility.RandomHairHue();
            int cloakHue = GetRandomHue();

            if (Female)
                AddItem(new FancyDress(GetRandomHue()));
            else
                AddItem(new FancyShirt(GetRandomHue()));

            int lowHue = GetRandomHue();

            AddItem(new ShortPants(lowHue));

            if (Female)
                AddItem(new ThighBoots(lowHue));
            else
                AddItem(new Boots(lowHue));

            if (!Female)
                AddItem(new Mustache(hairHue));

            // they are color coordinated :P
            AddItem(new Cloak(cloakHue));
            AddItem(new FeatheredHat(cloakHue));

            AddItem(new BlackStaff());

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(hairHue)); break;
                case 1: AddItem(new LongHair(hairHue)); break;
                case 2: AddItem(new ReceedingHair(hairHue)); break;
                case 3: AddItem(new PonyTail(hairHue)); break;
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
                                lines[0] = String.Format(
                                    "The merchant {0} was last seen somewhere {1} and is said to be bringing us some of {2} finest wares.",
                                    Name,
                                    RelativeLocation(),
                                    Female == true ? "her" : "his");

                                lines[1] = String.Format(
                                    "{0} was last seen near {1}" + " " +
                                    "Please see that {2} arrives safely.",
                                    Name,
                                    DescribeLocation(this),
                                    Name);

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
                                        lines[0] = String.Format(
                                            "The merchant {0} is under attack by {1}!" + " " +
                                            "Quickly now! There is no time to waste.",
                                            Name,
                                            (Villain(mob) == null) ? "Someone" : Villain(mob).Name);

                                        lines[1] = String.Format(
                                            "{0} was last seen near {1}",
                                            Name,
                                            DescribeLocation(this));
                                        break;

                                    case 1:
                                        lines[0] = String.Format(
                                            "Quickly, there is no time to waste!" + " " +
                                            "Britain's merchant {0} is under attack by {1}!",
                                            Name,
                                            (Villain(mob) == null) ? "Someone" : Villain(mob).Name);

                                        lines[1] = String.Format(
                                            "{0} was last seen somewhere near {1}",
                                            Name,
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
                                        lines[0] = String.Format("Great sadness befalls us. The merchant {0} has been killed.", Name);
                                        break;

                                    case 1:
                                        lines[0] = String.Format("Alas, the fair merchant {0} has been killed. We shall avenge those responsible!", Name);
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

        // this gold is handed out by the escort code
        public override void ProvideLoot(Mobile escorter)
        {
            if (escorter == null)
                return;

            // if it's being announced, it's better loot because more risk
#if DEBUG 
            if (true)
#else
            if (Announce == true)
#endif
            {
                // Use our unevenly weighted table for chance resolution
                Item item;
                item = Loot.RandomArmorOrShieldOrWeapon();
                GiveLoot(Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level6 /*6*/, 0, false));

                // gold
                GiveLoot(new Gold(1200, 1600));

                // level 2 rare wine or a wine book
                if (Utility.RandomChance(10))
                {
                    if (Utility.RandomBool())
                    {
                        /*if (Utility.RandomBool())
							GiveLoot(Server.Engines.RareFactory.AcquireRare(2, "Wines"), false);
						else
							GiveLoot(Server.Engines.RareFactory.AcquireRare(2, "WineBooks"), false);*/

                        GiveLoot(Loot.RareFactoryItem(.3, Loot.RareType.DungeonChestDropL6));
                    }
                    else
                    {
                        switch (Utility.Random(6))
                        {
                            case 0: GiveLoot(new SerpentBannerEastAddonDeed(), false); break;
                            case 1: GiveLoot(new SerpentBannerSouthAddonDeed(), false); break;
                            case 2: GiveLoot(new GoldenSerpentShieldSouthAddonDeed(), false); break;
                            case 3: GiveLoot(new GoldenSerpentShieldEastAddonDeed(), false); break;
                            case 4: GiveLoot(new SilverSerpentShieldEastAddonDeed(), false); break;
                            case 5: GiveLoot(new SilverSerpentShieldSouthAddonDeed(), false); break;
                        }
                    }
                }
            }
            else
            {
                // crappy loot if manually spawned 
                GiveLoot(new Gold(200, 250));
            }
            return;
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                // this loot drops with corpse
#if DEBUG
                if (true)
#else
                if (Announce == true)
#endif
                {
                    PackScroll(6, 8);
                    PackScroll(6, 8);
                    PackReg(10);
                    PackReg(10);
                }
                else
                {
                    // crappy loot if manually spawned 
                    PackGold(200, 250);
                }
            }
            else if (Core.RuleSets.SiegeStyleRules())
            {
                // this loot drops with corpse
#if DEBUG
                if (true)
#else
                if (Announce == true)
#endif
                {
                    if (!Spawning)
                    {
                        PackScroll(6, 8);
                        PackScroll(6, 8);
                        PackReg(10);
                        PackReg(10);
                    }
                }
                else
                {
                    // crappy loot if manually spawned 
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

        // timer callback to complain about moongate travel
        public override void tcMoongate()
        {
            Mobile mob = GetEscorterWithSideEffects();
            if (mob != null)
                this.Say("I'm sorry {0}, but magic scares me and I do not wish to travel this way.", mob.Name);
            else
                this.Say("I'm sorry, but magic scares me and I do not wish to travel this way.");
        }

        public override TeleportResult OnMagicTravel()
        {
            if (GateTravel == false)
            {
                Mobile mob = GetEscorterWithSideEffects();
                if (mob != null)
                {
                    int save = this.SpeechHue;
                    this.SpeechHue = 0x23F; // this.SpeechHue = 0x3B2;
                    SayTo(mob, "Wait! Please come back!");
                    this.SpeechHue = save;
                }
            }

            // BaseCreature OnMagicTravel() call this for gate travel:
            // OnMagicTravel() override calls tcMoongate() on a delayed callabck
            //	this is so the player escorting will see the message
            //	when they return through the gate to find their NPC
            return base.OnMagicTravel();
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