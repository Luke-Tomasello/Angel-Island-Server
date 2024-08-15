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

/* Engines/Township/TownshipNPCGump.cs
 * CHANGELOG:
 *  1/12/2024, Adam (WanterRange)
 *      ITownshipNPCc now have a wander range, use this when adjusting the NPCc RangeHome
 *  9/3/2023, Adam (enable/disable wander)
 *      When disable wander was true, the NPC would just slowly spin in place.
 *      Fix: Set RangeHome = 0 so that the NPC does not try to move.
 *      Update: 12/28/2023: set Home to their current location.
 *      Conversely, when wander is enabled, RangeHome = 4 (best guess)
 * 2/19/22, Yoar
 *		Initial version.
*/

using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using static Server.Utility;

namespace Server.Township
{
    public class TownshipNPCGump : Gump
    {
        private Mobile m_NPC;
        private Items.TownshipStone m_Stone;

        public TownshipNPCGump(Mobile npc, Items.TownshipStone stone)
            : base(20, 30)
        {
            m_NPC = npc;
            m_Stone = stone;

            AddPage(0);

            int height = 210;
            AddBackground(0, 0, 270, height, 5054);
            AddBackground(10, 10, 250, height - 20, 3000);

            AddHtml(20, 15, 250, 20, string.Format("Manage {0} {1}", m_NPC.Name, m_NPC.Title), false, false);

            AddButtonLabeled(20, 50, 1, "Move");
            AddButtonLabeled(20, 80, 2, "Bring To Home");
            AddButtonLabeled(20, 110, 3, m_NPC.CantWalkLand ? "Enable Wander" : "Disable Wander");
            AddButtonLabeled(20, 140, 4, "Redeed");
            AddButtonLabeled(20, 170, 0, "Exit");
        }

        private void AddButtonLabeled(int x, int y, int buttonID, string text, int width = 250)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            AddHtml(x + 35, y, width - 35, 20, text, false, false);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (m_NPC.Deleted || !TownshipNPCHelper.IsOwner(m_NPC, from))
                return;

            switch (info.ButtonID)
            {
                case 0: // close
                    {
                        break;
                    }
                case 1: // move
                    {
                        from.Target = new MoveNPCTarget(m_NPC, m_Stone);
                        from.SendMessage("Where do you wish to move this vendor to?");

                        break;
                    }
                case 2:
                    {
                        BringToHome(from);
                        from.CloseGump(typeof(TownshipNPCGump));
                        from.SendGump(new TownshipNPCGump(m_NPC, m_Stone));
                        break;
                    }
                case 3: // enable/disable wander
                    {
                        m_NPC.CantWalkLand = !m_NPC.CantWalkLand;

                        if (m_NPC.CantWalkLand)
                        {
                            from.SendMessage("The vendor will now remain stationary.");
                            if (m_NPC is BaseCreature bc)
                            {
                                // prevents spinning in place
                                bc.RangeHome = 0;
                                bc.Home = bc.Location;
                            }
                        }
                        else
                        {
                            from.SendMessage("The vendor will now wander about.");
                            if (m_NPC is BaseCreature bc)
                                // not sure what this should be
                                bc.RangeHome = (bc is ITownshipNPC tsnpc) ? tsnpc.WanderRange : 10;
                        }

                        from.CloseGump(typeof(TownshipNPCGump));
                        from.SendGump(new TownshipNPCGump(m_NPC, m_Stone));

                        break;
                    }
                case 4: // redeed
                    {
                        from.CloseGump(typeof(ConfirmRedeedNPCGump));
                        from.CloseGump(typeof(ConfirmRedeedNPCWarningGump));
                        if (m_NPC.GetType() == typeof(TSTownCrier) && m_Stone.Extended)
                        {
                            from.SendGump(new ConfirmRedeedNPCWarningGump(m_NPC, m_Stone));
                        }
                        else
                        {
                            from.SendGump(new ConfirmRedeedNPCGump(m_NPC, m_Stone));
                        }
                        break;
                    }
            }
        }
        private void BringToHome(Mobile from)
        {
            // we will prefer the npcs home within the township. If that's no good, use the ts stone
            if (m_NPC is BaseCreature bc && m_Stone is Items.TownshipStone stone && stone.Map != null && stone.Map != Map.Internal && stone.Location != Point3D.Zero)
            {
                Point3D px = Regions.TownshipRegion.GetTownshipAt(bc.Home, bc.Map) != null ? bc.Home : Point3D.Zero;
                if (px == Point3D.Zero)
                {   // save/restore CantWalkLand to ensure GetSpawnPosition does the right thing (might think it's a water mob or something.)
                    bool walk = bc.CantWalkLand;
                    bc.CantWalkLand = false;
                    px = Spawner.GetSpawnPosition(stone.Map, stone.Location, homeRange: 4, SpawnFlags.None, o: bc);
                    bc.CantWalkLand = walk;
                }

                bc.MoveToWorld(px, stone.Map);
            }
        }
        private class MoveNPCTarget : Target
        {
            private Mobile m_NPC;
            private Items.TownshipStone m_Stone;

            public MoveNPCTarget(Mobile npc, Items.TownshipStone stone)
                : base(-1, true, TargetFlags.None)
            {
                m_NPC = npc;
                m_Stone = stone;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_NPC.Deleted || !TownshipNPCHelper.IsOwner(m_NPC, from))
                    return;

                Map map = from.Map;

                if (map == null || map == Map.Internal || map != m_NPC.Map || !(targeted is IPoint3D))
                {
                    from.SendMessage("Invalid target.");
                }
                else
                {
                    Point3D loc = new Point3D((IPoint3D)targeted);

                    TownshipNPCHelper.PlaceNPCResult result = TownshipNPCHelper.CanPlaceNPC(from, m_NPC.GetType(), loc, from.Map);

                    if (result != TownshipNPCHelper.PlaceNPCResult.Success)
                    {
                        from.SendMessage(TownshipNPCHelper.GetMessage(result));
                    }
                    else
                    {
                        if (m_NPC is BaseCreature bc)
                        {
                            bc.Home = loc;
                            if (bc.CantWalkLand == true)    // prevents 'spinning'
                                bc.RangeHome = 0;
                            else
                                bc.RangeHome = (bc is ITownshipNPC tsnpc) ? tsnpc.WanderRange : 10;
                        }

                        m_NPC.MoveToWorld(loc, map);
                    }
                }

                from.CloseGump(typeof(TownshipNPCGump));
                from.SendGump(new TownshipNPCGump(m_NPC, m_Stone));
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (m_NPC.Deleted || !TownshipNPCHelper.IsOwner(m_NPC, from))
                    return;

                from.CloseGump(typeof(TownshipNPCGump));
                from.SendGump(new TownshipNPCGump(m_NPC, m_Stone));
            }
        }
    }
}