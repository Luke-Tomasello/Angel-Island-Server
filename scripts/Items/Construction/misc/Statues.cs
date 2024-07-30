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

/* Items/Construction/Misc/Stautes.cs
 * ChangeLog:
 *  10/18/23, Yoar
 *      Added BaseStatue base class
 *      Merged dual flipped types into one type
 *      Now implements ICraftable
 *      Now implements IMagicItem
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Craft;
using Server.Engines.EventResources;
using System;

namespace Server.Items
{
    public abstract class BaseStatue : Item, ICraftable, IMagicItem
    {
        public override double DefaultWeight { get { return 10.0; } }

        private MakersMark m_Crafter;
        private CraftQuality m_Quality;
        private CraftResource m_Resource;

        private bool m_Identified;
        private MagicItemEffect m_MagicEffect = MagicItemEffect.None;
        private int m_MagicCharges;

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
            get { return m_Resource; }
            set
            {
                if (m_Resource != value)
                {
                    EventResourceSystem.CheckRegistry(this, false);

                    m_Resource = value;

                    Hue = CraftResources.GetHue(value);

                    InvalidateProperties();

                    EventResourceSystem.CheckRegistry(this, true);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Identified
        {
            get { return m_Identified; }
            set { m_Identified = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MagicItemEffect MagicEffect
        {
            get { return m_MagicEffect; }
            set { m_MagicEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MagicCharges
        {
            get { return m_MagicCharges; }
            set { m_MagicCharges = value; InvalidateProperties(); }
        }

        public BaseStatue(int itemID)
            : base(itemID)
        {
        }

        public override string GetOldPrefix(ref Article article)
        {
            string prefix = "";

            if (m_Quality == CraftQuality.Exceptional)
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.An;

                prefix += "exceptional ";
            }

            if (m_Identified)
            {
                // add identifyable prefixes here
            }
            else if (m_MagicEffect != MagicItemEffect.None)
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.A;

                prefix += "magic ";
            }

            if (EventResourceSystem.Find(m_Resource) != null)
            {
                CraftResourceInfo info = CraftResources.GetInfo(m_Resource);

                if (info != null)
                {
                    if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                        article = info.Article;

                    prefix += String.Concat(info.Name.ToLower(), " ");
                }
            }

            return prefix;
        }

        public override string GetOldSuffix()
        {
            string suffix = "";

            if (m_Identified)
            {
                if (m_MagicEffect != MagicItemEffect.None)
                {
                    if (suffix.Length == 0)
                        suffix += " of ";
                    else
                        suffix += " and ";

                    suffix += MagicItems.GetOldSuffix(m_MagicEffect, m_MagicCharges);
                }
            }

            if (m_Crafter != null)
                suffix += " crafted by " + m_Crafter.Name;

            return suffix;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_MagicEffect != MagicItemEffect.None)
                MagicItems.OnUse(from, this, true);
        }

        public override void OnAfterDelete()
        {
            EventResourceSystem.CheckRegistry(this, false);
        }

        public BaseStatue(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x81); // version

            writer.Write((bool)m_Identified);
            writer.Write((sbyte)m_MagicEffect);
            writer.Write((int)m_MagicCharges);

            m_Crafter.Serialize(writer);
            writer.WriteEncodedInt((int)m_Quality);
            writer.WriteEncodedInt((int)m_Resource);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            if ((Utility.PeekByte(reader) & 0x80) == 0)
                return; // old version

            byte version = reader.ReadByte();

            switch (version)
            {
                case 0x81:
                    {
                        m_Identified = reader.ReadBool();
                        m_MagicEffect = (MagicItemEffect)reader.ReadSByte();
                        m_MagicCharges = reader.ReadInt();

                        goto case 0x80;
                    }
                case 0x80:
                    {
                        m_Crafter.Deserialize(reader);
                        m_Quality = (CraftQuality)reader.ReadEncodedInt();
                        m_Resource = (CraftResource)reader.ReadEncodedInt();

                        break;
                    }
            }

            EventResourceSystem.CheckRegistry(this, true);
        }

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
#if MARKABLE
            m_Quality = (CraftQuality)quality;

            if (makersMark)
                m_Crafter = from;
#endif

            Type resourceType = typeRes;

            if (resourceType == null)
                resourceType = craftItem.Resources.GetAt(0).ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            CraftContext context = craftSystem.GetContext(from);

            if (context != null && context.DoNotColor)
                Hue = 0;

#if MARKABLE
            return quality;
#else
            return 1;
#endif
        }

        #endregion
    }

    [TypeAlias("Server.Items.StatueEast2", "Server.Items.StatueSouth")]
    [Flipable(0x1224, 0x139A)]
    public class Statue1 : BaseStatue
    {
        [Constructable]
        public Statue1()
            : base(0x139A)
        {
        }

        public Statue1(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [TypeAlias("Server.Items.StatueSouthEast")]
    public class Statue2 : BaseStatue
    {
        [Constructable]
        public Statue2()
            : base(0x1225)
        {
        }

        public Statue2(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [TypeAlias("Server.Items.StatueNorth", "Server.Items.StatueWest")]
    [Flipable(0x1226, 0x139B)]
    public class Statue3 : BaseStatue
    {
        [Constructable]
        public Statue3()
            : base(0x139B)
        {
        }

        public Statue3(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [TypeAlias("Server.Items.StatueEast", "Server.Items.StatueSouth2")]
    [Flipable(0x1227, 0x139C)]
    public class Statue4 : BaseStatue
    {
        [Constructable]
        public Statue4()
            : base(0x139C)
        {
        }

        public Statue4(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [TypeAlias("Server.Items.BustEast", "Server.Items.BustSouth")]
    [Flipable(0x12CA, 0x12CB)]
    public class Bust : BaseStatue
    {
        [Constructable]
        public Bust()
            : base(0x12CB)
        {
        }

        public Bust(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [TypeAlias("Server.Items.StatuePegasus2")]
    [Flipable(0x1228, 0x139D)]
    public class StatuePegasus : BaseStatue
    {
        [Constructable]
        public StatuePegasus()
            : base(0x139D)
        {
        }

        public StatuePegasus(Serial serial)
            : base(serial)
        {
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}