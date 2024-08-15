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

/* Scripts/Items/Misc/BaseCraftableItem.cs
 * CHANGELOG
 * 04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 * 11/14/21, Yoar
 *		Initial version.
 */

using Server.Engines.Craft;
using Server.Engines.EventResources;
using System;

namespace Server.Items
{
    public enum CraftQuality
    {
        Low,
        Regular,
        Exceptional
    }

    public abstract class BaseCraftableItem : Item, ICraftable
    {
        private MakersMark m_Crafter;
        private CraftQuality m_Quality;
        private CraftResource m_Resource;

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
            set { if (m_Resource != value) { m_Resource = value; Hue = CraftResources.GetHue(value); InvalidateProperties(); } }
        }

        public BaseCraftableItem(int itemId)
            : base(itemId)
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

            if (EventResourceSystem.Find(m_Resource) != null)
            {
                CraftResourceInfo info = CraftResources.GetInfo(m_Resource);

                if (info != null)
                {
                    if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                        article = info.Article;

                    prefix += string.Concat(info.Name.ToLower(), " ");
                }
            }

            return prefix;
        }

        public override string GetOldSuffix()
        {
            string suffix = "";

            if (m_Crafter != null)
                suffix += " crafted by " + m_Crafter.Name;

            return suffix;
        }

        public BaseCraftableItem(Serial serial)
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

            byte version;

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

            PatchResourceHue(this, m_Resource);
        }

        public static void PatchResourceHue(Item item, CraftResource itemResource)
        {
            // revert back to OSI coloring
            if (itemResource == CraftResource.Bloodwood && item.Hue == 0x612)
                item.Hue = 0x4AA;
            else if (itemResource == CraftResource.Frostwood && item.Hue == 0xB8F)
                item.Hue = 0x47F;
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
}