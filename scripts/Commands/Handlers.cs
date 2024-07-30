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

/* Scripts/Commands/Handlers.cs
 * CHANGELOG
 *  9/4/2023, Adam ([go)
 *      Allow player access to faction locations (for now,) on Test Center
 *  6/13/23, Yoar ([bank)
 *      The [bank command now displays the total #items and weight inside the bank box of the targeted mobile
 *  5/28/2023, Adam ([go)
 *      Added the ability for 'go' to search your backpack for a recall rune or runebook entry to 'go to'
 *  9/20/22, Adam ([go)
 *      Allow [go to accept "(x, y, z)" format
 *  9/6/22, Adam (Where command)
 *      Add the notion of being in an "(Inn)" to the Where command
 *  8/30/22, Adam (spelling)
 *      proirity ==> priority
 * 11/14/21, Yoar
 *      Renamed board game related BaseBoard to BaseGameBoard.
 * 7/26/21, Pix
 *      Added [ClearHarvestMemory (admin only) as a way to manually clear harvest memory
 *	5/26/2021, Adam: allow raw sextant style [go destinations
 *		length == 4 is old-style sextant: [go 55 54 N 72 54 W (do nothing)
 *		length == 6 is new-style sextant: [go 55� 54'N 72� 54'W (patch)
 *	5/25/2021, Adam: Allow comma delimited coords in [go
 *		For example; [go 4406, 1032, 1
 *	3/12/11, Adam
 *		Add a [shard command to tell the user which shard they are on
 *		believe me, with SP, AI, MO, and TC and EVENT verdsions of each, it's needed.
 *		See Also: [Host
 * 11/11/10, pix
 *      Removed stats command functionality for UOSP.
 *	6/16/10, adam
 *		enhhance [where to show the location of the region controller
 *	6/15/10, Adam
 *		Give players access to the [stats command again.
 *		Now displays the number of adventurers and pvpers currently active.
 *	6/9/10, Adam
 *		Return [stats to a staff only command.
 *	3/13/10, Adam
 *		Add [backpack command; like [bank, but opens the backpack, even of ghosts
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *  7/6/08, Adam
 *      Enhance [Where to also show the region UID
 *  2/5/07, Adam
 *      Remove DropGuildStone command
 *  11/24/06, Rhiannon
 *      Moved [available to its own file, Available.cs
 *  11/22/06, Rhiannon
 *      Added [available command to announce that a staffmember is holding court in a Counselors Guild.
 *	10/17/06, Adam
 *		Add more info when [where is used within a house.
 *  07/21/06, Rhiannon
 *		Set [move to FightBroker access
 *	06/14/06, Adam
 *		Add new [DropGuildStone command
 *  04/13/06, Kit
 *		Set [move to Councelor access for fightbroker use.
 *  12/20/05, Pig
 *		Set [stats command to AccessLevel.Player . Changed command to check access level to display different
 *		info for players and staff.
 *	6/21/04, mith
 *		Added ClearContainers command to delete all items from containers (preferrably during shard wipe).
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Diagnostics;
using Server.Engines.RewardSystem;
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Spells;
using Server.Targeting;
using Server.Targets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Server.Commands
{
    public class CommandHandlers
    {
        public static void Initialize()
        {
            Server.CommandSystem.CommandPrefix = "[";

            Properties.Register();

            // 9/4/2023, Adam: Allow player access to faction locations (for now,) on Test Center
            Register("Go", AccessLevel.Player, new CommandEventHandler(Go_OnCommand));

            Register("DropHolding", AccessLevel.Counselor, new CommandEventHandler(DropHolding_OnCommand));

            Register("GetFollowers", AccessLevel.GameMaster, new CommandEventHandler(GetFollowers_OnCommand));

            Register("ClearFacet", AccessLevel.Administrator, new CommandEventHandler(ClearFacet_OnCommand));

            Register("ShaveHair", AccessLevel.GameMaster, new CommandEventHandler(ShaveHair_OnCommand));
            Register("ShaveBeard", AccessLevel.GameMaster, new CommandEventHandler(ShaveBeard_OnCommand));

            Register("Where", AccessLevel.Counselor, new CommandEventHandler(Where_OnCommand));

            Register("AutoPageNotify", AccessLevel.Counselor, new CommandEventHandler(APN_OnCommand));
            Register("APN", AccessLevel.Counselor, new CommandEventHandler(APN_OnCommand));

            Register("Animate", AccessLevel.GameMaster, new CommandEventHandler(Animate_OnCommand));

            Register("Cast", AccessLevel.Counselor, new CommandEventHandler(Cast_OnCommand));

            Register("Stuck", AccessLevel.Counselor, new CommandEventHandler(Stuck_OnCommand));

            Register("Help", AccessLevel.Player, new CommandEventHandler(Help_OnCommand));

            Register("Usage", AccessLevel.GameMaster, new CommandEventHandler(Usage_OnCommand));

            Register("Save", AccessLevel.Administrator, new CommandEventHandler(Save_OnCommand));

            Register("Move", AccessLevel.GameMaster, new CommandEventHandler(Move_OnCommand));
            Register("MoveMobile", AccessLevel.GameMaster, new CommandEventHandler(MoveMobile_OnCommand));
            Register("Client", AccessLevel.Counselor, new CommandEventHandler(Client_OnCommand));

            Register("SMsg", AccessLevel.Counselor, new CommandEventHandler(StaffMessage_OnCommand));
            Register("SM", AccessLevel.Counselor, new CommandEventHandler(StaffMessage_OnCommand));
            Register("S", AccessLevel.Counselor, new CommandEventHandler(StaffMessage_OnCommand));

            Register("BCast", AccessLevel.GameMaster, new CommandEventHandler(BroadcastMessage_OnCommand));
            Register("BC", AccessLevel.GameMaster, new CommandEventHandler(BroadcastMessage_OnCommand));
            Register("B", AccessLevel.GameMaster, new CommandEventHandler(BroadcastMessage_OnCommand));

            Register("Bank", AccessLevel.GameMaster, new CommandEventHandler(Bank_OnCommand));
            Register("Backpack", AccessLevel.GameMaster, new CommandEventHandler(Backpack_OnCommand));

            Register("RangeExit", AccessLevel.GameMaster, new CommandEventHandler(Distance_OnCommand));

            Register("Echo", AccessLevel.Player, new CommandEventHandler(Echo_OnCommand));

            Register("Sound", AccessLevel.GameMaster, new CommandEventHandler(Sound_OnCommand));

            Register("ViewEquip", AccessLevel.GameMaster, new CommandEventHandler(ViewEquip_OnCommand));

            Register("Light", AccessLevel.Counselor, new CommandEventHandler(Light_OnCommand));
            Register("Stats", AccessLevel.Player, new CommandEventHandler(Stats_OnCommand));

            Register("Mobiles", AccessLevel.Administrator, new CommandEventHandler(Mobiles_OnCommand));
            Register("Items", AccessLevel.Administrator, new CommandEventHandler(Items_OnCommand));

            Register("ReplaceBankers", AccessLevel.Administrator, new CommandEventHandler(ReplaceBankers_OnCommand));

            Register("Reward", AccessLevel.Player, new CommandEventHandler(RewardSystem.Reward_OnCommand));

            Register("ClearContainers", AccessLevel.Administrator, new CommandEventHandler(ClearContainers_OnCommand));

            Register("Shard", AccessLevel.Counselor, new CommandEventHandler(Shard_OnCommand));

            Register("ClearHarvestMemory", AccessLevel.Administrator, new CommandEventHandler(ClearHarvestMemory_OnCommand));
        }

        public static void Register(string command, AccessLevel access, CommandEventHandler handler)
        {
            Server.CommandSystem.Register(command, access, handler);
        }

        #region Where
        [Usage("Where")]
        [Description("Tells the commanding player his coordinates, region, and facet.")]
        public static void Where_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            Map map = from.Map;
            List<string> list = new();
            Where(from, map, list);
            foreach (string s in list)
                from.SendMessage(s);
#if false
            from.SendMessage("You are at {0} {1} {2} ({3}) in {4}.", from.X, from.Y, from.Z, Sextant.Where(from), map);
            string area = "unknown";
            if (Utility.World.BritMainLandWrap.Contains(from.Location))
                area = "Britain";
            else if (Utility.World.LostLandsWrap.Contains(from.Location))
                area = "Lost Lands";
            else if (Utility.World.Dungeons.Contains(from.Location))
                area = "Dungeons";

            from.SendMessage("  in area '{0}'.", area);

            if (map != null)
            {
                try
                {
                    ArrayList reglist = Region.FindAll(from.Location, map);
                    ArrayList multlist = BaseMulti.FindAll(from.Location, map);

                    foreach (Region rx in reglist)
                    {
                        if (rx is Region)
                        {
                            if (rx is HouseRegion)
                            {
                                HouseRegion hr = rx as HouseRegion;
                                string text = string.Format("Region is a {0} at {1} id {2}.", hr.GetType().Name, hr.GoLocation, rx.UId);
                                if (Region.IsHighestPriority(rx, from.Location, map))
                                    from.SendMessage("(Highest priority) {0}", text);
                                else
                                    from.SendMessage("{0}", text);
                            }
                            else if (rx is StaticRegion && StaticRegion.Controller(rx as StaticRegion) != null)
                            {
                                StaticRegionControl rc = StaticRegion.Controller(rx as StaticRegion);
                                if (rc != null)
                                {
                                    Point3D location = new Point3D(0, 0, 0);
                                    if (rc != null)
                                        location = rc.Location;

                                    string inn = string.Empty;
                                    foreach (Rectangle3D px in rx.InnBounds)
                                        if (px.Contains(from))
                                        {
                                            inn = "(Inn)";
                                            break;
                                        }

                                    string text = string.Format("Region is {0}{4} with a STATIC controller at {1}({3}) id {2}.", rx, location, rx.UId, rc.Serial, inn);
                                    if (Region.IsHighestPriority(rx, from.Location, map))
                                        from.SendMessage("(Highest priority) {0}", text);
                                    else
                                        from.SendMessage("{0}", text);
                                }
                            }
                            else if (rx is CustomRegion)
                            {
                                CustomRegionControl rc = ((CustomRegion)rx).Controller;
                                Point3D location = new Point3D(0, 0, 0);
                                if (rc != null)
                                    location = rc.Location;

                                string inn = string.Empty;
                                foreach (Rectangle3D px in rx.InnBounds)
                                    if (px.Contains(from))
                                    {
                                        inn = "(Inn)";
                                        break;
                                    }

                                string text = string.Format("Region is {0}{4} with a DYNAMIC controller at {1}({3}) id {2}.", rx, location, rx.UId, rc.Serial, inn);
                                if (Region.IsHighestPriority(rx, from.Location, map))
                                    from.SendMessage("(Highest priority) {0}", text);
                                else
                                    from.SendMessage("{0}", text);
                            }
                            else if (rx != map.DefaultRegion)
                            {
                                string text = string.Format("Region is {0} id {1}.", rx, rx.UId);
                                if (Region.IsHighestPriority(rx, from.Location, map))
                                    from.SendMessage("(Highest priority) {0}", text);
                                else
                                    from.SendMessage("{0}", text);
                            }
                            else
                            {
                                if (rx == map.DefaultRegion)
                                {
                                    string text = string.Format("Region is {0} (DefaultRegion) id {1}.", rx, rx.UId);
                                    if (Region.IsHighestPriority(rx, from.Location, map))
                                        from.SendMessage("(Highest priority) {0}", text);
                                    else
                                        from.SendMessage("{0}", text);
                                }
                                else
                                {
                                    string text = string.Format("Region is {0} id {1}.", rx, rx.UId);
                                    if (Region.IsHighestPriority(rx, from.Location, map))
                                        from.SendMessage("(Highest priority) {0}", text);
                                    else
                                        from.SendMessage("{0}", text);
                                }
                            }
                        }
                    }

                    foreach (BaseMulti bx in multlist)
                    {
                        if (bx is BaseMulti)
                        {
                            from.SendMessage("Multi is a {0}.", bx.GetType().Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }

            }
#endif
        }
        public static void Where(IEntity ent, Map map, List<string> output)
        {
            if (ent is Item item && item.RootParent != null)
                ent = item.RootParent as IEntity;

            if (ent == null)
                return;

            output.Add(string.Format("You are at {0} {1} {2} ({3}) in {4}.", ent.X, ent.Y, ent.Z, Sextant.Where(ent), map));
            string area = "unknown";
            if (Utility.World.BritMainLandWrap.Contains(ent.Location))
                area = "Britain";
            else if (Utility.World.LostLandsWrap.Contains(ent.Location))
                area = "Lost Lands";
            else if (Utility.World.Dungeons.Contains(ent.Location))
                area = "Dungeons";

            output.Add(string.Format("  in area '{0}'.", area));

            if (map != null)
            {
                try
                {
                    ArrayList reglist = Region.FindAll(ent.Location, map);
                    ArrayList multlist = BaseMulti.FindAll(ent.Location, map);

                    foreach (Region rx in reglist)
                    {
                        if (rx is Region)
                        {
                            if (rx is HouseRegion)
                            {
                                HouseRegion hr = rx as HouseRegion;
                                string text = string.Format("Region is a {0} at {1} id {2}.", hr.GetType().Name, hr.GoLocation, rx.UId);
                                if (Region.IsHighestPriority(rx, ent.Location, map))
                                    output.Add(string.Format("(Highest priority) {0}", text));
                                else
                                    output.Add(string.Format("{0}", text));
                            }
                            else if (rx is StaticRegion && StaticRegion.Controller(rx as StaticRegion) != null)
                            {
                                StaticRegionControl rc = StaticRegion.Controller(rx as StaticRegion);
                                if (rc != null)
                                {
                                    Point3D location = new Point3D(0, 0, 0);
                                    if (rc != null)
                                        location = rc.Location;

                                    string inn = string.Empty;
                                    foreach (Rectangle3D px in rx.InnBounds)
                                        if (px.Contains(ent))
                                        {
                                            inn = "(Inn)";
                                            break;
                                        }

                                    string text = string.Format("Region is {0}{4} with a STATIC controller at {1}({3}) id {2}.", rx, location, rx.UId, rc.Serial, inn);
                                    if (Region.IsHighestPriority(rx, ent.Location, map))
                                        output.Add(string.Format("(Highest priority) {0}", text));
                                    else
                                        output.Add(string.Format("{0}", text));
                                }
                            }
                            else if (rx is CustomRegion)
                            {
                                CustomRegionControl rc = ((CustomRegion)rx).Controller;
                                Point3D location = new Point3D(0, 0, 0);
                                if (rc != null)
                                    location = rc.Location;

                                string inn = string.Empty;
                                foreach (Rectangle3D px in rx.InnBounds)
                                    if (px.Contains(ent))
                                    {
                                        inn = "(Inn)";
                                        break;
                                    }

                                string text = string.Format("Region is {0}{4} with a DYNAMIC controller at {1}({3}) id {2}.", rx, location, rx.UId, rc.Serial, inn);
                                if (Region.IsHighestPriority(rx, ent.Location, map))
                                    output.Add(string.Format("(Highest priority) {0}", text));
                                else
                                    output.Add(string.Format("{0}", text));
                            }
                            else if (rx != map.DefaultRegion)
                            {
                                string text = string.Format("Region is {0} id {1}.", rx, rx.UId);
                                if (Region.IsHighestPriority(rx, ent.Location, map))
                                    output.Add(string.Format("(Highest priority) {0}", text));
                                else
                                    output.Add(string.Format("{0}", text));
                            }
                            else
                            {
                                if (rx == map.DefaultRegion)
                                {
                                    string text = string.Format("Region is {0} (DefaultRegion) id {1}.", rx, rx.UId);
                                    if (Region.IsHighestPriority(rx, ent.Location, map))
                                        output.Add(string.Format("(Highest priority) {0}", text));
                                    else
                                        output.Add(string.Format("{0}", text));
                                }
                                else
                                {
                                    string text = string.Format("Region is {0} id {1}.", rx, rx.UId);
                                    if (Region.IsHighestPriority(rx, ent.Location, map))
                                        output.Add(string.Format("(Highest priority) {0}", text));
                                    else
                                        output.Add(string.Format("{0}", text));
                                }
                            }
                        }
                    }

                    foreach (BaseMulti bx in multlist)
                    {
                        if (bx is BaseMulti)
                        {
                            output.Add(string.Format("Multi is a {0}.", bx.GetType().Name));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }

            }
        }
        #endregion Where

        [Usage("DropHolding")]
        [Description("Drops the item, if any, that a targeted player is holding. The item is placed into their backpack, or if that's full, at their feet.")]
        public static void DropHolding_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(DropHolding_OnTarget));
            e.Mobile.SendMessage("Target the player to drop what they are holding.");
        }

        public static void DropHolding_OnTarget(Mobile from, object obj)
        {
            if (obj is Mobile && ((Mobile)obj).Player)
            {
                Mobile targ = (Mobile)obj;
                Item held = targ.Holding;

                if (held == null)
                {
                    from.SendMessage("They are not holding anything.");
                }
                else
                {
                    if (from.AccessLevel == AccessLevel.Counselor)
                    {
                        Engines.Help.PageEntry pe = Engines.Help.PageQueue.GetEntry(targ);

                        if (pe == null || pe.Handler != from)
                        {
                            if (pe == null)
                                from.SendMessage("You may only use this command on someone who has paged you.");
                            else
                                from.SendMessage("You may only use this command if you are handling their help page.");

                            return;
                        }
                    }

                    if (targ.AddToBackpack(held))
                        from.SendMessage("The item they were holding has been placed into their backpack.");
                    else
                        from.SendMessage("The item they were holding has been placed at their feet.");

                    held.ClearBounce();

                    targ.Holding = null;
                }
            }
            else
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(DropHolding_OnTarget));
                from.SendMessage("That is not a player. Try again.");
            }
        }

        public static void DeleteList_Callback(Mobile from, bool okay, object state)
        {
            if (okay)
            {
                ArrayList list = (ArrayList)state;

                CommandLogging.WriteLine(from, "{0} {1} deleting {2} objects", from.AccessLevel, CommandLogging.Format(from), list.Count);

                for (int i = 0; i < list.Count; ++i)
                {
                    object obj = list[i];

                    if (obj is Item)
                        ((Item)obj).Delete();
                    else if (obj is Mobile)
                        ((Mobile)obj).Delete();
                }

                from.SendMessage("You have deleted {0} object{1}.", list.Count, list.Count == 1 ? "" : "s");
            }
            else
            {
                from.SendMessage("You have chosen not to delete those objects.");
            }
        }

        [Usage("ClearFacet")]
        [Description("Deletes all items and mobiles in your facet. Players and their inventory will not be deleted.")]
        public static void ClearFacet_OnCommand(CommandEventArgs e)
        {
            Map map = e.Mobile.Map;

            if (map == null || map == Map.Internal)
            {
                e.Mobile.SendMessage("You may not run that command here.");
                return;
            }

            ArrayList list = new ArrayList();

            foreach (Item item in World.Items.Values)
            {
                if (item.Map == map && item.Parent == null)
                    list.Add(item);
            }

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m.Map == map && !m.Player)
                    list.Add(m);
            }

            if (list.Count > 0)
            {
                CommandLogging.WriteLine(e.Mobile, "{0} {1} starting facet clear of {2} ({3} objects)", e.Mobile.AccessLevel, CommandLogging.Format(e.Mobile), map, list.Count);

                e.Mobile.SendGump(
                    new WarningGump(1060635, 30720,
                    String.Format("You are about to delete {0} object{1} from this facet.  Do you really wish to continue?",
                    list.Count, list.Count == 1 ? "" : "s"),
                    0xFFC000, 360, 260, new WarningGumpCallback(DeleteList_Callback), list));
            }
            else
            {
                e.Mobile.SendMessage("There were no objects found to delete.");
            }
        }

        [Usage("GetFollowers")]
        [Description("Teleports all pets of a targeted player to your location.")]
        public static void GetFollowers_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(GetFollowers_OnTarget));
            e.Mobile.SendMessage("Target a player to get their pets.");
        }

        public static void GetFollowers_OnTarget(Mobile from, object obj)
        {
            if (obj is Mobile && ((Mobile)obj).Player)
            {
                Mobile master = (Mobile)obj;
                ArrayList pets = new ArrayList();

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is BaseCreature)
                    {
                        BaseCreature bc = (BaseCreature)m;

                        if ((bc.Controlled && bc.ControlMaster == master) || (bc.Summoned && bc.SummonMaster == master))
                            pets.Add(bc);
                    }
                }

                if (pets.Count > 0)
                {
                    CommandLogging.WriteLine(from, "{0} {1} getting all followers of {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(master));

                    from.SendMessage("That player has {0} pet{1}.", pets.Count, pets.Count != 1 ? "s" : "");

                    for (int i = 0; i < pets.Count; ++i)
                    {
                        Mobile pet = (Mobile)pets[i];

                        if (pet is IMount)
                            ((IMount)pet).Rider = null; // make sure it's dismounted

                        pet.MoveToWorld(from.Location, from.Map);
                    }
                }
                else
                {
                    from.SendMessage("There were no pets found for that player.");
                }
            }
            else
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(GetFollowers_OnTarget));
                from.SendMessage("That is not a player. Try again.");
            }
        }

        public static void ReplaceBankers_OnCommand(CommandEventArgs e)
        {
            ArrayList list = new ArrayList();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if ((m is Banker) && !(m is BaseCreature))
                    list.Add(m);
            }

            foreach (Mobile m in list)
            {
                Map map = m.Map;

                if (map != null)
                {
                    bool hasBankerSpawner = false;

                    IPooledEnumerable eable = m.GetItemsInRange(0);
                    foreach (Item item in eable)
                    {
                        if (item is Spawner)
                        {
                            Spawner spawner = (Spawner)item;

                            for (int i = 0; !hasBankerSpawner && i < spawner.ObjectNames.Count; ++i)
                                hasBankerSpawner = Insensitive.Equals((string)spawner.ObjectNames[i], "banker");

                            if (hasBankerSpawner)
                                break;
                        }
                    }
                    eable.Free();

                    if (!hasBankerSpawner)
                    {
                        Spawner spawner = new Spawner(1, 1, 5, 0, 4, "banker");

                        spawner.MoveToWorld(m.Location, map);
                    }
                }
            }
        }

        private class ViewEqTarget : Target
        {
            public ViewEqTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!BaseCommand.IsAccessible(from, targeted))
                {
                    from.SendMessage("That is not accessible.");
                    return;
                }

                if (targeted is Mobile)
                    from.SendMenu(new EquipMenu(from, (Mobile)targeted, GetEquip((Mobile)targeted)));
            }

            private static ItemListEntry[] GetEquip(Mobile m)
            {
                ItemListEntry[] entries = new ItemListEntry[m.Items.Count];

                for (int i = 0; i < m.Items.Count; ++i)
                {
                    Item item = (Item)m.Items[i];

                    entries[i] = new ItemListEntry(String.Format("{0}: {1}", item.Layer, item.GetType().Name), item.ItemID, item.Hue);
                }

                return entries;
            }

            private class EquipMenu : ItemListMenu
            {
                private Mobile m_Mobile;

                public EquipMenu(Mobile from, Mobile m, ItemListEntry[] entries)
                    : base("Equipment", entries)
                {
                    m_Mobile = m;

                    CommandLogging.WriteLine(from, "{0} {1} getting equip for {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(m));
                }

                public override void OnResponse(NetState state, int index)
                {
                    if (index >= 0 && index < m_Mobile.Items.Count)
                    {
                        Item item = (Item)m_Mobile.Items[index];

                        state.Mobile.SendMenu(new EquipDetailsMenu(m_Mobile, item));
                    }
                }

                private class EquipDetailsMenu : QuestionMenu
                {
                    private Mobile m_Mobile;
                    private Item m_Item;

                    public EquipDetailsMenu(Mobile m, Item item)
                        : base(String.Format("{0}: {1}", item.Layer, item.GetType().Name), new string[] { "Move", "Delete", "Props" })
                    {
                        m_Mobile = m;
                        m_Item = item;
                    }

                    public override void OnCancel(NetState state)
                    {
                        state.Mobile.SendMenu(new EquipMenu(state.Mobile, m_Mobile, ViewEqTarget.GetEquip(m_Mobile)));
                    }

                    public override void OnResponse(NetState state, int index)
                    {
                        if (index == 0)
                        {
                            CommandLogging.WriteLine(state.Mobile, "{0} {1} moving equip item {2} of {3}", state.Mobile.AccessLevel, CommandLogging.Format(state.Mobile), CommandLogging.Format(m_Item), CommandLogging.Format(m_Mobile));
                            state.Mobile.Target = new MoveTarget(m_Item);
                        }
                        else if (index == 1)
                        {
                            CommandLogging.WriteLine(state.Mobile, "{0} {1} deleting equip item {2} of {3}", state.Mobile.AccessLevel, CommandLogging.Format(state.Mobile), CommandLogging.Format(m_Item), CommandLogging.Format(m_Mobile));
                            m_Item.Delete();
                        }
                        else if (index == 2)
                        {
                            CommandLogging.WriteLine(state.Mobile, "{0} {1} opening props for equip item {2} of {3}", state.Mobile.AccessLevel, CommandLogging.Format(state.Mobile), CommandLogging.Format(m_Item), CommandLogging.Format(m_Mobile));
                            state.Mobile.SendGump(new PropertiesGump(state.Mobile, m_Item));
                        }
                    }
                }
            }
        }

        [Usage("ViewEquip")]
        [Description("Lists equipment of a targeted mobile. From the list you can move, delete, or open props.")]
        public static void ViewEquip_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new ViewEqTarget();
        }

        [Usage("Sound <index> [toAll=true]")]
        [Description("Plays a sound to players within 12 tiles of you. The (toAll) argument specifies to everyone, or just those who can see you.")]
        public static void Sound_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
                PlaySound(e.Mobile, e.GetInt32(0), true);
            else if (e.Length == 2)
                PlaySound(e.Mobile, e.GetInt32(0), e.GetBoolean(1));
            else
                e.Mobile.SendMessage("Format: Sound <index> [toAll]");
        }

        private static void PlaySound(Mobile m, int index, bool toAll)
        {
            Map map = m.Map;

            if (map == null)
                return;

            CommandLogging.WriteLine(m, "{0} {1} playing sound {2} (toAll={3})", m.AccessLevel, CommandLogging.Format(m), index, toAll);

            Packet p = new PlaySound(index, m.Location);

            p.Acquire();

            IPooledEnumerable eable = m.GetClientsInRange(12);
            foreach (NetState state in eable)
            {
                if (toAll || state.Mobile.CanSee(m))
                    state.Send(p);
            }
            eable.Free();

            p.Release();
        }

        private class BankTarget : Target
        {
            public BankTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    Mobile m = (Mobile)targeted;

                    BankBox box = (m.Player ? m.BankBox : m.FindBankNoCreate());

                    if (box != null)
                    {
                        CommandLogging.WriteLine(from, "{0} {1} opening bank box of {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targeted));

                        if (from == targeted)
                        {
                            box.Open();
                        }
                        else
                        {
                            m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, true, String.Format("Bank container has {0} items, {1} stones", box.TotalItems, box.TotalWeight), from.NetState);
                            box.DisplayTo(from);
                        }
                    }
                    else
                    {
                        from.SendMessage("They have no bank box.");
                    }
                }
            }
        }

        private class BackpackTarget : Target
        {
            public BackpackTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    Mobile m = (Mobile)targeted;

                    Backpack box = (Backpack)m.Backpack;

                    if (box != null)
                    {
                        CommandLogging.WriteLine(from, "{0} {1} opening backpack of {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targeted));
                        box.DisplayTo(from);
                    }
                    else
                    {
                        from.SendMessage("They have no backpack.");
                    }
                }
            }
        }

        [Usage("Echo <text>")]
        [Description("Relays (text) as a system message.")]
        public static void Echo_OnCommand(CommandEventArgs e)
        {
            string toEcho = e.ArgString.Trim();

            if (toEcho.Length > 0)
                e.Mobile.SendMessage(toEcho);
            else
                e.Mobile.SendMessage("Format: Echo \"<text>\"");
        }

        [Usage("Bank")]
        [Description("Opens the bank box of a given target.")]
        public static void Bank_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new BankTarget();
        }

        [Usage("Backpack")]
        [Description("Opens the backpack of a given target.")]
        public static void Backpack_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new BackpackTarget();
        }

        [Usage("RangeExit")]
        [Description("Check the distance from here to there.")]
        public static void Distance_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new DistanceTarget();
        }

        private class DistanceTarget : Target
        {
            public DistanceTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                    from.SendMessage(String.Format("RangeExit is {0} tiles.", from.GetDistanceToSqrt(targeted as Mobile)));
                else if (targeted is Item)
                    from.SendMessage(String.Format("RangeExit is {0} tiles.", from.GetDistanceToSqrt((targeted as Item).Location)));
                else
                    from.SendMessage(String.Format("RangeExit is Unknown."));
            }
        }

        private class DismountTarget : Target
        {
            public DismountTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    CommandLogging.WriteLine(from, "{0} {1} dismounting {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targeted));

                    Mobile targ = (Mobile)targeted;

                    for (int i = 0; i < targ.Items.Count; ++i)
                    {
                        Item item = (Item)targ.Items[i];

                        if (item is IMountItem)
                        {
                            IMount mount = ((IMountItem)item).Mount;

                            if (mount != null)
                                mount.Rider = null;

                            if (targ.Items.IndexOf(item) == -1)
                                --i;
                        }
                    }

                    for (int i = 0; i < targ.Items.Count; ++i)
                    {
                        Item item = (Item)targ.Items[i];

                        if (item.Layer == Layer.Mount)
                        {
                            item.Delete();
                            --i;
                        }
                    }
                }
            }
        }

        private class ClientTarget : Target
        {
            public ClientTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    Mobile targ = (Mobile)targeted;

                    if (targ.NetState != null)
                    {
                        CommandLogging.WriteLine(from, "{0} {1} opening client menu of {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targeted));
                        from.SendGump(new ClientGump(from, targ.NetState));
                    }
                }
            }
        }

        [Usage("Client")]
        [Description("Opens the client gump menu for a given player.")]
        private static void Client_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new ClientTarget();
        }

        [Usage("Move")]
        [Description("Repositions a targeted item or mobile.")]
        private static void Move_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new PickMoveTarget(mobiles_only: false);
        }

        [Usage("MoveMobile")]
        [Description("Repositions a targeted mobile.")]
        private static void MoveMobile_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new PickMoveTarget(mobiles_only: true);
        }

        private class FirewallTarget : Target
        {
            public FirewallTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    Mobile targ = (Mobile)targeted;

                    NetState state = targ.NetState;

                    if (state != null)
                    {
                        CommandLogging.WriteLine(from, "{0} {1} firewalling {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targeted));

                        try
                        {
                            Firewall.Add(((IPEndPoint)state.Socket.RemoteEndPoint).Address.ToString());
                        }
                        catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                    }
                }
            }
        }

        [Usage("Save")]
        [Description("Saves the world.")]
        private static void Save_OnCommand(CommandEventArgs e)
        {
            Misc.AutoSave.Save();
        }

        private static bool FixMap(ref Map map, ref Point3D loc, Item item)
        {
            if (map == null || map == Map.Internal)
            {
                Mobile m = item.RootParent as Mobile;

                return (m != null && FixMap(ref map, ref loc, m));
            }

            return true;
        }

        private static bool FixMap(ref Map map, ref Point3D loc, Mobile m)
        {
            if (map == null || map == Map.Internal)
            {
                map = m.LogoutMap;
                loc = m.LogoutLocation;
            }

            return (map != null && map != Map.Internal);
        }
        private static Point3D GetMarkLocation(Mobile m, ref Map map, string description)
        {
            if (m.Backpack == null)
                return Point3D.Zero;

            ArrayList list = m.Backpack.FindAllItems();
            foreach (Item item in list)
            {
                if (item is RecallRune)
                {
                    RecallRune rune = (RecallRune)item;

                    if (rune.Marked && rune.TargetMap != null && rune.Description != null && rune.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                    {
                        map = rune.TargetMap;
                        return rune.Target;
                    }
                }
                else if (item is Moonstone)
                {
                    Moonstone stone = (Moonstone)item;

                    if (stone.Marked && stone.Description != null && stone.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                    {
                        // map = stone.TargetMap; no map info
                        return stone.Destination;
                    }
                }
                else if (item is Runebook)
                {
                    Runebook book = (Runebook)item;

                    for (int i = 0; i < book.Entries.Count; ++i)
                    {
                        RunebookEntry entry = (RunebookEntry)book.Entries[i];

                        if (entry.Map != null && entry.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                        {
                            map = entry.Map;
                            return entry.Location;
                        }
                    }
                }
            }

            return Point3D.Zero;
        }
        [Usage("Go [name | serial | (x y [z]) | (deg min (N | S) deg min (E | W))]")]
        [Description("With no arguments, this command brings up the go menu. With one argument, (name), you are moved to that regions \"go location.\" Or, if a numerical value is specified for one argument, (serial), you are moved to that object. Two or three arguments, (x y [z]), will move your character to that location. When six arguments are specified, (deg min (N | S) deg min (E | W)), your character will go to an approximate of those sextant coordinates.")]
        private static void Go_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            from.LightLevel = 100;

            if (Core.RuleSets.TestCenterRules() && from.AccessLevel == AccessLevel.Player)
            {
                Dictionary<string, Point3D> places = new(StringComparer.OrdinalIgnoreCase)
                {   // factions
                    { "Council of Mages", new Point3D(3750, 2241, 20) },
                    { "CouncilofMages", new Point3D(3750, 2241, 20) },
                    { "Council", new Point3D(3750, 2241, 20) },
                    { "COM", new Point3D(3750, 2241, 20) },
                    { "C", new Point3D(3750, 2241, 20) },

                    { "Minax", new Point3D(1172, 2593, 0) },
                    { "M", new Point3D(1172, 2593, 0) },

                    { "Shadowlords", new Point3D(969, 768, 0) },
                    { "Shadow Lords", new Point3D(969, 768, 0) },
                    { "Shadow", new Point3D(969, 768, 0) },
                    { "SL", new Point3D(969, 768, 0) },
                    { "S", new Point3D(969, 768, 0) },

                    { "True Britannians", new Point3D(1419, 1622, 20) },
                    { "TrueBritannians", new Point3D(1419, 1622, 20) },
                    { "True", new Point3D(1419, 1622, 20) },
                    { "TB", new Point3D(1419, 1622, 20) },
                    { "True Brits", new Point3D(1419, 1622, 20) },
                    { "TrueBrits", new Point3D(1419, 1622, 20) },
                    { "T", new Point3D(1419, 1622, 20) },
                    // towns
                    {"Britain", new Point3D(1592, 1680, 10) },
                    {"Magincia", new Point3D(3714, 2235, 20) },
                    {"Minoc", new Point3D(2471, 439, 15 )},
                    {"Moonglow", new Point3D(4436,1083, 0) },
                    {"Skara Brae", new Point3D( 576, 2200, 0) },
                    {"Trinsic", new Point3D(1914, 2717, 20) },
                    {"Vesper", new Point3D( 2982, 818, 0) },
                    { "Yew", new Point3D(548, 979, 0) },
                };
                string dest = string.Empty;
                foreach (string s in e.Arguments)
                {
                    dest += s;
                    if (places.ContainsKey(dest))
                    {
                        e.Mobile.MoveToWorld(places[s], e.Mobile.Map);
                        return;
                    }
                    else
                        dest += " ";
                }
                from.SendMessage("Format: Go [<faction_name> | <town_name>");
            }
            else if (from.AccessLevel > AccessLevel.Player)
            {
                if (e.Length == 0)
                {
                    GoGump.DisplayTo(from);
                }
                else if (e.Length == 1)
                {
                    try
                    {
                        int ser = e.GetInt32(0);

                        IEntity ent = World.FindEntity(ser);

                        if (ent is Item)
                        {
                            Item item = (Item)ent;

                            Map map = item.Map;
                            Point3D loc = item.GetWorldLocation();

                            Mobile owner = item.RootParent as Mobile;

                            if (owner != null && (owner.Map != null && owner.Map != Map.Internal) && !from.CanSee(owner))
                            {
                                from.SendMessage("You can not go to what you can not see.");
                                return;
                            }
                            else if (owner != null && (owner.Map == null || owner.Map == Map.Internal) && owner.Hidden && owner.AccessLevel >= from.AccessLevel)
                            {
                                from.SendMessage("You can not go to what you can not see.");
                                return;
                            }
                            else if (!FixMap(ref map, ref loc, item))
                            {
                                from.SendMessage("That is an internal item and you cannot go to it.");
                                return;
                            }

                            from.MoveToWorld(loc, map);

                            return;
                        }
                        else if (ent is Mobile)
                        {
                            Mobile m = (Mobile)ent;

                            Map map = m.Map;
                            Point3D loc = m.Location;

                            Mobile owner = m;

                            if (owner != null && (owner.Map != null && owner.Map != Map.Internal) && !from.CanSee(owner))
                            {
                                from.SendMessage("You can not go to what you can not see.");
                                return;
                            }
                            else if (owner != null && (owner.Map == null || owner.Map == Map.Internal) && owner.Hidden && owner.AccessLevel >= from.AccessLevel)
                            {
                                from.SendMessage("You can not go to what you can not see.");
                                return;
                            }
                            else if (!FixMap(ref map, ref loc, m))
                            {
                                from.SendMessage("That is an internal mobile and you cannot go to it.");
                                return;
                            }

                            from.MoveToWorld(loc, map);

                            return;
                        }
                        else
                        {
                            Map map = Map.Felucca;
                            if (GetMarkLocation(e.Mobile, ref map, e.GetString(0)) != Point3D.Zero)
                            {
                                from.Location = GetMarkLocation(e.Mobile, ref map, e.GetString(0));
                                from.Map = map;
                                return;
                            }
                            else if (Region.FindByName(e.GetString(0), e.Mobile.Map) != null)
                            {
                                string name = e.GetString(0);
                                var list = from.Map.RegionsSorted;//.Values.ToList();

                                for (int i = 0; i < list.Count; ++i)
                                {
                                    Region r = (Region)list[i];

                                    if (Insensitive.Equals(r.Name, name))
                                    {
                                        from.Location = new Point3D(r.GoLocation);
                                        return;
                                    }
                                }
                            }
                            // look for something like x,y,z (no spaces)
                            else if (e.ArgString.Contains(','))
                            {
                                string new_string = e.ArgString.Replace(",", " ");
                                string[] args = new_string.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                Go_OnCommand(new CommandEventArgs(e.Mobile, e.Command, new_string, args));
                                return;
                            }
                            else
                            {
                                if (ser != 0)
                                    from.SendMessage("No object with that serial was found.");
                                else
                                    from.SendMessage("No region with that name was found.");
                            }
                            return;
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                    from.SendMessage("Region name not found");
                }
                else if (e.Length == 2)
                {
                    Map map = from.Map;

                    if (map != null)
                    {
                        /// 6/1/2021, Adam: Allow comma delimited coords
                        /// For example; [go 4406, 1032
                        int[] ints = Utility.IntParser(string.Join(" ", e.Arguments));
                        from.Location = new Point3D(ints[0], ints[1], map.GetAverageZ(ints[0], ints[1]));
                    }
                }
                else if (e.Length == 3)
                {
                    /// 5/25/2021, Adam: Allow comma delimited coords
                    /// For example; [go 4406, 1032, 1
                    int[] ints = Utility.IntParser(string.Join(" ", e.Arguments));
                    from.Location = new Point3D(ints[0], ints[1], ints[2]);
                }
                else if (e.Length == 4 || e.Length == 6)
                {
                    // length == 6 is old-style sextant: [go 55 54 N 72 54 W
                    // length == 7 is new-style sextant: [go 55� 54'N 72� 54'W
                    Map map = from.Map;

                    if (map != null)
                    {
                        Point3D p = Sextant.Parse(map, string.Join(" ", e.Arguments));
                        if (p != Point3D.Zero)
                            from.Location = p;
                        else
                            from.SendMessage("Sextant reverse lookup failed.");
                    }
                }
                else
                {
                    from.SendMessage("Format: Go [name | serial | (x y [z]) | (deg min (N | S) deg min (E | W)]");
                }
            }
        }

        [Usage("Help")]
        [Description("Lists all available commands.")]
        public static void Help_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            ArrayList list = new ArrayList();

            foreach (CommandEntry entry in Server.CommandSystem.Entries.Values)
            {
                if (m.AccessLevel >= entry.AccessLevel)
                    list.Add(entry);
            }

            list.Sort();

            StringBuilder sb = new StringBuilder();

            if (list.Count > 0)
                sb.Append(((CommandEntry)list[0]).Command);

            ArrayList record_list = new ArrayList();
            for (int i = 1; i < list.Count; ++i)
            {
                string v = ((CommandEntry)list[i]).Command;
                if (m.AccessLevel > AccessLevel.Player)
                    record_list.Add(v = string.Format("[{0}] {1}", ((CommandEntry)list[i]).AccessLevel.ToString(), ((CommandEntry)list[i]).Command));

                if ((sb.Length + 1 + v.Length) >= 256)
                {
                    m.SendAsciiMessage(0x482, sb.ToString());
                    sb = new StringBuilder();
                    sb.Append(v);
                }
                else
                {
                    sb.Append(' ');
                    sb.Append(v);
                }
            }

            // record our list of commands sorted by access level
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                LogHelper Logger = new LogHelper("Commands.log", true);
                record_list.Sort();
                foreach (string tr in record_list)
                {
                    Logger.Log(LogType.Text, tr);
                }
                Logger.Finish();
            }

            if (sb.Length > 0)
                m.SendAsciiMessage(0x482, sb.ToString());
        }

        #region Usage
        [Usage("Usage")]
        [Description("Lists available properties and their meaning.")]
        public static void Usage_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            from.SendMessage("Target the item you would like help on...");
            from.Target = new UsageTarget();
        }
        private class UsageTarget : Target
        {
            public UsageTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item item)
                {
                    item.Usage(from);
                }
                else
                {
                    from.SendMessage("That is not an item.");
                }
            }
        }
        #endregion  Usage

        [Usage("SMsg <text>")]
        [Aliases("S", "SM")]
        [Description("Broadcasts a message to all online staff.")]
        public static void StaffMessage_OnCommand(CommandEventArgs e)
        {
            BroadcastMessage(AccessLevel.Counselor, e.Mobile.SpeechHue, String.Format("[{0}] {1}", e.Mobile.Name, e.ArgString));
        }

        [Usage("BCast <text>")]
        [Aliases("B", "BC")]
        [Description("Broadcasts a message to everyone online.")]
        public static void BroadcastMessage_OnCommand(CommandEventArgs e)
        {
            BroadcastMessage(AccessLevel.Player, 0x482, String.Format("Staff message from {0}:", e.Mobile.Name));
            BroadcastMessage(AccessLevel.Player, 0x482, e.ArgString);
        }

        public static void BroadcastMessage(AccessLevel ac, int hue, string message)
        {
            foreach (NetState state in NetState.Instances)
            {
                Mobile m = state.Mobile;

                if (m != null && m.AccessLevel >= ac)
                    m.SendMessage(hue, message);
            }
        }

        private class DeleteItemByLayerTarget : Target
        {
            private Layer m_Layer;

            public DeleteItemByLayerTarget(Layer layer)
                : base(-1, false, TargetFlags.None)
            {
                m_Layer = layer;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    Item item = ((Mobile)targeted).FindItemOnLayer(m_Layer);

                    if (item != null)
                    {
                        CommandLogging.WriteLine(from, "{0} {1} deleting item on layer {2} of {3}", from.AccessLevel, CommandLogging.Format(from), m_Layer, CommandLogging.Format(targeted));
                        item.Delete();
                    }
                }
                else
                {
                    from.SendMessage("Target a mobile.");
                }
            }
        }

        [Usage("ShaveHair")]
        [Description("Removes the hair of a targeted mobile.")]
        public static void ShaveHair_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new DeleteItemByLayerTarget(Layer.Hair);
        }

        [Usage("ShaveBeard")]
        [Description("Removes the beard of a targeted mobile.")]
        public static void ShaveBeard_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new DeleteItemByLayerTarget(Layer.FacialHair);
        }

        [Usage("AutoPageNotify")]
        [Aliases("APN")]
        [Description("Toggles your auto-page-notify status.")]
        public static void APN_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            m.AutoPageNotify = !m.AutoPageNotify;

            m.SendMessage("Your auto-page-notify has been turned {0}.", m.AutoPageNotify ? "on" : "off");
        }

        [Usage("Animate <action> <frameCount> <repeatCount> <forward> <repeat> <delay>")]
        [Description("Makes your character do a specified animation.")]
        public static void Animate_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 6)
            {
                e.Mobile.Animate(e.GetInt32(0), e.GetInt32(1), e.GetInt32(2), e.GetBoolean(3), e.GetBoolean(4), e.GetInt32(5));
            }
            else
            {
                e.Mobile.SendMessage("Format: Animate <action> <frameCount> <repeatCount> <forward> <repeat> <delay>");
            }
        }

        [Usage("Cast <name>")]
        [Description("Casts a spell by name.")]
        public static void Cast_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {
                if (!Multis.DesignContext.Check(e.Mobile))
                    return; // They are customizing

                Spell spell = SpellRegistry.NewSpell(e.GetString(0), e.Mobile, null);

                if (spell != null)
                    spell.Cast();
                else
                    e.Mobile.SendMessage("That spell was not found.");
            }
            else
            {
                e.Mobile.SendMessage("Format: Cast <name>");
            }
        }

        private class StuckMenuTarget : Target
        {
            public StuckMenuTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    from.SendGump(new StuckMenu(from, (Mobile)targeted, false));
                }
            }
        }

        [Usage("Stuck")]
        [Description("Opens a menu of towns, used for teleporting stuck mobiles.")]
        public static void Stuck_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new StuckMenuTarget();
        }

        [Usage("Light <level>")]
        [Description("Set your local lightlevel.")]
        public static void Light_OnCommand(CommandEventArgs e)
        {
            e.Mobile.LightLevel = e.GetInt32(0);
        }

        [Usage("Stats")]
        [Description("View some stats about the server.")]
        public static void Stats_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("{0}", Server.Misc.Stats.Format(e.Mobile.AccessLevel));
        }
        [Usage("Mobiles")]
        [Description("View a breakdown of mobiles.")]
        public static void Mobiles_OnCommand(CommandEventArgs e)
        {
            int mobileCount = World.Mobiles.Count;
            int mobiles_internal_map = 0;
            int mobiles_null_map = 0;
            foreach (Mobile m in World.Mobiles.Values)
                if (m.Map == null)
                    mobiles_null_map++;
                else if (m.Map == Map.Internal)
                    mobiles_internal_map++;

            e.Mobile.SendMessage("There are {0} total mobiles", mobileCount);
            e.Mobile.SendMessage("There are {0} mobiles on the internal map", mobiles_internal_map);
            e.Mobile.SendMessage("There are {0} mobiles on the null map", mobiles_null_map);
            e.Mobile.SendMessage("There are {0} mobiles in world", mobileCount - (mobiles_internal_map + mobiles_null_map));
        }
        [Usage("Items")]
        [Description("View a breakdown of items.")]
        public static void Items_OnCommand(CommandEventArgs e)
        {
            int itemCount = World.Items.Count;
            int items_internal_map = 0;
            int items_null_map = 0;
            foreach (Item item in World.Items.Values)
                if (item.Map == null)
                    items_null_map++;
                else if (item.Map == Map.Internal)
                    items_internal_map++;

            e.Mobile.SendMessage("There are {0} total items", itemCount);
            e.Mobile.SendMessage("There are {0} items on the internal map", items_internal_map);
            e.Mobile.SendMessage("There are {0} items on the null map", items_null_map);
            e.Mobile.SendMessage("There are {0} items in world", itemCount - (items_internal_map + items_null_map));
        }
        [Usage("ClearContainers")]
        [Description("Clears contents of all containers everywhere.")]
        public static void ClearContainers_OnCommand(CommandEventArgs e)
        {
            Console.WriteLine("Beginning Container Clearing.");
            e.Mobile.Say("Beginning Container Clearing.");

            ArrayList list = new ArrayList();
            int count = 0;
            int containers = 0;
            foreach (Item item in World.Items.Values)
                if (item is Container && item is BaseGameBoard == false && item.RootParent is Mobile == false)
                {
                    containers++;
                    foreach (Item toDelete in item.Items)
                        list.Add(toDelete);
                }

            if (list.Count > 0)
                for (int i = list.Count - 1; i > 0; i--)
                {
                    Item ix = list[i] as Item;
                    if (ix != null && ix.Deleted == false)
                    {
                        Container con = ix.Parent as Container;
                        if (con != null)
                        {
                            con.RemoveItem(ix);
                            ix.Delete();
                            count++;
                        }
                    }
                }

            Console.WriteLine("Finished clearing {0} items {1} containers.", count, containers);
            e.Mobile.Say("Finished clearing {0} items {1} containers.", count, containers);
        }

        [Usage("Shard")]
        [Description("Reminds of the current shard configuraton.")]
        public static void Shard_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage(String.Format("You are on {0}{1}{2} ({3}).", Core.Server, Core.UOTC_CFG ? " Test Center" : "", Core.UOEV_CFG ? " Event Shard" : "", Environment.MachineName));
        }


        [Usage("ClearHarvestMemory")]
        [Description("Clear harvest system memory.")]
        public static void ClearHarvestMemory_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel <= AccessLevel.GameMaster)
            {
                //safety check
                return;
            }


            try
            {
                if (Engines.Harvest.Mining.System.ClearBankMemory(e.Mobile))
                {
                    e.Mobile.SendMessage("Cleared mining memory");
                }
                if (Engines.Harvest.Lumberjacking.System.ClearBankMemory(e.Mobile))
                {
                    e.Mobile.SendMessage("Cleared lumberjacking memory");
                }
                if (Engines.Harvest.Fishing.System.ClearBankMemory(e.Mobile))
                {
                    e.Mobile.SendMessage("Cleared fishing memory");
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("ERROR clearing banks: {0}", ex.Message);
            }
        }

    }
}