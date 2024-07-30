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

/* Scripts\Engines\Plants\SetToDecorativeGump.cs
 * CHANGELOG:
 *  3/24/22, Adam
 *      Add call to m_Plant.OnAfterDecorative() after a plant is made Decorative.
 */

using Server.Gumps;
using Server.Network;

namespace Server.Engines.Plants
{
    public class SetToDecorativeGump : Gump
    {
        private PlantItem m_Plant;

        public SetToDecorativeGump(PlantItem plant)
            : base(20, 20)
        {
            m_Plant = plant;

            DrawBackground();

            AddLabel(115, 85, 0x44, "Set plant");
            AddLabel(82, 105, 0x44, "to decorative mode?");

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

            if (info.ButtonID == 0 || m_Plant.Deleted || m_Plant.PlantStatus != PlantStatus.Stage9 || !from.InRange(m_Plant.GetWorldLocation(), 3))
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
                        from.SendGump(new ReproductionGump(m_Plant));

                        break;
                    }
                case 2: // Help
                    {
                        from.Send(new DisplayHelpTopic(70, true)); // DECORATIVE MODE

                        from.SendGump(new SetToDecorativeGump(m_Plant));

                        break;
                    }
                case 3: // Ok
                    {
                        m_Plant.PlantStatus = PlantStatus.DecorativePlant;
                        m_Plant.LabelTo(from, 1053077); // You prune the plant. This plant will no longer produce resources or seeds, but will require no upkeep.
                        m_Plant.OnAfterDecorative();
                        break;
                    }
            }
        }
    }
}