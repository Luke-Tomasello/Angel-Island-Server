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

/* Scripts/Mobiles/Townfolk/TownCrier.cs
 * ChangeLog
 * 10/28/2023, Adam (CanBeDamaged/IsInvulnerable)
 *  We are moving towards an Invulnerable TownCrier for Siege since players have found they can sneak them out of town to
 *  train their pets. Currently on Siege, BaseVendors are vulnerable with the exception of Animal Trainers and now Town Criers
 * 8/18/22, Adam(Utc)
 * Convert all read/write of times to handle UTC (the server format.)
 *  All DateTime objects are written as UTC.
 *  All DateTime objects are read as UTC and returned as LOCAL.
 *  Why: This insures that a Saves folder moved from production will run correctly on a local (developers) machine.
 *  Example: (old system) The AI server computer runs at UTC, but but time is saved in 'local' format. This works fine as long as the world files are saved and loaded
 *          on the server. But if you then move the world files to a local developer machine running, say Pacific Time, then when you read the world files all dateTime 
 *          objects will be off by 7-8 hours, depending on DST
 *  The above change fixes this problem by ensuring all DateTime objects are always written as UTC and converted to local on read.
 * 8/10/21, pix
 *      No UnemploymentCheck for paying town crier.
 *	7/7/08, Adam
 *		In OnLoad() where we Load global town crier entry list, filter out those that are system generated (like IDOC announcements).
 *		This filtering is needed because 'the system' manages these entries manually (it needs to so that it can delete them.)
 *		This fixes the bug where after a restart dual copies of the IDOC messages were getting added and even if the player refreshed, the 
 *			IDOC message would remain on the town crier.
 *  08/12/06, Plasma
 *      -Change player meesage colours to yellow
 *      -Added new speech for announcing player messages
 *  11/19/06, Plasma
 *      - Added world load and save to GlobalTownCrierEntryList
 *      - Added xml save method for TownCrierListEntry
 *      - Added xml ctor for TownCrierListEntry
 *	05/19/06, Adam
 *		- Make sure staff can always hire the TC (> AccessLevel.Player)
 *		- typo
 *	01/13/06, Pix
 *		Changed using reference for TCCS/PJUM separation changes.
 *  12/11/05, Kit
 *		Added Serial number of player when sending staff message, all hireing now logged to file,
 *		only players with characters older then 7 days can now hire town criers.
 *  11/28/05 Taran Kain
 *      Changed TownCrier to be a BaseVendor instead of Mobile
 *      Implemented various override properties to make him act the same as he did before
 *  06/12/05 Taran Kain
 *		Added CanHire property to TownCrier serialization, was defaulting to false
 *		and thus causing "Hire" menu entry to not show
 *  06/03/05 Taran Kain
 *		Added Context Menu entry "Hire"
 *		Added functionality for players to enter and pay for TC messages
 *	1/27/05, Pix
 *		Incorporated TCCS system.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using Server.Engines;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Mobiles
{
    public interface ITownCrierEntryList
    {
        ArrayList Entries { get; }
        TownCrierEntry GetRandomEntry();
        void AddEntry(TownCrierEntry tce);
        void RemoveEntry(TownCrierEntry entry);
    }

    public class GlobalTownCrierEntryList : ITownCrierEntryList
    {
        private static GlobalTownCrierEntryList m_Instance;

        public static void Initialize()
        {
            Server.CommandSystem.Register("TownCriers", AccessLevel.GameMaster, new CommandEventHandler(TownCriers_OnCommand));
        }

        // plasma : allow global list to load / save with world
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        // plasma : save town crier global list
        public static void OnSave(WorldSaveEventArgs e)
        {
            System.Console.WriteLine("Town Crier Global Saving...");
            if (Instance.Entries == null)
                return;

            try
            {
                if (!Directory.Exists("Saves/AngelIsland"))
                    Directory.CreateDirectory("Saves/AngelIsland");

                string filePath = Path.Combine("Saves/AngelIsland", "TCGL.xml");

                using (StreamWriter op = new StreamWriter(filePath))
                {
                    XmlTextWriter xml = new XmlTextWriter(op);

                    xml.Formatting = Formatting.Indented;
                    xml.IndentChar = '\t';
                    xml.Indentation = 1;

                    xml.WriteStartDocument(true);
                    xml.WriteStartElement("TCGL");
                    // just to be complete..
                    xml.WriteAttributeString("Count", m_Instance.Entries.Count.ToString());
                    // Now write each entry
                    foreach (TownCrierEntry tce in Instance.Entries)
                        tce.Save(xml);
                    //and end doc
                    xml.WriteEndElement();

                    xml.Close();
                }

            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                System.Console.WriteLine("Error in GlobalTownCrierEntryList.OnSave(): " + ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            }
        }

        // plasma : Load global town crier entry list
        public static void OnLoad()
        {
            System.Console.WriteLine("TCGL Loading...");

            string filePath = Path.Combine("Saves/AngelIsland", "TCGL.xml");

            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                if (Instance.m_Entries == null)
                    Instance.m_Entries = new ArrayList();

                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                XmlElement root = doc["TCGL"];
                foreach (XmlElement entry in root.GetElementsByTagName("TCEntry"))
                {
                    try
                    {
                        // load in entry!
                        TownCrierEntry tce = new TownCrierEntry(entry);

                        // system messages cannot be loaded in this way as the system needs to maintain a handle to the message so that
                        //	it can be removed ondemand. This Load TCE system is designed for player 'paid for' messages.
                        if (tce.Poster != Serial.MinusOne)
                            // and add to the global TC list
                            Instance.AddEntry(tce);
                    }
                    catch
                    {
                        Console.WriteLine("Warning: A TCGL entry load failed");
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Exception caught loading TCGL.xml");
                Console.WriteLine(e.StackTrace);

            }
        }


        [Usage("TownCriers [clear|add]")]
        [Description("Manages the global town crier list.")]
        public static void TownCriers_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length != 1 || (e.Arguments[0].ToLower() != "clear" && e.Arguments[0].ToLower() != "add"))
            {
                e.Mobile.SendMessage("Usage: [TownCriers [clear|add]");
            }
            else if (e.Arguments[0].ToLower() == "add")
            {
                e.Mobile.SendGump(new TownCrierGump(e.Mobile, Instance));
            }
            else
            {   // doesn't work .. come back to fix
                if (Instance == null || Instance.Entries == null || Instance.Entries.Count == 0)
                {
                    e.Mobile.SendMessage("Town Crier has no news.");
                    return;
                }
                int count = 0;
                List<TownCrierEntry> list = new List<TownCrierEntry>();
                foreach (TownCrierEntry tce in Instance.Entries)
                    list.Add(tce);

                foreach (TownCrierEntry tce in list)
                {
                    count++;
                    Instance.RemoveEntry(tce);
                }

                e.Mobile.SendMessage("{0} Town Crier news items deleted.", count);
                return;
            }
        }

        public static GlobalTownCrierEntryList Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new GlobalTownCrierEntryList();

                return m_Instance;
            }
        }

        public bool IsEmpty { get { return (m_Entries == null || m_Entries.Count == 0); } }

        public GlobalTownCrierEntryList()
        {

        }

        private ArrayList m_Entries;

        public ArrayList Entries
        {
            get { return m_Entries; }
        }

        public TownCrierEntry GetRandomEntry()
        {
            if (m_Entries == null || m_Entries.Count == 0)
                return null;

            for (int i = m_Entries.Count - 1; m_Entries != null && i >= 0; --i)
            {
                if (i >= m_Entries.Count)
                    continue;

                TownCrierEntry tce = (TownCrierEntry)m_Entries[i];

                if (tce.Expired)
                    RemoveEntry(tce);
            }

            if (m_Entries == null || m_Entries.Count == 0)
                return null;

            return (TownCrierEntry)m_Entries[Utility.Random(m_Entries.Count)];
        }

        public void AddEntry(TownCrierEntry tce)
        {
            if (m_Entries == null)
                m_Entries = new ArrayList();

            m_Entries.Add(tce);

            ArrayList instances = TownCrier.Instances;

            for (int i = 0; i < instances.Count; ++i)
                ((TownCrier)instances[i]).ForceBeginAutoShout();
        }

        public void RemoveEntry(TownCrierEntry tce)
        {
            if (m_Entries == null)
                return;

            m_Entries.Remove(tce);

            if (m_Entries.Count == 0)
                m_Entries = null;
        }
    }

    public class TownCrierEntry
    {
        private string[] m_Lines;
        private DateTime m_ExpireTime;
        private Serial m_Poster;

        public string[] Lines { get { return m_Lines; } }
        public DateTime ExpireTime { get { return m_ExpireTime; } }
        public bool Expired { get { return (DateTime.UtcNow >= m_ExpireTime); } }
        public Serial Poster { get { return m_Poster; } }

        public TownCrierEntry(string[] lines, TimeSpan duration, Serial poster)
        {
            m_Lines = lines;

            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;
            else if (duration > TimeSpan.FromDays(365.0))
                duration = TimeSpan.FromDays(365.0);

            m_ExpireTime = DateTime.UtcNow + duration;

            m_Poster = poster;
        }

        public int CalculateCost()
        {
            int wordcount = 0;
            foreach (string s in m_Lines)
                wordcount += s.Split(' ').Length;

            return wordcount * (int)Math.Ceiling(((TimeSpan)(m_ExpireTime - DateTime.UtcNow)).TotalMinutes + .5) * CoreAI.TownCrierWordMinuteCost;
        }

        // plasma:  writes entry into a xml stream
        public void Save(XmlTextWriter xml)
        {
            try
            {
                xml.WriteStartElement("TCEntry");
                // how many lines?
                xml.WriteStartElement("LineCount");
                xml.WriteString(m_Lines.Length.ToString());
                xml.WriteEndElement();
                // the strings 
                for (int i = 0; i < m_Lines.Length; i++)
                {
                    xml.WriteStartElement("Line" + i);
                    xml.WriteString(m_Lines[i]);
                    xml.WriteEndElement();
                }
                // expire datetime
                xml.WriteStartElement("ExpireTime");
                xml.WriteString(XmlConvert.ToString(m_ExpireTime, XmlDateTimeSerializationMode.Utc));
                xml.WriteEndElement();
                // poster's serial
                xml.WriteStartElement("PosterSerial");
                xml.WriteString(Poster.ToString());
                xml.WriteEndElement();

                xml.WriteEndElement();
            }
            catch
            {
                Console.WriteLine("Error saving a town crier global entry!");
            }
        }

        public TownCrierEntry(XmlNode xml)      //pla: Xml ctor
        {
            // this is called to restore a TC item from TCGL.xml doc
            try
            {
                // convert everything back!
                int lines = XmlUtility.GetInt32(XmlUtility.GetText(xml["LineCount"], "0"), 0);
                m_Lines = new string[lines];
                for (int i = 0; i < lines; i++)
                    m_Lines[i] = XmlUtility.GetText(xml["Line" + i], "?");

                m_ExpireTime = XmlUtility.GetDateTime(XmlUtility.GetText(xml["ExpireTime"], null), DateTime.UtcNow);

                m_Poster = (Serial)Int32.Parse(XmlUtility.GetText(xml["PosterSerial"], "0").Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);

            }
            catch
            {
                Console.WriteLine("Warning!  A Town Crier global entry failed to load");
            }
        }
    }

    public class TownCrierDurationPrompt : Prompt
    {
        private ITownCrierEntryList m_Owner;

        public TownCrierDurationPrompt(ITownCrierEntryList owner)
        {
            m_Owner = owner;
        }

        public override void OnResponse(Mobile from, string text)
        {
            TimeSpan ts;

            try
            {
                ts = TimeSpan.Parse(text);
            }
            catch
            {
                from.SendMessage("Value was not properly formatted. Use: <hours:minutes:seconds>");
                from.SendGump(new TownCrierGump(from, m_Owner));
                return;
            }

            if (ts < TimeSpan.Zero)
                ts = TimeSpan.Zero;

            from.SendMessage("Duration set to: {0}", ts);
            from.SendMessage("Enter the first line to shout:");

            from.Prompt = new TownCrierLinesPrompt(m_Owner, null, new ArrayList(), ts);
        }

        public override void OnCancel(Mobile from)
        {
            from.SendLocalizedMessage(502980); // Message entry cancelled.
            from.SendGump(new TownCrierGump(from, m_Owner));
        }
    }

    public class TownCrierLinesPrompt : Prompt
    {
        private ITownCrierEntryList m_Owner;
        private TownCrierEntry m_Entry;
        private ArrayList m_Lines;
        private TimeSpan m_Duration;

        public TownCrierLinesPrompt(ITownCrierEntryList owner, TownCrierEntry entry, ArrayList lines, TimeSpan duration)
        {
            m_Owner = owner;
            m_Entry = entry;
            m_Lines = lines;
            m_Duration = duration;
        }

        public override void OnResponse(Mobile from, string text)
        {
            m_Lines.Add(text);

            from.SendMessage("Enter the next line to shout, or press <ESC> if the message is finished.");
            from.Prompt = new TownCrierLinesPrompt(m_Owner, m_Entry, m_Lines, m_Duration);
        }

        public override void OnCancel(Mobile from)
        {
            if (m_Entry != null)
            {
                try
                {
                    if (from.AccessLevel < AccessLevel.GameMaster)
                    {
                        from.SendMessage("A partial refund was delivered to your bank account.");
                        from.BankBox.DropItem(new Gold(m_Entry.CalculateCost()));
                    }
                }
                catch
                {
                    from.SendMessage("An error occurred while trying to put a refund in your bank account. Contact a Game Master immediately.");
                }

                m_Owner.RemoveEntry(m_Entry);
            }

            if (m_Lines.Count > 0)
            {
                if (from.AccessLevel < AccessLevel.GameMaster)
                {
                    TownCrierEntry tce = new TownCrierEntry((string[])m_Lines.ToArray(typeof(string)), m_Duration, from.Serial);
                    TownCrier.AddUnpaidEntry(from, tce);

                    from.SendMessage("This message will cost {0}gp. Drag gold or a check onto a Town Crier to pay.", tce.CalculateCost());
                }
                else
                {
                    m_Owner.AddEntry(new TownCrierEntry((string[])m_Lines.ToArray(typeof(string)), m_Duration, from.Serial));
                    from.SendMessage("Message has been set.");
                }
            }
            else
            {
                if (m_Entry != null)
                    from.SendMessage("Message deleted.");
                else
                    from.SendLocalizedMessage(502980); // Message entry cancelled.
            }

            from.SendGump(new TownCrierGump(from, m_Owner));
        }
    }

    public class TownCrierGump : Gump
    {
        private Mobile m_From;
        private ITownCrierEntryList m_Owner;

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                m_From.SendMessage("Enter the duration for the new message. Format: <hours:minutes:seconds>");
                m_From.Prompt = new TownCrierDurationPrompt(m_Owner);
            }
            else if (info.ButtonID > 1)
            {
                ArrayList entries = new ArrayList();
                if (m_Owner.Entries != null)
                {
                    foreach (TownCrierEntry tce in m_Owner.Entries)
                    {
                        if (m_From.AccessLevel >= AccessLevel.GameMaster ||
                            m_From.Serial == tce.Poster)
                            entries.Add(tce);
                    }
                }

                int index = info.ButtonID - 2;

                if (entries != null && index < entries.Count)
                {
                    TownCrierEntry tce = (TownCrierEntry)entries[index];
                    TimeSpan ts = tce.ExpireTime - DateTime.UtcNow;

                    if (ts < TimeSpan.Zero)
                        ts = TimeSpan.Zero;

                    m_From.SendMessage("Editing entry #{0}.", index + 1);
                    m_From.SendMessage("Enter the first line to shout:");
                    m_From.Prompt = new TownCrierLinesPrompt(m_Owner, tce, new ArrayList(), ts);
                }
            }
        }

        public TownCrierGump(Mobile from, ITownCrierEntryList owner)
            : base(50, 50)
        {
            m_From = from;
            m_Owner = owner;

            from.CloseGump(typeof(TownCrierGump));

            AddPage(0);

            ArrayList entries = new ArrayList();
            if (owner.Entries != null)
            {
                foreach (TownCrierEntry tce in owner.Entries)
                {
                    if (m_From.AccessLevel >= AccessLevel.GameMaster ||
                        m_From.Serial == tce.Poster)
                        entries.Add(tce);
                }
            }

            owner.GetRandomEntry(); // force expiration checks

            int count = 0;

            if (entries != null)
                count = entries.Count;

            AddImageTiled(0, 0, 300, 38 + (count == 0 ? 20 : (count * 85)), 0xA40);
            AddAlphaRegion(1, 1, 298, 36 + (count == 0 ? 20 : (count * 85)));

            AddHtml(8, 8, 300 - 8 - 30, 20, "<basefont color=#FFFFFF><center>TOWN CRIER MESSAGES</center></basefont>", false, false);

            AddButton(300 - 8 - 30, 8, 0xFAB, 0xFAD, 1, GumpButtonType.Reply, 0);

            if (count == 0)
            {
                AddHtml(8, 30, 284, 20, "<basefont color=#FFFFFF>The crier has no news.</basefont>", false, false);
            }
            else
            {
                for (int i = 0; i < entries.Count; ++i)
                {
                    TownCrierEntry tce = (TownCrierEntry)entries[i];

                    TimeSpan toExpire = tce.ExpireTime - DateTime.UtcNow;

                    if (toExpire < TimeSpan.Zero)
                        toExpire = TimeSpan.Zero;

                    StringBuilder sb = new StringBuilder();

                    sb.Append("[Expires: ");

                    if (toExpire.TotalHours >= 1)
                    {
                        sb.Append((int)toExpire.TotalHours);
                        sb.Append(':');
                        sb.Append(toExpire.Minutes.ToString("D2"));
                    }
                    else
                    {
                        sb.Append(toExpire.Minutes);
                    }

                    sb.Append(':');
                    sb.Append(toExpire.Seconds.ToString("D2"));

                    sb.Append("] ");

                    for (int j = 0; j < tce.Lines.Length; ++j)
                    {
                        if (j > 0)
                            sb.Append("<br>");

                        sb.Append(tce.Lines[j]);
                    }

                    sb.Append("<br>Poster: ");
                    sb.Append(tce.Poster.ToString());

                    AddHtml(8, 35 + (i * 85), 254, 80, sb.ToString(), true, true);

                    AddButton(300 - 8 - 26, 35 + (i * 85), 0x15E1, 0x15E5, 2 + i, GumpButtonType.Reply, 0);
                }
            }
        }
    }

    public class TownCrier : BaseVendor, ITownCrierEntryList
    {
        private ArrayList m_Entries;
        private Timer m_NewsTimer;
        private Timer m_AutoShoutTimer;
        private bool m_CanHire;
        private static Hashtable m_UnpaidEntries = new Hashtable();

        protected ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }
        public override void InitSBInfo()
        {
        }
        public override bool IsActiveVendor
        {
            get { return false; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanHire
        {
            get { return m_CanHire; }
            set { m_CanHire = value; }
        }

        public ArrayList Entries
        {
            get { return m_Entries; }
        }

        public static void AddUnpaidEntry(Mobile poster, TownCrierEntry tce)
        {
            m_UnpaidEntries[poster] = tce;
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (m_CanHire)
                list.Add(new ContextMenus.TownCrierHireEntry());
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            int val = 0;
            if (dropped is BankCheck && !(dropped is UnemploymentCheck))
                val = ((BankCheck)dropped).Worth;
            if (dropped is Gold)
                val = ((Gold)dropped).Amount;

            if (m_UnpaidEntries.ContainsKey(from))
            {
                TownCrierEntry tce = (TownCrierEntry)m_UnpaidEntries[from];
                if (val >= tce.CalculateCost())
                {
                    LogHelper Logger = new LogHelper("TownCrier.log", false, true);
                    Logger.Log(LogType.Mobile, from);
                    foreach (string s in tce.Lines)
                        Logger.Log(s);
                    Logger.Finish();
                    SayTo(from, "Thankee! I will tell the others of thy message.");
                    foreach (NetState state in NetState.Instances)
                    {
                        Mobile m = state.Mobile;

                        if (m != null && m.AccessLevel >= AccessLevel.Counselor)
                        {
                            m.SendMessage("{0}, {1} has posted the following message to the Town Criers:", from.Name, from.Serial);
                            foreach (string s in tce.Lines)
                                m.SendMessage(s);
                            m.SendMessage("This message will run for {0}.", ((TimeSpan)(tce.ExpireTime - DateTime.UtcNow)).ToString());
                        }
                    }

                    GlobalTownCrierEntryList.Instance.AddEntry(tce);
                    m_UnpaidEntries.Remove(from);
                    dropped.Delete();
                    return true;
                }
                else
                {
                    SayTo(from, "Dost thou attempt to cheat me? {0} gold is my price.", tce.CalculateCost());
                    return false;
                }
            }

            return base.OnDragDrop(from, dropped);
        }

        public TownCrierEntry GetRandomEntry()
        {
            if (m_Entries == null || m_Entries.Count == 0)
                return GlobalTownCrierEntryList.Instance.GetRandomEntry();

            for (int i = m_Entries.Count - 1; m_Entries != null && i >= 0; --i)
            {
                if (i >= m_Entries.Count)
                    continue;

                TownCrierEntry tce = (TownCrierEntry)m_Entries[i];

                if (tce.Expired)
                    RemoveEntry(tce);
            }

            if (m_Entries == null || m_Entries.Count == 0)
                return GlobalTownCrierEntryList.Instance.GetRandomEntry();

            TownCrierEntry entry = GlobalTownCrierEntryList.Instance.GetRandomEntry();

            if (entry == null || Utility.RandomBool())
                entry = (TownCrierEntry)m_Entries[Utility.Random(m_Entries.Count)];

            return entry;
        }

        public void ForceBeginAutoShout()
        {
            if (m_AutoShoutTimer == null)
                m_AutoShoutTimer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), TimeSpan.FromMinutes(1.0), new TimerCallback(AutoShout_Callback));
        }

        public void AddEntry(TownCrierEntry tce)
        {
            if (m_Entries == null)
                m_Entries = new ArrayList();

            m_Entries.Add(tce);

            if (m_AutoShoutTimer == null)
                m_AutoShoutTimer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), TimeSpan.FromMinutes(1.0), new TimerCallback(AutoShout_Callback));
        }

        public void RemoveEntry(TownCrierEntry tce)
        {
            if (m_Entries == null)
                return;

            m_Entries.Remove(tce);

            if (m_Entries.Count == 0)
                m_Entries = null;

            if (m_Entries == null && GlobalTownCrierEntryList.Instance.IsEmpty)
            {
                if (m_AutoShoutTimer != null)
                    m_AutoShoutTimer.Stop();

                m_AutoShoutTimer = null;
            }
        }

        private void AutoShout_Callback()
        {
            TownCrierEntry tce = GetRandomEntry();

            if (Utility.RandomDouble() > 0.5 || tce == null) //only spout off about macroers 1/2 the time
            {
                ListEntry le = TCCS.GetRandomEntry();
                if (le != null)
                {
                    tce = new TownCrierEntry(le.Lines, TimeSpan.FromMinutes(1), Serial.MinusOne);
                }
            }

            if (tce == null)
            {
                if (m_AutoShoutTimer != null)
                    m_AutoShoutTimer.Stop();

                m_AutoShoutTimer = null;
            }
            else if (m_NewsTimer == null)
            {
                m_NewsTimer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(3.0), new TimerStateCallback(ShoutNews_Callback), new object[] { tce, 0 });

                // pla: 12/08/06
                // changed to show a different mesasge in a yellow hue for player messages
                Mobile m = World.FindMobile(tce.Poster);
                if (m != null && m.AccessLevel == AccessLevel.Player)
                    PublicOverheadMessage(MessageType.Regular, 0x36, true, Utility.RandomBool() == true ? "A good citizen proclaims!" : "On behalf of a good citizen!");
                else
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 502976); // Hear ye! Hear ye!
            }
        }

        private void ShoutNews_Callback(object state)
        {
            object[] states = (object[])state;
            TownCrierEntry tce = (TownCrierEntry)states[0];
            int index = (int)states[1];

            if (index < 0 || index >= tce.Lines.Length)
            {
                if (m_NewsTimer != null)
                    m_NewsTimer.Stop();

                m_NewsTimer = null;
            }
            else
            {
                // pla: 12/08/06
                // changed to show in a yellow hue for player messages
                Mobile m = World.FindMobile(tce.Poster);
                if (m != null && m.AccessLevel == AccessLevel.Player)
                    PublicOverheadMessage(MessageType.Regular, 0x36, false, tce.Lines[index]);
                else
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, false, tce.Lines[index]);

                states[1] = index + 1;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                from.SendGump(new TownCrierGump(from, this));
            else
                base.OnDoubleClick(from);
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (m_NewsTimer == null && from.Alive && InRange(from, 12));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (m_NewsTimer == null && e.HasKeyword(0x30) && e.Mobile.Alive && InRange(e.Mobile, 12)) // *news*
            {
                Direction = GetDirectionTo(e.Mobile);

                TownCrierEntry tce = GetRandomEntry();

                if (tce == null)
                {
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 1005643); // I have no news at this time.
                }
                else
                {
                    m_NewsTimer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(3.0), new TimerStateCallback(ShoutNews_Callback), new object[] { tce, 0 });


                    // pla: 12/08/06
                    // added to show a yellow anouncement for player messages
                    Mobile m = World.FindMobile(tce.Poster);
                    if (m != null && m.AccessLevel == AccessLevel.Player)
                    {
                        PublicOverheadMessage(MessageType.Regular, 0x36, true, Utility.RandomBool() == true ? "A good citizen proclaims!" : "On behalf of a good citizen!");
                    }
                    else
                    {
                        PublicOverheadMessage(MessageType.Regular, 0x3B2, 502978); // Some of the latest news!
                    }
                }
            }
        }

        private static ArrayList m_Instances = new ArrayList();

        public static ArrayList Instances
        {
            get { return m_Instances; }
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = Utility.RandomSkinHue();

            NameHue = CalcInvulNameHue();

            if (this.Female = Utility.RandomBool())
            {
                this.Body = 0x191;
                this.Name = NameList.RandomName("female");
            }
            else
            {
                this.Body = 0x190;
                this.Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            AddItem(new FancyShirt(Utility.RandomBlueHue()));

            Item skirt;

            switch (Utility.Random(2))
            {
                case 0:
                    skirt = new Skirt();
                    break;
                default:
                case 1:
                    skirt = new Kilt();
                    break;
            }

            skirt.Hue = Utility.RandomGreenHue();

            AddItem(skirt);

            AddItem(new FeatheredHat(Utility.RandomGreenHue()));

            Item boots;

            switch (Utility.Random(2))
            {
                case 0:
                    boots = new Boots();
                    break;
                default:
                case 1:
                    boots = new ThighBoots();
                    break;
            }

            AddItem(boots);

            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));

            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;

            AddItem(hair);
        }

        [Constructable]
        public TownCrier()
            : base("the town crier")
        {
            m_Instances.Add(this);
            m_CanHire = true;

            ActiveSpeed = 5.0;
            PassiveSpeed = 5.0;
            CurrentSpeed = 5.0;
        }

        // 11/16/22, Adam
        //  Normally we would have: public override bool IsInvulnerable { get { return Core.RuleSets.BaseVendorInvulnerability(); } }
        // BaseVendorInvulnerability() is only true for publishes < 4
        //  but RunUO 2.6 has the towncrier coded this way, and UOSE also has an invul towncrier, which is not surprising:
        //      UOSE "This shard is mechanically-anchored in t2A: the specific era-accuracy date is OSI's Nov 23/'99 "Publish 1"."
        //      https://forums.uosecondage.com/viewtopic.php?t=65598
        // For now we will leave it as it is.
        // --
        // 10/28/2023, Adam (CanBeDamaged/IsInvulnerable)
        //  We are moving towards an Invulnerable TownCrier for Siege since players have found they can sneak them out of town to
        //  train their pets. Currently on Siege, BaseVendors are vulnerable with the exception of Town Criers and Animal Trainers
        //public override bool CanBeDamaged() { return false; }
        public override bool IsInvulnerable { get { return Core.RuleSets.SiegeStyleRules() ? true : Core.RuleSets.BaseVendorInvulnerability(); } }

        public override void OnDelete()
        {
            m_Instances.Remove(this);
            base.OnDelete();
        }

        public TownCrier(Serial serial)
            : base(serial)
        {
            m_Instances.Add(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write((bool)m_CanHire);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {
                        m_CanHire = reader.ReadBool();
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Error while deserializing Town Crier: Invalid save version.");
                        break;
                    }
            }

            NameHue = CalcInvulNameHue();
        }
    }
}

namespace Server.ContextMenus
{
    class TownCrierHireEntry : ContextMenuEntry
    {
        public TimeSpan HireAge { get { return TimeSpan.FromDays(7.0); } }

        public TownCrierHireEntry()
            : base(6120, 8)
        {
        }

        public override void OnClick()
        {
            if (this.Owner.From.Created + HireAge > DateTime.UtcNow && this.Owner.From.AccessLevel == AccessLevel.Player)
                this.Owner.From.SendMessage("I'm sorry, do I know you?");
            else
                this.Owner.From.SendGump(new Mobiles.TownCrierGump(this.Owner.From, Mobiles.GlobalTownCrierEntryList.Instance));
        }
    }
}