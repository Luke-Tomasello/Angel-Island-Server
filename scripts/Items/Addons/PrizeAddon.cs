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

/* Scripts/Items/Addons/PrizeAddon.cs
 * CHANGELOG:
 *  12/26/2023, Adam
 *      Push music system down to BaseAddon
 *  12/16/23, Yoar
 *      Added seasonal placement rule.
 *    1/27/22, Yoar
 *        Initial Version. XmlAddon that carries a single-click label.
 */

using System;
using System.IO;

namespace Server.Items
{
    public class PrizeAddon : XmlAddon
    {
        public override BaseAddonDeed Deed { get { return new PrizeAddonDeed(AddonID, IsRedeedable, RetainsDeedHue, m_Label, base.RazorFile, m_SeasonBegin, m_SeasonEnd, m_SeasonMessage); } }

        private string m_Label;

        private DateTime m_SeasonBegin;
        private DateTime m_SeasonEnd;
        private string m_SeasonMessage;

        [CommandProperty(AccessLevel.GameMaster)]
        new public string Label
        {
            get { return m_Label; }
            set { m_Label = value; InvalidateComponentProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Rank
        {
            get { return GetRankFromHue(Hue); }
            set { Hue = GetHueFromRank(value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get { return base.Hue; }
            set { base.Hue = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SeasonBegin
        {
            get { return m_SeasonBegin; }
            set { m_SeasonBegin = RemoveYear(value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SeasonEnd
        {
            get { return m_SeasonEnd; }
            set { m_SeasonEnd = RemoveYear(value); }
        }

        public static DateTime RemoveYear(DateTime dt)
        {
            return dt.AddYears(-dt.Year + 1);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string SeasonMessage
        {
            get { return m_SeasonMessage; }
            set { m_SeasonMessage = value; }
        }

        [Constructable]
        public PrizeAddon(string addonID)
            : this(addonID, true, true, null, null, DateTime.MinValue, DateTime.MinValue, null)
        {
        }

        public PrizeAddon(string addonID, bool redeedable, bool retainsDeedHue, string label, string songFile, DateTime seasonBegin, DateTime seasonEnd, string seasonMessage)
            : base(addonID, redeedable, retainsDeedHue)
        {
            m_Label = label;
            base.RazorFile = songFile;
            m_SeasonBegin = seasonBegin;
            m_SeasonEnd = seasonEnd;
            m_SeasonMessage = seasonMessage;
        }

        public override void GetComponentProperties(AddonComponent c, ObjectPropertyList list)
        {
            if (!String.IsNullOrEmpty(m_Label))
                list.Add(m_Label);

            int rank = Rank;

            if (rank != 0)
                list.Add("{0} Place", FormatRank(rank));
        }

        public override void OnComponentSingleClick(AddonComponent c, Mobile from)
        {
            if (!String.IsNullOrEmpty(m_Label))
                c.LabelTo(from, m_Label);

            int rank = Rank;

            if (rank != 0)
                c.LabelTo(from, "{0} Place", FormatRank(rank));
        }

        public PrizeAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3
            // remove 'song file' and Instrument. Now handled in BaseAddon

            // version 2
            writer.Write((DateTime)m_SeasonBegin);
            writer.Write((DateTime)m_SeasonEnd);
            writer.Write((string)m_SeasonMessage);

            // version 1
            //writer.Write((string)m_SongFile);
            //writer.Write((Item)m_MusicPlayer.Instrument);

            writer.Write((string)m_Label);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        goto case 2;
                    }
                case 2:
                    {
                        m_SeasonBegin = reader.ReadDateTime();
                        m_SeasonEnd = reader.ReadDateTime();
                        m_SeasonMessage = reader.ReadString();
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 3)
                        {   // no longer needed - handled in BaseAddon
                            base.RazorFile = reader.ReadString();
                            /*Item instrument=*/
                            reader.ReadItem();
                        }
                        goto case 0;
                    }
                case 0:
                    {
                        m_Label = reader.ReadString();
                        break;
                    }
            }
        }

        private static readonly int[] m_Hues = new int[]
            {
                0x8AB, // 1: valorite
                0x89F, // 2: verite
                0x979, // 3: agapite
                0x8A5, // 4: gold
                0x972, // 5: bronze
                0x96D, // 6: copper
                0x966, // 7: shadow iron
                0x973, // 8: dull copper
            };

        public static int GetRankFromHue(int hue)
        {
            return Array.IndexOf(m_Hues, hue) + 1;
        }

        public static int GetHueFromRank(int rank)
        {
            int index = rank - 1;

            if (index >= 0 && index < m_Hues.Length)
                return m_Hues[index];

            return 0;
        }

        public static string FormatRank(int rank)
        {
            string rankStr = rank.ToString("N0");

            if ((rank % 100) > 10 && (rank % 100) < 20)
                return rankStr + "th";

            switch (rank % 10)
            {
                case 1: return rankStr + "st";
                case 2: return rankStr + "nd";
                case 3: return rankStr + "rd";

                default: return rankStr + "th";
            }
        }
    }

    public class PrizeAddonDeed : XmlAddonDeed
    {
        public override BaseAddon Addon { get { return new PrizeAddon(AddonID, IsRedeedable, RetainsDeedHue, m_Label, m_SongFile, m_SeasonBegin, m_SeasonEnd, m_SeasonMessage); } }

        private string m_Label;

        private string m_SongFile;

        private DateTime m_SeasonBegin;
        private DateTime m_SeasonEnd;
        private string m_SeasonMessage;

        [CommandProperty(AccessLevel.GameMaster)]
        new public string Label
        {
            get { return m_Label; }
            set { m_Label = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Rank
        {
            get { return PrizeAddon.GetRankFromHue(Hue); }
            set { Hue = PrizeAddon.GetHueFromRank(value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get { return base.Hue; }
            set { base.Hue = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string SongFile
        {
            get { return m_SongFile; }
            set
            {
                if (value == null)
                    m_SongFile = null;
                else if (!value.EndsWith(".rzr") || !File.Exists(Path.Combine(Core.DataDirectory, "song files", value)))
                    m_SongFile = "Please enter: <name>.rzr";
                else
                    m_SongFile = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SeasonBegin
        {
            get { return m_SeasonBegin; }
            set { m_SeasonBegin = PrizeAddon.RemoveYear(value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SeasonEnd
        {
            get { return m_SeasonEnd; }
            set { m_SeasonEnd = PrizeAddon.RemoveYear(value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string SeasonMessage
        {
            get { return m_SeasonMessage; }
            set { m_SeasonMessage = value; }
        }

        [Constructable]
        public PrizeAddonDeed()
            : this(null, true, true, null, null, DateTime.MinValue, DateTime.MinValue, null)
        {
        }

        [Constructable]
        public PrizeAddonDeed(string addonID)
            : this(addonID, true, true, null, null, DateTime.MinValue, DateTime.MinValue, null)
        {
        }

        public PrizeAddonDeed(string addonID, bool redeedable, bool retainsDeedHue, string label, string songFile, DateTime seasonBegin, DateTime seasonEnd, string seasonMessage)
            : base(addonID, redeedable, retainsDeedHue)
        {
            LootType = LootType.Blessed;
            m_Label = label;
            m_SongFile = songFile;
            m_SeasonBegin = seasonBegin;
            m_SeasonEnd = seasonEnd;
            m_SeasonMessage = seasonMessage;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (!String.IsNullOrEmpty(m_Label))
                list.Add(m_Label);

            int rank = Rank;

            if (rank != 0)
                list.Add("{0} Place", PrizeAddon.FormatRank(rank));
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (!String.IsNullOrEmpty(m_Label))
                LabelTo(from, m_Label);

#if false
            int rank = Rank;

            if (rank != 0)
                LabelTo(from, "{0} Place", PrizeAddon.FormatRank(rank));
#endif
        }

        public override bool CanPlace(Mobile from)
        {
            if (m_SeasonBegin != DateTime.MinValue || m_SeasonEnd != DateTime.MinValue)
            {
                DateTime min = PrizeAddon.RemoveYear(m_SeasonBegin);
                DateTime max = PrizeAddon.RemoveYear(m_SeasonEnd);
                DateTime cur = PrizeAddon.RemoveYear(DateTime.UtcNow);

                if (max < min)
                    max = max.AddYears(1);

                if (cur < min || cur > max)
                {
                    if (!String.IsNullOrEmpty(m_SeasonMessage))
                        from.SendMessage(m_SeasonMessage);

                    return false;
                }
            }

            return true;
        }

        public PrizeAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3
            // remove local 'song file'. Now in BaseAddon

            // version 2
            writer.Write((DateTime)m_SeasonBegin);
            writer.Write((DateTime)m_SeasonEnd);
            writer.Write((string)m_SeasonMessage);

            // version 1
            //writer.Write((string)m_SongFile);

            writer.Write((string)m_Label);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {   // remove local 'song file'. Now in BaseAddon
                        goto case 2;
                    }
                case 2:
                    {
                        m_SeasonBegin = reader.ReadDateTime();
                        m_SeasonEnd = reader.ReadDateTime();
                        m_SeasonMessage = reader.ReadString();
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 3)
                            m_SongFile = reader.ReadString();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Label = reader.ReadString();
                        break;
                    }
            }
        }
    }
#if false
    public delegate bool MusicPlayerCallback(Mobile from, int tick);

    public class MusicPlayer
    {
        private RazorInstrument m_Instrument;
        private Mobile m_Mobile;
        private Timer m_Timer;
        private int m_Tick;
        private MusicPlayerCallback m_Callback;

        public RazorInstrument Instrument { get { return m_Instrument; } }
        public Mobile Mobile { get { return m_Mobile; } }
        public Timer Timer { get { return m_Timer; } }
        public int Tick { get { return m_Tick; } }
        public MusicPlayerCallback Callback { get { return m_Callback; } set { m_Callback = value; } }

        public bool Playing { get { return (m_Timer != null); } }

        public MusicPlayer()
        {
        }

        public bool Play(Item item, Mobile from, object song)
        {
            if (song is string songFile)
            {
                RazorInstrument ri = GetInstrument();

                if (ri == null)
                    return false;

                Stop();

                m_Mobile = from;
                m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0), new TimerCallback(OnTick));
                m_Tick = 0;

                ri.SongFile = songFile;
                ri.MinMusicSkill = 0;
                ri.PlayItem = item;
                ri.AllDone = Stop;
                ri.StartMusic(from, ri);
            }
            if (song is RolledUpSheetMusic rsm)
            {
                rsm.AllDone = new RazorInstrument.FinishCallback(Stop);                                     // establishes NextMusicTime
                rsm.CheckMobileStatus = new RazorInstrument.CheckMobileStatusCallback(CheckMobileStatus);   // make sure the mobile is still around
                rsm.Instrument = rsm;
                rsm.PlayItem = item;
                rsm.RequiresBackpack = false;
                rsm.OnDoubleClick(from);
            }
            else
                return false;

            return true;
        }
        public bool CheckMobileStatus(Mobile m, Item playItem)
        {   // called back from RazorInstrument
            return CheckMobile(m, playItem);
        }
        private bool CheckMobile(Mobile m, Item playItem)
        {   // is the mobile still available?
            bool result = (m != null && m is Mobiles.PlayerMobile pm && pm.NetState != null && pm.NetState.Mobile != null && pm.NetState.Running == true);
            if (result && playItem != null)
                // are we sufficiently close to our PlayItem?
                result |= playItem.Map == m.Map && playItem.GetDistanceToSqrt(m) < 15;
            return result;
        }
        public void Stop()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            m_Tick = 0;

            if (m_Instrument != null)
            {
                m_Instrument.PlayQueue.Clear();
                m_Instrument.SongBuffer.Clear();
            }

            if (m_Mobile != null)
            {
                if (Commands.Play.MusicConfig.ContainsKey(m_Mobile))
                    Commands.Play.MusicConfig[m_Mobile].PlayList.Clear();

                m_Mobile = null;
            }
        }

        private void OnTick()
        {
            int tick = ++m_Tick;

            if (m_Callback != null && !m_Callback(m_Mobile, tick))
                Stop();
        }

        private RazorInstrument GetInstrument()
        {
            if (m_Instrument != null && m_Instrument.Deleted)
                m_Instrument = null;

            if (m_Instrument == null)
            {
                m_Instrument = new RazorInstrument(RazorInstrument.InstrumentSoundType.LapHarp);
                m_Instrument.IsIntMapStorage = true;
            }

            return m_Instrument;
        }
    }
#endif
}