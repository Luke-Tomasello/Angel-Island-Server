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

/* Scripts\Gumps\MiniBossArmorBlessGump.cs
 * ChangeLog
 *	9/4/21, adam
 *		Initial creation.
 */

using Server.Items;
using Server.Network;

namespace Server.Gumps
{
    public class MiniBossArmorBlessGump : Gump
    {
        private Mobile m_Mobile;
        private MiniBossArmorBlessDeed m_deed;

        public MiniBossArmorBlessGump(Mobile mobile, MiniBossArmorBlessDeed deed)
            : base(110, 100)
        {
            m_Mobile = mobile;
            m_deed = deed;

            mobile.CloseGump(typeof(MiniBossArmorBlessGump));

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, 420, 280, 5054);

            AddImageTiled(10, 10, 400, 20, 2624);
            AddAlphaRegion(10, 10, 400, 20);

            AddHtmlLocalized(10, 10, 400, 20, 1060635, 30720, false, false); // <CENTER>WARNING</CENTER>

            AddImageTiled(10, 40, 400, 200, 2624);
            AddAlphaRegion(10, 40, 400, 200);
            AddHtml(10, 40, 400, 200, string.Format("Your armor will lose all special attributes except hue and durability.\nAre you sure you wish to continue?"), false, true);

            AddImageTiled(10, 250, 400, 20, 2624);
            AddAlphaRegion(10, 250, 400, 20);

            AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

            AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL

        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            // Just in case they packed it away with the gump up
            if (m_deed.Deleted)
                return;

            if (info.ButtonID == 0)
            {
                // bless cancelled
            }
            else
            {
                // Approved bless

                m_Mobile.SendMessage("What would you like to bless?");
                m_Mobile.Target = new MiniBossArmorBlessTarget(m_deed); // Call our target
            }
        }
    }
}