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

/* Scripts/Engines/Spawner/Spawner.cs
 * Changelog:
 *  5/25/2026, Adam
 * Historical changelog entries from 2004-present
 * have been moved to:
 *
 * Scripts/Engines/Spawner/History.txt
 */

using Server.Diagnostics;
using Server.Engines;
using Server.Engines.ChampionSpawn;
using System;
using System.Collections.Generic;
using static Server.Utility;

public enum SpawnerGraphics
{
    AllShardsRunningHue = 55,           // yellow
    AllShardsStoppedHue = 33,           // red 
    NerunRunningHue = 70,               // green
    NerunStoppedHue = 1231,             // violet
    CustomRunningHue = 1501,            // orange
    CustomStoppedHue = 1508,            // brown
}

namespace Server.Mobiles
{
    public partial class Spawner : Item
    {
        #region OnMovement
        #region Publish 4 Siege 
        private static bool PUBLISH_4_SIEGE = (Core.RuleSets.SiegeRules()) && PublishInfo.Publish >= 4;
        private Memory m_badGuys = PUBLISH_4_SIEGE ? new Memory() : null;
        public Memory BadGuys { get { return m_badGuys; } }
        #endregion Publish 4 Siege 
        public override bool HandlesOnMovement { get { return true; } }
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            #region Publish 4 Siege 
            /*
             * Publish 4
             * Shopkeeper Changes
             * If a shopkeeper is killed, a new shopkeeper will appear as soon as another player (other than the one that killed it) approaches.
             * http://www.uoguide.com/Publish_4
             */
            if (m != null && m.Player && PUBLISH_4_SIEGE)
            {   // we only have a memory (count > 0) if the spawned NPC died
                if (m_badGuys.Count > 0 && m_badGuys.Recall(m) == false)
                {   // if I need to spawn something and I don't remember this guy (m), spawn it now
                    this.NextSpawn = TimeSpan.Zero;     // now
                    m_badGuys = new Memory();           // we will respawn, no need for this old stale memory
                    this.SendMessageNearby("[Staff Message] Instant spawn override: BaseVendor.OnDeath");
                }
            }
            #endregion Publish 4 Siege 

            // auto showing of 'special spawners (they are color coded) to staff
            if (m != null && m.Player && m.AccessLevel >= AccessLevel.GameMaster)
                if (Core.Debug && (Core.RuleSets.StandardShardRules()))
                {
                    if (this.Visible == false)
                    {
                        this.Visible = true;
                        // auto hide this spawner again in 1 minute after movement stops
                        Timer.DelayCall(TimeSpan.FromSeconds(60), new TimerStateCallback(OnMovementTick), new object[] { this });
                    }
                }

            base.OnMovement(m, oldLocation);
        }
        private void OnMovementTick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is Spawner spawner && spawner.Deleted == false)
            {
                spawner.Visible = false;
            }
        }
        #endregion OnMovement

        public List<Type> ObjectTypes
        {
            get
            {
                List<Type> list = new List<Type>();
                List<string> names = ObjectNamesParsed;
                if (names.Count > 0)
                {
                    foreach (string name in names)
                    {
                        Type? type = SpawnerType.GetType(name);
                        if (type != null && list.Contains(type) == false)
                            list.Add(type);
                    }
                }
                return list;
            }
        }

        public virtual bool OkayStart()
        {
            return m_ObjectNames.Count > 0;
        }
        public virtual void Start()
        {
            if (OkayStart())
                DoTimer();

            UpdateDisplay();
        }
        public void Stop()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer.Flush();
            }

            UpdateDisplay();
        }

        public virtual void OnTick(bool external = false)
        {   // make virtual so we can get the tick in our derived class EventTimer

            if (external)
                // we are being 'triggered' externally. When this flag is set, we don't cleanup the mobile if this is an event spawner
                SetFlag(SpawnerAttribs.External, external);

            DoTimer();

            DoAutoHome();

            if (m_Group)
            {
                Defrag();

                if (m_Objects.Count == 0)
                {
                    Respawn();
                }
                else
                {
                    return;
                }
            }
            else
            {
                Spawn();
            }
        }

        private void DoAutoHome()
        {
            // If AutoHome is enabled, and we have spawned objects,
            // return mobiles to their Home when the last player leaves the region.
            if (AutoHome && Objects != null && Objects.Count > 0)
            {
                int homeRange = this.HomeRange;

                // If HomeRange is not meaningful, do nothing.
                if (homeRange <= 0)
                    return;

                foreach (object o in Objects)
                {
                    Mobile? mob = o as Mobile;

                    if (mob == null || mob.Deleted)
                        continue;

                    if (this.Map != Map.Felucca)
                        continue;

                    // just in case defrag didn't catch it
                    if (mob is BaseCreature creature && creature.IsTame)
                        continue;

                    // Only home individual mobiles that have wandered too far.
                    if (mob.Map == this.Map && mob.Combatant == null && mob.GetDistanceToSqrt(this.Location) > homeRange)
                    {
                        // Return just this mob to home.
                        mob.MoveToWorld(this.Location, this.Map);
                    }
                }
            }

        }

        public void Respawn(bool force = false)
        {
            RemoveObjects(force);

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            for (int i = 0; i < Count /*GetSlotCount(index: 0)*/ /*m_SlotCount*/; i++)
                Spawn();

            tc.End();
            if (Debug)
                this.SendMessageNearby(string.Format("Respawn took {0}", tc.TimeTaken));

            // 11/18/22, Adam: after a respawn, recalculate the NextSpawn.
            ResetTimer();
        }
        /*
         * Nerun’s Premium Spawner was originally designed to “fill all deficits” at once:
         * when the timer fired, every missing creature in every slot was spawned together.
         * While this works well for large outdoor zones, it feels unnatural in confined areas
         * where players can see the entire encounter space (e.g., dungeon rooms).
         *
         * This implementation retains Nerun’s slot and count configuration model, but
         * replaces its batch behavior with a *streaming* model identical to Classic RunUO:
         * one creature spawns per timer tick, with a new random delay (Min–Max) scheduled
         * each time. The result is steadier pacing, smoother world repopulation, and a
         * more authentic Ultima Online feel.
         */
        public virtual void Spawn()
        {
            // Classic pacing for ALL modes; Nerun/Multis are treated as config schema (slots+counts)
            if (GetFlag(SpawnerAttribs.ModeNeruns) || GetFlag(SpawnerAttribs.ModeMulti))
            {
                Defrag();

                // First pass: validate slot table, compute deficits, and total them (for weighted pick)
                int slotCount = m_ObjectNames.Count;
                if (slotCount == 0)
                    return;

                // We’ll store deficits per slot so we don’t recompute in the second pass
                // (use an array to avoid extra allocations)
                int[] deficits = new int[slotCount];
                int totalDeficit = 0;

                for (int ix = 0; ix < slotCount; ix++)
                {
                    // Ensure slot count exists; patch if missing (keeps your original safety behavior)
                    if (ix >= GetSlotTableSize())
                    {
                        Utility.Monitor.WriteLine(string.Format("Warning: Count index out of bounds: {0}", this), ConsoleColor.Red);
                        Utility.Monitor.WriteLine(string.Format("Warning: Patching: {0}", ix), ConsoleColor.Green);

                        SetSlotCount(index: ix, value: 1);

                        var logger = new LogHelper("Nerun Spawner Patch.log", false, true, true);
                        logger.Log(LogType.Item, this, string.Format("Missing 'count' for spawner item: '{0}' at index: {1}", m_ObjectNames[ix], ix));
                        logger.Finish();
                    }

                    int want = GetSlotCount(index: ix);
                    int have = 0;

                    // Count live objects for this slot by matching types from the colon list
                    List<string> nameList = GetObjectNames(m_ObjectNames[ix] as string);
                    if (nameList != null && nameList.Count > 0)
                    {
                        // Pre-resolve types for this slot to avoid repeated lookups
                        var types = new List<Type>(nameList.Count);
                        foreach (string name in nameList)
                        {
                            Type? t = SpawnerType.GetType(name);
                            if (t != null) types.Add(t);
                        }

                        if (types.Count > 0)
                        {
                            foreach (object o in m_Objects)
                            {
                                if (o == null) continue;
                                Type ot = o.GetType();
                                // exact-type match like your original code
                                for (int j = 0; j < types.Count; j++)
                                {
                                    if (ot == types[j]) { have++; break; }
                                }
                            }
                        }
                    }

                    int need = want - have;
                    if (need < 0) need = 0;

                    deficits[ix] = need;
                    totalDeficit += need;
                }

                // Nothing missing → nothing to do this tick
                if (totalDeficit <= 0)
                    return;

                // Second pass: weighted random pick by deficit (classic pacing = 1 spawn per tick)
                // e.g., if slot A needs 3 and slot B needs 1, A is 3x more likely to be chosen.
                int pick = Utility.RandomMinMax(1, totalDeficit);
                int running = 0;
                int chosenIndex = -1;

                for (int ix = 0; ix < slotCount; ix++)
                {
                    int need = deficits[ix];
                    if (need <= 0) continue;

                    running += need;
                    if (pick <= running)
                    {
                        chosenIndex = ix;
                        break;
                    }
                }

                if (chosenIndex >= 0)
                {
                    // Classic pacing: spawn exactly ONE from that slot this tick.
                    // Your Spawn(index, count) should already handle colon-list weighting inside the slot.
                    Spawn(chosenIndex, 1);
                }

                // Timer reset behavior stays wherever you manage it (outside this method or after return).
                return;
            }

            // Classic mode: unchanged — pick one entry from the single list.
            if (m_ObjectNames.Count > 0)
                Spawn(Utility.Random(m_ObjectNames.Count));
        }

        public void Spawn(string objectName)
        {
            for (int i = 0; i < m_ObjectNames.Count; i++)
            {
                if ((string)m_ObjectNames[i] == objectName)
                {
                    Spawn(i);
                    break;
                }
            }
        }
        public void Spawn(int index, int count)
        {
            if ((!GetFlag(SpawnerAttribs.ModeNeruns) && !GetFlag(SpawnerAttribs.ModeMulti)) || count == 0)
                return;
            else
                for (int ix = 0; ix < count; ix++)
                    Spawn(index);
        }

        public void Spawn(int index)
        {
            Map map = Map;

            if (map == null || map == Map.Internal || m_ObjectNames.Count == 0 || index >= m_ObjectNames.Count)
                return;

            Defrag();

            if ((!GetFlag(SpawnerAttribs.ModeNeruns) && !GetFlag(SpawnerAttribs.ModeMulti)))
            {
                if (m_Objects.Count >= GetSlotCount(index: 0)/*m_SlotCount*/)
                    return;
            }

            string thingToSpawn = string.Empty;
            string? text = ((string)m_ObjectNames[index]).Trim();
            //adam 8/29/22, we now allow ':' delimited lists of creatures
            string[] tokens = text.Split(':', StringSplitOptions.RemoveEmptyEntries);
            List<string> valid = new List<string>();
            int selector = Utility.Random(tokens.Length);
            for (int ix = 0; ix < tokens.Length; ix++)
            {
                string token = tokens[ix];

                Type? type = SpawnerType.GetType(token);

                if (type == null)
                {
                    Failed = true;
                    Failure = $"{token} is not a valid type name.";
                    Utility.Monitor.WriteLine(Failure, ConsoleColor.Red);
                    tokens[ix] = string.Empty;
                }
                else
                    valid.Add(token);
            }
            // select it
            if (selector >= valid.Count || valid.Count == 0)
            {
                Failed = true;
                Failure = $"No valid object to spawn at selector {selector}.";
                Console.WriteLine(Failure);
                Console.WriteLine($"\tLocation: {this.Location}, Map: {this.Map}, Serial: {this.Serial}.");
                return;
            }
            else
                thingToSpawn = valid[selector];

            try
            {
                // create the mobile or item
                object? o = CreateRaw(thingToSpawn);

                if (o != null)
                {
                    try
                    {
                        if (o is Mobile)
                        {
                            Mobile m = (Mobile)o;
                            m_Objects.Add(m);
                            InvalidateProperties();
                            Point3D loc = (Utility.IsTownVendor(m) ? this.Location : GetSpawnPosition(o));
                            MoveToWorld(m, loc, map);
                            OnAfterMobileSpawn(m);   //plasma: new "event" allows you to make changes to the mob after spawn
                            m.OnAfterSpawn();
                        }
                        else if (o is Item)
                        {
                            Item item = (Item)o;
                            m_Objects.Add(item);
                            InvalidateProperties();
                            MoveToWorld(item, GetSpawnPosition(o), map);
                            OnAfterItemSpawn(item);
                            item.OnAfterSpawn();

                            // Adam: Warn if we're spawning stuff in a house.
                            //	This is a problem because the item will be orphaned by the spawner (? - not sure this is the case anymore)
                            //	which can lead to excessive item generation.
                            if (InHouse(item) == true)
                            {
                                Console.WriteLine("Warning: House spawn: Item({0}, {1}, {2}), Spawner({3}, {4}, {5})", item.Location.X, item.Location.Y, item.Location.Z, this.Location.X, this.Location.Y, this.Location.Z);
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Warning: Spawner({0}, {1}, {2}) generating object not template compatible", this.Location.X, this.Location.Y, this.Location.Z);
                    }
                }
                else
                    Console.WriteLine("Warning: Spawner({0}, {1}, {2}, {3}) generating object that does not exist: {4}", this.Location.X, this.Location.Y, this.Location.Z, this.Map, thingToSpawn);
            }
            catch (Exception ex)
            {
                Failed = true;
                LogHelper.LogException(ex);
            }
        }

        private bool ThingInList(string text /*text from spawner */, string objToMatch /* what we are to lookup */)
        {
            text = text.ToLower();
            objToMatch = objToMatch.ToLower();
            //adam 8/29/22, we now allow ':' delimited lists of creatures
            string[] tokens = text.Split(':', StringSplitOptions.RemoveEmptyEntries);
            for (int ix = 0; ix < tokens.Length; ix++)
                if (tokens[ix] == objToMatch)
                    return true;
            return false;
        }
        public object? CreateRaw(string name)
        {
            Map map = Map;

            if (map == null || map == Map.Internal)
                return null;

            // make sure the thing we are asked to create exists in our list 
            //  of stuff to create.
            bool bFound = false;
            for (int i = 0; i < m_ObjectNames.Count; i++)
            {
                if (ThingInList((string)m_ObjectNames[i], name))
                {
                    bFound = true;
                    break;
                }
            }

            if (bFound == false)
                return null;

            Type? type = SpawnerType.GetType(name);
            object? o = null;

            if (type != null)
            {
                try
                {
                    o = Activator.CreateInstance(type);

                    if (o != null)
                    {
                        if (o is Mobile)
                        {
                            Mobile m = (Mobile)o;
                            if (m is BaseCreature)
                            {
                                BaseCreature c = (BaseCreature)m;

                                if (TemplateEnabled) //copy our template props to the spawning mobile
                                {
                                    bool templateMobileBroken = m_TemplateMobile.Deleted == true;
                                    if (templateMobileBroken)
                                    {   // something is wing-wang. Our template has been deleted
                                        Utility.Monitor.WriteLine("Error: Our Mobile template has been deleted.", ConsoleColor.Red, m_TemplateMobile);
                                        LogHelper logger = new LogHelper("spawner.CreateRaw.log", false);
                                        logger.Log(LogType.Text, "Our Mobile template has been deleted. Sound the alarm!");
                                        logger.Log(LogType.Mobile, m_TemplateMobile);
                                        logger.Log(LogType.Item, this);
                                        logger.Finish();

                                        // clear the template
                                        m_TemplateMobile = null;
                                    }
                                    else
                                    {
                                        TimeSpan OldLifespan = c.Lifespan;
                                        DateTime OldCreation = c.Created;
                                        Utility.CopyPropertyIntersection(c, m_TemplateMobile);
                                        Utility.CopySkills(c, m_TemplateMobile);
                                        Utility.SwapBackpack(c, TemplateMobile);    // swap backpack

                                        if (!DynamicCopy)
                                            Utility.CopyLayers(c, m_TemplateMobile, CopyLayerFlags.Default);

                                        if (DynamicCopy)
                                        {
                                            //get use a new name/sex/clothing/body layout
                                            c.InitBody();
                                            c.InitOutfit();
                                        }
                                        // restore these
                                        c.Lifespan = OldLifespan;
                                        c.Created = OldCreation;
                                        // these should not carry over to the spawned mobile
                                        c.SpawnerTempMob = false;
                                        c.IsIntMapStorage = false;
                                    }
                                }

                                c.RangeHome = (m_WalkRange == -1) ? m_HomeRange : m_WalkRange;
                                c.CurrentWayPoint = m_WayPoint;
                                c.NavDestination = m_NavDest;
                                if (m_Team > 0) c.Team = m_Team;    // why don't we just set it without checking?
                                c.Home = this.Location;
                                c.Spawner = this;                   //Pix: give the spawned creature a ref to this spawner
                                c.SterlingMin = m_SterlingMin;
                                c.SterlingMax = m_SterlingMax;

                                // tamable override
                                if (Tamable != BoolFlags.Default)
                                    c.Tamable = (Tamable == BoolFlags.True) ? true : false;

                                //if we have a navdestination as soon as we spawn start on it
                                //if (c.NavDestination != NavDestinations.None)
                                if (!string.IsNullOrEmpty(c.NavDestination))
                                    c.AIObject.Think();

                                /////////////////////////////
                                // customize the mob spawned
                                // IsInvulnerable has been deprecated. For spawners, we will use blessed
                                //  10/24/22, Adam: IsInvulnerable is still in RunUO 2.6 and the client still respects it.
                                //  putting it back.
                                c.IsInvulnerable = m_Invulnerable;

                                //  10/24/22, Adam: Exhibit creatures to replace Blessed
                                if (m_Exhibit)
                                    MakeExhibit(c);

                                // 12/28/22, Adam: Should guards ignore this creature? (town invasions)
                                c.GuardIgnore = GuardIgnore;

                                // training dummy. Doesn't move, doesn't take damage, and doesn't fight back
                                if (Dummy)
                                    MakeDummy(c);

                                // robot creature - robot AI, not tame, but only wonders and stays(control orders.)
                                c.Robot = Robot;

                                // should this creature take damage?
                                c.BlockDamage = BlockDamage;

                                // should this creature be unstoppable? (requires BlockDamage)
                                c.MissionCritical = MissionCritical;

                                // not only changes AI to Melee, but also assigns skill, a weapon, and equips it
                                if (m_MorphMode == MorphMode.Melee)
                                    ChampEngine.MakeMelee(c, c.AIObject, boss: false);

                                // this may be a debug spawner, spawning creatures in debug mode
                                //c.DebugMode = Debug ? DebugFlags.AI : DebugFlags.None;

                                // if it's not a template mob, it may be a paragon!
                                if (TemplateEnabled == false && m_Exhibit == false)
                                    if (Utility.RandomChance(10) && HighPowerTameNearby())
                                        c.MakeParagon();
                            }
                        }
                        else if (o is Item)
                        {
                            Item item = (Item)o;

                            if (!Registry.Contains(item.ItemID))
                                Registry.Add(item.ItemID);

                            if (m_TemplateItem != null && m_TemplateItem.Deleted == true)
                            {   // something is wing-wang. Our template has been deleted
                                Console.WriteLine("Our Item template has been deleted. Sound the alarm!", m_TemplateItem);
                                LogHelper logger = new LogHelper("spawner.CreateRaw.log", false);
                                logger.Log(LogType.Text, "Our Item template has been deleted. Sound the alarm!");
                                logger.Log(LogType.Item, m_TemplateItem);
                                logger.Finish();
                            }

                            if (TemplateEnabled)
                            {
                                if (TemplateStyle == TemplateMode.CopyProperties)
                                {
                                    Utility.CopyPropertyIntersection(item, m_TemplateItem);
                                    item.LastMoved = DateTime.UtcNow;    // refresh decay
                                    item.SpawnerTempItem = false;        // Flag this as not a template
                                    item.IsIntMapStorage = false;        // flag this a not intmapstorage			
                                }
                                else if (TemplateStyle == TemplateMode.Dupe)
                                {
                                    item.Delete();
                                    o = Utility.Dupe(m_TemplateItem);
                                    System.Diagnostics.Debug.Assert(o != null);
                                    item = (Item)o;
                                    item.LastMoved = DateTime.UtcNow;    // refresh decay
                                    item.SpawnerTempItem = false;        // Flag this as not a template
                                    item.IsIntMapStorage = false;        // flag this a not intmapstorage			
                                }
                                else
                                    System.Diagnostics.Debug.Assert(false);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {   // kill the damn thing
                    Running = false;
                    if (this is EventSpawner)
                    {
                        (this as EventSpawner).EventStart = DateTime.MinValue.ToString();
                        (this as EventSpawner).EventEnd = (this as EventSpawner).EventStart;
                    }
                    SendMessageNearby("Incompatible types");
                    SendMessageNearby("You may not mix template types, for example Items and Mobiles");
                    Console.WriteLine("Warning: Spawner({0}, {1}, {2}) generating object not template compatible", this.Location.X, this.Location.Y, this.Location.Z);
                    Console.WriteLine("{0}", ex);
                    LogHelper.LogException(ex);
                }
            }

            return o;
        }

        public int CountObjects(string objectName)
        {
            Defrag();

            int count = 0;
            if (GetFlag(SpawnerAttribs.ModeNeruns) || GetFlag(SpawnerAttribs.ModeMulti) && objectName.Contains(':'))
            {
                List<string> list = GetObjectNames(objectName);
                for (int i = 0; i < m_Objects.Count; ++i)
                    foreach (string name in list)
                        if (Insensitive.Equals(name, m_Objects[i].GetType().Name))
                            ++count;
            }
            else
            {
                for (int i = 0; i < m_Objects.Count; ++i)
                    if (Insensitive.Equals(objectName, m_Objects[i].GetType().Name))
                        ++count;
            }

            return count;
        }
        public void RemoveObjects(string objectName)
        {
            Defrag();

            objectName = objectName.ToLower();

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object? o = m_Objects[i];

                if (Insensitive.Equals(objectName, o.GetType().Name))
                {
                    if (o is Item)
                        ((Item)o).Delete();
                    else if (o is Mobile)
                        ((Mobile)o).Delete();
                }
            }

            InvalidateProperties();
        }
        public void RemoveObjects(bool force = false)
        {
            Defrag();

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object? o = m_Objects[i];

                if (o is Item item && (!item.GetItemBool(ItemBoolTable.IsLinked) || force))
                    ((Item)o).Delete();
                else if (o is Mobile mobile && (!mobile.GetMobileBool(Mobile.MobileBoolTable.IsLinked) || force))
                    ((Mobile)o).Delete();
            }

            InvalidateProperties();
        }

        public void BringToHome()
        {
            Defrag();

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object? o = m_Objects[i];

                if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    m.MoveToWorld(Location, Map);
                }
                else if (o is Item)
                {
                    Item item = (Item)o;

                    item.MoveToWorld(Location, Map);
                }
            }
        }

        public override void MoveToWorld(Point3D location, Map map, Mobile? responsible = null)
        {
            base.MoveToWorld(location, map);
        }
        private bool CanDeleteTemplate(IEntity o)
        {
            if (o is Mobile m && m.SpawnerTempRefCount == 0) { return true; }
            else if (o is Item i && i.SpawnerTempRefCount == 0) { return true; }
            return false;
        }
        public override void OnDelete()
        {
            base.OnDelete();

            // Remove Templates
            if (m_LootPack != null)
            {
                m_LootPack.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_LootPack))
                    m_LootPack.Delete();
            }

            if (m_CarvePack != null)
            {
                m_CarvePack.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_CarvePack))
                    m_CarvePack.Delete();
            }

            if (m_TemplateMobile != null)
            {
                m_TemplateMobile.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_TemplateMobile))
                    m_TemplateMobile.Delete();
            }

            if (m_TemplateItem != null)
            {
                m_TemplateItem.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_TemplateItem))
                    m_TemplateItem.Delete();
            }

            if (m_ArtifactPack != null)
            {
                m_ArtifactPack.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_ArtifactPack))
                    m_ArtifactPack.Delete();
            }

            // Remove Creatures
            RemoveObjects();
            if (m_Timer != null)
                m_Timer.Stop();

            // inform our spawner cache this spawner has been deleted
            try { CacheFactory.UpdateQuickTables(this, CacheFactory.QuickTableUpdate.Deleted); }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            Instances.Remove(this);
        }
    }
}