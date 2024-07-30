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

/* Scripts/Multis/Tent/TentPackGump.cs
 * ChangeLog
 *	8/03/06, weaver
 *		Fixed minor spelling + wording errors.
 *	5/23/06, weaver
 *		Added code to handle Siege Tents.
 *	1/05/06, weaver
 *		Initial creation.
 */

using Server.Items;
using Server.Multis;
using Server.Multis.Deeds;
using Server.Network;
using System;
using System.Collections;

namespace Server.Gumps
{
    public class TentPackGump : Gump
    {
        private Mobile m_Mobile;
        private BaseHouse m_House;

        public TentPackGump(Mobile mobile, BaseHouse house)
            : base(110, 100)
        {
            m_Mobile = mobile;
            m_House = house;

            mobile.CloseGump(typeof(TentPackGump));

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, 420, 280, 5054);

            AddImageTiled(10, 10, 400, 20, 2624);
            AddAlphaRegion(10, 10, 400, 20);

            AddHtmlLocalized(10, 10, 400, 20, 1060635, 30720, false, false); // <CENTER>WARNING</CENTER>

            AddImageTiled(10, 40, 400, 200, 2624);
            AddAlphaRegion(10, 40, 400, 200);
            AddHtml(10, 40, 400, 200, string.Format("You are about to pack up your tent.\nYou will place the tent back in its tent bag.\nThe tent will remain behind and can be freely picked up by anyone.\nOnce the tent has been put away, anyone can attempt to place a new house or tent on the vacant land.\n	Are you sure you wish to continue?"), false, true);

            AddImageTiled(10, 250, 400, 20, 2624);
            AddAlphaRegion(10, 250, 400, 20);

            AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

            AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 1 && !m_House.Deleted)
            {
                if (m_House.IsOwner(m_Mobile))
                {
                    if (m_House.FindGuildstone() != null)
                    {
                        m_Mobile.SendLocalizedMessage(501389); // You cannot redeed a house with a guildstone inside.
                        return;
                    }
                    else if (m_House.FindPlayerVendor() != null)
                    {
                        m_Mobile.SendLocalizedMessage(503236); // You need to collect your vendor's belongings before moving.
                        return;
                    }
                    else if (m_House.FindPlayer() != null)
                    {
                        m_Mobile.SendMessage("It is not safe to demolish this tent with someone still inside."); // You need to collect your vendor's belongings before moving.
                                                                                                                 //Tell staff that an exploit is in progress
                                                                                                                 //Server.Commands.CommandHandlers.BroadcastMessage( AccessLevel.Counselor, 
                                                                                                                 //0x482, 
                                                                                                                 //String.Format( "Exploit in progress at {0}. Stay hidden, Jail involved players, get acct name, ban.", m_House.Location.ToString() ) );
                        return;
                    }

                    Item toGive;

                    if (m_House is Tent)
                    {
                        toGive = (TentBag)((Tent)m_House).GetDeed();
                    }
                    else if (m_House is SiegeTent)
                    {
                        toGive = (SiegeTentBag)((SiegeTent)m_House).GetDeed();
                    }
                    else
                    {
                        Console.WriteLine("Invalid type detected");
                        return;
                    }


                    // Find the roof
                    IEnumerator ta_enum = m_House.Addons.GetEnumerator();

                    while (ta_enum.MoveNext())
                        if (ta_enum.Current is TentRoof)
                            break;

                    // Hue the tent bag to the roof hue
                    toGive.Hue = ((TentRoof)ta_enum.Current).Hue;
                    toGive.MoveToWorld(m_House.Location, m_Mobile.Map);

                    m_House.Delete();
                }
                else
                {
                    m_Mobile.SendMessage("Only the tent owner may do this.");
                }
            }
        }
    }
}