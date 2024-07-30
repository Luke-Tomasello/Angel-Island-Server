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

/* Items/Skill Items/Camping/Bedroll.cs
 * CHANGELOG:
 *	2/22/11, Adam
 *		o Update to RunUO 2.0
 *		o Check for invalid 'area' (like ransom chest areas)
 *  03/27/07, plasma
 *      - Fixed bedroll from disappearing if used within any container other than the backpack.
 *	08/14/06, weaver
 *		- Fixed so bedroll doesn't disappear when accessed via a nested container (modified to 
 *		perform iterative check on backpack).
 *		- Added sanity message for players so they know bedroll must be used from the ground.
 *	10/22/04 - Pix
 *		Changed camping to not use campfireregion.
 *	9/11/04, Pixie
 *		Updates Campfire's new OwnerUsedBedroll so the only people using bedrolls logout instantly.
 *	5/10/04, Pixie
 *		Initial working revision
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Items
{
    public class Bedroll : Item
    {
        [Constructable]
        public Bedroll()
            : base(0xA57)
        {
            Weight = 5.0;
        }

        public Bedroll(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (this.Parent != null || !this.VerifyMove(from))
                return;

            if (!from.InRange(this, 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            //adam : if within a rectricted camping zone then don't allow camping, i.e., a ransom chest rect
            if (CampHelper.InRestrictedArea(from))
            {
                from.SendMessage("You do not consider it safe to secure a camp here");
                return;
            }

            if (this.ItemID == 0xA57) // rolled
            {
                Direction dir = PlayerMobile.GetDirection4(from.Location, this.Location);

                if (dir == Direction.North || dir == Direction.South)
                    this.ItemID = 0xA55;
                else
                    this.ItemID = 0xA56;
            }
            else // unrolled
            {
                this.ItemID = 0xA57;

                if (!from.HasGump(typeof(LogoutGump)))
                {
                    CampfireEntry entry = Campfire.GetEntry(from);

                    if (entry != null && entry.Safe)
                        from.SendGump(new LogoutGump(entry, this));
                }
            }
        }

        private class LogoutGump : Gump
        {
            private Timer m_CloseTimer;

            private CampfireEntry m_Entry;
            private Bedroll m_Bedroll;

            public LogoutGump(CampfireEntry entry, Bedroll bedroll)
                : base(100, 0)
            {
                m_Entry = entry;
                m_Bedroll = bedroll;

                m_CloseTimer = Timer.DelayCall(TimeSpan.FromSeconds(10.0), new TimerCallback(CloseGump));

                AddBackground(0, 0, 400, 350, 0xA28);

                AddHtmlLocalized(100, 20, 200, 35, 1011015, false, false); // <center>Logging out via camping</center>

                /* Using a bedroll in the safety of a camp will log you out of the game safely.
				 * If this is what you wish to do choose CONTINUE and you will be logged out.
				 * Otherwise, select the CANCEL button to avoid logging out at this time.
				 * The camp will remain secure for 10 seconds at which time this window will close
				 * and you not be logged out.
				 */
                AddHtmlLocalized(50, 55, 300, 140, 1011016, true, true);

                AddButton(45, 298, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(80, 300, 110, 35, 1011011, false, false); // CONTINUE

                AddButton(200, 298, 0xFA5, 0xFA7, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(235, 300, 110, 35, 1011012, false, false); // CANCEL
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                PlayerMobile pm = m_Entry.Player;

                m_CloseTimer.Stop();

                if (Campfire.GetEntry(pm) != m_Entry)
                    return;

                if (info.ButtonID == 1 && m_Entry.Safe && m_Bedroll.Parent == null && m_Bedroll.IsAccessibleTo(pm)
                    && m_Bedroll.VerifyMove(pm) && m_Bedroll.Map == pm.Map && pm.InRange(m_Bedroll, 2))
                {
                    pm.PlaceInBackpack(m_Bedroll);

                    pm.BedrollLogout = true;
                    sender.Dispose();
                }

                Campfire.RemoveEntry(m_Entry);
            }

            private void CloseGump()
            {
                Campfire.RemoveEntry(m_Entry);
                m_Entry.Player.CloseGump(typeof(LogoutGump));
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}