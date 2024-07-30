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

using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class CustomHairstylist : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override bool ClickTitle { get { return false; } }

        public override bool IsActiveBuyer { get { return false; } }
        public override bool IsActiveSeller { get { return true; } }

        public override bool OnBuyItems(Mobile buyer, List<BuyItemResponse> list)
        {
            return false;
        }

        public static readonly object From = new object();
        public static readonly object Vendor = new object();
        public static readonly object Price = new object();

        private static HairstylistBuyInfo[] m_SellList = new HairstylistBuyInfo[]
            {
                new HairstylistBuyInfo( 1018357, 50000, Layer.Hair, typeof( ChangeHairstyleGump ), new object[]
                    { From, Vendor, Price, Layer.Hair, ChangeHairstyleEntry.HairEntries } ),
                new HairstylistBuyInfo( 1018358, 50000, Layer.FacialHair, typeof( ChangeHairstyleGump ), new object[]
                    { From, Vendor, Price, Layer.FacialHair, ChangeHairstyleEntry.BeardEntries } ),
                new HairstylistBuyInfo( 1018359, 50, Layer.Hair, typeof( ChangeHairHueGump ), new object[]
                    { From, Vendor, Price, new Layer[]{ Layer.Hair, Layer.FacialHair }, ChangeHairHueEntry.RegularEntries } ),
                new HairstylistBuyInfo( 1018360, 500000, Layer.Hair, typeof( ChangeHairHueGump ), new object[]
                    { From, Vendor, Price, new Layer[]{ Layer.Hair, Layer.FacialHair }, ChangeHairHueEntry.BrightEntries } ),
                new HairstylistBuyInfo( 1018361, 30000, Layer.Hair, typeof( ChangeHairHueGump ), new object[]
                    { From, Vendor, Price, new Layer[]{ Layer.Hair }, ChangeHairHueEntry.RegularEntries } ),
                new HairstylistBuyInfo( 1018362, 30000, Layer.FacialHair, typeof( ChangeHairHueGump ), new object[]
                    { From, Vendor, Price, new Layer[]{ Layer.FacialHair }, ChangeHairHueEntry.RegularEntries } ),
                new HairstylistBuyInfo( 1018363, 500000, Layer.Hair, typeof( ChangeHairHueGump ), new object[]
                    { From, Vendor, Price, new Layer[]{ Layer.Hair }, ChangeHairHueEntry.BrightEntries } ),
                new HairstylistBuyInfo( 1018364, 500000, Layer.FacialHair, typeof( ChangeHairHueGump ), new object[]
                    { From, Vendor, Price, new Layer[]{ Layer.FacialHair }, ChangeHairHueEntry.BrightEntries } )
            };

        public override void VendorBuy(Mobile from)
        {
            from.SendGump(new HairstylistBuyGump(from, this, m_SellList));
        }

        [Constructable]
        public CustomHairstylist()
            : base("the hairstylist")
        {
        }

        public override int GetHairHue()
        {
            return RandomBrightHue();
        }

        public override VendorShoeType ShoeType
        {
            get { return Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals; }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Server.Items.Robe(Utility.RandomPinkHue()));
        }

        public override void InitSBInfo()
        {
        }

        public CustomHairstylist(Serial serial)
            : base(serial)
        {
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

    public class HairstylistBuyInfo
    {
        private int m_Title;
        private string m_TitleString;
        private int m_Price;
        private Layer m_Layer;
        private Type m_GumpType;
        private object[] m_GumpArgs;

        public int Title { get { return m_Title; } }
        public string TitleString { get { return m_TitleString; } }
        public int Price { get { return m_Price; } }
        public Layer Layer { get { return m_Layer; } }
        public Type GumpType { get { return m_GumpType; } }
        public object[] GumpArgs { get { return m_GumpArgs; } }

        public HairstylistBuyInfo(int title, int price, Layer layer, Type gumpType, object[] args)
        {
            m_Title = title;
            m_Price = price;
            m_Layer = layer;
            m_GumpType = gumpType;
            m_GumpArgs = args;
        }

        public HairstylistBuyInfo(string title, int price, Layer layer, Type gumpType, object[] args)
        {
            m_TitleString = title;
            m_Price = price;
            m_Layer = layer;
            m_GumpType = gumpType;
            m_GumpArgs = args;
        }
    }

    public class HairstylistBuyGump : Gump
    {
        private Mobile m_From;
        private Mobile m_Vendor;
        private HairstylistBuyInfo[] m_SellList;

        public HairstylistBuyGump(Mobile from, Mobile vendor, HairstylistBuyInfo[] sellList)
            : base(50, 50)
        {
            m_From = from;
            m_Vendor = vendor;
            m_SellList = sellList;

            from.CloseGump(typeof(HairstylistBuyGump));
            from.CloseGump(typeof(ChangeHairHueGump));
            from.CloseGump(typeof(ChangeHairstyleGump));

            bool isFemale = (from.Female || from.Body.IsFemale);

            int balance = Banker.GetAccessibleBalance(from);
            int canAfford = 8;              //int canAfford = 0;  <--Old Salty changed this line

            for (int i = 0; i < sellList.Length; ++i)
            {
                if (balance >= sellList[i].Price && (sellList[i].Layer != Layer.FacialHair || !isFemale))
                    canAfford = 8;      //++canAfford;  <--Old Salty changed this line
            }

            AddPage(0);

            AddBackground(50, 10, 450, 100 + (canAfford * 25), 2600);

            AddHtmlLocalized(100, 40, 350, 20, 1018356, false, false); // Choose your hairstyle change:

            int index = 0;

            for (int i = 0; i < sellList.Length; ++i)
            {
                if (sellList[i].Layer != Layer.FacialHair || !isFemale) //if ( balance >= sellList[i].Price && (sellList[i].Layer != Layer.FacialHair || !isFemale) ) <-- Old Salty changed this line
                {
                    if (sellList[i].TitleString != null)
                        AddHtml(140, 75 + (index * 25), 300, 20, sellList[i].TitleString, false, false);
                    else
                        AddHtmlLocalized(140, 75 + (index * 25), 300, 20, sellList[i].Title, false, false);

                    AddButton(100, 75 + (index++ * 25), 4005, 4007, 1 + i, GumpButtonType.Reply, 0);
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            int index = info.ButtonID - 1;

            if (index >= 0 && index < m_SellList.Length)
            {
                HairstylistBuyInfo buyInfo = m_SellList[index];

                int balance = Banker.GetAccessibleBalance(m_From);

                bool isFemale = (m_From.Female || m_From.Body.IsFemale);

                if (buyInfo.Layer == Layer.FacialHair && isFemale)
                {
                    // You cannot place facial hair on a woman!
                    m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1010639, m_From.NetState);
                }
                else if (balance >= buyInfo.Price)
                {
                    try
                    {
                        object[] origArgs = buyInfo.GumpArgs;
                        object[] args = new object[origArgs.Length];

                        for (int i = 0; i < args.Length; ++i)
                        {
                            if (origArgs[i] == CustomHairstylist.Price)
                                args[i] = m_SellList[index].Price;
                            else if (origArgs[i] == CustomHairstylist.From)
                                args[i] = m_From;
                            else if (origArgs[i] == CustomHairstylist.Vendor)
                                args[i] = m_Vendor;
                            else
                                args[i] = origArgs[i];
                        }

                        Gump g = Activator.CreateInstance(buyInfo.GumpType, args) as Gump;

                        m_From.SendGump(g);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
                else
                {
                    // You cannot afford my services for that style.
                    m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042293, m_From.NetState);
                }
            }
        }
    }

    public class ChangeHairHueEntry
    {
        private string m_Name;
        private int[] m_Hues;

        public string Name { get { return m_Name; } }
        public int[] Hues { get { return m_Hues; } }

        public ChangeHairHueEntry(string name, int[] hues)
        {
            m_Name = name;
            m_Hues = hues;
        }

        public ChangeHairHueEntry(string name, int start, int count)
        {
            m_Name = name;

            m_Hues = new int[count];

            for (int i = 0; i < count; ++i)
                m_Hues[i] = start + i;
        }

        public static readonly ChangeHairHueEntry[] BrightEntries = new ChangeHairHueEntry[]
            {
                new ChangeHairHueEntry( "*****", 12, 10 ),
                new ChangeHairHueEntry( "*****", 32, 5 ),
                new ChangeHairHueEntry( "*****", 38, 8 ),
                new ChangeHairHueEntry( "*****", 54, 3 ),
                new ChangeHairHueEntry( "*****", 62, 10 ),
                new ChangeHairHueEntry( "*****", 81, 2 ),
                new ChangeHairHueEntry( "*****", 89, 2 ),
                new ChangeHairHueEntry( "*****", 1153, 2 )
            };

        public static readonly ChangeHairHueEntry[] RegularEntries = new ChangeHairHueEntry[]
            {
                new ChangeHairHueEntry( "*****", 1602, 26 ),
                new ChangeHairHueEntry( "*****", 1628, 27 ),
                new ChangeHairHueEntry( "*****", 1502, 32 ),
                new ChangeHairHueEntry( "*****", 1302, 32 ),
                new ChangeHairHueEntry( "*****", 1402, 32 ),
                new ChangeHairHueEntry( "*****", 1202, 24 ),
                new ChangeHairHueEntry( "*****", 2402, 29 ),
                new ChangeHairHueEntry( "*****", 2213, 6 ),
                new ChangeHairHueEntry( "*****", 1102, 8 ),
                new ChangeHairHueEntry( "*****", 1110, 8 ),
                new ChangeHairHueEntry( "*****", 1118, 16 ),
                new ChangeHairHueEntry( "*****", 1134, 16 )
            };
    }

    public class ChangeHairHueGump : Gump
    {
        private Mobile m_From;
        private Mobile m_Vendor;
        private int m_Price;
        private Layer[] m_Layers;
        private ChangeHairHueEntry[] m_Entries;

        public ChangeHairHueGump(Mobile from, Mobile vendor, int price, Layer[] layers, ChangeHairHueEntry[] entries)
            : base(50, 50)
        {
            m_From = from;
            m_Vendor = vendor;
            m_Price = price;
            m_Layers = layers;
            m_Entries = entries;

            from.CloseGump(typeof(HairstylistBuyGump));
            from.CloseGump(typeof(ChangeHairHueGump));
            from.CloseGump(typeof(ChangeHairstyleGump));

            AddPage(0);

            AddBackground(100, 10, 350, 370, 2600);
            AddBackground(120, 54, 110, 270, 5100);

            AddHtmlLocalized(155, 25, 240, 30, 1011013, false, false); // <center>Hair Color Selection Menu</center>

            AddHtmlLocalized(150, 330, 220, 35, 1011014, false, false); // Dye my hair this color!
            AddButton(380, 330, 4005, 4007, 1, GumpButtonType.Reply, 0);

            for (int i = 0; i < entries.Length; ++i)
            {
                ChangeHairHueEntry entry = entries[i];

                AddLabel(130, 59 + (i * 22), entry.Hues[0] - 1, entry.Name);
                AddButton(207, 60 + (i * 22), 5224, 5224, 0, GumpButtonType.Page, 1 + i);
            }

            for (int i = 0; i < entries.Length; ++i)
            {
                ChangeHairHueEntry entry = entries[i];
                int[] hues = entry.Hues;
                string name = entry.Name;

                AddPage(1 + i);

                for (int j = 0; j < hues.Length; ++j)
                {
                    AddLabel(278 + ((j / 16) * 80), 52 + ((j % 16) * 17), hues[j] - 1, name);
                    AddRadio(260 + ((j / 16) * 80), 52 + ((j % 16) * 17), 210, 211, false, (j * entries.Length) + i);
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                int[] switches = info.Switches;

                if (switches.Length > 0)
                {
                    int index = switches[0] % m_Entries.Length;
                    int offset = switches[0] / m_Entries.Length;

                    if (index >= 0 && index < m_Entries.Length)
                    {
                        if (offset >= 0 && offset < m_Entries[index].Hues.Length)
                        {
                            int hue = m_Entries[index].Hues[offset];

                            bool hasConsumed = false;

                            for (int i = 0; i < m_Layers.Length; ++i)
                            {
                                Item item = m_From.FindItemOnLayer(m_Layers[i]);

                                if (item == null)
                                    continue;

                                if (!hasConsumed)
                                {
                                    if (!Banker.CombinedWithdrawFromAllEnrolled(m_From, m_Price))
                                    {
                                        m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042293, m_From.NetState); // You cannot afford my services for that style.
                                        return;
                                    }

                                    hasConsumed = true;
                                }

                                item.Hue = hue;
                            }

                            if (!hasConsumed)
                                m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502623, m_From.NetState); // You have no hair to dye and you cannot use this.
                        }
                    }
                }
                else
                {
                    // You decide not to change your hairstyle.
                    m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, m_From.NetState);
                }
            }
            else
            {
                // You decide not to change your hairstyle.
                m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, m_From.NetState);
            }
        }
    }

    public class ChangeHairstyleEntry
    {
        private Type m_ItemType;
        private int m_GumpID;
        private int m_X, m_Y;

        public Type ItemType { get { return m_ItemType; } }
        public int GumpID { get { return m_GumpID; } }
        public int X { get { return m_X; } }
        public int Y { get { return m_Y; } }

        public ChangeHairstyleEntry(int gumpID, int x, int y, Type itemType)
        {
            m_GumpID = gumpID;
            m_X = x;
            m_Y = y;
            m_ItemType = itemType;
        }

        public static readonly ChangeHairstyleEntry[] HairEntries = new ChangeHairstyleEntry[]
            {
                new ChangeHairstyleEntry( 50700,  70 - 137,  20 -  60, typeof( ShortHair ) ),
                new ChangeHairstyleEntry( 60710, 193 - 260,  18 -  60, typeof( PageboyHair ) ),
                new ChangeHairstyleEntry( 50703, 316 - 383,  25 -  60, typeof( Mohawk ) ),
                new ChangeHairstyleEntry( 60708,  70 - 137,  75 - 125, typeof( LongHair ) ),
                new ChangeHairstyleEntry( 60900, 193 - 260,  85 - 125, typeof( Afro ) ),
                new ChangeHairstyleEntry( 60713, 320 - 383,  85 - 125, typeof( KrisnaHair ) ),
                new ChangeHairstyleEntry( 60702,  70 - 137, 140 - 190, typeof( PonyTail ) ),
                new ChangeHairstyleEntry( 60707, 193 - 260, 140 - 190, typeof( TwoPigTails ) ),
                new ChangeHairstyleEntry( 60901, 315 - 383, 150 - 190, typeof( ReceedingHair ) ),
                new ChangeHairstyleEntry( 0, 0, 0, null )
            };

        public static readonly ChangeHairstyleEntry[] BeardEntries = new ChangeHairstyleEntry[]
            {
                new ChangeHairstyleEntry( 50800, 120 - 187,  30 -  80, typeof( Goatee ) ),
                new ChangeHairstyleEntry( 50904, 243 - 310,  33 -  80, typeof( MediumShortBeard ) ),
                new ChangeHairstyleEntry( 50906, 120 - 187, 100 - 150, typeof( Vandyke ) ),
                new ChangeHairstyleEntry( 50801, 243 - 310,  95 - 150, typeof( LongBeard ) ),
                new ChangeHairstyleEntry( 50802, 120 - 187, 173 - 220, typeof( ShortBeard ) ),
                new ChangeHairstyleEntry( 50905, 243 - 310, 165 - 220, typeof( MediumLongBeard ) ),
                new ChangeHairstyleEntry( 50808, 120 - 187, 242 - 290, typeof( Mustache ) ),
                new ChangeHairstyleEntry( 0, 0, 0, null )
            };
    }

    public class ChangeHairstyleGump : Gump
    {
        private Mobile m_From;
        private Mobile m_Vendor;
        private int m_Price;
        private Layer m_Layer;
        private ChangeHairstyleEntry[] m_Entries;

        public ChangeHairstyleGump(Mobile from, Mobile vendor, int price, Layer layer, ChangeHairstyleEntry[] entries)
            : base(50, 50)
        {
            m_From = from;
            m_Vendor = vendor;
            m_Price = price;
            m_Layer = layer;
            m_Entries = entries;

            from.CloseGump(typeof(HairstylistBuyGump));
            from.CloseGump(typeof(ChangeHairHueGump));
            from.CloseGump(typeof(ChangeHairstyleGump));

            int tableWidth = (layer == Layer.Hair ? 3 : 2);
            int tableHeight = ((entries.Length + tableWidth - (layer == Layer.Hair ? 2 : 1)) / tableWidth);
            int offsetWidth = 123;
            int offsetHeight = (layer == Layer.Hair ? 65 : 70);

            AddPage(0);

            AddBackground(0, 0, 81 + (tableWidth * offsetWidth), 105 + (tableHeight * offsetHeight), 2600);

            AddButton(45, 45 + (tableHeight * offsetHeight), 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(77, 45 + (tableHeight * offsetHeight), 90, 35, 1006044, false, false); // Ok

            AddButton(81 + (tableWidth * offsetWidth) - 180, 45 + (tableHeight * offsetHeight), 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(81 + (tableWidth * offsetWidth) - 148, 45 + (tableHeight * offsetHeight), 90, 35, 1006045, false, false); // Cancel

            if (layer == Layer.Hair)
                AddHtmlLocalized(50, 15, 350, 20, 1018353, false, false); // <center>New Hairstyle</center>
            else
                AddHtmlLocalized(55, 15, 200, 20, 1018354, false, false); // <center>New Beard</center>

            for (int i = 0; i < entries.Length; ++i)
            {
                int xTable = i % tableWidth;
                int yTable = i / tableWidth;

                if (entries[i].GumpID != 0)
                {
                    AddRadio(40 + (xTable * offsetWidth), 70 + (yTable * offsetHeight), 208, 209, false, i);
                    AddBackground(87 + (xTable * offsetWidth), 50 + (yTable * offsetHeight), 50, 50, 2620);
                    AddImage(87 + (xTable * offsetWidth) + entries[i].X, 50 + (yTable * offsetHeight) + entries[i].Y, entries[i].GumpID);
                }
                else if (layer == Layer.Hair)
                {
                    AddRadio(40 + ((xTable + 1) * offsetWidth), 240, 208, 209, false, i);
                    AddHtmlLocalized(60 + ((xTable + 1) * offsetWidth), 240, 85, 35, 1011064, false, false); // Bald
                }
                else
                {
                    AddRadio(40 + (xTable * offsetWidth), 70 + (yTable * offsetHeight), 208, 209, false, i);
                    AddHtmlLocalized(60 + (xTable * offsetWidth), 70 + (yTable * offsetHeight), 85, 35, 1011064, false, false); // Bald
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Layer == Layer.FacialHair && (m_From.Female || m_From.Body.IsFemale))
                return;

            if (info.ButtonID == 1)
            {
                int[] switches = info.Switches;

                if (switches.Length > 0)
                {
                    int index = switches[0];

                    if (index >= 0 && index < m_Entries.Length)
                    {
                        ChangeHairstyleEntry entry = m_Entries[index];

                        if (m_From is PlayerMobile)
                            ((PlayerMobile)m_From).SetHairMods(-1, -1);

                        Item hair = m_From.FindItemOnLayer(m_Layer);

                        if (entry.ItemType == null)
                        {
                            if (hair == null)
                                return;

                            if (Banker.CombinedWithdrawFromAllEnrolled(m_From, m_Price))
                                hair.Delete();
                            else
                                m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042293, m_From.NetState); // You cannot afford my services for that style.
                        }
                        else
                        {
                            if (hair != null && hair.GetType() == entry.ItemType)
                                return;

                            Item newHair = null;

                            try { newHair = Activator.CreateInstance(entry.ItemType, null) as Item; }
                            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                            if (newHair == null)
                                return;

                            if (Banker.CombinedWithdrawFromAllEnrolled(m_From, m_Price))
                            {
                                if (hair != null)
                                {
                                    newHair.Hue = hair.Hue;
                                    hair.Delete();
                                }

                                m_From.AddItem(newHair);
                            }
                            else
                            {
                                newHair.Delete();
                                m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042293, m_From.NetState); // You cannot afford my services for that style.
                            }
                        }
                    }
                }
                else
                {
                    // You decide not to change your hairstyle.
                    m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, m_From.NetState);
                }
            }
            else
            {
                // You decide not to change your hairstyle.
                m_Vendor.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1013009, m_From.NetState);
            }
        }
    }
}