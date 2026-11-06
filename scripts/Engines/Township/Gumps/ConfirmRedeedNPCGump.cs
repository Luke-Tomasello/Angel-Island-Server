/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Engines/Township/ConfirmDismissNPCGump.cs
 * CHANGELOG:
 *  3/12/2024, Adam (town crier warning)
 *      Warn players of the possible consequences of dismissing/redeeding their town crier NPC.
 *  2/4/2024, Adam
 *      DefragTownshipNPCs() after redeeding the NPC to avoid bogus charges.
 *  1/30/22, Adam
 *		Initial version.
*/

using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using System;
using static Server.Utility;

namespace Server.Township
{
    public class ConfirmRedeedNPCGump : Gump
    {
        private Mobile m_NPC;
        private Items.TownshipStone m_Stone;

        public ConfirmRedeedNPCGump(Mobile npc)
            : this(npc, null)
        {
        }

        public ConfirmRedeedNPCGump(Mobile npc, Items.TownshipStone stone)
            : base(50, 50)
        {
            m_NPC = npc;
            m_Stone = stone;

            AddPage(0);

            AddBackground(10, 10, 190, 140, 0x242C);

            AddHtml(25, 30, 160, 80, string.Format("<center>Are you sure you wish to redeed {0} {1}?</center>", m_NPC.Name, m_NPC.Title), false, false);

            AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
            AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (m_NPC.Deleted || !Mobiles.TownshipNPCHelper.IsOwner(m_NPC, from))
                return;

            switch (info.ButtonID)
            {
                case 0x0: // close
                case 0x2: // cancel
                    {
                        from.CloseGump(typeof(TownshipNPCGump));
                        from.SendGump(new TownshipNPCGump(m_NPC, m_Stone));

                        break;
                    }
                case 0x1: // okay
                    {
                        Type type = m_NPC.GetType();
                        /*
                         * We create a temporary clone of the Township NPC so we can preserve
                         * its customized appearance and equipment across redeeding.
                         *
                         * Players frequently personalize Township NPCs with clothing, weapons,
                         * lanterns, dyes, and other decorative items to create a unique look.
                         * Recreating the NPC later from only its base type would lose all of
                         * that visual customization.
                         *
                         * The clone acts as a serialized snapshot of the NPC at the moment the
                         * deed is created, allowing the restoration system to faithfully restore
                         * the NPC's appearance and inventory later.
                         */
                        #region Redeeding NPC
                        Mobile? doppelganger = MakeNPC(from, type);
                        CloneNPC(doppelganger, m_NPC);
                        TownshipNPCDeed? deed = GetDeed(from, type);
                        deed.RestorationMobile = doppelganger.Serial;
                        if (deed != null)
                        {
                            from.AddToBackpack(deed);
                            m_Stone.RegisterStashedNPC(doppelganger);
                        }

                        #endregion Redeeding NPC

                        m_NPC.Delete();

                        from.SendMessage("The vendor deed has been placed in your backpack.");
                        m_Stone.DefragTownshipNPCs();
                        m_Stone.CheckNPCRequirements(type);

                        break;
                    }
            }
        }
        #region Tools
        public static Mobile? MakeNPC(Mobile from, Type type)
        {
            BaseCreature npc = TownshipNPCHelper.Construct<BaseCreature>(type);

            if (npc == null)
                return null;

            npc.Home = from.Location;
            npc.RangeHome = 3;                      // will this ever be a non ITownshipNPC?
            npc.Guild = from.Guild;
            npc.DisplayGuildTitle = true;

            if (npc is ITownshipNPC tsNpc)
            {
                npc.RangeHome = tsNpc.WanderRange;  // handled correctly here
                tsNpc.Owner = from;
            }

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(from.Location, from.Map);

            npc.MoveToIntStorage();

            return npc;
        }
        public static void CloneNPC(Mobile dest, Mobile src)
        {
            // copy the clothes, jewelry, everything
            Utility.CopyLayers(dest, src, CopyLayerFlags.Default);

            // now everything else
            dest.Name = src.Name;
            dest.Hue = src.Hue;
            dest.Body = (src.Female) ? 401 : 400;   // get the correct body
            dest.Female = src.Female;               // get the correct death sound
            dest.Direction = src.Direction;         // face them the correct way
            dest.MoveToIntStorage();
        }
        public static TownshipNPCDeed? GetDeed(Mobile from, Type type)
        {

            Type? deedType = TownshipNPCHelper.GetDeedType(type);

            if (deedType == null)
                return null;

            TownshipNPCDeed deed = TownshipNPCHelper.Construct<TownshipNPCDeed>(deedType);

            if (deed == null)
                return null;

            deed.Guild = from.Guild as Server.Guilds.Guild;

            return deed;
        }
        #endregion Tools
    }
}