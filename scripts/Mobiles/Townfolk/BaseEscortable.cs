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

/* Scripts/Mobiles/Townfolk/BaseEscortable.cs
 * ChangeLog
 *  4/13/2024, Adam
 *      Add customizable Destination, Lead On, and We Have Arrived text
 *  3/30/2024, Adam (Rename GetEscorter() => GetEscorterWithSideEffects())
 *      This is at least the second time I've introduced a problem calling the seemingly innocuously named GetEscorter()
 *      this fucking call does all sorts of shit including the control order (which borked my Treasure Hunter.)
 *      To make this a method little less likely to cause problems in the future, I've renamed it as above.
 *      At some point it should get a better name / rewrite.
 *  3/29/2024, Adam
 *      We allow for customizable speeds, so removed RunUO's hardcoded ActiveSpeed, PassiveSpeed, and CurrentSpeed
 *  3/26/2024, Adam
 *      * m_AbandonDelay, allow individual escortables to have an extended abandon delay
 *      * m_StepDelay, allow individual escortables to have a slower walking speed 
 *      * m_LastSeenEscorter, age-old bug fix. This was never serialized which would cause the NPC to be deleted on restart
 *      * m_AllowMagicTravel, setting to false will cause the escortable to complain about be afraid of magic travel
 *  10/16/2023, Adam (EscorterAbandonment())
 *      You can now abandon escorts with the keyword 'abandon'
 *      (Usual timeouts will still apply)
 * 5/28/23, Yoar
 *      Reworked how the escort's destination location is described.
 *      We now prefer town/dungeon/IOB regions when describing the region we want to go to.
 * 5/4/23, Adam (Siege backpack gold)
 *      Remove backpack gold on Siege
 *      https://www.uoguide.com/Siege_Ruleset#Townsfolk
 *      https://www.uoguide.com/Siege_Perilous
 * 10/2/21, Adam
 *      Exclude destinations that are within 100 tiles from where we stand.
 * 9/3/21, Adam: (DefaultRegionContains())
 *      Some destinations (like skara brey east docks) are not in a region
 *      other than the default region, and since dest.Contains() fails the point test
 *      we will accept it here.
 *      Note: the expanded escortable system generates destinations that fall outside the normal regions.
 * 8/18/21, Adam (PickRandomDestination)
 *      Add logic to prevent spawned escort from selecting a destination in a region where they currently are.
 * 8/15/21, Adam: CheckAtDestination()
 *      Make sure the escorter didn't just lure the escort through a gate without actually following them
 *      we check GetDistanceToSqrt to escorter
 *	2/16/11, adam
 *		don't allow profitable farming of blue townsfolk from a region which is usually guarded.
 *		Note: murders already only get 1/3 of creatures loot, so this is a double whammy for them
 *  2/27/06, Adam
 *      Log when an Escortable NPC is Abandoned.
 *      This is important for addressing player complaints regarding the high cost 'Treasure Hunter' NPCs
 *	06/22/06, Adam
 *		Remove the "RETURNING 2 MINUTES" text from the console output
 *	05/18/06, Adam
 *		- rewrite to elimnate named locations and replace with Point locations.
 *			This change lets us set a distination independent of the region name.
 *		- double fame for unguarded locations
 *	04/27/06, weaver
 *		- Added a virtual TimeSpan AbandonDelay to control delay before escort
 *		abandons its owner.
 *		- Altered abandon logic to use this new function instead of fixed 2 minute time.
 *	02/11/06, Adam
 *		Make common the formatting of sextant coords.
 *	1/27/06, Adam
 *		add GetMaster(). Like GetEscorter but without all the kooky side-effects :\
 *		Used in: AddCustomContextEntries()
 *		Note: GetEscorter() was doing all sorts of stuff including set ControlOrder
 *	1/20/06, Adam
 *		1. More virtuals to over ride text and behaviors
 *		2. unfortunately the BaseEscortable class relies on GetDestination() being
 *		called from OnThink() to reload the destination info from the saved string
 *		after a WorldLoad  ... lame!
 *		We should find a better way to handle this and eliminate this hack
 *	1/16/06, Adam
 *		Also add a virtual ArrivedSpeak() so we can have a custom "Arrived" message
 *	1/15/06, Adam
 *		Add support for coordinate based escorts instead of just town/dungeons.
 *	1/13/06, Adam
 *		Virtualize the distribution of loot so that it can be over ridden in the derived class.
 *		Extend the system to give items as well as gold: ProvideLoot(), GiveLoot( Item item )
 *	6/27/04, Pix
 *		lowered gold in their backpack
 *	6/27/04, Pix
 *		Further tweaks to escorts:
 *		Escort Delay: 10 minutes
 *		gold to town: 100-300
 *		gold to non-town: 200-400
 *  6/23/04, Pix
 *		Halved gold if going to town.
 *  6/22/04, Old Salty
 * 		Increased time between escorts from 5 to 20 minutes.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/24/04, mith
 *		Commented Compassion gain on escort complete, since virtues are now disabled.
 *	4/24/04, adam
 *		Commented out "bool gainedPath = false;"
 */

using Server.Commands;
using Server.ContextMenus;
using Server.Diagnostics;			// LogHelper
using Server.Items;
using Server.Regions;
using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace Server.Mobiles
{
    public class BaseEscortable : BaseOverland
    {

        public override long SlowMovement() { return m_StepDelay; }
        private long m_StepDelay = 0;
        [CommandProperty(AccessLevel.Seer)]
        public long StepDelay
        {   // milliseconds
            get { return m_StepDelay; }
            set { m_StepDelay = Math.Abs(value); }
        }

        TimeSpan m_AbandonDelay = TimeSpan.FromMinutes(2.0);    // default
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan AbandonmentTimer
        {
            get { return m_AbandonDelay; }
            set { m_AbandonDelay = value; }
        }

        private bool m_GiveReward = true;
        [CommandProperty(AccessLevel.Seer)]
        public bool GiveReward
        {
            get { return m_GiveReward; }
            set { m_GiveReward = value; }
        }

        private string m_ArrivedText = "We have arrived! I thank thee, {player}! I have no further need of thy services. Here is thy pay.";
        [CommandProperty(AccessLevel.Seer)]
        public string ArrivedText
        {
            get { return m_ArrivedText; }
            set { m_ArrivedText = value; }
        }

        private string m_LeadOnText = "Lead on! Payment will be made when we arrive in {destination}.";
        [CommandProperty(AccessLevel.Seer)]
        public string LeadOnText
        {
            get { return m_LeadOnText; }
            set { m_LeadOnText = value; }
        }

        private string m_DestinationText = "I am looking to go to {destination}, will you take me?";
        [CommandProperty(AccessLevel.Seer)]
        public string DestinationText
        {
            get
            {   // give a little help text
                SendSystemMessage("Hint: {destination} is replaced with the destination.");
                SendSystemMessage("Hint: {player} is replaced with the player's name.");
                return m_DestinationText;
            }
            set { m_DestinationText = value; }
        }

        public virtual void ArrivedSpeak(string name)
        {
            // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.
            Say(PatchText(m_ArrivedText));
        }

        public virtual void LeadOnSpeak(string dest)
        {   // "Lead on! Payment will be made when we arrive in {0}."
            Say(PatchText(m_LeadOnText));
        }

        public virtual void DestinationSpeak(string dest)
        {   // "I am looking to go to {0}, will you take me?"
            Say(PatchText(m_DestinationText));
        }

        private string PatchText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "hmm?";
            string dest = string.Format("{0}", DescribeLocation(this.Map, GetDestination()));
            if (Regex.IsMatch(dest, @"^\d"))
                text = text.Replace("arrive in", "arrive at");
            string new_text = Regex.Replace(text, "{destination}", dest);
            new_text = Regex.Replace(new_text, "{player}", (GetMaster() != null) ? GetMaster().Name : "");
            return new_text;
        }

        private bool m_AllowMagicTravel = true;
        [CommandProperty(AccessLevel.Seer)]
        public bool AllowMagicTravel
        {
            get { return m_AllowMagicTravel; }
            set { m_AllowMagicTravel = value; }
        }
        public override bool GateTravel => m_AllowMagicTravel;

        private TimeSpan m_EscortDelay = TimeSpan.FromMinutes(10.0);
        [CommandProperty(AccessLevel.Seer)]
        public virtual TimeSpan EscortDelay
        {
            get { return m_EscortDelay; }
            set { m_EscortDelay = value; }
        }
        //protected virtual TimeSpan EscortDelay { get { return TimeSpan.FromMinutes(10.0); } }

        // wea: added to control delay before abandoning
        public virtual TimeSpan AbandonDelay
        {
            get
            {
                //Console.WriteLine("RETURNING 2 MINUTES");
                return m_AbandonDelay;
            }
        }

        private Point3D m_Destination;
        private DateTime m_DeleteTime;
        private Timer m_DeleteTimer;

        public override bool Commandable { get { return false; } } // Our master cannot boss us around!

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Point3D Destination
        {
            get { return m_Destination; }
            // 5/31/2021, Adam
            //	added the fall back position of the default region. This is because historically, escortables only went to towns,
            //	but out Overland mobiles need access to the entire map
            set { m_Destination = value; }
        }

        private static Point3D[] m_TownNames = new Point3D[]
            {

                new Point3D(2275,1210,0),	// "Cove" 
				new Point3D(1495,1629,10),	// "Britain"
				new Point3D(1383,3815,0),	// "Jhelom"

				new Point3D(2466,544,0),		// "Minoc"
				new Point3D(3650,2653,0),	// "Ocllo"
				new Point3D(1867,2780,0),	// "Trinsic"

				new Point3D(2892,685,0),		// "Vesper"
				new Point3D(635,860,0),		// "Yew"

				new Point3D(632,2233,0),		// "Skara Brae"
				new Point3D(3732,1279,0),	// "Nujel'm"

				new Point3D(4442,1172,0),	// "Moonglow" 
				new Point3D(3714,2220,20),	// "Magincia"

				// new places!!
				new Point3D(4530,1378,23),	// "Britannia Royal Zoo"
			};

        [Constructable]
        public BaseEscortable()
        {

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
            AddItem(new FancyShirt(Utility.RandomNeutralHue()));
            AddItem(new ShortPants(Utility.RandomNeutralHue()));
            AddItem(new Boots(Utility.RandomNeutralHue()));

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new TwoPigTails(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new KrisnaHair(Utility.RandomHairHue())); break;
            }

            if (!Core.RuleSets.SiegeStyleRules())
                PackGold(50, 125);
        }

        public override bool OnBeforeDeath()
        {
            bool obd = base.OnBeforeDeath();

            if (obd && !GiveReward)
            {
                Container pack = this.Backpack;
                if (pack != null)
                {
                    // how much gold is on the creature?
                    Item[] golds = pack.FindItemsByType(typeof(Gold), true);
                    foreach (Item g in golds)
                    {
                        pack.RemoveItem(g);
                        g.Delete();
                    }
                }
            }

            // don't allow profitable farming of blue townsfolk from a region which is usually guarded.
            //	Note: murders already only get 1/3 of creatures loot, so this is a double whammy for them
            if (obd && Core.RuleSets.MortalisRules())
                if (!(this.Spawner != null && Region.Find(this.Spawner.Location, this.Spawner.Map) as Regions.GuardedRegion != null && Region.Find(this.Spawner.Location, this.Spawner.Map).IsGuarded))
                {
                    // first find out how much gold this creature is dropping
                    int MobGold = this.GetGold();

                    // reds get 1/3 of usual gold
                    int NewGold = MobGold / 3;

                    // first delete all dropped gold
                    Container pack = this.Backpack;
                    if (pack != null)
                    {
                        // how much gold is on the creature?
                        Item[] golds = pack.FindItemsByType(typeof(Gold), true);
                        foreach (Item g in golds)
                        {
                            pack.RemoveItem(g);
                            g.Delete();
                        }

                        this.PackGold(NewGold);
                    }
                }

            return obd;
        }

        public virtual bool SayDestinationTo(Mobile m)
        {
            /*Region dest = Region.Find(GetDestination(), this.Map);
			string name = "?";
			if (dest == null || dest.Name == null || dest.Name == "")
            {

            }*/

            if (!m.Alive)
                return false;

            Mobile escorter = GetEscorterWithSideEffects();

            if (escorter == null)
            {
                DestinationSpeak(DescribeLocation(this.Map, GetDestination()));
                return true;
            }
            else if (escorter == m)
            {
                LeadOnSpeak(DescribeLocation(this.Map, GetDestination()));
                return true;
            }

            return false;
        }

        private static Hashtable m_EscortTable = new Hashtable();

        public static Hashtable EscortTable
        {
            get { return m_EscortTable; }
        }

        public virtual bool AcceptEscorter(Mobile m)
        {
            Point3D dest = GetDestination();

            if (dest == Point3D.Zero)
                return false;

            Mobile escorter = GetEscorterWithSideEffects();

            if (escorter != null || !m.Alive)
                return false;

            BaseEscortable escortable = (BaseEscortable)m_EscortTable[m];

            if (escortable != null && !escortable.Deleted && escortable.GetEscorterWithSideEffects() == m)
            {
                Say("I see you already have an escort.");
                return false;
            }
            else if (m is PlayerMobile && (((PlayerMobile)m).LastEscortTime + EscortDelay) >= DateTime.UtcNow)
            {
                int minutes = (int)Math.Ceiling(((((PlayerMobile)m).LastEscortTime + EscortDelay) - DateTime.UtcNow).TotalMinutes);

                Say("You must rest {0} minute{1} before we set out on this journey.", minutes, minutes == 1 ? "" : "s");
                return false;
            }
            else if (SetControlMaster(m))
            {
                m_LastSeenEscorter = DateTime.UtcNow;

                if (m is PlayerMobile)
                    ((PlayerMobile)m).LastEscortTime = DateTime.UtcNow;

                LeadOnSpeak(DescribeLocation(this.Map, GetDestination()));
                m_EscortTable[m] = this;
                StartFollow();
                return true;
            }

            return false;
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

            Point3D dest = GetDestination();

            if (dest != Point3D.Zero && !e.Handled && e.Mobile.InRange(this.Location, 3))
            {
                if (m_Abandoning == false)
                {
                    if (e.HasKeyword(0x1D)) // *destination*
                        e.Handled = SayDestinationTo(e.Mobile);
                    else if (e.HasKeyword(0x1E)) // *i will take thee*
                        e.Handled = AcceptEscorter(e.Mobile);
                    else if (e.Speech.Contains("abandon", StringComparison.OrdinalIgnoreCase))
                        e.Handled = EscorterAbandonment(e.Mobile);
                }
                else if (e.HasKeyword(0x1D) || e.HasKeyword(0x1E))
                {
                    e.Handled = true;
                    Say("Sorry, but I am not going anywhere.");
                }
            }
        }
        private bool m_Abandoning = false;
        public bool EscorterAbandonment(Mobile m)
        {
            Mobile escorter = GetEscorterWithSideEffects();

            if (escorter != m || !m.Alive)
                return false;

            m_Abandoning = true;
            StopFollow();
            Controlled = false;
            ControlMaster = null;
            if (AIObject != null)
                AIObject.Action = ActionType.Wander;
            OnThink();

            Timer.DelayCall(TimeSpan.FromSeconds(3), new TimerCallback(Emote));
            Timer.DelayCall(TimeSpan.FromMinutes(2), new TimerCallback(Delete));

            return true;
        }

        private void Emote()
        {
            if (this.Deleted == false && this.Map != Map.Internal && this.Map != null)
            {
                if (this.Female)
                    EmoteCommand.OnCommand(new CommandEventArgs(this, "e", "cry", new string[] { "cry" }));
                else
                    EmoteCommand.OnCommand(new CommandEventArgs(this, "e", "spit", new string[] { "spit" }));
            }
        }

        public override void OnAfterDelete()
        {
            if (m_DeleteTimer != null)
                m_DeleteTimer.Stop();

            m_DeleteTimer = null;

            base.OnAfterDelete();
        }

        public override void OnThink()
        {
            Mobile e = null;
            if ((e = GetMaster()) != null && e.CanSee(this))
                m_LastSeenEscorter = DateTime.UtcNow;

            // unfortunately the BaseEscortable class relies on GetDestination() being
            //	called from OnThink() to reload the destination info from the saved string
            //	after a WorldLoad  ... lame!
            //	We should find a better way to handle this and eliminate this hack
            GetDestination(); /* hack */

            base.OnThink();
            CheckAtDestination();
        }

        protected override bool OnMove(Direction d)
        {
            if (!base.OnMove(d))
                return false;

            CheckAtDestination();

            return true;
        }

        public virtual void StartFollow()
        {
            StartFollow(GetEscorterWithSideEffects());
        }

        public virtual void StartFollow(Mobile escorter)
        {
            if (escorter == null)
                return;

            // 3/289/2024, Adam: We allow for customizable speeds.
            //ActiveSpeed = 0.1;
            //PassiveSpeed = 0.2;

            ControlOrder = OrderType.Follow;
            ControlTarget = escorter;

            //CurrentSpeed = 0.1;
        }

        public virtual void StopFollow()
        {
            // 3/289/2024, Adam: We allow for customizable speeds.
            //ActiveSpeed = 0.2;
            //PassiveSpeed = 1.0;

            ControlOrder = OrderType.None;
            ControlTarget = null;

            //CurrentSpeed = 1.0;
        }

        private DateTime m_LastSeenEscorter;

        public bool HasEscort(Mobile m)
        {
            if (m != null)
            {
                foreach (var f in m.Followers)
                    if (f is BaseOverland)
                        return true;
            }
            return false;
        }

        // Like GetEscorter but without all the kooky side-effects :\
        new public Mobile GetMaster()
        {
            if (!Controlled)
                return null;

            Mobile master = ControlMaster;

            if (master == null)
                return null;

            if (master.Deleted || master.Map != this.Map || !master.InRange(Location, 30) || !master.Alive)
                return null;

            return master;
        }

        public virtual Mobile GetEscorterWithSideEffects()
        {
            if (!Controlled)
                return null;

            Mobile master = ControlMaster;

            if (master == null)
                return null;

            if (master.Deleted || master.Map != this.Map || !master.InRange(Location, 30) || !master.Alive)
            {
                StopFollow();

                TimeSpan lastSeenDelay = DateTime.UtcNow - m_LastSeenEscorter;

                if (lastSeenDelay >= AbandonDelay)
                {
                    master.SendLocalizedMessage(1042473); // You have lost the person you were escorting.
                    Say(1005653); // Hmmm.  I seem to have lost my master.

                    SetControlMaster(null);
                    m_EscortTable.Remove(master);

                    Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(Delete));
                    LogAbandon(master);
                    return null;
                }
                else
                {
                    ControlOrder = OrderType.Stay;
                    return master;
                }
            }

            if (ControlOrder != OrderType.Follow)
                StartFollow(master);

            m_LastSeenEscorter = DateTime.UtcNow;
            return master;
        }

        private void LogAbandon(Mobile master)
        {
            LogHelper Logger = new LogHelper("EscortableAbandoned.log", false);
            Logger.Log(LogType.Text, "The player:");
            Logger.Log(LogType.Mobile, master);
            Logger.Log(LogType.Text, "Has abandoned the Escortable NPC:");
            Logger.Log(LogType.Mobile, this);
            Logger.Finish();
        }

        public virtual void BeginDelete()
        {
            if (m_DeleteTimer != null)
                m_DeleteTimer.Stop();

            m_DeleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(3.0);

            m_DeleteTimer = new DeleteTimer(this, m_DeleteTime - DateTime.UtcNow);
            m_DeleteTimer.Start();
        }

        protected bool IsSafe(Point3D point_dest)
        {
            Region dest = Region.Find(point_dest, this.Map);
            // is it a guarded region?
            if (dest != null && dest is GuardedRegion && ((GuardedRegion)dest).IsGuarded)
                // yes it is, and it is actively guarded
                return true;

            return false;
        }

        public void GiveLoot(Item item)
        {   // adam: default is to try to pack an enchanted scroll
            GiveLoot(item, true);
        }

        public void GiveLoot(Item item, bool EScrollChance)
        {
            Mobile escorter = GetEscorterWithSideEffects();

            if (escorter == null)
                return;

            // sanity
            if (item == null)
            {
                Console.WriteLine("Warning: Null item generated in BaseEscortable.PackItem");
                return;
            }

            Container cont = escorter.Backpack;

            if (cont == null)
                cont = escorter.BankBox;

            if (cont == null || !cont.TryDropItem(escorter, item, false))
                item.MoveToWorld(escorter.Location, escorter.Map);
        }

        public virtual void ProvideLoot(Mobile escorter)
        {
            if (escorter == null)
                return;

            Gold gold = new Gold(200, 400);

            //lower gold if we're going to town
            Point3D dest = GetDestination();
            if (dest != Point3D.Zero && IsSafe(dest))
            {
                //Say("This is a Safe location");
                gold.Amount -= 100;
                Misc.Titles.AwardFame(escorter, 10, true);

            }
            else
            {   // more gold, more fame
                //Say("This is NOT a Safe location");
                Misc.Titles.AwardFame(escorter, 20, true);
            }

            GiveLoot(gold);
        }

        //public virtual void ArrivedSpeak(string name)
        //{
        //    // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.
        //    Say(1042809, name);
        //}

        //public virtual void LeadOnSpeak(string name)
        //{
        //    Say("Lead on! Payment will be made when we arrive in {0}.", name);
        //}

        //public virtual void DestinationSpeak(string name)
        //{
        //    Say("I am looking to go to {0}, will you take me?", name);
        //}

        public virtual bool CheckAtDestination()
        {
            Mobile escorter = GetEscorterWithSideEffects();

            if (escorter == null)
                return false;

            // 8/15/21, Adam: Make sure the escorter didn't just lure the escort through a gate without actually following them
            //  (GetDistanceToSqrt)
            if (There() && GetDistanceToSqrt(escorter) <= RangePerception)
            {
                // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.
                ArrivedSpeak(escorter.Name);

                if (GiveReward)
                    ProvideLoot(escorter);  // Give the player their reward for this escort	
                Reset();                    // not going anywhere
                OnEscortComplete();         // ask the Town Crier to stop
                Cleanup();                  // start the delete timer

                return true;
            }

            return false;
        }

        public BaseEscortable(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)4); // version

            // version 4
            writer.Write(m_GiveReward);

            // version 3
            writer.Write(m_DestinationText);
            writer.Write(m_LeadOnText);
            writer.Write(m_ArrivedText);

            // version 2
            writer.Write(m_AllowMagicTravel);
            writer.Write(m_EscortDelay);

            // Version 1
            writer.Write(m_AbandonDelay);
            writer.Write(m_StepDelay);
            writer.Write(m_LastSeenEscorter);   // bug fix

            // version 0
            writer.Write(Destination);

            writer.Write(m_DeleteTimer != null);

            if (m_DeleteTimer != null)
                writer.WriteDeltaTime(m_DeleteTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        m_GiveReward = reader.ReadBool();
                        goto case 3;
                    }
                case 3:
                    {
                        m_DestinationText = reader.ReadString();
                        m_LeadOnText = reader.ReadString();
                        m_ArrivedText = reader.ReadString();
                        goto case 2;
                    }
                case 2:
                    {
                        m_AllowMagicTravel = reader.ReadBool();
                        m_EscortDelay = reader.ReadTimeSpan();
                        goto case 1;
                    }
                case 1:
                    {
                        m_AbandonDelay = reader.ReadTimeSpan();
                        m_StepDelay = reader.ReadLong();
                        m_LastSeenEscorter = reader.ReadDateTime();   // bug fix
                        goto case 0;
                    }
                case 0:
                    {
                        Destination = reader.ReadPoint3D();

                        if (reader.ReadBool())
                        {
                            m_DeleteTime = reader.ReadDeltaTime();
                            m_DeleteTimer = new DeleteTimer(this, m_DeleteTime - DateTime.UtcNow);
                            m_DeleteTimer.Start();
                        }
                        break;
                    }
            }
        }

        public override bool CanBeRenamedBy(Mobile from)
        {
            return (from.AccessLevel >= AccessLevel.GameMaster);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            Point3D dest = GetDestination();

            if (dest != Point3D.Zero && from.Alive)
            {
                Mobile escorter = GetMaster();

                if (escorter == null || escorter == from)
                    list.Add(new AskDestinationEntry(this, from));

                if (escorter == null)
                    list.Add(new AcceptEscortEntry(this, from));
                else if (escorter == from)
                    list.Add(new AbandonEscortEntry(this, from));
            }

            base.AddCustomContextEntries(from, list);
        }

        public virtual Point3D[] GetPossibleDestinations()
        {
            return m_TownNames;
        }

        public bool SystemInitialized()
        {
            if (Map.Felucca.RegionsSorted.Count == 0 || Map == null || Map == Map.Internal || Location == Point3D.Zero)
                return false; // Not yet fully initialized

            return true;
        }

        public virtual Point3D PickRandomDestination()
        {
            Point3D point;
            return PickRandomDestination(out point);
        }

        public virtual Point3D PickRandomDestination(out Point3D point)
        {
            point = new Point3D();

            if (SystemInitialized() == false)
                return Point3D.Zero; // Not yet fully initialized

            // sanity
            if (GetPossibleDestinations() == null || GetPossibleDestinations().Length == 0)
                return Point3D.Zero;

            Point3D[] px = GetPossibleDestinations();

            // don't select a location that is the same as the location where we are.
            for (int ix = 0; ix < px.Length; ix++)
            {
                point = px[Utility.Random(px.Length)];
                if (this.Region.Contains(point))
                    continue;

                // since some of these destinations may not be in regions, we also check distance to destination
                if (this.GetDistanceToSqrt(point) < 100)
                    continue;   // too close

                return point;
            }

            return Point3D.Zero;
#if JUNK

            /*object[,] places = new object[px.Length, 2];

			for (int ix = 0; ix < px.Length; ix++)
			{
				places[ix, 0] = px[ix];
				places[ix, 1] = Utility.RandomDouble();
			}

			// Bubble sort method.
			object[,] holder = new object[1, 2];
			for (int x = 0; x < px.Length; x++)
				for (int y = 0; y < px.Length - 1; y++)
					if ((double)places[y, 1] > (double)places[y + 1, 1])
					{
						// holder = places[y + 1];
						holder[0, 0] = places[y + 1, 0];
						holder[0, 1] = places[y + 1, 1];

						// places[y + 1] = places[y];
						places[y + 1, 0] = places[y, 0];
						places[y + 1, 1] = places[y, 1];

						// places[y] = holder;
						places[y, 0] = holder[0, 0];
						places[y, 1] = holder[0, 1];
					}

			for (int jx = 0; jx < px.Length; jx++)
			{
				Region reg = Find(places[jx, 0] as string);
				// keep trying if we pick a spot where we are
				if (reg == null
					|| reg.Contains(this.Location)
					|| reg.Name == null
					|| reg.Name == ""
					|| reg.Name == "DynRegion")
					continue;

				point = Point3D.Parse(places[jx, 0] as string);
				return reg;
			}

			return null;*/

            // Adam: remove this old implementation as it can loop forever if the array passed is bad.

            /*if (SystemInitialized() == false)
				return null; // Not yet fully initialized

			string[] possible = GetPossibleDestinations();
			string picked = null;
			Region test = null;

			while (picked == null && possible != null)
			{
				picked = possible[Utility.Random(possible.Length)];
				test = Find(picked);

				// keep trying if we pick a spot where we are
				if (test == null
					|| test.Contains(this.Location)
					|| test.Name == null
					|| test.Name == ""
					|| test.Name == "DynRegion")
					picked = null;
			}

			return test;*/
#endif
        }

        public Point3D GetDestination()
        {
            if (SystemInitialized() == false)
                return Point3D.Zero;    // Not yet fully initialized

            if (m_Destination == Point3D.Zero && m_DeleteTimer == null)
                m_Destination = PickRandomDestination();
            else
                return m_Destination;

            if (m_Destination != Point3D.Zero)
            {
                return m_Destination;
            }

            return (m_Destination = Point3D.Zero);
        }

        public class DeleteTimer : Timer
        {
            private Mobile m_Mobile;

            public DeleteTimer(Mobile m, TimeSpan delay)
                : base(delay)
            {
                m_Mobile = m;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                m_Mobile.Delete();
            }
        }

        // 6/27/23, Yoar: Decreased the range of our fake region from 100 to 12.
        public static int DefaultRegionRange = 12;

        public virtual bool There()
        {
            Point3D dest = GetDestination();

            if (dest == Point3D.Zero)
                return false;

            Region region = Region.Find(dest, this.Map);

            // 9/3/21, Adam: Some destinations (like skara brey east docks) are not in a region
            //  other than the default region, and since dest.Contains() fails the point test
            //  we will accept it here.
            //  Note: the expanded escortable system generates destinations that fall inside the default region.
            if (region.IsDefault)
                return (this.GetDistanceToSqrt(GetDestination()) <= DefaultRegionRange);

            return region.Contains(this.Location);
        }

        public void Reset()
        {
            // not going anywhere
            m_Destination = Point3D.Zero;
            Mobile escorter = GetEscorterWithSideEffects();
            if (escorter != null)
                m_EscortTable.Remove(escorter);
        }

        public virtual void Cleanup()
        {
            StopFollow();
            SetControlMaster(null);
            BeginDelete();
        }

        public Region Find(string name)
        {
            // add it if it is valid 
            Point3D location = Point3D.Zero;
            Region reg = null;
            if (name != null)   // added by Adam 5/24
            {
                try { location = Point3D.Parse(name); }
                catch { location = Point3D.Zero; }
            }
            if (location != Point3D.Zero)
            {   // add it then fall through
                // custom regions
                reg = Region.Find(location, Map.Felucca);
            }

            return reg;
        }

        public string DescribeLocation(Map map, Point3D p)
        {
            string coords = FormatCoordinates(p, map);
            string region = FormatRegion(p, map);

            string result = "";

            if (!String.IsNullOrEmpty(coords))
                result += coords;

            if (!String.IsNullOrEmpty(region))
                result += " in " + region;

            return result;
        }

        public static string FormatCoordinates(Point3D loc, Map map)
        {
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            if (Sextant.Format(loc, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
                return Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
            else
                return "somewhere";//string.Format("{0} {1}", p.X, p.Y);
        }

        public static string FormatRegion(Point3D loc, Map map)
        {
            ArrayList regions = Region.FindAll(loc, map);

            Region bestRegion = Map.Internal.DefaultRegion;

            for (int i = regions.Count - 1; i >= 0; i--)
            {
                Region region = (Region)regions[i];

                if (region.IsDefault)
                    continue;

                bestRegion = region;

                if (!String.IsNullOrEmpty(bestRegion.Name) && IsInterestingRegion(bestRegion))
                    break;
            }

            return bestRegion.Name;
        }

        private static bool IsInterestingRegion(Region region)
        {
            if (region.IsTownRules || region.IsDungeonRules)
                return true;

            if (region is StaticRegion)
            {
                StaticRegion sr = (StaticRegion)region;

                if (sr.GuildAlignment != Engines.Alignment.AlignmentType.None || sr.IOBZone)
                    return true;
            }

            return false;
        }
    }

    public class AskDestinationEntry : ContextMenuEntry
    {
        private BaseEscortable m_Mobile;
        private Mobile m_From;

        public AskDestinationEntry(BaseEscortable m, Mobile from)
            : base(6100, 3)
        {
            m_Mobile = m;
            m_From = from;
        }

        public override void OnClick()
        {
            m_Mobile.SayDestinationTo(m_From);
        }
    }

    public class AcceptEscortEntry : ContextMenuEntry
    {
        private BaseEscortable m_Mobile;
        private Mobile m_From;

        public AcceptEscortEntry(BaseEscortable m, Mobile from)
            : base(6101, 3)
        {
            m_Mobile = m;
            m_From = from;
        }

        public override void OnClick()
        {
            m_Mobile.AcceptEscorter(m_From);
        }
    }

    public class AbandonEscortEntry : ContextMenuEntry
    {
        private BaseEscortable m_Mobile;
        private Mobile m_From;

        public AbandonEscortEntry(BaseEscortable m, Mobile from)
            : base(6102, 3)
        {
            m_Mobile = m;
            m_From = from;
        }

        public override void OnClick()
        {
            m_Mobile.EscorterAbandonment(m_From);
            //m_Mobile.Delete(); // OSI just seems to delete instantly
        }
    }
}