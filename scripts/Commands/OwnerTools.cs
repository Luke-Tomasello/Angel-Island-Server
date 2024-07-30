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

/* Scripts/Commands/OwnerTools.cs
 * CHANGELOG:
 *  3/16/23, Adam ([PreLaunch)
 *      Add 'Initialize the ResourcePool' to steps.
 *      We don't want resources from another shard appearing on a new shard.
 *  9/29/22, Adam (loadjump)
 *      normalize the Z when loading a jump list from a region controller. (otherwise we end up at like -128 Z)
 *  9/22/22, Adam (PreserveTeleporter & DeleteAllTeleporters)
 *      More teleporter work. Delete's not interesting, but Preserve is.
 *      PreserveTeleporter is used after Nuke.AnalyzeTeleporters. Nuke.AnalyzeTeleporters generated a Jump list of teleporters to investigate (loads the players Jump List.)
 *      While reviewing the teleporter, you'll call PreserveTeleporter to indicate it is a keeper.
 *      The output 'patch file' will be used by Patcher.cs to patch the shards with the appropriate patch file.
 *  9/15/22, Adam (FindBuggedMap)
 *      Added [FindBuggedMap to locate bugged map tiles.
 *      add [GetAverageZ
 *  9/14/22, Adam (LoadJump)
 *      [loadjump now accepts RegionControl as a target
 *  9/13/22, Adam
 *      Added a StackDepth command. Counts items at targeted X/Y
 *      Enhance DistanceToSqrt to accept one or more points, 2D or 3D (although, Z is always thrown away.)
 *  8/5/22, Adam (IsOwnerIP())
 *      Owner IP addresses are granted special server access:
 *      - Access to the Login Server from the server list
 *      - Character creation on this shard is automatically made an administrator.
 *      Note: we need this since we no longer have (working) IPExceptions and we need to be able to create an account
 *          there for individuals in special circumstances. 
 *  2/19/22, Adam (TileOverload)
 *      Looks for, and deletes excessive items on a single tile
 *  12/11/21 (ItemProfiler)
 *      Add an item profiler command to search out and identify excessive item generation.
 *	3/15/16, Adam
  *		Initial Version
 */

using NAudio.Wave;
using Server.Accounting;
using Server.Diagnostics;
using Server.Engines.ChampionSpawn;
using Server.Engines.ResourcePool;
using Server.Gumps;
using Server.Items;
using Server.Items.Triggers;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TextCopy;
using static Server.Utility;

namespace Server.Commands
{
    public class OwnerTools
    {
        public static void Initialize()
        {
            CommandSystem.Register("RemoveBackpackHue", AccessLevel.Player, new CommandEventHandler(RemoveBackpackHue_OnCommand));
            CommandSystem.Register("SaveBackpack", AccessLevel.Player, new CommandEventHandler(SaveBackpack_OnCommand));
            CommandSystem.Register("RestoreBackpack", AccessLevel.Player, new CommandEventHandler(RestoreBackpack_OnCommand));
            CommandSystem.Register("RefreshBankBox", AccessLevel.Owner, new CommandEventHandler(RefreshBankBox_OnCommand));
            CommandSystem.Register("DistanceToSqrt", AccessLevel.GameMaster, new CommandEventHandler(DistanceToSqrt_OnCommand));
            CommandSystem.Register("LoadJump", AccessLevel.GameMaster, new CommandEventHandler(LoadJump_OnCommand));
            CommandSystem.Register("LoadMEOPJump", AccessLevel.GameMaster, new CommandEventHandler(LoadMEOPJump_OnCommand));
            CommandSystem.Register("PreLaunch", AccessLevel.Owner, new CommandEventHandler(PreLaunch_OnCommand));
            CommandSystem.Register("RecycleBin", AccessLevel.Owner, new CommandEventHandler(RecycleBin_OnCommand));
            CommandSystem.Register("Clobber", AccessLevel.Owner, new CommandEventHandler(Clobber_OnCommand));
            CommandSystem.Register("Monitor", AccessLevel.Owner, new CommandEventHandler(Monitor_OnCommand));
            CommandSystem.Register("ItemProfiler", AccessLevel.Owner, new CommandEventHandler(ItemProfiler_OnCommand));
            CommandSystem.Register("TileOverload", AccessLevel.Owner, new CommandEventHandler(TileOverload_OnCommand));
            CommandSystem.Register("SeedAccounts", AccessLevel.Owner, new CommandEventHandler(SeedAccounts_OnCommand));
            CommandSystem.Register("CanSpawnMobileZ", AccessLevel.Administrator, new CommandEventHandler(CanSpawnMobileZ_OnCommand));
            CommandSystem.Register("CanSpawnMobileT", AccessLevel.Owner, new CommandEventHandler(CanSpawnMobileT_OnCommand));
            CommandSystem.Register("FindBuggedMap", AccessLevel.Owner, new CommandEventHandler(FindBuggedMap_OnCommand));
            CommandSystem.Register("GetAverageZ", AccessLevel.Administrator, new CommandEventHandler(GetAverageZ_OnCommand));
            CommandSystem.Register("GetSurfaceTop", AccessLevel.Administrator, new CommandEventHandler(GetSurfaceTop_OnCommand));
            CommandSystem.Register("StackDepth", AccessLevel.Administrator, new CommandEventHandler(StackDepth_OnCommand));
            CommandSystem.Register("SectorBounds", AccessLevel.GameMaster, new CommandEventHandler(SectorBounds_OnCommand));
            CommandSystem.Register("GetSectorInfo", AccessLevel.GameMaster, new CommandEventHandler(GetSectorInfo_OnCommand));
            CommandSystem.Register("Colorize", AccessLevel.GameMaster, new CommandEventHandler(ColorizeRect_OnCommand));
            CommandSystem.Register("BoundingRect", AccessLevel.GameMaster, new CommandEventHandler(BoundingRect_OnCommand));
            CommandSystem.Register("ViewItems", AccessLevel.GameMaster, new CommandEventHandler(ViewItems_OnCommand));
            CommandSystem.Register("CopyItems", AccessLevel.GameMaster, new CommandEventHandler(CopyItems_OnCommand));
            CommandSystem.Register("LocationRecorder", AccessLevel.Owner, new CommandEventHandler(LocationRecorder_OnCommand));
            CommandSystem.Register("ConstructRect", AccessLevel.Owner, new CommandEventHandler(ConstructRect_OnCommand));
            CommandSystem.Register("GetNoto", AccessLevel.GameMaster, new CommandEventHandler(GetNoto_OnCommand));
            CommandSystem.Register("TimeToDie", AccessLevel.Owner, new CommandEventHandler(TimeToDie_OnCommand));
            CommandSystem.Register("WipeIPAddresses", AccessLevel.Administrator, new CommandEventHandler(WipeIPAddresses_OnCommand));
            CommandSystem.Register("WipeMachines", AccessLevel.Administrator, new CommandEventHandler(WipeMachines_OnCommand));
            CommandSystem.Register("SetAccess", AccessLevel.GameMaster, new CommandEventHandler(SetAccess_OnCommand));
            TargetCommands.Register(new MoveToIntMapStorageCommand());
            TargetCommands.Register(new MakeEventObjectPermanent());
            TargetCommands.Register(new ControlDebugger());
            CommandSystem.Register("LinkTransport", AccessLevel.GameMaster, new CommandEventHandler(LinkTransport_OnCommand));
            CommandSystem.Register("StretchRect", AccessLevel.GameMaster, new CommandEventHandler(StretchRect_OnCommand));
            CommandSystem.Register("CompileSpawnablePoints", AccessLevel.GameMaster, new CommandEventHandler(CompileSpawnablePoints_OnCommand));
            CommandSystem.Register("SwapStatics", AccessLevel.Seer, new CommandEventHandler(SwapStatics_OnCommand));
            CommandSystem.Register("AddRare", AccessLevel.Seer, new CommandEventHandler(AddRare_OnCommand));
        }
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }
        #region Add Rare
        private static void AddRare_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the item you would like to log as a rare...");
            e.Mobile.Target = new AddRareTarget(null);
        }
        public class AddRareTarget : Target
        {
            public AddRareTarget(string command)
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                int id = 0;
                string name = string.Empty;
                if (Utility.ObjectInfo(target, ref name, ref id))
                {
                    LogHelper logger = new LogHelper("AddRare.log", overwrite: false, sline: true, quiet: true);
                    string text = string.Format($"new CrownSterlingReward(itemO: {id}, baseCost: 5000, 0,label: null),  // {name}");
                    logger.Log(text);
                    logger.Finish();
                    from.SendMessage(text);

                    // cleanup duplicates
                    string[] lines = File.ReadAllLines(Path.Combine(Core.LogsDirectory, "AddRare.log"));
                    lines = lines.Distinct().ToArray();
                    File.WriteAllLines(Path.Combine(Core.LogsDirectory, "AddRare.log"), lines);
                }
                else
                {
                    from.SendMessage("That is not an item.");
                    return;
                }
            }
        }
        #endregion Add Rare
        #region Swap Statics
        private static void SwapStatics_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the top tile or yourself if you are standing on the tile...");
            e.Mobile.Target = new SwapStaticsTarget(null);
        }
        public class SwapStaticsTarget : Target
        {
            public SwapStaticsTarget(string command)
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Static || target is Mobile)
                {
                    IEntity top = target as IEntity;
                    List<Item> items = new();
                    OwnerTools.GetStack(top.X, top.Y, top.Map, items: items);
                    items.RemoveAll(x => Math.Abs(x.Z - top.Z) > 1);
                    items.RemoveAll(x => x is not Static);

                    if (items.Count == 2)
                    {
                        Static lower = new Static(1/*top.ItemID*/);
                        Static higher = new Static(1/*top.ItemID*/);

                        if (items[0].Serial > items[1].Serial)
                        {
                            Utility.CopyProperties(lower, items[0]);
                            Utility.CopyProperties(higher, items[1]);
                            lower.MoveToWorld(items[0].Location);
                            higher.MoveToWorld(items[1].Location);
                            items[0].Delete();
                            items[1].Delete();
                        }
                        else
                        {
                            Utility.CopyProperties(higher, items[0]);
                            Utility.CopyProperties(lower, items[1]);
                            higher.MoveToWorld(items[0].Location);
                            lower.MoveToWorld(items[1].Location);
                            items[0].MoveToIntStorage();
                            items[1].MoveToIntStorage();
                            items[0].Delete();
                            items[1].Delete();
                        }

                        from.SendEverything();
                        from.SendMessage($"Swapped {items.Count} static tiles.");
                    }
                    else
                        from.SendMessage($"Can't swap {items.Count} static tiles.");
                }
                else
                {
                    from.SendMessage("That is not a static tile.");
                    return;
                }
            }
        }
        #endregion Swap Statics
        #region Compile Spawnable Points
        [Usage("CompileSpawnablePoints")]
        [Description("CompileSpawnablePoints")]
        public static void CompileSpawnablePoints_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the FDRegionControl you wish to compile.");
            e.Mobile.Target = new CompileSpawnablePointsTarget(null);
        }
        public class CompileSpawnablePointsTarget : Target
        {
            public CompileSpawnablePointsTarget(object o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                List<Point2D> points = new List<Point2D>();
                List<Point2D> unique = new List<Point2D>();
                if (targeted is FDFile.FDRegionControl rc)
                {
                    Console.WriteLine("Spawnable Points Compiling...");
                    Utility.TimeCheck tc = new Utility.TimeCheck();
                    tc.Start();
                    List<Rectangle2D> rects = new();
                    foreach (var rect in rc.CustomRegion.Coords)
                        rects.Add(new Rectangle2D(rect.Start, rect.End));

                    List<Point2D> temp = new List<Point2D>();
                    foreach (Rectangle2D rectangle in rects)
                        foreach (var point in rectangle.PointsInRect2D())
                            temp.Add(point);

                    unique = temp.Distinct().ToList();

                    int unusable = 0;
                    foreach (var point in unique)
                        if (Utility.CanSpawnLandMobile(rc.Map, new Point3D(point.X, point.Y, rc.Map.GetAverageZ(point.X, point.Y))))
                            points.Add(point);
                        else
                            unusable++;

                    SaveSpawnablePoints(rc.CustomRegion.Name, points);
                    tc.End();
                    Console.WriteLine("Compiled {0} points with {1} usable in {2}", points.Count + unusable, points.Count, tc.TimeTaken);
                }
                else
                    from.SendMessage("That is not an FDRegionControl.");
            }

            public static void SaveSpawnablePoints(string filename, List<Point2D> points)
            {
                Console.WriteLine(string.Format("Saves/{0}.bin Saving...", filename));
                try
                {
                    BinaryFileWriter writer = new BinaryFileWriter(string.Format("Saves/{0}.bin", filename), true);

                    writer.Write(points.Count);

                    foreach (var point in points)
                        writer.Write(point);

                    writer.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Error writing Saves/{0}.bin", filename));
                    Console.WriteLine(ex.ToString());
                    return;
                }

                Console.WriteLine(string.Format("Done writing Saves/{0}.bin", filename));
            }
        }
        #endregion Compile Spawnable Points
        #region StretchRect
        [Usage("StretchRect")]
        [Description("StretchRect")]
        public static void StretchRect_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new StretchRectTarget(null);
        }
        public class StretchRectTarget : Target
        {
            public StretchRectTarget(object o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;

                int landZ = 0, landAvg = 0, landTop = 0;
                from.Map.GetAverageZ(loc.X, loc.Y, ref landZ, ref landAvg, ref landTop);
                if (loc != Point3D.Zero)
                {
                    Rectangle2D rect = new Rectangle2D(loc, new Point2D(loc.X + 1, loc.Y + 1));
                    rect = new SpawnableRect(rect).MaximumSpawnableRect(from.Map, 128, from.Map.GetAverageZ(rect.X, rect.Y));
                    from.SendMessage("Your max spawnable rect is {0}.", rect);
                    string text = string.Format("({0} {1}, {2}, {3})", rect.Start.X, rect.Start.Y, rect.End.X, rect.End.Y);
                    from.SendMessage(text);
                    ClipboardService.SetText(text);
                    Server.Gumps.EditAreaGump.FlashArea(from, rect, from.Map);
                }
                else
                    from.SendMessage("Can't grow a rect here.");
            }
        }
        #endregion StretchRect
        [Usage("LinkTransport")]
        [Description("Link a teleporter's MapDest and PointDest to <target>.")]
        public static void LinkTransport_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the spawner you would like to link...");
            e.Mobile.Target = new LinkTransportFirstTarget();
        }
        private class LinkTransportFirstTarget : Target
        {
            public LinkTransportFirstTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Teleporter transport = targeted as Teleporter;

                if (transport == null)
                {
                    from.SendMessage("You must select a teleporter or other transport object");
                }
                else
                {
                    from.SendMessage("Target the location you would like to link to...");
                    from.Target = new LinkTransportSecondTarget(transport);
                }
            }
        }

        private class LinkTransportSecondTarget : Target
        {
            private Teleporter m_teleporter;

            public LinkTransportSecondTarget(Teleporter transport)
                : base(-1, false, TargetFlags.None)
            {
                m_teleporter = transport;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                IPoint3D px = targeted as IPoint3D;

                if (px == null)
                {
                    from.SendMessage("Unable to resolve that location.");
                    return;
                }

                if (px is Item item)
                    px = item.GetWorldTop();
                else if (px is Mobile mobile)
                    px = mobile.Location;

                m_teleporter.PointDest = new Point3D(px);
                m_teleporter.MapDest = from.Map;
                from.SendMessage("Link established: {0} ({1}).", m_teleporter.PointDest, m_teleporter.MapDest);
            }
        }
        private class MakeEventObjectPermanent : BaseCommand
        {
            public MakeEventObjectPermanent()
            {
                AccessLevel = AccessLevel.Seer;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "MakeEventObjectPermanent", "MEOP" };
                ObjectTypes = ObjectTypes.Both;
                Usage = "MakeEventObjectPermanent <target> [-off]";
                Description = "Convert an Event Object (spawner, teleporter, etc.) to a Permanent Event Object.";
            }
            public override void Execute(CommandEventArgs e, object obj)
            {
                bool activate = true;
                if (e.GetString(0) == "-off")
                    activate = false;

                string reason;
                if (Utility.Meop(e.Mobile, obj, activate: activate, out reason))
                    e.Mobile.SendMessage(reason);
                else
                    LogFailure(reason);
            }
        }
        #region ControlDebugger
        private class ControlDebugger : BaseCommand
        {
            public ControlDebugger()
            {
                AccessLevel = AccessLevel.Seer;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "ControlDebugger", "CD" };
                ObjectTypes = ObjectTypes.Both;
                Usage = "ControlDebugger <target> [-link|-else <1|2|...>]";
                Description = "Display the links out of a controller and for the next N controllers.";
            }
            [Flags]
            public enum CDCommands
            {
                None = 0x00,
                Link = 0x01,
                Else = 0x02,
                Highlight = 0x04,
                Depth = 0x08,
                Label = 0x10,
                Jump = 0x20,
            }
            public class CDParms
            {
                private CDCommands m_Command;
                public CDCommands Command { get { return m_Command; } set { m_Command = value; } }
                private int m_Depth;
                public int Depth { get { return m_Depth; } set { m_Depth = value; } }
                public CDParms(CDCommands command, int depth)
                {
                    m_Command = command;
                    m_Depth = depth;
                }
            }
            public override void Execute(CommandEventArgs e, object obj)
            {
                string usage = "Usage: [cd -link|-else|-highlight|-depth <N>|-Label|-jump]";
                try
                {
                    if (obj is ITrigger || obj is TriggerRelay)
                    {
                        CDParms parms = new CDParms(CDCommands.None, 0);
                        Item trigger = obj as Item;

                        string argString;
                        if (e.HasCommand("-link", out argString))
                        {
                            parms.Command |= CDCommands.Link;
                        }
                        if (e.HasCommand("-else", out argString))
                        {
                            parms.Command |= CDCommands.Else;
                        }
                        if (e.HasCommand("-highlight", out argString))
                        {
                            parms.Command |= CDCommands.Highlight;
                        }
                        if (e.HasCommand("-depth", out argString))
                        {
                            parms.Command |= CDCommands.Depth;
                            if (!uint.TryParse(argString, out uint value))
                                throw new ArgumentException("-depth <N>");
                            parms.Depth = int.Parse(argString);
                        }
                        if (e.HasCommand("-Label", out argString))
                        {
                            parms.Command |= CDCommands.Label;
                        }
                        if (e.HasCommand("-jump", out argString))
                        {
                            parms.Command |= CDCommands.Jump;
                        }

                        if (parms.Command == CDCommands.None)
                            throw new ArgumentException(usage);

                        object[] args = null;
                        if (parms.Command.HasFlag(CDCommands.Depth))
                            args = new object[1] { parms.Depth };

                        ShowLinks(from: e.Mobile, trigger: trigger, command: parms.Command, args: args);
                    }
                    else
                        throw new ArgumentException("Not a triggerable object"); // LogFailure("Not a triggerable object");
                }
                catch (Exception ex)
                {
                    LogFailure(ex.Message);
                    if (ex.Message != usage)
                        e.Mobile.SendMessage(usage);
                }
            }
            private static void ShowLinks(Mobile from, Item trigger, CDCommands command, object[] args)
            {
                List<string> lines = new();

                int count = 128;                        // default depth if no depth is specified
                if (command.HasFlag(CDCommands.Depth))
                    count = (int)args[0];               // how deep we should look
                int depth = 0;                            // recursion protection

                // add the target. If it's a trigger relay, remove it
                List<Item> list = new() { trigger };
                if (trigger is TriggerRelay)
                    list.Clear();

                if (command.HasFlag(CDCommands.Jump) && from is PlayerMobile pm)
                {
                    pm.JumpIndex = 0;
                    pm.JumpList = new ArrayList();
                }

                // we will call ProcessLinks once for each traversal. This allows us to colorize based on path
                if (command.HasFlag(CDCommands.Link | CDCommands.Else))
                {
                    List<Item> LinkList = new(list);
                    List<Item> ElseList = new(list);
                    ProcessLinks(trigger, command & ~CDCommands.Else, LinkList, ref depth);
                    ProcessDisplay(from, command & ~CDCommands.Else, LinkList);
                    lines.AddRange(AssembleOutput(command & ~CDCommands.Else, LinkList));
                    depth = 0;                          // reset for a fresh traversal
                    ProcessLinks(trigger, command & ~CDCommands.Link, ElseList, ref depth);
                    ProcessDisplay(from, command & ~CDCommands.Link, ElseList);
                    lines.AddRange(AssembleOutput(command & ~CDCommands.Link, ElseList));
                }
                else
                {
                    ProcessLinks(trigger, command, list, ref depth);
                    ProcessDisplay(from, command, list);
                    lines.AddRange(AssembleOutput(command, list));
                }

                lines = lines.Take(count).ToList();     // trim to the requested depth

                // show the user
                from.CloseGump(typeof(ControlRelationshipVisualizerGump));
                from.SendGump(new ControlRelationshipVisualizerGump(from, lines));

                // tell the user about their jumplist
                if (command.HasFlag(CDCommands.Jump) && from is PlayerMobile staff && staff.JumpList.Count > 0)
                {
                    staff.SendMessage($"Your jumplist has been loaded with {staff.JumpList.Count}");
                    staff.SendMessage("Use the [next command to move to each object.");
                }
            }
            private static List<string> AssembleOutput(CDCommands command, List<Item> list)
            {
                List<string> lines = new();
                string text = string.Empty;
                for (int ix = 0; ix < list.Count; ix++)
                {
                    text += string.Format($"{list[ix].GetType().Name}({list[ix].Serial}) => ");
                    if (CheckEnd(command, list[ix]))
                    {
                        text += "(end)";            // add a tail node
                        //from.SendMessage(text);   // useful for debugging
                        lines.Add(text);            // another complete chain
                        text = string.Empty;        // fresh
                    }
                }
                return lines;
            }
            private static void ProcessDisplay(Mobile to, CDCommands command, List<Item> list)
            {
                // Highlight
                int blue = Utility.RandomSpecialHue(Utility.ColorSelect.Blue, Utility.GetStableHashCode(0));
                int gold = Utility.RandomSpecialHue(Utility.ColorSelect.Gold, Utility.GetStableHashCode(0));
                if (command.HasFlag(CDCommands.Highlight))
                    foreach (Item item in list)
                        item.Blink(command.HasFlag(CDCommands.Link) ? blue : gold);

                // label
                if (command.HasFlag(CDCommands.Label))
                    foreach (Item item in list)
                    {
                        int hue = Utility.RandomSpecialHue(item.GetType().ToString());
                        // LabelTo(from, '(' + Serial.ToString() + ')');
                        item.LabelToHued(to, item.GetType().Name, hue);
                        item.LabelToHued(to, '(' + item.Serial.ToString() + ')', hue);
                    }

                // jumplist
                if (command.HasFlag(CDCommands.Jump) && to is PlayerMobile pm)
                    foreach (Item item in list)
                        pm.JumpList.Add(item);
            }
            private static bool CheckEnd(CDCommands command, Item item)
            {
                if (item is TriggerRelay)
                {
                    return GetRelayLinks(item).Count == 0;
                }
                else
                {
                    string name = null;
                    if (command.HasFlag(CDCommands.Link))
                        name = "Link";
                    else if (command.HasFlag(CDCommands.Else))
                        name = "Else";

                    return GetLink(item, name) == null;
                }
            }
            private static void ProcessLinks(Item trigger, CDCommands command, List<Item> list, ref int depth)
            {
                if (depth++ > 100)
                    throw new ApplicationException("Too complex or self-referential controls not supported");

                string name = null;
                if (command.HasFlag(CDCommands.Link))
                    name = "Link";
                else if (command.HasFlag(CDCommands.Else))
                    name = "Else";

                if (trigger is TriggerRelay)
                {
                    List<Item> relays = GetRelayLinks(trigger);
                    foreach (Item relay_link in relays)
                    {
                        list.Add(trigger);
                        // Depth first: We follow each leg of the relay instead of list all legs up front
                        list.Add(relay_link);
                        ProcessLinks(relay_link, command, list, ref depth);
                    }
                }
                else
                {
                    Item temp = null;
                    Item new_trigger = trigger;
                    while ((temp = GetLink(new_trigger, name)) != null)
                    {
                        new_trigger = temp;
                        if (new_trigger is TriggerRelay)
                            ProcessLinks(new_trigger, command, list, ref depth);
                        else
                            list.Add(new_trigger);

                        if (new_trigger == null)
                            break;
                    }
                }
            }
            private static List<Item> GetRelayLinks(Item trigger)
            {
                string name = "Link1;Link2;Link3;Link4;Link5;Link6;";
                string[] prop_names = name.Split(';', StringSplitOptions.RemoveEmptyEntries);
                List<Item> list = new();
                Item temp = null;
                foreach (string prop_name in prop_names)
                    if ((temp = GetLink(trigger, prop_name)) != null)
                        list.Add(temp);
                return list;
            }
            private static Item GetLink(Item trigger, string prop_name)
            {
                string result = null;
                result = Server.Commands.Properties.GetValue(World.GetSystemAcct(), trigger, name: prop_name);
                if (result.Contains(Properties.PropNull) || result.Contains("not found"))
                    return null;
                return World.FindItem(Convert.ToInt32(KeyValue(result), 16));
            }
            private static readonly Regex SubStrKey = new(@"0[xX][0-9a-fA-F]+", RegexOptions.Compiled);
            private static string KeyValue(string s)
            {
                string result = SubStrKey.Match(s).Value;
                return result;
            }
        }
        #endregion ControlDebugger
        private class MoveToIntMapStorageCommand : BaseCommand
        {
            public MoveToIntMapStorageCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "MoveToIntMapStorage" };
                ObjectTypes = ObjectTypes.Both;
                Usage = "MoveToIntMapStorage <target>";
                Description = "Move the mobile/item to internal map storage.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Item item)
                {
                    item.MoveToIntStorage();
                }
                else if (obj is Mobile mobile)
                {
                    mobile.MoveToIntStorage();
                }
                else
                    LogFailure("That is not a mobile or an item");
            }
        }
        [Usage("WipeIPAddresses <account>")]
        [Description("WipeIPAddresses for the player account.")]
        public static void WipeIPAddresses_OnCommand(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
            {
                e.Mobile.SendMessage("WipeIPAddresses <account>");
                return;
            }

            Account acct = FindAccount(e.ArgString);
            if (acct == null)
            {
                e.Mobile.SendMessage("Cannot find account {0}", e.ArgString);
                return;
            }

            e.Mobile.SendMessage("This account has {0} login IP addresses", acct.LoginIPs.Length);
            acct.LoginIPs = new IPAddress[0];
            acct.ClearGAMELogin();
            e.Mobile.SendMessage("All IP addresses cleared.");
        }
        [Usage("WipeMachines <account>")]
        [Description("WipeMachines for the player account.")]
        public static void WipeMachines_OnCommand(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
            {
                e.Mobile.SendMessage("WipeMachines <account>");
                return;
            }

            Account acct = FindAccount(e.ArgString);
            if (acct == null)
            {
                e.Mobile.SendMessage("Cannot find account {0}", e.ArgString);
                return;
            }

            e.Mobile.SendMessage("This account has {0} machine signatures", acct.Machines.Count);
            acct.ClearMachines();
            e.Mobile.SendMessage("All machines cleared.");
        }
        private static Accounting.Account FindAccount(string username)
        {
            foreach (Accounting.Account a in Accounting.Accounts.Table.Values)
                if (a.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                    return a;
            return null;
        }
        [Usage("SetAccess <AccessLevel>")]
        [Description("Allow staff to downgrade their SccessLevel.")]
        public static void SetAccess_OnCommand(CommandEventArgs e)
        {
            AccessLevel al;
            string AccessLevel = "AccessLevel";
            try
            {
                al = (AccessLevel)Enum.Parse(typeof(AccessLevel), e.GetString(0), ignoreCase: true);
            }
            catch
            {
                goto usage;
            }

            if (e.Mobile.AccessLevel > al)
            {
                bool push_ok = true;
                string reason = string.Empty;
                // save their current access level so they can 'pop' it
                if (!Diagnostics.PropertyStackContains(e.Mobile, AccessLevel))
                    push_ok = Diagnostics.Push(e.Mobile as PlayerMobile, prop: AccessLevel, out reason);

                if (!push_ok)
                    e.Mobile.SendMessage(reason);
                else
                {
                    e.Mobile.AccessLevel = al;
                    e.Mobile.SendMessage("Use [Pop to recover your previous access level.");
                }
            }
            else if (e.Mobile.AccessLevel == al)
            {
                e.Mobile.SendMessage("AccessLevel unchanged.");
            }
            else
            {
                e.Mobile.SendMessage("You may not escalate your AccessLevel.");
            }

            return;

        usage:
            e.Mobile.SendMessage("SetAccess <Accesslevel>");
        }
        [Usage("TimeToDie")]
        [Description("Kills all BaseCreatures in the world.")]
        public static void TimeToDie_OnCommand(CommandEventArgs e)
        {
            foreach (Mobile m in World.Mobiles.Values)
                if (m is BaseCreature bc)
                    bc.TimeToDie(TimeSpan.FromMilliseconds(Utility.Random(1, 5000)));
        }
        #region Location Recorder
        [Usage("ConstructRect")]
        [Description("Build a rect from data collected by LocationRecorder.")]
        public static void ConstructRect_OnCommand(CommandEventArgs e)
        {
            try
            {   // gather the points
                List<Point2D> list2D = new();
                foreach (string line in File.ReadAllLines(Path.Combine(Core.LogsDirectory, "LocationRecorder.log")))
                    list2D.Add(Point2D.Parse(line));

                // construct the rectangle
                Rectangle2D rect = new Rectangle2D(list2D[0], list2D[1]);
                foreach (Point2D point in list2D)
                    rect.MakeHold(point);

                // let the user know
                e.Mobile.SendMessage("Your new rectangle is {0}", rect);
                e.Mobile.SendMessage("Start: {0}, End: {1}", rect.Start, rect.End);
            }
            catch
            {
                e.Mobile.SendMessage("LocationRecorder.log file missing or corrupt.");
            }
        }
        private static LogHelper LocationRecorder = null;
        private static Server.Timer LRTimer = null;
        private static Rectangle2D LRRect = new Rectangle2D();
        [Usage("LocationRecorder <on/off/record/clear>")]
        [Description("Records the mobiles location to a log file.")]
        public static void LocationRecorder_OnCommand(CommandEventArgs e)
        {
            if (e.GetString(0) == "on" || e.GetString(0) == "auto")
            {
                if (LocationRecorder != null)
                {
                    LocationRecorder.Finish();
                    LocationRecorder = null;
                }
                if (LRTimer != null)
                {
                    LRTimer.Stop();
                    LRTimer = null;
                }
                e.Mobile.SendMessage("Recorder started...");
                LocationRecorder = new LogHelper("LocationRecorder.log", overwrite: false, sline: true, quiet: true);
                if (e.GetString(0) == "auto")
                {
                    LRTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1), new TimerStateCallback(LocationTick), e.Mobile);
                    LRRect = new Rectangle2D(e.Mobile.X, e.Mobile.Y, 1, 1); // center on the mobile
                }
            }
            else if (e.GetString(0) == "off")
            {
                if (LocationRecorder != null)
                {
                    LocationRecorder.Finish();
                    LocationRecorder = null;
                    e.Mobile.SendMessage("Recorder stopped.");
                    e.Mobile.SendMessage("Find your recording in LocationRecorder.log");
                    if (LRTimer != null)
                    {
                        LRTimer.Stop();
                        LRTimer = null;
                    }
                }
            }
            else if (e.GetString(0) == "record")
            {
                if (LocationRecorder != null)
                {
                    e.Mobile.SendMessage("Target the location to record");
                    e.Mobile.Target = new LocationRecorderTarget();
                }
                else
                {
                    e.Mobile.SendMessage("Recorder not running.");
                    e.Mobile.SendMessage("Usage: LocationRecorder <on/off/record/clear>.");
                }
            }
            else if (e.GetString(0) == "clear")
            {
                if (LocationRecorder != null)
                    LocationRecorder.Finish();


                LocationRecorder = new LogHelper("LocationRecorder.log", overwrite: true, sline: true, quiet: true);
                LocationRecorder.Finish();
                e.Mobile.SendMessage("Recorder cleared.");
            }
            else
                e.Mobile.SendMessage("Usage: LocationRecorder <on/off/record/clear>.");
        }
        private static void LocationTick(object state)
        {
            Mobile m = state as Mobile;
            LRRect.MakeHold(new Point2D(m.Location));
            string text = string.Format("({0} {1}, {2}, {3})", LRRect.Start.X, LRRect.Start.Y, LRRect.End.X, LRRect.End.Y);
            m.SendMessage(text);
            ClipboardService.SetText(text);
        }
        public class LocationRecorderTarget : Target
        {
            public LocationRecorderTarget() : base(12, true, TargetFlags.None) {; }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                IPoint3D point = targeted as IPoint3D;
                string text = string.Format("{0}", new Point2D(point.X, point.Y));
                from.SendMessage("Logging:" + text);
                LocationRecorder.Log(text);
            }
        }
        #endregion Location Recorder
        [Usage("GetNoto")]
        [Description("Gets of the Notoriety of this creature.")]
        public static void GetNoto_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the Mobil you wish to check.");
            e.Mobile.Target = new GetMobileTarget(false);
        }
        public class GetMobileTarget : Target
        {
            private bool m_copy = false;
            public GetMobileTarget(object o) : base(12, true, TargetFlags.None) { m_copy = (bool)o; }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                string clipboard = string.Empty;
                if (mt == null)
                {
                    from.SendMessage("That is not a mobile.");
                    return;
                }

                int result = Server.Misc.NotorietyHandlers.MobileNotoriety(from, mt);
                switch (result)
                {
                    case Notoriety.Innocent:
                        from.SendMessage("Innocent");
                        break;
                    case Notoriety.Ally:
                        from.SendMessage("Ally");
                        break;
                    case Notoriety.CanBeAttacked:
                        from.SendMessage("CanBeAttacked");
                        break;
                    case Notoriety.Criminal:
                        from.SendMessage("Criminal");
                        break;
                    case Notoriety.Enemy:
                        from.SendMessage("Enemy");
                        break;
                    case Notoriety.Murderer:
                        from.SendMessage("Murderer");
                        break;
                    case Notoriety.Invulnerable:
                        from.SendMessage("Invulnerable");
                        break;
                    default:
                        from.SendMessage("Unknown");
                        break;
                }
            }
        }
        [Usage("ViewItems")]
        [Description("Views the 'Items' associated with this Mobile or Item.")]
        public static void ViewItems_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the Mobile or Item you wish to check.");
            e.Mobile.Target = new GetItemsTarget(false);
        }
        [Usage("CopyItems")]
        [Description("Copies the 'Items' associated with this Mobile or Item to the clipboard.")]
        public static void CopyItems_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the Mobile or Item you wish to check.");
            e.Mobile.Target = new GetItemsTarget(true);
        }
        public class GetItemsTarget : Target
        {
            private bool m_copy = false;
            public GetItemsTarget(object o) : base(12, true, TargetFlags.None) { m_copy = (bool)o; }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                string clipboard = string.Empty;
                if (mt != null)
                {
                    foreach (Item item in mt.Items)
                    {
                        from.SendMessage("{0}", item.ToString());
                        Console.WriteLine("{0}", item.ToString());
                        if (m_copy)
                            clipboard += item.ToString() + "\n";
                    }

                    if (m_copy)
                        ClipboardService.SetText(clipboard);

                    from.SendMessage("Total items: {0}", mt.Items.Count);
                    Console.WriteLine("Total items: {0}", mt.Items.Count);
                }
                else if (it != null)
                {
                    foreach (Item item in it.Items)
                    {
                        from.SendMessage("{0}", item.ToString());
                        Console.WriteLine("{0}", item.ToString());
                        if (m_copy)
                            clipboard += item.ToString() + "\n";
                    }

                    if (m_copy)
                        ClipboardService.SetText(clipboard);

                    from.SendMessage("Total items: {0}", it.Items.Count);
                    Console.WriteLine("Total items: {0}", it.Items.Count);
                }
                else
                    from.SendMessage("That is neither a mobile or item.");
            }
        }
        [Usage("Colorize")]
        [Description("Colorize all items in the bounding rect. This cannot be undone.")]
        public static void ColorizeRect_OnCommand(CommandEventArgs e)
        {
            if (e.Length != 1)
            {
                e.Mobile.SendMessage("Usage [Colorize <true|false>");
                return;
            }
            if (Core.UOTC_CFG == false)
            {
                e.Mobile.SendMessage("This command may only be run on test center");
                return;
            }
            try
            {
                BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(Colorize_Callback), e.GetBoolean(0));
            }
            catch
            {
                e.Mobile.SendMessage("Usage [Colorize <true|false>");
            }
        }

        private static void Colorize_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            foreach (object o in from.Map.GetObjectsInBounds(rect))
                if (o is Item item)
                    item.Colorize = (bool)state;

            from.SendMessage("done.");
        }
        [Usage("BoundingRect")]
        [Description("You define a rect, it is shown to you, and the rect is copied to the clipboard.")]
        public static void BoundingRect_OnCommand(CommandEventArgs e)
        {
            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(Rect_Callback), 0x01);
        }

        private static void Rect_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            ClipboardService.SetText(rect.ToString());
            Server.Gumps.EditAreaGump.FlashArea(from, rect, from.Map);
            from.SendMessage("{0} has been copied to your clipboard.", rect.ToString());
            int mobs = 0;
            int items = 0;
            foreach (object o in from.Map.GetObjectsInBounds(rect))
                if (o is Mobile mob && !mob.Deleted)
                    mobs++;
                else if (o is Item item && !item.Deleted)
                    items++;
            from.SendMessage("There are {0} mobiles and {1} items in this rect.", mobs, items);
        }
        [Usage("SectorBounds")]
        [Description("Displays the bounding area of the current sector.")]
        public static void SectorBounds_OnCommand(CommandEventArgs e)
        {
            Sector mySect = e.Mobile.Map.GetSector(e.Mobile);
            int sectorX = mySect.X << Map.SectorShift;
            int sectorY = mySect.Y << Map.SectorShift;
            var rect = new Rectangle2D(sectorX, sectorY, Map.SectorSize, Map.SectorSize);
            Server.Gumps.EditAreaGump.FlashArea(e.Mobile, rect, e.Mobile.Map);
        }

        [Usage("GetSectorInfo")]
        [Description("Displays the XY current sector and number of mobiles.")]
        public static void GetSectorInfo_OnCommand(CommandEventArgs e)
        {
            Sector mySect = e.Mobile.Map.GetSector(e.Mobile);
            int sectorX = mySect.X << Map.SectorShift;
            int sectorY = mySect.Y << Map.SectorShift;
            var rect = new Rectangle2D(sectorX, sectorY, Map.SectorSize, Map.SectorSize);
            e.Mobile.SendMessage("Sector {0} with {1} mobiles.", rect, mySect.Mobiles.Count);
        }
        [Usage("GetSurfaceTop")]
        [Description("Get Surface Top")]
        public static void GetSurfaceTop_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new GetSurfaceTopTarget(null);
        }
        public class GetSurfaceTopTarget : Target
        {
            public GetSurfaceTopTarget(object o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                IPoint3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                Spells.SpellHelper.GetSurfaceTop(ref loc);
                from.SendMessage(String.Format("GetSurfaceTop: Z:{0}", loc.Z));
            }
        }
        [Usage("GetAverageZ")]
        [Description("Get Average Z")]
        public static void GetAverageZ_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new GetAverageZTarget(null);
        }
        public class GetAverageZTarget : Target
        {
            public GetAverageZTarget(object o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;

                int landZ = 0, landAvg = 0, landTop = 0;
                from.Map.GetAverageZ(loc.X, loc.Y, ref landZ, ref landAvg, ref landTop);
                from.SendMessage(String.Format("GetAverageZ(loc.X={0}, loc.Y={1}, landZ={2}, landAvg={3}, landTop={4})",
                    loc.X, loc.Y, landZ, landAvg, landTop));
            }
        }
        /// <summary>
        /// FindBuggedMap is a map walker that visits every tile on the map. At each location it will spawn check whether a mobile can be spawned there, an if so
        ///     record the maximum Z that is spawnable. The algo will then query if N more mobiles could be spawned on adjacent tiles. If not, we move along to the next location.
        ///     The reason a mobile cannot be spawned here is due to some blocking tile: boulder, mountain, tree etc.
        ///     When all N spawn locations are successfully acquired, we make the following assumption:
        ///     All mobiles spawned on adjacent tiles must have the same or similar Zz (+-5).
        ///     When we have one or more locations with a depth(Z) greater than our threshold (say a Z of 30,) we create a 'report' record for that location.
        ///     Before we write the report however, we want to remove records that likely point to the same hole. I.e., they are in close proximity to one another.
        ///     When this is accomplished, we write the report to the \Logs folder.
        /// </summary>
        /// <param name="e"></param>
        [Usage("FindBuggedMap")]
        [Description("Log 'holes' in the map. See FindBuggedMap.log")]
        public static void FindBuggedMap_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Calculating bugged map tiles.");
            e.Mobile.SendMessage("This will take 3-5 minutes...");
            Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(Tick), e);
        }
        private static void Tick(object state)
        {
            CommandEventArgs e = state as CommandEventArgs;
            FindBuggedMap(e);
        }
        public static void FindBuggedMap(CommandEventArgs e)
        {
            int depth = 0;
            if (e.Arguments.Length == 0 || int.TryParse(e.ArgString, out depth) == false)
            {
                e.Mobile.SendMessage("Usage: FindBuggedMap <depth threshold>");
                return;
            }
            LogHelper logger = new LogHelper("FindBuggedMap.log", true, true, true);
            try
            {
                Console.WriteLine("Calculating bugged map tiles.");
                Console.WriteLine("This will take 3-5 minutes...");
                DateTime dt = DateTime.UtcNow;
                Point2D last = new Point2D(0, 0);               // last point seen
                Rectangle2D fel = Utility.World.BritWrap[0];    // our world
                List<int[]> records = new();                    // X, Y, X (depth of hole)
                int slop = 5;                                   // how far two holes are from one another to be considered unique
                int[] Ztab = new int[4];                        // storage for our 4 discovered Zs

                for (int y = fel.Start.Y; y < (fel.Height); y++)
                {
                    for (int x = fel.Start.X; x < (fel.Width); x++)
                    {
                        // check at end of map
                        if (x + 4 >= Map.Felucca.Width) continue;

#if false
                        // experimental star pattern
                        int Z = 0;
                        int X = x;
                        int Y = y;
                        int index = 0;

                        for (int jx = 0; jx < 4; jx++)
                        {   // check a star pattern around x/y
                            if (Utility.CanSpawnMobile(Map.Felucca, NextX(jx,  X), NextY(jx,  Y), ref Z))
                                Ztab[index++] = Z;
                            else break;

                            if (jx + 1 == 4)
                                goto Success;   // we got all 4 spawn positions
                        }
                        continue;
#else
                        int Z = 0;
                        int X = x;
                        int index = 0;

                        for (int jx = 0; jx < 4; jx++)
                        {
                            if (Utility.CanSpawnMobile(Map.Felucca, X++, y, ref Z))
                                Ztab[index++] = Z;
                            else break;

                            if (jx + 1 == 4)
                                goto Success;   // we got all 4 spawn positions
                        }
                        continue;
#endif
                    Success:
                        // okay, we were able to spawn 4 consecutive mobiles This should not be possible if there was some blocking tile in the way,
                        // like a wall, bounder, tree, etc.
                        // So if we were able to spawn all four, lets make sure the Zz match up (or are close)
                        int bd = BiggestDifference(Ztab);

                        if (bd >= depth)
                        {
                            records.Add(new int[] { x, y, bd });
                            last = new Point2D(X, y);
                            Console.Write(".");
                        }
                        continue;
                    }
                }

                // remove all neighbors within 'slop' distance from one another. I.e., we don't want 20 reports of the same 'hole'
                RemoveNeighbors(slop, records);

                // look for our known 'holes' in Jhelom and Minax
                int JhelomSlop = 8;     // we know this checker comes within 7.2 tiles of our Jhelom hole (get only closest match)
                int MinaxSlop = 6;      // we know this checker comes within 5.3 tiles of our Minax hole (get only closest match)
                for (int ix = 0; ix < records.Count; ix++)
                {
                    if (Utility.GetDistanceToSqrt(new Point2D(records[ix][0], records[ix][1]), new Point2D(1418, 3991)) <= JhelomSlop)
                        logger.Log(LogType.Text, string.Format("Found our known Jhelom 'hole' near {0} {1} at {2} {3}.", 1418, 3991, records[ix][0], records[ix][1]));

                    if (Utility.GetDistanceToSqrt(new Point2D(records[ix][0], records[ix][1]), new Point2D(1108, 2610)) <= MinaxSlop)
                        logger.Log(LogType.Text, string.Format("Found our known Minax 'hole' near {0} {1} at {2} {3}.", 1418, 3991, records[ix][0], records[ix][1]));
                }

                // output our results
                for (int ix = 0; ix < records.Count; ix++)
                {
                    logger.Log(LogType.Text, string.Format("Hole located at {0} {1} depth is {2}", records[ix][0], records[ix][1], records[ix][2]));
                }

                Console.WriteLine();
                Console.WriteLine("done in {0:0.00} seconds.", (DateTime.UtcNow - dt).TotalSeconds);
                e.Mobile.SendMessage("done in {0:0.00} seconds.", (DateTime.UtcNow - dt).TotalSeconds);
                logger.Finish();
            }
            catch
            {
                logger.Finish();
            }

        }
        /// <summary>
        /// The core algo here returns every point that seems to be a hole in the map. THis means that if the hole is 12 tiles, we will get 12 positives.
        /// RemoveNeighbors will for example attempt to remove all but reference to the hole - drastically shrinking the report file.
        /// 
        /// </summary>
        /// <param name="Slop"> The integer threshold that determines when report records are to be kept or they are noise and should be deleted.</param>
        /// <param name="records"> The records to analyze</param>
        /// <returns>number of records removed</returns>
        private static int RemoveNeighbors(int Slop, List<int[]> records)
        {
            int oldCount = records.Count;
            List<int[]> delete = new();
            for (int ix = 0; ix < records.Count; ix++)
            {
                for (int jx = 0; jx < records.Count; jx++)
                {
                    Point2D pxi = new Point2D(records[ix][0], records[ix][1]);
                    Point2D pxj = new Point2D(records[jx][0], records[jx][1]);
                    if (pxi == pxj || delete.Contains(records[ix]) || delete.Contains(records[jx]))
                        continue;
                    double dtsr = Utility.GetDistanceToSqrt(pxi, pxj);
                    if (dtsr <= Slop)
                        if (pxi > pxj)
                        {
                            if (!delete.Contains(records[ix]))
                                delete.Add(records[ix]);
                        }
                        else if (pxi < pxj)
                        {
                            if (!delete.Contains(records[jx]))
                                delete.Add(records[jx]);
                        }
                        else
                        {   // interestingly (to me anyway) you cannot compare (>, <) to two points if they are on different X's or Y's (see Geometry.cs) 
                            // so, we'll prefer (somewhat arbitrarily) the smaller X
                            if (records[jx][0]/*X*/ < records[ix][0]/*X*/)
                            {
                                if (!delete.Contains(records[jx]))
                                    delete.Add(records[jx]);
                            }
                            else if (!delete.Contains(records[ix]))
                                delete.Add(records[ix]);
                            else
                                ;// wtf
                        }
                }
            }

            for (int ix = 0; ix < delete.Count; ix++)
                if (records.Contains(delete[ix]))
                    records.Remove(delete[ix]);

            return oldCount - records.Count;
        }
        private static int NextX(int n, int x)
        {
            switch (n)
            {
                default:
                case 0: return x;
                case 1: return x + 1;
                case 2: return x - 1;
                case 3: return x + 1;
                case 4: return x - 1;
            }
        }
        private static int NextY(int n, int y)
        {
            switch (n)
            {
                default:
                case 0: return y;
                case 1: return y + 1;
                case 2: return y - 1;
                case 3: return y - 1;
                case 4: return y + 1;
            }
        }
        private static int BiggestDifference(int[] A)
        {
            int N = A.Length;
            if (N < 1) return 0;

            int max = 0;
            int result = 0;

            for (int i = N - 1; i >= 0; --i)
            {
                if (A[i] > max)
                    max = A[i];

                var tmpResult = max - A[i];
                if (tmpResult > result)
                    result = tmpResult;
            }

            return result;
        }
        [Usage("StackDepth")]
        [Description("How many items are at this point?")]
        public static void StackDepth_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new StackDepthTarget(null);
        }
        public class StackDepthTarget : Target
        {
            public StackDepthTarget(object o)
                : base(12, false, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : Point3D.Zero;
                List<int> Zs = new();
                int count = StackDepth(loc.X, loc.Y, from.Map, Zs);
                Zs.Sort();
                from.SendMessage(String.Format("There are {0} items stacked at {1}, Z{3}={2}", count, new Point2D(loc.X, loc.Y),
                    (count == 0) ? from.Map.GetAverageZ(loc.X, loc.Y) :
                    (count == 1) ? Zs[0].ToString() :
                    String.Format("{0}-{1}", Zs.First(), Zs.Last()),
                    count > 1 ? "s" : ""
                    ));
            }
        }
        public static int StackDepth(int X, int Y, Map map)
        {
            List<int> Zs = new();
            foreach (object o in map.GetItemsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Item item && item.Deleted == false)
                    if (item.Location.X == X && item.Location.Y == Y)
                        Zs.Add(item.Z);

            return Zs.Count;
        }
        public static int StackDepth(int X, int Y, Map map, List<int> Zs)
        {
            foreach (object o in map.GetItemsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Item item && item.Deleted == false)
                    if (item.Location.X == X && item.Location.Y == Y)
                        Zs.Add(item.Z);

            return Zs.Count;
        }
        public static int GetStack(int X, int Y, Map map, List<Item> items)
        {
            foreach (object o in map.GetItemsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Item item && item.Deleted == false)
                    items.Add(item);

            return items.Count;
        }

        [Usage("CanSpawnMobile <Y, Y, Z>")]
        [Description("Can we fit a mobile at this loction? needed for treasure map locations.")]
        public static void CanSpawnMobileT_OnCommand(CommandEventArgs e)
        {
            Point3D px;
            if (e.Length != 3)
            {
                e.Mobile.SendMessage("Point3D format error. Format is (XXX, YYY, ZZZ)");
                return;
            }
            try
            {
                px = Point3D.Parse(e.ArgString);
                int z = px.Z;
                if (e.Mobile.Map.CanSpawnLandMobile(px))
                    e.Mobile.SendMessage("A mobile can be spawned at location {0}", px);
                else
                {
                    e.Mobile.SendMessage("A mobile cannot be spawned at location {0}, however...", px);
                    px.Z = e.Mobile.Map.GetAverageZ(px.X, px.Y);
                    if (e.Mobile.Map.CanSpawnLandMobile(px))
                        e.Mobile.SendMessage("Still cannot spawn a mobile at location {0} using GetAverageZ()", px);
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Point3D format error. Format is (XXX, YYY, ZZZ) {0}", ex);
            }
        }
        [Usage("CanSpawnMobileZ")]
        [Description("Can we spawn a mobile at this location?")]
        public static void CanSpawnMobileZ_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new CanSpawnMobileTargetZ(null);
        }
        public class CanSpawnMobileTargetZ : Target
        {
            public CanSpawnMobileTargetZ(object o)
                : base(12, true, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                int Z = -1;
                if (loc != Point3D.Zero && Utility.CanSpawnMobile(from.Map, loc.X, loc.Y, ref Z))
                    from.SendMessage("A mobile can be spawned at {0}, Z = {1}", new Point2D(loc.X, loc.Y), Z);
                else
                    from.SendMessage("A mobile cannot be spawned at {0}, Z=?", new Point2D(loc.X, loc.Y));
            }
        }
        [Usage("SeedAccounts")]
        [Description("Seeds the login database from the accounts.xml on this server.")]
        public static void SeedAccounts_OnCommand(CommandEventArgs e)
        {
            if (Core.UseLoginDB)
            {
                Utility.ConsoleWriteLine("Seeding Login Database from server {0}.", ConsoleColor.Green, Core.Server);
                Server.Accounting.AccountsDatabase.SeedAccounts();
            }
            else
            {
                Utility.ConsoleWriteLine("Not using the Login Database.", ConsoleColor.Red);
            }
        }
        public static bool IsOwnerIP(System.Net.IPAddress ip)
        {
            return (
                /*Yoar ip.Equals(System.Net.IPAddress.Parse("83.87.255.92"))*/
                /*Adam*/   ip.Equals(System.Net.IPAddress.Parse("192.168.1.67"))    // me logging into production from home
                /*Adam*/|| ip.Equals(System.Net.IPAddress.Parse("127.0.0.1"))       // me logging into localhost from home
                );
        }
        [Usage("TileOverload")]
        [Description("Looks for, and deletes excessive items on a single tile.")]
        public static void TileOverload_OnCommand(CommandEventArgs e)
        {

            Dictionary<Point3D, KeyValuePair<Map, int>> database = new Dictionary<Point3D, KeyValuePair<Map, int>>();
            try
            {
                foreach (Item i in World.Items.Values)
                {
                    if (i == null || i.Parent != null || i.Map == Map.Internal)
                        continue;   // only items 'in the world'

                    if (database.ContainsKey(i.Location))
                        database[i.Location] = new KeyValuePair<Map, int>(i.Map, database[i.Location].Value + 1);
                    else
                        database.Add(i.Location, new KeyValuePair<Map, int>(i.Map, 1));
                }

                Dictionary<Point3D, KeyValuePair<Map, int>> problemDatabase = new Dictionary<Point3D, KeyValuePair<Map, int>>();
                foreach (KeyValuePair<Point3D, KeyValuePair<Map, int>> kvp in database)
                {
                    if (kvp.Value.Value > 666)
                        if (!problemDatabase.ContainsKey(kvp.Key))
                            problemDatabase.Add(kvp.Key, kvp.Value);
                }

                foreach (KeyValuePair<Point3D, KeyValuePair<Map, int>> kvp in problemDatabase)
                {
                    LogHelper logger = new LogHelper("TileOverload.log", false, true);
                    string text = string.Format("{0} items found at {1}.", kvp.Value.Value, kvp.Key);
                    logger.Log(text);
                    logger.Finish();
                    e.Mobile.SendMessage(text);
                }

                if (e.Arguments.Length > 0 && e.ArgString.ToLower() == "fix")
                {
                    foreach (KeyValuePair<Point3D, KeyValuePair<Map, int>> kvp in problemDatabase)
                    {
                        LogHelper logger = new LogHelper("TileOverload.log", false, true);
                        string text = string.Format("Deleting {0} items at {1}.", kvp.Value.Value, kvp.Key);
                        logger.Log(text);
                        logger.Finish();
                        e.Mobile.SendMessage(text);

                        IPooledEnumerable eable = kvp.Value.Key.GetItemsInRange(kvp.Key, 0);
                        List<Item> list = new List<Item>();
                        foreach (Item ix in eable)
                        {   // list of items to delete
                            // Note: we need to recheck the problemdatabase since GetItemsInRange() ignores the Z plane
                            if (problemDatabase.ContainsKey(ix.Location))
                                list.Add(ix);
                        }
                        eable.Free();

                        foreach (Item item in list)
                            item.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        [Usage("ItemProfiler")]
        [Description("Profiles Item and looks for leaks.")]
        public static void ItemProfiler_OnCommand(CommandEventArgs e)
        {
            Dictionary<int, int> intItems = new Dictionary<int, int>();
            Dictionary<int, int> felItems = new Dictionary<int, int>();
            int oddMapCount = 0;
            try
            {   // catalog items
                foreach (Item i in World.Items.Values)
                {
                    if (i.Map == Map.Internal)
                    {
                        if (intItems.ContainsKey(i.ItemID))
                            intItems[i.ItemID]++;
                        else
                            intItems.Add(i.ItemID, 1);
                    }
                    else if (i.Map == Map.Felucca)
                    {
                        if (felItems.ContainsKey(i.ItemID))
                            felItems[i.ItemID]++;
                        else
                            felItems.Add(i.ItemID, 1);
                    }
                    else
                    {
                        oddMapCount++;
                    }
                }
                // sort by count
                List<KeyValuePair<int, int>> sortedIntItems = new List<KeyValuePair<int, int>>();
                foreach (KeyValuePair<int, int> kvp in intItems)
                    sortedIntItems.Add(kvp);

                sortedIntItems.Sort((e1, e2) =>
                {
                    return e2.Value.CompareTo(e1.Value);
                });

                List<KeyValuePair<int, int>> sortedFelItems = new List<KeyValuePair<int, int>>();
                foreach (KeyValuePair<int, int> kvp in felItems)
                    sortedFelItems.Add(kvp);

                sortedFelItems.Sort((e1, e2) =>
                {
                    return e2.Value.CompareTo(e1.Value);
                });

                Utility.ConsoleWriteLine("There are {0} items on the internal map.", ConsoleColor.Red, sortedIntItems.Count);
                Utility.ConsoleWriteLine("There are {0} items on the felucca map.", ConsoleColor.Red, sortedFelItems.Count);
                Utility.ConsoleWriteLine("There are {0} items on other maps.", ConsoleColor.Red, oddMapCount);

                if (sortedIntItems.Count > 0)
                    Utility.ConsoleWriteLine("Biggest offender on the internal map is ItemID {0} with a count of {1}.", ConsoleColor.Red, sortedIntItems[0].Key, sortedIntItems[0].Value);
                if (sortedFelItems.Count > 0)
                    Utility.ConsoleWriteLine("Biggest offender on the felucca map is ItemID {0} with a count of {1}.", ConsoleColor.Red, sortedFelItems[0].Key, sortedFelItems[0].Value);

                /////////////
                // the code that follows is intended for analysis in the debugger
                /////////////
                int ItemID = -1;
                Map map = Map.Felucca;
                if (sortedIntItems.Count > 0)
                {
                    ItemID = sortedIntItems[0].Key;
                    map = Map.Internal;
                }
                if (sortedFelItems.Count > 0 && sortedFelItems[0].Value > sortedIntItems[0].Value)
                {
                    ItemID = sortedFelItems[0].Key;
                    map = Map.Felucca;
                }

                Dictionary<Serial, DateTime> reportItems = new Dictionary<Serial, DateTime>();
                List<string> report = new List<string>();
                if (ItemID > -1)
                {   // catalog items
                    foreach (Item i in World.Items.Values)
                    {
                        if (i.Map == map && i.ItemID == ItemID)
                        {
                            report.Add(string.Format("{0}, created:{1}, location:{2}, parent{3}", i, i.Created, i.Location, i.Parent));
                            reportItems.Add(i.Serial, i.Created);
                        }
                    }
                }

                List<KeyValuePair<Item, DateTime>> sortedReportItems = new List<KeyValuePair<Item, DateTime>>();
                foreach (KeyValuePair<Serial, DateTime> kvp in reportItems)
                    sortedReportItems.Add(new KeyValuePair<Item, DateTime>(World.FindItem(kvp.Key), kvp.Value));

                sortedReportItems.Sort((e1, e2) =>
                {
                    return e2.Value.CompareTo(e1.Value);
                });

                return;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        [Usage("Monitor <item>")]
        [Description("Asks the item to updates us when interestings happen.")]
        public static void Monitor_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Target the item you wish to monitor.");
                e.Mobile.Target = new MonitorTarget(e);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public class MonitorTarget : Target
        {
            CommandEventArgs m_e;
            public MonitorTarget(object o)
                : base(12, false, TargetFlags.None)
            {
                m_e = (CommandEventArgs)o;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Item item = (targeted as Item);
                Mobile mobile = (targeted as Mobile);
                if (item != null)
                {
                    if (m_e.ArgString != null && m_e.ArgString.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        if (item.Monitors.Contains(from))
                            item.Monitors.Remove(from);

                        from.SendMessage("Monitor removed from {0}.", item);
                    }
                    else if (!item.Monitors.Contains(from))
                    {
                        item.Monitors.Add(from);
                        from.SendMessage("Monitor added from {0}.", item);
                    }
                    else if (item.Monitors.Contains(from))
                    {
                        item.Monitors.Remove(from);
                        from.SendMessage("Monitor removed from {0}.", item);
                    }
                }
                else if (mobile != null)
                {
                    if (m_e.ArgString != null && m_e.ArgString.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        if (mobile.Monitors.Contains(from))
                            mobile.Monitors.Remove(from);

                        from.SendMessage("Monitor removed from {0}.", mobile);
                    }
                    else if (!mobile.Monitors.Contains(from))
                    {
                        mobile.Monitors.Add(from);
                        from.SendMessage("Monitor added from {0}.", mobile);
                    }
                    else if (mobile.Monitors.Contains(from))
                    {
                        mobile.Monitors.Remove(from);
                        from.SendMessage("Monitor removed from {0}.", mobile);
                    }
                }
                else
                    from.SendMessage("That is neither a mobile or an item.");
            }
        }
        [Usage("Clobber [account name]")]
        [Description("Resets an account's player characters to starting skills/stats and empties bank box.")]
        public static void Clobber_OnCommand(CommandEventArgs e)
        {
            bool clobbered = false;
            try
            {
                if (e.Arguments == null || e.Length != 1)
                {
                    e.Mobile.SendMessage("Usage: Clobber [account name]");
                    return;
                }
                foreach (Account acct in Accounts.Table.Values)
                {
                    if (acct == null)
                        continue;

                    if (acct.Username.ToLower() != e.ArgString.ToLower())
                        continue;

                    for (int charndx = 0; charndx < acct.Limit; charndx++)
                    {
                        if (acct[charndx] == null)
                            break;                      // no more characters on this account

                        clobbered = true;
                        Console.WriteLine("Clobbering character {0}.", acct[charndx].Name);
                        ClobberMobile(acct[charndx]);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            if (clobbered == true)
                Console.WriteLine("Clobbering complete.");
            else
                Console.WriteLine("{0} not found.", e.ArgString);
        }
        private static void ClobberMobile(Mobile m)
        {
            // clobber stats
            m.Str = m.Str / 4;
            m.Dex = m.Dex / 4;
            m.Int = m.Int / 4;

            // clobber skills
            Server.Skills skills = m.Skills;
            for (int i = 0; i < skills.Length; ++i)
                if (skills[i].Base > 10)
                    skills[i].Base = skills[i].Base / 4;

            // clobber backpack
            if (m.BankBox != null)
            {
                List<Item> items = new List<Item>();
                items = new List<Item>(m.Backpack.Items);
                foreach (Item item in items)
                {
                    m.Backpack.RemoveItem(item);
                    item.Delete();
                }
            }

            // clobber bank box
            if (m.BankBox != null)
            {
                List<Item> items = new List<Item>();
                items = new List<Item>(m.BankBox.Items);
                foreach (Item item in items)
                {
                    m.BankBox.RemoveItem(item);
                    item.Delete();
                }
            }
        }
        public enum operation
        {
            INFO,
            RECOVER
        }
        [Usage("RecycleBin info|recover")]
        [Description("Dumps info about a containers overload items and optionally recovers them.")]
        public static void RecycleBin_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Arguments == null || e.Length != 1 || (!e.ArgString.ToLower().Contains("recover") && !e.ArgString.ToLower().Contains("info")))
                {
                    e.Mobile.SendMessage("Usage: RecycleBin [recover|info]");
                    return;
                }
                e.Mobile.SendMessage("Target the container you wish to process.");
                e.Mobile.Target = new ContainerTarget(e.ArgString.ToLower().Contains("recover") ? operation.RECOVER : operation.INFO);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public class ContainerTarget : Target
        {
            operation m_op;
            public ContainerTarget(operation op)
                : base(12, false, TargetFlags.None)
            {
                m_op = op;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Container container = (targeted as Container);
                List<Container> units = new List<Container>();
                if (container != null)
                {
                    if (Engines.RecycleBin.RecycleBin.Database.ContainsKey(container.Serial))
                        // found a matching RecycleBin
                        units.Add(Engines.RecycleBin.RecycleBin.Database[container.Serial]);
                }
                else
                    from.SendMessage("That is not a container.");

                // okay, we lave a list of matching Recycle bins for this container
                //  (we currently only support one.)
                int archived = 0;
                List<Container> bins = new List<Container>();
                if (units.Count != 0)
                {
                    foreach (Container unit in units)
                    {
                        if (m_op == operation.INFO)
                            from.SendMessage(string.Format("Recycle bin: {0} with: {1} items archived.", unit.Serial, unit.Items.Count));
                        bins.Add(unit);
                        archived += unit.Items.Count;
                    }
                    if (m_op == operation.INFO)
                    {
                        from.SendMessage(string.Format("There are {0} matching recycle bins.", units.Count));
                        from.SendMessage(string.Format("{0} total items archived.", archived));
                    }
                    else if (m_op == operation.RECOVER)
                    {
                        if (bins.Count > 0)
                        {
                            foreach (Container cx in bins)
                            {
                                cx.MoveToWorld(from.Location, from.Map);
                                cx.Visible = true;
                                cx.Movable = true;
                                Engines.RecycleBin.RecycleBin.Database.Remove(container.Serial);
                            }
                            from.SendMessage(string.Format("Done."));
                        }
                        else
                            from.SendMessage(string.Format("There are no archived items to recover."));
                    }
                }
                else
                    from.SendMessage("No RecycleBin units found for this container.");
            }
        }
        [Usage("PreLaunch")]
        [Description("Wipe Shard - Pre-Launch.")]
        public static void PreLaunch_OnCommand(CommandEventArgs e)
        {
            try
            {

                if (e.Mobile != null) e.Mobile.SendMessage("Begin Wipe.");
#if false
                // first rehydrate world
                RehydrateWorld.RehydrateWorld_OnCommand(e);
#endif
                // delete accounts 
                int count = DeleteAccounts(e);
                if (e.Mobile != null) e.Mobile.SendMessage("{0} Accounts deleted.", count);

                // delete contents of all containers
                count = EmptyContainers(e);
                if (e.Mobile != null) e.Mobile.SendMessage("{0} Items deleted.", count);

                // replace all things like library books.
                if (e.Mobile != null) e.Mobile.SendMessage("Respawning world...");
                TotalRespondCommand.TotalRespawn_OnCommand(e);

                // deactivate all quest givers
                count = DeactivateQuestGivers();
                if (e.Mobile != null) e.Mobile.SendMessage("{0} QuestGivers deactivated.", count);

                // Initialize the ResourcePool
                count = InitializeResourcePool();
                if (e.Mobile != null) e.Mobile.SendMessage(String.Format("{0} resource pool consignments deleted.", count));

                // all done
                if (e.Mobile != null) e.Mobile.SendMessage("Wipe Complete.");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                if (e.Mobile != null)
                    e.Mobile.SendMessage("** Wipe Failed **");
            }
        }

        public static int InitializeResourcePool()
        {
            // Initialize the ResourcePool
            int count = 0;
            string saves = Path.Combine(Core.BaseDirectory, "Saves");
            string resourcePool = Path.Combine(Core.BaseDirectory, "Saves", "ResourcePool");
            string transactionHistories = Path.Combine(Core.BaseDirectory, "Saves", "TransactionHistories");
            if (Directory.Exists(resourcePool))
            {
                DirectoryInfo di = new DirectoryInfo(resourcePool);
                RecursiveDelete(di);
            }
            if (!Directory.Exists(resourcePool))
                Directory.CreateDirectory(resourcePool);
            if (!Directory.Exists(transactionHistories))
                Directory.CreateDirectory(transactionHistories);
            if (ResourcePool.Consignments != null)
            {
                count = ResourcePool.Consignments.Count;
                //EchoOut(String.Format("{0} resource pool consignments detected.", rpc), ConsoleColor.Magenta);
                ResourcePool.Consignments.Clear();
                //EchoOut(String.Format("{0} resource pool consignments deleted.", rpc), ConsoleColor.Magenta);
            }
            //else
            //EchoOut(String.Format("No resource pool consignments to deleted."), ConsoleColor.Magenta);
            return count;
        }
        public static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RecursiveDelete(dir);
            }
            baseDir.Delete(true);
        }
        public static int DeactivateQuestGivers()
        {
            int count = 0;
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is null) continue;
                if (m is QuestGiver qg)
                {
                    count++;
                    qg.NeedsReview = true;
                }
            }
            return count;
        }
        public static int EmptyContainers(CommandEventArgs e)
        {
            #region Get Loot Packs
            List<Container> lootPacks = new List<Container>();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted == true)
                    continue;

                if (item is Spawner || item is ChestItemSpawner)
                {
                    Spawner s = item as Spawner;
                    ChestItemSpawner cis = item as ChestItemSpawner;
                    if (s != null)
                    {
                        if (s.LootPack is Container sl)
                            lootPacks.Add(sl);
                        if (s.ArtifactPack is Container sap)
                            lootPacks.Add(sap);
                        if (s.CarvePack is Container scp)
                            lootPacks.Add(scp);
                    }
                    else if (cis != null && cis.LootPack is Container cislp)
                        lootPacks.Add(cislp);
                }
            }
            #endregion Get Loot Packs

            int total_items = 0;
            #region Empty containers
            {
                List<Container> containers = new List<Container>();
                foreach (Item item in World.Items.Values)
                {
                    if (item == null || item.Deleted == true || !(item is Container))
                        continue;

                    if (Owner(e, item) != null && Owner(e, item).AccessLevel == AccessLevel.Owner)
                        continue;

                    if (lootPacks.Contains(item))
                        continue;

                    containers.Add(item as Container);
                }

                ArrayList toDelete = new ArrayList();
                foreach (Container container in containers)
                {
                    List<Item> items = container.Items;
                    total_items += items.Count;
                    foreach (object o in items)
                    {
                        Item item = o as Item;
                        if (item == null || item.Deleted == true)
                            continue;

                        toDelete.Add(new object[] { container, item });
                    }
                }
                foreach (object o in toDelete)
                {
                    object[] pair = o as object[];
                    Container cont = pair[0] as Container;
                    Item item = pair[1] as Item;
                    cont.RemoveItem(item);
                    item.Delete();
                }
            }
            #endregion Empty containers
            return total_items;
        }
        public static Account Owner(CommandEventArgs e, Item item)
        {
            // first, check the backpack and bankbox of the item
            if (item.RootParent != null && (item.RootParent is PlayerMobile) && (item.RootParent as PlayerMobile).Account != null)
                return (item.RootParent as PlayerMobile).Account as Account;
            // now check for items in nested containers on the ground of a house.
            else if (item.RootParent != null && (item.RootParent is Item) && BaseHouse.FindHouseAt((item.RootParent as Item)) != null)
            {
                BaseHouse house = BaseHouse.FindHouseAt((item.RootParent as Item));
                if (house.Owner != null && (house.Owner is PlayerMobile) && (house.Owner as PlayerMobile).Account != null)
                    return (house.Owner as PlayerMobile).Account as Account;
                else
                    return null;
            }
            // now check for items in a container on the ground of a house
            else if (item.RootParent == null && BaseHouse.FindHouseAt((item)) != null)
            {
                BaseHouse house = BaseHouse.FindHouseAt((item));
                if (house.Owner != null && (house.Owner is PlayerMobile) && (house.Owner as PlayerMobile).Account != null)
                    return (house.Owner as PlayerMobile).Account as Account;
                else
                    return null;
            }
            // now look for PlayerVendors in a house (maybe adam's house (custom homes), and adam's NPC's that sell these houses.
            //  their backpacks are full of these deeds, don't delete.
            else if (item.RootParent != null && item.RootParent is PlayerVendor && BaseHouse.FindHouseAt((item)) != null)
            {
                // now check for items in a container on the ground of a house
                BaseHouse house = BaseHouse.FindHouseAt(item.RootParent as Mobile);
                if (house.Owner != null && (house.Owner is PlayerMobile) && (house.Owner as PlayerMobile).Account != null)
                {   // okay, probably one of my custom homes in the custom housing area.
                    //  Use this opportunity to patch the NPC Owner
                    if (item.RootParent is PlayerVendor && house.Owner.Serial == e.Mobile.Serial && e.Mobile.Serial == Server.World.GetAdminAcct().Serial)
                    {   // patch and return
                        if ((item.RootParent as PlayerVendor).Owner == null)
                            (item.RootParent as PlayerVendor).Owner = Server.World.GetAdminAcct();
                    }

                    return (house.Owner as PlayerMobile).Account as Account;
                }
                else
                    return null;
            }
            // find other BaseVendors belonging to Adam. These include the QuestGiver NPC
            else if (item.RootParent != null && item.RootParent is BaseVendor && BaseHouse.FindHouseAt((item)) != null)
            {
                // now check for items in a container on the ground of a house
                BaseHouse house = BaseHouse.FindHouseAt(item.RootParent as Mobile);
                if (house.Owner != null && (house.Owner is PlayerMobile) && (house.Owner as PlayerMobile).Account != null)
                    return (house.Owner as PlayerMobile).Account as Account;
                else
                    return null;
            }
            else
                return null;
        }
        public static int DeleteAccounts(CommandEventArgs e, bool include_owner = false)
        {
            List<Account> clients = new List<Account>();
            foreach (Account check in Accounts.Table.Values)
            {
                if (check == null)
                    continue;

                if (check.AccessLevel == AccessLevel.Owner && include_owner == false)
                    continue;

                clients.Add(check);
            }

            // kick everyone
            foreach (NetState ns in NetState.Instances)
                if (ns != null && ns.Running)
                    if (ns.Account != null && (ns.Account as Accounting.Account).AccessLevel == AccessLevel.Owner)
                    {
                        if (include_owner)
                            ns.Dispose();
                        else
                            continue;
                    }
                    else
                        ns.Dispose();

            foreach (Account check in clients)
            {
                check.Delete();
            }

            return clients.Count;
        }
        [Usage("LoadJump")]
        [Description("Load our 'jump' table with the creatures on this spawner.")]
        public static void LoadJump_OnCommand(CommandEventArgs e)
        {

            try
            {
                e.Mobile.SendMessage("Target the object you wish to load from.");
                e.Mobile.Target = new LoadTarget();
            }
            catch
            {
                e.Mobile.SendMessage("There was a problem loading the jump list.");
            }

        }
        public class LoadTarget : Target
        {
            public LoadTarget()
                : base(12, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                // reset jump table
                PlayerMobile pm = from as PlayerMobile;
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();
                if (pm != null)
                {
                    if (targeted is ChampEngine champ)
                    {
                        List<Mobile> list = new();
                        if (champ.Monsters != null)
                            foreach (Mobile m in champ.Monsters)
                                if (m != null)
                                    list.Add(m);

                        if (champ.FreeMonsters != null)
                            foreach (Mobile m in champ.FreeMonsters)
                                if (m != null)
                                    if (!list.Contains(m))
                                        list.Add(m);

                        if (list.Count != 0)
                        {
                            foreach (object ox in list)
                                if (ox is Mobile)
                                    pm.JumpList.Add(ox);

                            from.SendMessage(string.Format("Jump table loaded with {0} objects.", list.Count));
                        }
                        else
                            from.SendMessage("That champ engine has spawned no mobiles.");
                    }
                    else if (targeted is Spawner spawner)
                    {
                        if (spawner.Running != true)
                            from.SendMessage("That spawner is not running.");
                        if (spawner.Objects != null && spawner.Objects.Count != 0)
                        {
                            foreach (object ox in spawner.Objects)
                                if (ox is Item)
                                    pm.JumpList.Add((ox as Item).GetWorldLocation());
                                else if (ox is Mobile)
                                    pm.JumpList.Add(ox);
                                else
                                    pm.JumpList.Add(ox);

                            from.SendMessage(string.Format("Jump table loaded with {0} objects.", spawner.Objects.Count));
                        }
                        else
                            from.SendMessage("That spawner has spawned no objects.");
                    }
                    else if (targeted is CustomRegionControl rc)
                    {
                        int count = 0;
                        foreach (Rectangle3D rect in rc.CustomRegion.Coords)
                        {
                            pm.JumpList.Add(NormalizeZ(from.Map, rect.Start));
                            pm.JumpList.Add(NormalizeZ(from.Map, rect.End));
                            count += 2;
                        }

                        from.SendMessage(string.Format("Jump table loaded with {0} objects.", count));
                    }
                    else if (targeted is Runebook rb)
                    {
                        foreach (var o in rb.Entries)
                        {
                            RunebookEntry entry = o as RunebookEntry;
                            Apple dummy = new Apple();
                            dummy.Location = entry.Location;
                            dummy.Map = entry.Map;
                            pm.JumpList.Add(dummy.GetWorldLocation());
                            dummy.Delete();
                        }
                    }
                    else
                    {
                        from.SendMessage("Don't know how to loadjump for {0}", targeted);
                        return;
                    }
                }

                from.SendMessage("Your jump list has been loaded with {0} objects", pm.JumpList.Count);

            }
        }

        [Usage("LoadMEOPJump")]
        [Description("Load our 'jump' table event objects that have been made permanent.")]
        public static void LoadMEOPJump_OnCommand(CommandEventArgs e)
        {

            try
            {
                PlayerMobile pm = e.Mobile as PlayerMobile;
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();

                foreach (Item item in World.Items.Values)
                    if (Utility.IsEventObject(item))
                        if (item is FishController fc)
                        {
                            if (DateTime.Parse(fc.Event.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventTeleporter et)
                        {
                            if (DateTime.Parse(et.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventSungate es)
                        {
                            if (DateTime.Parse(es.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventSpawner esp)
                        {
                            if (DateTime.Parse(esp.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventKeywordTeleporter ekt)
                        {
                            if (DateTime.Parse(ekt.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventConfirmationSungate ecs)
                        {
                            if (DateTime.Parse(ecs.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }

                e.Mobile.SendMessage("Your jump list was loaded with {0} objects.", pm.JumpList.Count);
            }
            catch
            {
                e.Mobile.SendMessage("There was a problem loading the jump list.");
            }

        }
        private static Point3D NormalizeZ(Map map, Point3D px)
        {
            return new Point3D(px.X, px.Y, map.GetAverageZ(px.X, px.Y));
        }
        [Usage("DistanceToSqrt")]
        [Description("Calculate the distance from point X/Y to here.")]
        public static void DistanceToSqrt_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Arguments.Length == 0)
                {
                    e.Mobile.SendMessage("Target the location you wish to calculate the distance to.");
                    e.Mobile.Target = new GetDistanceToSqrtTarget(e.Mobile, e.Mobile.Location);
                    return;
                }

                Point2D[] points;

                try { points = GetPoints(e.ArgString); }
                catch
                {
                    e.Mobile.Say("Usage GetDistanceToSqrt [X Y [X Y]]");
                    return;
                }

                if (points.Length == 1)
                {
                    e.Mobile.Say(string.Format("GetDistanceToSqrt returned: {0:0.00}", e.Mobile.GetDistanceToSqrt(points[0])));
                }
                else if (points.Length == 2)
                {
                    e.Mobile.Say(string.Format("GetDistanceToSqrt returned: {0:0.00}", Utility.GetDistanceToSqrt(points[0], points[1])));
                }
                else
                    e.Mobile.Say("Usage GetDistanceToSqrt [X Y [X Y]]");
            }
            catch
            {
                e.Mobile.Say("Usage GetDistanceToSqrt [X Y [X Y]]");
                return;
            }

        }
        private static Point2D[] GetPoints(string input)
        {
            List<Point2D> points = new List<Point2D>();
            if (!string.IsNullOrEmpty(input))
            {
                string[] sPoints = input.Split(new char[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                int index = 0;
                points.Add(Point2D.Parse(sPoints[index++] + " " + sPoints[index++]));       // first point
                if (sPoints.Length == 4)
                    points.Add(Point2D.Parse(sPoints[index++] + " " + sPoints[index]));     // second point
                else if (sPoints.Length == 5 || sPoints.Length == 6)                        // assume the third/sixth value is Z - we can't be sure if it's (X, Y, Z) (X, Y), or (X, Y) (X, Y, Z) 
                    // skip the Z
                    points.Add(Point2D.Parse(sPoints[++index] + " " + sPoints[++index]));

            }
            return points.ToArray();
        }
        public class GetDistanceToSqrtTarget : Target
        {
            Mobile m_moble;
            Point3D m_Location;
            public GetDistanceToSqrtTarget(Mobile m, Point3D location)
                : base(32, true, TargetFlags.None)
            {
                m_moble = m;
                m_Location = location;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : Point3D.Zero;
                m_moble.Say(string.Format("GetDistanceToSqrt returned: {0}", Utility.GetDistanceToSqrt(loc, m_Location)));
            }
        }
        [Usage("RemoveBackpackHue")]
        [Description("Removed any hue associated with your backpack.")]
        public static void RemoveBackpackHue_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null || e.Mobile.Backpack == null) return;

            if (e.Mobile.Backpack.Hue == 0)
                return;

            e.Mobile.Backpack.Hue = 0;
        }
        [Usage("SaveBackpack")]
        [Description("Backs up your backpack.")]
        public static void SaveBackpack_OnCommand(CommandEventArgs e)
        {
            bool shard_allow = Core.Debug || Core.UOTC_CFG;
            PlayerMobile pm = e.Mobile as PlayerMobile;
            try
            {
                if (shard_allow || e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                {
                    if (BackpackRecovery.ContainsKey(pm))
                    {
                        if (pm.Backpack != null)
                            e.Mobile.SendMessage("Overwriting your saved backpack...");
                        else
                        {
                            e.Mobile.SendMessage("No backpack");
                            return;
                        }
                    }
                    else
                        BackpackRecovery.Add(pm, new List<Item>());


                    BackpackRecovery[pm] = new();
                    BackpackRecovery[pm].AddRange(Utility.GetEquippedItems(pm));
                    BackpackRecovery[pm].AddRange(Utility.GetBackpackItems(pm));

                    e.Mobile.SendMessage("Done. Backed up {0} items", BackpackRecovery[pm].Count);
                }
                else
                {
                    e.Mobile.SendMessage("You are not authorized to use this command on this shard.");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        [Usage("RestoreBackpack")]
        [Description("Restores your backpack to its saved state. Any extra items will be deleted.")]
        public static void RestoreBackpack_OnCommand(CommandEventArgs e)
        {
            bool shard_allow = Core.Debug || Core.UOTC_CFG;
            PlayerMobile pm = e.Mobile as PlayerMobile;
            try
            {
                if (shard_allow || e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                {
                    if (!BackpackRecovery.ContainsKey(pm))
                        e.Mobile.SendMessage("No saved backpack");
                    else if (pm.Backpack == null)
                        e.Mobile.SendMessage("No backpack");
                    else
                    {
                        List<Item> to_delete = new();
                        foreach (Item item in pm.Backpack.Items)
                            if (!BackpackRecovery[pm].Contains(item))
                                to_delete.Add(item);

                        foreach (Item item in to_delete)
                            item.Delete();
                    }
                }
                else
                {
                    e.Mobile.SendMessage("You are not authorized to use this command on this shard.");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        [Usage("RefreshBankBox")]
        [Description("Drops your bankbox to the ground and generates a fresh one.")]
        public static void RefreshBankBox_OnCommand(CommandEventArgs e)
        {
            try
            {
                //e.Mobile.SendMessage("Begin nuclearization.");
                RefreshBankBoxWorker(e);
                //e.Mobile.SendMessage("Nuclearization complete with {0} items processed.", count);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public static void RefreshBankBoxWorker(CommandEventArgs e)
        {

            if (e.Mobile.BankBox == null || e.Mobile.BankBox.Deleted)
            {
                e.Mobile.SendMessage("You do not have a bank box.");
                return;
            }

            e.Mobile.BankBox = e.Mobile.FindItemOnLayer(Layer.Bank) as BankBox;

            if (e.Mobile.BankBox != null)
            {
                e.Mobile.BankBox.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
                e.Mobile.BankBox = null;
                e.Mobile.AddItem(e.Mobile.BankBox = new BankBox(e.Mobile));
                e.Mobile.SendMessage("Bank box refreshed.");
            }
        }

        public class SetZRegionControl : CustomRegionControl
        {
            private int m_SetZ;
            [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
            public int SetZ { get { return m_SetZ; } set { m_SetZ = value; } }
            public override string DefaultName { get { return "SetZ System Region Control"; } }

            [Constructable]
            public SetZRegionControl()
            : base()
            {
                Movable = true;
                Visible = true;
                if (this.CustomRegion != null)
                    this.CustomRegion.Name = "SetZ region";
            }
            public SetZRegionControl(Serial serial) : base(serial) { }
            public override void OnEnter(Mobile m)
            {
                m.Z = m_SetZ;
                base.OnEnter(m);
            }
            public override void OnSingleClick(Mobile m)
            {
                base.OnSingleClick(m);
                LabelTo(m, string.Format("({0})", CustomRegion.Map));
            }
            public override Item Dupe(int amount)
            {   // when duping a CustomRegionControl, we don't actually want the region itself as it's already
                //  been 'registered' with its own UId.
                // The region carries all the following info, which we will need for our dupe
                SetZRegionControl new_crc = new();
                if (CustomRegion != null)
                {
                    Utility.CopyProperties(new_crc.CustomRegion, CustomRegion);
                    new_crc.CustomRegion.Coords = new(CustomRegion.Coords);
                }
                return base.Dupe(new_crc, amount);
            }
            //override O
            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);
                int version = 0;
                writer.Write(0);

                // version 0
                writer.Write(m_SetZ);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        m_SetZ = reader.ReadInt();
                        break;
                }

            }
        }

        #region Serialization
        private static Dictionary<PlayerMobile, List<Item>> BackpackRecovery = new();
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("BackpackRecovery Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/BackpackRecovery.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            int count = BackpackRecovery.Count;             // number of records
                            writer.Write(count);
                            foreach (var kvp in BackpackRecovery)
                            {
                                writer.Write(kvp.Key);                      // write the mobile
                                writer.Write(kvp.Value.Count);              // number of items

                                foreach (var el in kvp.Value)
                                    writer.Write(el);
                            }

                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Saves/BackpackRecovery.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        public static void Load()
        {
            if (File.Exists("Saves/BackpackRecovery.bin"))
                Console.WriteLine("Backpack Recovery Loading...");
            else
            {
                Console.WriteLine("No Backpack Recovery elements to Load.");
                return;
            }
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/BackpackRecovery.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int record_count = reader.ReadInt();           // number of records
                            for (int ix = 0; ix < record_count; ix++)
                            {
                                List<Item> items = new();
                                Mobile m = reader.ReadMobile();             // the mobile
                                int item_count = reader.ReadInt();          // number of items
                                for (int jx = 0; jx < item_count; jx++)
                                {
                                    Item item = reader.ReadItem();
                                    if (item != null)
                                        items.Add(item);
                                }
                                if (m != null)
                                    BackpackRecovery.Add(m as PlayerMobile, items);
                            }

                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid BackpackRecovery.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading Saves/BackpackRecovery.bin, using default values:");
                Utility.PopColor();
            }
        }
        #endregion Serialization
    }
}