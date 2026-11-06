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


/* Server/Utility.Commands.cs
 * ChangeLog:
 *  10/6/2026
 *      Moved from Utility.cs as part of a structural cleanup; no behavior change.
 *      Staff commands implemented in Utility; owns Utility.Initialize().
 */

using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace Server
{
    public partial class Utility
    {
        #region Staff Commands
        #region Remap Staff Commands
        /// <summary>
        /// In order to protect the integrity of our map, We no longer allow staff to create Spawners, Moongates, or Teleporters.
        /// We remap these particular items to their 'Event' equivalents which are limited in duration.
        /// Note: we may add provisions for allowing staff to build these things in some 'world building mode'.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="args"></param>
        public static bool AccessLevelRemap(Mobile from, string[] args)
        {
            if (from == null || from.AccessLevel == AccessLevel.Owner)
                return false;

            // not for the production world.
            if (Core.RuleSets.TestCenterRules() || Core.Debug)
                return false;

            return ItemRemap(args);
        }
        public static bool ItemRemap(string[] args)
        {
            bool remapped = false;
            if (args != null)
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower() == "spawner")
                    { args[i] = "EventSpawner"; remapped = true; }
                    if (args[i].ToLower() == "moongate")
                    { args[i] = "EventSungate"; remapped = true; }
                    if (args[i].ToLower() == "teleporter")
                    { args[i] = "EventTeleporter"; remapped = true; }
                    if (args[i].ToLower() == "confirmationmoongate")
                    { args[i] = "EventConfirmationSungate"; remapped = true; }
                }

            return remapped;
        }
        #endregion Remap Staff Commands

        public static void Initialize()
        {
            Server.CommandSystem.Register("RefreshRewardHue", AccessLevel.GameMaster, new CommandEventHandler(RefreshRewardHue_OnCommand));
            Server.CommandSystem.Register("SpawnableRect", AccessLevel.Owner, new CommandEventHandler(SpawnableRect_OnCommand));
            Server.CommandSystem.Register("ConvertMCToStandard", AccessLevel.Owner, new CommandEventHandler(ConvertMCToStandard_OnCommand));
            CommandSystem.Register("TestHouseSafeZone", AccessLevel.Administrator, new CommandEventHandler(TestHouseSafeZone_OnCommand));
        }
        #region ConvertMCToStandard
        [Usage("ConvertMCToStandard")]
        [Description("Converts an array of MovingCrates to standard crates.")]
        public static void ConvertMCToStandard_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Select the object area to dump...");
            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(ConvertArray_Callback), null);
        }
        private static void ConvertArray_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            string? filename = state as string;

            // Create rec and retrieve items within from bounding box callback result
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);

            // locals
            try
            {
                // See what we got
                List<Item> WhatWeGot = new();                   // surface items we found
                #region WhatWeGot
                IPooledEnumerable eable = map.GetItemsInBounds(rect);
                foreach (object obj in eable)
                {
                    if (obj is Mobiles.PlayerMobile) continue;  // ignore the <target> mobile (staff member)
                    if (obj is Mobile) continue;                // for now, ignore all mobiles
                    if (obj is BaseHouse) continue;
                    if (obj is Item old_item)
                        WhatWeGot.Add(old_item);
                }
                eable.Free();
                #endregion WhatWeGot

                #region Check
                {
                    foreach (Item item in WhatWeGot)
                        if (item is not MovingCrate)
                        {
                            from.SendMessage("{0} is not a moving crate", item);
                            return;
                        }
                }
                #endregion Check

                #region Process
                {
                    foreach (MovingCrate mc in WhatWeGot)
                    {
                        List<Item> temp = new();
                        LargeCrate lc = new();
                        Utility.CopyPropertyIntersection(lc, mc);
                        lc.Name += " " + mc.Label;
                        from.SendMessage("Moving {0} items from {1} to {2}...", mc.Items.Count, mc, lc);
                        int intended = mc.Items.Count;
                        foreach (Item item in mc.Items)
                            temp.Add(item);

                        foreach (Item item in temp)
                        {
                            mc.RemoveItem(item);
                            lc.AddItem(item);
                        }
                        Debug.Assert(mc.Deleted);
                        Debug.Assert(lc.Items.Count == intended);
                        from.SendMessage("{0} items moved from {1} to {2}...", lc.Items.Count, mc, lc);
                        lc.UpdateAllTotals();
                        lc.MoveToWorld(lc.Location, lc.Map);
                    }
                }
                #endregion Process
            }
            catch
            {
                ;
            }
        }
        #endregion ConvertMCToStandard
        #region RefreshRewardHue

        [Usage("RefreshRewardHue")]
        [Description("Resets the reward hue of the targeted item to match the owner's RewardHue.")]
        public static void RefreshRewardHue_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the reward item to update.");
            e.Mobile.Target = new RefreshRewardHueTarget();
        }

        private sealed class RefreshRewardHueTarget : Target
        {
            public RefreshRewardHueTarget()
                : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Item? item = targeted as Item;

                if (item == null)
                {
                    from.SendMessage("That is not an item.");
                    return;
                }

                // Rule: targeted item must have a hue (0 means no hue / default)
                // and should not already appear to be a reward item (i.e., not already RewardHue).
                if (item.Hue == 0)
                {
                    from.SendMessage("That item has no hue and does not appear to be a reward item.");
                    return;
                }

                // Rule: RootParent must be a PlayerMobile
                if (item.RootParent is not PlayerMobile pm)
                {
                    from.SendMessage("That item is not owned by a player (must be in a player's pack or equipped).");
                    return;
                }

                // Rule: player must have a nonzero RewardHue
                if (pm.RewardHue == 0)
                {
                    from.SendMessage("That player has no RewardHue set.");
                    return;
                }

                // If it already matches the player's RewardHue, treat it as already refreshed.
                if (item.Hue == pm.RewardHue)
                {
                    from.SendMessage("That item already matches the player's RewardHue.");
                    return;
                }

                // Apply
                int oldHue = item.Hue;
                item.Hue = pm.RewardHue;

                from.SendMessage("Updated item hue from {0} to {1} to match {2}'s RewardHue.",
                    oldHue, item.Hue, pm.Name);
            }
        }
        #endregion RefreshRewardHue
        #region SpawnableRect

        [Usage("SpawnableRect")]
        [Description("Tests our SpawnableRect.")]
        public static void SpawnableRect_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the center of the rect to grow.");
            e.Mobile.Target = new SpawnableRectTarget(null);
        }
        public class SpawnableRectTarget : Target
        {
            public SpawnableRectTarget(object? o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                StaticTarget? st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;

                if (loc != Point3D.Zero)
                {
                    Rectangle2D rect = new Rectangle2D(loc, new Point2D(loc.X + 1, loc.Y + 1));
                    rect = new SpawnableRect(rect).MaximumSpawnableRect(from.Map, 128, from.Map.GetAverageZ(rect.X, rect.Y));
                    from.SendMessage("Your max spawnable rect is {0}.", rect);
                }
                else
                    from.SendMessage("Can't grow a rect here.");
            }
        }
        public class SpawnableRect
        {
            Rectangle2D m_rect;
            public SpawnableRect(Rectangle2D rectToGrow)
            {
                m_rect = rectToGrow;
            }
            /// <summary>
            /// Grow the given rectangle in all directions in which a mobile may be spawned.
            /// Use caution: You should only call this in a closed area, otherwise your rectangle can grow quite large.
            /// </summary>
            /// <param name="map"></param>
            public Rectangle2D MaximumSpawnableRect(Map? map, int max, int averageZ)
            {

                Rectangle2D change = new Rectangle2D();
                // we need to start without width(1) and height(1) because it will put us over our max
                Rectangle2D orig = new Rectangle2D(m_rect.X, m_rect.Y, Math.Min(m_rect.Width, 0), Math.Min(m_rect.Height, 0));
                do
                {   // we will compare the old and new rect and see if it changed.
                    change = m_rect;
                    // let's start with the right side of the rect.
                    //  We will walk down the Y axis pushing out to the right X if we can spawn a mobile there.
                    if (!HitMax(orig, max))
                        for (int newY = m_rect.Start.Y; newY < m_rect.End.Y; newY++)
                        {
                            for (int X = m_rect.End.X; ; X++)
                            {
                                Rectangle2D bump = new Rectangle2D(m_rect.Start, m_rect.End);
                                if (Utility.CanSpawnLandMobile(map, X, newY, averageZ))
                                {
                                    m_rect.End = new Point2D(X, m_rect.End.Y);
                                    if (!m_rect.Equals(bump))
                                        goto LeftSide;
                                }
                                else
                                    break;  // try the next Y
                            }
                        }
                    // now we will extend the left side
                    LeftSide:
                    if (!HitMax(orig, max))
                        for (int newY = m_rect.Start.Y; newY < m_rect.End.Y; newY++)
                        {
                            for (int X = m_rect.Start.X; ; X--)
                            {
                                Rectangle2D bump = new Rectangle2D(m_rect.Start, m_rect.End);
                                if (Utility.CanSpawnLandMobile(map, X, newY, averageZ))
                                {
                                    m_rect.Start = new Point2D(X, m_rect.Start.Y);
                                    if (!m_rect.Equals(bump))
                                        goto Top;
                                }
                                else
                                    break;  // try the next Y
                            }
                        }
                    // now we will extend the top 
                    Top:
                    if (!HitMax(orig, max))
                        for (int newX = m_rect.Start.X; newX < m_rect.End.X; newX++)
                        {
                            for (int Y = m_rect.Start.Y; ; Y--)
                            {
                                Rectangle2D bump = new Rectangle2D(m_rect.Start, m_rect.End);
                                if (Utility.CanSpawnLandMobile(map, newX, Y, averageZ))
                                {
                                    m_rect.Start = new Point2D(m_rect.Start.X, Y);
                                    if (!m_rect.Equals(bump))
                                        goto Bottom;
                                }

                                else
                                    break;  // try the next X
                            }
                        }
                    // now we will extend the bottom
                    Bottom:
                    if (!HitMax(orig, max))
                        for (int newX = m_rect.Start.X; newX < m_rect.End.X; newX++)
                        {
                            for (int Y = m_rect.End.Y; ; Y++)
                            {
                                Rectangle2D bump = new Rectangle2D(m_rect.Start, m_rect.End);
                                if (Utility.CanSpawnLandMobile(map, newX, Y, averageZ))
                                {
                                    m_rect.End = new Point2D(m_rect.End.X, Y);
                                    if (!m_rect.Equals(bump))
                                        goto Loop;
                                }
                                else
                                    break;  // try the next X
                            }
                        }
                    Loop:;

                    // if the rect changed size, say height, then there are new points to check for the left and right.
                    //  keep calling until there is no more growth.
                } while (!change.Equals(m_rect) && !HitMax(orig, max));

                return m_rect;
            }
            private bool HitMax(Rectangle2D orig, int max)
            {
                return (Math.Abs(m_rect.Width - orig.Width) >= max) || (Math.Abs(m_rect.Height - orig.Height) >= max);
            }
        }
        #endregion SpawnableRect
        #endregion Staff Commands
    }
}
