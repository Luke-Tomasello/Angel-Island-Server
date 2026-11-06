/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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
 *  10/22/2026, Adam ([go)
 *      Player-level Go (Test Center "go" speech command and the Go gump): players see and resolve only the player
 *      location tree, and every Go move is checked by PlayerMayGo: no change of facet, and on Trammel (Map.TestCenter)
 *      only destinations inside the new player starting area. Staff behavior is unchanged.
 *  10/18/2026, Adam ([bank)
 *      The bank box count shown to staff uses Container.ItemCountText (capacity slots appended when they differ).
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
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Misc;
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

            Register("Go", AccessLevel.GameMaster, new CommandEventHandler(Go_OnCommand));

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

            if (!Core.RuleSets.MortalisRules())
                Register("Echo", AccessLevel.Player, new CommandEventHandler(Echo_OnCommand));

            Register("Sound", AccessLevel.GameMaster, new CommandEventHandler(Sound_OnCommand));

            Register("ViewEquip", AccessLevel.GameMaster, new CommandEventHandler(ViewEquip_OnCommand));

            Register("Light", AccessLevel.Counselor, new CommandEventHandler(Light_OnCommand));
            Register("Stats", AccessLevel.Player, new CommandEventHandler(Stats_OnCommand));

            Register("Mobiles", AccessLevel.Administrator, new CommandEventHandler(Mobiles_OnCommand));
            Register("Items", AccessLevel.Administrator, new CommandEventHandler(Items_OnCommand));

            Register("ReplaceBankers", AccessLevel.Administrator, new CommandEventHandler(ReplaceBankers_OnCommand));

            //if (!Core.RuleSets.MortalisStyleRules())
            //    Register("Reward", AccessLevel.Player, new CommandEventHandler(RewardSystem.Reward_OnCommand));

            Register("ClearContainers", AccessLevel.Administrator, new CommandEventHandler(ClearContainers_OnCommand));

            Register("Shard", AccessLevel.Counselor, new CommandEventHandler(Shard_OnCommand));

            Register("ClearHarvestMemory", AccessLevel.Administrator, new CommandEventHandler(ClearHarvestMemory_OnCommand));
        }

        public static void Register(string command, AccessLevel access, CommandEventHandler handler)
        {
            Server.CommandSystem.Register(command, access, handler);
        }

        #region Where
        [Usage("Where [name]")]
        [Description("Tells the commanding player his coordinates, region, and facet. Optionally specify a mobile name to locate them and show distance.")]
        public static void Where_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (e.Arguments.Length == 0)
            {
                // Original behavior - show info about self
                List<string> list = new List<string>();
                Where(from, from.Map, list, isSelf: true);
                foreach (string s in list)
                    from.SendMessage(s);
            }
            else
            {
                // Target a specific mobile by name
                string searchName = e.Arguments[0];
                Mobile? target = null;

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m != null && m.Name != null && Insensitive.Equals(m.Name, searchName))
                    {
                        target = m;
                        break;
                    }
                }

                if (target == null)
                {
                    from.SendMessage($"Could not find any mobile named '{searchName}'.");
                    return;
                }

                List<string> list = new List<string>();
                Where(target, target.Map, list, isSelf: false);

                // Add distance from command user
                double distance = from.GetDistanceToSqrt(target);
                list.Add(string.Format("Distance from you: {0:F1} tiles.", distance));

                foreach (string s in list)
                    from.SendMessage(s);
            }
        }

        public static void Where(IEntity ent, Map map, List<string> output, bool isSelf = true)
        {
            if (ent is Item item && item.RootParent != null)
                ent = item.RootParent as IEntity;

            if (ent == null)
                return;

            string subject = isSelf ? "You are" : "They are";

            output.Add(string.Format("{0} at {1} {2} {3} ({4}) in {5}.",
                subject, ent.X, ent.Y, ent.Z, Sextant.Where(ent), map));

            string area = "unknown";
            if (Utility.WorldMap.BritMainLandWrap.Contains(ent.Location))
                area = "Britain";
            else if (Utility.WorldMap.LostLandsWrap.Contains(ent.Location))
                area = "Lost Lands";
            else if (Utility.WorldMap.Dungeons.Contains(ent.Location))
                area = "Dungeons";

            output.Add(string.Format(" in area '{0}'.", area));

            if (map != null)
            {
                try
                {
                    ArrayList reglist = Region.FindAll(ent.Location, map);
                    ArrayList multlist = BaseMulti.FindAll(ent.Location, map);

                    foreach (Region rx in reglist)
                    {
                        if (rx is HouseRegion hr)
                        {
                            string text = string.Format("Region is a {0} at {1} id {2}.", hr.GetType().Name, hr.GoLocation, rx.UId);
                            if (Region.IsHighestPriority(rx, ent.Location, map))
                                output.Add(string.Format("(Highest priority) {0}", text));
                            else
                                output.Add(string.Format("{0}", text));
                        }
                        else if (rx is CustomRegion)
                        {
                            CustomRegionControl rc = ((CustomRegion)rx).Controller;
                            Point3D location = rc?.Location ?? new Point3D(0, 0, 0);

                            string inn = string.Empty;
                            foreach (Rectangle3D px in rx.InnBounds)
                                if (px.Contains(ent))
                                {
                                    inn = "(Inn)";
                                    break;
                                }

                            string text = $"Region is {rx}{inn} with a Dynamic controller at {location} [{map}] ({rc?.Serial}) id {rx.UId}.";
                            if (Region.IsHighestPriority(rx, ent.Location, map))
                                output.Add(string.Format("(Highest priority) {0}", text));
                            else
                                output.Add(string.Format("{0}", text));
                        }
                        else if (rx is StaticRegion && StaticRegion.Controller(rx as StaticRegion) != null)
                        {
                            StaticRegionControl? rc = StaticRegion.Controller(rx as StaticRegion);
                            if (rc != null)
                            {
                                Point3D location = rc.Location;
                                string inn = string.Empty;
                                foreach (Rectangle3D px in rx.InnBounds)
                                    if (px.Contains(ent))
                                    {
                                        inn = "(Inn)";
                                        break;
                                    }

                                string text = string.Format("Region is {0}{4} with a Static controller at {1}({3}) id {2}.", rx, location, rx.UId, rc.Serial, inn);
                                if (Region.IsHighestPriority(rx, ent.Location, map))
                                    output.Add(string.Format("(Highest priority) {0}", text));
                                else
                                    output.Add(string.Format("{0}", text));
                            }
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
                            string text = string.Format("Region is {0} (DefaultRegion) id {1}.", rx, rx.UId);
                            if (Region.IsHighestPriority(rx, ent.Location, map))
                                output.Add(string.Format("(Highest priority) {0}", text));
                            else
                                output.Add(string.Format("{0}", text));
                        }
                    }

                    foreach (BaseMulti bx in multlist)
                    {
                        if (bx != null)
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
                Item held = targ.Dragging;

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

                    targ.Dragging = null;
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
                    object? obj = list[i];

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
                    string.Format("You are about to delete {0} object{1} from this facet.  Do you really wish to continue?",
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
                        Mobile? pet = (Mobile?)pets[i];

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

                    entries[i] = new ItemListEntry(string.Format("{0}: {1}", item.Layer, item.GetType().Name), item.ItemID, item.Hue);
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
                        : base(string.Format("{0}: {1}", item.Layer, item.GetType().Name), new string[] { "Move", "Delete", "Props" })
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
                            m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, true, string.Format("Bank container has {0}, {1} stones", box.ItemCountText, box.TotalWeight), from.NetState);
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
                    from.SendMessage(string.Format("RangeExit is {0} tiles.", from.GetDistanceToSqrt(targeted as Mobile)));
                else if (targeted is Item)
                    from.SendMessage(string.Format("RangeExit is {0} tiles.", from.GetDistanceToSqrt((targeted as Item).Location)));
                else
                    from.SendMessage(string.Format("RangeExit is Unknown."));
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
            // Block manual saves while maintenance/serverwars is in flight.
            if (Maintenance.Scheduled)
            {
                e.Mobile.SendMessage("Save cannot be run while maintenance is scheduled or Server Wars are in progress. Cancel maintenance first, or wait until it completes.");
                return;
            }

            Misc.AutoSave.Save();
        }

        private static bool FixMap(ref Map map, ref Point3D loc, Item item)
        {
            if (map == null || map == Map.Internal)
            {
                Mobile? m = item.RootParent as Mobile;

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
        private class GoMarkResult
        {
            public Point3D Location;
            public Map? Map;
        }

        [Usage("Go [name | serial | player | (x y [z]) | (deg min (N | S) deg min (E | W))]")]
        [Description(
            "With no arguments, this command brings up the go menu.\n" +
            "\n" +
            "You may travel using one of several formats:\n" +
            "\n" +
            "- Name: Travels to a location defined in the go menu or world signs (supports multi-word names, e.g. 'West Britain Bank').\n" +
            "- Serial: Travels to an object or mobile by serial (decimal or hex, e.g. 0x40012345; staff only).\n" +
            "- Player: Travels to a player by name (requires equal or higher access level; players may not cross maps).\n" +
            "- Coordinates: Travels to (x y) or (x y z) on your current map.\n" +
            "- Sextant: Travels using sextant coordinates on your current map.\n" +
            "- Mark: If a rune, runebook entry, or moonstone in your backpack matches the name, it will be used.\n" +
            "\n" +
            "Name resolution priority:\n" +
            "  Mark (runebook/rune) -> Go menu locations -> Sign locations -> Player\n"
        )]
        private static void Go_OnCommand(CommandEventArgs e)
        {
            ExecuteGo(e.Mobile, e.ArgString);
        }

        public static void ExecuteGo(Mobile from, string raw)
        {
            if (from == null)
                return;

            from.LightLevel = 100;

            if (raw == null)
                raw = "";

            // we accept coords like
            // [go (4406, 1032, 1)
            raw = raw.Trim(' ', '(', ')');

            // No args: open the Go gump (players see only player-accessible destinations).
            if (raw.Length == 0)
            {
                if (from.AccessLevel == AccessLevel.Player)
                    GoGump.DisplayTo(from, playerView: true);
                else
                    GoGump.DisplayTo(from);
                return;
            }

            // Normalize comma-delimited coordinates:
            // [go 4406,1032
            // [go 4406, 1032, 1
            string normalized = raw.Replace(",", " ");
            string[] args = normalized.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // 1. One numeric argument: treat as serial.
            //    Important: only do this for exactly one argument so names like
            //    "west brit bank" are not affected.
            if (args.Length == 1)
            {
                int? ser = Utility.ToInt32Nullable(args[0]);

                if (ser.HasValue)
                {
                    if (from.AccessLevel == AccessLevel.Player)
                    {
                        from.SendMessage("You cannot go to serials.");
                        return;
                    }

                    if (GoToSerial(from, ser.Value))
                        return;

                    if (ser.Value != 0)
                    {
                        from.SendMessage("No object with that serial was found.");
                        return;
                    }
                }
            }

            // 2. Clear coordinate forms:
            //    [go x y
            //    [go x y z
            //    [go x,y
            //    [go x,y,z
            //
            //    We only treat this as coordinates if the input yielded exactly
            //    2 or 3 integers AND every argument token is numeric-ish.
            if (args.Length == 2 || args.Length == 3)
            {
                if (AllTokensAreInteger(args))
                {
                    Map map = from.Map;

                    if (map != null)
                    {
                        int[] ints = Utility.IntParser(normalized);

                        if (ints.Length == 2)
                        {
                            int x = ints[0];
                            int y = ints[1];

                            Point3D dest = new Point3D(x, y, map.GetAverageZ(x, y));
                            if (PlayerMayGo(from, dest, map))
                                from.Location = dest;
                            return;
                        }

                        if (ints.Length == 3)
                        {
                            Point3D dest = new Point3D(ints[0], ints[1], ints[2]);
                            if (PlayerMayGo(from, dest, map))
                                from.Location = dest;
                            return;
                        }
                    }
                }
            }

            // 3. Sextant forms.
            //    Old style:
            //      [go 55 54 N 72 54 W
            //
            //    New style is also delegated to Sextant.Parse.
            if (LooksLikeSextant(normalized))
            {
                Map map = from.Map;

                if (map != null)
                {
                    Point3D p = Sextant.Parse(map, normalized);

                    if (p != Point3D.Zero)
                    {
                        if (PlayerMayGo(from, p, map))
                            from.Location = p;
                        return;
                    }

                    from.SendMessage("Sextant reverse lookup failed.");
                    return;
                }
            }

            // 4. Dynamic marked locations.
            //    This preserves the useful behavior where a rune/runebook entry
            //    named "Home" can be reached with:
            //
            //      [go home
            //
            //    Mark locations intentionally win over LocationTree names.
            GoMarkResult? mark = FindMarkLocation(from, raw);

            if (mark != null && mark.Map != null && mark.Map != Map.Internal)
            {
                if (from.AccessLevel == AccessLevel.Player && mark.Map != from.Map)
                {
                    from.SendMessage("You cannot go to marked locations on another map.");
                    return;
                }

                if (PlayerMayGo(from, mark.Location, mark.Map))
                    from.MoveToWorld(mark.Location, mark.Map);
                return;
            }

            // 5. LocationTree name resolution.
            //    This is the new multi-word path:
            //
            //      [go west brit bank
            //      [go destard entrance
            //      [go chaos shrine
            LocationTree tree = GoGump.GetTreeFor(from.Map, playerView: from.AccessLevel == AccessLevel.Player);

            if (tree != null && tree.Root != null)
            {
                ChildNode? node = tree.Resolve(raw);

                if (node != null)
                {
                    Map targetMap = node.Map ?? tree.Map;

                    // Preserve existing Felucca XML -> Trammel/TestCenter tree behavior.
                    if (node.Map == Map.Felucca && tree.Map == Map.Trammel)
                        targetMap = Map.Trammel;

                    if (targetMap != null && targetMap != Map.Internal)
                    {
                        if (from.AccessLevel == AccessLevel.Player && targetMap != from.Map)
                        {
                            from.SendMessage("You cannot go to locations on another map.");
                            return;
                        }

                        if (PlayerMayGo(from, node.Location, targetMap))
                            from.MoveToWorld(node.Location, targetMap);
                        return;
                    }
                }
            }

            // 6. region name resolution.
            //    We restrict this to staff since it allows access to jail, proson, green acres, etc
            var region = Region.FindByName(raw, from.Map);
            if (from.AccessLevel > AccessLevel.Player && region != null)
            {
                from.MoveToWorld(region.GoLocation, from.Map);
                return;
            }

            // 7. Sign name resolution.
            //    Signs are loaded from data/signs.cfg and resolved through cliloc labels.
            //    LocationTree intentionally wins over sign names.
            SignParser.SignLocation? sign = SignParser.FindSignLocation(from, raw);

            if (sign != null && sign.Map != null && sign.Map != Map.Internal)
            {
                if (from.AccessLevel == AccessLevel.Player && sign.Map != from.Map)
                {
                    from.SendMessage("You cannot go to sign locations on another map.");
                    return;
                }

                if (PlayerMayGo(from, sign.Location, sign.Map))
                    from.MoveToWorld(sign.Location, sign.Map);
                return;
            }

            // 8. Player name resolution (last fallback)
            GoPlayerResult? player = FindPlayerByName(raw);

            if (player != null)
            {
                PlayerMobile target = player.Player;

                if (target.AccessLevel > from.AccessLevel)
                {
                    from.SendMessage("You cannot go to that player.");
                    return;
                }

                if (from.AccessLevel == AccessLevel.Player && player.Map != from.Map)
                {
                    from.SendMessage("You cannot go to players on another map.");
                    return;
                }

                if (PlayerMayGo(from, player.Location, player.Map))
                    from.MoveToWorld(player.Location, player.Map);
                return;
            }

            from.SendMessage("No location with that name was found.");
        }

        /// <summary>
        /// Go travel rules for player-level users (the Test Center "go" speech command and the Go gump); staff are not
        /// restricted. A player may not change facets, and on Trammel (Map.TestCenter) may only go to destinations inside
        /// the new player starting area, the one part of Test Center open to players.
        /// </summary>
        public static bool PlayerMayGo(Mobile from, Point3D dest, Map map)
        {
            if (from == null || from.AccessLevel > AccessLevel.Player)
                return true;

            if (map != from.Map)
            {
                from.SendMessage("You cannot go to locations on another map.");
                return false;
            }

            if (map == Map.TestCenter && !Utility.WorldMap.NewPlayerStartingArea.Contains(dest))
            {
                from.SendMessage("You cannot go to that location.");
                return false;
            }

            return true;
        }
        private static GoMarkResult? FindMarkLocation(Mobile m, string description)
        {
            if (m == null || m.Backpack == null || string.IsNullOrWhiteSpace(description))
                return null;

            ArrayList list = m.Backpack.FindAllItems();

            foreach (Item item in list)
            {
                if (item is RecallRune rune)
                {
                    if (rune.Marked && rune.TargetMap != null && rune.Description != null &&
                        rune.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                    {
                        return new GoMarkResult
                        {
                            Location = rune.Target,
                            Map = rune.TargetMap
                        };
                    }
                }
                else if (item is Moonstone stone)
                {
                    if (stone.Marked && stone.Description != null &&
                        stone.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                    {
                        return new GoMarkResult
                        {
                            Location = stone.Destination,
                            Map = m.Map // moonstone has no map info
                        };
                    }
                }
                else if (item is Runebook book)
                {
                    for (int i = 0; i < book.Entries.Count; ++i)
                    {
                        RunebookEntry? entry = (RunebookEntry?)book.Entries[i];

                        if (entry.Map != null && entry.Description != null &&
                            entry.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                        {
                            return new GoMarkResult
                            {
                                Location = entry.Location,
                                Map = entry.Map
                            };
                        }
                    }
                }
            }

            return null;
        }
        private class GoPlayerResult
        {
            public PlayerMobile Player;
            public Point3D Location;
            public Map Map;
        }
        private static GoPlayerResult? FindPlayerByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Trim();

            // Prefer online players.
            foreach (NetState ns in NetState.Instances)
            {
                Mobile m = ns.Mobile;

                PlayerMobile? pm = m as PlayerMobile;

                if (pm == null || pm.Deleted)
                    continue;

                if (!Insensitive.Equals(pm.Name, name))
                    continue;

                return BuildGoPlayerResult(pm);
            }

            // Fallback: known PlayerMobiles, including offline/internal players.
            foreach (PlayerMobile pm in PlayerMobile.Instances)
            {
                if (pm == null || pm.Deleted)
                    continue;

                if (!Insensitive.Equals(pm.Name, name))
                    continue;

                return BuildGoPlayerResult(pm);
            }

            return null;
        }

        private static GoPlayerResult? BuildGoPlayerResult(PlayerMobile pm)
        {
            if (pm == null || pm.Deleted)
                return null;

            Map map = pm.Map;
            Point3D loc = pm.Location;

            // If the player is internal/offline, use last logout position.
            if (map == null || map == Map.Internal)
            {
                map = pm.LogoutMap;
                loc = pm.LogoutLocation;
            }

            if (map == null || map == Map.Internal)
                return null;

            return new GoPlayerResult
            {
                Player = pm,
                Location = loc,
                Map = map
            };
        }
        private static bool AllTokensAreInteger(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                int value;

                if (!int.TryParse(args[i], out value))
                    return false;
            }

            return true;
        }
        private static bool LooksLikeSextant(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // The old supported form contains cardinal direction tokens.
            // Keep this intentionally broad and let Sextant.Parse decide validity.
            string[] args = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            bool hasNorthSouth = false;
            bool hasEastWest = false;

            for (int i = 0; i < args.Length; i++)
            {
                string s = args[i];

                if (Insensitive.Equals(s, "N") || Insensitive.Equals(s, "S"))
                    hasNorthSouth = true;
                else if (Insensitive.Equals(s, "E") || Insensitive.Equals(s, "W"))
                    hasEastWest = true;
            }

            return hasNorthSouth && hasEastWest;
        }
        private static bool GoToSerial(Mobile from, int ser)
        {
            IEntity? ent = World.FindEntity(ser);

            if (ent is Item)
            {
                Item item = (Item)ent;

                Map map = item.Map;
                Point3D loc = item.GetWorldLocation();

                Mobile? owner = item.RootParent as Mobile;

                if (owner != null && (owner.Map != null && owner.Map != Map.Internal) && !from.CanSee(owner))
                {
                    from.SendMessage("You can not go to what you can not see.");
                    return true;
                }
                else if (owner != null && (owner.Map == null || owner.Map == Map.Internal) && owner.Hidden && owner.AccessLevel >= from.AccessLevel)
                {
                    from.SendMessage("You can not go to what you can not see.");
                    return true;
                }
                else if (!FixMap(ref map, ref loc, item))
                {
                    from.SendMessage("That is an internal item and you cannot go to it.");
                    return true;
                }

                from.MoveToWorld(loc, map);
                return true;
            }

            if (ent is Mobile)
            {
                Mobile m = (Mobile)ent;

                Map map = m.Map;
                Point3D loc = m.Location;

                Mobile owner = m;

                if (owner != null && (owner.Map != null && owner.Map != Map.Internal) && !from.CanSee(owner))
                {
                    from.SendMessage("You can not go to what you can not see.");
                    return true;
                }
                else if (owner != null && (owner.Map == null || owner.Map == Map.Internal) && owner.Hidden && owner.AccessLevel >= from.AccessLevel)
                {
                    from.SendMessage("You can not go to what you can not see.");
                    return true;
                }
                else if (!FixMap(ref map, ref loc, m))
                {
                    from.SendMessage("That is an internal mobile and you cannot go to it.");
                    return true;
                }

                from.MoveToWorld(loc, map);
                return true;
            }

            return false;
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
                string? v = ((CommandEntry)list[i]).Command;
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
            BroadcastMessage(AccessLevel.Counselor, e.Mobile.SpeechHue, string.Format("[{0}] {1}", e.Mobile.Name, e.ArgString));
        }

        [Usage("BCast <text>")]
        [Aliases("B", "BC")]
        [Description("Broadcasts a message to everyone online.")]
        public static void BroadcastMessage_OnCommand(CommandEventArgs e)
        {
            BroadcastMessage(AccessLevel.Player, 0x482, string.Format("Staff message from {0}:", e.Mobile.Name));
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
                    Item? item = ((Mobile)targeted).FindItemOnLayer(m_Layer);

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

                Spell? spell = SpellRegistry.NewSpell(e.GetString(0), e.Mobile, null);

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
                    Item? ix = list[i] as Item;
                    if (ix != null && ix.Deleted == false)
                    {
                        Container? con = ix.Parent as Container;
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

        [Usage("Shard [List | <shard name> Hidden <true|false>]")]
        [Description("Displays the current shard configuration or manages shard visibility.")]
        public static void Shard_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length == 0)
            {
                e.Mobile.SendMessage(
                    "You are on {0}{1}{2} ({3}).",
                    Core.Server,
                    Core.GMNTC_CFG ? " Test Center" : "",
                    Core.GMNEV_CFG ? " Event Shard" : "",
                    Environment.MachineName);

                return;
            }

            if (e.Arguments.Length == 1 &&
                e.Arguments[0].Equals("List", StringComparison.OrdinalIgnoreCase))
            {
                DisplayShardList(e.Mobile);
                return;
            }

            // Changing shard visibility is an Owner-level operation.
            if (e.Mobile.AccessLevel < AccessLevel.Owner)
            {
                e.Mobile.SendMessage("You do not have access to modify shard visibility.");
                return;
            }

            if (e.Arguments.Length >= 3 &&
                e.Arguments[^2].Equals("Hidden", StringComparison.OrdinalIgnoreCase) &&
                bool.TryParse(e.Arguments[^1], out bool hidden))
            {
                // Everything preceding "Hidden" is the shard name. This allows
                // names containing spaces whether or not the command parser
                // preserves quoted arguments.
                string shardName = string.Join(" ", e.Arguments, 0, e.Arguments.Length - 2);

                Server.Misc.ServerList.ShardEntry shard =
                    Server.Misc.ServerList.FindShard(shardName);

                if (shard == null)
                {
                    e.Mobile.SendMessage("Unknown shard '{0}'. Use [Shard List for valid shard names.", shardName);
                    return;
                }

                shard.Visible = !hidden;

                e.Mobile.SendMessage(
                    "{0}, port {1}, status: {2}",
                    shard.Name,
                    shard.Port,
                    shard.Visible ? "visible" : "hidden");

                return;
            }

            e.Mobile.SendMessage("Usage:");
            e.Mobile.SendMessage("[Shard");
            e.Mobile.SendMessage("[Shard List");
            e.Mobile.SendMessage("[Shard <shard name> Hidden <true|false>");
        }
        public static void DisplayShardList(Mobile from)
        {
            foreach (Server.Misc.ServerList.ShardEntry shard in Server.Misc.ServerList.Shards)
            {
                from.SendMessage(
                    "{0}, port {1}, status: {2}",
                    shard.Name,
                    shard.Port,
                    shard.Visible ? "visible" : "hidden");
            }

            from.SendMessage(
                "Administrative, port {0}, status: owner only",
                SocketOptions.LoginServerPortBase);
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