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

/* Scripts\Engines\CTFSystem\CTFUtility.cs
 * CHANGELOG:
 * 67/1/10, adam
 *		remove misplaced simicolon
 * 5/25/10, adam
 *		update book.Version = 1.1 - add RespawnWithMana rule
 * 5/23/10, adam
 *		o check New RespawnWithMana player rule option to see if we should regen mana after death-respawn
 *		o New function RefreshPlayers() called from NewRound() to refresh all player stats (between rounds)
 * 4/10/10, adam
 *		initial framework.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using static Server.Utility;
namespace Server.Engines
{
    public partial class CTFControl : CustomRegionControl
    {
        public void OnDragDrop(Mobile broker, Mobile from, Item dropped)
        {
            if (dropped is BaseBook == false)
                return;

            // make sure Captain 1 is giving us this book, and we are at least into the registration phase.
            if (MobileOk(Captain1) && Captain1 == from && CurrentState > States.Quiescent)
            {
                // reset the magery table to system defaults.
                // because we have an AllowMagery setting that can turn on/off all magery, we need to keep a copy
                //	of the Default setup in case the player drops multiple books on the Fightbroker. For instance:
                // Book1 turns off magery, then book2 turns on Teleport, we would endup with a teleport only spell config.
                CustomRegion.RestrictedSpells.Clear();
                foreach (int spellID in DefaultsSpellConfig)
                    CustomRegion.SetRestrictedSpell(spellID, true);

                RuleBookParsing error = ReadRuleBook(dropped as BaseBook);

                // if ther user turned off magery, we process it now (after all individual properties have been set)
                if (AllowMagery == false)
                    AllowMageryImp = false;

                if (error == RuleBookParsing.Ok)
                {   // validate the rulebook values
                    // because of the way parsing works, B'bool' values should be ok
                    // lets check the int values:
                    if (Rounds < 1 || Rounds > 60)
                    {
                        Rounds = 4;
                        broker.SayTo(from, "You may not have more than 60 rounds per game. I'm setting the number of rounds to 4.");
                    }

                    if (RoundMinutes < 1 || RoundMinutes > 60)
                    {
                        RoundMinutes = 5;
                        broker.SayTo(from, "You may not have more than 60 minutes per round. I'm setting the minutes per round to 5.");
                    }

                    if (RoundMinutes * Rounds > 60)
                    {
                        RoundMinutes = 5;
                        Rounds = 4;
                        broker.SayTo(from, "You may not have more than 60 minutes per game. I'm setting the minutes per round to 5.");
                        broker.SayTo(from, "I'm setting the number of rounds to 4.");
                    }

                    if (FlagHPDamage < 1 || FlagHPDamage > 140)
                    {
                        FlagHPDamage = 140;
                        broker.SayTo(from, "Maximum flag damage is 140 hitpoints. I'm setting the flag hitpoint damage to 140.");
                    }

                    if (EvalIntCap < 0 || EvalIntCap > 9999)
                    {
                        EvalIntCap = 9999;
                        broker.SayTo(from, "Resetting the Evaluating Intelligence limit to 9999 (no limit.)");
                    }
                }

                if (error == RuleBookParsing.Ok == false)
                {
                    broker.SayTo(from, "I'm sorry, but your rule book has errors in it.");
                    // give them another 2 minutes to fix and return the rule book
                    m_StateTimer[States.WaitBookReturn] = DateTime.UtcNow + TimeSpan.FromSeconds(120);
                    return;
                }

                // all is well, but give them back their book anyway
                broker.SayTo(from, string.Format("Very good. We'll be using {0} by {1}.", (dropped as BaseBook).Title, (dropped as BaseBook).Author));
                BookReceived = true;
            }
            else if (CurrentState == States.Quiescent)
            {
                broker.SayTo(from, "You'll need to register first.");
            }
            else
            {   // someone other than captain 1 tried to give a rule book
                broker.SayTo(from, "I'll only accept a rule book from the initiator of this match.");
            }
            return;
        }

        private bool CTFSetupOK()
        {
            if (BlueBase == Point3D.Zero || RedBase == Point3D.Zero)
                return false;

            return true;
        }

        private enum RuleBookParsing { Ok, };

        private RuleBookParsing ReadRuleBook(BaseBook book)
        {
            CTFControl src = this as CTFControl;
            PropertyInfo[] props = src.GetType().GetProperties();
            RuleBookParsing result = RuleBookParsing.Ok;

            // for each page in the book
            for (int jx = 0; jx < book.PagesCount; jx++)
            {
                BookPageInfo info = book.Pages[jx];

                // for each line on the page
                for (int mx = 0; mx < info.Lines.Length; mx++)
                {
                    string text = info.Lines[mx];
                    if (text == null || text.Length < 1)
                        continue;

                    text = FormatProp(text, FormatPropMode.glue);

                    // find the prop
                    for (int i = 0; i < props.Length; i++)
                    {
                        if (text[0] == '+' || text[0] == '-')
                        {   // process bool
                            if (text.Substring(1).ToLower() == props[i].Name.ToLower())
                                if (props[i].CanWrite && IsPlayerProp(props[i]) && props[i].DeclaringType.Name == "CTFControl")
                                {
                                    //props[i].SetValue(dest, props[i].GetValue(src, null), null);
                                    if (text[0] == '+')
                                        props[i].SetValue(this, true, null);
                                    else
                                        props[i].SetValue(this, false, null);
                                }
                        }
                        else if (text.Contains(":"))
                        {   // process int
                            int ndx = text.IndexOf(':');
                            if (text.Substring(0, ndx).ToLower() == props[i].Name.ToLower())
                                if (props[i].CanWrite && IsPlayerProp(props[i]) && props[i].DeclaringType.Name == "CTFControl")
                                {
                                    int value = 0;
                                    if (int.TryParse(text.Substring(ndx + 1), System.Globalization.NumberStyles.AllowLeadingWhite, null, out value))
                                        props[i].SetValue(this, value, null);
                                }
                        }
                    }
                }
            }

            return result;
        }

        private BaseBook WriteRuleBook()
        {
            CTFControl src = this as CTFControl;
            PropertyInfo[] props = src.GetType().GetProperties();
            BrownBook book = new BrownBook("Default Rules", "Adam Ant", 1, true);
            List<string> booklines = new List<string>();
            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    if (props[i].CanRead && IsPlayerProp(props[i]) && props[i].DeclaringType.Name == "CTFControl")
                    {
                        string name = FormatProp(props[i].Name, FormatPropMode.parse);
                        if (name.Contains(" H P ")) name = name.Replace("H P", "HP"); // special case fixup
                        string line = "";
                        if (props[i].PropertyType == typeof(bool))
                            line = string.Format("{0}{1}", props[i].GetValue(src, null) is bool && (props[i].GetValue(src, null).ToString().ToLower() == "true") == true ? "+" : "-", name);
                        else if (props[i].PropertyType == typeof(int))
                            line = string.Format("{0}: {1}", name, props[i].GetValue(src, null).ToString());
                        booklines.Add(line);
                    }
                }
                catch (Exception e)
                {
                    Server.Diagnostics.LogHelper.LogException(e, string.Format("CTFControl : WriteRuleBook() : {0}", e.ToString()));
                }
                finally
                {

                }
            }

            booklines.Sort();
            foreach (string s in booklines)
                book.AddLine(s);

            book.Subtype = BaseBook.BookSubtype.CTFRules;

            book.Version = 1.1;         // add RespawnWithMana rule
                                        //book.Version = 1.0;		// original version

            return book;
        }

        private bool PlayerHasBook(Mobile m, BaseBook.BookSubtype type, double version)
        {
            if (m == null || m.Backpack == null)
                return false;

            ArrayList list = m.Backpack.FindAllItems();
            for (int ix = 0; ix < list.Count; ix++)
            {   // is it a book?
                BaseBook bb = list[ix] as BaseBook;
                // is it the right type?
                if (bb != null && bb.Subtype == type)
                    // is it the right version?
                    if (bb.Version >= version)
                        return true;
            }

            return false;
        }

        private BaseBook WriteHelpBook()
        {
            string[] text = new string[] {"You begin a Capture The",
                "Flag game by registering",
                "on the Fightbroker with",
                "the phrase 'register",
                "ctf'. The Fightbroker will",
                "then ask you to target",
                "the opposing team captain.",
                "One you and the opposing",
                "team captain have been",
                "selected, you will be given",
                "a Rule Book which you",
                "can modify and hand back",
                "the Fightbroker. You can",
                "change the rules by",
                "enabling or disabling Rule",
                "Book options (+) or (-).",
                "Once the rules have been",
                "given back to the",
                "Fightbroker, each captain",
                "along with all of their",
                "Party members will be",
                "teleported to the CTF",
                "battle front.",
                "GOAL: The two teams will",
                "be assigned to either the",
                "Red or Blue teams and",
                "teleported to their home",
                "base. One of the teams",
                "will start with the flag",
                "in their home base, this",
                "team is defending the",
                "flag. The team playing",
                "offense will come and try",
                "to capture the flag and",
                "take it back to their",
                "base to score. Once an",
                "offense team scores, the",
                "offense and defense roles",
                "will be swapped and the",
                "next round begins.",
                "Good Luck and have fun!"};

            RedBook book = new RedBook("Capture The Flag", "Adam Ant", 1, true);
            {
                try
                {
                    for (int ix = 0; ix < text.Length; ix++)
                        book.AddLine(text[ix]);
                }
                catch (Exception e)
                {
                    Server.Diagnostics.LogHelper.LogException(e, string.Format("CTFControl : WriteHelpBook() : {0}", e.ToString()));
                }
                finally
                {

                }
            }

            book.Subtype = BaseBook.BookSubtype.CTFHelp;
            book.Version = 1.0;
            book.Writable = false;
            return book;
        }

        private enum FormatPropMode { parse, glue }
        private string FormatProp(string text, FormatPropMode mode)
        {
            if (mode == FormatPropMode.glue)
                return text.Replace(" ", "");
            // parse
            string temp = "";
            for (int ix = 0; ix < text.Length; ix++)
            {
                if (Char.IsUpper(text[ix]))     // BlatFarb -> Blat Farb
                    temp += " ";

                temp += text[ix];
            }

            return temp.Trim();
        }

        private bool IsPlayerProp(PropertyInfo prop)
        {
            object[] attrs = prop.GetCustomAttributes(typeof(Server.CommandPropertyAttribute), false);

            if (attrs.Length > 0)
                return AccessLevel.Player >= (attrs[0] as Server.CommandPropertyAttribute).ReadLevel;
            else
                return false;
        }

        public class ActionTimer : Timer
        {
            private CTFControl m_CTFC;

            public ActionTimer(CTFControl ix, TimeSpan delay)
                : base(TimeSpan.FromSeconds(10), delay)
            {
                m_CTFC = ix;
                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                m_CTFC.OnEvent();
            }
        }

        public void Say(string text)
        {   // realtime feedback
            Console.WriteLine(text);
            PublicOverheadMessage(0, 0x3B2, false, text);
        }

        public void DebugSay(string text)
        {
            if (this.Debug == true)
                Say(text);
        }

        // you cannot talk to the controller but we can send text to it from the fight broker.
        public override bool HandlesOnSpeech { get { return false; } }

        // all bool data
        private void ClearBoolData()
        {
            m_IntData[(int)IntNdx.BoolData] = 0;
        }

        // Called from Region.OnPlayerAdd
        public void OnPlayerAdd(Mobile m)
        {
            if (GetTeam(m) == Team.None)
            {   // boot non players if they log into or enter the CTF region.
                if (m.AccessLevel == AccessLevel.Player)
                    Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(Strandedness_Callback), m);
            }
            else
            {   // send tardy player back to his base
                Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(TeleportPlayerCTF_Callback), m);

                // restart their arrow
                Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(RestartArrow_Callback), m);
            }
        }

        public Team Offense { get { return Defense == Team.Red ? Team.Blue : Team.Red; } }

        public void TeleportPlayerCTF_Callback(object o)
        {
            TeleportPlayerCTF(o as Mobile, GetTeamBase(o as Mobile));
        }

        public void RestartArrow_Callback(object o)
        {
            // show the arrow
            if (Round != 0)
            {
                // show arrow to offense
                if (GetTeam(o as Mobile) == Offense)
                    ShowPlayerArrow(o as Mobile);

                // and to defense if the flag is away from home
                if (GetTeam(o as Mobile) == Defense && !IsFlagHome())
                    ShowPlayerArrow(o as Mobile);
            }
        }

        //  Twizzle, twazzle, twozzle, twome - time for this one to come home.
        public void Strandedness_Callback(object o)
        {
            //	they MAY have been a player at one time, so we need to deal with clothes
            DeColorizePlayer(o as Mobile);
            // force map to felucca since it's possible to get booted to the current CTF map!
            Misc.Strandedness.ProcessStranded(o as Mobile, false, Map.Felucca);
        }

        private FlagRespawnTimer m_FlagRespawnTimer = null;
        public class FlagRespawnTimer : Timer
        {
            private CTFControl m_ctfc;
            private double m_msCountcown;
            private int m_round;

            // flag recovery vs flag reset - complicated
            // to the best of my understanding, if you leave the area of the flag AT ALL, you will only get a flag reset
            //	if you are able to stay with the flag, then you will get a flag recovery and it will be much faster.
            //	
            private bool m_RecoveryPossible = true;

            // total speed up so far. This value is lost (added back to the timer) if thee are no defenders near the flag.
            double m_bonus = 0;

            public FlagRespawnTimer(CTFControl ctfc)
                : base(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1))
            {
                Priority = TimerPriority.TwoFiftyMS;
                m_ctfc = ctfc;
                m_msCountcown = new TimeSpan(0, 0, m_ctfc.FlagRespawnSeconds).TotalMilliseconds;
                m_round = m_ctfc.Round;     // saved to check if a new round started
            }

            protected override void OnTick()
            {
                if (m_msCountcown <= 0)
                {
                    this.Stop();
                    this.Flush();

                    // basic sanity
                    if (m_ctfc != null && !m_ctfc.Deleted && m_ctfc.Flag != null && !m_ctfc.Flag.Deleted)
                    {
                        // make sure we've not started a new round, if so just abandon the reset
                        if (m_round != m_ctfc.Round)
                            return;

                        m_ctfc.RespawnFlag(m_RecoveryPossible);
                    }
                }
                else
                {
                    // make sure we've not started a new round, if so just abandon the reset
                    if (m_round != m_ctfc.Round)
                    {
                        this.Stop();
                        this.Flush();
                        return;
                    }

                    double GuardingFlag = 0.0;
                    if (m_RecoveryPossible == true)                 // don't waste your time calculating if no recovery is possible
                    {
                        GuardingFlag = m_ctfc.GuardingFlag();       // how many percent of defenders are guarding the flag

                        if (GuardingFlag == 0.0)                    // if there are no defenders no recovery bonus possible
                        {
                            m_RecoveryPossible = false;             // defenders just lost all hope of a (fast) recovery
                            m_ctfc.FlagRecoveryBonusLost();
                        }
                    }

                    if (m_RecoveryPossible == true)
                    {   // fast timer 
                        m_bonus += 1000.0 * (GuardingFlag / 100.0);                 // bonus timer speed up
                        m_msCountcown -= 1000 + 1000.0 * (GuardingFlag / 100.0);    // count down offset + bonus
                    }
                    else
                    {   // slow timer
                        if (m_RecoveryPossible == false && m_bonus != 0.0)          // defenders left the flag area
                        {
                            m_msCountcown += m_bonus;                               //	and they are now loosing their bonus
                            m_bonus = 0.0;
                        }
                        m_msCountcown -= 1000;
                    }
                }
            }
        }

        public void FlagRecoveryBonusLost()
        {
            BroadcastMessage(GetTeam(Defense), SystemMessageColor, "Flag recovery no longer possible.");
        }

        private Point3D GetTeamBase(Mobile m)
        {
            return GetTeamBase(GetTeam(m));
        }

        private Point3D GetTeamBase(Team team)
        {
            return (team == Team.Red) ? m_RedBase : m_BlueBase;
        }


        private Item GetTeamChest(Mobile m)
        {
            return GetTeamChest(GetTeam(m));
        }

        private Item GetTeamChest(Team team)
        {
            return (team == Team.Red) ? RedBaseChest : BlueBaseChest;
        }

        // Count the team members guarding the flag.
        // less than 6 tiles from the Flag counts as 'guarding the flag'
        public double GuardingFlag()
        {
            Dictionary<PlayerMobile, PlayerContextData> team = GetTeam(Defense);
            int count = 0;
            if (team != null && Flag != null && !Flag.Deleted)
            {
                foreach (KeyValuePair<PlayerMobile, PlayerContextData> kvp in team)
                {   // less than 6 tiles counts as 'guarding the base'
                    if (kvp.Key.GetDistanceToSqrt(Flag.Location) < 6.0)
                        count++;
                }
            }
            double percent = (double)count / (double)team.Count * 100.0;
            return percent;
        }

        public void RespawnFlag(bool bRecovered)
        {
            // if recovered, then the team qualifies for a flag recovery, otherwise it's a flag reset
            // In  Halo, flag recoveries are faster and because Desense was NEAR the flag all the while it was on the ground
            Flag.MoveToWorld(GetTeamBase(Defense), CustomRegion.Map);
            BroadcastMessage(GetTeam(Defense), SystemMessageColor, bRecovered ? "Flag recovered." : "Flag reset.");

            // hide flag arrow to defense
            BroadcastArrow(GetTeam(Defense), false);
        }

        public void RespawnMobile(Mobile m)
        {
            Point3D destination = Spawner.GetSpawnPosition(CustomRegion.Map, GetTeamBase(m), 6, SpawnFlags.None, m);
            TeleportPlayer(m as PlayerMobile, CustomRegion.Map, destination);
            Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(ResurrectMobile_Callback), m);
        }

        public void ResurrectMobile_Callback(object o)
        {
            ResurrectMobile(o as Mobile);
        }

        private bool IsFlagHome()
        {
            if (Flag == null)
                return false;

            if (Flag.GetDistanceToSqrt(GetTeamBase(Defense)) <= 6.0 && Flag.Parent == null)
                return true;

            return false;
        }

        private bool IsPlayerHome(Mobile m)
        {
            if (m == null || GetTeam(m) == Team.None)
                return false;

            if (m.GetDistanceToSqrt(GetTeamBase(GetTeam(m))) <= 6.0)
                return true;

            return false;
        }

        public void ClearHands(Mobile m)
        {
            ClearHand(m, m.FindItemOnLayer(Layer.OneHanded));
            ClearHand(m, m.FindItemOnLayer(Layer.TwoHanded));
        }

        public void ClearHand(Mobile m, Item item)
        {
            if (item != null && item.Movable)
            {
                Container pack = m.Backpack;

                if (pack != null)
                    pack.DropItem(item);
            }
        }

        TimeSpan GetFlagRespawnTime()
        {
            return new TimeSpan(0, 0, FlagRespawnSeconds);
        }

        TimeSpan GetRoundRemainingTime()
        {
            if (m_StateTimer.ContainsKey(States.PlayRound))
            {
                if (DateTime.UtcNow > m_StateTimer[States.PlayRound])
                    return TimeSpan.Zero;
                else
                    return m_StateTimer[States.PlayRound] - DateTime.UtcNow;
            }
            else
                return TimeSpan.Zero;
        }

        TimeSpan GetGameRemainingTime()
        {
            if (Round == 0)
                return TimeSpan.Zero;

            int rounds_left = Rounds - Round;
            return new TimeSpan(0, rounds_left * RoundMinutes, (int)GetRoundRemainingTime().TotalSeconds);
        }

        private Dictionary<PlayerMobile, PlayerContextData> GetOpposingTeam(Team t)
        {
            if (t == Team.Red)
                return m_BlueTeam;
            if (t == Team.Blue)
                return m_RedTeam;
            return null;
        }

        private Dictionary<PlayerMobile, PlayerContextData> GetTeam(Team t)
        {
            if (t == Team.Red)
                return m_RedTeam;
            if (t == Team.Blue)
                return m_BlueTeam;
            return null;
        }

        private Team GetTeam(Mobile m)
        {
            PlayerMobile pm = m as PlayerMobile;
            if (pm == null)
                return Team.None;
            if (m_RedTeam.ContainsKey(pm))
                return Team.Red;
            if (m_BlueTeam.ContainsKey(pm))
                return Team.Blue;
            return Team.None;
        }

        public int TeamColor(Team team)
        {
            if (team == Team.Red)
                return RedTeamClothing;
            else
                return BlueTeamClothing;
        }

        PlayerContextData GetPlayerContextData(Mobile m)
        {
            Dictionary<PlayerMobile, PlayerContextData> team = GetTeam(GetTeam(m));
            if (team == null)
                return null;

            if (team.ContainsKey(m as PlayerMobile))
                return team[m as PlayerMobile];

            return null;
        }

        private void SetFlag(BoolData flag, bool value)
        {
            if (value)
                m_IntData[(int)IntNdx.BoolData] |= (int)flag;
            else
                m_IntData[(int)IntNdx.BoolData] &= ~(int)flag;
        }

        private bool GetFlag(BoolData flag)
        {
            return (((BoolData)m_IntData[(int)IntNdx.BoolData] & flag) != 0);
        }

        // list of players asking to register today. this list is used to prevent players from collecting excessive books
        //	and tying up the system by holding it in a registration state whereby preventing other players from playing.
        protected class PlayerRequests
        {
            public DateTime LastQuestion = DateTime.UtcNow;    // last time the player tried to register
            public int Count = 0;                           // number of registrations allowed
#if DEBUG
            private const int Limit = 100;                  // number of times a player can request registration (without playing)
#else
            private const int Limit = 6;					// number of times a player can request registration (without playing)
#endif
            public bool Valid                               // this is an ASK for permission, too many asks and the answer is no!
            {
                get
                {
                    // a new day == a fresh start!
                    if (LastQuestion.DayOfYear != DateTime.UtcNow.DayOfYear)
                    {
                        LastQuestion = DateTime.UtcNow;
                        Count = 0;
                        return true;
                    }

                    // same day, limit requests
                    if (Count++ > Limit)
                        return false;

                    // okay, all clear to register
                    return true;
                }
            }
        }
        private Dictionary<Mobile, PlayerRequests> m_WatchTable = new Dictionary<Mobile, PlayerRequests>();

        private bool MobileOk(Mobile m)
        {
            if (m == null || m.Deleted)
                return false;

            if (m is PlayerMobile)
            {
                if (m.Backpack == null)                 // sanity
                    return false;

                if (m.NetState == null)                 // logged in?
                    return false;

                if (!m.Alive)                           // alive 
                    return false;

                if (m.Criminal)                         // Thou'rt a criminal and cannot escape so easily.
                    return false;

                if (Spells.SpellHelper.CheckCombat(m))  // Wouldst thou flee during the heat of battle??
                    return false;

                // prison, jail, dungeon?
                if (!Spells.SpellHelper.CheckTravel(m, Spells.TravelCheckType.GateFrom, false))
                    return false;
            }

            return true;
        }

        private bool IsCTFRegion(Point3D location, Map map)
        {
            ArrayList regions = Server.Region.FindAll(location, map);
            if (regions != null)
            {
                for (int ix = 0; ix < regions.Count; ix++)
                {
                    Region rx = regions[ix] as Region;
                    if (rx.UId == this.CustomRegion.UId)
                        return true;
                }
            }

            return false;
        }

        private class CTFTarget : Target
        {
            private CTFControl m_ctfc;
            private int m_SessionId;
            public CTFTarget(CTFControl ctfc, int SessionId)
                : base(12, false, TargetFlags.None)
            {
                m_SessionId = SessionId;
                m_ctfc = ctfc;
            }

            private void Cancel()
            {
                if (m_ctfc != null && !m_ctfc.Deleted)
                    m_ctfc.OnCancel();
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!(targeted is PlayerMobile))
                {
                    from.SendMessage("You can only target players!");
                    Cancel();
                    return;
                }
                else if (!(targeted as Mobile).Alive)
                {
                    from.SendMessage("That's funny.");
                    Cancel();
                    return;
                }
                else if (targeted == from)
                {
                    from.SendMessage("I don't think I could take {0}!", from.Female ? "her" : "him");
                    Cancel();
                    return;
                }
                else if ((targeted as PlayerMobile).Party == from.Party && from.Party != null)
                {
                    from.SendMessage("You may not be in the same party as your opponent.");
                    Cancel();
                    return;
                }
                else
                {
                    // do not allow the overright of captain2 if the session id has changed
                    if (m_ctfc.SessionId == m_SessionId)
                        m_ctfc.Captain2 = targeted as Mobile;
                }
            }
        }

        private void TeleportPlayer(Mobile m, Map map, Point3D destination)
        {   // do not teleport players that are not online and have already been moved to the internal map
            if (m == null || m.Deleted || (m.NetState == null && m.Map == Map.Internal))
                return;

            DropHolding(m);                     // drop holding
            m.PlaySound(0x1FC);                 // source sound
            m.MoveToWorld(destination, map);    // do it
            m.PlaySound(0x1FC);                 // dest sound
        }

        private void TeleportPlayer(PlayerMobile m, Map map, Point3D destination)
        {
            TeleportPlayer(m as Mobile, map, destination);
        }

        private void TeleportPlayerCTF(Mobile m, Point3D destination)
        {
            destination = Spawner.GetSpawnPosition(CustomRegion.Map, destination, 6, SpawnFlags.None, m);
            TeleportPlayer(m, CustomRegion.Map, destination);
        }

        private void TeleportTeamCTF(Dictionary<PlayerMobile, PlayerContextData> table, Point3D destination)
        {
            foreach (PlayerMobile m in table.Keys)
            {   // don't send them if they are already there
                if (IsPlayerHome(m) == false)
                    TeleportPlayerCTF(m, destination);
            }
        }

        private void TeleportTeamHome(Dictionary<PlayerMobile, PlayerContextData> table)
        {
            // send them back to where they started from
            foreach (KeyValuePair<PlayerMobile, PlayerContextData> kvp in table)
            {
                TeleportPlayer(kvp.Key, kvp.Value.Map, kvp.Value.Location);
            }
        }

        private void ColorizeTeam(Dictionary<PlayerMobile, PlayerContextData> table, int hue)
        {
            foreach (PlayerMobile m in table.Keys)
            {
                ArrayList equip = new ArrayList(m.Items);

                // Colorize any items being worn
                foreach (Item i in equip)
                {
                    if (Sungate.RestrictedItem(m, i) == false)
                        continue;
                    else
                    {
                        if (i is BaseClothing)
                            (i as BaseClothing).PushHue(hue);
                        else if (i is BaseArmor)
                            (i as BaseArmor).PushHue(hue);
                    }
                }

                // Get a count of all items in the player's backpack.
                ArrayList items = new ArrayList(m.Backpack.Items);

                // Run through all items in player's pack, move them to the bag we just dropped in the bank
                foreach (Item i in items)
                {
                    if (i is BaseClothing)
                        (i as BaseClothing).PushHue(hue);
                    else if (i is BaseArmor)
                        (i as BaseArmor).PushHue(hue);
                }

            }
        }

        private void DeColorizePlayer(Mobile m)
        {
            if (m == null)
                return;

            ArrayList equip = new ArrayList(m.Items);

            // Colorize any items being worn
            foreach (Item i in equip)
            {
                if (Sungate.RestrictedItem(m, i) == false)
                    continue;
                else
                {
                    if (i is BaseClothing)
                        (i as BaseClothing).PopHue();
                    else if (i is BaseArmor)
                        (i as BaseArmor).PopHue();
                }
            }

            // Get a count of all items in the player's backpack.
            ArrayList items = new ArrayList(m.Backpack.Items);

            // Run through all items in player's pack, move them to the bag we just dropped in the bank
            foreach (Item i in items)
            {
                if (i is BaseClothing)
                    (i as BaseClothing).PopHue();
                else if (i is BaseArmor)
                    (i as BaseArmor).PopHue();
            }

        }

        private void DeColorizeTeam(Dictionary<PlayerMobile, PlayerContextData> table)
        {
            foreach (PlayerMobile m in table.Keys)
            {
                DeColorizePlayer(m);
            }
        }

        private void FreezeTeam(Dictionary<PlayerMobile, PlayerContextData> table, bool freeze)
        {
            if (table != null)
            {
                foreach (PlayerMobile m in table.Keys)
                {
                    if (m != null && m.NetState != null)
                        m.Frozen = freeze;
                }
            }
            else
            {
                if (m_RedTeam != null)
                    FreezeTeam(m_RedTeam, freeze);

                if (m_BlueTeam != null)
                    FreezeTeam(m_BlueTeam, freeze);
            }
        }

        private void ShowPlayerArrow(Mobile m)
        {
            // set the range
            if (this.CustomRegion != null && this.CustomRegion.Coords.Count != 0)
            {
                // kill old arrow
                if (m.QuestArrow != null && m.QuestArrow.Running)
                    m.QuestArrow.Stop();

                // give a new arrow if the player is not holding the flag
                if (HasFlag(m) == false)
                {
                    Rectangle3D rect = this.CustomRegion.Coords[0];
                    int range = Math.Max(Math.Abs(rect.Width), Math.Abs(rect.Height));
                    m.QuestArrow = new TrackArrow(m, Flag, range);
                }
            }
        }

        private void BroadcastArrow(Dictionary<PlayerMobile, PlayerContextData> table)
        {
            BroadcastArrow(table, true);
        }

        private void BroadcastArrow(Dictionary<PlayerMobile, PlayerContextData> table, bool show)
        {
            if (table != null)
            {
                foreach (PlayerMobile m in table.Keys)
                {
                    if (m != null && m.NetState != null)
                    {
                        if (show)
                        {
                            ShowPlayerArrow(m);
                        }
                        else if (m.QuestArrow != null && m.QuestArrow.Running)
                        {
                            m.QuestArrow.Stop();
                        }
                    }
                }
            }
            else
            {
                if (m_RedTeam != null)
                    BroadcastArrow(m_RedTeam, show);

                if (m_BlueTeam != null)
                    BroadcastArrow(m_BlueTeam, show);
            }
        }

        private void BroadcastMessage(Dictionary<PlayerMobile, PlayerContextData> table, int hue, string message)
        {
            if (table != null)
            {
                foreach (PlayerMobile m in table.Keys)
                {
                    if (m != null && m.NetState != null)
                        m.SendMessage(hue, message);
                }
            }
            else
            {
                if (m_RedTeam != null)
                    BroadcastMessage(m_RedTeam, hue, message);

                if (m_BlueTeam != null)
                    BroadcastMessage(m_BlueTeam, hue, message);
            }
        }

        private void BroadcastBeep(Dictionary<PlayerMobile, PlayerContextData> table, int SoundId)
        {
            if (table != null)
            {
                foreach (PlayerMobile m in table.Keys)
                {
                    if (m != null && m.NetState != null)
                        Beep(m, SoundId);
                }
            }
            else
            {
                if (m_RedTeam != null)
                    BroadcastBeep(m_RedTeam, SoundId);

                if (m_BlueTeam != null)
                    BroadcastBeep(m_BlueTeam, SoundId);
            }
        }

        public void Beep(Mobile m, int soundID)
        {
            if (m != null && m.NetState != null)
            {
                Packet p = Packet.Acquire(new Network.PlaySound(soundID, m));
                m.NetState.Send(p);
                Packet.Release(p);
            }
        }

        private bool LoadTeams(PlayerMobile Captain, Dictionary<PlayerMobile, PlayerContextData> table)
        {
            // should never happen
            if (!MobileOk(Captain) || Captain.Party == null)
                return false;

            foreach (Server.Engines.PartySystem.PartyMemberInfo pmi in (Captain.Party as Server.Engines.PartySystem.Party).Members)
            {
                if (pmi != null && pmi.Mobile != null && pmi.Mobile is PlayerMobile)
                {
                    // we added the pmi.Mobile.Criminal check because we don't want players using this as a nice way to tele out of trouble!
                    //	jail, prison, etc.
                    if (MobileOk(pmi.Mobile))
                        table[pmi.Mobile as PlayerMobile] = new PlayerContextData(pmi.Mobile, this);
                    else
                    {
                        pmi.Mobile.SendMessage("You will be unable to attent the CTF tournament.");
                        Captain.SendMessage(string.Format("{0} will be unable to attent the CTF tournament.", pmi.Mobile.Name));
                    }
                }
            }

            return true;
        }

        private bool CheckTeam(Dictionary<PlayerMobile, PlayerContextData> table)
        {
            int total = 0;
            if (table != null)
                foreach (KeyValuePair<PlayerMobile, PlayerContextData> kvp in table)
                {
                    total += (int)kvp.Key.Skills.EvalInt.Base;
                    if (total > EvalIntCap)
                        return false;
                }

            return true;
        }

        public enum Team { Red, Blue, None }

        private bool IsRedTeam(PlayerMobile m)
        {
            if (m == null)
                return false;
            return m_RedTeam.ContainsKey(m as PlayerMobile);
        }

        private bool IsBlueTeam(PlayerMobile m)
        {
            if (m == null)
                return false;
            return m_BlueTeam.ContainsKey(m as PlayerMobile);
        }

        private bool HasFlag(Mobile m)
        {
            if (Flag == null || m == null)
                return false;

            if (Flag.RootParent != null && Flag.RootParent == m)
                return true;

            return false;
        }

        private int OffenseScore { get { if (Offense == Team.Red) return RedTeamPoints; else return BlueTeamPoints; } set { if (Offense == Team.Red) RedTeamPoints = value; else BlueTeamPoints = value; } }
        private int DefenseScore { get { if (Defense == Team.Red) return RedTeamPoints; else return BlueTeamPoints; } set { if (Defense == Team.Red) RedTeamPoints = value; else BlueTeamPoints = value; } }

        private void LogStatus(string text)
        {
            LogHelper Logger = new LogHelper("ctf.log", false, true);
            Logger.Log(LogType.Text, text);
            Logger.Finish();
        }

        public class TrackArrow : QuestArrow
        {
            private Mobile m_From;
            private Timer m_Timer;

            public TrackArrow(Mobile from, Item target, int range)
                : base(from)
            {
                m_From = from;
                m_Timer = new TrackTimer(from, target, range, this);
                m_Timer.Start();
            }

            public override void OnClick(bool rightClick)
            {
                if (rightClick)
                {
                    m_From = null;

                    Stop();
                }
            }

            public override void OnStop()
            {
                m_Timer.Stop();
                m_Timer.Flush();

                if (m_From != null)
                {
                    // m_From.SendMessage("You no longer know where the flag is.");
                }
            }
        }

        public class TrackTimer : Timer
        {
            private Mobile m_From;
            private Item m_Flag;
            private int m_Range;
            private int m_LastX, m_LastY;
            private QuestArrow m_Arrow;

            public TrackTimer(Mobile from, Item target, int range, QuestArrow arrow)
                : base(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(2.5))
            {
                m_From = from;
                m_Flag = target;
                m_Range = range;
                m_Arrow = arrow;
            }

            protected override void OnTick()
            {
                if (m_Flag == null)

                    if (!m_Arrow.Running)
                    {
                        Stop();
                        return;
                    }
                    else if (m_From.NetState == null || m_From.Deleted || m_Flag == null || m_Flag.Deleted || m_From.Map != m_Flag.Map || !m_From.InRange(m_Flag, m_Range))
                    {
                        m_From.Send(new CancelArrow());
                        m_From.SendMessage("You no longer know where the flag is.");
                        Stop();
                        return;
                    }

                // button button who's got the button
                Point3D location = (m_Flag.Parent == null) ? m_Flag.Location :                      // flag on the ground location
                    (m_Flag.RootParent is Mobile) ? (m_Flag.RootParent as Mobile).Location :        // mobile holding the flag location
                    (m_Flag.RootParent is Item) ? (m_Flag.RootParent as Item).Location :            // item (chest?) holding the flag location
                    Point3D.Zero;                                                                   // punt

                if (m_LastX != location.X || m_LastY != location.Y)
                {
                    m_LastX = location.X;
                    m_LastY = location.Y;
                    m_Arrow.Update(m_LastX, m_LastY);
                }
            }
        }

        // turn magery either all on or all off
        protected bool AllowMageryImp
        {
            get
            {   // if any spells are allowed, the magery is said to be allowd
                for (int spellID = 0; spellID < Server.Spells.SpellRegistry.Types.Length; spellID++)
                    if (!CustomRegion.RestrictedSpells.Contains(spellID))
                        return true;
                return false;
            }
            set
            {   // turn magery either all on, or all off
                if (value)
                    CustomRegion.RestrictedSpells.Clear();
                else
                    for (int spellID = 0; spellID < Server.Spells.SpellRegistry.Types.Length; spellID++)
                        CustomRegion.RestrictedSpells.Add(spellID);
            }
        }

        private void SendFinalScore()
        {
            // send final score
            Dictionary<PlayerMobile, PlayerContextData>[] teams = new Dictionary<PlayerMobile, PlayerContextData>[2] { m_RedTeam, m_BlueTeam };
            for (int ix = 0; ix < 2; ix++)
            {
                if (teams[ix] != null)
                {
                    foreach (KeyValuePair<PlayerMobile, PlayerContextData> kvp in teams[ix])
                    {
                        string message = string.Format("Final Score is {0}-{1}. ({2})",
                            Math.Max(OffenseScore, DefenseScore), Math.Min(OffenseScore, DefenseScore),
                            (OffenseScore > DefenseScore && GetTeam(kvp.Key) == Offense) ? "You Win!" :
                            (DefenseScore > OffenseScore && GetTeam(kvp.Key) == Defense) ? "You Win!" :
                            (DefenseScore == OffenseScore) ? "You Tie." : "You Lose.");

                        kvp.Key.SendMessage(SystemMessageColor, message);
                    }
                }
            }
        }

        private void RefreshPlayers()
        {
            // regenerate player stats
            Dictionary<PlayerMobile, PlayerContextData>[] teams = new Dictionary<PlayerMobile, PlayerContextData>[2] { m_RedTeam, m_BlueTeam };
            for (int ix = 0; ix < 2; ix++)
            {
                if (teams[ix] != null)
                {
                    foreach (KeyValuePair<PlayerMobile, PlayerContextData> kvp in teams[ix])
                    {
                        if (kvp.Key.NetState != null && kvp.Key.Alive)
                        {
                            kvp.Key.Hits = kvp.Key.HitsMax;     // regen hits 
                            kvp.Key.Stam = kvp.Key.StamMax;     // regen stam 
                            kvp.Key.Mana = kvp.Key.ManaMax;     // regen mana
                        }
                    }
                }
            }
        }

        private Dictionary<PlayerMobile, PlayerContextData> GetWinningTeam()
        {
            if (OffenseScore > DefenseScore)
                return GetTeam(Offense);
            else if (DefenseScore > OffenseScore)
                return GetTeam(Defense);
            else
                return null;
        }

        private Dictionary<PlayerMobile, PlayerContextData> GetLosingTeam()
        {
            if (OffenseScore > DefenseScore)
                return GetTeam(Defense);
            else if (DefenseScore > OffenseScore)
                return GetTeam(Offense);
            else
                return null;
        }

        private BaseContainer GetWinningTeamChest()
        {
            if (OffenseScore > DefenseScore)
                return GetTeamChest(Offense) as BaseContainer;
            else if (DefenseScore > OffenseScore)
                return GetTeamChest(Defense) as BaseContainer;
            else
                return null;
        }

        private BaseContainer GetLosingTeamChest()
        {
            if (OffenseScore > DefenseScore)
                return GetTeamChest(Defense) as BaseContainer;
            else if (DefenseScore > OffenseScore)
                return GetTeamChest(Offense) as BaseContainer;
            else
                return null;
        }

    } // end
}