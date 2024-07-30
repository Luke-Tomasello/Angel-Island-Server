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

/* Items/Special/Enchanted/EnchantedScroll.cs
 * CHANGELOG:
 *	9/24/2023, Adam
 *		Initial check-in
 */

using Server.Diagnostics;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Items
{
    public class EnchantedScroll : Item
    {
        public static void Initialize()
        {
            CommandSystem.Register("Enchant", AccessLevel.Owner, new CommandEventHandler(Enchant_OnCommand));
        }

        private static int[] ItemIDs = Enumerable.Range(0x2260, 0x2279 - 0x2260 + 1).ToArray();

        private Item m_Item;

        public override Item Dupe(int amount)
        {

            EnchantedScroll new_scroll = new EnchantedScroll();
            if (new_scroll.Item != null)
            {   // gets defaulted to a random item;
                new_scroll.Item.Delete();
                new_scroll.Item = null;
            }
            // won't copy the item as it is marked CopyableAttribute(CopyType.DoNotCopy)
            Utility.CopyProperties(new_scroll, this);
            if (Item != null)
            {   // create a new item to attach to this new EnchantedScroll
                Type type = Item.GetType();
                object o = null;
                o = Activator.CreateInstance(type);
                Utility.CopyProperties((Item)o, Item);
                new_scroll.Item = o as Item;
            }
            return base.Dupe(new_scroll, amount);
        }
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public Item Item
        {
            get { return m_Item; }
            set
            {
                m_Item = value;

                if (m_Item != null)
                {
                    UpdateGrphic(hint: m_Item);
                    m_Item.SetItemBool(ItemBoolTable.Enchanted, true);
                    m_Item.MoveToIntStorage();
                }
            }
        }

        private static List<Type> ValidTypes = new() { typeof(BaseWeapon), typeof(BaseArmor), typeof(BaseJewel), typeof(BaseClothing), typeof(BaseWand) };

        public static bool IsValidType(Item item)
        {
            if (item != null)
            {
                Type type = item.GetType();
                foreach (Type t in ValidTypes)
                    if (t.IsAssignableFrom(type))
                        return true;
            }
            return false;
        }

        public static Item CreateRandom()
        {
            Item item = null;
            int min = 1;
            int max = 3;
            switch (Utility.Random(5))
            {
                case 0:
                    item = Loot.RandomMagicWeapon(min, max, 1.0);
                    break;
                case 1:
                    item = Loot.RandomMagicArmor(min, max, 1.0);
                    break;
                case 2:
                    item = Loot.RandomMagicJewelry(min, max, 1.0);
                    break;
                case 3:
                    item = Loot.RandomMagicClothing(min, max, 1.0);
                    break;
                case 4:
                    item = Loot.RandomMagicWand(min, max, 1.0);
                    break;
            }

            return item;
        }

        private void UpdateGrphic(Item hint)
        {   // each item type will have a pseudo unique scroll appearance
            if (hint != null)
                this.ItemID = ItemIDs[Utility.GetStableHashCode(hint.ItemID) % ItemIDs.Length];
        }

        [Constructable]
        public EnchantedScroll()
            : this(CreateRandom())
        {   /* useful for development */ }

        public EnchantedScroll(Item item)
            : base()
        {
            if (item != null && !IsValidType(item))
                throw new ArgumentException("Invalid type");
            else
            {
                Name = "an enchanted scroll";
                m_Item = item;
                if (m_Item != null)
                {
                    UpdateGrphic(hint: item);
                    m_Item.SetItemBool(ItemBoolTable.Enchanted, true);
                    m_Item.MoveToIntStorage();
                }

                Weight = 1;
            }
        }

        public EnchantedScroll(Serial serial)
            : base(serial)
        {
            Name = "an enchanted scroll";
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, "an enchanted scroll for");
            if (m_Item != null)
            {   // we need to handle OnSingleClick explicitly here since the actual item is on the internal map

                // blessed / cursed, etc.
                if (DisplayLootType)
                    LabelLootTypeTo(from);

                NetState ns = from.NetState;
                string name = m_Item.OldSchoolName();
                string typeName = m_Item.GetType().Name;
                bool needRealName = false;
                if (    // exact match
                        !name.Contains(typeName, StringComparison.OrdinalIgnoreCase) &&
                        // RingmailGloves => Ringmail Gloves
                        !name.Contains(Utility.SplitOnCase(typeName), StringComparison.OrdinalIgnoreCase) &&
                        // Make Executioner's Axe =>  Executioners Axe
                        !name.Replace("'", "").Contains(Utility.SplitOnCase(typeName), StringComparison.OrdinalIgnoreCase)
                        )
                    needRealName = true;

                // Trickery!
                ns.Send(new UnicodeMessage(this.Serial, m_Item.ItemID, MessageType.Label, 0x3B2, 3, "ENU", "", name));

                if (needRealName)
                    LabelTo(from, string.Format("({0})", typeName));
            }
            else
                base.OnSingleClick(from);
        }

        public override void OnAfterDelete()
        {
            if (m_Item != null)
                m_Item.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Item);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Item = reader.ReadItem();
                        break;
                    }
            }
        }

        [Usage("Enchant <Item>")]
        [Description("Converts item to an Enchanted Scroll")]
        public static void Enchant_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Target the item you wish to enchant.");
                e.Mobile.Target = new ItemTarget();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        public class ItemTarget : Target
        {
            public ItemTarget()
                : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Item item = (targeted as Item);
                if (item != null)
                {
                    if (IsValidType(item))
                    {
                        EnchantedScroll scroll = new EnchantedScroll(null);
                        if (scroll != null)
                        {
                            Utility.ReplaceItem(scroll, item, copy_properties: false);
                            scroll.Item = item;
                        }
                    }
                    else
                        from.SendMessage("That cannot be made into a scroll.");
                }
                else
                    from.SendMessage("That is not an Item.");
            }
        }

        public static EnchantedScroll Find(Mobile from, Type type)
        {
            Container pack = from.Backpack;

            if (pack == null)
                return null;

            foreach (EnchantedScroll scroll in pack.FindItemsByType<EnchantedScroll>())
            {
                if (scroll.Item != null && scroll.Item.GetType() == type)
                    return scroll;
            }

            return null;
        }

        public double GetImbueChance(Mobile from, SkillName skill)
        {
            double reqSkill, minSkill, maxSkill;

            return GetImbueChance(from, skill, out reqSkill, out minSkill, out maxSkill);
        }

        public double GetImbueChance(Mobile from, SkillName skill, out double reqSkill, out double minSkill, out double maxSkill)
        {
            reqSkill = Math.Min(100.0, 80.0 + CalculateIntensity());
            minSkill = reqSkill - 1.0;
            maxSkill = reqSkill + 49.0;

            double skillValue = from.Skills[skill].Value;

            if (skillValue < reqSkill)
                return 0.0;

            return (skillValue - minSkill) / (maxSkill - minSkill);
        }

        public bool HandleCraft(Mobile from, SkillName skill, out Item item, out TextDefinition notice)
        {
            item = null;

            double chance = GetImbueChance(from, skill);

            bool success = (chance > 0.0 && Utility.RandomDouble() < chance);

            if (!success)
            {
                notice = 0;
                return false;
            }

            Effects.PlaySound(from.Location, from.Map, 0x1E9);
            Effects.SendTargetParticles(from, 0x373A, 35, 45, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

            item = Extract();

            if (item == null)
            {
                notice = 0;
                return false;
            }

            SetIdentified(item, true);

            notice = "You imbue the item using the enchanted scroll.";

            LogHelper logger = new LogHelper("Enchanted Items.log", false, true);
            logger.Log(string.Format("{0} successfully imbued {1}", from, item));
            logger.Finish();

            return true;
        }

        public Item Extract()
        {
            Item item = m_Item;

            if (item != null)
            {
                m_Item = null;
                item.IsIntMapStorage = false;
                ReplaceWith(item);
            }

            return item;
        }

        private double CalculateIntensity()
        {
            double value = 0.0;

            if (m_Item is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)m_Item;

                value += GetMagicIntentity((int)weapon.DamageLevel);
                value += GetMagicIntentity((int)weapon.AccuracyLevel);
                value += GetMagicIntentity((int)weapon.DurabilityLevel);
            }
            else if (m_Item is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)m_Item;

                value += GetMagicIntentity((int)armor.ProtectionLevel);
                value += GetMagicIntentity((int)armor.DurabilityLevel);
            }

            if (m_Item is IMagicEquip)
            {
                IMagicEquip magicEquip = (IMagicEquip)m_Item;

                if (magicEquip.MagicEffect != MagicEquipEffect.None)
                    value += 0.1 * magicEquip.MagicCharges;
            }

            return value;
        }

        private static readonly double[] m_MagicIntensityTable = new double[]
            {
                1.0, // Ruin        Accurate        Defense         Durable
                2.0, // Might       Surpassingly    Guarding        Substantial
                3.0, // Force       Eminently       Hardening       Massive
                4.0, // Power       Exceedingly     Fortification   Fortified
                5.0, // Vanquishing Supremely       Invulnerability Indestructible
            };

        private static double GetMagicIntentity(int v)
        {
            if (v <= 0)
                return 0.0;

            int index = v - 1;

            if (index >= m_MagicIntensityTable.Length)
                index = m_MagicIntensityTable.Length - 1;

            return m_MagicIntensityTable[index];
        }

        private static void SetIdentified(Item item, bool value)
        {
            if (item is BaseWeapon)
                ((BaseWeapon)item).Identified = value;
            else if (item is BaseArmor)
                ((BaseArmor)item).Identified = value;
            else if (item is BaseJewel)
                ((BaseJewel)item).Identified = value;
            else if (item is BaseClothing)
                ((BaseClothing)item).Identified = value;
        }

        public static void CopyProps(Item from, Item to)
        {
            if (from.Hue != 0)
            {   // Adam: some enchanted items already have a resource type and hue, restore that here.
                //  Also, this is where also reset the quality 
                to.Hue = from.Hue;
                if (to is BaseWeapon)
                    (to as BaseWeapon).Resource = (from as BaseWeapon).Resource;
                else if (to is BaseArmor)
                    (to as BaseArmor).Resource = (from as BaseArmor).Resource;
                else if (to is BaseClothing)
                    (to as BaseClothing).Resource = (from as BaseClothing).Resource;
                else if (to is BaseJewel)
                    (to as BaseJewel).Resource = (from as BaseJewel).Resource;
            }
            if (to is BaseWeapon)
            {
                (to as BaseWeapon).Quality = (from as BaseWeapon).Quality;
                (to as BaseWeapon).Origin = (from as BaseWeapon).Origin;
            }
            else if (to is BaseArmor)
            {
                (to as BaseArmor).Quality = (from as BaseArmor).Quality;
                (to as BaseArmor).Origin = (from as BaseArmor).Origin;
            }
            else if (to is BaseClothing)
            {
                (to as BaseClothing).Quality = (from as BaseClothing).Quality;
                (to as BaseClothing).Origin = (from as BaseClothing).Origin;
            }
            else if (to is BaseJewel)
            {
                (to as BaseJewel).Quality = (from as BaseJewel).Quality;
                (to as BaseJewel).Origin = (from as BaseJewel).Origin;
            }
        }
    }
}