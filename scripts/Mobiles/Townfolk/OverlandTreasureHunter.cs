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

/* Scripts/Mobiles/Townfolk/OverlandTreasureHunter.cs
 * ChangeLog
 *  3/30/2024, Adam (Rename GetEscorter() => GetEscorterWithSideEffects())
 *      Bug Fix: I was calling GetEscorter() in OnThink to record that last time I saw my escorter. Seems safe enough...
 *          I now call GetMaster().
 *      This is at least the second time I've introduced a problem calling the seemingly innocuously named GetEscorter()
 *      this fucking call does all sorts of shit including the control order (which borked my Treasure Hunter.)
 *      To make this a method little less likely to cause problems in the future, I've renamed it as above.
 *      At some point it should get a better name / rewrite.
 *  7/12/2023, Adam
 *      Have the treasure hunter calculate the largest stack of each gem held by players.
 *          We then base our demands on this number. This calculation is also done every 4 hours in Cron.
 *      Have the treasure hunter unlock & disarm the chest.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  10/25/07 Taran Kain
 *      Fixed CheckGems() logic
 *	9/10/06, Adam
 *		- Update parameters to new (public) DescribeLocation.
 *	5/18/06, Adam
 *		- Updated to work with the redesigned BaseEscortable.cs (DescribeLocation() function)
 *	4/27/06, weaver
 *		- Added override to new BaseEscortable.AbandonDelay so that OTH abandons player
 *		after 10 minutes, not the standard escortable's 2
 *		- Added check to make sure they have sufficient control slots before entering new 
 *		quest state and accepting the dragdrop
 *		- Fixed script location comment
 *	1/27/06, Adam
 *		Add a backtracking engine to restart previous states if we get moved
 *		from the goal (chest)
 *	1/24/06, Adam
 *		Add a filter to prevent queuing redundant town crier messages.
 *	1/20/06, Adam
 *		Pretty much a rewrite of core logic.
 *		Add a 10 state state-machine to guide the quest
 *	1/18/06, Adam
 *		Call base.OverlandSystemMessage(state, mob) on exit.
 *			This call ensures the base class knows the message context.
 *	1/16/06, Adam
 *		Swap an SDrop for a level 5 map
 *		Also add ArrivedSpeak() override so we can have a custom "Arrived" message
 *	1/15/06, Adam
 *		New coordinate based escortable - First time check-in
 */

using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using System;
using System.Collections;
using System.Linq;

namespace Server.Mobiles
{
    public class OverlandTreasureHunter : BaseEscortable
    {
        private static int[] GemCounts = new int[Loot.GemTypes.Count()];
        public new static void Initialize()
        {
            EventSink.ServerStarted += new ServerStartedEventHandler(EventSink_ServerStarted);
        }
        public static void EventSink_ServerStarted()
        {
            CountGems();
        }

        // called here, and as a Cron job every 4 hours or so.
        public static int CountGems()
        {
            // count the largest stacks of gems currently in game.
            //  Our treasure hunter will base his request on these amounts.
            foreach (Item item in World.Items.Values)
            {
                var index = Array.FindIndex(Loot.GemTypes, row => row == item.GetType());
                if (index != -1)
                    // found one
                    if (item.Amount > GemCounts[index])
                        GemCounts[index] = item.Amount;
            }

            return GemCounts.Max();
        }

        // wea: added override to new AbandonDelay
        public override TimeSpan AbandonDelay { get { return TimeSpan.FromMinutes(10.0); } }

        public override TimeSpan EscortDelay
        {
            get { return TimeSpan.Zero; }
            //set { m_EscortDelay = value; }
        }

        // we wanted this to be false, but I couldn't get the escourt on my boat!
        public override bool GateTravel { get { return true; } }

        // ReadyChest helper vars
        DateTime m_LastReadyChestTalk = DateTime.MinValue;

        int m_GemIndex = 0;
        int m_GemsRequired = 0;

        public enum QuestState
        {
            Error,      // error
            Initialize, // Setup
            Ready,      // ready and waiting for a player
            Paid,       // I've received my gems
            Journey,    // basic escort to chest state
            ChestMarch, // at near the chest, moving into position
            AtChest,    // tell everyone to stand back
            ReadyDig,   // ready to dig baby!
            Waiting,    // Waiting for the map the be completed.
            Finishing,  // award any fame, do any cleanup
            Done,       // my work is done, time to die I guess
        }

        QuestState m_QuestState = QuestState.Initialize;
        [CommandProperty(AccessLevel.GameMaster)]
        public QuestState State
        {
            get { return m_QuestState; }
            set { m_QuestState = value; DoEnterState(); }
        }

        private TreasureMap FindMap()
        {
            Container pack = this.Backpack;
            if (pack == null) return null;
            TreasureMap map;
            map = pack.FindItemByType(typeof(TreasureMap), false) as TreasureMap;
            return map;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D ChestLocation
        {
            get { return (FindMap() == null) ? Point2D.Zero : FindMap().ChestLocation; }
        }

        public override Point3D[] GetPossibleDestinations()
        {   // we do not support random destinations

            // 5/31/2021, adam  Add a destination (the treasure chest!)
            //////////////////////
            return new Point3D[] { Point3D.Zero };
            //////////////////////
            //return null;
        }

        [Constructable]
        public OverlandTreasureHunter()
        {
            SpeechHue = Utility.RandomSpeechHue();
            Title = "the treasure hunter";

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(23, 27);

            SetSkill(SkillName.Anatomy, 60.0, 82.5);
            SetSkill(SkillName.Wrestling, 88.5, 100.0);
            SetSkill(SkillName.MagicResist, 88.5, 100.0);
            SetSkill(SkillName.Tactics, 60.0, 82.5);

            Fame = 2500;
            Karma = -2500;

            // Configure our treasuremap
            TreasureMap map = new TreasureMap(5, Map.Felucca);
            map.LootType = LootType.Newbied;
            if (Core.RuleSets.ThemedTreasureMaps())
            {
                map.Themed = true;
                map.Theme = TreasureTheme.RandomOverlandTheme();
            }
            else
            {
                map.Themed = false;
                map.Theme = ChestThemeType.None;
            }
            map.Decoder = this;
            PackItem(map);

            Shovel shovel = new Shovel();
            shovel.LootType = LootType.Newbied;
            PackItem(shovel);

            Runebook book = new Runebook();
            book.LootType = LootType.Newbied;
            book.Name = "Fromm's treasure hunting guide";
            AddItem(book);

            Item light = null;
            if (Utility.RandomBool())
                light = new Lantern();
            else
                light = new Torch();
            PackItem(light);

#if false
            // select a gem for payment
            m_GemIndex = Utility.Random(TreasureMapChest.m_GemTypes.Length);
            m_GemsRequired = 50 * (2 + Utility.Random(9)); // 100-500
#else
            int maxValue = GemCounts.Max();
            int maxIndex = GemCounts.ToList().IndexOf(maxValue);
            m_GemIndex = maxIndex;
            if (maxValue < 100)
            {   // protect against a gem requirement of 0 
                m_GemsRequired = Math.Max(1, Utility.RandomMinMax(1, maxValue));
            }
            else
            {
                m_GemsRequired = Math.Min(Utility.RandomMinMax(100, 500), maxValue);
            }

            if (GemCounts[maxIndex] >= m_GemsRequired)
                GemCounts[maxIndex] -= m_GemsRequired;
            else
                GemCounts[maxIndex] = 0;
#endif

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
        }

        public OverlandTreasureHunter(Serial serial)
            : base(serial)
        {
        }

        public override bool ClickTitle { get { return false; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)); } }

        private string GetGemName(Type type, bool plural)
        {
            if (plural == false)
                return type.Name;

            if (type == typeof(Amber) || type == typeof(Amethyst))
                return string.Format("Pieces of {0}", type.Name);
            else if (type == typeof(Ruby))
                return string.Format("rubies");
            else
                return string.Format("{0}s", type.Name);
        }

        public override void InitOutfit()
        {
            int hairHue = Utility.RandomHairHue();

            AddItem(new FancyShirt(GetRandomHue()));

            int lowHue = GetRandomHue();

            AddItem(new ShortPants(lowHue));

            if (Female)
                AddItem(new ThighBoots(lowHue));
            else
                AddItem(new Shoes(lowHue));

            if (!Female)
                if (Utility.RandomBool())
                    AddItem(new Mustache(hairHue));

            if (Utility.RandomBool())
                AddItem(new StrawHat(Utility.RandomSpecialBrownHue()));

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
                                lines[0] = string.Format(
                                    "The treasure hunter {0} was last seen somewhere {1} and is said to be in need of assistance.",
                                    Name,
                                    RelativeLocation());

                                lines[1] = string.Format(
                                    "{0} was last seen near {1}" + " " +
                                    "Please see that {2} receives whatever help {3} needs.",
                                    Name,
                                    DescribeLocation(this),
                                    Name,
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
                                            "The treasure hunter {0} is under attack by {1}!" + " " +
                                            "Quickly now, {2} needs your help!",
                                            Name,
                                            (Villain(mob) == null) ? "Someone" : Villain(mob).Name,
                                            Female == true ? "she" : "he");

                                        lines[1] = string.Format(
                                            "{0} was last seen near {1}",
                                            Name,
                                            DescribeLocation(this));
                                        break;

                                    case 1:
                                        lines[0] = string.Format(
                                            "Quickly, there is no time to waste!" + " " +
                                            "Britain's own treasure hunter {0} is under attack by {1}!",
                                            Name,
                                            (Villain(mob) == null) ? "Someone" : Villain(mob).Name);

                                        lines[1] = string.Format(
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
                                        lines[0] = string.Format("Great sadness befalls us. The treasure hunter {0} has been killed.", Name);
                                        break;

                                    case 1:
                                        lines[0] = string.Format("Alas, the fair treasure hunter {0} has been killed. We shall avenge those responsible!", Name);
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

        public override void ArrivedSpeak(string name)
        {
            // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.
            this.Say("Ah! This is the place, thank thee {0}!", name);
            this.Say("Hmm. I know it is around here somewhere...");
            this.Say("*searches*");
        }

        public override void LeadOnSpeak(string name)
        {
            TreasureMap tm = FindMap();
            if (tm == null) return;
            name = DescribeLocation(tm.Map, new Point3D(ChestLocation, 0));
            Say("Lead on! I shall share the treasure located at {0}.", name);
        }

        public override void DestinationSpeak(string name)
        {
            TreasureMap tm = FindMap();
            if (tm == null) return;
            name = DescribeLocation(tm.Map, new Point3D(ChestLocation, 0));
            Say("I am looking to go to {0}, will you take me?", name);
        }

        public override bool There() { return false; }
        public override bool CheckAtDestination() { return false; }

        public bool FindGeneralLocation()
        {
            Point3D dest = GetDestination();
            Mobile escorter = GetEscorterWithSideEffects();

            // should never happen
            if (escorter == null || dest == Point3D.Zero)
            {
                State = QuestState.Error;
                return false;
            }

            // make sure we are following our master
            StartFollow();

            // looks like we're here
            if (this.GetDistanceToSqrt(ChestLocation) <= 10.0)
            {
                // tell the escort this look like the place
                ArrivedSpeak(escorter.Name);
                State = QuestState.ChestMarch;
                return true;
            }

            return false;
        }

        private void EnterChestMarchState()
        {
            // stop following our master
            //	but we want to remain 'controlled'so that we're attacked by monsters
            StopFollow();

            // now start walking to the chest
            //	we want a home range of 1 and not zero in case we at any time
            //	are standing ON the chest, we will keep wandering until we get 
            //	off of it.
            this.Home = new Point3D(ChestLocation, 0);
            this.RangeHome = 1;
        }
        private void EnterFinishingState()
        {
            // ask the Town Crier to stop
            OnEscortComplete();

            Mobile escorter = GetEscorterWithSideEffects();
            if (escorter != null)
            {
                // make awards here
                Misc.Titles.AwardFame(escorter, 10, true);
            }

            // not going anywhere else, so clear the destination info
            //	and clear the escourt as 'my boss'
            Reset();

            // start cleanup on the mobile
            Cleanup();
        }

        public override void Cleanup()
        {
            StopFollow();
            BeginDelete();
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

        // We are within 10 tiles of the chest and will use normal pathing to find our Home
        //	Home is now the chest location
        private void FindChest()
        {
            if (this.GetDistanceToSqrt(ChestLocation) != 1.0)
                return; // keep wandering

            // stop here and get ready to dig
            this.Home = Location;
            this.RangeHome = 0;

            // ready for digging state
            State = QuestState.AtChest;
        }

        // We are 1 tile from the chest location. We will now begin warning players to stand back.
        private void ReadyChest()
        {
            // have all players stand back
            IPooledEnumerable eable = this.GetMobilesInRange(2);
            foreach (Mobile m in eable)
            {
                if (m is PlayerMobile)
                {
                    Direction = GetDirectionTo(m);
                    TimeSpan ts = DateTime.UtcNow - m_LastReadyChestTalk;
                    if (ts.TotalSeconds > 7 || m_LastReadyChestTalk == DateTime.MinValue)
                    {
                        if (Utility.RandomBool())
                            this.Say("Excuse me {0}, but you will need to stand back.", m.Name);
                        else
                            this.Say("Please stand back {0} while I dig this up.", m.Name);
                        m_LastReadyChestTalk = DateTime.UtcNow;
                    }
                    eable.Free();
                    return;
                }
            }
            eable.Free();

            // thank the players for moving :P
            if (m_LastReadyChestTalk != DateTime.MinValue)
                this.Say("Thank you.");

            this.Say("You may wish to hide, there are sometimes guardians.");

            // enter the ready to dig state
            State = QuestState.ReadyDig;
        }

        // Okay. All's well, we can start digging
        private void DigChest()
        {
            // face the chest and dig baby!
            Direction = GetDirectionTo(ChestLocation);

            // stay. if we don't, the wander code will make us keep turning
            ControlOrder = OrderType.Stay;

            TreasureMap map = null;
            try
            { map = FindMap(); }
            catch
            {
                State = QuestState.Error;
                return;
            }

            // make sure the map has not been completed yet
            if (map.Completed == false)
            {   // Completed will be set to true in OnNPCBeginDig()
                map.OnNPCBeginDig(this);    // Start digging man!
            }
            else
            {   // we need to now of this ever happens
                State = QuestState.Error;
                return;
            }

            // We now start waiting for the map to go into 'completed status'
            State = QuestState.Waiting;
        }

        public override void OnThink()
        {
            base.OnThink();
            ProcessState();
        }

        protected override bool OnMove(Direction d)
        {
            if (!base.OnMove(d))
                return false;

            ProcessState();
            return true;
        }

        private bool ProcessState()
        {
            // if we were close, but were gated out, or left because we got into a fight,
            //	backtrack to an appropriate state
            if (BackTrack())
                return ProcessState();

            QuestState oldState = State;
            switch (State)
            {
                case QuestState.Initialize: // initialize startup states
                    State = QuestState.Ready;
                    break;

                case QuestState.Ready:      // just wait here for an escort
                    break;

                case QuestState.Paid:       // user has paid us, transition to Journey
                    State = QuestState.Journey;
                    break;

                case QuestState.Journey:    // user is still trying to get us near the chest
                    FindGeneralLocation();  //	are we within 10 tiles?	
                    break;

                case QuestState.ChestMarch: // start searching for the exact location
                    FindChest();            //	are we within 1 tile
                    break;

                case QuestState.AtChest:    // tell players to stand back
                    ReadyChest();           //	clear players from the area
                    break;

                case QuestState.ReadyDig:   // start digging
                    DigChest();             //	dig it baby!
                    break;

                case QuestState.Waiting:    // waiting for the map to go 'completed'
                    TestComplete();
                    break;

                case QuestState.Finishing:  // award fame, clear the town crier, etc..
                    State = QuestState.Done;
                    break;

                case QuestState.Done:       // all done
                    DebugSay(DebugFlags.AI, "Quest completed with state {0}", State.ToString());
                    break;

                default: break;             // wtf!
            }

            return State != oldState;
        }

        // backtrack to a previous state if needed
        private bool BackTrack()
        {
            QuestState oldState = State;

            switch (State)
            {
                case QuestState.ChestMarch: // start searching for the exact location
                                            // did we somehow get distracted -- fight? , gate?
                                            // If we are 5 tiles outside the 10 tile perimeter, reenter Journey mode
                    if (this.GetDistanceToSqrt(ChestLocation) > 15.0)
                        State = QuestState.Journey;
                    break;

                case QuestState.AtChest:    // tell players to stand back
                case QuestState.ReadyDig:   // start digging
                case QuestState.Waiting:    // waiting for the map to go 'completed'
                                            // did we somehow get distracted -- fight? , gate?
                                            // If we are 5 tiles outside the 10 tile perimeter, reenter Journey mode
                    if (this.GetDistanceToSqrt(ChestLocation) > 15.0)
                        State = QuestState.Journey;
                    // if we're no longer next to the chest, reenter ChestMarch
                    else if (this.GetDistanceToSqrt(ChestLocation) != 1.0)
                        State = QuestState.ChestMarch;
                    break;

                default: break;
            }

            if (oldState != State)
                DebugSay(DebugFlags.AI, "I am backtracking to the {0} state again", State.ToString());

            return oldState != State;
        }

        // Perform new state initialization
        private void DoEnterState()
        {
            switch (State)
            {
                case QuestState.Initialize: // initialize startup states
                                            // no enter state
                    break;

                case QuestState.Ready:      // just wait here for an escort
                                            // no enter state
                    if (Destination == Point3D.Zero)
                    {
                        TreasureMap map = null;
                        if (this.Backpack == null)
                        {
                            LogHelper Logger = new LogHelper("EscortableProblem.log", false);
                            //Logger.Log(LogType.Text, "The player:");
                            //Logger.Log(LogType.Mobile, this.ControlMaster);
                            Logger.Log(LogType.Text, "Treasure Hunter has no backpack:");
                            Logger.Log(LogType.Mobile, this);
                            Logger.Finish();
                            break;
                        }
                        // delete contraban. For now, lighthouse passes count as contraban
                        ArrayList items = new ArrayList(this.Backpack.Items);
                        foreach (Item i in items)
                        {
                            if (i is TreasureMap)
                            {
                                map = i as TreasureMap;
                                break;
                            }
                        }
                        if (map == null)
                        {
                            LogHelper Logger = new LogHelper("EscortableProblem.log", false);
                            //Logger.Log(LogType.Text, "The player:");
                            //Logger.Log(LogType.Mobile, this.ControlMaster);
                            Logger.Log(LogType.Text, "Treasure Hunter map is null:");
                            Logger.Log(LogType.Mobile, this);
                            Logger.Finish();
                            break;
                        }
                        // configure our distination (to the chest)
                        Destination = new Point3D(map.ChestLocation, 0);
                        Region dest_reg = Region.Find(base.GetDestination(), this.Map);
                        if (dest_reg == null || dest_reg.Name == null || dest_reg.Name == "")
                        {
                            Console.WriteLine("Treasure Hunter traveling to the Map.DefaultRegion region. (FYI)");

                        }
                        if (Destination != Point3D.Zero)
                            Console.WriteLine("Treasure Hunter traveling to: " + base.Destination);
                        else
                            Console.WriteLine("Treasure Hunter traveling an unnamed region.");
                        if (Destination == Point3D.Zero)
                        {
                            LogHelper Logger = new LogHelper("EscortableProblem.log", false);
                            //Logger.Log(LogType.Text, "The player:");
                            //Logger.Log(LogType.Mobile, this.ControlMaster);
                            Logger.Log(LogType.Text, "Treasure Hunter region is null:");
                            Logger.Log(LogType.Mobile, this);
                            Logger.Finish();
                        }
                    }
                    break;

                case QuestState.Paid:       // user has paid us, transition to Journey
                                            // no enter state
                    break;

                case QuestState.Journey:    // user is still trying to get us near the chest
                                            // no enter state
                    break;

                case QuestState.ChestMarch: // start searching for the exact location
                    EnterChestMarchState();
                    break;

                case QuestState.AtChest:    // tell players to stand back
                                            // no enter state
                    break;

                case QuestState.ReadyDig:   // start digging
                                            // no enter state
                    break;

                case QuestState.Waiting:    // waiting for the map to go 'completed'
                                            // no enter state
                    break;

                case QuestState.Finishing:  // award fame, clear the town crier, etc..
                    EnterFinishingState();
                    break;

                case QuestState.Done:       // all done
                                            // no enter state
                    break;

                default: break;             // wtf!
            }

            DebugSay(DebugFlags.AI, "I am entering {0} state", State.ToString());
        }

        private void TestComplete()
        {
            TreasureMap map = null;
            try
            { map = FindMap(); }
            catch
            {
                State = QuestState.Error;
                return;
            }

            if (map.Completed == true)
                State = QuestState.Finishing;

            return;
        }


        public override bool OnDragDrop(Mobile from, Item dropped)
        {

            if (HasEscort(from) == false)
            {
                // wea: check to make sure they have sufficient control slots before 
                // entering new quest state and accepting the dragdrop
                if ((from.FollowerCount + this.ControlSlots) <= from.FollowersMax)
                {
                    if (CheckGems(from, dropped))
                    {
                        State = QuestState.Paid;
                        dropped.Delete();
                        DebugSay(DebugFlags.AI, "I am entering {0} state", State.ToString());
                        AcceptEscorter(from);
                        return true;
                    }
                }
                else
                    this.Say("Thou art rather too busy to accept further followers. Free thy hands first, perhaps?");
            }
            else
                Say("I see you already have an escort.");

            return base.OnDragDrop(from, dropped);
        }


        private bool CheckGems(Mobile from, Item dropped)
        {
            // try block so we can use a finally block
            int oldSpeechHue = this.SpeechHue;
            try
            {
                this.SpeechHue = 0x23F;

                if ((int)State >= (int)QuestState.Paid)
                {
                    SayTo(from, "I've already been paid, but thank you just the same!");
                    SayDestinationTo(from);
                    return false;
                }
                else
                {
                    if (dropped.Amount >= m_GemsRequired &&                            // enough?
                        dropped.GetType() == TreasureMapChest.m_GemTypes[m_GemIndex]) // the right type?
                    {
                        SayTo(from, "Ah, {0}, very good.", GetGemName(dropped.GetType(), true));
                        return true;
                    }
                    else
                    {
                        SayTo(from, "Get back to me when you have {0} {1} for me.", m_GemsRequired, GetGemName(TreasureMapChest.m_GemTypes[m_GemIndex], true));
                        return false;
                    }
                }
            }
            catch (Exception e) // if we've got a finally block, might as well have a catch
            {
                SayTo(from, "Ooh, I don't feel so good. Contact a Game Master!");
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Empty(): " + e.Message);
                System.Console.WriteLine(e.StackTrace);

                return false;
            }
            finally
            {
                this.SpeechHue = oldSpeechHue;
            }
        }

        public override bool AcceptEscorter(Mobile m)
        {
            if ((int)State < (int)QuestState.Paid && HasEscort(m) == false)
            {
                Say("For {0} {1} you may accompany me on my treasure hunt.", m_GemsRequired, GetGemName(TreasureMapChest.m_GemTypes[m_GemIndex], true));
                return false;
            }
            else if (HasEscort(m) && ControlMaster != m)
            {
                Say("I see you already have an escort.");
                return false;
            }

            return base.AcceptEscorter(m);
        }
        #region OnSee(TreasureMapChest)

        public override void OnSee(Mobile from, Item item)
        {
            base.OnSee(from, item);
            if (item is TreasureMapChest)
                switch (Utility.Random(3))
                {
                    case 0:
                        Say("I got this.");
                        break;
                    case 1:
                        Say("I think I see something!");
                        break;
                    case 2:
                        Say("Come on baby...");
                        break;
                }
        }
        #endregion OnSee(TreasureMapChest)
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;

            writer.Write(version);          // write version
            writer.Write((int)m_QuestState);    // version 1
            writer.Write(m_GemIndex);
            writer.Write(m_GemsRequired);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    m_QuestState = (QuestState)reader.ReadInt();
                    m_GemIndex = reader.ReadInt();
                    m_GemsRequired = reader.ReadInt();
                    goto case 0;

                case 0:
                    break;
            }
        }
    }
}