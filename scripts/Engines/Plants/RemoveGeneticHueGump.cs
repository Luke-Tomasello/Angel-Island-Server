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

/* Scripts\Engines\Plants\RemoveGeneticHueGump.cs
 * CHANGELOG:
 *  3/28/22, Adam
 *      When confirming 'plant bleaching', check for either of normal plant access, or township 'lock down' access.
 *  3/27/22, Adam
 *      first time check in
 */

using Server.Gumps;
using Server.Network;

namespace Server.Engines.Plants
{
    public class RemoveGeneticHueGump : Gump
    {
        private PlantItem m_Plant;

        public RemoveGeneticHueGump(PlantItem plant)
            : base(20, 20)
        {
            m_Plant = plant;

            DrawBackground();

            AddLabel(115, 85, 0x44, "Bleach plant's");
            //AddLabel(82, 105, 0x44, "genetic color?");
            AddLabel(115, 105, 0x44, "genetic color?");

            AddButton(98, 140, 0x47E, 0x480, 1, GumpButtonType.Reply, 0); // Cancel

            AddButton(138, 141, 0xD2, 0xD2, 2, GumpButtonType.Reply, 0); // Help
            AddLabel(143, 141, 0x835, "?");

            AddButton(168, 140, 0x481, 0x483, 3, GumpButtonType.Reply, 0); // Ok
        }

        private void DrawBackground()
        {
            AddBackground(50, 50, 200, 150, 0xE10);

            AddItem(25, 45, 0xCEB);
            AddItem(25, 118, 0xCEC);

            AddItem(227, 45, 0xCEF);
            AddItem(227, 118, 0xCF0);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (info.ButtonID == 0 || m_Plant.Deleted || m_Plant.PlantStatus == PlantStatus.DeadTwigs || !from.InRange(m_Plant.GetWorldLocation(), 3))
                return;

            if (!m_Plant.IsUsableBy(from))
            {
                m_Plant.LabelTo(from, 1061856); // You must have the item in your backpack or locked down in order to use it.
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Cancel
                    {
                        from.SendMessage("You decide not to bleach this plant.");
                        break;
                    }
                case 2: // Help
                    {
                        from.SendMessage("You are about to remove all genetic color from this plant.");
                        from.SendGump(new RemoveGeneticHueGump(m_Plant));

                        break;
                    }
                case 3: // Ok
                    {
                        m_Plant.PlantHue = PlantHue.Plain;
                        from.PlaySound(0x4E);
                        from.SendMessage("All color has been bleached from this plant.");
                        break;
                    }
            }
        }
    }
}