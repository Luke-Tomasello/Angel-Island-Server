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

/* Items/Deeds/SkinHueCreme.cs
 * ChangeLog:
 *	1/30/07
 *		- rewrite of script from RunUO forums 
 *      - remove hue from deed
 *      - remove stupid colors
 *      - add in correct color transform code (thanks pix)
 *      - make bacpack check on DoubleClick
 *      - convert from a deed to a rare bottle of creme
 */

using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    public class SkinHueCreme : Item
    {
        [Constructable]
        public SkinHueCreme()
            : base(0x0EFB)
        {
            Weight = 1.0;
            Name = "Adregan's skin creme";
            Hue = 0;
        }

        public SkinHueCreme(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void OnDoubleClick(Mobile from)
        {
            // Make sure is in pack
            if (from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);  // Must be in pack to use!!
                return;
            }

            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.CloseGump(typeof(SkinHueGump));
                from.SendGump(new SkinHueGump(this));
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 906, 1019045);
            }
        }
    }

    public class SkinHueGump : Gump
    {
        private SkinHueCreme m_SkinHue;

        private class SkinHueEntry
        {
            private string m_Name;
            private int m_HueStart;
            private int m_HueCount;

            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            public int HueStart
            {
                get
                {
                    return m_HueStart;
                }
            }

            public int HueCount
            {
                get
                {
                    return m_HueCount;
                }
            }

            public SkinHueEntry(string name, int hueStart, int hueCount)
            {
                m_Name = name;
                m_HueStart = hueStart;
                m_HueCount = hueCount;
            }
        }

        /*
		 * Interestingly enough, in charactercreation.cs, it calls Utility.ClipSkinHue(int hue) which is:

			  public static int ClipSkinHue( int hue ) 
			  { 
				   if ( hue < 1002 ) 
						return 1002; 
				   else if ( hue > 1058 ) 
						return 1058; 
				   else 
						return hue; 
			  } 
			so the only valid skins for character creation are 1002-1058 (inclusive)
			newChar.Hue = Utility.ClipSkinHue( args.Hue & 0x3FFF ) | 0x8000;
		 */

        private static SkinHueEntry[] m_Entries = new SkinHueEntry[]
            {
                //The first number is the starting hue, The second is how
                //many hues after the starting hue will show up on the page.
                new SkinHueEntry( "*****",  1002, 8 ),  // 1002 - 1009
                new SkinHueEntry( "*****",  1010, 8 ),  // 1010 - 1017
                new SkinHueEntry( "*****",  1018, 8 ),  // 1018 - 1025
                new SkinHueEntry( "*****",  1026, 8 ),  // 1026 - 1033
                new SkinHueEntry( "*****",  1034, 8 ),  // 1034 - 1041
                new SkinHueEntry( "*****",  1042, 8 ),  // 1042 - 1049
                new SkinHueEntry( "*****",  1050, 9 ),  // 1050 - 1058

                /* RunUO scripters retarded colors
				new SkinHueEntry( "*****", 1602, 26 ),
				new SkinHueEntry( "*****", 1628, 27 ),
				new SkinHueEntry( "*****", 1502, 32 ),
				new SkinHueEntry( "*****", 1302, 32 ),
				new SkinHueEntry( "*****", 1402, 32 ),
				new SkinHueEntry( "*****", 1202, 24 ),
				new SkinHueEntry( "*****", 2402, 29 ),
				new SkinHueEntry( "*****", 2213, 6 ),
				new SkinHueEntry( "*****", 1102, 8 ),
				new SkinHueEntry( "*****", 1110, 8 ),
				new SkinHueEntry( "*****", 1118, 16 ),
				new SkinHueEntry( "*****", 1134, 16 )
                 **/
			};

        public SkinHueGump(SkinHueCreme dye)
            : base(50, 50)
        {
            m_SkinHue = dye;

            AddPage(0);

            AddBackground(100, 10, 350, 355, 2600);
            AddBackground(120, 54, 110, 270, 5100);

            AddLabel(150, 25, 400, @"Skin tone Selection Menu");

            AddButton(149, 328, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddLabel(185, 329, 250, @"Apply this skin tone");

            for (int i = 0; i < m_Entries.Length; ++i)
            {
                AddLabel(130, 59 + (i * 22), m_Entries[i].HueStart - 1, m_Entries[i].Name);
                AddButton(207, 60 + (i * 22), 5224, 5224, 0, GumpButtonType.Page, i + 1);
            }

            for (int i = 0; i < m_Entries.Length; ++i)
            {
                SkinHueEntry e = m_Entries[i];

                AddPage(i + 1);

                for (int j = 0; j < e.HueCount; ++j)
                {
                    AddLabel(278 + ((j / 16) * 80), 52 + ((j % 16) * 17), e.HueStart + j - 1, "*****");
                    AddRadio(260 + ((j / 16) * 80), 52 + ((j % 16) * 17), 210, 211, false, (i * 100) + j);
                }
            }
        }

        public override void OnResponse(NetState from, RelayInfo info)
        {
            if (m_SkinHue.Deleted)
                return;

            Mobile m = from.Mobile;
            int[] switches = info.Switches;

            if (m.Backpack == null || !m_SkinHue.IsChildOf(m.Backpack))
            {
                m.SendLocalizedMessage(1042010);
                return;
            }

            if (info.ButtonID != 0 && switches.Length > 0)
            {
                int entryIndex = switches[0] / 100;
                int hueOffset = switches[0] % 100;

                if (entryIndex >= 0 && entryIndex < m_Entries.Length)
                {
                    SkinHueEntry e = m_Entries[entryIndex];

                    if (hueOffset >= 0 && hueOffset < e.HueCount)
                    {
                        int hue = e.HueStart + hueOffset;

                        m.Hue = Utility.ClipSkinHue(hue & 0x3FFF) | 0x8000;
                        m.SendMessage("You apply the lotion.");
                        m_SkinHue.Delete();
                        m.PlaySound(0x4E);
                    }
                }
            }
            else
            {
                m.SendMessage("You decide not to apply the creme.");
            }
        }
    }
}