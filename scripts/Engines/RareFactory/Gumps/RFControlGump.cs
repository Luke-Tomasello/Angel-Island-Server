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

/* Scripts/Engines/RareFactory/RFControlGump.cs
 * ChangeLog:
 *  5/29/07, Adam
 *      Add exception logging
 *	13/Mar/2007, weaver
 *		- Modified creation process to utilise new interfaces, added to provide
 *		additional functionality.
 *		- Added call to new field DODInstance.Expire() to centrally 
 *		manage rare deletion
 *	12/Mar/2007, weaver
 *		Added shallow copy of item.
 *	12/Mar/2007, weaver
 *		Initial creation.
 * 
 */

using Server.Diagnostics;
using Server.Engines;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
using System;
using System.Text.RegularExpressions;

namespace Server.Gumps
{
    public class RFControlGump : Gump
    {
        const int gx = 180;
        const int gy = 280;

        const int gw = 350;
        const int gh = 100;

        // Buttons here are all fixed, so we can define thus
        private enum RFButtons
        {
            AddRareButton = 1,
            DelRareButton,
            NextRareButton,
            PrevRareButton,
            TestButton
        }

        public RFControlGump()
            : base(gx, gy)
        {
            Closable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(gx, gy, gw, gh, 9270);
            AddAlphaRegion(gx + 5, gy + 5, gw - 10, gh - 10);

            AddLabel(gx + 11, gy + 11, 1152, "Rare Factory - Control");

            AddButton(gx + 153, gy + 35, 0x99C, 0x99D, (int)RFButtons.AddRareButton, GumpButtonType.Reply, 0);   // add 
            AddButton(gx + 145, gy + 55, 0x99F, 0x9A0, (int)RFButtons.DelRareButton, GumpButtonType.Reply, 0);   // delete
            AddButton(gx + 15, gy + 25, 0x119D, 0x119D, (int)RFButtons.PrevRareButton, GumpButtonType.Reply, 0);   // <--
            AddButton(gx + 280, gy + 25, 0x1196, 0x1196, (int)RFButtons.NextRareButton, GumpButtonType.Reply, 0);   // -->

            AddButton(gx + 260, gy + 65, 0x2C94, 0x2C94, (int)RFButtons.TestButton, GumpButtonType.Reply, 0);   // test!!

        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            switch ((RFButtons)info.ButtonID)
            {

                case RFButtons.AddRareButton:
                    {
                        // Make sure a group has been defined (if one has, one will be 
                        // selected by default - group 0)
                        if (RareFactory.DODGroup.Count == 0)
                        {
                            from.SendMessage("You must define a Dynamic Object Definition Group before adding rares.");
                            break;
                        }

                        from.SendMessage("Select the item you wish to use as a template for this rare...");
                        from.Target = new RareTarget();
                        break;
                    }

                case RFButtons.DelRareButton:
                    {
                        if (RareFactory.ViewingDODGroup.DODInst.Count == 0)
                        {
                            from.SendMessage("There are no rares to delete!");
                            break;
                        }

                        from.SendMessage("Are you sure you wish to delete this rare? (y/n)");
                        from.Prompt = new DelConfirmPrompt(from);
                        break;
                    }


                case RFButtons.NextRareButton:
                    {
                        if (RareFactory.DODGroup.Count == 0)
                        {
                            from.SendMessage("No rares defined!");
                            break;
                        }

                        // The .ViewingDODIndex references the relative index
                        // within this particular DODGroup

                        DODGroup dg = (DODGroup)RareFactory.DODGroup[RareFactory.ViewingDODGroupIndex];

                        if ((RareFactory.ViewingDODIndex + 1) >= dg.DODInst.Count)
                        {
                            from.SendMessage("Last rare in group!");
                            break;
                        }
                        else
                        {
                            RareFactory.ViewingDODIndex++;
                            break;
                        }
                    }

                case RFButtons.PrevRareButton:
                    {
                        if (RareFactory.DODGroup.Count == 0)
                        {
                            from.SendMessage("No rares defined!");
                            break;
                        }

                        // The .ViewingDODIndex references the relative index
                        // within this particular DODGroup

                        if (RareFactory.ViewingDODIndex == 0)
                        {
                            from.SendMessage("First rare in group!");
                            break;
                        }
                        else
                        {
                            RareFactory.ViewingDODIndex--;
                            break;
                        }
                    }

                case RFButtons.TestButton:
                    {
                        if (RareFactory.DODGroup.Count == 0)
                        {
                            from.SendMessage("Define Dynamic Object Definition Groups and objects (rares) before attempting to generate test batches.");
                            break;
                        }

                        if (((DODGroup)RareFactory.DODGroup[RareFactory.ViewingDODGroupIndex]).DODInst.Count == 0)
                        {
                            from.SendMessage("You must add rares to the group before attempting to generate test batches.");
                            break;
                        }

                        int prevCur = RareFactory.ViewingDOD.CurIndex;

                        // Generates a complete set of rares as a test
                        from.SendMessage("Generating example rare set...");

                        for (int i = RareFactory.ViewingDOD.StartIndex; i <= RareFactory.ViewingDOD.LastIndex; i++)
                        {
                            // Create a new item
                            DODInstance dodi = RareFactory.ViewingDOD;
                            Item newitem = RareFactory.DupeItem(dodi.RareTemplate);

                            newitem.Movable = true;
                            dodi.CurIndex = (short)i;
                            dodi.DynamicFill(newitem);
                            from.AddToBackpack(newitem);
                        }

                        RareFactory.ViewingDOD.CurIndex = (short)prevCur;

                        from.SendMessage("Completed. {0} rares generated and added to your backpack.",
                                            (RareFactory.ViewingDOD.LastIndex -
                                             RareFactory.ViewingDOD.StartIndex) + 1);

                        break;
                    }
                default:
                    {
                        return;
                    }
            }

            RareFactory.ReloadViews(from);
        }


        // The RareTarget class handles the targetting of new rares
        // (the process which sucks them into the factory)

        private class RareTarget : Target
        {

            public RareTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {

                if (!(targ is Item))
                {
                    from.SendMessage("You can only create rares from items.");
                    return;
                }

                // Create the rare from the item
                RareFactory.AddRare((Item)targ);

                // Give it a name! Viewing DOD will be the one we're after now
                from.SendMessage("Enter the name you wish to use to represent the rare :");
                from.Prompt = new RareNamePrompt(from, RareFactory.ViewingDOD);
            }
        }

        private class RareNamePrompt : Prompt
        {
            private Mobile m_from;
            private DODInstance m_DODInst;

            public RareNamePrompt(Mobile from, DODInstance dodinst)
            {
                m_from = from;
                m_DODInst = dodinst;
            }

            public override void OnResponse(Mobile from, string text)
            {

                // Pattern match for invalid characters
                Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");

                if (InvalidPatt.IsMatch(text) || text.Length < 1 || text.Length > 15)
                {
                    // Invalid chars
                    from.SendMessage("You may only use numbers, letters, apostrophes, hyphens and spaces (1-15 characters).");
                    from.SendMessage("Please re-enter an allowed name :");
                    from.Prompt = new RareNamePrompt(from, RareFactory.ViewingDOD);
                }
                else
                {
                    m_DODInst.Name = text;

                    from.SendMessage("Please enter the 'Start Index' of the rare :");
                    from.Prompt = new IndexPrompt(0, from, m_DODInst);

                    // Update their display 
                    RareFactory.ReloadViews(from);
                }


            }

        }

        // Provides the prompting for "StartIndex" and "LastIndex"
        // (CurIndex just defaults to 0)

        private class IndexPrompt : Prompt
        {
            private Mobile m_from;
            private DODInstance m_DODInst;
            private short state;       // 0 = StartIndex, 1 = LastIndex

            public IndexPrompt(short st, Mobile from, DODInstance dodinst)
            {
                state = st;
                m_from = from;
                m_DODInst = dodinst;
            }

            public override void OnResponse(Mobile from, string text)
            {
                bool invalid = false;

                // Pattern match for invalid characters
                Regex InvalidPatt = new Regex("[^0-9]");

                if (InvalidPatt.IsMatch(text))
                    invalid = true;
                else
                    if (text.Length > 3)
                    invalid = true;
                else
                        if (Convert.ToInt16(text) > 255 || Convert.ToInt16(text) < 1)
                    invalid = true;

                if (invalid == true)
                {
                    // Invalid chars
                    from.SendMessage("You may only use numbers (1-255) to define indexes.");
                    from.SendMessage("Please enter a number instead :");
                    from.Prompt = new IndexPrompt(state, from, RareFactory.ViewingDOD);
                }
                else
                {
                    // The property we set + whether we send an additional prompt
                    // is dependent on our entry state
                    switch (state)
                    {
                        case 0:
                            {
                                m_DODInst.StartIndex = Convert.ToInt16(text);
                                from.SendMessage("Please enter the 'Last Index' of the rare : ");
                                from.Prompt = new IndexPrompt(((short)(state + 1)), from, m_DODInst);
                                break;
                            }
                        case 1:
                            {
                                m_DODInst.LastIndex = Convert.ToInt16(text);
                                from.SendMessage("Please enter the 'Start Date' of the rare : ");
                                from.Prompt = new DatePrompt(((short)0), from, m_DODInst);
                                break;
                            }
                    }

                    // Update their display 
                    RareFactory.ReloadViews(from);

                }

            }

        }


        // Provides the prompting for "StartDate" and "EndDate"

        private class DatePrompt : Prompt
        {
            private Mobile m_from;
            private DODInstance m_DODInst;
            private short state;       // 0 = StartDate, 1 = EndDate

            public DatePrompt(short st, Mobile from, DODInstance dodinst)
            {
                state = st;
                m_from = from;
                m_DODInst = dodinst;
            }

            public override void OnResponse(Mobile from, string text)
            {
                // Pattern match for invalid characters
                Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");

                DateTime resDateTime = new DateTime();

                try
                {
                    resDateTime = Convert.ToDateTime(text);
                }
                catch (Exception e)
                {
                    from.SendMessage("You must use a valid date/time format.");
                    from.SendMessage("Please re-enter in the format <DD/MM/YYYY HH:MM:SS> :");
                    from.Prompt = new DatePrompt(state, from, RareFactory.ViewingDOD);
                    LogHelper.LogException(e);
                    return;
                }

                try
                {
                    // The property we set + whether we send an additional prompt
                    // is dependent on our entry state
                    switch (state)
                    {
                        case 0:
                            {
                                m_DODInst.StartDate = Convert.ToDateTime(text);
                                from.SendMessage("Please enter the 'End Date' of the rare : ");
                                from.Prompt = new DatePrompt(((short)(state + 1)), from, m_DODInst);
                                break;
                            }
                        case 1:
                            {
                                m_DODInst.EndDate = Convert.ToDateTime(text);
                                from.SendMessage("Sucessfully defined new rare '" + m_DODInst.Name + "'!");
                                break;
                            }
                    }
                }
                catch (Exception e)
                {
                    // Console.WriteLine("RFControlGump() : Caught exception trying to set date properties : {0}", e);
                    LogHelper.LogException(e);
                }


                // Update their display 
                RareFactory.ReloadViews(from);


            }

        }


        private class DelConfirmPrompt : Prompt
        {
            private Mobile m_from;

            public DelConfirmPrompt(Mobile from)
            {
                m_from = from;
            }

            public override void OnResponse(Mobile from, string text)
            {

                if (text.ToLower() == "y")
                {
                    RareFactory.ViewingDOD.Expire();

                    if (RareFactory.ViewingDODIndex > 0)
                        RareFactory.ViewingDODIndex -= 1;
                }
                else
                {
                    from.SendMessage("Rare deletion cancelled.");
                }

                RareFactory.ReloadViews(from);

            }

        }

    }

}