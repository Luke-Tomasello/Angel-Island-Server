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

/* Scripts/Items/Misc/BaseOtherEquipable.cs
/* CHANGELOG
 * 04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 * 11/14/21, Yoar
 *		Initial version.
 */

using Server.Engines.Craft;
using System;

namespace Server.Items
{
    public abstract class BaseOtherEquipable : Item, ICraftable
    {
        private MakersMark m_Crafter;
        private CraftQuality m_Quality;
        private CraftResource m_Resource;

        public virtual bool AllowMaleWearer { get { return true; } }
        public virtual bool AllowFemaleWearer { get { return true; } }

        public virtual int AosStrReq { get { return 0; } }
        public virtual int AosDexReq { get { return 0; } }
        public virtual int AosIntReq { get { return 0; } }

        public virtual int OldStrReq { get { return 0; } }
        public virtual int OldDexReq { get { return 0; } }
        public virtual int OldIntReq { get { return 0; } }

        public virtual int StrRequirement { get { return Core.RuleSets.AOSRules() ? AosStrReq : OldStrReq; } }
        public virtual int DexRequirement { get { return Core.RuleSets.AOSRules() ? AosDexReq : OldDexReq; } }
        public virtual int IntRequirement { get { return Core.RuleSets.AOSRules() ? AosIntReq : OldIntReq; } }

        public virtual CraftResource DefaultResource { get { return CraftResource.RegularWood; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public MakersMark Crafter
        {
            get { return m_Crafter; }
            set { m_Crafter = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftQuality Quality
        {
            get { return m_Quality; }
            set { m_Quality = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get
            {
                return m_Resource;
            }
            set
            {
                if (m_Resource != value)
                {
                    m_Resource = value;
                    Hue = CraftResources.GetHue(m_Resource);
                    InvalidateProperties();
                }
            }
        }

        public int ComputeStatReq(StatType type)
        {
            int v;

            if (type == StatType.Str)
                v = StrRequirement;
            else if (type == StatType.Dex)
                v = DexRequirement;
            else
                v = IntRequirement;

            return v;
        }

        public BaseOtherEquipable(int itemId)
            : base(itemId)
        {
            m_Quality = CraftQuality.Regular;
            Resource = DefaultResource;
        }

        public override bool CheckConflictingLayer(Mobile m, Item item, Layer layer)
        {
            if (base.CheckConflictingLayer(m, item, layer))
                return true;

            if (this.Layer == Layer.TwoHanded && layer == Layer.OneHanded)
            {
                m.SendLocalizedMessage(500214); // You already have something in both hands.
                return true;
            }
            else if (this.Layer == Layer.OneHanded && layer == Layer.TwoHanded && !(item is BaseShield) && !(item is BaseEquipableLight))
            {
                m.SendLocalizedMessage(500215); // You can only wield one weapon at a time.
                return true;
            }

            return false;
        }

        public BaseOtherEquipable(Serial serial)
            : base(serial)
        {
        }
        private const byte mask = 0x80;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x81); // version

            m_Crafter.Serialize(writer);
            writer.WriteEncodedInt((int)m_Quality);
            writer.WriteEncodedInt((int)m_Resource);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version;

            if ((Utility.PeekByte(reader) & mask) == 0)
            {
                version = 0; // old version
            }
            else
            {
                version = reader.ReadByte();

                switch (version)
                {
                    case 0x81:
                    case 0x80:
                        {
                            if (version >= 0x81)
                                m_Crafter.Deserialize(reader);
                            else
                                m_Crafter = reader.ReadMobile();

                            m_Quality = (CraftQuality)reader.ReadEncodedInt();
                            m_Resource = (CraftResource)reader.ReadEncodedInt();

                            break;
                        }
                }
            }

            if (Parent is Mobile)
                ((Mobile)Parent).CheckStatTimers();

            BaseCraftableItem.PatchResourceHue(this, m_Resource);
        }

        public override bool CanEquip(Mobile from)
        {
            if (!Ethics.Ethic.CheckEquip(from, this))
                return false;

            if (from.AccessLevel < AccessLevel.GameMaster)
            {
                if (!AllowMaleWearer && !from.Female)
                {
                    if (AllowFemaleWearer)
                        from.SendLocalizedMessage(1010388); // Only females can wear this.
                    else
                        from.SendMessage("You may not wear this.");

                    return false;
                }
                else if (!AllowFemaleWearer && from.Female)
                {
                    if (AllowMaleWearer)
                        from.SendLocalizedMessage(1063343); // Only males can wear this.
                    else
                        from.SendMessage("You may not wear this.");

                    return false;
                }
                else
                {
                    int strReq = ComputeStatReq(StatType.Str);
                    int dexReq = ComputeStatReq(StatType.Dex);
                    int intReq = ComputeStatReq(StatType.Int);

                    if (from.Dex < dexReq)
                    {
                        from.SendLocalizedMessage(502077); // You do not have enough dexterity to equip this item.
                        return false;
                    }
                    else if (from.Str < strReq)
                    {
                        from.SendLocalizedMessage(500213); // You are not strong enough to equip that.
                        return false;
                    }
                    else if (from.Int < intReq)
                    {
                        from.SendMessage("You are not smart enough to equip that.");
                        return false;
                    }
                }
            }

            return base.CanEquip(from);
        }

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            m_Quality = (CraftQuality)quality;

            if (makersMark)
                m_Crafter = from;

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