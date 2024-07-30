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

/* Scripts/Gumps/ClientGump.cs
 * Changelog:
 *	9/7/05, Pix
 *		Added install versions for newer clients.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Accounting;
using Server.Network;
using Server.Scripts.Gumps;
using Server.Targets;
using System;

namespace Server.Gumps
{
    public class ClientGump : Gump
    {
        private NetState m_State;

        private void Resend(Mobile to, RelayInfo info)
        {
            TextRelay te = info.GetTextEntry(0);

            to.SendGump(new ClientGump(to, m_State, te == null ? "" : te.Text));
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_State == null)
                return;

            Mobile focus = m_State.Mobile;
            Mobile from = state.Mobile;

            if (focus == null)
            {
                from.SendMessage("That character is no longer online.");
                return;
            }
            else if (focus.Deleted)
            {
                from.SendMessage("That character no longer exists.");
                return;
            }
            else if (from != focus && focus.Hidden && from.AccessLevel < focus.AccessLevel)
            {
                from.SendMessage("That character is no longer visible.");
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Tell
                    {
                        TextRelay text = info.GetTextEntry(0);

                        if (text != null)
                        {
                            focus.SendMessage(0x482, "{0} tells you:", from.Name);
                            focus.SendMessage(0x482, text.Text);
                        }

                        from.SendGump(new ClientGump(from, m_State));

                        break;
                    }
                case 4: // Props
                    {
                        Resend(from, info);
                        from.SendGump(new PropertiesGump(from, focus));

                        break;
                    }
                case 5: // Go to
                    {
                        if (focus.Map == null || focus.Map == Map.Internal)
                        {
                            from.SendMessage("That character is not in the world.");
                        }
                        else
                        {
                            from.MoveToWorld(focus.Location, focus.Map);
                            Resend(from, info);
                        }

                        break;
                    }
                case 6: // Get
                    {
                        if (from.Map == null || from.Map == Map.Internal)
                        {
                            from.SendMessage("You cannot bring that person here.");
                        }
                        else
                        {
                            focus.MoveToWorld(from.Location, from.Map);
                            Resend(from, info);
                        }

                        break;
                    }
                case 7: // Move
                    {
                        from.Target = new MoveTarget(focus);
                        Resend(from, info);

                        break;
                    }
                case 8: // Kick
                    {
                        if (from.AccessLevel >= AccessLevel.GameMaster && from.AccessLevel > focus.AccessLevel)
                        {
                            focus.Say("I've been kicked!");

                            m_State.Dispose();
                        }

                        break;
                    }
                case 9: // Kill
                    {
                        if (from.AccessLevel >= AccessLevel.GameMaster && from.AccessLevel > focus.AccessLevel)
                            focus.Kill();

                        Resend(from, info);

                        break;
                    }
                case 10: //Res
                    {
                        if (from.AccessLevel >= AccessLevel.GameMaster && from.AccessLevel > focus.AccessLevel)
                        {
                            focus.PlaySound(0x214);
                            focus.FixedEffect(0x376A, 10, 16);

                            focus.Resurrect();
                        }

                        Resend(from, info);

                        break;
                    }
                case 11: // Skills
                    {
                        Resend(from, info);

                        if (from.AccessLevel > focus.AccessLevel)
                            from.SendGump(new SkillsGump(from, (Mobile)focus));

                        break;
                    }
            }
        }

        public ClientGump(Mobile from, NetState state)
            : this(from, state, "")
        {
        }

        private const int LabelColor32 = 0xFFFFFF;

        public ClientGump(Mobile from, NetState state, string initialText)
            : base(30, 20)
        {
            if (state == null)
                return;

            m_State = state;

            AddPage(0);

            AddBackground(0, 0, 400, 274, 5054);

            AddImageTiled(10, 10, 380, 19, 0xA40);
            AddAlphaRegion(10, 10, 380, 19);

            AddImageTiled(10, 32, 380, 232, 0xA40);
            AddAlphaRegion(10, 32, 380, 232);

            AddHtml(10, 10, 380, 20, Color(Center("User Information"), LabelColor32), false, false);

            int line = 0;

            AddHtml(14, 36 + (line * 20), 200, 20, Color("Address:", LabelColor32), false, false);
            AddHtml(70, 36 + (line++ * 20), 200, 20, Color(state.ToString(), LabelColor32), false, false);

            AddHtml(14, 36 + (line * 20), 200, 20, Color("Client:", LabelColor32), false, false);
            AddHtml(70, 36 + (line++ * 20), 200, 20, Color(state.Version == null ? "(null)" : state.Version.ToString(), LabelColor32), false, false);

            AddHtml(14, 36 + (line * 20), 200, 20, Color("Version:", LabelColor32), false, false);
#if true
            ExpansionInfo info = state.ExpansionInfo;
            string expansionName = info.Name;
#else
            string installVersion = "Unknown";
            // some flags yet unverified
            if ((state.Flags & 0x80) != 0) installVersion = "Unknown 0x80";
            else if ((state.Flags & 0x40) != 0) installVersion = "Unknown 0x40";
            else if ((state.Flags & 0x20) != 0) installVersion = "Mondain's Legacy";
            else if ((state.Flags & 0x10) != 0) installVersion = "Samurai Empire";
            else if ((state.Flags & 0x08) != 0) installVersion = "Age of Shadows";
            else if ((state.Flags & 0x04) != 0) installVersion = "Blackthorn's Revenge";
            else if ((state.Flags & 0x02) != 0) installVersion = "Third Dawn";
            else if ((state.Flags & 0x01) != 0) installVersion = "Renaissance";
            else installVersion = "The Second Age";
#endif
            //Pix: old:
            //AddHtml( 70, 36 + (line++ * 20), 200, 20, Color( ((state.Flags & 0x08) != 0) ? "Age of Shadows" : ((state.Flags & 0x04) != 0) ? "Blackthorn's Revenge" : ((state.Flags & 0x02) != 0) ? "Third Dawn" : ((state.Flags & 0x01) != 0) ? "Renaissance" : "The Second Age", LabelColor32 ), false, false ); // some flags yet unverified
            AddHtml(70, 36 + (line++ * 20), 200, 20, Color(expansionName, LabelColor32), false, false);

            Account a = state.Account as Account;
            Mobile m = state.Mobile;

            if (from.AccessLevel >= AccessLevel.GameMaster && a != null)
            {
                AddHtml(14, 36 + (line * 20), 200, 20, Color("Account:", LabelColor32), false, false);
                AddHtml(70, 36 + (line++ * 20), 200, 20, Color(a.Username, LabelColor32), false, false);
            }

            if (m != null)
            {
                AddHtml(14, 36 + (line * 20), 200, 20, Color("Mobile:", LabelColor32), false, false);
                AddHtml(70, 36 + (line++ * 20), 200, 20, Color(String.Format("{0} (0x{1:X})", m.Name, m.Serial.Value), LabelColor32), false, false);

                AddHtml(14, 36 + (line * 20), 200, 20, Color("Location:", LabelColor32), false, false);
                AddHtml(70, 36 + (line++ * 20), 200, 20, Color(String.Format("{0} [{1}]", m.Location, m.Map), LabelColor32), false, false);

                AddButton(13, 157, 0xFAB, 0xFAD, 1, GumpButtonType.Reply, 0);
                AddHtml(48, 158, 200, 20, Color("Send Message", LabelColor32), false, false);

                AddImageTiled(12, 182, 376, 80, 0xA40);
                AddImageTiled(13, 183, 374, 78, 0xBBC);
                AddTextEntry(15, 183, 372, 78, 0x480, 0, "");

                AddImageTiled(245, 35, 142, 144, 5058);

                AddImageTiled(246, 36, 140, 142, 0xA40);
                AddAlphaRegion(246, 36, 140, 142);

                line = 0;

                AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 4, GumpButtonType.Reply, 0);
                AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Properties", LabelColor32), false, false);

                if (from != m)
                {
                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 5, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Go to them", LabelColor32), false, false);

                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 6, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Bring them here", LabelColor32), false, false);
                }

                AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 7, GumpButtonType.Reply, 0);
                AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Move to target", LabelColor32), false, false);

                if (from.AccessLevel >= AccessLevel.GameMaster && from.AccessLevel > m.AccessLevel)
                {
                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 8, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Disconnect", LabelColor32), false, false);

                    if (m.Alive)
                    {
                        AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 9, GumpButtonType.Reply, 0);
                        AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Kill", LabelColor32), false, false);
                    }
                    else
                    {
                        AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 10, GumpButtonType.Reply, 0);
                        AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Resurrect", LabelColor32), false, false);
                    }
                }

                if (from.AccessLevel >= AccessLevel.Counselor && from.AccessLevel > m.AccessLevel)
                {
                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 11, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Skills browser", LabelColor32), false, false);
                }
            }

#if false
			AddPage( 0 );

			AddBackground( 0, 0, 300, 370, 5054 );
			AddBackground( 8, 8, 284, 354, 3000 );

			AddLabel( 12, 12, 0, "Client Info" );

			int line = 0;

			AddLabel( 12, 38 + (line++ * 22), 0, String.Format( "Address: {0}", state ) );
			AddLabel( 12, 38 + (line++ * 22), 0, String.Format( "Version: {0}; Flags: 0x{1:X}", state.Version == null ? "(null)" : state.Version.ToString(), state.Flags ) );

			Account a = state.Account as Account;
			Mobile m = state.Mobile;

			if ( from.AccessLevel >= AccessLevel.GameMaster && a != null )
			{
				AddLabel( 12, 38 + (line++ * 22), 0, String.Format( "Account: {0}", a.Username ) );
			}

			if ( m != null )
			{
				AddLabel( 12, 38 + (line++ * 22), 0, String.Format( "Mobile: {0} (0x{1:X})", m.Name, m.Serial.Value ) );
				AddLabel( 12, 38 + (line++ * 22), 0, String.Format( "Location: {0} [{1}]", m.Location, m.Map ) );

				AddButton( 12, 38 + (line * 22), 0xFAB, 0xFAD, 1, GumpButtonType.Reply, 0 );
				AddImageTiled( 47, 38 + (line * 22), 234, 22, 0xA40 );
				AddImageTiled( 48, 38 + (line * 22) + 1, 232, 20, 0xBBC );
				AddTextEntry( 48, 38 + (line++ * 22) + 1, 232, 20, 0, 0, initialText );
			}

			/*if ( from.AccessLevel >= AccessLevel.Administrator && a != null && from != m )
			{
				AddButton( 12, 38 + (line * 22), 0xFA2, 0xFA4, 2, GumpButtonType.Reply, 0 );
				AddLabel( 48, 38 + (line++ * 22), 0, "Ban" );
			}*/

			/*if ( from.AccessLevel >= AccessLevel.Administrator && from != m )
			{
				AddButton( 12, 38 + (line * 22), 0xFA2, 0xFA4, 3, GumpButtonType.Reply, 0 );
				AddLabel( 48, 38 + (line++ * 22), 0, "Firewall" );
			}*/

			if ( m != null )
			{
				AddButton( 12, 38 + (line * 22), 0xFB7, 0xFB9, 4, GumpButtonType.Reply, 0 );
				AddLabel( 48, 38 + (line++ * 22), 0, "Props" );

				if ( from != m )
				{
					AddButton( 12, 38 + (line * 22), 0xFA5, 0xFA7, 5, GumpButtonType.Reply, 0 );
					AddLabel( 48, 38 + (line++ * 22), 0, "Goto" );

					AddButton( 12, 38 + (line * 22), 0xFAE, 0xFB0, 6, GumpButtonType.Reply, 0 );
					AddLabel( 48, 38 + (line++ * 22), 0, "Get" );
				}

				AddButton( 12, 38 + (line * 22), 0xFA8, 0xFAA, 7, GumpButtonType.Reply, 0 );
				AddLabel( 48, 38 + (line++ * 22), 0, "Move" );

				if ( from.AccessLevel >= AccessLevel.GameMaster && from.AccessLevel > m.AccessLevel )
				{
					AddButton( 12, 38 + (line * 22), 0xFA2, 0xFA4, 8, GumpButtonType.Reply, 0 );
					AddLabel( 48, 38 + (line++ * 22), 0, "Kick" );

					AddButton( 12, 38 + (line * 22), 0xFB7, 0xFB9, 9, GumpButtonType.Reply, 0 );
					AddLabel( 48, 38 + (line++ * 22), 0, "Kill" );

					AddButton( 12, 38 + (line * 22), 0xFB7, 0xFB9, 10, GumpButtonType.Reply, 0 );
					AddLabel( 48, 38 + (line++ * 22), 0, "Resurrect" );

					AddButton( 12, 38 + (line * 22), 0xFB7, 0xFB9, 11, GumpButtonType.Reply, 0 );
					AddLabel( 48, 38 + (line++ * 22), 0, "Player's Skills" );
				}
			}
#endif
        }
    }
}