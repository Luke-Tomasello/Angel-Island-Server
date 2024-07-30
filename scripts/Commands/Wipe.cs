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

using Server.Items;
using Server.Multis;
using System;
using System.Collections;

namespace Server.Commands
{
    public class Wipe
    {
        [Flags]
        public enum WipeType
        {
            Items = 0x01,
            Mobiles = 0x02,
            Multis = 0x04,
            Corpses = 0x08,
            All = Items | Mobiles | Multis | Corpses
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("Wipe", AccessLevel.GameMaster, new CommandEventHandler(WipeAll_OnCommand));
            Server.CommandSystem.Register("WipeItems", AccessLevel.GameMaster, new CommandEventHandler(WipeItems_OnCommand));
            Server.CommandSystem.Register("WipeNPCs", AccessLevel.GameMaster, new CommandEventHandler(WipeNPCs_OnCommand));
            Server.CommandSystem.Register("WipeCorpses", AccessLevel.GameMaster, new CommandEventHandler(WipeCorpses_OnCommand));
            Server.CommandSystem.Register("WipeMultis", AccessLevel.GameMaster, new CommandEventHandler(WipeMultis_OnCommand));
        }

        [Usage("Wipe")]
        [Description("Wipes all items and npcs in a targeted bounding box.")]
        private static void WipeAll_OnCommand(CommandEventArgs e)
        {
            if (e.Length != 0)
            {
                if (e.Mobile != null)
                    e.Mobile.SendMessage("Usage: Wipe | WipeItems | WipeNPCs | WipeCorpses | WipeMultis");
                return;
            }
            BeginWipe(e.Mobile, WipeType.Items | WipeType.Mobiles);
        }

        [Usage("WipeItems")]
        [Description("Wipes all items in a targeted bounding box.")]
        private static void WipeItems_OnCommand(CommandEventArgs e)
        {
            if (e.Length != 0)
            {
                if (e.Mobile != null)
                    e.Mobile.SendMessage("Usage: Wipe | WipeItems | WipeNPCs | WipeCorpses | WipeMultis");
                return;
            }
            BeginWipe(e.Mobile, WipeType.Items);
        }

        [Usage("WipeNPCs")]
        [Description("Wipes all npcs in a targeted bounding box.")]
        private static void WipeNPCs_OnCommand(CommandEventArgs e)
        {
            if (e.Length != 0)
            {
                if (e.Mobile != null)
                    e.Mobile.SendMessage("Usage: Wipe | WipeItems | WipeNPCs | WipeCorpses | WipeMultis");
                return;
            }
            BeginWipe(e.Mobile, WipeType.Mobiles);
        }

        [Usage("WipeMultis")]
        [Description("Wipes all multis in a targeted bounding box.")]
        private static void WipeMultis_OnCommand(CommandEventArgs e)
        {
            if (e.Length != 0)
            {
                if (e.Mobile != null)
                    e.Mobile.SendMessage("Usage: Wipe | WipeItems | WipeNPCs | WipeCorpses | WipeMultis");
                return;
            }
            BeginWipe(e.Mobile, WipeType.Multis);
        }
        [Usage("WipeCorpses")]
        [Description("Wipes all corpses in a targeted bounding box.")]
        private static void WipeCorpses_OnCommand(CommandEventArgs e)
        {
            if (e.Length != 0)
            {
                if (e.Mobile != null)
                    e.Mobile.SendMessage("Usage: Wipe | WipeItems | WipeNPCs | WipeCorpses | WipeMultis");
                return;
            }
            BeginWipe(e.Mobile, WipeType.Corpses);
        }
        public static void BeginWipe(Mobile from, WipeType type)
        {
            BoundingBoxPicker.Begin(from, new BoundingBoxCallback(WipeBox_Callback), type);
        }

        private static void WipeBox_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            DoWipe(from, map, start, end, (WipeType)state);
        }

        public static void DoWipe(Mobile from, Map map, Point3D start, Point3D end, WipeType type)
        {
            CommandLogging.WriteLine(from, "{0} {1} wiping from {2} to {3} in {5} ({4})", from.AccessLevel, CommandLogging.Format(from), start, end, type, map);

            bool mobiles = ((type & WipeType.Mobiles) != 0);
            bool multis = ((type & WipeType.Multis) != 0);
            bool items = ((type & WipeType.Items) != 0);
            bool corpses = ((type & WipeType.Corpses) != 0);

            ArrayList toDelete = new ArrayList();

            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);

            IPooledEnumerable eable;

            if ((items || multis) && mobiles)
                eable = map.GetObjectsInBounds(rect);
            else if (items || multis || corpses)
                eable = map.GetItemsInBounds(rect);
            else if (mobiles)
                eable = map.GetMobilesInBounds(rect);
            else
                return;

            foreach (object obj in eable)
            {
                if (corpses && (obj is Corpse))
                    toDelete.Add(obj);
                else if (items && (obj is Item) && !((obj is BaseMulti) || (obj is HouseSign)))
                    toDelete.Add(obj);
                else if (multis && (obj is BaseMulti))
                    toDelete.Add(obj);
                else if (mobiles && (obj is Mobile) && !((Mobile)obj).Player)
                    toDelete.Add(obj);
            }

            eable.Free();

            for (int i = 0; i < toDelete.Count; ++i)
            {
                if (toDelete[i] is Item item)
                {
                    if (Utility.IsRestrictedObject(item) && from.AccessLevel < AccessLevel.Owner)
                    {
                        string text = string.Format(string.Format("{0}s are not authorized to delete core shard assets.", from.AccessLevel));
                        CommandLogging.WriteLine(from, "{0} {1} attempting to delete {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(item));
                        from.SendMessage(text);
                        continue;
                    }
                    ((Item)toDelete[i]).Delete();
                }
                else if (toDelete[i] is Mobile)
                    ((Mobile)toDelete[i]).Delete();
            }
        }
    }
}