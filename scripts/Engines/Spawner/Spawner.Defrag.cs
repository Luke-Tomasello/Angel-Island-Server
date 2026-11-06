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
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
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
