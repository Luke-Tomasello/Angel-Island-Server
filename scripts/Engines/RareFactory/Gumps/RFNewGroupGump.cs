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

/* Scripts/Engines/RareFactory/RFNewGroupGump.cs
 * ChangeLog:
 *	17/Mar/2007, weaver 
 *		Initial creation.
 * 
 */

using Server.Engines;
using Server.Network;
using Server.Prompts;
using System.Text.RegularExpressions;


namespace Server.Gumps
{
    public class RFNewGroupGump : Gump
    {
        const int gx = 100;
        const int gy = 100;

        const int gw = 260;
        const int gh = 200;

        const int entrystarty = 155;
        const int entryspacery = 45;

        // Temporary vars to hold the "current" values
        // selected via this gump
        private string m_GroupName = "";
        public string GroupName
        {
            get
            {
                return m_GroupName;
            }

            set
            {
                m_GroupName = value;
            }
        }

        private short m_RarityLevel = 0;

        public RFNewGroupGump(string grpname, short raritylevel)
            : base(gx, gy)
        {
            m_GroupName = grpname;
            m_RarityLevel = raritylevel;

            Closable = true;
            Dragable = true;
            Resizable = false;


            AddPage(0);
            AddBackground(gx, gy, gw, gh, 9270);
            AddAlphaRegion(gx + 5, gy + 5, gw - 10, gh - 10);

            // Window title
            AddLabel(gx + 11, gy + 11, 1152, "Rare Factory - New Group");

            // Group name
            AddLabel(gx + 25, entrystarty, 1152, "Group name : ");
            AddLabel(gx + 130, entrystarty, 1152, (m_GroupName != "" ? m_GroupName : "* undefined *"));

            // Choose name
            AddButton(gx + 230, entrystarty, 0x4B9, 0x4BA, 5, GumpButtonType.Reply, 0);

            // Rarity level
            AddLabel(gx + 25, entrystarty + (entryspacery * 1), 1152, "Rarity level : ");
            AddLabel(gx + 130, entrystarty + (entryspacery * 1), 1152, (m_RarityLevel != 0 ? m_RarityLevel.ToString() : "0"));

            // Raise rarity
            AddButton(gx + 165, entrystarty + (entryspacery * 1) - (entryspacery / 4), 0xFB, 0xFB, 1, GumpButtonType.Reply, 0);

            // Lower rarity
            AddButton(gx + 165, entrystarty + (entryspacery * 1) + (entryspacery / 4), 0xFC, 0xFC, 2, GumpButtonType.Reply, 0);

            // OK
            AddButton(gx + (gw / 2 - (gw / 8)) - (gw / 16), entrystarty + (entryspacery * 2) + 10, 0x481, 0x482, 3, GumpButtonType.Reply, 0);

            // Cancel
            AddButton(gx + (gw / 2 + (gw / 8)) - (gw / 16), entrystarty + (entryspacery * 2) + 10, 0x47E, 0x47F, 4, GumpButtonType.Reply, 0);

        }

        // Refresh the gump display
        public void Refresh(Mobile from)
        {
            from.CloseGump(typeof(RFNewGroupGump));
            from.SendGump(new RFNewGroupGump(m_GroupName, m_RarityLevel));
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            switch (info.ButtonID)
            {
                case 0:
                    {
                        // Close this gump
                        return;
                    }
                case 1:
                    {
                        // Raise rarity

                        if (m_RarityLevel >= 10)
                        {
                            from.SendMessage("Maximum rarity level is 10 (most unique)");
                            break;
                        }

                        m_RarityLevel++;

                        break;
                    }
                case 2:
                    {
                        // Lower rarity

                        if (m_RarityLevel == 0)
                        {
                            from.SendMessage("Minimum rarity level is 0 (most common)");
                            break;
                        }

                        m_RarityLevel--;

                        break;
                    }
                case 3:
                    {
                        // Make sure they've specified a name :P
                        if (m_GroupName == "")
                        {
                            from.SendMessage("You must specify a group name to identify this group by!");
                            break;
                        }

                        // Now make sure that the name doesn't already exist :)
                        if (RareFactory.DODGroup.Count > 0)
                        {
                            for (int i = 0; i < RareFactory.DODGroup.Count; i++)
                            {
                                DODGroup dg = (DODGroup)RareFactory.DODGroup[i];
                                if (dg.Name == m_GroupName)
                                {
                                    // Don't bother with the rest of this code - they messed up!
                                    from.SendMessage("DOD Group names must be unique - the name you have chosen already exists! Please try again.");
                                    break;
                                }
                            }
                        }

                        // Accept + create
                        RareFactory.DODGroup.Add(new DODGroup(m_GroupName, m_RarityLevel));

                        // Update their display 
                        RareFactory.ReloadViews(from);

                        return;
                    }
                case 4:
                    {
                        // Cancel the whole thing
                        return;
                    }
                case 5:
                    {
                        // Set the group name
                        from.SendMessage("Enter the name you wish to use to represent this group of rares :");
                        from.Prompt = new GroupNamePrompt(from, this);
                        return;
                    }
                default:
                    {
                        break;
                    }
            }

            Refresh(from);
        }

        private class GroupNamePrompt : Prompt
        {
            private Mobile m_from;
            private RFNewGroupGump m_groupgump;

            public GroupNamePrompt(Mobile from, RFNewGroupGump grpgump)
            {
                m_from = from;
                m_groupgump = grpgump;
            }

            public override void OnResponse(Mobile from, string text)
            {
                // Pattern match for invalid characters
                Regex InvalidPatt = new Regex("[^-a-zA-Z0-9']");

                if (InvalidPatt.IsMatch(text) || text.Length < 1 || text.Length > 15)
                {
                    // Invalid chars
                    from.SendMessage("You may only use numbers, letters, apostrophes and hyphens (1-15 chars, no spaces).");
                    from.SendMessage("Please re-enter an allowed name :");
                    from.Prompt = new GroupNamePrompt(from, m_groupgump);
                }
                else
                {
                    if (m_groupgump == null)        // it is a possibility!
                        return;

                    m_groupgump.GroupName = text;
                    m_groupgump.Refresh(from);

                }

            }

        }

    }

}