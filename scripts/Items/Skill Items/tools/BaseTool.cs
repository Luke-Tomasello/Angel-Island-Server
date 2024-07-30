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

/* Items/SkillItems/Tools/BaseTool.cs
 * CHANGELOG:
 *  5/10/23, Yoar
 *      Tools now display quality, crafter on SingleClick
 *  5/1/22, Yoar
 *      Added m_Resource field.
 *      Tool uses are now scaled by resource (similar to how armor durability is scaled by resource).
 *  04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Craft;
using Server.Engines.EventResources;
using Server.Engines.OldSchoolCraft;
using Server.Network;
using System;

namespace Server.Items
{
    public enum ToolQuality
    {
        Low,
        Regular,
        Exceptional
    }

    public abstract class BaseTool : Item, IUsesRemaining, ICraftable
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

        public abstract CraftSystem CraftSystem { get; }

        public BaseTool(int itemID)
            : this(50, itemID)
        {
        }

        public BaseTool(int uses, int itemID)
            : base(itemID)
        {
            m_UsesRemaining = uses;
            m_Quality = ToolQuality.Regular;
        }

        public BaseTool(Serial serial)
            : base(serial)
        {
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

        public override string GetOldPrefix(ref Article article)
        {
            string prefix = "";

            if (m_Quality == ToolQuality.Exceptional)
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.An;

                prefix += "exceptional ";
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

        public static bool CheckAccessible(Item tool, Mobile m)
        {
            return (tool.IsChildOf(m) || tool.Parent == m);
        }

        public static bool CheckTool(Item tool, Mobile m)
        {
            Item check = m.FindItemOnLayer(Layer.OneHanded);

            if (check is BaseTool && check != tool)
                return false;

            check = m.FindItemOnLayer(Layer.TwoHanded);

            if (check is BaseTool && check != tool)
                return false;

            return true;
        }

        public static bool ToolsDisplayDurability
        {
            get
            {
                // we would like a date here but we do not know when Durability display appeared.
                //	for now we will just condition on SP
                return (!Core.RuleSets.SiegeStyleRules());
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (ToolsDisplayDurability)
                DisplayDurabilityTo(from);

            base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            CraftSystem system = this.CraftSystem;

            foreach (EventResourceSystem ers in EventResourceSystem.Systems)
            {
                CraftSystem mutateSystem = ers.MutateCraft(from, this);

                if (mutateSystem != null)
                {
                    system = mutateSystem;
                    break;
                }
            }

            if (!system.Enabled)
                return;

            if (IsChildOf(from.Backpack) || Parent == from)
            {
                TextDefinition badCraft = system.CanCraft(from, this, null);

                if (!TextDefinition.IsNullOrEmpty(badCraft))
                {
                    TextDefinition.SendMessageTo(from, badCraft);
                }
                else
                {
                    // What good is this? Just to ensure creation?
                    CraftContext context = system.GetContext(from);

                    // add UOSP old school craft system hook here
                    // 10/31/22, Adam: incomplete, and probably not worth the bother as it's very
                    //  clunky and difficult to use. The consideration here is quality of life, and having
                    //  the old-school craft system just isn't worth it.
                    if (false /*Core.RuleSets.SiegeStyleRules()*/)
                    {
                        if (new OldSchoolCraft(from, system, this, null).DoOldSchoolCraft() == false)
                            from.SendGump(new CraftGump(from, system, this, null)); // call the old system as the old-school system is not impl
                    }
                    else
                        from.SendGump(new CraftGump(from, system, this, null));
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
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