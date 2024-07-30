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

/* Scripts/Engines/Craft/Core/CraftSystem.cs
 * ChangeLog:
 *  9/28/2023, Adam (CraftableTypes / IsCraftable)
 *      1. Expose the list of craftable items for external access
 *      2. Add a query if an item is craftable under our system.
 *  6/20/23, Yoar (OnBeforeResDelete)
 *      Removed OnBeforeResDelete. This was causing weight overloading issues.
 *      Internally, it was probably caused by Item.Dupe. Anyways, OnBeforeResDelete
 *      was only used for seed planting. We'll redo seed planting in another way.
 *  4/18/23, Yoar
 *      Added Enabled virtual getter.
 *  2/14/22, Yoar
 *      Added SetPicture method.
 *  3/17.22, Adam (OnBeforeResDelete)
 *   Add virtual void OnBeforeResDelete() to allow craft systems to make use of the items used to create the CraftItem
 *  2/14/22, Yoar
 *      Added virtual GetCraftEffectMax to set the number of craft effects for a particular CraftItem.
 *  1/8/22, Yoar
 *      Simplified input structure of CraftRes, CraftGroup constructors using TextEntry.
 *      Added CraftRes.Predicate: Check additional property requirements for the resource.
 *  12/16/21, Yoar
 *      Generalized "Resmelt" option into "Recycle" option.
 *  11/29/21, Yoar
 *      Moved TextEntry to Misc/TextEntry.cs
 *  11/22/21, Yoar
 *      Added CraftItem.ItemID, CraftItem.ItemHue: Sets the item display in the craft gump.
 *      Added CraftSystem.GiveItem: Instead of adding the item to the player's pack, do something else.
 *  10/12/21, Yoar
 *      Changed return type of CraftSystem.PlayEndingEffect to "object" to support strings
 *  9/19/21, Yoar
 *      Added virtual DoesCraftEffect, DoesSkillGain methods
 *  9/19/21, Yoar
 *      Redid input structure of AddCraft using TextEntry
 *	10/15/05, erlein
 *		Added NeedWater for special dying system.
 */

using Server.Items;
using Server.Misc;
using Server.Township;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Engines.Craft
{
    public enum CraftECA
    {
        ChanceMinusSixty,
        FiftyPercentChanceMinusTenPercent,
        ChanceMinusSixtyToFourtyFive
    }

    public abstract class CraftSystem
    {
        private static readonly List<CraftSystem> m_Instances = new List<CraftSystem>();

        public static List<CraftSystem> Instances { get { return m_Instances; } }

        public virtual bool Enabled { get { return true; } }

        public virtual bool SupportsEnchantedScrolls { get { return true; } }

        private int m_MinCraftEffect;
        private int m_MaxCraftEffect;
        private double m_Delay;
        private Recycle m_Recycle;
        private bool m_Repair;
        private bool m_MarkOption;
        private bool m_CanEnhance;

        private CraftItemCol m_CraftItems;
        private CraftGroupCol m_CraftGroups;
        private CraftSubResCol m_CraftSubRes;
        private CraftSubResCol m_CraftSubRes2;

        public int MinCraftEffect { get { return m_MinCraftEffect; } set { m_MinCraftEffect = value; } }
        public int MaxCraftEffect { get { return m_MaxCraftEffect; } set { m_MaxCraftEffect = value; } }
        public double Delay { get { return m_Delay; } set { m_Delay = value; } }

        public CraftItemCol CraftItems { get { return m_CraftItems; } }
        public CraftGroupCol CraftGroups { get { return m_CraftGroups; } }
        public CraftSubResCol CraftSubRes { get { return m_CraftSubRes; } }
        public CraftSubResCol CraftSubRes2 { get { return m_CraftSubRes2; } }

        public abstract SkillName MainSkill { get; }

        public virtual TextDefinition GumpTitle { get { return 0; } }

        public virtual CraftECA ECA { get { return CraftECA.ChanceMinusSixty; } }

        private Hashtable m_ContextTable = new Hashtable();

        public abstract double GetChanceAtMin(CraftItem item);

        public virtual bool RetainsColorFrom(CraftItem item, Type type)
        {
            return false;
        }

        public CraftContext GetContext(Mobile m)
        {
            if (m == null)
                return null;

            if (m.Deleted)
            {
                m_ContextTable.Remove(m);
                return null;
            }

            CraftContext c = (CraftContext)m_ContextTable[m];

            if (c == null)
                m_ContextTable[m] = c = new CraftContext();

            return c;
        }

        public void OnMade(Mobile m, CraftItem item)
        {
            CraftContext c = GetContext(m);

            if (c != null)
                c.OnMade(item);
        }

        public Recycle Recycle
        {
            get { return m_Recycle; }
            set { m_Recycle = value; }
        }

        public bool Repair
        {
            get { return m_Repair; }
            set { m_Repair = value; }
        }

        public bool MarkOption
        {
            get { return m_MarkOption; }
            set { m_MarkOption = value; }
        }

        public bool CanEnhance
        {
            get { return m_CanEnhance; }
            set { m_CanEnhance = value; }
        }

        public CraftSystem(int minCraftEffect, int maxCraftEffect, double delay)
        {
            m_MinCraftEffect = minCraftEffect;
            m_MaxCraftEffect = maxCraftEffect;
            m_Delay = delay;

            m_CraftItems = new CraftItemCol();
            m_CraftGroups = new CraftGroupCol();
            m_CraftSubRes = new CraftSubResCol();
            m_CraftSubRes2 = new CraftSubResCol();

            InitCraftList();

            m_Instances.Add(this);
        }

        public virtual bool ConsumeResOnFailure(Mobile from, Type resourceType, CraftItem craftItem)
        {
            return true;
        }

        public virtual bool ConsumeAttributeOnFailure(Mobile from)
        {
            return false;
        }

        public void CreateItem(Mobile from, Type type, Type typeRes, BaseTool tool, CraftItem realCraftItem)
        {
            // Verify if the type is in the list of the craftable item
            CraftItem craftItem = m_CraftItems.SearchFor(type);
            if (craftItem != null)
            {
                // The item is in the list, try to create it
                // Test code: items like sextant parts can be crafted either directly from ingots, or from different parts
                realCraftItem.Craft(from, this, typeRes, tool);
                //craftItem.Craft( from, this, typeRes, tool );
            }
        }

        public int AddCraft(Type typeItem, TextDefinition groupName, TextDefinition itemName, double minSkill, double maxSkill, Type typeRes, TextDefinition nameRes, int amount)
        {
            return AddCraft(typeItem, groupName, itemName, MainSkill, minSkill, maxSkill, typeRes, nameRes, amount, null, null, null);
        }

        public int AddCraft(Type typeItem, TextDefinition groupName, TextDefinition itemName, double minSkill, double maxSkill, Type typeRes, TextDefinition nameRes, int amount, TextDefinition message)
        {
            return AddCraft(typeItem, groupName, itemName, MainSkill, minSkill, maxSkill, typeRes, nameRes, amount, message, null, null);
        }

        public int AddCraft(Type typeItem, TextDefinition groupName, TextDefinition itemName, double minSkill, double maxSkill, Type typeRes, TextDefinition nameRes, int amount, TextDefinition message, params object[] args)
        {
            return AddCraft(typeItem, groupName, itemName, MainSkill, minSkill, maxSkill, typeRes, nameRes, amount, message, null, args);
        }

        public int AddCraft(Type typeItem, TextDefinition groupName, TextDefinition itemName, double minSkill, double maxSkill, Type typeRes, TextDefinition nameRes, int amount, TextDefinition message, Predicate<Item> predicate, params object[] args)
        {
            return AddCraft(typeItem, groupName, itemName, MainSkill, minSkill, maxSkill, typeRes, nameRes, amount, message, predicate, args);
        }

        public int AddCraft(Type typeItem, TextDefinition groupName, TextDefinition itemName, SkillName skillToMake, double minSkill, double maxSkill, Type typeRes, TextDefinition nameRes, int amount)
        {
            return AddCraft(typeItem, groupName, itemName, skillToMake, minSkill, maxSkill, typeRes, nameRes, amount, null, null, null);
        }

        public int AddCraft(Type typeItem, TextDefinition groupName, TextDefinition itemName, SkillName skillToMake, double minSkill, double maxSkill, Type typeRes, TextDefinition nameRes, int amount, TextDefinition message)
        {
            return AddCraft(typeItem, groupName, itemName, skillToMake, minSkill, maxSkill, typeRes, nameRes, amount, message, null, null);
        }

        public int AddCraft(Type typeItem, TextDefinition groupName, TextDefinition itemName, SkillName skillToMake, double minSkill, double maxSkill, Type typeRes, TextDefinition nameRes, int amount, TextDefinition message, params object[] args)
        {
            return AddCraft(typeItem, groupName, itemName, skillToMake, minSkill, maxSkill, typeRes, nameRes, amount, message, null, args);
        }
        private static List<Type> m_CraftableTypes = new();
        public static List<Type> CraftableTypes { get { return m_CraftableTypes; } }
        public static bool IsCraftable(Item item)
        {
            return IsCraftable(item.GetType());
        }

        //private static List<Type> m_fail = new();
        public static bool IsCraftable(Type type)
        {
            if (m_CraftableTypes.Contains(type))
                return true;

            //if (!m_fail.Contains(type))
            //{
            //    m_fail.Add(type);
            //    LogHelper logger = new LogHelper("cant make.log", false, true, true);
            //    logger.Log(type.ToString());

            //    logger.Finish();
            //}

            return false;
        }
        public static int InitCraftTable()
        {   // this calls the InitCraftList for each craft whereby loading our IsCraftable table with craftable types
            CraftSystem temp;
            int patched = 0;
            temp = DefAlchemy.CraftSystem; patched++;
            temp = DefBlacksmithy.CraftSystem; patched++;
            temp = DefBowFletching.CraftSystem; patched++;
            temp = DefCarpentry.CraftSystem; patched++;
            temp = DefCartography.CraftSystem; patched++;
            temp = DefCooking.CraftSystem; patched++;
            temp = DefGlassblowing.CraftSystem; patched++;
            temp = DefInscription.CraftSystem; patched++;
            temp = DefMasonry.CraftSystem; patched++;
            temp = DefTailoring.CraftSystem; patched++;
            temp = DefTinkering.CraftSystem; patched++;
            temp = DefTownshipCraft.CraftSystem; patched++;
            return patched;
        }
        public int AddCraft(Type typeItem, TextDefinition groupName, TextDefinition itemName, SkillName skillToMake, double minSkill, double maxSkill, Type typeRes, TextDefinition nameRes, int amount, TextDefinition message, Predicate<Item> predicate, params object[] args)
        {
            if (!m_CraftableTypes.Contains(typeItem))
                m_CraftableTypes.Add(typeItem);

            CraftItem craftItem = new CraftItem(typeItem, groupName, itemName);

            craftItem.AddRes(typeRes, nameRes, amount, message, predicate);
            craftItem.AddSkill(skillToMake, minSkill, maxSkill);
            craftItem.CraftArgs = args;

            DoGroup(groupName, craftItem);

            return m_CraftItems.Add(craftItem);
        }

        private void DoGroup(TextDefinition groupName, CraftItem craftItem)
        {
            int index = m_CraftGroups.SearchFor(groupName);

            if (index == -1)
            {
                CraftGroup craftGroup = new CraftGroup(groupName);

                craftGroup.AddCraftItem(craftItem);

                m_CraftGroups.Add(craftGroup);
            }
            else
            {
                m_CraftGroups.GetAt(index).AddCraftItem(craftItem);
            }
        }

        public void SetItemID(int index, int id)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.ItemID = id;
        }

        public void SetItemHue(int index, int hue)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.ItemHue = hue;
        }

        public void SetPicture(int index, ItemPicture picture)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.Picture = picture;
        }

        public void SetManaReq(int index, int mana)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.Mana = mana;
        }

        public void SetStamReq(int index, int stam)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.Stam = stam;
        }

        public void SetHitsReq(int index, int hits)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.Hits = hits;
        }

        public void SetUseAllRes(int index, bool useAll)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.UseAllRes = useAll;
        }

        public void SetNeedHeat(int index, bool needHeat)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.NeedHeat = needHeat;
        }

        public void SetNeedOven(int index, bool needOven)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.NeedOven = needOven;
        }

        public void SetNeedMill(int index, bool needMill)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.NeedMill = needMill;
        }

        public void SetNeedWater(int index, bool needWater)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.NeedWater = needWater;
        }

        public void AddRes(int index, Type type, TextDefinition name, int amount)
        {
            AddRes(index, type, name, amount, null, null);
        }

        public void AddRes(int index, Type type, TextDefinition name, int amount, TextDefinition message)
        {
            AddRes(index, type, name, amount, message, null);
        }

        public void AddRes(int index, Type type, TextDefinition name, int amount, TextDefinition message, Predicate<Item> predicate)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.AddRes(type, name, amount, message, predicate);
        }


        public void AddRes(int index, Type type, string name, int amount)
        {
            AddRes(index, type, name, amount, "");
        }

        public void AddRes(int index, Type type, string name, int amount, int localizedMessage)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.AddRes(type, name, amount, localizedMessage);
        }

        public void AddRes(int index, Type type, string name, int amount, string strMessage)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.AddRes(type, name, amount, strMessage);
        }

        public void AddSkill(int index, SkillName skillToMake, double minSkill, double maxSkill)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.AddSkill(skillToMake, minSkill, maxSkill);
        }

        public void SetUseSubRes2(int index, bool val)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.UseSubRes2 = val;
        }

        //public void AddRecipe(int index, int id)
        //{
        //    CraftItem craftItem = m_CraftItems.GetAt(index);
        //    craftItem.AddRecipe(id, this);
        //}

        public void ForceNonExceptional(int index)
        {
            CraftItem craftItem = m_CraftItems.GetAt(index);
            craftItem.ForceNonExceptional = true;
        }

        public void SetSubRes(Type type, TextDefinition name)
        {
            m_CraftSubRes.ResType = type;
            m_CraftSubRes.Name = name;
            m_CraftSubRes.Init = true;
        }

        public void AddSubRes(Type type, TextDefinition name, double reqSkill, TextDefinition message)
        {
            CraftSubRes craftSubRes = new CraftSubRes(type, name, reqSkill, message);
            m_CraftSubRes.Add(craftSubRes);
        }

        public void AddSubRes(Type type, TextDefinition name, double reqSkill, TextDefinition genericName, TextDefinition message)
        {
            CraftSubRes craftSubRes = new CraftSubRes(type, name, reqSkill, genericName, message);
            m_CraftSubRes.Add(craftSubRes);
        }


        public void SetSubRes2(Type type, TextDefinition name)
        {
            m_CraftSubRes2.ResType = type;
            m_CraftSubRes2.Name = name;
            m_CraftSubRes2.Init = true;
        }

        public void AddSubRes2(Type type, TextDefinition name, double reqSkill, TextDefinition message)
        {
            CraftSubRes craftSubRes = new CraftSubRes(type, name, reqSkill, message);
            m_CraftSubRes2.Add(craftSubRes);
        }

        public void AddSubRes2(Type type, TextDefinition name, double reqSkill, TextDefinition genericName, TextDefinition message)
        {
            CraftSubRes craftSubRes = new CraftSubRes(type, name, reqSkill, genericName, message);
            m_CraftSubRes2.Add(craftSubRes);
        }


        public abstract void InitCraftList();

        public abstract void PlayCraftEffect(Mobile from, object obj = null);
        public abstract TextDefinition PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item);

        public abstract TextDefinition CanCraft(Mobile from, BaseTool tool, Type itemType);

        #region old school menus
        private bool m_oldSchool = false;
        public bool OldSchool { get { return m_oldSchool; } set { m_oldSchool = value; } }
        #endregion

        public virtual int GetCraftEffectMax(CraftItem craftItem)
        {
            // returns 1 plus the number of craft effects that will be played
            return 1 + Utility.RandomMinMax(m_MinCraftEffect, m_MaxCraftEffect);
        }

        public virtual bool DoesCraftEffect(CraftItem craftItem)
        {
            return true;
        }

        public virtual bool DoesSkillGain(CraftItem craftItem)
        {
            return true;
        }

        public virtual bool GiveItem(Mobile from, Item item)
        {
            return false;
        }
    }
}