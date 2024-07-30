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

/* Changelog Scripts/Gumps/PlayerVendorGumps.cs
 *  1/6/22, Adam (CombinedWithdrawFromAllEnrolled)
 *      Use the new banking system to pay for the purchase if the backpack doesn't have sufficient funds.
 *	4/21/10, adam
 *		Only charge commissions on comission vendors
 *	1/17/08, Adam
 *		- Remove double call to m_Vendor.ChargeCommission()
 *		- Remove explicit check for AccessLevel.Administrator owned vendors and replaced mobile.StaffOwned (set if a snaff member places the mobile.)
 *		- Add display of commission for commission based vendors
 *  1/14/08, Adam
 *      Add support for Commission based PricingModel
 *	7/01/07: Pix
 *		Added no charge notification if this vendor is owned by an admin.
 * 04/22/05, Kitaras
 *	Removed all clothing/weapons from custimize menu, now only allows changeing of hair/bear and dyeing of hair/beard
 * 04/18/05, Kitaras
 *	Added check so that if a item is hued via customize menu it becomes unmoveable(prevents players from hueing sandals other colors)
 * 04/17/05, Kitaras
 *	Made custom items on playervendors un-movable to prevent players spawning them then dragging off and respawning for a 
 *	infinite resource potential, as players can now dress their vendors(via paperdoll)
 * 10/31/04, Froste 
 *      Newbied custom items on playervendors so they don't drop when the vendor dies.
 */

using Server.HuePickers;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Gumps
{
    public class PlayerVendorBuyGump : Gump
    {
        private Item m_Item;
        private VendorItem m_VI;
        private PlayerVendor m_Vendor;

        public PlayerVendorBuyGump(Item i, PlayerVendor vend, VendorItem vi)
            //: base(100, 200)
            : base(120, 200)
        {
            m_Item = i;
            m_Vendor = vend;
            m_VI = vi;

            string desc = m_VI.Description;

            if (desc != null && (desc = desc.Trim()).Length == 0)
                desc = null;

            AddPage(0);

            AddBackground(100, 10, 300, 150, 5054);

            AddHtmlLocalized(125, 20, 250, 24, 1019070, false, false); // You have agreed to purchase:

            int offset = 0;
            if (desc != null)
            {
                string[] strings = LineWrap(desc);
                if (strings.Length == 1)
                    AddLabel(125, 45, 0, desc);
                else
                {
                    AddLabel(125, 45, 0, strings[0]);
                    AddLabel(125, 45 + 12, 0, strings[1]);
                    offset = 12;
                }
            }
            else
                AddHtmlLocalized(125, 45, 250, 24, 1019072, false, false); // an item without a description

            AddHtmlLocalized(125, 70 + offset, 250, 24, 1019071, false, false); // for the amount of:
            AddLabel(125, 95, 0, String.Format("{0:n0}", m_VI.Price));

            AddButton(250, 130, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(282, 130, 100, 24, 1011012, false, false); // CANCEL

            AddButton(120, 130, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtmlLocalized(152, 130, 100, 24, 1011036, false, false); // OKAY
        }

        private static string[] LineWrap(string text)
        {
            int breakAt = 38;                                               // 38 characters per line
            List<string> lines = new() { string.Empty, string.Empty };      // max two lines
            int maxLines = lines.Count;                                     // how many lines we support
            if (text.Length > breakAt)                                      // do we even need to break this line?
            {                                                               // chop it up and start processing
                string[] toks = text.Split(' ', StringSplitOptions.TrimEntries);
                int lineNdx = 0;
                for (int ndx = 0; ndx < toks.Length; ndx++)
                {                                                           // concatenate to this line
                    string nextTok = (ndx + 1 < toks.Length) ? toks[ndx + 1] : "";
                    if (lines[lineNdx].Length + 1 /*space*/ + nextTok.Length <= breakAt)
                        lines[lineNdx] += toks[ndx] + " ";
                    else
                    {                                                       // move to the next line
                        lines[lineNdx] = lines[lineNdx].Trim();
                        if (lineNdx + 1 == maxLines)
                            break;                                          // too many lines. give up
                        else
                            lineNdx++;                                      // next line
                        lines[lineNdx] += toks[ndx] + " ";                  // start building now
                    }
                }

                lines[lineNdx] = lines[lineNdx].Trim();                     // cleanup and we're done
            }
            else
                return new string[] { text };                                 // no need for a second line

            return lines.ToArray();                                         // here you go!
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_Vendor.Deleted)
                return;

            Mobile from = state.Mobile;

            from.CloseGump(typeof(PlayerVendorBuyGump));

            if (info.ButtonID == 2)
            {
                if (m_VI.IsForSale && m_Item.RootParent == m_Vendor)
                {
                    m_Vendor.Say(from.Name);

                    Container pack = from.Backpack;

                    if (pack != null && (pack.ConsumeTotal(typeof(Gold), m_VI.Price)) || Banker.CombinedWithdrawFromAllEnrolled(from, m_VI.Price))
                    {
                        int price = m_VI.Price;
                        m_Vendor.HoldGold += price;
                        m_Vendor.RemoveInfo(m_Item);

                        if (pack == null || !pack.TryDropItem(from, m_Item, false))
                        {
                            m_Vendor.SayTo(from, 503204); // You do not have room in your backpack for this.
                            m_Item.MoveToWorld(from.Location, from.Map);
                        }

                        // charge our N% commission AFTER the customer gets their goods (in case the commission charge causes the vendor to die.)
                        if (m_Vendor.PricingModel == PricingModel.Commission)
                            m_Vendor.Charge((int)(price * m_Vendor.Commission), PlayerVendor.ChargeReason.Commission);

                        // record this item purchase for our gold sink tracking
                        if (m_Vendor.Owner == World.GetAdminAcct())
                            Engines.DataRecorder.DataRecorder.GoldSink(from, m_Item, m_VI.Price);
                    }
                    else
                    {
                        m_Vendor.SayTo(from, 503205); // You cannot afford this item.
                    }
                }
                else
                {
                    m_Vendor.SayTo(from, 503216); // You can't buy that.
                }
            }
        }
    }

    public class PlayerVendorOwnerGump : Gump
    {
        private PlayerVendor m_Vendor;

        public PlayerVendorOwnerGump(PlayerVendor vend, Mobile from)
            : base(50, (vend.PricingModel == PricingModel.Commission) ? 224 : 200)
        {
            m_Vendor = vend;
            int perDay = m_Vendor.ChargePerDay;

            from.CloseGump(typeof(PlayerVendorOwnerGump));
            from.CloseGump(typeof(PlayerVendorCustomizeGump));

            AddPage(0);
            AddBackground(25, 10, 530, (vend.PricingModel == PricingModel.Commission) ? 164 : 140, 5054);

            AddHtmlLocalized(425, 25, 120, 20, 1019068, false, false); // See goods
            AddButton(390, 25, 4005, 4007, 1, GumpButtonType.Reply, 0);

            if (m_Vendor.Body == 0x191 || m_Vendor.Body == 0x190)
            {
                AddHtmlLocalized(425, 48, 120, 20, 1019069, false, false); // Customize
                AddButton(390, 48, 4005, 4007, 2, GumpButtonType.Reply, 0);
            }

            // 8/26/23, Yoar: If we don't have CMEs, allow opening of the paperdoll through the gump
            if ((m_Vendor.Body == 0x191 || m_Vendor.Body == 0x190) && !Core.RuleSets.ContextMenuRules())
            {
                AddHtmlLocalized(425, 72, 120, 20, 3006123, false, false); // Open Paperdoll
                AddButton(390, 71, 4005, 4007, 3, GumpButtonType.Reply, 0);

                AddHtmlLocalized(425, 96, 120, 20, 1011012, false, false); // CANCEL
                AddButton(390, 95, 4005, 4007, 0, GumpButtonType.Reply, 0);
            }
            else
            {
                AddHtmlLocalized(425, 72, 120, 20, 1011012, false, false); // CANCEL
                AddButton(390, 71, 4005, 4007, 0, GumpButtonType.Reply, 0);
            }

            AddHtmlLocalized(40, 72, 260, 20, 1038321, false, false); // Gold held for you:
            AddLabel(300, 72, 0, m_Vendor.HoldGold.ToString());
            AddHtmlLocalized(40, 96, 260, 20, 1038322, false, false); // Gold held in my account:
            AddLabel(300, 96, 0, m_Vendor.BankAccount.ToString());

            //AddHtmlLocalized( 40, 120, 260, 20, 1038324, false, false ); // My charge per day is:
            // Localization has changed, we must use a string here
            AddHtml(40, 120, 260, 20, "My charge per day is:", false, false);
            AddLabel(300, 120, 0, perDay.ToString());

            if (vend.PricingModel == PricingModel.Commission)
            {
                AddHtml(40, 144, 260, 20, "My per item commission is:", false, false);
                AddLabel(300, 144, 0, String.Format("{0}%", ((int)(vend.Commission * 100)).ToString()));
            }

            double days = (m_Vendor.HoldGold + m_Vendor.BankAccount) / ((double)perDay);

            AddHtmlLocalized(40, 25, 260, 20, 1038318, false, false); // Amount of days I can work: 
            AddLabel(300, 25, 0, ((int)days).ToString());
            AddHtmlLocalized(40, 48, 260, 20, 1038319, false, false); // Earth days: 
            AddLabel(300, 48, 0, ((int)(days / 12.0)).ToString());

            if (m_Vendor.IsStaffOwned == true)
            {
                AddHtml(350, 120, 300, 20, "(no charge: admin vendor)", false, false);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_Vendor.Deleted)
                return;

            Mobile from = state.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        Container pack = m_Vendor.Backpack;

                        if (pack != null)
                        {
                            m_Vendor.SayTo(from, 1010642); // Take a look at your goods.
                            pack.DisplayTo(from);
                        }

                        break;
                    }
                case 2:
                    {
                        from.SendGump(new PlayerVendorCustomizeGump(m_Vendor, from));
                        break;
                    }
                case 3:
                    {
                        if ((m_Vendor.Body == 0x191 || m_Vendor.Body == 0x190) && !Core.RuleSets.ContextMenuRules())
                            m_Vendor.DisplayPaperdollTo(from);

                        break;
                    }
            }
        }
    }

    public class PlayerVendorCustomizeGump : Gump
    {
        private Mobile m_Vendor;

        private class CustomItem
        {
            private Type m_Type;
            private int m_LocNum;
            private int m_ArtNum;
            private bool m_LongText;

            public CustomItem(Type type, int loc)
                : this(type, loc, 0)
            {
            }

            public CustomItem(Type type, int loc, int art)
                : this(type, loc, art, false)
            {
            }

            public CustomItem(Type type, int loc, int art, bool longText)
            {
                m_Type = type;
                m_LocNum = loc;
                m_ArtNum = art;
                m_LongText = longText;
            }

            public Item Create()
            {
                Item i = null;

                try
                {
                    ConstructorInfo ctor = m_Type.GetConstructor(new Type[0]);
                    if (ctor != null)
                        i = ctor.Invoke(null) as Item;
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                return i;
            }

            public Type Type { get { return m_Type; } }
            public int LocNumber { get { return m_LocNum; } }
            public int ArtNumber { get { return m_ArtNum; } }
            public bool LongText { get { return m_LongText; } }
        }

        private class CustomCategory
        {
            private CustomItem[] m_Entries;
            private Layer m_Layer;
            private bool m_CanDye;
            private int m_LocNum;

            public CustomCategory(Layer layer, int loc, bool canDye, CustomItem[] items)
            {
                m_Entries = items;
                m_CanDye = canDye;
                m_Layer = layer;
                m_LocNum = loc;
            }

            public bool CanDye { get { return m_CanDye; } }
            public CustomItem[] Entries { get { return m_Entries; } }
            public Layer Layer { get { return m_Layer; } }
            public int LocNumber { get { return m_LocNum; } }
        }

        private static CustomCategory[] Categories = new CustomCategory[]{
			//new CustomCategory( Layer.InnerTorso, 1011357, true, new CustomItem[]{// Upper Torso
				//new CustomItem( typeof( Shirt ), 		1011359, 5399 ),
				//new CustomItem( typeof( FancyShirt ),	1011360, 7933 ),
				//new CustomItem( typeof( PlainDress ),	1011363, 7937 ),
				//new CustomItem( typeof( FancyDress ),	1011364, 7935 ),
				//new CustomItem( typeof( Robe ),			1011365, 7939 )
			//} ),
			
			//new CustomCategory( Layer.MiddleTorso, 1011371, true, new CustomItem[]{//Over chest
				//new CustomItem( typeof( Doublet ), 		1011358, 8059 ),
				//new CustomItem( typeof( Tunic ),		1011361, 8097 ),
			//	new CustomItem( typeof( JesterSuit ), 	1011366, 8095 ),
			//	new CustomItem( typeof( BodySash ),		1011372, 5441 ),
			//	new CustomItem( typeof( Surcoat ),		1011362, 8189 ),
			//	new CustomItem( typeof( HalfApron ),	1011373, 5435 ),
			//	new CustomItem( typeof( FullApron ),	1011374, 5437 ),
			//} ),
			
			//new CustomCategory( Layer.Shoes, 1011388, true, new CustomItem[]{//Footwear
			//	new CustomItem( typeof( Sandals ),		1011389, 5901 ),
			//	new CustomItem( typeof( Shoes ),		1011390, 5904 ),
			//	new CustomItem( typeof( Boots ),		1011391, 5899 ),
			//	new CustomItem( typeof( ThighBoots ),	1011392, 5906 ),
			//} ),
			
			//new CustomCategory( Layer.Helm, 1011375, true, new CustomItem[]{//Hats
			//	new CustomItem( typeof( SkullCap ),		1011376, 5444 ),
			//	new CustomItem( typeof( Bandana ), 		1011377, 5440 ),
			//	new CustomItem( typeof( FloppyHat ),	1011378, 5907 ),
			//	new CustomItem( typeof( WideBrimHat ),	1011379, 5908 ),
			//	new CustomItem( typeof( Cap ),			1011380, 5909 ),
			//	new CustomItem( typeof( TallStrawHat ),	1011382, 5910 )
			//} ),
			
			//new CustomCategory( Layer.Helm, 1015319, true, new CustomItem[]{//More Hats
			//    new CustomItem( typeof( StrawHat ),		1011382, 5911 ),
			///	new CustomItem( typeof( WizardsHat ), 	1011383, 5912 ),
			//	new CustomItem( typeof( Bonnet ),		1011384, 5913 ),
			//	new CustomItem( typeof( FeatheredHat ),	1011385, 5914 ),
			//	new CustomItem( typeof( TricorneHat ),	1011386, 5915 ),
			//	new CustomItem( typeof( JesterHat ),	1011387, 5916 )
			//} ),
			
			//new CustomCategory( Layer.Pants, 1011367, true, new CustomItem[]{ //Lower Torso
			//	new CustomItem( typeof( LongPants ),	1011368, 5433 ),
			//	new CustomItem( typeof( Kilt ), 		1011369, 5431 ),
			//	new CustomItem( typeof( Skirt ),		1011370, 5398 ),
			//} ),
			
			//new CustomCategory( Layer.Cloak, 1011393, true, new CustomItem[]{ // Back
			//	new CustomItem( typeof( Cloak ),		1011394, 5397 )
			//} ),
			
			new CustomCategory( Layer.Hair, 1011395, true, new CustomItem[]{ // Hair
				new CustomItem( typeof( ShortHair ),    1011052 ),
                new CustomItem( typeof( LongHair ),     1011053 ),
                new CustomItem( typeof( PonyTail ),     1011054 ),
                new CustomItem( typeof( Mohawk ),       1011055 ),
                new CustomItem( typeof( PageboyHair ),  1011047 ),
                new CustomItem( typeof( KrisnaHair ),   1011050 ),
                new CustomItem( typeof( Afro ),         1011396 ),
                new CustomItem( typeof( ReceedingHair ),1011048 ),
                new CustomItem( typeof( TwoPigTails ),  1011049 ),
            } ),

            new CustomCategory( Layer.FacialHair, 1015320, true, new CustomItem[]{//Facial Hair
				new CustomItem( typeof( Mustache ),         1011062 ),
                new CustomItem( typeof( ShortBeard ),       1011060 ),
                new CustomItem( typeof( MediumShortBeard ), 1015321, 0, true ),
                new CustomItem( typeof( LongBeard ),        1011061 ),
                new CustomItem( typeof( MediumLongBeard ),  1015322, 0, true ),
                new CustomItem( typeof( Goatee ),           1015323 ),
                new CustomItem( typeof( Vandyke ),          1011401 ),
            } )
			
			//new CustomCategory( Layer.FirstValid, 1011397, false, new CustomItem[]{//Held items
			//	new CustomItem( typeof( FishingPole ), 	1011406, 3520 ),
			//	new CustomItem( typeof( Pickaxe ),		1011407, 3717 ),
			//	new CustomItem( typeof( Pitchfork ),	1011408, 3720 ),
			//	new CustomItem( typeof( Cleaver ),		1015324, 3778 ),
			//	new CustomItem( typeof( Mace ),			1011409, 3933 ),
			////	new CustomItem( typeof( Torch ),		1011410, 3940 ),
			//	new CustomItem( typeof( Hammer ),		1011411, 4020 ),
			////	new CustomItem( typeof( Longsword ),	1011412, 3936 ),
			//	new CustomItem( typeof( GnarledStaff ), 1011413, 5113 )
			//} ),
			
			//new CustomCategory( Layer.FirstValid, 1015325, false, new CustomItem[]{//More held items
			//	new CustomItem( typeof( Crossbow ),		1011414, 3920 ),
			//	new CustomItem( typeof( WarMace ),		1011415, 5126 ),
			//	new CustomItem( typeof( TwoHandedAxe ),	1011416, 5186 ),
			//	new CustomItem( typeof( Spear ),		1011417, 3939 ),
			//	new CustomItem( typeof( Katana ),		1011418, 5118 ),
			//	new CustomItem( typeof( Spellbook ),	1011419, 3834 )
			//} )
		};

        public PlayerVendorCustomizeGump(Mobile v, Mobile from)
            : base(30, 40)
        {
            m_Vendor = v;
            int x, y;

            from.CloseGump(typeof(PlayerVendorCustomizeGump));

            AddPage(0);
            AddBackground(0, 0, 585, 393, 5054);
            AddBackground(195, 36, 387, 275, 3000);
            AddHtmlLocalized(10, 10, 565, 18, 1011356, false, false); // <center>VENDOR CUSTOMIZATION MENU</center>
            AddHtmlLocalized(60, 355, 150, 18, 1011036, false, false); // OKAY
            AddButton(25, 355, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(320, 355, 150, 18, 1011012, false, false); // CANCEL
            AddButton(285, 355, 4005, 4007, 0, GumpButtonType.Reply, 0);

            y = 35;
            for (int i = 0; i < Categories.Length; i++)
            {
                CustomCategory cat = (CustomCategory)Categories[i];
                AddHtmlLocalized(5, y, 150, 25, cat.LocNumber, true, false);
                AddButton(155, y, 4005, 4007, 0, GumpButtonType.Page, i + 1);
                y += 25;
            }

            for (int i = 0; i < Categories.Length; i++)
            {
                CustomCategory cat = (CustomCategory)Categories[i];
                AddPage(i + 1);

                for (int c = 0; c < cat.Entries.Length; c++)
                {
                    CustomItem entry = (CustomItem)cat.Entries[c];
                    x = 198 + (c % 3) * 129;
                    y = 38 + (c / 3) * 67;

                    AddHtmlLocalized(x, y, 100, entry.LongText ? 36 : 18, entry.LocNumber, false, false);

                    if (entry.ArtNumber != 0)
                        AddItem(x + 20, y + 25, entry.ArtNumber);

                    AddRadio(x, y + (entry.LongText ? 40 : 20), 210, 211, false, (c << 8) + i);
                }

                if (cat.CanDye)
                {
                    AddHtmlLocalized(327, 239, 100, 18, 1011402, false, false); // Color
                    AddRadio(327, 259, 210, 211, false, 100 + i);
                }

                AddHtmlLocalized(456, 239, 100, 18, 1011403, false, false); // Remove
                AddRadio(456, 259, 210, 211, false, 200 + i);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_Vendor.Deleted)
                return;

            Mobile from = state.Mobile;

            from.CloseGump(typeof(PlayerVendorCustomizeGump));

            if (info.ButtonID == 0)
            {
                if (m_Vendor is PlayerVendor) // do nothing for barkeeps
                {
                    m_Vendor.Direction = m_Vendor.GetDirectionTo(from);
                    m_Vendor.Animate(32, 5, 1, true, false, 0);//bow
                    m_Vendor.SayTo(from, 1043310 + Utility.Random(12)); // a little random speech
                }
            }
            else if (info.ButtonID == 1 && info.Switches.Length > 0)
            {
                int cnum = info.Switches[0];
                int cat = cnum % 256;
                int ent = cnum >> 8;

                if (cat < Categories.Length && cat >= 0)
                {
                    if (ent < Categories[cat].Entries.Length && ent >= 0)
                    {
                        Item item = m_Vendor.FindItemOnLayer(Categories[cat].Layer);

                        if (item != null)
                            item.Delete();

                        List<Item> items = m_Vendor.Items;

                        for (int i = 0; item == null && i < items.Count; ++i)
                        {
                            Item checkitem = (Item)items[i];
                            Type type = checkitem.GetType();

                            for (int j = 0; item == null && j < Categories[cat].Entries.Length; ++j)
                            {
                                if (type == Categories[cat].Entries[j].Type)
                                    item = checkitem;
                            }
                        }

                        if (item != null)
                            item.Delete();

                        if (m_Vendor.Female && Categories[cat].Layer == Layer.FacialHair)
                        {
                            from.SendLocalizedMessage(1010639); // You cannot place facial hair on a woman!
                        }
                        else
                        {
                            item = Categories[cat].Entries[ent].Create();


                            if (item != null)
                            {
                                item.Layer = Categories[cat].Layer;
                                item.LootType = LootType.Newbied; // newbie these items so they don't drop when the vendor dies
                                item.Movable = false; //make any items added unmovable so they cant be taken off then smelted, new ones spawned,etc
                                if (!m_Vendor.EquipItem(item))
                                    item.Delete();
                            }
                        }

                        from.SendGump(new PlayerVendorCustomizeGump(m_Vendor, from));
                    }
                }
                else
                {
                    cat -= 100;

                    if (cat < 100)
                    {
                        if (cat < Categories.Length && cat >= 0)
                        {
                            Item item = null;

                            List<Item> items = m_Vendor.Items;

                            for (int i = 0; item == null && i < items.Count; ++i)
                            {
                                Item checkitem = (Item)items[i];
                                Type type = checkitem.GetType();

                                for (int j = 0; item == null && j < Categories[cat].Entries.Length; ++j)
                                {
                                    if (type == Categories[cat].Entries[j].Type)
                                        item = checkitem;
                                }
                            }

                            if (item != null)
                                new PVHuePicker(item, m_Vendor, from).SendTo(state);
                        }
                    }
                    else
                    {
                        cat -= 100;

                        if (cat < Categories.Length && cat >= 0)
                        {
                            Item item = null;

                            List<Item> items = m_Vendor.Items;

                            for (int i = 0; item == null && i < items.Count; ++i)
                            {
                                Item checkitem = (Item)items[i];
                                Type type = checkitem.GetType();

                                for (int j = 0; item == null && j < Categories[cat].Entries.Length; ++j)
                                {
                                    if (type == Categories[cat].Entries[j].Type)
                                        item = checkitem;
                                }
                            }

                            if (item != null)
                                item.Delete();

                            from.SendGump(new PlayerVendorCustomizeGump(m_Vendor, from));
                        }
                    }
                }
            }
        }

        private class PVHuePicker : HuePicker
        {
            private Item m_Item;
            private Mobile m_Vendor;
            private Mobile m_Mob;

            public PVHuePicker(Item item, Mobile v, Mobile from)
                : base((item.Layer == Layer.Hair || item.Layer == Layer.FacialHair) ? 0xFAB : item.ItemID)
            {
                m_Vendor = v;
                m_Item = item;
                m_Mob = from;
            }

            public override void OnResponse(int hue)
            {
                m_Item.Hue = hue;
                m_Item.Movable = false;
                m_Mob.SendGump(new PlayerVendorCustomizeGump(m_Vendor, m_Mob));
            }
        }
    }
}