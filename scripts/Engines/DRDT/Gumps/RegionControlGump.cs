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

/* Scripts/Engines/DRDT/Gumps/RegionControlGump.cs
 *  9/19/22, Yoar
 *      Added "Go To Control" button (CustomRegion only)
 *  9/13/22, Yoar
 *      Removed calls to CloseGump so that we may view multiple region controllers
 *      at the same time
 *  9/11/22, Yoar (Custom Region Overhaul)
 *      Completely overhauled custom region system
 *  05/02/05, Kit
 *	 Added Inn Support and Gumps.
 *  04/30/05, Kitaras
 *	 Added gump to enter regions via x/y 
 */

using Server.Network;
using Server.Regions;
using System;

namespace Server.Gumps
{
    public class RegionControlGump : Gump
    {
        private enum Buttons : byte
        {
            Close,
            EditSpells,
            EditSkills,
            EditArea,
            EditInns,
            ViewProps,
            GoToControl,
        }

        private StaticRegion m_Region;

        public RegionControlGump(StaticRegion sr)
            : base(50, 50)
        {
            m_Region = sr;

            AddBackground(0, 0, 590, 300, 9270);
            AddAlphaRegion(0, 0, 590, 300);

            AddHtml(30, 20, 530, 20, string.Format("<BASEFONT COLOR=#FFFFFF><CENTER>Region Control of {0}</CENTER></BASEFONT>", m_Region.Name), false, false);

            AddButton(35, 55, 5569, 5570, (int)Buttons.EditSpells, GumpButtonType.Reply, 0);
            AddLabel(120, 75, 1152, "Edit Restricted Spells");

            AddButton(320, 55, 5581, 5582, (int)Buttons.EditSkills, GumpButtonType.Reply, 0);
            AddLabel(405, 75, 1152, "Edit Restricted Skills");

            AddButton(30, 130, 7006, 7006, (int)Buttons.EditArea, GumpButtonType.Reply, 0);
            AddLabel(120, 155, 1152, "Edit Area");

            AddButton(315, 130, 7006, 7006, (int)Buttons.EditInns, GumpButtonType.Reply, 0);
            AddLabel(405, 155, 1152, "Edit Inns");

            AddButton(30, 210, 7020, 7020, (int)Buttons.ViewProps, GumpButtonType.Reply, 0);
            AddLabel(120, 235, 1152, "View Props");

            if (m_Region is CustomRegion)
            {
                AddButton(315, 210, 7021, 7021, (int)Buttons.GoToControl, GumpButtonType.Reply, 0);
                AddLabel(405, 235, 1152, "Go To Control");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            switch ((Buttons)info.ButtonID)
            {
                case Buttons.EditSpells:
                    {
                        from.SendGump(new RegionControlGump(m_Region));
                        from.SendGump(new EditRestrictedSpellsGump(m_Region.RestrictedSpells));
                        break;
                    }
                case Buttons.EditSkills:
                    {
                        from.SendGump(new RegionControlGump(m_Region));
                        from.SendGump(new EditRestrictedSkillsGump(m_Region.RestrictedSkills));
                        break;
                    }
                case Buttons.EditArea:
                    {
                        from.SendGump(new RegionControlGump(m_Region));
                        from.SendGump(new EditAreaGump(m_Region, false));
                        break;
                    }
                case Buttons.EditInns:
                    {
                        from.SendGump(new RegionControlGump(m_Region));
                        from.SendGump(new EditAreaGump(m_Region, true));
                        break;
                    }
                case Buttons.ViewProps:
                    {
                        from.SendGump(new RegionControlGump(m_Region));
                        from.SendGump(new PropertiesGump(from, m_Region));
                        break;
                    }
                case Buttons.GoToControl:
                    {
                        if (m_Region is CustomRegion)
                        {
                            CustomRegion cr = (CustomRegion)m_Region;

                            from.MoveToWorld(cr.Controller.GetWorldLocation(), cr.Controller.Map);
                        }

                        from.SendGump(new RegionControlGump(m_Region));
                        break;
                    }
            }
        }
    }
}