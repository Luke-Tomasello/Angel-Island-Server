
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

/* Scripts/Gumps/CreatureTuningGump.cs
 * Changelog:
 *  3/4/2026, Adam
 *      First time checkin
 *
 * Notes:
 *  GM tool for shard-specific creature balancing.
 *  Allows live tuning of creature stats, behaviors, and skills.
 *  Save writes overrides to: Data/<Core.Server>/<CreatureType>.json
 *
 *  On creature creation, BaseCreature schedules a one-shot timer that
 *  calls ApplyMobileOverrides(), which loads and applies these values.
 *
 *  This allows shard calibration (e.g., Mortalis population tuning)
 *  without modifying creature source code.
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Server.Scripts.Gumps
{
    public class CreatureTuningGump : Gump
    {
        private Mobile m_From;
        private BaseCreature m_Target;

        private int m_Page;
        private SkillsGumpGroup? m_Selected;

        // Button IDs
        private const int ButtonSave = 100;
        private const int ButtonGoSkills = 101;
        private const int ButtonGoTuning = 102;

        // Skills button ID base (to avoid collisions with tuning IDs)
        private const int SkillsButtonBase = 1000;

        public CreatureTuningGump(Mobile from, BaseCreature target)
            : this(from, target, 0, null)
        {
        }

        public CreatureTuningGump(Mobile from, BaseCreature target, int page, SkillsGumpGroup? selected)
            : base(SkillsGump.GumpOffsetX, SkillsGump.GumpOffsetY)
        {
            m_From = from;
            m_Target = target;
            m_Page = page;
            m_Selected = selected;

            AddPage(0);

            if (m_Page == 0)
                BuildTuningPage();
            else
                BuildSkillsPage();
        }

        // ----------------------------
        // Page 0: Tuning
        // ----------------------------

        private void BuildTuningPage()
        {
            // Grow gump to fit new rows
            AddBackground(0, 0, 420, 430, SkillsGump.BackGumpID);

            int y = 20;

            AddLabel(20, y, SkillsGump.TextHue, "Stats");
            y += 25;

            AddRow("Str", m_Target.Str.ToString(), 1, y); y += 25;
            AddRow("Dex", m_Target.Dex.ToString(), 2, y); y += 25;
            AddRow("Int", m_Target.Int.ToString(), 3, y); y += 25;

            // New: Hits, VirtualArmor, Damage
            AddRow("Hits", m_Target.HitsMax.ToString(), 4, y); y += 25;
            AddRow("VirtualArmor", m_Target.VirtualArmor.ToString(), 5, y); y += 25;
            AddRow("Damage", GetDamageText(m_Target), 6, y); y += 25;

            y += 10;
            AddLabel(20, y, SkillsGump.TextHue, "Behavior");
            y += 25;

            AddRow("UsesHumanWeapons", m_Target.UsesHumanWeapons.ToString(), 7, y); y += 25;
            AddRow("UsesBandages", m_Target.UsesBandages.ToString(), 8, y); y += 25;
            AddRow("UsesPotions", m_Target.UsesPotions.ToString(), 9, y); y += 25;
            AddRow("CanRun", m_Target.CanRun.ToString(), 10, y); y += 25;
            AddRow("CanReveal", m_Target.CanReveal.ToString(), 11, y); y += 25;

            // Bottom row buttons (moved down)
            int by = 390;

            AddButton(40, by, 4005, 4007, ButtonGoSkills, GumpButtonType.Reply, 0);
            AddLabel(75, by, SkillsGump.TextHue, "Skills");

            AddButton(160, by, 4005, 4007, ButtonSave, GumpButtonType.Reply, 0);
            AddLabel(195, by, SkillsGump.TextHue, "Save");

            AddButton(280, by, 4017, 4019, 0, GumpButtonType.Reply, 0);
            AddLabel(315, by, SkillsGump.TextHue, "Close");
        }

        private static string GetDamageText(BaseCreature bc)
        {
            return bc.DamageMax.ToString();
        }

        private void AddRow(string name, string value, int buttonID, int y)
        {
            AddLabel(40, y, SkillsGump.TextHue, name);
            AddLabel(200, y, SkillsGump.TextHue, value);
            AddButton(300, y, SkillsGump.SetButtonID1, SkillsGump.SetButtonID2, buttonID, GumpButtonType.Reply, 0);
        }

        // ----------------------------
        // Page 1: Skills (embedded SkillsGump)
        // ----------------------------

        private static bool OldStyle = SkillsGump.OldStyle;

        private const int TotalWidth = SkillsGump.OffsetSize + 107 + SkillsGump.OffsetSize + 128 + SkillsGump.OffsetSize + SkillsGump.SetWidth + SkillsGump.OffsetSize;
        private const int IndentWidth = 12;

        private void BuildSkillsPage()
        {
            SkillsGumpGroup[] groups = SkillsGumpGroup.Groups;

            int count = groups.Length;
            if (m_Selected != null)
                count += m_Selected.Skills.Length;

            int totalHeight = SkillsGump.OffsetSize + ((SkillsGump.EntryHeight + SkillsGump.OffsetSize) * (count + 1));

            // Add extra space at bottom for our "tab" buttons.
            const int extraBottom = 40;

            int backWidth = SkillsGump.BorderSize + TotalWidth + SkillsGump.BorderSize;
            int backHeight = SkillsGump.BorderSize + totalHeight + SkillsGump.BorderSize + extraBottom;

            AddBackground(0, 0, backWidth, backHeight, SkillsGump.BackGumpID);
            AddImageTiled(SkillsGump.BorderSize, SkillsGump.BorderSize, TotalWidth - (OldStyle ? SkillsGump.SetWidth + SkillsGump.OffsetSize : 0), totalHeight, SkillsGump.OffsetGumpID);

            int x = SkillsGump.BorderSize + SkillsGump.OffsetSize;
            int y = SkillsGump.BorderSize + SkillsGump.OffsetSize;

            int emptyWidth = TotalWidth - SkillsGump.PrevWidth - SkillsGump.NextWidth - (SkillsGump.OffsetSize * 4) - (OldStyle ? SkillsGump.SetWidth + SkillsGump.OffsetSize : 0);

            if (OldStyle)
                AddImageTiled(x, y, TotalWidth - (SkillsGump.OffsetSize * 3) - SkillsGump.SetWidth, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);
            else
                AddImageTiled(x, y, SkillsGump.PrevWidth, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);

            x += SkillsGump.PrevWidth + SkillsGump.OffsetSize;

            if (!OldStyle)
                AddImageTiled(x - (OldStyle ? SkillsGump.OffsetSize : 0), y, emptyWidth + (OldStyle ? SkillsGump.OffsetSize * 2 : 0), SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);

            x += emptyWidth + SkillsGump.OffsetSize;

            if (!OldStyle)
                AddImageTiled(x, y, SkillsGump.NextWidth, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);

            for (int i = 0; i < groups.Length; ++i)
            {
                x = SkillsGump.BorderSize + SkillsGump.OffsetSize;
                y += SkillsGump.EntryHeight + SkillsGump.OffsetSize;

                SkillsGumpGroup group = groups[i];

                AddImageTiled(x, y, SkillsGump.PrevWidth, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);

                if (group == m_Selected)
                    AddButton(x + SkillsGump.PrevOffsetX, y + SkillsGump.PrevOffsetY, 0x15E2, 0x15E6, GetSkillButtonID(0, i), GumpButtonType.Reply, 0);
                else
                    AddButton(x + SkillsGump.PrevOffsetX, y + SkillsGump.PrevOffsetY, 0x15E1, 0x15E5, GetSkillButtonID(0, i), GumpButtonType.Reply, 0);

                x += SkillsGump.PrevWidth + SkillsGump.OffsetSize;
                x -= (OldStyle ? SkillsGump.OffsetSize : 0);

                AddImageTiled(x, y, emptyWidth + (OldStyle ? SkillsGump.OffsetSize * 2 : 0), SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
                AddLabel(x + SkillsGump.TextOffsetX, y, SkillsGump.TextHue, group.Name);

                x += emptyWidth + (OldStyle ? SkillsGump.OffsetSize * 2 : 0);
                x += SkillsGump.OffsetSize;

                if (SkillsGump.SetGumpID != 0)
                    AddImageTiled(x, y, SkillsGump.SetWidth, SkillsGump.EntryHeight, SkillsGump.SetGumpID);

                if (group == m_Selected)
                {
                    int indentMaskX = SkillsGump.BorderSize;
                    int indentMaskY = y + SkillsGump.EntryHeight + SkillsGump.OffsetSize;

                    for (int j = 0; j < group.Skills.Length; ++j)
                    {
                        Skill sk = m_Target.Skills[group.Skills[j]];

                        x = SkillsGump.BorderSize + SkillsGump.OffsetSize;
                        y += SkillsGump.EntryHeight + SkillsGump.OffsetSize;

                        x += SkillsGump.OffsetSize;
                        x += IndentWidth;

                        AddImageTiled(x, y, SkillsGump.PrevWidth, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);
                        AddButton(x + SkillsGump.PrevOffsetX, y + SkillsGump.PrevOffsetY, 0x15E1, 0x15E5, GetSkillButtonID(1, j), GumpButtonType.Reply, 0);

                        x += SkillsGump.PrevWidth + SkillsGump.OffsetSize;
                        x -= (OldStyle ? SkillsGump.OffsetSize : 0);

                        AddImageTiled(x, y, emptyWidth + (OldStyle ? SkillsGump.OffsetSize * 2 : 0) - SkillsGump.OffsetSize - IndentWidth, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
                        AddLabel(x + SkillsGump.TextOffsetX, y, SkillsGump.TextHue, sk == null ? "(null)" : sk.Name);

                        x += emptyWidth + (OldStyle ? SkillsGump.OffsetSize * 2 : 0) - SkillsGump.OffsetSize - IndentWidth;
                        x += SkillsGump.OffsetSize;

                        if (SkillsGump.SetGumpID != 0)
                            AddImageTiled(x, y, SkillsGump.SetWidth, SkillsGump.EntryHeight, SkillsGump.SetGumpID);

                        if (sk != null)
                        {
                            int buttonID1, buttonID2;
                            int xOffset, yOffset;

                            switch (sk.Lock)
                            {
                                default:
                                case SkillLock.Up: buttonID1 = 0x983; buttonID2 = 0x983; xOffset = 6; yOffset = 4; break;
                                case SkillLock.Down: buttonID1 = 0x985; buttonID2 = 0x985; xOffset = 6; yOffset = 4; break;
                                case SkillLock.Locked: buttonID1 = 0x82C; buttonID2 = 0x82C; xOffset = 5; yOffset = 2; break;
                            }

                            AddButton(x + xOffset, y + yOffset, buttonID1, buttonID2, GetSkillButtonID(2, j), GumpButtonType.Reply, 0);

                            y += 1;
                            x -= SkillsGump.OffsetSize;
                            x -= 1;
                            x -= 50;

                            AddImageTiled(x, y, 50, SkillsGump.EntryHeight - 2, SkillsGump.OffsetGumpID);

                            x += 1;
                            y += 1;

                            AddImageTiled(x, y, 48, SkillsGump.EntryHeight - 4, SkillsGump.EntryGumpID);
                            AddLabelCropped(x + SkillsGump.TextOffsetX, y - 1, 48 - SkillsGump.TextOffsetX, SkillsGump.EntryHeight - 3, SkillsGump.TextHue, sk.Base.ToString("F1"));

                            y -= 2;
                        }
                    }

                    AddImageTiled(indentMaskX, indentMaskY, IndentWidth + SkillsGump.OffsetSize, (group.Skills.Length * (SkillsGump.EntryHeight + SkillsGump.OffsetSize)) - (i < (groups.Length - 1) ? SkillsGump.OffsetSize : 0), SkillsGump.BackGumpID + 4);
                }
            }

            // Bottom row "tab" buttons
            int by = SkillsGump.BorderSize + totalHeight + 10;

            const int leftPad = 12;
            const int rightPad = 12;

            const int buttonW = 20;      // gump button art is basically ~20px wide
            const int labelOffset = 35;  // your existing label offset
            const int minGap = 6;        // minimum spacing between clusters
            const int minLabelW = 40;    // don't go smaller than this; crop if needed

            int inner = backWidth - leftPad - rightPad;

            // Compute a cluster width that actually fits this page.
            // total = 3*groupW + 2*gap <= inner
            int groupW = (inner - (2 * minGap)) / 3;
            if (groupW < (labelOffset + minLabelW))
                groupW = labelOffset + minLabelW;

            int gap = (inner - (3 * groupW)) / 2;
            if (gap < minGap) gap = minGap;

            // Recompute groupW one last time if the above clamp made us overflow.
            int total = (3 * groupW) + (2 * gap);
            if (total > inner)
            {
                groupW = (inner - (2 * minGap)) / 3;
                gap = minGap;
            }

            // Now we have a guaranteed-fit layout.
            int startX = leftPad + Math.Max(0, (inner - ((3 * groupW) + (2 * gap))) / 2);

            int xTuning = startX;
            int xSave = xTuning + groupW + gap;
            int xClose = xSave + groupW + gap;

            // Label width is whatever is left in the cluster after the offset.
            int labelW = groupW - labelOffset;
            if (labelW < 0) labelW = 0;

            AddButton(xTuning, by, 4005, 4007, ButtonGoTuning, GumpButtonType.Reply, 0);
            AddLabelCropped(xTuning + labelOffset, by, labelW, 20, SkillsGump.TextHue, "Tuning");

            AddButton(xSave, by, 4005, 4007, ButtonSave, GumpButtonType.Reply, 0);
            AddLabelCropped(xSave + labelOffset, by, labelW, 20, SkillsGump.TextHue, "Save");

            AddButton(xClose, by, 4017, 4019, 0, GumpButtonType.Reply, 0);
            AddLabelCropped(xClose + labelOffset, by, labelW, 20, SkillsGump.TextHue, "Close");

        }

        private int GetSkillButtonID(int type, int index)
        {
            // Matches SkillsGump.GetButtonID, but offset to avoid collisions.
            return SkillsButtonBase + (1 + (index * 3) + type);
        }

        private void DecodeSkillButton(int buttonID, out int type, out int index)
        {
            int raw = buttonID - SkillsButtonBase - 1;
            index = raw / 3;
            type = raw % 3;
        }

        // ----------------------------
        // Responses
        // ----------------------------

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            // Tabs
            if (info.ButtonID == ButtonGoSkills)
            {
                m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 1, m_Selected));
                return;
            }

            if (info.ButtonID == ButtonGoTuning)
            {
                m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 0, m_Selected));
                return;
            }

            if (info.ButtonID == ButtonSave)
            {
                SaveConfig();
                m_From.SendGump(new CreatureTuningGump(m_From, m_Target, m_Page, m_Selected));
                return;
            }

            // Skills page interactions
            if (info.ButtonID >= SkillsButtonBase)
            {
                HandleSkillsResponse(info.ButtonID);
                return;
            }

            // Tuning page interactions
            switch (info.ButtonID)
            {
                case 1: Edit("Str"); break;
                case 2: Edit("Dex"); break;
                case 3: Edit("Int"); break;

                case 4: Edit("Hits"); break;
                case 5: Edit("VirtualArmor"); break;
                case 6: Edit("Damage"); break;

                case 7: Edit("UsesHumanWeapons"); break;
                case 8: Edit("UsesBandages"); break;
                case 9: Edit("UsesPotions"); break;
                case 10: Edit("CanRun"); break;
                case 11: Edit("CanReveal"); break;

                default:
                    break;
            }
        }

        private void HandleSkillsResponse(int buttonID)
        {
            SkillsGumpGroup[] groups = SkillsGumpGroup.Groups;

            int type, index;
            DecodeSkillButton(buttonID, out type, out index);

            switch (type)
            {
                case 0:
                    {
                        if (index >= 0 && index < groups.Length)
                        {
                            SkillsGumpGroup newSelection = groups[index];

                            if (m_Selected != newSelection)
                                m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 1, newSelection));
                            else
                                m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 1, null));
                        }

                        break;
                    }
                case 1:
                    {
                        if (m_Selected != null && index >= 0 && index < m_Selected.Skills.Length)
                        {
                            Skill sk = m_Target.Skills[m_Selected.Skills[index]];

                            if (sk != null)
                            {
                                if (m_From.AccessLevel >= AccessLevel.GameMaster)
                                {
                                    m_From.SendGump(new EditSkillForTuningGump(m_From, m_Target, sk, m_Selected));
                                }
                                else
                                {
                                    m_From.SendMessage("You may not change that.");
                                    m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 1, m_Selected));
                                }
                            }
                            else
                            {
                                m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 1, m_Selected));
                            }
                        }

                        break;
                    }
                case 2:
                    {
                        if (m_Selected != null && index >= 0 && index < m_Selected.Skills.Length)
                        {
                            Skill sk = m_Target.Skills[m_Selected.Skills[index]];

                            if (sk != null)
                            {
                                if (m_From.AccessLevel >= AccessLevel.GameMaster)
                                {
                                    switch (sk.Lock)
                                    {
                                        case SkillLock.Up: sk.SetLockNoRelay(SkillLock.Down); sk.Update(); break;
                                        case SkillLock.Down: sk.SetLockNoRelay(SkillLock.Locked); sk.Update(); break;
                                        case SkillLock.Locked: sk.SetLockNoRelay(SkillLock.Up); sk.Update(); break;
                                    }
                                }
                                else
                                {
                                    m_From.SendMessage("You may not change that.");
                                }

                                m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 1, m_Selected));
                            }
                        }

                        break;
                    }
            }
        }

        private void Edit(string field)
        {
            m_From.SendGump(new EditCreatureValueGump(m_From, m_Target, field));
        }

        // ----------------------------
        // Save
        // ----------------------------

        private void SaveConfig()
        {
            try
            {
                string serverName = Core.Server; // e.g. "Mortalis"
                string dir = Path.Combine("Data", serverName);
                string file = m_Target.GetType().Name + ".json";
                string path = Path.Combine(dir, file);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                StringBuilder sb = new StringBuilder(4096);

                sb.Append("{\n");
                sb.Append("  \"Type\": \"").Append(EscapeJson(m_Target.GetType().Name)).Append("\",\n");
                sb.Append("  \"SavedLocal\": \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("\",\n");

                // Stats
                sb.Append("  \"Stats\": {\n");
                sb.Append("    \"Str\": ").Append(m_Target.Str).Append(",\n");
                sb.Append("    \"Dex\": ").Append(m_Target.Dex).Append(",\n");
                sb.Append("    \"Int\": ").Append(m_Target.Int).Append(",\n");
                sb.Append("    \"Hits\": ").Append(m_Target.HitsMax).Append(",\n");
                sb.Append("    \"VirtualArmor\": ").Append(m_Target.VirtualArmor).Append(",\n");
                sb.Append("    \"Damage\": ").Append(m_Target.DamageMin).Append("\n");
                sb.Append("  },\n");

                // Behavior
                sb.Append("  \"Behavior\": {\n");
                sb.Append("    \"UsesHumanWeapons\": ").Append(m_Target.UsesHumanWeapons ? "true" : "false").Append(",\n");
                sb.Append("    \"UsesBandages\": ").Append(m_Target.UsesBandages ? "true" : "false").Append(",\n");
                sb.Append("    \"UsesPotions\": ").Append(m_Target.UsesPotions ? "true" : "false").Append(",\n");
                sb.Append("    \"CanRun\": ").Append(m_Target.CanRun ? "true" : "false").Append(",\n");
                sb.Append("    \"CanReveal\": ").Append(m_Target.CanReveal ? "true" : "false").Append("\n");
                sb.Append("  },\n");

                // Skills (keyed by SkillName enum string for easy load)
                sb.Append("  \"Skills\": {\n");

                bool firstSkill = true;

                foreach (SkillsGumpGroup group in SkillsGumpGroup.Groups)
                {
                    SkillName[] list = group.Skills;

                    for (int i = 0; i < list.Length; i++)
                    {
                        SkillName sn = list[i];
                        Skill sk = m_Target.Skills[sn];

                        if (sk == null)
                            continue;

                        // omit zeros to keep file small
                        if (sk.Base <= 0.0)
                            continue;

                        if (!firstSkill)
                            sb.Append(",\n");

                        firstSkill = false;

                        sb.Append("    \"")
                          .Append(sn.ToString())
                          .Append("\": ")
                          .Append(sk.Base.ToString("F1", CultureInfo.InvariantCulture));
                    }
                }

                sb.Append("\n");
                sb.Append("  }\n");
                sb.Append("}\n");

                File.WriteAllText(path, sb.ToString());

                m_From.SendMessage("Saved: {0}", path);
            }
            catch (Exception ex)
            {
                m_From.SendMessage("Save failed: {0}", ex.Message);
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            StringBuilder sb = new StringBuilder(s.Length + 8);

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;

                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("X4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            return sb.ToString();
        }
    }

    public class EditCreatureValueGump : Gump
    {
        private Mobile m_From;
        private BaseCreature m_Target;
        private string m_Field;

        public EditCreatureValueGump(Mobile from, BaseCreature target, string field)
            : base(SkillsGump.GumpOffsetX, SkillsGump.GumpOffsetY)
        {
            m_From = from;
            m_Target = target;
            m_Field = field;

            AddBackground(0, 0, 220, 120, SkillsGump.BackGumpID);

            AddLabel(20, 20, SkillsGump.TextHue, field);

            AddTextEntry(20, 50, 160, 20, SkillsGump.TextHue, 0, GetValue());

            AddButton(180, 50, SkillsGump.SetButtonID1, SkillsGump.SetButtonID2, 1, GumpButtonType.Reply, 0);
        }

        private string GetValue()
        {
            switch (m_Field)
            {
                case "Str": return m_Target.Str.ToString();
                case "Dex": return m_Target.Dex.ToString();
                case "Int": return m_Target.Int.ToString();

                case "Hits": return m_Target.HitsMax.ToString();
                case "VirtualArmor": return m_Target.VirtualArmor.ToString();
                case "Damage": return m_Target.DamageMax.ToString();

                case "UsesHumanWeapons": return m_Target.UsesHumanWeapons.ToString();
                case "UsesBandages": return m_Target.UsesBandages.ToString();
                case "UsesPotions": return m_Target.UsesPotions.ToString();
                case "CanRun": return m_Target.CanRun.ToString();
                case "CanReveal": return m_Target.CanReveal.ToString();
            }

            return "";
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            TextRelay? text = info.GetTextEntry(0);
            if (text == null)
                return;

            string val = text.Text;

            try
            {
                switch (m_Field)
                {
                    case "Str":
                        {
                            int x = Convert.ToInt32(val);
                            m_Target.SetStr(x);
                            break;
                        }
                    case "Dex":
                        {
                            int x = Convert.ToInt32(val);
                            m_Target.SetDex(x);
                            break;
                        }
                    case "Int":
                        {
                            int x = Convert.ToInt32(val);
                            m_Target.SetInt(x);
                            break;
                        }
                    case "Hits":
                        {
                            int x = Convert.ToInt32(val);
                            m_Target.SetHits(x, false);
                            break;
                        }
                    case "VirtualArmor":
                        {
                            int x = Convert.ToInt32(val);
                            m_Target.VirtualArmor = x;
                            break;
                        }
                    case "Damage":
                        {
                            int n = Convert.ToInt32(val);
                            m_Target.SetDamage(n);
                            break;
                        }
                    case "UsesHumanWeapons":
                        m_Target.UsesHumanWeapons = Convert.ToBoolean(val);
                        break;

                    case "UsesBandages":
                        m_Target.UsesBandages = Convert.ToBoolean(val);
                        break;

                    case "UsesPotions":
                        m_Target.UsesPotions = Convert.ToBoolean(val);
                        break;

                    case "CanRun":
                        m_Target.CanRun = Convert.ToBoolean(val);
                        break;

                    case "CanReveal":
                        m_Target.CanReveal = Convert.ToBoolean(val);
                        break;
                }
            }
            catch
            {
                m_From.SendMessage("Invalid value.");
                m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 0, null));
                return;
            }

            // Always return to tuning page after editing a tuning value.
            m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 0, null));
        }
    }

    // Clone of EditSkillGump that returns to CreatureTuningGump page 1.
    public class EditSkillForTuningGump : Gump
    {
        private Mobile m_From;
        private BaseCreature m_Target;
        private Skill m_Skill;
        private SkillsGumpGroup m_Selected;

        public EditSkillForTuningGump(Mobile from, BaseCreature target, Skill skill, SkillsGumpGroup selected)
            : base(SkillsGump.GumpOffsetX, SkillsGump.GumpOffsetY)
        {
            m_From = from;
            m_Target = target;
            m_Skill = skill;
            m_Selected = selected;

            string initialText = m_Skill.Base.ToString("F1", CultureInfo.InvariantCulture);

            AddPage(0);

            // Match EditSkillGump sizing.
            const int entryWidth = 160;
            int totalWidth = SkillsGump.OffsetSize + entryWidth + SkillsGump.OffsetSize + SkillsGump.SetWidth + SkillsGump.OffsetSize;
            int totalHeight = SkillsGump.OffsetSize + (2 * (SkillsGump.EntryHeight + SkillsGump.OffsetSize));
            int backWidth = SkillsGump.BorderSize + totalWidth + SkillsGump.BorderSize;
            int backHeight = SkillsGump.BorderSize + totalHeight + SkillsGump.BorderSize;

            AddBackground(0, 0, backWidth, backHeight, SkillsGump.BackGumpID);
            AddImageTiled(SkillsGump.BorderSize, SkillsGump.BorderSize, totalWidth - (EditSkillGump.OldStyle ? SkillsGump.SetWidth + SkillsGump.OffsetSize : 0), totalHeight, SkillsGump.OffsetGumpID);

            int x = SkillsGump.BorderSize + SkillsGump.OffsetSize;
            int y = SkillsGump.BorderSize + SkillsGump.OffsetSize;

            AddImageTiled(x, y, entryWidth, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
            AddLabelCropped(x + SkillsGump.TextOffsetX, y, entryWidth - SkillsGump.TextOffsetX, SkillsGump.EntryHeight, SkillsGump.TextHue, skill.Name);
            x += entryWidth + SkillsGump.OffsetSize;

            if (SkillsGump.SetGumpID != 0)
                AddImageTiled(x, y, SkillsGump.SetWidth, SkillsGump.EntryHeight, SkillsGump.SetGumpID);

            x = SkillsGump.BorderSize + SkillsGump.OffsetSize;
            y += SkillsGump.EntryHeight + SkillsGump.OffsetSize;

            AddImageTiled(x, y, entryWidth, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
            AddTextEntry(x + SkillsGump.TextOffsetX, y, entryWidth - SkillsGump.TextOffsetX, SkillsGump.EntryHeight, SkillsGump.TextHue, 0, initialText);
            x += entryWidth + SkillsGump.OffsetSize;

            if (SkillsGump.SetGumpID != 0)
                AddImageTiled(x, y, SkillsGump.SetWidth, SkillsGump.EntryHeight, SkillsGump.SetGumpID);

            AddButton(x + SkillsGump.SetOffsetX, y + SkillsGump.SetOffsetY, SkillsGump.SetButtonID1, SkillsGump.SetButtonID2, 1, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                try
                {
                    if (m_From.AccessLevel >= AccessLevel.GameMaster)
                    {
                        TextRelay? text = info.GetTextEntry(0);

                        if (text != null)
                        {
                            m_Skill.Base = Convert.ToDouble(text.Text);
                            Server.Commands.CommandLogging.LogChangeProperty(m_From, m_Target, string.Format("{0}.Base", m_Skill), m_Skill.Base.ToString());
                        }
                    }
                    else
                    {
                        m_From.SendMessage("You may not change that.");
                    }

                    m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 1, m_Selected));
                }
                catch
                {
                    m_From.SendMessage("Bad format. ###.# expected.");
                    m_From.SendGump(new EditSkillForTuningGump(m_From, m_Target, m_Skill, m_Selected));
                }
            }
            else
            {
                m_From.SendGump(new CreatureTuningGump(m_From, m_Target, 1, m_Selected));
            }
        }
    }
}

