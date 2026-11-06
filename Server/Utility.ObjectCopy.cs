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


/* Server/Utility.ObjectCopy.cs
 * ChangeLog:
 *  10/6/2026
 *      Moved from Utility.cs as part of a structural cleanup; no behavior change.
 *      Object duplication, property copying, layers, creature template copying and property introspection.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Server.Items.TownshipStone;
using CPA = Server.CommandPropertyAttribute;
namespace Server
{
    public partial class Utility
    {
        #region Object Duplication & Property Copying
        #region Creature Template Copying

        public static List<Item> CopyLoot(Type source_type)
        {
            List<Item> list = new();
            BaseCreature? src = null;
            try
            {
                if (!source_type.IsAssignableTo(typeof(BaseCreature))) return list;
                src = (BaseCreature)Activator.CreateInstance(source_type);
                if (src == null) return list;
                src.Kill();
                if (src.Corpse != null && src.Corpse.Items != null)
                    foreach (Item item in src.Corpse.Items)
                        list.Add(item);
            }
            catch
            {
                return list;
            }
            finally
            {
                if (src != null)
                    src.Delete();
            }

            return list;
        }
        public static bool CopyBody(Type source_type, BaseCreature dest)
        {
            BaseCreature? src = null;
            try
            {
                if (!source_type.IsAssignableTo(typeof(BaseCreature))) return false;
                src = (BaseCreature)Activator.CreateInstance(source_type);
                if (src == null) return false;
                if (dest == null) return false;
                dest.Body = src.Body;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (src != null)
                    src.Delete();
            }

            return true;
        }
        public static bool CopyConstruction(Type source_type, BaseCreature dest)
        {
            BaseCreature? src = null;
            try
            {
                if (!source_type.IsAssignableTo(typeof(BaseCreature))) return false;
                src = (BaseCreature)Activator.CreateInstance(source_type);
                if (src == null) return false;
                if (dest == null) return false;
                // See CopyBody()
                dest.AI = src.AI;
                dest.FightMode = src.FightMode;
                dest.FightStyle = src.FightStyle;
                dest.RangePerception = src.RangePerception;
                dest.RangeFight = src.RangeFight;
                dest.ActiveSpeed = src.ActiveSpeed;
                dest.PassiveSpeed = src.PassiveSpeed;

                dest.BardImmune = src.BardImmune;
                dest.UsesHumanWeapons = src.UsesHumanWeapons;
                dest.UsesBandages = src.UsesBandages;
                dest.UsesPotions = src.UsesPotions;
                dest.CanRun = src.CanRun;
                dest.CanReveal = src.CanReveal;
                dest.CrossHeals = src.CrossHeals;

                dest.Fame = src.Fame;
                dest.Karma = src.Karma;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (src != null)
                    src.Delete();
            }

            return true;
        }
        public static bool CopyStats(Type source_type, BaseCreature dest, double stat_multiplier = 1, double damage_multiplier = 1, double skill_multiplier = 1)
        {
            BaseCreature? src = null;
            try
            {
                if (!source_type.IsAssignableTo(typeof(BaseCreature))) return false;
                src = (BaseCreature)Activator.CreateInstance(source_type);
                if (src == null) return false;
                if (dest == null) return false;
                dest.SetStr((int)(src.RawStr * stat_multiplier));
                dest.SetDex((int)(src.RawDex * stat_multiplier));
                dest.SetInt((int)(src.RawInt * stat_multiplier));

                dest.SetHits((int)(src.HitsMax * stat_multiplier));
                dest.SetMana((int)(src.ManaMax * stat_multiplier));

                dest.VirtualArmor = (int)(src.VirtualArmor * stat_multiplier);

                dest.SetDamage((int)(src.DamageMin * damage_multiplier), (int)(src.DamageMax * damage_multiplier));

                if (src.Skills.Length != dest.Skills.Length)
                    return false;

                Skills source_skills = src.Skills;
                Skills dest_skills = dest.Skills;
                for (int i = 0; i < source_skills.Length; ++i)
                    dest_skills[i].Base = source_skills[i].Base * skill_multiplier;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (src != null)
                    src.Delete();
            }

            return true;
        }
        #endregion Creature Template Copying
        #region Temporary Objects
        public static Item MakeTempItem(Item item_to_dupe)
        {
            if (item_to_dupe == null) return null;
            Item? new_item = Utility.Dupe(item_to_dupe);
            if (new_item == null) return null;
            (new_item as RolledUpSheetMusic).RequiresBackpack = false;
            new_item.SetItemBool(Item.ItemBoolTable.TempObject, true);  // mark as okay to delete
            new_item.IsStaffOwned = true;                       // this keeps other rules from deleting it
            return new_item;
        }
        public static Mobile MakeTempMobile(string? name = null)
        {
            Mobile m = new Mobile();
            if (m != null)
            {
                m.Name = name ?? "music context object";
                m.SetMobileBool(Mobile.MobileBoolTable.TempObject, true); // mark as okay to delete
                m.IsStaffOwned = true;                          // this keeps other rules from deleting it
            }
            return m;
        }
        #endregion Temporary Objects
        #region Item Replacement
        public static void ReplaceItem(Item newItem, Item oldItem, bool copyProperties = true)
        {
            if (newItem == null || oldItem == null || newItem == oldItem)
                return;

            // --------------------------------------------------------------------
            // Context detection
            // --------------------------------------------------------------------
            bool inContainer = false;
            bool isLockedDown = false;
            bool isSecure = false;
            bool isHardStatic = false;
            bool isHeld = false;
            bool isOneHanded = false;

            Container? parentContainer = oldItem.Parent as Container;
            Mobile holder = oldItem.ParentMobile;
            BaseHouse? house = null;

            if (!oldItem.Movable)
            {
                // Non-movable items may be locked down, secure, or hard static.
                house = BaseHouse.FindHouseAt(oldItem);

                if (house != null && house.IsLockedDown(oldItem))
                    isLockedDown = true;
                else if (house != null && house.IsSecure(oldItem))
                    isSecure = true;
                else
                    isHardStatic = true;
            }

            if (parentContainer != null)
                inContainer = true;

            // Equipped (held) item: parent is a Mobile, but not a container.
            if (holder != null && oldItem.ParentContainer == null)
            {
                Item? thing = holder.FindItemOnLayer(Layer.OneHanded);
                if (thing == oldItem)
                {
                    isOneHanded = true;
                    isHeld = true;
                }

                if (!isHeld)
                {
                    thing = holder.FindItemOnLayer(Layer.TwoHanded);
                    if (thing == oldItem)
                        isHeld = true;
                }
            }

            // --------------------------------------------------------------------
            // Property copy + positional normalization
            // --------------------------------------------------------------------
            if (copyProperties)
            {
                // Copy arbitrary shared properties (hue, name, weight, etc.).
                Utility.CopyProperties(oldItem, newItem);
            }

            // Ensure Map/Location are correct regardless of what CopyProperties does.
            // For container / equipped cases, the subsequent operations will override
            // this as appropriate, which is fine.
            newItem.Map = oldItem.Map;
            newItem.Location = oldItem.Location;

            // --------------------------------------------------------------------
            // Context-specific replacement
            // NOTE: This routine does NOT delete oldItem. The caller is responsible
            // for retiring or deleting oldItem once replacement is complete.
            // --------------------------------------------------------------------

            if (inContainer && parentContainer != null)
            {
                // In a container:
                //  - Clear parent to avoid "already contained" issues.
                //  - Drop new item into the same container.
                //  - Remove old item from that container.
                newItem.Parent = null;
                parentContainer.DropItem(newItem);
                parentContainer.RemoveItem(oldItem);
            }
            else if (isHeld && holder != null)
            {
                // Held in hand:
                //  - Move the old item to the backpack.
                //  - Equip the new item in its place.
                Item? held;

                if (isOneHanded)
                {
                    held = holder.FindItemOnLayer(Layer.OneHanded);
                }
                else
                {
                    held = holder.FindItemOnLayer(Layer.TwoHanded);
                }

                if (held == oldItem)
                    holder.AddToBackpack(held);

                holder.EquipItem(newItem);
            }
            else if (isLockedDown || isSecure)
            {
                // Locked down or secure item in a house:
                //  - Place the new item in the world at the same spot.
                //  - Re-apply lockdown to the new item.
                //  - Clear lockdown from the old item.
                if (house == null)
                    house = BaseHouse.FindHouseAt(oldItem);

                if (house != null)
                {
                    // Ensure it is in the world before lockdown.
                    newItem.Movable = true;
                    newItem.MoveToWorld(oldItem.Location, oldItem.Map);

                    // The shard convention: secure items effectively behave like
                    // locked-down items at the item level.
                    house.SetLockdown(oldItem, false);
                    house.SetLockdown(newItem, true);
                }
                else
                {
                    // Fallback: treat as a hard static in-world item.
                    newItem.MoveToWorld(oldItem.Location, oldItem.Map);
                    newItem.Movable = false;
                }
            }
            else if (isHardStatic)
            {
                // Non-movable, non-house static item:
                //  - Place the new item at the same world location.
                //  - Keep it non-movable.
                newItem.MoveToWorld(oldItem.Location, oldItem.Map);
                newItem.Movable = false;
            }
            else
            {
                // Default: world item (not in container, not held, movable).
                newItem.MoveToWorld(oldItem.Location, oldItem.Map);
            }

            // Caller decides what to do with oldItem (Delete, move elsewhere, etc.).
        }
        public static int ReplaceLockdowns(ReplaceEntry[] table)
        {
            Dictionary<Item, ReplaceEntry> replaceDict = new Dictionary<Item, ReplaceEntry>();
            foreach (Item item in Server.World.Items.Values)
            {
                foreach (ReplaceEntry re in table)
                {
                    if (item.GetType() == re.SourceType && (re.SourceItemID == -1 || item.ItemID == re.SourceItemID))
                    {
                        replaceDict[item] = re;
                        break;
                    }
                }
            }
            int count = 0;
            foreach (KeyValuePair<Item, ReplaceEntry> kvp in replaceDict)
            {
                Item oldItem = kvp.Key;
                ReplaceEntry re = kvp.Value;
                Item? new_item = null;
                try
                {
                    new_item = (Item)Activator.CreateInstance(re.TargetType, re.TargetArgs);
                }
                catch
                {
                }
                if (new_item != null)
                {
                    new_item.Amount = oldItem.Amount;
                    new_item.Hue = oldItem.Hue;
                    new_item.Movable = oldItem.Movable;
                    ReplaceLockdown(oldItem, new_item);
                    count++;
                }
            }
            return count;
        }
        public class ReplaceEntry
        {
            private Type m_SourceType;
            private int m_SourceItemID;
            private Type m_TargetType;
            private object[]? m_TargetArgs;
            public Type SourceType { get { return m_SourceType; } }
            public int SourceItemID { get { return m_SourceItemID; } }
            public Type TargetType { get { return m_TargetType; } }
            public object[]? TargetArgs { get { return m_TargetArgs; } }
            public ReplaceEntry(Type sourceType, int sourceItemID, Type targetType, object[]? targetArgs)
            {
                m_SourceType = sourceType;
                m_SourceItemID = sourceItemID;
                m_TargetType = targetType;
                m_TargetArgs = targetArgs;
            }
        }
        private static void ReplaceLockdown(Item oldItem, Item new_item)
        {
            BaseHouse? house = BaseHouse.FindHouseAt(oldItem);
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(oldItem.GetWorldLocation(), oldItem.Map);

            bool isHouseLockdown = (house != null && house.IsLockedDown(oldItem));
            bool isTownshipLockdown = (tsr != null && tsr.TStone != null && tsr.TStone.IsLockedDown(oldItem));
            bool isHouseAddon = (house != null && house.Addons.Contains(oldItem));

            Mobile? townshipOwner = null;

            if (isTownshipLockdown)
            {
                LockDownContext? context;

                if (tsr.TStone.LockdownRegistry.TryGetValue(oldItem, out context))
                    townshipOwner = context.Mobile;
            }

            if (isHouseLockdown)
                house.SetLockdown(oldItem, false);

            if (isTownshipLockdown)
            {
                tsr.TStone.LockdownRegistry.Remove(oldItem);
                oldItem.Movable = true;
            }

            oldItem.ReplaceWith(new_item);

            if (isHouseLockdown)
                house.SetLockdown(new_item, true);

            if (isTownshipLockdown)
            {
                tsr.TStone.LockdownRegistry[new_item] = new LockDownContext(townshipOwner);
                new_item.Movable = false;
            }

            if (isHouseAddon)
                house.Addons.Add(new_item);
        }
        #endregion Item Replacement
        #region Dupe
        public static Mobile? Dupe(Mobile src_mobile)
        {
            Mobile old_mobile = src_mobile;
            Type t = old_mobile.GetType();
            Mobile? new_mobile = null;
            try
            {
                if (HasParameterlessConstructor(old_mobile.GetType()))
                {
                    new_mobile = old_mobile.Dupe();
                }
                else
                {
                    return new_mobile;
                }
            }
            finally
            {
                ;
            }
            return new_mobile;
        }
        public static Item? Dupe(object? o)
        {
            if (o is not Item)
                return null;

            Item old_item = (Item)o;
            Type t = old_item.GetType();
            Item? new_item = null;
            try
            {
                if (HasParameterlessConstructor(old_item.GetType()))
                {   // InformedDupe: The object has a dupe() override - use it
                    if (ItemOverrides(old_item, method_name: "Dupe", param_types: new Type[] { typeof(int) }))
                    {
                        if ((new_item = InformedDupe(old_item)) != null)
                            return new_item;
                    }
                    // RawDupe: The object does not have a dupe() override - use default
                    if ((new_item = RawDupe(old_item)) != null)
                        return new_item;
                }
                else
                {
                    // if this type overrides Dupe(), use it!
                    if (ItemOverrides(old_item, method_name: "Dupe", param_types: new Type[] { typeof(int) }))
                        return (new_item = old_item.Dupe(1));

                    new_item = ParamDupe(old_item.GetType(), old_item);
                    System.Diagnostics.Debug.Assert(new_item != null);
                    return new_item;
                }
            }
            finally
            {
                if (new_item != null)
                {
                    if (new_item is Container cont)
                        cont.UpdateAllTotals();
                    else
                        new_item.UpdateTotals();
                }
            }
            return null;
        }
        public static bool ItemOverrides(Item item, string method_name, Type[] param_types)
        {
            return (item.GetType().GetMethod(method_name, BindingFlags.Public | BindingFlags.Instance,
                    null,
                    CallingConventions.Any,
                    param_types,
                    null).DeclaringType == item.GetType());
        }
        private static Item? InformedDupe(Item old_item)
        {
            if (old_item == null) return null;
            try
            {
                Item new_item = old_item.Dupe(1);           // call the native duping mechanic
                if (new_item != null)
                {                                           // no native dupe, revert to Raw dupe
                    CopyProperties(old_item, new_item);     // now get the properties
                    return new_item;
                }
                else
                    return null;
            }
            catch
            {
                Utility.Monitor.WriteLine(string.Format("Logic Error: {0}. ", Utility.FileInfo()), ConsoleColor.Red);
                return null;
            }
        }
        private static Item? RawDupe(Item old_item)
        {
            if (old_item == null) return null;
            Type t = old_item.GetType();
            ConstructorInfo[] info = t.GetConstructors();
            foreach (ConstructorInfo c in info)
            {
                ParameterInfo[] paramInfo = c.GetParameters();

                if (paramInfo.Length == 0)
                {
                    object[] objParams = new object[0];

                    try
                    {
                        object o = c.Invoke(objParams);

                        if (o != null && o is Item)
                        {
                            Item new_item = (Item)o;
                            CopyProperties(old_item, new_item);  // now get the properties
                            return new_item;
                        }
                    }
                    catch
                    {
                        Utility.Monitor.WriteLine(string.Format("Logic Error: {0}. ", Utility.FileInfo()), ConsoleColor.Red);
                        return null;
                    }
                }
            }

            return null;
        }

        public static ArrayList Dupe(ArrayList src_items)
        {
            ArrayList theItems = new();
            if (src_items == null)
                return theItems;

            if (src_items.Count == 0)
                return theItems;

            foreach (object o in src_items)
                if (o is Item)
                    theItems.Add(Dupe(o));

            return theItems;
        }
        public static Item DeepDupe(Item? old_item, bool raw = false, List<KeyValuePair<Item, Item>>? dupeMap = null)
        {
            if (old_item == null) return null;
            Item? new_item = null;

            if (HasParameterlessConstructor(old_item.GetType()))
            {
                // we use RawDupe here because sometimes items have their own dupe. When they do this,
                // they may 'dupe' internally referenced items. Maybe you want this, maybe not.
                if (raw)
                    new_item = RawDupe(old_item);
                else
                    new_item = Dupe(old_item);

                System.Diagnostics.Debug.Assert(new_item != null);

                if (new_item != null)
                {
                    if (dupeMap != null)
                        dupeMap.Add(new KeyValuePair<Item, Item>(old_item, new_item));

                    // not supported checker
                    List<Type> types = new();

                    // dupes and copies the items from the old item to the new item
                    if (old_item.PeekItems != null && old_item.PeekItems.Count > 0)
                        foreach (Item old_sub_item in old_item.PeekItems)
                        {
                            new_item.AddItem(DeepDupe(old_sub_item, raw, dupeMap));
                            // items that have a dupe override will already have duped their items
                            //if (old_item.NeedsSubItemsDuped())
                            //    new_item.AddItem(DeepDupe(old_sub_item, raw, dupeMap));
                            //else if (old_item is not Container)
                            //{
                            //    // music motion controller for instance (stashes sheet music in its item table)
                            //    foreach (Item new_sub_item in new_item.PeekItems)
                            //        if (new_sub_item.GetType() == old_sub_item.GetType())
                            //        {
                            //            types.Add(new_sub_item.GetType());
                            //            dupeMap.Add(new KeyValuePair<Item, Item>(old_sub_item, new_sub_item));
                            //        }
                            //}
                            //else
                            //{
                            //    //  Treasure chests, fillable containers, etc. generate their own loot, so don't dupe contents
                            //}
                        }
                    // we don't currently support items that may be stashing more than one of the same type of object in it's item table.
                    System.Diagnostics.Debug.Assert(types.Count < 2);
                }
            }
            else
            {

                // if this type overrides Dupe(), use it!
                if (ItemOverrides(old_item, method_name: "Dupe", param_types: new Type[] { typeof(int) }))
                {
                    new_item = old_item.Dupe(1);
                    if (new_item != null)
                    {
                        if (dupeMap != null)
                            dupeMap.Add(new KeyValuePair<Item, Item>(old_item, new_item));
                    }
                    return new_item;
                }

                // ParamDupe() handles a bunch of common items (like Statics) that don't have a zero parm constructor.
                //  We do our best to construct one of these objects from what is known about the source item
                new_item = ParamDupe(old_item.GetType(), old_item);
                if (new_item != null)
                {
                    if (dupeMap != null)
                        dupeMap.Add(new KeyValuePair<Item, Item>(old_item, new_item));

                    if (new_item is not Container || new_item is Container cont && cont.AutoFills == false)
                    {   // dupes and copies the items from the old item to the new item
                        if (old_item.PeekItems != null && old_item.PeekItems.Count > 0)
                            foreach (Item old_sub_item in old_item.PeekItems)
                                new_item.AddItem(DeepDupe(old_sub_item, raw, dupeMap));
                    }
                }
                System.Diagnostics.Debug.Assert(new_item != null);
            }
            return new_item;
        }
        public static List<Item> GetInternalItems(Item item, bool null_map = false)
        {
            List<Item> elements = new();

            // we will ignore Item properties and only focus in the derived class properties.
            Item? linked_item = null;
            PropertyInfo[] props = Utility.ItemRWPropertyProperties(item, IsAssignableTo: typeof(Item));
            for (int i = 0; i < props.Length; i++)
                if ((linked_item = (Item)props[i].GetValue(item, null)) != null)
                    if ((!null_map && linked_item.Map == Map.Internal) || (null_map && linked_item.Map == null))
                        elements.Add(linked_item);

            return elements;
        }
        public static bool HasInternalItems(Item item)
        {
            List<Item> list = GetInternalItems(item);
            return list.Count > 0;
        }
        public static Item? GetFirstInternalItem(Item item)
        {
            List<Item> list = GetInternalItems(item);
            if (list.Count > 0)
                return GetInternalItems(item)[0];
            return null;
        }
        public static Item? CopyItem(Item? item)
        {
            if (item == null) return null;
            Item new_item = DeepDupe(item, raw: true);
            List<string> prop_list_names = ItemRWPropertyNames(item, IsAssignableTo: typeof(Item));
            foreach (string prop_list_name in prop_list_names)
                SetItemProp(new_item, prop_list_name, CopyItem(GetItemProp(item, prop_list_name) as Item));

            return new_item;
        }
        public static Item? ParamDupe(Type t, Item old_item)
        {
            Item new_item = ParamDupeInternal(t, old_item);
            if (new_item != null)
                CopyProperties(old_item, new_item);             // now get the properties
            return new_item;
        }
        private static Item ParamDupeInternal(Type t, Item old_item)
        {

            if (t == typeof(Static))
            {
                return new Static(old_item.ItemID);
            }
            else if (t == typeof(XmlAddon))
            {
                return (old_item as XmlAddon).Dupe(1);
            }
            else if (t == typeof(RaisableItem))
            {
                return (old_item as RaisableItem).Dupe(1);
            }
            else if (t == typeof(EffectItem))
            {
                return new EffectItem(old_item.ItemID);
            }
            else if (t == typeof(AddonComponent))
            {
                return new Static(old_item.ItemID);
            }
            else if (t == typeof(LocalizedStatic))
            {
                return new LocalizedStatic(old_item.ItemID);
            }
            else if (t == typeof(LocalizedSign))
            {
                return new LocalizedSign(old_item.ItemID, (old_item as LocalizedSign).LabelNumber);
            }
            else if (t == typeof(HintItem))
            {
                return new HintItem((old_item as HintItem).ItemID, (old_item as HintItem).Range, (old_item as HintItem).WarningNumber, (old_item as HintItem).HintNumber);
            }
            else if (t == typeof(WarningItem))
            {
                return new WarningItem((old_item as WarningItem).ItemID, (old_item as WarningItem).Range, (old_item as WarningItem).WarningNumber);
            }
            else if (t == typeof(RattanDoor))
            {
                return new RattanDoor((old_item as RattanDoor).Facing);
            }
            else if (t == typeof(LightWoodDoor))
            {
                return new LightWoodDoor((old_item as LightWoodDoor).Facing);
            }
            else if (t == typeof(DarkWoodDoor))
            {
                return new DarkWoodDoor((old_item as DarkWoodDoor).Facing);
            }
            else if (t == typeof(LightWoodGate))
            {
                return new LightWoodGate((old_item as LightWoodGate).Facing);
            }
            else if (t == typeof(StrongWoodDoor))
            {
                return new StrongWoodDoor((old_item as StrongWoodDoor).Facing);
            }
            else if (t == typeof(MetalDoor))
            {
                return new MetalDoor((old_item as MetalDoor).Facing);
            }
            else if (t == typeof(MetalDoor2))
            {
                return new MetalDoor2((old_item as MetalDoor2).Facing);
            }
            else if (t == typeof(BarredMetalDoor))
            {
                return new BarredMetalDoor((old_item as BarredMetalDoor).Facing);
            }
            else if (t == typeof(BarredMetalDoor2))
            {
                return new BarredMetalDoor2((old_item as BarredMetalDoor2).Facing);
            }
            else if (t == typeof(DarkWoodGate))
            {
                return new DarkWoodGate((old_item as DarkWoodGate).Facing);
            }
            else if (t == typeof(IronGateShort))
            {
                return new IronGateShort((old_item as IronGateShort).Facing);
            }
            else if (t == typeof(IronGate))
            {
                return new IronGate((old_item as IronGate).Facing);
            }
            else if (t == typeof(SecretWoodenDoor))
            {
                return new SecretWoodenDoor((old_item as SecretWoodenDoor).Facing);
            }
            else if (t == typeof(SecretDungeonDoor))
            {
                return new SecretDungeonDoor((old_item as SecretDungeonDoor).Facing);
            }
            else if (t == typeof(SecretStoneDoor1))
            {
                return new SecretStoneDoor1((old_item as SecretStoneDoor1).Facing);
            }
            else if (t == typeof(SecretStoneDoor3))
            {
                return new SecretStoneDoor3((old_item as SecretStoneDoor3).Facing);
            }
            else if (t == typeof(MediumWoodDoor))
            {
                return new MediumWoodDoor((old_item as MediumWoodDoor).Facing);
            }
            else if (t == typeof(DarkWoodHouseDoor))
            {
                return new DarkWoodHouseDoor((old_item as DarkWoodHouseDoor).Facing);
            }
            else if (t == typeof(BeverageBottle))
            {
                return new BeverageBottle((old_item as BeverageBottle).Content);
            }
            else if (t == typeof(Jug))
            {
                return new Jug((old_item as Jug).Content);
            }
            else if (t == typeof(ShipwreckedItem))
            {
                return new ShipwreckedItem(old_item.ItemID);
            }
            else if (t == typeof(DecorativeBow))
            {
                /*  bow type to item ID lookup
                    if (type == 0) ItemID = 5468;
                    if (type == 1) ItemID = 5469;
                    if (type == 2) ItemID = 5470;
                    if (type == 3) ItemID = 5471;
                 */
                Dictionary<int, int> Lookup = new()
                {
                    {5468,0 },
                    {5469,1 },
                    {5470,2 },
                    {5471,3 }
                };

                return new DecorativeBow(Lookup[old_item.ItemID]);
            }
            else if (t == typeof(TreasureMap))
            {
                return new TreasureMap((old_item as TreasureMap).Level);
            }
            else if (t == typeof(TreasureMapChest))
            {
                return new TreasureMapChest((old_item as TreasureMapChest).Level, (old_item as TreasureMapChest).ThemeType);
            }
            else if (t == typeof(DungeonTreasureChest))
            {
                return new DungeonTreasureChest((old_item as DungeonTreasureChest).Level);
            }
            else if (t == typeof(ChampionSkull))
            {
                return new ChampionSkull((old_item as ChampionSkull).Type);
            }
            else if (t == typeof(LargeDockedDragonBoat))
            {
                return new LargeDockedDragonBoat((old_item as LargeDockedDragonBoat).Boat);
            }
            else if (t == typeof(SmallDockedBoat))
            {
                return new SmallDockedBoat((old_item as SmallDockedBoat).Boat);
            }
            if (t == typeof(Server.Engines.Quests.Haven.Cannon))
            {
                return new Server.Engines.Quests.Haven.Cannon((old_item as Server.Engines.Quests.Haven.Cannon).CannonDirection);
            }
            else
            {   // need to handle
                LogHelper logger = new LogHelper("Dupe error.log", overwrite: false, sline: true, quiet: true);
                logger.Log(string.Format("Unable to dupe {0}", old_item));
                logger.Finish();
            }

            throw new ApplicationException(string.Format("Unable to create {0}/{1}", t, old_item));
        }
        #endregion Dupe
        #region Dynamic Mobile Creation
        public static string CompileMobileProperties(Mobile src_mobile)
        {
            string defintion = string.Empty;
            PropertyInfo[] props = MobileRWProperties(src_mobile);
            try
            {
                foreach (var prop in props)
                {
                    defintion += prop.Name;
                    defintion += " ";
                    object? temp = prop.GetValue(src_mobile, null);
                    if (temp == null || string.IsNullOrEmpty(temp.ToString()))
                        defintion += Commands.Properties.PropNull;
                    else
                        defintion += temp.ToString();
                    defintion += ";";
                }
            }
            catch (Exception ex)
            {
                ;
            }
            return defintion;
        }
        public static void CopyMobileProperties(Mobile dest, string src)
        {
            if (string.IsNullOrEmpty(src)) return;
            Dictionary<string, string>? keyValuePairs = ParseString(src);
            if (keyValuePairs == null) return;
            try
            {
                List<string> bad_props = new();
                foreach (var kvp in keyValuePairs)
                {
                    string result = Server.Commands.Properties.SetValue(dest, kvp.Key, kvp.Value);
                    if (result == "Property has been set.")
                        continue;
                    else
                    {
                        bad_props.Add(kvp.Key);
                        continue;
                    }
                }
            }
            catch { }
        }
        public static string CompileMobileSkills(Mobile src_mobile)
        {
            string defintion = string.Empty;

            Server.Skills this_skills = src_mobile.Skills;        // skills
            for (int i = 0; i < this_skills.Length; ++i)
            {
                defintion += this_skills[i].SkillName;
                defintion += " ";
                defintion += this_skills[i].Base.ToString();
                defintion += ";";
            }

            return defintion;
        }
        public static void CopyMobileSkills(Mobile dest_mobile, string src)
        {
            if (string.IsNullOrEmpty(src)) return;
            Dictionary<string, string>? keyValuePairs = ParseString(src);
            if (keyValuePairs == null) return;
            try
            {
                Server.Skills this_skills = dest_mobile.Skills;
                int index = 0;
                foreach (var kvp in keyValuePairs)
                    this_skills[index++].Base = double.Parse(kvp.Value);
            }
            catch { }
        }
#nullable enable
        public static Dictionary<string, string>? ParseString(string src)
        {
            if (string.IsNullOrEmpty(src)) return null;
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            try
            {
                string[] tokens = src.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string token in tokens)
                {
                    var spaceIndex = token.IndexOf(" ");
                    var keyword = token.Substring(0, spaceIndex);
                    var value = token.Substring(spaceIndex + 1);
                    keyValuePairs.Add(keyword, value);
                }
                return keyValuePairs;
            }
            catch { }
            return null;
        }
#nullable disable
        #endregion Dynamic Mobile Creation
        #region Property Copying
        private static List<string> ItemPropertyNames = new();
        public static object GetItemProp(Item item, string prop_name, bool ignore_base_item_props = true)
        {
            if (item == null) return null;
            // we will ignore Item properties and only focus in the derived class properties.
            if (ignore_base_item_props == true && ItemPropertyNames.Count == 0)
            {
                PropertyInfo[] base_props = typeof(Item).GetProperties();
                for (int i = 0; i < base_props.Length; i++)
                    if (base_props[i].CanRead)
                        ItemPropertyNames.Add(base_props[i].Name);
            }

            Item internal_item = null;
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead)
                    if (props[i].Name.Equals(prop_name, StringComparison.OrdinalIgnoreCase))
                        if (ignore_base_item_props)
                        {
                            if (!ItemPropertyNames.Contains(props[i].Name))                 // not a base item property
                                return props[i].GetValue(item, null);
                        }
                        else
                            return props[i].GetValue(item, null);


            return null;
        }
        public static void SetItemProp(Item item, string prop_name, object value)
        {
            Item internal_item = null;
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (props[i].Name.Equals(prop_name, StringComparison.OrdinalIgnoreCase))
                    {
                        props[i].SetValue(item, value, null);
                        break;
                    }
            return;
        }
        private static bool PropertyIntersection(IEntity dest, IEntity src, out SortedList<string, PropertyInfo> dest_accepts, out SortedList<string, PropertyInfo> src_provides)
        {
            dest_accepts = new();
            foreach (PropertyInfo pi in dest.GetType().GetProperties())
                dest_accepts.Add(pi.Name, pi);

            src_provides = new();
            foreach (PropertyInfo pi in src.GetType().GetProperties())
            {
                CopyableAttribute ca = GetCA(src.GetType(), pi.Name);
                if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                    continue;
                src_provides.Add(pi.Name, pi);
            }

            //var src_props = src.GetType().GetProperties();
            //var dest_props = dest.GetType().GetProperties();
            //var srcNames = src_props.Select(p => p.Name).ToHashSet();
            //dest_props_list = dest_props.Where(p => srcNames.Contains(p.Name)).ToList();
            //src_props_list = src_props.Where(p => srcNames.Contains(p.Name)).ToList();
            return dest_accepts.Count > 0 && dest_accepts.Count > 0;
        }
        public static List<string> BaseRWItemPropertyNames(Type IsAssignableTo = null)
        {
            List<string> ItemPropertyNames = new();
            PropertyInfo[] props = typeof(Item).GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (IsAssignableTo != null && !props[i].PropertyType.IsAssignableTo(IsAssignableTo))     // holds one of these
                        continue;
                    else
                        ItemPropertyNames.Add(props[i].Name);

            return ItemPropertyNames;
        }
        public static PropertyInfo[] ItemRWPropertyProperties(Item item, Type IsAssignableTo = null)
        {
            List<PropertyInfo> ItemProperties = new();
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (IsAssignableTo != null && !props[i].PropertyType.IsAssignableTo(IsAssignableTo))     // holds one of these
                        continue;
                    else
                        ItemProperties.Add(props[i]);

            return ItemProperties.ToArray();
        }
        public static List<string> ItemRWPropertyNames(Item item, Type IsAssignableTo = null)
        {
            List<string> ItemProperties = new();
            //List<string> baseRWItemPropertyNames = BaseRWItemPropertyNames(IsAssignableTo);
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (IsAssignableTo != null && !props[i].PropertyType.IsAssignableTo(IsAssignableTo))     // holds one of these
                        continue;
                    else //if (!baseRWItemPropertyNames.Contains(props[i].Name))
                        ItemProperties.Add(props[i].Name);

            return ItemProperties;
        }
        public static PropertyInfo[] ItemRWProperties(Item item)
        {
            List<PropertyInfo> ItemProperties = new();
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    ItemProperties.Add(props[i]);

            return ItemProperties.ToArray();
        }
        public static PropertyInfo[] RegionRWProperties(Region region)
        {
            List<PropertyInfo> RegionProperties = new();
            PropertyInfo[] props = region.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    RegionProperties.Add(props[i]);

            return RegionProperties.ToArray();
        }
        public static PropertyInfo[] MobileRWProperties(Mobile mobile)
        {
            List<PropertyInfo> MobileProperties = new();
            PropertyInfo[] props = mobile.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                {
                    // some properties have side effects, they should be marked as Do Not Copy
                    //CopyableAttribute ca = GetCA(props[i]);
                    CopyableAttribute ca = GetCA(mobile.GetType(), props[i].Name);
                    if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                        continue;
                    else
                        MobileProperties.Add(props[i]);
                }

            return MobileProperties.ToArray();
        }
        public static PropertyInfo FindMobileRWProperty(Mobile mobile, string name)
        {
            PropertyInfo[] props = mobile.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (props[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return props[i];
            return null;
        }
        public static void CopySkills(Mobile dest, Mobile src, bool include_stats = true)
        {
            Server.Skills src_skills = src.Skills;
            Server.Skills dest_skills = dest.Skills;
            for (int i = 0; i < src_skills.Length; ++i)
                dest_skills[i].Base = src_skills[i].Base;

            if (include_stats)
            {
                dest.RawDex = src.RawDex;
                dest.RawInt = src.RawInt;
                dest.RawStr = src.RawStr;

                dest.Stam = src.Stam;
                dest.Mana = src.Mana;
                dest.Hits = src.Hits;
            }

            dest.Karma = src.Karma;
            dest.Fame = src.Fame;
        }
        public static void SwapBackpack(Mobile dest, Mobile src, bool include_stats = true)
        {
            if (src.Backpack == null) return;
            if (dest.Backpack != null)
                dest.Backpack.Delete();

            Item new_backpack = DeepDupe(src.Backpack);
            dest.AddItem(new_backpack);
        }
        public static bool CanCopy(PropertyInfo pi)
        {
            CopyableAttribute ca = GetCA(pi.PropertyType, pi.Name);
            if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                return false;
            return true;
        }
        public static bool CanCopy(Type t, string name)
        {
            CopyableAttribute ca = GetCA(t, name);
            if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                return false;
            return true;
        }
        public static void CopyPropertyIntersection(IEntity dest, IEntity src)
        {
            SortedList<string, PropertyInfo> dest_accepts;
            SortedList<string, PropertyInfo> src_provides;
            bool okay = PropertyIntersection(dest, src, out dest_accepts, out src_provides);

            if (okay)
                try
                {
                    foreach (var dest_prop in dest_accepts)                                                         // for each dest property
                        if (src_provides.ContainsKey(dest_prop.Key))                                                // does source have that property?
                            if (dest_prop.Value.CanRead && dest_prop.Value.CanWrite)                                // can read/write dest?
                                if (src_provides[dest_prop.Key].CanRead && src_provides[dest_prop.Key].CanWrite)    // can read/write src?
                                                                                                                    //if (CanCopy(dest_prop.Value) && CanCopy(src_provides[dest_prop.Key]))       // marked as DoNotCopy
                                                                                                                    //if (CanCopy(dest_prop.Value) && CanCopy(src_provides[dest_prop.Key]))
                                    dest_prop.Value.SetValue(dest, src_provides[dest_prop.Key].GetValue(src, null), null); // do it baby!
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
        }
        public static void CopyProperties(Region dest, Region src)
        {
            Dictionary<string, object> PropertyOverwrite = new();

            #region CopyProperties
            {
                PropertyInfo[] props = RegionRWProperties(src);
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        // some properties have side effects, they should be marked as Do Not Copy
                        //CopyableAttribute ca = GetCA(props[i]);
                        CopyableAttribute ca = GetCA(src.GetType(), props[i].Name);

                        if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                            continue;

                        // copy the property, but a;so track if it gets changed by a subsequent Set
                        object o = props[i].GetValue(src, null);
                        props[i].SetValue(dest, o, null);
                        PropertyOverwrite.Add(props[i].Name, o);
                    }
                    catch
                    {
                        //Console.WriteLine( "Denied" );
                    }
                }
            }
            #endregion  CopyProperties

            #region Verify
            if (!Core.Debug)
                goto done;
            {
                PropertyInfo[] props = dest.GetType().GetProperties();
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        if (props[i].CanRead && props[i].CanWrite)
                        {
                            object o = props[i].GetValue(dest, null);
                            if (PropertyOverwrite.ContainsKey(props[i].Name))
                                if (!PropsToIgnore.Contains(props[i].Name))
                                    if (!Same(PropertyOverwrite[props[i].Name], o))
                                    {
                                        //Utility.ConsoleOut(string.Format("Copying properties of {0} is unreliable.", src), ConsoleColor.DarkRed);
                                        //Utility.ConsoleOut(string.Format("Offending property {0}.", props[i].Name), ConsoleColor.DarkRed);
                                        //Utility.ConsoleOut(string.Format("You will need a special case handler in CopyProperties.", src), ConsoleColor.DarkRed);
                                        ;// debug
                                    }
                                    else
                                        ;// debug
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }
        #endregion Verify

        done:
            return;
        }
        public static void CopyProperties(Mobile dest, Mobile src)
        {
            Dictionary<string, object> PropertyOverwrite = new();

            #region CopyProperties
            {
                PropertyInfo[] props = MobileRWProperties(src);
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        // some properties have side effects, they should be marked as Do Not Copy
                        //CopyableAttribute ca = GetCA(props[i]);
                        CopyableAttribute ca = GetCA(src.GetType(), props[i].Name);
                        if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                            continue;

                        // copy the property, but a;so track if it gets changed by a subsequent Set
                        object o = props[i].GetValue(src, null);
                        props[i].SetValue(dest, o, null);
                        PropertyOverwrite.Add(props[i].Name, o);
                    }
                    catch
                    {
                        //Console.WriteLine( "Denied" );
                    }
                }
            }
            #endregion  CopyProperties

            #region Verify
            {
                PropertyInfo[] props = dest.GetType().GetProperties();
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        if (props[i].CanRead && props[i].CanWrite)
                        {
                            object o = props[i].GetValue(dest, null);
                            if (PropertyOverwrite.ContainsKey(props[i].Name))
                                if (!PropsToIgnore.Contains(props[i].Name))
                                    if (!Same(PropertyOverwrite[props[i].Name], o))
                                    {
                                        Utility.Monitor.WriteLine(string.Format("Copying properties of {0} is unreliable.", src), ConsoleColor.DarkRed);
                                        Utility.Monitor.WriteLine(string.Format("Offending property {0}.", props[i].Name), ConsoleColor.DarkRed);
                                        Utility.Monitor.WriteLine(string.Format("You will need a special case handler in CopyProperties.", src), ConsoleColor.DarkRed);
                                        ;// debug
                                    }
                                    else
                                        ;// debug
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }
            #endregion Verify
        }
        // these properties are expected to be changing without side effect
        private static List<string> PropsToIgnore = new()
        { 
            // Items
            "LastMoved",    // set when you create a new object, so it will always be different
            "BoolTable",    // underlies Movable, Visible, etc.. 
            // Mobiles
            "Lifespan",
            "NextGrowth",
            "NextMating",
            "Virtues",
        };
        public static void CopyProperties(Item src, Item dest)
        {
            if (src == null || dest == null || src == dest)
                return;

            /* 2/8/2024, Adam
             * CAUTION: CopyProperties
             * CopyProperties does not work for all items.
             *  For example: effect controller (probably others.)
             *  This is because Setting property value X, then setting property value Y may unset or change X.
             *  EffectController for instance: All of SourceItem, SourceMobile, and SourceNull operate on 
             *  IEntity m_Source. If SourceItem is set to some item, and CopyProperties correctly copies it to the 
             *  destination item, all is well. But when it tries to copy SourceMobile, SourceMobile returns null, since
             *  m_Source is an Item and not a Mobile.
             *  When SourceMobile in the destination object gets set to null, so does SourceItem! funky.
             * The correct way to handle this us with [CopyableAttribute(CopyType.DoNotCopy)] property
             *  This prevents CopyProperties() from copying that property
             */
            Dictionary<string, object> PropertyOverwrite = new();

            #region CopyProperties
            {
                PropertyInfo[] props = ItemRWProperties(src);
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        // some properties have side effects, they should be marked as Do Not Copy
                        CopyableAttribute ca = GetCA(src.GetType(), props[i].Name);
                        if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                            continue;

                        // copy the property, but also track if it gets changed by a subsequent Set
                        object o = props[i].GetValue(src, null);
                        props[i].SetValue(dest, o, null);
                        PropertyOverwrite.Add(props[i].Name, o);
                    }
                    catch
                    {
                        //Console.WriteLine( "Denied" );
                    }
                }
            }
            #endregion  CopyProperties

            // we now handle BaseDoor explicitly in DupeUtility.Dupe()
            //#region  CopyProperties Override
            ///* some items like BaseDoor do not dupe properly. E.g., OpenedID is calculated, so copying that 'calculated' value to the new door's
            // * OpenedID gets you a wrong OpenedID when it is recalculated on the destination door. (ClosedID has the same problem.)
            // * BaseDoor's CopyProperties corrects this odd behavior.
            // * Unfortunately, calling the item's CopyProperties() will recopy the Item's properties, but this is only a handful of simple objects.
            // */
            //src.CopyProperties(dest);
            //#endregion  CopyProperties Override

#if DEBUG
            #region Verify
            {
                PropertyInfo[] props = dest.GetType().GetProperties();
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        if (props[i].CanRead && props[i].CanWrite)
                        {
                            object o = props[i].GetValue(dest, null);
                            if (PropertyOverwrite.ContainsKey(props[i].Name))
                                if (!PropsToIgnore.Contains(props[i].Name))
                                    if (!Same(PropertyOverwrite[props[i].Name], o))
                                    {
                                        //Utility.ConsoleOut(string.Format("Copying properties of {0} is unreliable.", src), ConsoleColor.DarkRed);
                                        //Utility.ConsoleOut(string.Format("Offending property {0}.", props[i].Name), ConsoleColor.DarkRed);
                                        //Utility.ConsoleOut(string.Format("You will need a special case handler in CopyProperties.", src), ConsoleColor.DarkRed);
                                        //break;
                                        ;
                                    }
                                    else
                                        ;// debug
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }
            #endregion Verify
#endif
        }
        //private static CopyableAttribute GetCA(PropertyInfo prop)
        //{
        //    object[] attrs = prop.GetCustomAttributes(typeof(CopyableAttribute), inherit: false);

        //    if (attrs.Length > 0)
        //        return attrs[0] as CopyableAttribute;
        //    else
        //        return null;
        //}
        public static CopyableAttribute GetCA(Type t, string name)
        {   // Be advised: GetCustomAttributes do NOT carry forward to derived classes if the Property is virtual.
            //  The Microsoft docs say this, but it's somewhat unintuitive.
            //  Make sure any properties you are marking as DoNotCopy are NOT virtual.
            object attr = t
              .GetProperty(name)
              .GetCustomAttributes(typeof(CopyableAttribute), false)
              .FirstOrDefault();

            return attr as CopyableAttribute;
        }
        public static bool PatchProperty(Item dest, string property_name, object new_value)
        {
            bool patched = false;
            PropertyInfo[] props = dest.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    if (props[i].CanRead && props[i].CanWrite && props[i].Name == property_name)
                    {
                        props[i].SetValue(dest, new_value, null);
                        patched = true;
                        break;
                    }
                }
                catch
                {
                }
            }
            return patched;
        }
        public static bool Same(object o1, object o2)
        {
            if (o1 == null && o2 != null) return false;
            if (o1 != null && o2 == null) return false;
            if (o1 == null && o2 == null) return true;
            return o1.Equals(o2);
        }
        #endregion Property Copying
        #region Layers
        public static void WipeLayers(Mobile m)
        {
            try
            {
                foreach (Item item in Utility.GetLayerItems(m, includeHair: true))
                {
                    m.RemoveItem(item);
                    item.Delete();
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }
        }
        public static void WipeLayersHue(Mobile m, int newHue = 0)
        {
            try
            {
                foreach (Item item in Utility.GetLayerItems(m, includeHair: false))
                {
                    if (item is BaseClothing bc)
                        bc.Hue = newHue;
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }
        }
        [Flags]
        public enum CopyLayerFlags
        {
            Default,    // maintains source and destination symmetry
            Immovable,  // 
            Newbie,     //
            //Regular    // remove newbie flag
        }
        public static List<Item> GetLayerItems(Mobile src, bool includeHair = false)
        {
            List<Item> items = new List<Item>();

            if (src == null)
                return items;

            Layer[] layers = includeHair
                ? new Layer[]
                {
                    Layer.Shoes,
                    Layer.Pants,
                    Layer.Shirt,
                    Layer.Helm,
                    Layer.Gloves,
                    Layer.Neck,
                    Layer.Waist,
                    Layer.InnerTorso,
                    Layer.MiddleTorso,
                    Layer.Arms,
                    Layer.Cloak,
                    Layer.OuterTorso,
                    Layer.OuterLegs,
                    Layer.InnerLegs,
                    Layer.Bracelet,
                    Layer.Ring,
                    Layer.Earrings,
                    Layer.OneHanded,
                    Layer.TwoHanded,
                    Layer.Hair,
                    Layer.FacialHair
                }
                : new Layer[]
                {
                    Layer.Shoes,
                    Layer.Pants,
                    Layer.Shirt,
                    Layer.Helm,
                    Layer.Gloves,
                    Layer.Neck,
                    Layer.Waist,
                    Layer.InnerTorso,
                    Layer.MiddleTorso,
                    Layer.Arms,
                    Layer.Cloak,
                    Layer.OuterTorso,
                    Layer.OuterLegs,
                    Layer.InnerLegs,
                    Layer.Bracelet,
                    Layer.Ring,
                    Layer.Earrings,
                    Layer.OneHanded,
                    Layer.TwoHanded
                };

            for (int i = 0; i < layers.Length; i++)
            {
                Item item = src.FindItemOnLayer(layers[i]);

                if (item != null)
                    items.Add(item);
            }

            return items;
        }
        public static void CopyLayers(Mobile dest, Mobile src, CopyLayerFlags flags)
        {   // wipe layers of dest mobile, copy identical layers from src to dest
            try
            {
                Utility.WipeLayers(dest);

                foreach (Item item in Utility.GetLayerItems(src, includeHair: true))
                {
                    if (item == null)
                        continue;

                    Item new_item = null;

                    // some items do not have a parameterless constructor
                    // i.e., StuddedGlovesOfMining do not have such a constructor
                    // when we encounter such an item, just keep going
                    try
                    {
                        new_item = Dupe(item);
                    }
                    catch
                    {
                        /* keep going */
                    }

                    if (new_item == null)
                        continue;

                    // Default: first assume the properties of the source
                    new_item.Movable = item.Movable;
                    new_item.LootType = item.LootType;

                    // override
                    if (flags.HasFlag(CopyLayerFlags.Immovable))
                        new_item.Movable = false;
                    else
                        new_item.Movable = true;

                    if (flags.HasFlag(CopyLayerFlags.Newbie))
                        new_item.LootType = LootType.Newbied;
                    else
                        new_item.LootType = LootType.Regular;

                    // in all cases, remove the FamilyID. Otherwise the server will delete
                    new_item.FamilyID = 0;

                    // add it
                    dest.AddItem(new_item);
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }
        }
        public static EquipmentPack DupeLayers(Mobile src)
        {
            EquipmentPack pack = null;

            if (HasLayers(src) > 0)
                pack = new();
            else
                return null;

            try
            {
                foreach (Item item in GetLayerItems(src, includeHair: true))
                {
                    if (!HasParameterlessConstructor(item.GetType()))
                        continue;

                    Item new_item = null;

                    try
                    {
                        new_item = Dupe(item);
                    }
                    catch
                    {
                        /* keep going */
                    }

                    if (new_item != null)
                        pack.AddItem(new_item);
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

            return pack;
        }

        public static int HasLayers(Mobile src)
        {
            int count = 0;

            try
            {
                foreach (Item item in GetLayerItems(src, includeHair: true))
                {
                    if (HasParameterlessConstructor(item.GetType()))
                        count++;
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

            return count;
        }


        #endregion Layers
        #endregion Object Duplication & Property Copying

        #region Reflection & Property Introspection
        #region Object Property Display
        public static List<string> GetObjectProperties(object o)
        {
            List<string> return_list = new();
            ArrayList list = BuildList(o);
            for (int index = 0; index < list.Count; index++)
            {
                if (o == null)
                {
                    ;
                }
                else if (o is Type)
                {
                    Type type = (Type)o;
                    return_list.Add(type.Name);
                }
                else if (o is PropertyInfo)
                {
                    PropertyInfo prop = (PropertyInfo)o;
                    return_list.Add(ValueToString(o, prop));
                }
            }

            return return_list;
        }
        private static ArrayList BuildList(object o)
        {
            Type type = o.GetType();

            PropertyInfo[] props = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

            ArrayList groups = GetGroups(type, props);
            ArrayList list = new ArrayList();

            for (int i = 0; i < groups.Count; ++i)
            {
                DictionaryEntry de = (DictionaryEntry)groups[i];
                ArrayList groupList = (ArrayList)de.Value;

                if (i != 0)
                    list.Add(null);

                list.Add(de.Key);
                list.AddRange(groupList);
            }

            return list;
        }
        private static Type typeofCPA = typeof(CPA);
        private static Type typeofObject = typeof(object);
        private static CPA GetCPA(PropertyInfo prop)
        {
            object[] attrs = prop.GetCustomAttributes(typeofCPA, false);

            if (attrs.Length > 0)
                return attrs[0] as CPA;
            else
                return null;
        }
        private static ArrayList GetGroups(Type objectType, PropertyInfo[] props)
        {
            Hashtable groups = new Hashtable();

            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo prop = props[i];

                if (prop.CanRead)
                {
                    CPA attr = GetCPA(prop);

                    if (attr != null /*&& m_Mobile.AccessLevel >= attr.ReadLevel*/)
                    {
                        Type type = prop.DeclaringType;

                        while (true)
                        {
                            Type baseType = type.BaseType;

                            if (baseType == null || baseType == typeofObject)
                                break;

                            if (baseType.GetProperty(prop.Name, prop.PropertyType) != null)
                                type = baseType;
                            else
                                break;
                        }

                        ArrayList list = (ArrayList)groups[type];

                        if (list == null)
                            groups[type] = list = new ArrayList();

                        list.Add(prop);
                    }
                }
            }

            ArrayList sorted = new ArrayList(groups);

            sorted.Sort(new GroupComparer(objectType));

            return sorted;
        }
        private class GroupComparer : IComparer
        {
            private Type m_Start;

            public GroupComparer(Type start)
            {
                m_Start = start;
            }

            private static Type typeofObject = typeof(Object);

            private int GetDistance(Type type)
            {
                Type current = m_Start;

                int dist;

                for (dist = 0; current != null && current != typeofObject && current != type; ++dist)
                    current = current.BaseType;

                return dist;
            }

            public int Compare(object x, object y)
            {
                if (x == null && y == null)
                    return 0;
                else if (x == null)
                    return -1;
                else if (y == null)
                    return 1;

                if (!(x is DictionaryEntry) || !(y is DictionaryEntry))
                    throw new ArgumentException();

                DictionaryEntry de1 = (DictionaryEntry)x;
                DictionaryEntry de2 = (DictionaryEntry)y;

                Type a = (Type)de1.Key;
                Type b = (Type)de2.Key;

                return GetDistance(a).CompareTo(GetDistance(b));
            }
        }
        public static string ValueToString(object obj, PropertyInfo prop)
        {
            try
            {
                if (prop == null)
                    return "!Null PropertyInfo!";

                if (obj == null)
                {
                    MethodInfo mi = prop.GetGetMethod(false);
                    if (mi == null) // don't expect this to *ever* get hit.. would only occur on set-only props
                        return "!Null Obj, MethodInfo!"; // or non-public ones
                    if (!mi.IsStatic)
                        return "-null-";
                    // okay, wtf is going on!
                    throw new ApplicationException(string.Format("{0} was null", prop.ToString()));
                }

                return ValueToString(prop.GetValue(obj, null));
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                return string.Format("!{0}!", e.GetType());
            }
        }
        public static string ValueToString(object o)
        {
            if (o == null)
            {
                return "-null-";
            }
            else if (o is AccessLevel)
            {
                return o.ToString();
            }
            else if (o is string)
            {
                return string.Format("\"{0}\"", (string)o);
            }
            else if (o is bool)
            {
                return o.ToString();
            }
            else if (o is char)
            {
                return string.Format("0x{0:X} '{1}'", (int)(char)o, (char)o);
            }
            else if (o is Serial)
            {
                Serial s = (Serial)o;

                if (s.IsValid)
                {
                    if (s.IsItem)
                    {
                        return string.Format("(I) 0x{0:X}", s.Value);
                    }
                    else if (s.IsMobile)
                    {
                        return string.Format("(M) 0x{0:X}", s.Value);
                    }
                }

                return string.Format("(?) 0x{0:X}", s.Value);
            }
            else if (o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint || o is long || o is ulong)
            {
                return string.Format("{0} (0x{0:X})", o);
            }
            else if (o is Mobile)
            {
                return string.Format("(M) 0x{0:X} \"{1}\"", ((Mobile)o).Serial.Value, ((Mobile)o).Name);
            }
            else if (o is Item)
            {
                return string.Format("(I) 0x{0:X}", ((Item)o).Serial.Value);
            }
            else if (o is Type)
            {
                return ((Type)o).Name;
            }
            else
            {
                return o.ToString();
            }
        }

        #endregion Object Property Display
        public static bool HasParameterlessConstructor(Type t)
        {
            ConstructorInfo[] info = t.GetConstructors();

            foreach (ConstructorInfo c in info)
            {
                ParameterInfo[] paramInfo = c.GetParameters();

                if (paramInfo.Length == 0)
                    return true;
            }

            return false;
        }
        #endregion Reflection & Property Introspection
    }
}
