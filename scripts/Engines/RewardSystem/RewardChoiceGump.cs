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

/* Scripts/Engines/Reward System/RewardChoiceGump.cs
 * Created 5/23/04 by mith
 * ChangeLog
 */

using Server.Gumps;
using Server.Network;
using System;
using System.Collections;

namespace Server.Engines.RewardSystem
{
    public class RewardChoiceGump : Gump
    {
        private Mobile m_From;

        private void RenderBackground()
        {
            AddPage(0);

            AddBackground(10, 10, 600, 450, 2600);

            AddButton(530, 415, 4017, 4019, 0, GumpButtonType.Reply, 0);
        }

        private int GetButtonID(int type, int index)
        {
            return 2 + (index * 20) + type;
        }

        private void RenderCategory(RewardCategory category, int index)
        {
            AddPage(1);

            ArrayList entries = category.Entries;

            for (int i = 0; i < entries.Count; ++i)
            {
                RewardEntry entry = (RewardEntry)entries[i];

                AddButton(55 + ((i / 4) * 250), 80 + ((i % 4) * 50), 5540, 5541, GetButtonID(index, i), GumpButtonType.Reply, 0);

                if (entry.NameString != null)
                    AddHtml(80 + ((i / 4) * 250), 80 + ((i % 4) * 50), 250, 20, entry.NameString, false, false);
                else
                    AddHtmlLocalized(80 + ((i / 4) * 250), 80 + ((i % 4) * 50), 250, 20, entry.Name, false, false);

                DrawItem(entry, 250 + ((i / 4) * 250), 50 + ((i % 4) * 50));
            }
        }

        public void DrawItem(RewardEntry entry, int x, int y)
        {
            if (entry != null)
            {
                Item item = Activator.CreateInstance(entry.ItemType, entry.Args) as Item;

                AddItem(x, y, item.ItemID);
                item.Delete();
            }
        }

        public RewardChoiceGump(Mobile from, string categoryName)
            : base(0, 0)
        {
            m_From = from;

            from.CloseGump(typeof(RewardChoiceGump));

            RenderBackground();

            RewardCategory[] categories = RewardSystem.Categories;

            for (int i = 0; i < categories.Length; ++i)
            {
                if (categories[i].Entries.Count == 0)
                    continue;

                if (!RewardSystem.HasAccess(m_From, (RewardCategory)categories[i]))
                    continue;

                if (categories[i].XmlString == categoryName)
                    RenderCategory(categories[i], i);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            int buttonID = info.ButtonID - 1;

            if (buttonID == 0)
            {
                m_From.CloseGump(typeof(RewardChoiceGump));
            }
            else
            {
                --buttonID;

                int type = (buttonID % 20);
                int index = (buttonID / 20);

                RewardCategory[] categories = RewardSystem.Categories;

                if (type >= 0 && type < categories.Length)
                {
                    RewardCategory category = categories[type];

                    if (index >= 0 && index < category.Entries.Count)
                    {
                        RewardEntry entry = (RewardEntry)category.Entries[index];

                        //						if ( !RewardSystem.HasAccess( m_From, entry ) )
                        //							return;

                        m_From.SendGump(new RewardConfirmGump(m_From, entry));
                    }
                }
            }
        }
    }
}