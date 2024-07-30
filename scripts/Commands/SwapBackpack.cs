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

/* Scripts/Commands/SwapBackpack.cs
 * CHANGELOG:
 *  11/38/21, Adam (CheckHold)
 *      Add calls to check hold to ensure container that holds the swap backpack (if there is one) can hold the 
 *          incoming backpack.
 *      Also, if we are swapping into another container, call AddItem and not MoveToWorld
 *	3/15/16, Adam
 *		Initial Version
 */

using Server.Diagnostics;
using Server.Items;
using Server.Multis;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Commands
{
    public class SwapBackpack
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("SwapBackPack", AccessLevel.Player, new CommandEventHandler(SwapBackPack_OnCommand));
        }
        [Usage("SwapBackPack")]
        [Description("Swap out a players backpack complete with all contents. Keeps newbied and blessed items.")]
        public static void SwapBackPack_OnCommand(CommandEventArgs e)
        {
            try
            {
                SwapBackPackWorker(e);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        /// <summary>
        /// Swaps one backpack for another.
        /// This will be part of the new quick resupply system. 
        /// Players will be able to have N backpacks at their home or in a bankbox.
        /// The player can they 'swap out' their old backpack for a new one.
        /// Any blessed or newbiwd items will carry over to the new backpack
        /// (players will have a different access to this mechanism.)
        /// </summary>
        public static void SwapBackPackWorker(CommandEventArgs e)
        {
            if (e.Mobile.Backpack == null || e.Mobile.Backpack.Deleted)
            {
                e.Mobile.SendMessage("You have no backpack.");
                return;
            }
            e.Mobile.SendMessage("Target the backpack you wish to swap.");
            e.Mobile.Target = new SwapTarget();
        }

        public class TickState
        {
            bool m_add;
            bool m_moveBlessed;
            public bool Add { get { return m_add; } }
            public bool MoveBlessed { get { return m_moveBlessed; } }
            public TickState(bool add, bool move_blessed)
            {
                m_add = add;
                m_moveBlessed = move_blessed;
            }
        }
        public class SwapTarget : Target
        {
            public SwapTarget()
                : base(12, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Backpack)
                {
                    if ((targeted as Backpack) != from.Backpack)
                    {
                        Container player_backpack = from.Backpack;
                        Backpack target_backpack = (targeted as Backpack);
                        BaseHouse house = BaseHouse.FindHouseAt(target_backpack);
                        if (house != null)
                        {   // it's in their house
                            if (!(target_backpack.RootParent is Mobile))
                            {
                                bool locked_down = !target_backpack.Movable;
                                if (!house.IsSecure(target_backpack))
                                {   // check for overload of parent container
                                    if (target_backpack.Parent != null && target_backpack.Parent is BaseContainer bc && !bc.CheckHold(from, player_backpack, true))
                                    {
                                        // message has already been sent
                                    }
                                    else
                                    {
                                        if (house.IsFriend(from) || house.IsOwner(from))
                                        {
                                            List<Item> Player_backpack_contents = player_backpack.Items;
                                            List<Item> target_backpack_contents = target_backpack.Items;

                                            // remove the items
                                            foreach (Item item in Player_backpack_contents)
                                                Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { player_backpack, item, new TickState(false, true) });
                                            foreach (Item item in target_backpack_contents)
                                                Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { target_backpack, item, new TickState(false, true) });

                                            // lets begin
                                            if (locked_down) house.Release(from, target_backpack);
                                            player_backpack.Movable = true;
                                            if (target_backpack.Parent != null && target_backpack.Parent is BaseContainer bp)
                                                bp.AddItem(from.Backpack);
                                            else
                                                from.Backpack.MoveToWorld(target_backpack.Location, from.Map);
                                            if (locked_down) house.LockDown(from, player_backpack);
                                            from.Backpack = null;
                                            from.AddItem(target_backpack);

                                            // Note, at this point, player_backpack and target_backpack are reversed

                                            // lets put the all the items items back except for blessed and newbied
                                            foreach (Item item in Player_backpack_contents)
                                                Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { player_backpack, item, new TickState(true, false) });
                                            foreach (Item item in target_backpack_contents)
                                                Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { target_backpack, item, new TickState(true, false) });

                                            // okay, now lets move any blessed or newbied items back to the players backpack
                                            foreach (Item item in Player_backpack_contents)
                                                Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { target_backpack, item, new TickState(true, true) });

                                            // now place any blessed stuff from players origional backpack into the storage pack
                                            foreach (Item item in target_backpack_contents)
                                                Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { player_backpack, item, new TickState(true, true) });

                                            from.SendMessage("Backpack refreshed.");
                                        }
                                        else
                                            from.SendMessage("You are not a friend of this house.");
                                    }
                                }
                                else
                                    from.SendMessage("That is secure and cannot be used.");
                            }
                            else
                                from.SendMessage("That is not your backpack.");
                        }
                        else
                        { // see if its a bankbox
                            List<object> list = GetParentStack(targeted as Item);
                            // if the first item is null, the backpsck (or whatever) is on the ground
                            // there needs to be at least two elements in the list, the bankbox and the parent mobile
                            // if the top of the list is not a mobile, it's not a bankbox
                            // the second to last item must be a bank box
                            if (list[0] == null || list.Count < 2 || !(list[list.Count - 1] is Mobile) || !(list[list.Count - 2] is BankBox))
                            {
                                from.SendMessage("That must be in your bank box.");
                                return;
                            }
                            // okay, looks like the pack is in the owners bankbox, let's continue

                            // Razor and probably other macor programs don't recognize changes in reagents and such when bags are sawped.
                            // For this reason, we need remove all the items from both backpacks, then read add them
                            //  First we remove them from the players backpack to tell razor reagents et al. are now zero.
                            //  We then move the objects from the target to the new players backpack to force razor to reinventory
                            // Interesting side-note: this timer-tap-dance wasn't necessary when swapping from the house (coded above.) I believe 
                            //  this is due to the fact that the bankbox is stored on the player character and that razor could still *see* the old reagents.
                            //  I decided to add the timer-tap-dance to both implementations to help yet unknown clients and macro toold understand what's going on.
                            List<Item> Player_backpack_contents = from.Backpack.Items;
                            List<Item> target_backpack_contents = (targeted as Backpack).Items;
                            player_backpack = from.Backpack;
                            target_backpack = targeted as Backpack;
                            Container target_container = list[0] as Container;  // list[0] will be the nested container in the bankbox, or the bankbox itself.                            

                            if (target_backpack.Parent != null && target_backpack.Parent is Container c && !c.CheckHold(from, player_backpack, true))
                            {
                                // message has already been sent
                            }
                            else
                            {
                                // remove the items including newbied and blessed
                                foreach (Item item in Player_backpack_contents)
                                    Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { player_backpack, item, new TickState(false, true) });
                                foreach (Item item in target_backpack_contents)
                                    Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { target_backpack, item, new TickState(false, true) });

                                // do the actual swap
                                player_backpack.Movable = true;                     // make the backpack movable
                                target_container.AddItem(player_backpack);          // put the player backpack into the container
                                Point3D location = new Point3D(target_backpack.Location);
                                player_backpack.Location = location;                // move the backpack in their bankbox to the same location as the targeted one
                                from.Backpack = null;                               // kill the old backpack
                                from.AddItem(target_backpack);                      // drop the targeted backpack on the player

                                // Note, at this point, player_backpack and target_backpack are reversed

                                // lets put all the items back except newbied and blessed
                                foreach (Item item in Player_backpack_contents)
                                    Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { player_backpack, item, new TickState(true, false) });
                                foreach (Item item in target_backpack_contents)
                                    Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { target_backpack, item, new TickState(true, false) });

                                // okay, now lets move any blessed or newbied items back to the players backpack
                                foreach (Item item in Player_backpack_contents)
                                    Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { target_backpack, item, new TickState(true, true) });

                                // now place any blessed stuff from players origional backpack into the storage pack
                                foreach (Item item in target_backpack_contents)
                                    Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(Tick), new object[] { player_backpack, item, new TickState(true, true) });

                                from.SendMessage("Backpack refreshed.");
                            }
                        }

                    }
                    else
                        from.SendMessage("Please select a different backpack.");
                }
                else
                {
                    from.SendMessage("That is not a backpack.");
                }
            }
        }

        private static void Tick(object state)
        {
            object[] aState = (object[])state;
            Container backpack = aState[0] as Container;
            Item item = aState[1] as Item;
            bool add = (aState[2] as TickState).Add;
            bool moveBlessed = (aState[2] as TickState).MoveBlessed;

            if (add == true)
            {
                if (moveBlessed == true)
                {
                    if (item.GetFlag(LootType.Newbied) || item.GetFlag(LootType.Blessed))
                        backpack.AddItem(item);
                }
                else
                    if (!item.GetFlag(LootType.Newbied) && !item.GetFlag(LootType.Blessed))
                    backpack.AddItem(item);
            }
            else
            {
                backpack.RemoveItem(item);
            }

            // recalc total weight
            backpack.UpdateAllTotals();
        }

        public static List<object> GetParentStack(Item item)
        {
            List<object> list = new List<object>();
            object p = item.Parent;
            list.Add(p);    // could be null, that's ok

            while (p is Item)
            {
                Item pitem = (Item)p;

                if (pitem.Parent == null)
                {
                    break;
                }
                else
                {
                    p = pitem.Parent;
                    list.Add(p);
                }
            }

            return list;
        }
    }
}