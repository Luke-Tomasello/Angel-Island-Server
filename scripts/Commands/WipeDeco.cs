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

/* Scripts\Commands\WipeDeco.cs
 * ChangeLog
 *  12/10/2025, Adam
 *  First time check in.
 *  Purpose: Command to wipe decorative items in a targeted bounding box
 */

using Server.Items;
using Server.Multis;
using System;
using System.Collections;

namespace Server.Commands
{
    public class WipeDeco
    {
        // Add any additional types you want to preserve here.
        // Derived types are automatically preserved via IsInstanceOfType checks.
        private static readonly Type[] s_IgnoreTypes = new Type[]
        {
            typeof(Server.Mobiles.Spawner) // preserve spawners and specialized spawners
        };

        public static void Initialize()
        {
            CommandSystem.Register("WipeDeco", AccessLevel.GameMaster, new CommandEventHandler(WipeDeco_OnCommand));
        }

        [Usage("WipeDeco")]
        [Description("Wipes decorative items in a targeted bounding box, ignoring protected types (e.g., Spawner).")]
        private static void WipeDeco_OnCommand(CommandEventArgs e)
        {
            if (e.Length != 0)
            {
                e.Mobile?.SendMessage("Usage: WipeDeco");
                return;
            }

            BeginWipeDeco(e.Mobile);
        }

        private static void BeginWipeDeco(Mobile from)
        {
            BoundingBoxPicker.Begin(from, new BoundingBoxCallback(WipeDeco_Callback), null);
        }

        private static void WipeDeco_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            DoWipeDeco(from, map, start, end);
        }

        public static void DoWipeDeco(Mobile from, Map map, Point3D start, Point3D end)
        {
            CommandLogging.WriteLine(
                from,
                "{0} {1} WipeDeco from {2} to {3} in {4}",
                from.AccessLevel,
                CommandLogging.Format(from),
                start,
                end,
                map
            );

            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);

            // We only care about items for WipeDeco
            IPooledEnumerable eable = map.GetItemsInBounds(rect);
            ArrayList toDelete = new ArrayList();

            foreach (object obj in eable)
            {
                if (obj is Item item)
                {
                    // Never touch multis or house signs (mirror WipeItems behavior)
                    if (item is BaseMulti || item is HouseSign)
                        continue;

                    // Ignore anything on the preservation list (Spawner and subclasses)
                    if (IsIgnored(item))
                        continue;

                    // Keep consistency with shard-asset protection used elsewhere
                    if (Utility.IsRestrictedObject(item) && from.AccessLevel < AccessLevel.Owner)
                    {
                        CommandLogging.WriteLine(
                            from,
                            "{0} {1} attempted to delete restricted {2}",
                            from.AccessLevel,
                            CommandLogging.Format(from),
                            CommandLogging.Format(item)
                        );
                        from.SendMessage("{0}s are not authorized to delete core shard assets.", from.AccessLevel);
                        continue;
                    }

                    toDelete.Add(item);
                }
            }

            eable.Free();

            for (int i = 0; i < toDelete.Count; ++i)
                ((Item)toDelete[i]).Delete();
        }

        private static bool IsIgnored(Item item)
        {
            // Preserve item if it is an instance of any type on the ignore list (covers inheritance)
            for (int i = 0; i < s_IgnoreTypes.Length; i++)
            {
                Type t = s_IgnoreTypes[i];
                if (t != null && t.IsInstanceOfType(item))
                    return true;
            }
            return false;
        }
    }
}
