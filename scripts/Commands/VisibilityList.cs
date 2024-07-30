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

/*
 * ChangeLog:
 * 6/14/2023, Adam
 *      Add targ.SendEverything() so staff don't show up as naked
 * 7/21/06, Rhiannon
 * 		Set access level for all commands to Reporter
 *  8/30/04 smerX
 *      Added VisRemove
 *      More filtering for Vis
 */
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;
using System.Collections;

namespace Server.Commands
{
    public class VisibilityList
    {
        public static void Initialize()
        {
            EventSink.Login += new LoginEventHandler(OnLogin);
            Server.CommandSystem.Register("Vis", AccessLevel.Reporter, new CommandEventHandler(Vis_OnCommand));
            Server.CommandSystem.Register("VisList", AccessLevel.Reporter, new CommandEventHandler(VisList_OnCommand));
            Server.CommandSystem.Register("VisClear", AccessLevel.Reporter, new CommandEventHandler(VisClear_OnCommand));
            Server.CommandSystem.Register("VisRemove", AccessLevel.Reporter, new CommandEventHandler(VisRemove_OnCommand));
        }

        public static void OnLogin(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)e.Mobile;

                pm.VisibilityList.Clear();
            }
        }

        [Usage("Vis")]
        [Description("Adds or removes a targeted player from your visibility list.  Anyone on your visibility list will be able to see you at all times, even when you're hidden.")]
        public static void Vis_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile)
            {
                e.Mobile.Target = new VisTarget();
                e.Mobile.SendMessage("Select person to add or remove from your visibility list.");
            }
        }

        [Usage("VisList")]
        [Description("Shows the names of everyone in your visibility list.")]
        public static void VisList_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)e.Mobile;
                ArrayList list = pm.VisibilityList;

                if (list.Count > 0)
                {
                    pm.SendMessage("You are visible to {0} mobile{1}:", list.Count, list.Count == 1 ? "" : "s");

                    for (int i = 0; i < list.Count; ++i)
                        pm.SendMessage("#{0}: {1}", i + 1, ((Mobile)list[i]).Name);
                }
                else
                {
                    pm.SendMessage("Your visibility list is empty.");
                }
            }
        }

        [Usage("VisRemove")]
        [Description("Manage your visibility list.")]
        public static void VisRemove_OnCommand(CommandEventArgs e)
        {

            if (e.Mobile is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)e.Mobile;
                ArrayList list = pm.VisibilityList;

                if (list.Count > 0)
                {
                    pm.SendGump(new VisListGump(pm, list, 0));
                }
                else
                {
                    pm.SendMessage("Your visibility list is empty.");
                }
            }
        }

        [Usage("VisClear")]
        [Description("Removes everyone from your visibility list.")]
        public static void VisClear_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)e.Mobile;
                ArrayList list = new ArrayList(pm.VisibilityList);

                pm.VisibilityList.Clear();
                pm.SendMessage("Your visibility list has been cleared.");

                for (int i = 0; i < list.Count; ++i)
                {
                    Mobile m = (Mobile)list[i];

                    if (!m.CanSee(pm) && Utility.InUpdateRange(m, pm))
                        m.Send(pm.RemovePacket);
                }
            }
        }

        private class VisTarget : Target
        {
            public VisTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from is PlayerMobile && targeted is PlayerMobile && ((PlayerMobile)from) != ((PlayerMobile)targeted))
                {
                    PlayerMobile pm = (PlayerMobile)from;
                    Mobile targ = (Mobile)targeted;

                    if (targ.AccessLevel <= from.AccessLevel)
                    {
                        ArrayList list = pm.VisibilityList;

                        if (list.Contains(targ))
                        {
                            list.Remove(targ);
                            from.SendMessage("{0} has been removed from your visibility list.", targ.Name);
                        }
                        else
                        {
                            list.Add(targeted);
                            from.SendMessage("{0} has been added to your visibility list.", targ.Name);
                        }

                        if (Utility.InUpdateRange(targ, from))
                        {
                            if (targ.CanSee(from))
                            {
                                targ.Send(new Network.MobileIncoming(targ, from));

                                if (ObjectPropertyList.Enabled)
                                {
                                    targ.Send(from.OPLPacket);

                                    foreach (Item item in from.Items)
                                        targ.Send(item.OPLPacket);

                                    targ.SendEverything();
                                }
                            }
                            else
                            {
                                targ.Send(from.RemovePacket);
                            }
                        }
                    }
                    else
                    {
                        from.SendMessage("They can already see you!");
                    }
                }
                else
                {
                    from.SendMessage("That is not a valid target.");
                }
            }
        }
    }
}