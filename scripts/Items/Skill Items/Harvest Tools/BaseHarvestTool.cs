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

/* Scripts/Items/Skill Items\Harvest Tools\BaseHarvestTool.cs
 * CHANGELOG
 *  5/1/22, Yoar
 *      Added m_Resource field.
 *      Tool uses are now scaled by resource (similar to how armor durability is scaled by resource).
 *  04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 */

using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Engines.Harvest;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections;

namespace Server.Items
{
    interface IUsesRemaining
    {
        int UsesRemaining { get; set; }
        bool ShowUsesRemaining { get; set; }
    }

    public abstract class BaseHarvestTool : Item, IUsesRemaining, ICraftable
    {
        private MakersMark m_Crafter;
        private ToolQuality m_Quality;
        private int m_UsesRemaining;
        private CraftResource m_Resource;

        [CommandProperty(AccessLevel.GameMaster)]
        public MakersMark Crafter
        {
            get { return m_Crafter; }
            set { m_Crafter = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ToolQuality Quality
        {
            get { return m_Quality; }
            set { UnscaleUses(); m_Quality = value; InvalidateProperties(); ScaleUses(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set
            {
                if (m_Resource != value)
                {
                    UnscaleUses();

                    m_Resource = value;
                    Hue = CraftResources.GetHue(m_Resource);

                    InvalidateProperties();

                    ScaleUses();
                }
            }
        }

        public void ScaleUses()
        {
            int scale = 100 + GetDurabilityBonus();

            m_UsesRemaining = (m_UsesRemaining * scale) / 100;
            InvalidateProperties();
        }

        public void UnscaleUses()
        {
            int scale = 100 + GetDurabilityBonus();

            m_UsesRemaining = (m_UsesRemaining * 100) / scale;
        }

        public int GetDurabilityBonus()
        {
            int bonus = 0;

            if (m_Quality == ToolQuality.Exceptional)
                bonus += 100;

            switch (m_Resource)
            {
                case CraftResource.OakWood:
                case CraftResource.DullCopper: bonus += 5; break;
                case CraftResource.ShadowIron: bonus += 10; break;
                case CraftResource.AshWood:
                case CraftResource.Copper: bonus += 15; break;
                case CraftResource.YewWood:
                case CraftResource.Bronze: bonus += 20; break;
                case CraftResource.Gold: bonus += 50; break;
                case CraftResource.Heartwood:
                case CraftResource.Agapite: bonus += 70; break;
                case CraftResource.Bloodwood:
                case CraftResource.Verite: bonus += 100; break;
                case CraftResource.Frostwood:
                case CraftResource.Valorite: bonus += 120; break;
            }

            return bonus;
        }

        public bool ShowUsesRemaining { get { return true; } set { } }

        public abstract HarvestSystem HarvestSystem { get; }

        public BaseHarvestTool(int itemID)
            : this(50, itemID)
        {
        }

        public BaseHarvestTool(int usesRemaining, int itemID)
            : base(itemID)
        {
            m_UsesRemaining = usesRemaining;
            m_Quality = ToolQuality.Regular;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            // Makers mark not displayed on OSI
            //if ( m_Crafter != null )
            //	list.Add( 1050043, m_Crafter.Name ); // crafted by ~1_NAME~

            if (m_Quality == ToolQuality.Exceptional)
                list.Add(1060636); // exceptional

            list.Add(1060584, m_UsesRemaining.ToString()); // uses remaining: ~1_val~
        }

        public virtual void DisplayDurabilityTo(Mobile m)
        {
            LabelToAffix(m, 1017323, AffixType.Append, ": " + m_UsesRemaining.ToString()); // Durability
        }

        public override void OnSingleClick(Mobile from)
        {
            DisplayDurabilityTo(from);

            base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack) || Parent == from)
                HarvestSystem.BeginHarvesting(from, this);
            else
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            AddContextMenuEntries(from, this, list, HarvestSystem);
        }

        public static void AddContextMenuEntries(Mobile from, Item item, ArrayList list, HarvestSystem system)
        {
            if (system != Mining.System)
                return;

            if (!item.IsChildOf(from.Backpack) && item.Parent != from)
                return;

            PlayerMobile pm = from as PlayerMobile;

            if (pm == null)
                return;

            ContextMenuEntry miningEntry = new ContextMenuEntry(pm.ToggleMiningStone ? 6179 : 6178);
            miningEntry.Color = 0x421F;
            list.Add(miningEntry);

            list.Add(new ToggleMiningStoneEntry(pm, false, 6176));
            list.Add(new ToggleMiningStoneEntry(pm, true, 6177));
        }

        private class ToggleMiningStoneEntry : ContextMenuEntry
        {
            private PlayerMobile m_Mobile;
            private bool m_Value;

            public ToggleMiningStoneEntry(PlayerMobile mobile, bool value, int number)
                : base(number)
            {
                m_Mobile = mobile;
                m_Value = value;

                bool stoneMining = (mobile.StoneMining && mobile.Skills[SkillName.Mining].Base >= 100.0);

                if (mobile.ToggleMiningStone == value || (value && !stoneMining))
                    this.Flags |= CMEFlags.Disabled;
            }

            public override void OnClick()
            {
                bool oldValue = m_Mobile.ToggleMiningStone;

                if (m_Value)
                {
                    if (oldValue)
                    {
                        m_Mobile.SendLocalizedMessage(1054023); // You are already set to mine both ore and stone!
                    }
                    else if (!m_Mobile.StoneMining || m_Mobile.Skills[SkillName.Mining].Base < 100.0)
                    {
                        m_Mobile.SendLocalizedMessage(1054024); // You have not learned how to mine stone or you do not have enough skill!
                    }
                    else
                    {
                        m_Mobile.ToggleMiningStone = true;
                        m_Mobile.SendLocalizedMessage(1054022); // You are now set to mine both ore and stone.
                    }
                }
                else
                {
                    if (oldValue)
                    {
                        m_Mobile.ToggleMiningStone = false;
                        m_Mobile.SendLocalizedMessage(1054020); // You are now set to mine only ore.
                    }
                    else
                    {
                        m_Mobile.SendLocalizedMessage(1054021); // You are already set to mine only ore!
                    }
                }
            }
        }

        public BaseHarvestTool(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3
            writer.Write((int)m_Resource);

            // version 1
            m_Crafter.Serialize(writer);
            writer.Write((int)m_Quality);

            // version 0
            writer.Write((int)m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_Resource = (CraftResource)reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                case 1:
                    {
                        if (version >= 2)
                            m_Crafter.Deserialize(reader);
                        else
                            m_Crafter = reader.ReadMobile();

                        m_Quality = (ToolQuality)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_UsesRemaining = reader.ReadInt();
                        break;
                    }
            }
        }

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            Quality = (ToolQuality)quality;

            if (makersMark)
                Crafter = from;

            Type resourceType = typeRes;

            if (resourceType == null)
                resourceType = craftItem.Resources.GetAt(0).ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            CraftContext context = craftSystem.GetContext(from);

            if (context != null && context.DoNotColor)
                Hue = 0;

            return quality;
        }

        #endregion
    }
}