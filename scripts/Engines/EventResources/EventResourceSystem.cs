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

/* Server/Engines/EventResources/EventResourceSystem.cs
 * CHANGELOG:
 *  12/9/23, Yoar
 *      Initial version.
 */

using Server.Diagnostics;
using Server.Engines.Craft;
using Server.Items;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Engines.EventResources
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventCraftAttribute : Attribute
    {
        public static CraftResource Find(Type type)
        {
            EventCraftAttribute eca = type.GetCustomAttribute(typeof(EventCraftAttribute)) as EventCraftAttribute;

            return (eca == null ? CraftResource.None : eca.Resource);
        }

        private CraftResource m_Resource;

        public CraftResource Resource { get { return m_Resource; } }

        public EventCraftAttribute(CraftResource resource)
        {
            m_Resource = resource;
        }
    }

    public abstract class EventResourceSystem
    {
        private static readonly List<EventResourceSystem> m_Systems = new List<EventResourceSystem>();

        public static List<EventResourceSystem> Systems { get { return m_Systems; } }

        static EventResourceSystem()
        {
            #region Dynamic Registration

            foreach (Assembly asm in ScriptCompiler.Assemblies)
            {
                foreach (Type type in asm.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(EventResourceSystem)))
                    {
                        PropertyInfo prop = type.GetProperty("System", BindingFlags.Static | BindingFlags.Public);

                        if (prop == null)
                            continue;

                        MethodInfo accessor = prop.GetGetMethod();

                        if (accessor == null)
                            continue;

                        m_Systems.Add((EventResourceSystem)accessor.Invoke(null, null));
                    }
                }
            }

            #endregion
        }

        public static EventResourceSystem Find(CraftResource resource)
        {
            foreach (EventResourceSystem system in m_Systems)
            {
                if (system.Resource == resource)
                    return system;
            }

            return null;
        }

        public static void CheckRegistry(Item item, bool add)
        {
            foreach (EventResourceSystem system in m_Systems)
            {
                system.Defrag();

                if (add)
                {
                    if (GetResource(item) == system.Resource && !system.Registry.Contains(item))
                        system.Registry.Add(item);
                }
                else
                {
                    system.Registry.Remove(item);
                }
            }
        }

        private CraftResource m_Resource;

        private int m_ArmorBonus;
        private ArmorProtectionLevel m_ProtectionLevel;
        private int m_DamageBonus;
        private WeaponDamageLevel m_DamageLevel;
        private int m_DurabilityScalar;

        private double m_ArmorImbueChance;
        private int m_ArmorImbueCap;
        private bool m_ArmorImbueScaling;

        private double m_WeaponImbueChance;
        private int m_WeaponImbueCap;
        private bool m_WeaponImbueScaling;

        private double m_StatueImbueChance;
        private int m_StatueImbueCap;
        private bool m_StatueImbueScaling;

        private EnchantmentEntry[] m_Enchantments;

        private List<Item> m_Registry;

        public CraftResource Resource { get { return m_Resource; } set { m_Resource = value; } }

        public int ArmorBonus { get { return m_ArmorBonus; } set { m_ArmorBonus = value; } }
        public ArmorProtectionLevel ProtectionLevel { get { return m_ProtectionLevel; } set { m_ProtectionLevel = value; } }
        public int DamageBonus { get { return m_DamageBonus; } set { m_DamageBonus = value; } }
        public WeaponDamageLevel DamageLevel { get { return m_DamageLevel; } set { m_DamageLevel = value; } }
        public int DurabilityScalar { get { return m_DurabilityScalar; } set { m_DurabilityScalar = value; } }

        public double ArmorImbueChance { get { return m_ArmorImbueChance; } set { m_ArmorImbueChance = value; } }
        public int ArmorImbueCap { get { return m_ArmorImbueCap; } set { m_ArmorImbueCap = value; } }
        public bool ArmorImbueScaling { get { return m_ArmorImbueScaling; } set { m_ArmorImbueScaling = value; } }

        public double WeaponImbueChance { get { return m_WeaponImbueChance; } set { m_WeaponImbueChance = value; } }
        public int WeaponImbueCap { get { return m_WeaponImbueCap; } set { m_WeaponImbueCap = value; } }
        public bool WeaponImbueScaling { get { return m_WeaponImbueScaling; } set { m_WeaponImbueScaling = value; } }

        public double StatueImbueChance { get { return m_StatueImbueChance; } set { m_StatueImbueChance = value; } }
        public int StatueImbueCap { get { return m_StatueImbueCap; } set { m_StatueImbueCap = value; } }
        public bool StatueImbueScaling { get { return m_StatueImbueScaling; } set { m_StatueImbueScaling = value; } }

        public EnchantmentEntry[] Enchantments { get { return m_Enchantments; } set { m_Enchantments = value; } }

        public List<Item> Registry { get { return m_Registry; } }

        public EventResourceSystem()
        {
            m_Enchantments = new EnchantmentEntry[0];

            m_Registry = new List<Item>();
        }

        public virtual CraftSystem MutateCraft(Mobile from, BaseTool tool)
        {
            return null;
        }

        public virtual bool Validate(Item item)
        {
            return true;
        }

        public void OnCraft(Mobile from, Item item)
        {
            if (m_ProtectionLevel != ArmorProtectionLevel.Regular && item is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)item;

                armor.ProtectionLevel = m_ProtectionLevel;
                armor.Identified = true;
            }

            if (m_DamageLevel != WeaponDamageLevel.Regular && item is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)item;

                weapon.DamageLevel = m_DamageLevel;
                weapon.Identified = true;
            }

            foreach (EnchantmentEntry ae in m_Enchantments)
            {
                if (ae.BaseType.IsAssignableFrom(item.GetType()))
                {
                    BaseEnchantment[] table = ae.Enchantments;

                    if (Utility.RandomDouble() < GetImbueChance(item.GetType()))
                    {
                        BaseEnchantment enchantment = GetRandomEnchantment(item, table);

                        if (enchantment != null)
                        {
                            enchantment.Enchant(item);

                            from.SendMessage("The item becomes imbued with magical properties.");

                            from.FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist);
                            from.PlaySound(0x1F7);
                        }
                    }

                    break;
                }
            }

            LogHelper logger = new LogHelper("EventResource.log", false, true);
            logger.Log(LogType.Mobile, from, String.Format("Crafted item {0} ({1}) with event resource {2}. MagicEffect={3}, MagicCharges={4}, Slayer={5}.",
                item.ToString(),
                item.GetType().Name,
                m_Resource,
                (item is IMagicItem ? ((IMagicItem)item).MagicEffect : MagicItemEffect.None),
                (item is IMagicItem ? ((IMagicItem)item).MagicCharges : 0),
                (item is BaseWeapon ? ((BaseWeapon)item).Slayer : SlayerName.None)));
            logger.Finish();
        }

        public double GetImbueChance(Type itemType)
        {
            if (typeof(BaseArmor).IsAssignableFrom(itemType))
            {
                if (m_ArmorImbueScaling)
                    return ScaleChance(m_ArmorImbueChance, m_ArmorImbueCap, CountImbuedByType(typeof(BaseArmor)));
                else
                    return m_ArmorImbueChance;
            }
            else if (typeof(BaseStatue).IsAssignableFrom(itemType))
            {
                if (m_StatueImbueScaling)
                    return ScaleChance(m_StatueImbueChance, m_StatueImbueCap, CountImbuedByType(typeof(BaseStatue)));
                else
                    return m_StatueImbueChance;
            }
            else if (typeof(BaseWeapon).IsAssignableFrom(itemType))
            {
                if (m_WeaponImbueScaling)
                    return ScaleChance(m_WeaponImbueChance, m_WeaponImbueCap, CountImbuedByType(typeof(BaseWeapon)));
                else
                    return m_WeaponImbueChance;
            }

            return 0.0;
        }

        protected void Defrag()
        {
            for (int i = m_Registry.Count - 1; i >= 0; i--)
            {
                if (m_Registry[i].Deleted || GetResource(m_Registry[i]) != m_Resource)
                    m_Registry.RemoveAt(i);
            }
        }

        public int CountByType(Type baseType)
        {
            Defrag();

            int count = 0;

            foreach (Item item in m_Registry)
            {
                if (baseType.IsAssignableFrom(item.GetType()))
                    count++;
            }

            return count;
        }

        public int CountImbuedByType(Type baseType)
        {
            Defrag();

            int count = 0;

            foreach (Item item in m_Registry)
            {
                if (baseType.IsAssignableFrom(item.GetType()) && IsImbued(item))
                    count++;
            }

            return count;
        }

        protected static CraftResource GetResource(Item item)
        {
            if (item is BaseArmor)
                return ((BaseArmor)item).Resource;
            else if (item is BaseStatue)
                return ((BaseStatue)item).Resource;
            else if (item is BaseWeapon)
                return ((BaseWeapon)item).Resource;
            else
                return CraftResource.None;
        }

        protected static bool IsImbued(Item item)
        {
            if (item is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)item;

                return (armor.MagicEffect != MagicEquipEffect.None && armor.MagicCharges > 0);
            }
            else if (item is BaseStatue)
            {
                BaseStatue statue = (BaseStatue)item;

                return (statue.MagicEffect != MagicItemEffect.None && statue.MagicCharges > 0);
            }
            else if (item is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)item;

                return ((weapon.MagicEffect != MagicItemEffect.None && weapon.MagicCharges > 0) || weapon.Slayer != SlayerName.None);
            }

            return false;
        }

        protected static double ScaleChance(double maxChance, int maxItems, int curItems)
        {
            if (curItems <= 0)
                return maxChance;
            else if (curItems >= maxItems)
                return 0.0;

            return maxChance - Math.Sqrt((maxChance * maxChance / maxItems) * curItems);
        }

        protected static BaseEnchantment GetRandomEnchantment(Item item, BaseEnchantment[] table)
        {
            int total = 0;

            for (int i = 0; i < table.Length; i++)
            {
                if (table[i].Validate(item))
                    total += table[i].Weight;
            }

            if (total <= 0)
                return null;

            int rnd = Utility.Random(total);

            for (int i = 0; i < table.Length; i++)
            {
                if (table[i].Validate(item))
                {
                    if (rnd < table[i].Weight)
                        return table[i];
                    else
                        rnd -= table[i].Weight;
                }
            }

            return null;
        }
    }

    public class EnchantmentEntry
    {
        private Type m_BaseType;
        private BaseEnchantment[] m_Enchantments;

        public Type BaseType { get { return m_BaseType; } }
        public BaseEnchantment[] Enchantments { get { return m_Enchantments; } }

        public EnchantmentEntry(Type baseType, BaseEnchantment[] enchantments)
        {
            m_BaseType = baseType;
            m_Enchantments = enchantments;
        }
    }
}