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

/* Items/Special/Holiday/HolidayDeed.cs
 * ChangeLog:
 * 11/28/21, Yoar
 *      Initial version.
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Mobiles;
using Server.Network;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Server.Items
{
    public enum HolidaySeason : byte
    {
        None,

        AprilFools,
        // 12/29/2023, Adam: disallow construction of these old deeds/trees.
        //  See ChristmasTreeAddonDeed
        Christmas__Obsolete,
        Easter,
        Halloween,
        Valentines,
        Thanksgiving,
        Anniversary
    }

    public interface IHolidayItem
    {
        int Year { get; set; }
    }

    public class HolidayDeed : Item, IHolidayItem
    {
        private HolidaySeason m_Season;
        private int m_Year;

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidaySeason Season
        {
            get { return m_Season; }
            set { m_Season = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Year
        {
            get { return m_Year; }
            set { m_Year = value; InvalidateProperties(); }
        }

        // 12/29/2023, Adam: disallow construction of these old deeds/trees.
        //  See ChristmasTreeAddonDeed
        //[Constructable]
        public HolidayDeed()
            : this(HolidaySeason.Christmas__Obsolete, DateTime.UtcNow.Year)
        {
            Name = "Holiday Deed";
            Hue = 0x47; // old-style holiday deeds are hued
        }

        [Constructable]
        public HolidayDeed(HolidaySeason season)
            : this(season, DateTime.UtcNow.Year)
        {
        }

        [Constructable]
        public HolidayDeed(HolidaySeason season, int year)
            : base(0x14F0)
        {
            Weight = 1.0;
            m_Season = season;
            m_Year = year;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Name == "Holiday Deed")
                list.Add(String.Format(HolidayDeedSystem.GetSeasonLabel(m_Season), m_Year));
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Name == "Holiday Deed")
                LabelTo(from, String.Format(HolidayDeedSystem.GetSeasonLabel(m_Season), m_Year));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (HolidayDeedSystem.HintMessage != null)
            {
                if (!from.InRange(GetWorldLocation(), 2))
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                else
                    from.SendMessage(HolidayDeedSystem.HintMessage);
            }
        }

        public override bool DropToItem(Mobile from, Item target, Point3D p)
        {
            if (target.GetType().Name == HolidayDeedSystem.GiveToType && GiftTo(from))
                return true;

            return base.DropToItem(from, target, p);
        }

        public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
        {
            if (target.GetType().Name == HolidayDeedSystem.GiveToType && GiftTo(from))
                return true;

            return base.DropToMobile(from, target, p);
        }

        public bool GiftTo(Mobile m)
        {
            BankBox bank = m.BankBox;

            if (bank != null)
            {
                Item gift = HolidayDeedSystem.MakeGift(m, m_Season, m_Year);

                if (gift != null)
                {
                    bank.DropItem(gift);

                    this.Delete();

                    m.SendMessage("A gift box has been placed in your bank box!");
                    return true;
                }
            }

            m.SendMessage("Something went wrong!");
            return false;
        }

        public HolidayDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((byte)m_Season);
            writer.WriteEncodedInt(m_Year);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Season = (HolidaySeason)reader.ReadByte();
                        m_Year = reader.ReadEncodedInt();

                        break;
                    }
            }
        }
    }

    public static class HolidayDeedSystem
    {
        public static string HintMessage;
        public static string GiveToType; // TODO: Replace GiveToType with DeliverCondition
        public static HolidayGiftEntry[] Gifts = new HolidayGiftEntry[12];

        public static void Configure()
        {
            EventSink.WorldSave += EventSink_OnWorldSave;
            EventSink.WorldLoad += EventSink_OnWorldLoad;
        }

        private static void EventSink_OnWorldSave(WorldSaveEventArgs e)
        {
            SaveSettings();
        }

        private static void EventSink_OnWorldLoad()
        {
            LoadSettings();
        }

        #region Save/Load

        public const string SavesFolder = "Saves";
        public const string FileName = "HolidayDeedSettings.xml";

        private static void SaveSettings()
        {
            SaveFile(Path.Combine(SavesFolder, FileName));
        }

        private static void LoadSettings()
        {
            if (!LoadFile(Path.Combine(SavesFolder, FileName)))
                LoadFile(Path.Combine(Core.DataDirectory, FileName));
        }

        private static void SaveFile(string fileName)
        {
            XmlTextWriter writer = null;

            try
            {
                string folder = Path.GetDirectoryName(fileName);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                writer = new XmlTextWriter(fileName, Encoding.Default);
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument(true);

                try
                {
                    writer.WriteStartElement("HolidayDeedSettings");
                    writer.WriteAttributeString("version", "0");

                    writer.WriteElementString("HintMessage", HintMessage);
                    writer.WriteElementString("GiveToType", GiveToType);

                    int count = 0;

                    for (int i = 0; i < Gifts.Length; i++)
                    {
                        if (Gifts[i] != null)
                            count++;
                    }

                    try
                    {
                        writer.WriteStartElement("Gifts");
                        writer.WriteAttributeString("Count", count.ToString());

                        for (int i = 0; i < Gifts.Length; i++)
                        {
                            if (Gifts[i] != null)
                                Gifts[i].Save(writer);
                        }
                    }
                    finally
                    {
                        writer.WriteEndElement();
                    }
                }
                finally
                {
                    writer.WriteEndElement();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("HolidayDeedSystem Error: {0}", e);
            }
            finally
            {
                if (writer != null)
                {
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
        }

        private static bool LoadFile(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            XmlTextReader reader = null;

            try
            {
                reader = new XmlTextReader(fileName);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Utility.ToInt32(reader.GetAttribute("version"));

                reader.ReadStartElement("HolidayDeedSettings");

                switch (version)
                {
                    case 0:
                        {
                            HintMessage = reader.ReadElementString("HintMessage");
                            GiveToType = reader.ReadElementString("GiveToType");

                            int count = Utility.ToInt32(reader.GetAttribute("Count"));

                            Gifts = new HolidayGiftEntry[Math.Max(12, count)]; // ensure at least 12 elements

                            reader.ReadStartElement("Gifts");

                            for (int i = 0; i < count; i++)
                            {
                                HolidayGiftEntry gift = new HolidayGiftEntry();

                                gift.Load(reader);

                                Gifts[i] = gift;
                            }

                            // fill up the gift array
                            for (int i = count; i < Gifts.Length; i++)
                                Gifts[i] = new HolidayGiftEntry();

                            reader.ReadEndElement();

                            break;
                        }
                }

                reader.ReadEndElement();

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("HolidayDeedSystem Error: {0}", e);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            return false;
        }

        #endregion

        public static Item MakeGift(Mobile m, HolidaySeason season, int year)
        {
            Container giftBox = new GiftBox();

            giftBox.Name = String.Format(GetSeasonLabel(season), year);

            try
            {
                for (int i = 0; i < Gifts.Length; i++)
                {
                    HolidayGiftEntry entry = Gifts[i];

                    if (String.IsNullOrEmpty(entry.Type))
                        continue;

                    string giftTypeName = GetValue(entry.Type);

                    if (String.IsNullOrEmpty(giftTypeName))
                        throw new InvalidOperationException("InvalidGiftType");

                    Type giftType = SpawnerType.GetType(giftTypeName);

                    if (giftType == null || !typeof(Item).IsAssignableFrom(giftType))
                        throw new InvalidOperationException("InvalidGiftType");

                    Item item = (Item)Construct(giftType);

                    if (item == null)
                        throw new InvalidOperationException("NullGift");

                    if (!String.IsNullOrEmpty(entry.Name))
                    {
                        string name = GetValue(entry.Name);

                        if (String.IsNullOrEmpty(name))
                            throw new InvalidOperationException("InvalidGiftName");

                        item.Name = name;
                    }

                    if (!String.IsNullOrEmpty(entry.Hue))
                    {
                        string hueStr = GetValue(entry.Hue);

                        int hue;

                        if (!Int32.TryParse(hueStr, out hue))
                            throw new InvalidOperationException("InvalidGiftHue");

                        item.Hue = hue;
                    }

                    if (item is IHolidayItem)
                        ((IHolidayItem)item).Year = year;

                    giftBox.DropItem(item);
                }

                LogHelper logger = new LogHelper("HolidayDeed.log", false, true);

                string[] itemList = new string[giftBox.Items.Count];

                for (int i = 0; i < itemList.Length; i++)
                    itemList[i] = giftBox.Items[i].ToString();

                logger.Log(LogType.Mobile, m, String.Format("Gift creation: {0} containing {1}", giftBox.ToString(), String.Join(", ", itemList)));
                logger.Finish();

                return giftBox;
            }
            catch (Exception e)
            {
                giftBox.Delete();

                EventSink.InvokeLogException(new LogExceptionEventArgs(e));
#if DEBUG
                Console.WriteLine(e);
#endif
            }

            return null;
        }

        private static string GetValue(string str)
        {
            if (String.IsNullOrEmpty(str))
                return null;

            string[] split = str.Split('|');

            if (split.Length == 0)
                return null;

            return split[Utility.Random(split.Length)].Trim();
        }

        private static Item Construct(Type type)
        {
            if (type == null)
                return null;

            ConstructorInfo ctor = type.GetConstructor(new Type[0]);

            if (ctor == null || !Add.IsConstructable(ctor))
                return null;

            try
            {
                return (Item)ctor.Invoke(new object[0]);
            }
            catch
            {
                return null;
            }
        }

        private static readonly string[] m_SeasonLabels = new string[]
            {
                String.Empty,

                "April Fool's Day {0}",
                "Christmas {0}", // OSI typically labels "Winter"
                "Easter {0}",
                "Halloween {0}",
                "Valentine's Day {0}",
                "Thanksgiving {0}",
                "Anniversary {0}",
            };

        public static string GetSeasonLabel(HolidaySeason season)
        {
            int index = (int)season;

            if (index >= 0 && index < m_SeasonLabels.Length)
                return m_SeasonLabels[index];

            return String.Concat(season.ToString(), " {0}");
        }
    }

    [NoSort]
    [PropertyObject]
    public class HolidayGiftEntry
    {
        private string m_Type;
        private string m_Name; // TODO: Replace Name, Hue with SetProps
        private string m_Hue;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Hue
        {
            get { return m_Hue; }
            set { m_Hue = value; }
        }

        #region Save/Load

        public void Save(XmlTextWriter writer)
        {
            try
            {
                writer.WriteStartElement("Gift");
                writer.WriteAttributeString("version", "0");

                writer.WriteElementString("Type", m_Type);
                writer.WriteElementString("Name", m_Name);
                writer.WriteElementString("Hue", m_Hue);
            }
            finally
            {
                writer.WriteEndElement();
            }
        }

        public void Load(XmlTextReader reader)
        {
            int version = Utility.ToInt32(reader.GetAttribute("version"));

            reader.ReadStartElement("Gift");

            switch (version)
            {
                case 0:
                    {
                        m_Type = reader.ReadElementString("Type");
                        m_Name = reader.ReadElementString("Name");
                        m_Hue = reader.ReadElementString("Hue");

                        break;
                    }
            }

            reader.ReadEndElement();
        }

        #endregion

        public override string ToString()
        {
            return m_Type;
        }
    }

    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class HolidayDeedConsole : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public string HintMessage
        {
            get { return HolidayDeedSystem.HintMessage; }
            set { HolidayDeedSystem.HintMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GiveToType
        {
            get { return HolidayDeedSystem.GiveToType; }
            set { HolidayDeedSystem.GiveToType = value; }
        }

        #region Gift Accessors

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift1
        {
            get { return GetGift(0); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift2
        {
            get { return GetGift(1); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift3
        {
            get { return GetGift(2); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift4
        {
            get { return GetGift(3); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift5
        {
            get { return GetGift(4); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift6
        {
            get { return GetGift(5); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift7
        {
            get { return GetGift(6); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift8
        {
            get { return GetGift(7); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift9
        {
            get { return GetGift(8); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift10
        {
            get { return GetGift(9); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift11
        {
            get { return GetGift(10); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HolidayGiftEntry Gift12
        {
            get { return GetGift(11); }
            set { }
        }

        private HolidayGiftEntry GetGift(int index)
        {
            if (index >= 0 && index < HolidayDeedSystem.Gifts.Length)
                return HolidayDeedSystem.Gifts[index];

            return null;
        }

        #endregion

        [Constructable]
        public HolidayDeedConsole()
            : base(0x1F14)
        {
            Hue = 62;
            Name = "Holiday Deed Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public HolidayDeedConsole(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadEncodedInt();
        }
    }
}