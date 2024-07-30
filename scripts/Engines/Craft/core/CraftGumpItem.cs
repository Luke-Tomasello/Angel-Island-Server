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

/* Scripts/Engines/Craft/Core/CraftGumpItem.cs
 * CHANGELOG
 *  1/8/22, Yoar
 *      Disabled runebook special case. We use CraftResource.Predicate now.
 *  12/30/21, Yoar
 *      Added CraftItem.Picture. This is used to draw addons in the CraftGumpItem display.
 *      Whatever is drawn in the display is now correctly centered.
 *  11/22/21, Yoar
 *      Added CraftItem.ItemID, CraftItem.ItemHue: Sets the item display in the craft gump.
 *  11/17/21, Yoar
 *      Converted labeled text into html text.
 *  08/31/05 Taran Kain
 *		DrawItem(): Added Exceptional Chance display for bolas.
 *	8/18/05, erlein
 *		DrawItem(): Added Exceptional Chance display for jewellery.
 *	8/1/05, erlein
 *		DrawItem(): Added Exceptional Chance display for Runebooks.
 *	8/12/04, mith
 *		DrawItem(): Added Exceptional Chance display for Tools.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.EventResources;
using Server.Gumps;
using Server.Items;
using Server.Network;
using System;

namespace Server.Engines.Craft
{
    public class CraftGumpItem : Gump
    {
        private Mobile m_From;
        private CraftSystem m_CraftSystem;
        private CraftItem m_CraftItem;
        private BaseTool m_Tool;

        private const int LabelHue = 0x480; // 0x384
        private const int LabelColor = 0x7FFF;
        private const int FontColor = 0xFFFFFF;

        private int m_OtherCount;

        public CraftGumpItem(Mobile from, CraftSystem craftSystem, CraftItem craftItem, BaseTool tool)
            : base(40, 40)
        {
            m_From = from;
            m_CraftSystem = craftSystem;
            m_CraftItem = craftItem;
            m_Tool = tool;

            from.CloseGump(typeof(CraftGump));
            from.CloseGump(typeof(CraftGumpItem));

            AddPage(0);
            AddBackground(0, 0, 530, 417, 5054);
            AddImageTiled(10, 10, 510, 22, 2624);
            AddImageTiled(10, 37, 150, 148, 2624);
            AddImageTiled(165, 37, 355, 90, 2624);
            AddImageTiled(10, 190, 155, 22, 2624);
            AddImageTiled(10, 217, 150, 53, 2624);
            AddImageTiled(165, 132, 355, 80, 2624);
            AddImageTiled(10, 275, 155, 22, 2624);
            AddImageTiled(10, 302, 150, 53, 2624);
            AddImageTiled(165, 217, 355, 80, 2624);
            AddImageTiled(10, 360, 155, 22, 2624);
            AddImageTiled(165, 302, 355, 80, 2624);
            AddImageTiled(10, 387, 510, 22, 2624);
            AddAlphaRegion(10, 10, 510, 399);

            AddHtmlLocalized(170, 40, 150, 20, 1044053, LabelColor, false, false); // ITEM
            AddHtmlLocalized(10, 192, 150, 22, 1044054, LabelColor, false, false); // <CENTER>SKILLS</CENTER>
            AddHtmlLocalized(10, 277, 150, 22, 1044055, LabelColor, false, false); // <CENTER>MATERIALS</CENTER>
            AddHtmlLocalized(10, 362, 150, 22, 1044056, LabelColor, false, false); // <CENTER>OTHER</CENTER>

            TextDefinition.AddHtmlText(this, 10, 12, 510, 20, craftSystem.GumpTitle, false, false, LabelColor, FontColor);

            AddButton(15, 387, 4014, 4016, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(50, 390, 150, 18, 1044150, LabelColor, false, false); // BACK

            AddButton(270, 387, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(305, 390, 150, 18, 1044151, LabelColor, false, false); // MAKE NOW

            TextDefinition.AddHtmlText(this, 330, 40, 180, 18, craftItem.Name, false, false, LabelColor, FontColor);

            if (craftItem.UseAllRes)
                AddHtmlLocalized(170, 302 + (m_OtherCount++ * 20), 310, 18, 1048176, LabelColor, false, false); // Makes as many as possible at once

            DrawItem();
            DrawSkill();
            DrawRessource();

            if (craftSystem == Township.DefTownshipCraft.CraftSystem && Township.DefTownshipCraft.RequiresMasonry(craftItem))
                AddHtml(170, 302 + (m_OtherCount++ * 20), 310, 18, Color("Requires stonecraft"), false, false);
        }

        private bool m_ShowExceptionalChance;

        public void DrawItem()
        {
            Type type = m_CraftItem.ItemType;

            if (m_CraftItem.Picture != null)
            {
                m_CraftItem.Picture.CompileCentered(this, 10, 37, 150, 148);
            }
            else if (m_CraftItem.ItemID != 0)
            {
                Rectangle2D bounds = ItemBounds.Table[m_CraftItem.ItemID & 0x3FFF];

                int dx = (150 - bounds.Start.X - bounds.End.X) / 2;
                int dy = (148 - bounds.Start.Y - bounds.End.Y) / 2;

                AddItem(10 + dx, 37 + dy, m_CraftItem.ItemID, m_CraftItem.ItemHue);
            }

            if (m_CraftItem.IsMarkable(type))
            {
                AddHtmlLocalized(170, 302 + (m_OtherCount++ * 20), 310, 18, 1044059, LabelColor, false, false); // This item may hold its maker's mark
                m_ShowExceptionalChance = true;
            }
        }

        public void DrawSkill()
        {
            for (int i = 0; i < m_CraftItem.Skills.Count; i++)
            {
                CraftSkill skill = m_CraftItem.Skills.GetAt(i);
                double minSkill = skill.MinSkill, maxSkill = skill.MaxSkill;

                if (minSkill < 0)
                    minSkill = 0;

                AddHtmlLocalized(170, 132 + (i * 20), 200, 18, 1044060 + (int)skill.SkillToMake, LabelColor, false, false);
                AddLabel(430, 132 + (i * 20), LabelHue, String.Format("{0:F1}", minSkill));
            }

            CraftSubResCol res = (m_CraftItem.UseSubRes2 ? m_CraftSystem.CraftSubRes2 : m_CraftSystem.CraftSubRes);
            int resIndex = -1;

            CraftContext context = m_CraftSystem.GetContext(m_From);

            if (context != null)
                resIndex = (m_CraftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex);

            bool allRequiredSkills = true;
            double chance = m_CraftItem.GetSuccessChance(m_From, resIndex > -1 ? res.GetAt(resIndex).ItemType : null, m_CraftSystem, false, ref allRequiredSkills, m_Tool);

            double excepChance = m_CraftItem.GetExceptionalChance(m_CraftSystem, chance, m_From, m_Tool);

            if (chance < 0.0)
                chance = 0.0;
            else if (chance > 1.0)
                chance = 1.0;

            if (excepChance < 0.0)
                excepChance = 0.0;
            else if (excepChance > 1.0)
                excepChance = 1.0;

            AddHtmlLocalized(170, 80, 250, 18, 1044057, LabelColor, false, false); // Success Chance:
            AddLabel(430, 80, LabelHue, String.Format("{0:F1}%", chance * 100));

            if (m_ShowExceptionalChance)
            {
                AddHtmlLocalized(170, 100, 250, 18, 1044058, 32767, false, false); // Exceptional Chance:
                AddLabel(430, 100, LabelHue, String.Format("{0:F1}%", excepChance * 100));
            }

            EnchantedScroll enchantedScroll = EnchantedScroll.Find(m_From, m_CraftItem.ItemType);

            if (m_CraftSystem.SupportsEnchantedScrolls && enchantedScroll != null)
            {
                double imbueChance = enchantedScroll.GetImbueChance(m_From, m_CraftSystem.MainSkill);

                AddHtml(170, 60, 250, 18, "<BASEFONT COLOR=#FFFFFF>Imbue Chance:</BASEFONT>", false, false);
                AddLabel(430, 60, LabelHue, String.Format("{0:F1}%", imbueChance * 100));
            }
            else
            {
                EventResourceSystem ecs = EventResourceSystem.Find(EventCraftAttribute.Find(m_CraftSystem.GetType()));

                if (ecs != null)
                {
                    double imbueChance = ecs.GetImbueChance(m_CraftItem.ItemType);

                    AddHtml(170, 60, 250, 18, "<BASEFONT COLOR=#FFFFFF>Imbue Chance:</BASEFONT>", false, false);
                    AddLabel(430, 60, LabelHue, String.Format("{0:F1}%", imbueChance * 100));
                }
            }
        }

        public void DrawRessource()
        {
            bool retainedColor = false;

            CraftContext context = m_CraftSystem.GetContext(m_From);

            CraftSubResCol res = (m_CraftItem.UseSubRes2 ? m_CraftSystem.CraftSubRes2 : m_CraftSystem.CraftSubRes);
            int resIndex = -1;

            if (context != null)
                resIndex = (m_CraftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex);

            for (int i = 0; i < m_CraftItem.Resources.Count && i < 4; i++)
            {
                Type type;
                TextDefinition name;

                CraftRes craftResource = m_CraftItem.Resources.GetAt(i);

                type = craftResource.ItemType;
                name = craftResource.Name;

                // Resource Mutation
                if (type == res.ResType && resIndex > -1)
                {
                    CraftSubRes subResource = res.GetAt(resIndex);

                    type = subResource.ItemType;
                    name = subResource.GenericName;

                    if (TextDefinition.IsNullOrEmpty(name))
                        name = subResource.Name;
                }
                // ******************

                if (!retainedColor && m_CraftItem.RetainsColorFrom(m_CraftSystem, type))
                {
                    retainedColor = true;
                    AddHtmlLocalized(170, 302 + (m_OtherCount++ * 20), 310, 18, 1044152, LabelColor, false, false); // * The item retains the color of this material
                    AddLabel(500, 219 + (i * 20), LabelHue, "*");
                }

                TextDefinition.AddHtmlText(this, 170, 219 + (i * 20), 310, 18, name, false, false, LabelColor, FontColor);

                AddLabel(430, 219 + (i * 20), LabelHue, craftResource.Amount.ToString());
            }

#if RunUO
            if (m_CraftItem.NameNumber == 1041267) // runebook
            {
                AddHtmlLocalized(170, 219 + (m_CraftItem.Resources.Count * 20), 310, 18, 1044447, LabelColor, false, false);
                AddLabel(430, 219 + (m_CraftItem.Resources.Count * 20), LabelHue, "1");
            }
#endif
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            // Back Button
            if (info.ButtonID == 0)
            {
                CraftGump craftGump = new CraftGump(m_From, m_CraftSystem, m_Tool, null);
                m_From.SendGump(craftGump);
            }
            else // Make Button
            {
                TextDefinition badCraft = m_CraftSystem.CanCraft(m_From, m_Tool, m_CraftItem.ItemType);

                if (!TextDefinition.IsNullOrEmpty(badCraft))
                {
                    m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, badCraft));
                }
                else
                {
                    Type type = null;

                    CraftContext context = m_CraftSystem.GetContext(m_From);

                    if (context != null)
                    {
                        CraftSubResCol res = (m_CraftItem.UseSubRes2 ? m_CraftSystem.CraftSubRes2 : m_CraftSystem.CraftSubRes);
                        int resIndex = (m_CraftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex);

                        if (resIndex > -1)
                            type = res.GetAt(resIndex).ItemType;
                    }

                    m_CraftSystem.CreateItem(m_From, m_CraftItem.ItemType, type, m_Tool, m_CraftItem);
                }
            }
        }
    }
}