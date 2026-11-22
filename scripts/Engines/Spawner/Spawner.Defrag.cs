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

/* Scripts\Engines\Spawner\Spawner.Defrag.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - New Module checkin
 *  6/25/2023, Adam (Defrag())
 *      since players can places houses on top of spawners, and since spawners, when they cannot find 
 *          a good spawn location, default to the location of a spawner, you can get a scenario where 
 *          all the creatures are spawning in the players house!
 *          To solve this, in Defrag() we check to see if a house has been dropped on us. If so,
 *          we move the spawner to the houses 'ban location'
 *	10/18/06, Adam
 *		- Remove notion of 'Fixed' non-decaying objects
 *		- Refresh() items still on a spawner before decaying (called by ItemDecay in Heartbeat)
 *	3/9/05, Adam
 *		In Defrag() we now check to see if the spawned item is in a HouseRegion.
 *		items in a HouseRegion are assumed to be 'freed' from the spawner and may therefore
 *		be removed from the spawners list of managed items.
 *		We needed this new check because putting an item in your house leaves the parent null which prevents 
 *		the 'fixed' item from decaying and a new item from being spawned. (See previous change)
 *	02/28/05, Adam
 *		1. Add new FreezeDecay property to freeze the decay on spawned items.
 *		2. When someone picks something up, parent is set to non-null. When this happens
 *			We can clear the 'Fixed' flag (item.Fixed = false).
 *		Detailed explanation: 
 *			Items are removed from the spawners list of spawned items during the Defrag() run. 
 *			This Defrag() run is roughly the frequency of the spawner. In the case of daily rares, 
 *			this respawn rate can be days long.
 *			Because spawned rares must necessarily sit on the ground for days and not decay, 
 *			they have a special 'fixed' attribute that prevents them from decaying.
 *			When a player gets a rare home, it will still be fixed until the spawner next runs and 
 *			defrags. When this happens, the 'fixed' attribute will be cleared, and the item will 
 *			decay as usual.
 */

using Server.Items;
using Server.Multis;
using Server.Regions;
using System;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        private bool m_worldLoaded = false;

        public bool InHouse(object o)
        {
            if (o != null)
            {
                IPoint3D? ip = o as IPoint3D;
                Item? item = o as Item;
                if (ip != null && item != null)
                {
                    ip = item.GetWorldTop();
                    Point3D p = new Point3D(ip);
                    Region region = Region.Find(p, item.Map);
                    if (region is HouseRegion)
                        return true;
                }
            }

            return false;
        }

        public override void WorldLoaded()
        {
            m_worldLoaded = true;
            return;
        }
        private bool DefragOk()
        {   // don't allow defragging of things that have not been fully initialized
            return m_worldLoaded;
        }
        public void Defrag()
        {
            #region Spawner placement under a house
            if (this.Map != Map.Internal && this.Map != null && DefragOk())
            {
                BaseHouse house;
                if ((house = BaseHouse.Find(this.Location, this.Map)) != null)
                {

                    // Exhibit spawners are okay inside houses. That's like the demo creatures
                    //  in the Siege Tent in the new player starting area.
                    // If we are spawning a multi like a camp, that's okay too
                    if (!this.Exhibit && !SpawningMulti())
                    {
                        Utility.Monitor.WriteLine(string.Format("Spawner {0} located at {1} in a house. Relocating...", this, this.Location), ConsoleColor.Red);

                        Point3D new_location = this.Location;
                        if (house.BanLocation != Point3D.Zero)
                            new_location = house.BanLocation;

                        this.Location = new_location;

                        if (Running)
                        {   // Defrag is called on server-up on all spawners running or not to do this Relocating, this is why we check 'Running' here
                            this.RemoveObjects();
                            this.Respawn();
                        }
                    }

                }
            }
            #endregion Spawner placement under a house

            #region Normal Defrag
            bool removed = false;

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object? o = m_Objects[i];

                if (o is Item)
                {
                    Item item = (Item)o;

                    // Adam:
                    // 1. If GetBool(BoolTable.OnSpawner) == false here, the lock was lifted in Item.OnItemLifted()
                    // 2. Because our Spawner's timer starts before the world is fully loaded, i.e., that thing to defrag
                    //  has not finished Deserialization, we cannot check OnSpawner until the world has finished loading.
                    if (item.Deleted || item.Parent != null || (DefragOk() && !item.GetItemBool(ItemBoolTable.OnSpawner)))
                    {
                        item.SetItemBool(Item.ItemBoolTable.OnSpawner, false);
                        m_Objects.RemoveAt(i);
                        --i;
                        removed = true;
                    }
                }
                else if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    if (m.Deleted)
                    {
                        m_Objects.RemoveAt(i);
                        --i;
                        removed = true;
                    }
                    else if (m is BaseCreature)
                    {
                        if (((BaseCreature)m).Controlled || ((BaseCreature)m).IsAnyStabled || ((BaseCreature)m).GetCreatureBool(CreatureBoolTable.IsTownshipLivestock))
                        {
                            m_Objects.RemoveAt(i);
                            --i;
                            removed = true;
                        }
                    }
                }
                else
                {
                    m_Objects.RemoveAt(i);
                    --i;
                    removed = true;
                }
            }

            if (removed)
                InvalidateProperties();
            #endregion Normal Defrag
        }
        private bool SpawningMulti()
        {
            if (ObjectNamesRaw != null)
                foreach (object o in ObjectNamesRaw)
                    if (o is string name)
                    {
                        Type? t = ScriptCompiler.FindTypeByName(name);
                        if (t != null)
                            if (t.IsAssignableTo(typeof(BaseMulti)))
                                return true;
                    }

            return false;
        }
    }
}
