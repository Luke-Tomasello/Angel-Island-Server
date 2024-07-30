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

/* Scripts/Engines/RareFactory/RFGroupGump.cs
 * ChangeLog:
 *	17/Mar/2007, weaver
 *		Moved group addition code to RFNewGroupGump.cs
 *	13/Mar/2007, weaver 
 *		Added range checks.
 *	28/Feb/2007, weaver
 *		Initial creation.
 * 
 */

using Server.Engines;
using Server.Network;
using Server.Prompts;


namespace Server.Gumps
{
    public class RFGroupGump : Gump
    {
        const int gx = 100;
        const int gy = 100;

        const int gw = 170;
        const int gh = 340;

        public int numPages;
        public int curPage;

        public RFGroupGump(int page)
            : base(gx, gy)
        {
            curPage = page;
            numPages = (int)(RareFactory.DODGroup.Count / 5);
            // Console.WriteLine("RFGroupGump() : numPages = {0}", numPages);

            Closable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(gx, gy, gw, gh, 9270);
            AddAlphaRegion(gx + 5, gy + 5, gw - 10, gh - 10);

            AddLabel(gx + 11, gy + 11, 1152, "Rare Factory - Groups");

            GenerateGroups();
        }

        public void GenerateGroups()
        {
            // Add group button always exists
            AddButton(gx + 63, gy + 300, 0xFA8, 0xFAA, 1, GumpButtonType.Reply, 0);

            // Next group
            AddButton(gx + 103, gy + 300, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0);

            // Prev group
            AddButton(gx + 23, gy + 300, 0xFAE, 0xFB0, 3, GumpButtonType.Reply, 0);


            if (RareFactory.DODGroup.Count == 0)
            {
                AddLabel(gx + 16, gy + 50, 1152, "No groups defined");
                return;
            }

            // On construction we work out the groups
            // available and create the buttons to handle 
            // them dynamically

            int startidx = curPage * 5;

            for (int i = startidx; (i < startidx + 5) && (i < RareFactory.DODGroup.Count); i++)
            {
                DODGroup dg = (DODGroup)RareFactory.DODGroup[i];

                int ypos = ((i - startidx) * 30) + 170;

                // View group
                AddButton(gx + 13, ypos, 0xFAB, 0xFAD, (i * 2) + 4, GumpButtonType.Reply, 0);
                // Del group
                AddButton(gx + 43, ypos, 0xFB1, 0xFB3, (i * 2) + 5, GumpButtonType.Reply, 0);
                // Group name
                AddLabel(gx + 78, ypos, 1152, dg.Name);
            }

        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            // Console.WriteLine("Group button {0} pressed", info.ButtonID);

            switch (info.ButtonID)
            {
                case 0:
                    {
                        return;
                    }
                case 1:
                    {
                        RareFactory.ReloadViews(from);
                        from.SendGump(new RFNewGroupGump("", 0));

                        /*
						from.SendMessage("Enter name for new rare group :");
						 * from.Prompt = new GroupNamePrompt(from);
						 */

                        return;

                    }
                case 2:
                    {
                        // Next Page
                        if (curPage == numPages)
                        {
                            from.SendMessage("You are already viewing the last page of DOD groups.");
                            break;
                        }

                        curPage++;

                        break;
                    }
                case 3:
                    {
                        // Prev Page
                        if (curPage == 0)
                        {
                            from.SendMessage("You are already viewing the first page of DOD groups.");
                            break;
                        }

                        curPage--;

                        break;
                    }
                default:
                    {
                        // Work out whether we're deleting or selecting and 
                        // act accordingly

                        // 1 - view, 2 - delete etc. (odd = view, even = delete)

                        if (info.ButtonID % 2 > 0)
                        {
                            // odd, so we're deleting
                            int grpNum = (((info.ButtonID + 1) / 2) - 3);
                            from.SendMessage(string.Format("Are you sure you wish to delete rare group '{0}' and all associated rares? (y/n)", ((DODGroup)RareFactory.DODGroup[grpNum]).Name));
                            from.Prompt = new GrpDelConfirmPrompt(from, (short)grpNum);
                            break;
                        }
                        else
                        {
                            // even, so we're viewing
                            int grpNum = ((info.ButtonID / 2) - 2);
                            RareFactory.ViewingDODGroupIndex = grpNum;
                        }

                        break;
                    }
            }
            RareFactory.ViewingDODGroupPage = curPage;
            RareFactory.ReloadViews(from);
        }


        private class GrpDelConfirmPrompt : Prompt
        {
            private Mobile m_from;
            private short m_idg;

            public GrpDelConfirmPrompt(Mobile from, short idg)
            {
                m_from = from;
                m_idg = idg;
            }

            public override void OnResponse(Mobile from, string text)
            {

                if (text.ToLower() == "y")
                {
                    // Delete all the DOD instances first
                    DODGroup dg = (DODGroup)RareFactory.DODGroup[m_idg];

                    for (int i = 0; i < dg.DODInst.Count; i++)
                    {
                        string sName = "";

                        if (dg.DODInst[i] is DODInstance)
                            sName = ((DODInstance)dg.DODInst[i]).Name;

                        from.SendMessage("Deleting rare '{0}'...", sName);

                        for (int inst = 0; inst < RareFactory.DODInst.Count; inst++)
                            if (((DODInstance)RareFactory.DODInst[inst]) == dg.DODInst[i])
                            {   // There should never be more than one of these right?
                                RareFactory.DODInst.RemoveAt(inst);
                                break;
                            }
                    }

                    from.SendMessage("Deleting group '{0}'", dg.Name);
                    RareFactory.DODGroup.RemoveAt(m_idg);

                    from.SendMessage("Rare group deletion complete.");
                    if (RareFactory.ViewingDODGroupIndex > 0)
                        RareFactory.ViewingDODGroupIndex -= 1;
                }
                else
                {
                    from.SendMessage("Rare group deletion cancelled.");
                }

                RareFactory.ReloadViews(from);

            }

        }


    }

}