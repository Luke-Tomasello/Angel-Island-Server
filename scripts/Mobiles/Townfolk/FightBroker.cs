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

/* Scripts/Mobiles/Townfolk/FightBroker.cs
 * CHANGELOG:
 *  2010.06.10 - Pix
 *      Changed timeout to 10 seconds.
 *  2010.06.09 - Pix
 *      Added 'timeout' to fight broker registration to eliminate possibility of opportunistic joining away 
 *      from the fightbroker.
 *	05/27/10, adam
 *		Make all fight broker actions free
 *	08/01/07, Pix
 *		Added consequences for blue-healers with the Fight Brokers.
 *  06/27/06, Kit
 *		Changed IsInvunerable to constructor as is no longer a virtual function.
 *	02/11/06, Adam
 *		Make common the formatting of sextant coords.
 *  11/28/05 Taran Kain
 *      Changed DisallowAllMoves to return false, so the FB will turn when his AI tells him to.
 *	12/01/04 - Pix
 *		Made it so you can only use the "where" command if you are registered.
 *	10/22/04 - Pix
 *		Removed checks for CampfireRegion since camping has changed and that class no longer exists.
 *	10/19/04, Adam
 *		Base the FightBroker on BaseVendor to pickup Invulnerability.
 *		PS. I don't like having to make this guy a vendor, as we set IsInvulnerable() and CanBeDamaged()
 *			as appropriate. But it seems that being a Vendor is checked in BaseCreature.CanBeHarmful()
 *			and rather than touch that code too, it's easier to make the change local.
 *	10/18/04 - Pixie
 *		Removed guild title from display.
 *		Added "online" keyword to "list" so people can list only online folks.
 *		Changed so guild tag will only display when they've got it set to shown.
 *		Added [fightbroker command.
 *	10/15/04 - Pixie
 *		Fixed display of guildless and titleless people registered.
 *	10/15/04 - Pixie
 *		Added Guild Title and Guild Abbreviation to list.
 *	10/15/04 - Pixie
 *		Made the list be public instead of private with the person who said "list"
 *		Access levels above player will never be registered now.
 *		Added confirmation gump to register.
 *	10/15/04 - Pixie
 *		Added (offline) tag to list of people if the person is offline.
 *		Set the hue of the person to be the hue of their character (red/blue/grey) in the list.
 *	10/14/04 - Pixie
 *		Enhancements.  Fixed speech, fixed the locations in dungeons, added region name to location given.
 *	10/13/04: Pixie
 *		Initial Version.
 */

using Server.Diagnostics;
using Server.Engines;				// Capture the Flag
using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class FightBroker : BaseVendor
    {
        private const int REGISTRATION_FEE = 0;         // adam: make free, was 100
        private const int REGISTRATION_ADDITION = 0;    // adam: make free, was 10 - this is charged per registrant already registered
        private const int WHERE_FEE = 0;                // Adam: make free, was 100;
        private const double REGISTER_TIME = 4.0;       // hours
        private const double INTERFERER_TIME = 1.0;     // hours
        private static List<FightMember> m_participants;
        private static List<FightMember> m_healer_interferers;

        protected ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }
        public override bool IsActiveVendor { get { return false; } }
        public override bool CanBeDamaged() { return false; }
        public override bool ShowFameTitle { get { return false; } }
        public override bool DisallowAllMoves { get { return false; } }
        public override bool ClickTitle { get { return true; } }            // this allows the title to show in the name
        public override bool CanTeach { get { return false; } }
        public override void InitSBInfo() { }

        [Constructable]
        public FightBroker()
            : base("the fight broker")
        {
            IsInvulnerable = true;
        }

        public FightBroker(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            base.InitBody();
        }

        public override void InitOutfit()
        {
            AddItem(new LongPants(0x322), LootType.Newbied);
            AddItem(new Shoes(GetRandomHue()));
            AddItem(new FancyShirt(0x47E), LootType.Newbied);
            AddItem(new GoldRing());
            AddItem(new FloppyHat(GetRandomHue()));

            Runebook runebook = new Runebook();
            runebook.Hue = 0x47E;
            runebook.Name = "Fight Book";
            runebook.Movable = false;
            AddItem(runebook, LootType.Newbied);
        }

        public new static void Initialize()
        {
            m_participants = new List<FightMember>();
            m_healer_interferers = new List<FightMember>();

            Server.CommandSystem.Register("fightbroker", AccessLevel.GameMaster, new CommandEventHandler(FightBroker_OnCommand));
        }

        private static void FightBroker_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            try
            {

                if (e.Length == 0)
                {
                    m.SendMessage("valid command formats are:");
                    m.SendMessage("[fightbroker list");
                    m.SendMessage("[fightbroker where <name>");
                }
                else
                {
                    string cmd = e.GetString(0);
                    if (cmd.ToLower() == "list")
                    {
                        int hue = 0x58;
                        FightBroker.FlushOldParticipants();
                        if (FightBroker.Participants.Count > 0)
                        {
                            m.SendMessage("Here are the people registered:");
                            foreach (FightMember fm in FightBroker.Participants)
                            {
                                string output;
                                if (fm.Mobile.Guild == null || fm.Mobile.DisplayGuildTitle == false)
                                {
                                    output = string.Format("{0}{1}", fm.Mobile.Name, fm.Mobile.Map != Map.Internal ? "" : " (offline)");
                                }
                                else
                                {
                                    output = string.Format("{0} [{2}]{1}", fm.Mobile.Name, fm.Mobile.Map != Map.Internal ? "" : " (offline)", fm.Mobile.Guild.Abbreviation);
                                }
                                // 8/10/22, Adam: I don't think Core.RedsInTown matters here. 
                                if (fm.Mobile.Red)
                                {
                                    hue = 0x21;
                                }
                                else if (fm.Mobile.Criminal)
                                {
                                    hue = 0x3B1;
                                }
                                else
                                {
                                    hue = 0x58;
                                }
                                m.SendMessage(hue, output);
                            }
                        }
                        else
                        {
                            m.SendMessage("The fightbroker list is empty.");
                        }

                    }
                    else if (cmd.ToLower() == "where")
                    {
                        string name;
                        int index = e.ArgString.ToLower().IndexOf("where") + 6;
                        if (index > e.ArgString.Length)
                        {
                            m.SendMessage("Please specify a name: [fightbroker where <name>");
                        }
                        else
                        {
                            name = e.ArgString.Substring(index);
                            if (FightBroker.IsMatchedAndLoggedIn(name))
                            {
                                FightBroker.SendLocation(m, name);
                            }
                            else
                            {
                                m.SendMessage("{0} is not registered or is not logged in", name);
                            }
                        }
                    }
                    else
                    {
                        m.SendMessage("valid command formats are:");
                        m.SendMessage("[fightbroker list");
                        m.SendMessage("[fightbroker where <name>");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                System.Console.WriteLine("Caught exception in [fightbroker command: " + ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            }
        }

        private static List<FightMember> Participants
        {
            get { return m_participants; }
        }

        private static List<FightMember> Interferers
        {
            get { return m_healer_interferers; }
        }

        public static bool AddParticipant(Mobile m)
        {
            bool bAdd = true;
            foreach (FightMember fm in m_participants)
            {
                if (fm.Mobile == m)
                {
                    bAdd = false;
                }
            }
            if (bAdd)
            {
                if (m.AccessLevel > AccessLevel.Player)
                {
                    m.SendMessage("You weren't added because your accesslevel is above player level.");
                }
                else
                {
                    m_participants.Add(new FightMember(m));
                }
            }
            return bAdd;
        }

        public static bool AddHealerInterferer(Mobile m)
        {
            bool bAdd = true;
            if (IsAlreadyRegistered(m))
            {
                bAdd = false;
            }
            else
            {
                foreach (FightMember fm in m_healer_interferers)
                {
                    if (fm.Mobile == m)
                    {
                        fm.Time = DateTime.UtcNow;
                        bAdd = false;
                    }
                }
                if (bAdd)
                {
                    if (m.AccessLevel > AccessLevel.Player)
                    {
                    }
                    else
                    {
                        m.SendMessage("You are now vulnerable to anyone registered with the Fight Broker.  You can avoid this consequence in the future by not healing people whose names are purple.");
                        m_healer_interferers.Add(new FightMember(m));
                    }
                }
            }
            return bAdd;
        }

        public static bool IsAlreadyRegistered(Mobile m)
        {
            foreach (FightMember fm in m_participants)
            {
                if (fm.Mobile == m)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsHealerInterferer(Mobile m)
        {
            foreach (FightMember fm in m_healer_interferers)
            {
                if (fm.Mobile == m)
                {
                    return true;
                }
            }
            return false;
        }

        public static void SendLocation(Mobile to, string name)
        {
            foreach (FightMember fm in m_participants)
            {
                if (fm.Mobile.Name.ToLower() == name.ToLower())
                {
                    //to.SendMessage("{0} is at {1}", fm.Mobile.Name, fm.Mobile.Location );
                    //to.SendMessage("{0} is at {1} {2} {3} in {4}.", fm.Mobile.Name, fm.Mobile.X, fm.Mobile.Y, fm.Mobile.Z, fm.Mobile.Map );
                    int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                    bool xEast = false, ySouth = false;
                    string location;
                    Map map = fm.Mobile.Map;

                    bool valid = Sextant.Format(fm.Mobile.Location, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

                    if (valid)
                        location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                    else
                        location = "????";

                    if (!valid)
                        location = string.Format("{0} {1}", fm.Mobile.X, fm.Mobile.Y);

                    if (map != null)
                    {
                        Region reg = fm.Mobile.Region;

                        if (reg != map.DefaultRegion)
                        {
                            location += (" in " + reg);
                            //from.SendMessage( "Your region is {0}.", reg );
                        }
                    }

                    to.SendMessage("{0} is at {1}", fm.Mobile.Name, location);
                }
            }
        }

        public static bool IsMatchedAndLoggedIn(string name)
        {
            foreach (FightMember fm in m_participants)
            {
                if (fm.Mobile.Name.ToLower() == name.ToLower())
                {
                    if (fm.Mobile.Map != Map.Internal) //make sure they're not logged out
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void FlushOldParticipants()
        {
            try
            {
                List<FightMember> toRemove = new List<FightMember>();
                foreach (FightMember fm in m_participants)
                {
                    if (DateTime.UtcNow - fm.Time > TimeSpan.FromHours(REGISTER_TIME))
                    {
                        toRemove.Add(fm);
                    }
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    m_participants.Remove(toRemove[i]);
                }

                toRemove.Clear();

                foreach (FightMember fm in m_healer_interferers)
                {
                    if (DateTime.UtcNow - fm.Time > TimeSpan.FromHours(INTERFERER_TIME))
                    {
                        toRemove.Add(fm);
                    }
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    m_healer_interferers.Remove(toRemove[i]);
                }

                toRemove.Clear();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 12))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public void AddMobileConfirmed(Mobile m)
        {
            int totalCost = REGISTRATION_FEE + REGISTRATION_ADDITION * FightBroker.Participants.Count;
            if (GetGoldFrom(m, totalCost))
            {
                if (FightBroker.AddParticipant(m))
                {
                    SayTo(m, "You have registered for {0} gold - be wary, people will find you now!", totalCost);
                }
                else
                {
                    SayTo(m, "You are already registered!");
                }
            }
            else
            {
                SayTo(m, "You don't have enough money for this.");
            }
        }

        private CTFControl FindCTFControl()
        {   // need smart selector logic here
            if (CTFControl.CtfGames != null && CTFControl.CtfGames.Count > 0)
            {
                for (int ix = 0; ix < CTFControl.CtfGames.Count; ix++)
                {
                    if (CTFControl.CtfGames[ix] != null && !CTFControl.CtfGames[ix].Deleted)
                        if (CTFControl.CtfGames[ix].CustomRegion.Registered)
                            if (CTFControl.CtfGames[ix].CurrentState <= CTFControl.States.WaitBookReturn)
                                return CTFControl.CtfGames[ix];
                }
            }

            return null;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            CTFControl ctfc = FindCTFControl();
            // we will need to use the SessionId to locate an available session
            if (ctfc != null)
            {   // process it
                ctfc.OnDragDrop(this, from, dropped);
            }
            else
                this.Say("I cannot take that at this time.");

            return base.OnDragDrop(from, dropped);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            CTFControl ctfc = FindCTFControl();

            try
            {
                if (!e.Handled && e.Mobile.InRange(this.Location, 4))
                {
                    // if our CTF understands the text
                    if (CTFControl.UnderstandsSpeech(e) && ctfc != null)
                    {   // process it
                        ctfc.OnSpeech(this, e);
                    }
                    else if (e.Speech.ToLower().IndexOf("cost") != -1)
                    {
                        Direction = GetDirectionTo(e.Mobile);

                        SayTo(e.Mobile, "It costs {0} gold to find someone.", WHERE_FEE);
                        SayTo(e.Mobile, "It currently costs {0} gold to register.", REGISTRATION_FEE + REGISTRATION_ADDITION * FightBroker.Participants.Count);
                    }
                    else if (e.Speech.ToLower().IndexOf("register") != -1)
                    {
                        Direction = GetDirectionTo(e.Mobile);

                        FightBroker.FlushOldParticipants();
                        if (FightBroker.IsAlreadyRegistered(e.Mobile))
                        {
                            SayTo(e.Mobile, "You are already registered!");
                        }
                        else
                        {
                            e.Mobile.SendGump(new ConfirmRegisterGump(e.Mobile, this));
                        }
                    }
                    else if (e.Speech.ToLower().IndexOf("list") != -1)
                    {
                        Direction = GetDirectionTo(e.Mobile);

                        bool bDisplayOnlineOnly = false;
                        if (e.Speech.ToLower().IndexOf("online") != -1)
                        {
                            bDisplayOnlineOnly = true;
                        }

                        FightBroker.FlushOldParticipants();
                        if (FightBroker.Participants.Count > 0)
                        {
                            //SayTo(e.Mobile, "Here are the people registered:");
                            Say("Here are the people registered:");
                            int originalspeechhue = SpeechHue;
                            foreach (FightMember fm in FightBroker.Participants)
                            {
                                if (!(bDisplayOnlineOnly && fm.Mobile.Map == Map.Internal))
                                {
                                    string output;
                                    if (fm.Mobile.Guild == null || fm.Mobile.DisplayGuildTitle == false)
                                    {
                                        output = string.Format("{0}{1}", fm.Mobile.Name, fm.Mobile.Map != Map.Internal ? "" : " (offline)");
                                    }
                                    else
                                    {
                                        output = string.Format("{0} [{2}]{1}", fm.Mobile.Name, fm.Mobile.Map != Map.Internal ? "" : " (offline)", fm.Mobile.Guild.Abbreviation);
                                    }
                                    // 8/10/22, Adam: I don't think Core.RedsInTown matters here. 
                                    if (fm.Mobile.Red)
                                    {
                                        SpeechHue = 0x21;
                                    }
                                    else if (fm.Mobile.Criminal)
                                    {
                                        SpeechHue = 0x3B1;
                                    }
                                    else
                                    {
                                        SpeechHue = 0x58;
                                    }
                                    //SayTo(e.Mobile, output);
                                    Say(output);
                                    SpeechHue = originalspeechhue;
                                }
                            }
                        }
                        else
                        {
                            //SayTo(e.Mobile, "Nobody has been brave enough to join recently.");
                            Say("Nobody has been brave enough to join recently.");
                        }
                    }
                    else if (e.Speech.ToLower().IndexOf("where") != -1)
                    {
                        Direction = GetDirectionTo(e.Mobile);

                        FightBroker.FlushOldParticipants();
                        if (FightBroker.IsAlreadyRegistered(e.Mobile))
                        {
                            string name;
                            if (e.Speech.ToLower().IndexOf("where is ") == -1)
                            {
                                name = e.Speech.Substring(e.Speech.ToLower().IndexOf("where ") + 6);
                            }
                            else
                            {
                                name = e.Speech.Substring(e.Speech.ToLower().IndexOf("where is ") + 9);
                            }
                            SayTo(e.Mobile, "So, you wish to find {0}, that will cost you {1} gold.", name, WHERE_FEE);
                            if (FightBroker.IsMatchedAndLoggedIn(name))
                            {
                                if (GetGoldFrom(e.Mobile, WHERE_FEE))
                                {
                                    FightBroker.SendLocation(e.Mobile, name);
                                    e.Mobile.SendMessage("You have spent {0} gold to find {1}", WHERE_FEE, name);
                                }
                                else
                                {
                                    SayTo(e.Mobile, "You don't have enough money for this.");
                                }
                            }
                            else
                            {
                                SayTo(e.Mobile, "{0} isn't registered.", name);
                            }
                        }
                        else
                        {
                            SayTo(e.Mobile, "You must be registered to locate someone.");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Error in FightBroker.OnSpeech():");
                System.Console.WriteLine(exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }

            base.OnSpeech(e);
        }

        private bool GetGoldFrom(Mobile buyer, int amount)
        {
            bool bought = false;
            Container cont;

            // it's free!
            if (amount == 0)
                return true;

            cont = buyer.Backpack;
            if (!bought && cont != null)
            {
                if (cont != null && cont.ConsumeTotal(typeof(Gold), amount))
                {
                    bought = true;
                }
            }

            if (!bought)
            {
                cont = buyer.BankBox;
                if (cont != null && cont.ConsumeTotal(typeof(Gold), amount))
                {
                    bought = true;
                }
            }

            return bought;
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            base.AddCustomContextEntries(from, list);
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

        private class FightMember
        {
            private Mobile m_Mobile;
            private DateTime m_DateTime;
            public Mobile Mobile
            {
                get { return m_Mobile; }
            }
            public DateTime Time
            {
                get { return m_DateTime; }
                set { m_DateTime = value; }
            }

            public FightMember(Mobile m)
            {
                m_Mobile = m;
                m_DateTime = DateTime.UtcNow;
            }
        }

        private class ConfirmRegisterGump : Gump
        {
            private FightBroker m_Broker;
            private DateTime m_GumpTime = DateTime.MinValue;

            public ConfirmRegisterGump(Mobile from, FightBroker fb)
                : base(50, 50)
            {
                from.CloseGump(typeof(ConfirmRegisterGump));

                m_GumpTime = DateTime.UtcNow;

                AddPage(0);

                AddBackground(10, 10, 190, 140, 0x242C);

                AddHtml(30, 30, 150, 75, String.Format("<div align=CENTER>{0}</div>", "Do you wish to register with the fight broker?"), false, false);

                AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
                m_Broker = fb;
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                Mobile from = state.Mobile;

                if (info.ButtonID == 1)
                {
                    if (DateTime.UtcNow > m_GumpTime.AddSeconds(10.0))
                    {
                        from.SendMessage("You have taken too long to respond, please revisit the fight broker to register.");
                    }
                    else
                    {
                        m_Broker.AddMobileConfirmed(from);
                    }
                }
            }
        }

    }

}