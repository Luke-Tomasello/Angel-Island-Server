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

/* Scripts/Gumps/ForensicKillerGump.cs
 *
 *	ChangeLog:
 *	5/19/04 created by smerX
 */

using Server.Items;
using Server.Network;
using System.Collections;

namespace Server.Gumps
{
    public class ForensicChoiceGump : Gump
    {
        private Mobile m_Owner;
        private Mobile m_Killer;
        private ArrayList m_Looters;
        private BountyLedger m_Book;

        public ForensicChoiceGump(Mobile owner, Mobile killer, ArrayList looters, BountyLedger book)
            : base(30, 30)
        {
            owner.CloseGump(typeof(ForensicChoiceGump));

            m_Owner = owner;
            m_Killer = killer;
            m_Looters = looters;
            m_Book = book;

            BuildChoiceGump();
        }

        public void BuildChoiceGump()
        {
            int gumpsizeX = 300;
            int gumpsizeY = 140;
            int borderwidth = 8;

            int firstbuttonY = 65;
            int secondbuttonY = 100;
            int bothX = 195;

            AddPage(0);

            AddBackground(0, 0, gumpsizeX, gumpsizeY, PropsConfig.BackGumpID);
            AddImageTiled(borderwidth - 1, borderwidth, 35 + ((gumpsizeX - (borderwidth * 2)) / 2), gumpsizeY - (borderwidth * 2), PropsConfig.HeaderGumpID);

            AddLabel(20, 20, 0x47e, "Do you seek information");
            AddLabel(20, 50, 0x47e, "about the killer, or those");
            AddLabel(20, 80, 0x47e, "who looted the corpse?");

            AddButton(bothX, firstbuttonY, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
            AddLabel(bothX + 35, firstbuttonY, 0x0, "Killer");
            //AddHtml( firstbuttonX + 35, bothY, 90, 80, "Cancel", false, false );

            AddButton(bothX, secondbuttonY, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0);
            AddLabel(bothX + 35, secondbuttonY, 0x0, "Looters");
            //AddHtml( secondbuttonX + 35, bothY, 90, 80, "Add", false, false );
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            switch (info.ButtonID)
            {
                case 0: // Closed
                    {
                        from.SendMessage("You decide not to examine this corpse.");
                        break;
                    }
                case 1: // Killer
                    {
                        Mobile m = m_Killer;

                        if (m.Deleted)
                        {
                            from.SendMessage("That player has deleted their character.");
                            from.SendGump(new ForensicChoiceGump(from, m_Killer, m_Looters, m_Book));
                        }
                        else
                        {
                            from.SendGump(new ForensicKillerGump(m_Owner, m_Killer, m_Book));
                        }

                        break;
                    }
                default: // Looters
                    {
                        from.SendGump(new ForensicLootGump(from, m_Looters, 0, m_Book));
                        break;
                    }
            }
        }
    }
}