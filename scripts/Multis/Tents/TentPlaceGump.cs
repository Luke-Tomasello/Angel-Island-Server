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

/* Scripts/Multis/Tent/TentPlaceGump.cs
 * ChangeLog
 *	9/02/06, weaver
 *		Added bool to indicate whether or not the tent has been placed.
 *	8/02/06, weaver
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
    public class TentPlaceGump : Gump
    {
        private Mobile m_Mobile;
        private BaseHouse m_House;
        //private ArrayList m_toMove;
        //private Point3D m_Center;
        //private HouseDeed m_deed;

        public TentPlaceGump(Mobile mobile, BaseHouse house)
            : base(110, 100)
        {
            m_Mobile = mobile;
            m_House = house;

            mobile.CloseGump(typeof(TentPlaceGump));

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, 420, 280, 5054);

            AddImageTiled(10, 10, 400, 20, 2624);
            AddAlphaRegion(10, 10, 400, 20);

            AddHtmlLocalized(10, 10, 400, 20, 1060635, 30720, false, false); // <CENTER>WARNING</CENTER>

            AddImageTiled(10, 40, 400, 200, 2624);
            AddAlphaRegion(10, 40, 400, 200);
            AddHtml(10, 40, 400, 200, string.Format("You are about to place your tent here.\nAre you sure you wish to continue?"), false, true);

            AddImageTiled(10, 250, 400, 20, 2624);
            AddAlphaRegion(10, 250, 400, 20);

            AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

            AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL

            if (house is SiegeTent)
            {
                ((SiegeTent)house).TentPack.Placed = false;
            }
            else if (house is Tent)
            {
                ((Tent)house).TentPack.Placed = false;
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            // Just in case they packed it away with the gump up
            if (m_House.Deleted)
                return;

            if (info.ButtonID == 0)
            {
                // Placement cancelled

                // Re-pack the tent and place back in pack
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
                //toGive.MoveToWorld( m_House.Location, m_Mobile.Map );
                state.Mobile.AddToBackpack(toGive);

                m_House.Delete();
            }
            else
            {
                // Approved placement

                // wea: 2/Sep/2006 Set the placed bool to indicate that this is now a useable tent
                if (m_House is SiegeTent)
                {
                    ((SiegeTent)m_House).TentPack.Placed = true;
                }
                else if (m_House is Tent)
                {
                    ((Tent)m_House).TentPack.Placed = true;
                }
            }
        }
    }
}