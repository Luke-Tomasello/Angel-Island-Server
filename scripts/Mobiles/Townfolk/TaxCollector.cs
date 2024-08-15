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

/* Scripts\Mobiles\Townfolk\TaxCollector.cs
 * ChangeLog
 *  1/11/23, Adam (OurDistanceToTreasure)
 *      o) OnSee was coded to not add you to the list of tax evaders if you had left the TreasureMapRegion - but this region is tiny.
 *      A better check is to see if the tax collector and the player are near the treasure.. if so, you'll get added.
 *      Removed the TreasureMapRegion check, added OurDistanceToTreasure()
 *      o) Change requested gold from 180% - 200% of chest gold to 40%
 *  2/19/22, Adam
 *      Remove the check to ignore Accesslevel > x and replaced it with blessed.
 *      This allows developers to test the system without a need to pull in a second account char.
 *      The rule is, staff on production should always be blessed anyway... 
 *	8/26/10, Adam
 *		Check for needed gold before reporting the treasure hunters
 *	8/18/10, adam
 *		Add an OnEvent handler so that the AI can notify us when we recall away
 *	8/7/10, Adam
 *		initial creation
 */

using Server.Diagnostics;
using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class TaxCollector : BaseCreature
    {
        public override bool ShowFameTitle { get { return false; } }
        private int m_goldNeeded = 0;
        // Unyielding, Threatening, Sympathetic, Corrupt
        private Personality m_personality = (Personality)Utility.RandomList(0, 1, 2, 3);
        private List<Mobile> m_players = new List<Mobile>();

        // spawner or GM created, random gold requirement, and no auto delete
        [Constructable]
        public TaxCollector()
            : base(AIType.AI_TaxCollector, FightMode.Aggressor, 22, 1, 0.175, 0.350)
        {
            InitBody();
            InitOutfit();
            Title = "the tax collector";

            BardImmune = true;
            CanRun = true;

            SetSkill(SkillName.Wrestling, 100.1, 170.0);
            SetSkill(SkillName.Tactics, 100.1, 170.0);
            SetSkill(SkillName.Anatomy, 100.1, 170.0);

            SetSkill(SkillName.EvalInt, 100.1, 170.0);
            SetSkill(SkillName.Magery, 100.1, 170.0);
            SetSkill(SkillName.Meditation, 100.1, 170.0);
            SetSkill(SkillName.MagicResist, 100.1, 170.0);

            SetDamage(15, 30);

            SetFameLevel(3);
            SetKarmaLevel(3);
            Karma = -Karma;

            new Horse().Rider = this;

            // don't wait too long before reporting this chest as taxable!
            Timer.DelayCall(TimeSpan.FromMinutes(2.0), new TimerStateCallback(Cleanup), null);

            // start out by thinking/looking around
            Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(Startup), null);
        }

        // called when the treasure chest opens
        public TaxCollector(Container c)
            : this()
        {
            if (c is TreasureMapChest)
            {   // where we will walk to
                Home = (c as TreasureMapChest).Location;
                // gold we will want
                int chestLevel = (c as TreasureMapChest).Level;
                // 40% of chest gold
                m_goldNeeded = Math.Max((int)(GetTreasureGold(c as TreasureMapChest) * 0.4), 30);
            }
        }
        private int GetTreasureGold(TreasureMapChest tmc)
        {
            int amount = 0;
            if (tmc.Items != null)
                foreach (Item item in tmc.Items)
                    if (item is Gold)
                        amount += (item as Gold).Amount; ;
            return amount;
        }
        public override void OnSee(Mobile m)
        {
            // yeah
            if (m is PlayerMobile == false)
                return;

            // sanity
            if (m.Deleted || m.Hidden || !m.Alive || m.AccessLevel > this.AccessLevel || !this.CanSee(m))
                return;

            // only valid regions
            if (m.Region == null || m.Region == Map.DefaultRegion || OurDistanceToTreasure(m) > RangePerception)
                return;

            if (m_players.Contains(m) == false)
                m_players.Add(m);
        }
        private double OurDistanceToTreasure(Mobile m)
        {
            double myDistance = 100;
            double hisDistance = 100;
            IPooledEnumerable eable = Map.GetItemsInRange(Location, RangePerception);
            foreach (Item item in eable)
                if (item is TreasureMapChest tc)
                {
                    myDistance = GetDistanceToSqrt(tc.Location);
                    hisDistance = m.GetDistanceToSqrt(tc.Location);
                    break;
                }
            eable.Free();

            return Math.Max(myDistance, hisDistance);

        }
        private void Startup(object state)
        {   // on a server restart, we may actually be done
            if (m_goldNeeded <= 0 && AIObject != null)
                AIObject.Action = ActionType.Flee;

            OnThink();
        }

        private void Cleanup(object state)
        {
            Say("I can't be waiting around all day.");
            if (AIObject != null)
            {   // okay, hit the road!
                if (FocusMob == null)
                {
                    FocusMob = NearbyPlayer();
                    if (FocusMob == null)
                    {   // nobody around, just delete
                        this.Delete();
                        return;
                    }
                }

                // okay, run away from the player and recall
                AIObject.Action = ActionType.Flee;
            }
        }

        public override void OnThink()
        {
            // we are loney and want to talk to someone
            if (AIObject != null && AIObject.Action == ActionType.Wander && FocusMob == null)
            {   // if we're too far from home, don't interact, just wander
                if (Home == Point3D.Zero || GetDistanceToSqrt(Home) < RangePerception)
                {   // if the mobile we want to talk to is too far away, ignore him
                    Mobile temp = NearbyPlayer();
                    if (temp != null && (Home == Point3D.Zero || temp.GetDistanceToSqrt(Home) < RangePerception))
                    {
                        FocusMob = temp;
                        AIObject.Action = ActionType.Interact;
                        DebugSay(DebugFlags.AI, "I'm going to talk to {0}", FocusMob.Name);
                    }
                }
            }

            base.OnThink();
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            Mobile from = e.Mobile;

            if (from.InRange(this, 4) && !e.Handled)
            {
                string[] SwearingKeywords = new string[] { "fuck", "damn", "hell", "cunt", "asshole", "bastard", "bitch", "whore", "dick", "jerk", "shit", "bullshit", "ass" };
                for (int ix = 0; ix < SwearingKeywords.Length; ix++)
                {
                    if (e.Speech.Contains(SwearingKeywords[ix]))
                    {
                        e.Handled = true;
                        this.FocusMob = from;
                        if (AIObject != null)
                            AIObject.Action = ActionType.Interact;
                        switch (m_personality)
                        {
                            case Personality.Unyielding:
                                Say("Your pedestrian vocabulary will not deter my tax collectione efforts.");
                                break;
                            case Personality.Threatening:
                                Say("If I wasn't on duty right now, I'd cram my fist into that smart mouth of yours.");
                                break;
                            case Personality.Sympathetic:
                                Say("Come on, please, let's keep this civil.  We're all working together here.");
                                break;
                            case Personality.Corrupt:
                                Emote("*Laughs*");
                                Say("Curse all you like, just make the payment.");
                                break;
                        }
                        break;
                    }
                }

                string[] LordBritishKeywords = new string[] { "king", "british", "lord", "master" };
                for (int ix = 0; ix < LordBritishKeywords.Length; ix++)
                {
                    if (e.Speech.Contains(LordBritishKeywords[ix]))
                    {
                        e.Handled = true;
                        this.FocusMob = from;
                        if (AIObject != null)
                            AIObject.Action = ActionType.Interact;
                        switch (m_personality)
                        {
                            case Personality.Unyielding:
                                Say("The King will know of every word you've spoken.");
                                break;
                            case Personality.Threatening:
                                Say("Just shut your mouth and make the tax payment.");
                                break;
                            case Personality.Sympathetic:
                                Say("I will deliver your message for better or worse.");
                                break;
                            case Personality.Corrupt:
                                Say("I don't really care.  I'm in this for me.");
                                break;
                        }
                        break;
                    }
                }

                string[] TaxationKeywords = new string[] { "tax", "taxation", "assessment", "collector" };
                for (int ix = 0; ix < TaxationKeywords.Length; ix++)
                {
                    if (e.Speech.Contains(TaxationKeywords[ix]))
                    {
                        e.Handled = true;
                        this.FocusMob = from;
                        if (AIObject != null)
                            AIObject.Action = ActionType.Interact;
                        switch (m_personality)
                        {
                            case Personality.Unyielding:
                                Say("All profits derived from the lands of Lord British are subject to assessment and taxation.");
                                break;
                            case Personality.Threatening:
                                Say("You will pay when you take from the King's land.  No amount of groveling, threatening, or crying is going to change that.");
                                break;
                            case Personality.Sympathetic:
                                Say("Paying your taxes isn't enjoyable, but through our gracious and socialist King we will all benefit from it.  Please help us all by paying your taxes.");
                                break;
                            case Personality.Corrupt:
                                Say("You could pay a lot more for your taxes or you could pay me to forget you exist.  Your choice.");
                                break;
                        }
                        break;
                    }
                }
            }
        }

        private Mobile NearbyPlayer()
        {
            IPooledEnumerable eable = this.Map.GetMobilesInRange(this.Location, 12);
            foreach (Mobile m in eable)
            {
                // ignore staff (blessed only, not access level)
                if (m.Blessed)
                    continue;

                // prefer a player by returning it now
                if (m is PlayerMobile && !m.Hidden)
                {
                    eable.Free();
                    return m;
                }
            }
            eable.Free();

            return null;
        }

        public override void InitBody()
        {
            SetStr(500, 900);
            SetDex(500, 900);
            SetInt(500, 900);
            VirtualArmor = 90;

            Hue = 33770;        // white guy
            Female = false;     // male
            Body = 400;
            Name = NameList.RandomName("tax collector");
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            int Valorite = 0x8AB;
            EmoteHue = Utility.RandomYellowHue();

            ChainChest chest = new ChainChest();
            chest.Resource = CraftResource.Valorite;
            chest.LootType = LootType.Newbied;
            AddItem(chest);

            ChainLegs legs = new ChainLegs();
            legs.Resource = CraftResource.Valorite;
            legs.LootType = LootType.Newbied;
            AddItem(legs);

            FeatheredHat hat = new FeatheredHat();
            hat.Hue = Valorite;
            hat.LootType = LootType.Regular;
            AddItem(hat);

            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new PageboyHair(Utility.RandomHairHue()));

            Runebook runebook = new Runebook();
            runebook.Hue = Utility.RandomMetalHue();
            runebook.Name = "Collections";
            runebook.LootType = LootType.Newbied;
            AddItem(runebook);

            AddItem(new GoldRing());

            PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
            PackStrongPotions(6, 12);
            PackItem(new Pouch(), lootType: LootType.UnStealable);

            PackGold(50, 60);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            bool bReturn = false;
            try
            {
                if (dropped is Gold)
                {
                    bReturn = true;
                    int amount = (dropped as Gold).Amount;
                    dropped.Delete();
                    m_goldNeeded -= amount;

                    if (m_goldNeeded <= 0)
                    {
                        Dialog(State.Paid);
                        if (AIObject != null)
                        {   // okay, hit the road!
                            FocusMob = from;
                            AIObject.Action = ActionType.Flee;

                            // kill these states if we have been paid
                            m_said[(int)State.Enter] = true;
                            m_said[(int)State.Arrives] = true;
                            m_said[(int)State.Proposal] = true;
                            m_said[(int)State.RefinedProposal] = true;
                        }
                    }
                    else
                    {
                        Say("Thanks, but you will need {0} more gold to satisfy your debt.", m_goldNeeded);
                    }
                }
                else
                {
                    bReturn = true;
                    string name = dropped.Name;
                    if (name == null)
                        name = dropped.GetType().Name;
                    Say("Thanks for the {0}. Now about that gold...", name);
                    dropped.Delete();
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Error (nonfatal) in TaxCollector.OnDragDrop(): " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            return bReturn;
        }

        private enum Personality
        {
            Unyielding,
            Threatening,
            Sympathetic,
            Corrupt
        }

        private enum State
        {
            Enter,
            Arrives,
            Proposal,
            RefinedProposal,
            Paid,
            Attacked,
            RunningAway
        }

        public void OnNextMessage(object state)
        {
            if (state is State && !Deleted && AIObject != null && AIObject.Action == ActionType.Interact)
                Dialog((State)state);
        }

        public override void OnActionInteract()
        {
            Dialog(State.Enter);
            base.OnActionInteract();
        }

        public override void OnActionFlee()
        {
            if (m_goldNeeded > 0)
            {   // we always say this even if not attacked as long as we are leaving and haven't been paid
                Dialog(State.RunningAway);
            }
            base.OnActionFlee();
        }

        private void ReportTaxEvasion()
        {
            if (this.Home != Point3D.Zero)
            {
                string location;
                int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;
                bool valid = Sextant.Format(this.Home, this.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

                if (valid)
                    location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                else
                    location = "????";

                if (!valid)
                    location = string.Format("{0} {1}", this.X, this.Y);

                if (this.Map != null)
                {
                    Region reg = this.Region;
                    if (reg != this.Map.DefaultRegion)
                        location += (" in " + reg);
                }

                string[] lines = new string[2];
                lines[0] = string.Format("Lord British declares the treasure hunt at {0} unlawful and calls for a citizen militia to dispatch this party of criminals.", location);
                lines[1] = string.Format("Let it be known across the land; Lord British requires taxes paid on all treasure recovered within his kingdom.");

                DateTime dt = DateTime.UtcNow + TimeSpan.FromMinutes(10);
                Engines.ListEntry le = new Engines.ListEntry(lines, null, dt, Engines.ListEntryType.TownCrier);
                Engines.TCCS.AddEntry(le);

                foreach (PlayerMobile m in m_players)
                {
                    m.MakeCriminal(dt - DateTime.UtcNow);
                }
            }
        }

        public override void OnEvent(object eo)
        {
            if (eo is Spells.Fourth.RecallSpell.InternalTarget)
            {   // our guy is recalling away, 
                if (m_goldNeeded > 0)
                    ReportTaxEvasion();
            }
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            Dialog(State.Attacked);
            base.AggressiveAction(aggressor, criminal);
        }

        private bool[] m_said = new bool[System.Enum.GetValues(typeof(State)).Length];

        private void Dialog(State state)
        {
            try
            {
                // see if we have already said this
                if (m_said[(int)state] == true)
                    return;

                // okay, flag that we've said this
                m_said[(int)state] = true;

                switch (state)
                {
                    case State.Enter:
                        Timer.DelayCall(TimeSpan.FromSeconds(1.8), new TimerStateCallback(OnNextMessage), new object[] { State.Arrives });
                        break;

                    case State.Arrives:
                        Timer.DelayCall(TimeSpan.FromSeconds(10.0), new TimerStateCallback(OnNextMessage), new object[] { State.Proposal });
                        // tax collector arrives:  Well hello. I see you have quite the find here.																							
                        switch (m_personality)
                        {
                            case Personality.Unyielding:
                                Say("Hail!");
                                Say("That is quite a bit of treasure you've found there.");
                                break;
                            case Personality.Threatening:
                                Say("Halt!");
                                Say("Stand your ground and listen carefully!");
                                break;
                            case Personality.Sympathetic:
                                Say("Pardon my intrusion on your dig site.");
                                Say("I fear we must discuss proper tax assessment.");
                                break;
                            case Personality.Corrupt:
                                Say("Well hello there travellers.");
                                Say("What is it that we have here?");
                                break;
                        }
                        break;


                    case State.Proposal:
                        Timer.DelayCall(TimeSpan.FromSeconds(15.0), new TimerStateCallback(OnNextMessage), new object[] { State.RefinedProposal });
                        // tax collector makes proposal:  I am {0}, tax collector for the king. There is a tax due on all treasures you may find																							
                        switch (m_personality)
                        {
                            case Personality.Unyielding:
                                Say("I am {0}, royal tax assessor for Lord British.", this.Name);
                                Say("All lands belong to Lord British and as such all earnings from said lands are subject to taxation.");
                                break;
                            case Personality.Threatening:
                                Say("I am {0} and it is my job to make certain all taxes are properly paid to our king, Lord British.", this.Name);
                                Say("You will abide by the laws of these lands and pay proper taxes on your earnings from his land!");
                                break;
                            case Personality.Sympathetic:
                                Say("My name is {0}.", this.Name);
                                Say("I'm a tax assessor for our king.");
                                Say("Unfortunately, all treasure taken from his lands is subject to taxation.");
                                Say("I understand this may be frustrating and inconvenient for you.");
                                break;
                            case Personality.Corrupt:
                                Say("I am a tax assessor for the king.");
                                Say("My name is unimportant.");
                                Say("You will need to pay taxes on that treasure unless, perhaps, we can find another arrangement to keep everything legal.");
                                break;
                        }
                        break;


                    case State.RefinedProposal:
                        // tax collector refined proposal:  You will need to pay {0} in gold now is you wish to continue unfettered.
                        switch (m_personality)
                        {
                            case Personality.Unyielding:
                                Emote("*{0} studies the treasure chest carefully*", this.Name);
                                Say("Pursuant to the King's law, I assess your tax liability at {0} gold coins to be paid into my trust immediately.", m_goldNeeded);
                                break;
                            case Personality.Threatening:
                                Emote("*{0} glowers as he looks inside the chest*", this.Name);
                                Say("You tax evaders will pay the tax of {0} gold coins to me or the consequences will be dire!", m_goldNeeded);
                                break;
                            case Personality.Sympathetic:
                                Say("Looking at the treasure involved, I am duty bound to charge you a tax fee of {0} gold coins.  I really wish I could give you a discount, but my hands are tied here.  Please pay me now.", m_goldNeeded);
                                break;
                            case Personality.Corrupt:
                                Say("Why don't you pay me {0} gold coins and I'll just be on my way?  We never met, right?  If you don't, well then I have to inform the King and things may get very uncomfortable for you.", m_goldNeeded);
                                Say("We never met, right?");
                                Say("If you don't, well then I have to inform the King and things may get very uncomfortable for you.");
                                break;
                        }
                        break;

                    case State.Paid:
                        // tax collector paid, now leaves:  Thank you and good day!
                        switch (m_personality)
                        {
                            case Personality.Unyielding:
                                Say("Thank you for your payment, citizen.");
                                Say("The King will know that you are a lawful tax payer.");
                                Say("Good day!");
                                break;
                            case Personality.Threatening:
                                Say("You made the right and only choice.");
                                Say("Carry on and remember to always pay your taxes.");
                                break;
                            case Personality.Sympathetic:
                                Say("Thank you for your payment.");
                                Say("I really appreciate your understanding.");
                                Say("Have a great day and good luck with your treasure hunting.");
                                break;
                            case Personality.Corrupt:
                                Say("We have a deal.");
                                Say("I don't know you and you don't know me.");
                                break;
                        }
                        break;

                    case State.Attacked:
                        // tax collector is attacked:  WTFOMG! Youi are so busted!! *runs home to mommy*
                        switch (m_personality)
                        {
                            case Personality.Unyielding:
                                Say("You have made a grave mistake!");
                                break;
                            case Personality.Threatening:
                                Say("Damn you!");
                                Say("I will not stand for this!");
                                break;
                            case Personality.Sympathetic:
                                Say("HELP!");
                                Say("EndMusic!");
                                Say("Why would you do this?!");
                                break;
                            case Personality.Corrupt:
                                Say("Fine!");
                                Say("Refusing my offer will mean your death!");
                                break;
                        }
                        break;

                    case State.RunningAway:
                        // tax collector running away:  This will be reported to the king!
                        switch (m_personality)
                        {
                            case Personality.Unyielding:
                                Say("The King shall be made aware of this transgression!");
                                break;
                            case Personality.Threatening:
                                Say("There will be nothing but death for you when the King hears of this!");
                                break;
                            case Personality.Sympathetic:
                                Say("The King must be informed of this attack!");
                                break;
                            case Personality.Corrupt:
                                Say("The King will be told you refused to pay your taxes!");
                                break;
                        }
                        break;
                }
            }
            catch { }
        }

        public TaxCollector(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);   // version

            // version 0
            writer.Write((int)m_goldNeeded);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    m_goldNeeded = reader.ReadInt();
                    goto default;

                default:
                    break;
            }


            // we don't bother saving all the timer states, but instead just restart the whole interaction
            //	we do apply any gold already paid tho. We will (re)explain about the taxes no matter what

            // don't wait too long before reporting this chest as taxable!
            Timer.DelayCall(TimeSpan.FromMinutes(2.0), new TimerStateCallback(Cleanup), null);

            // start out by thinking/looking around
            Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(Startup), null);
        }
    }
}