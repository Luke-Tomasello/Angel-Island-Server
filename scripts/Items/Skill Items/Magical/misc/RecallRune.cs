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

/* Scripts\Items\Skill Items\Magical\Misc\RecallRune.cs
 * CHANGELOG:
 *  1/19/22, Yoar
 *      The description of recall runes is now automatically set when marking them (if possible)
 *  7/6/08, Adam
 *      Log no friends marking a rune in a house
 *	9/29/05, Adam
 *		While we are one facet, there is no need to display [Felucca]
 */

using Server.Diagnostics;
using Server.Multis;
using Server.Prompts;
using System;
using System.Collections;

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class RecallRune : Item
    {
        private string m_Description;
        private bool m_Marked;
        private Point3D m_Target;
        private Map m_TargetMap;
        private BaseHouse m_House;

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            if (m_House != null && !m_House.Deleted)
            {
                writer.Write((int)1); // version

                writer.Write((Item)m_House);
            }
            else
            {
                writer.Write((int)0); // version
            }

            writer.Write((string)m_Description);
            writer.Write((bool)m_Marked);
            writer.Write((Point3D)m_Target);
            writer.Write((Map)m_TargetMap);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_House = reader.ReadItem() as BaseHouse;
                        goto case 0;
                    }
                case 0:
                    {
                        m_Description = reader.ReadString();
                        m_Marked = reader.ReadBool();
                        m_Target = reader.ReadPoint3D();
                        m_TargetMap = reader.ReadMap();

                        CalculateHue();

                        break;
                    }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public BaseHouse House
        {
            get
            {
                if (m_House != null && m_House.Deleted)
                    House = null;

                return m_House;
            }
            set { m_House = value; CalculateHue(); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public bool Marked
        {
            get
            {
                return m_Marked;
            }
            set
            {
                if (m_Marked != value)
                {
                    m_Marked = value;
                    CalculateHue();
                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Point3D Target
        {
            get
            {
                return m_Target;
            }
            set
            {
                m_Target = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Map TargetMap
        {
            get
            {
                return m_TargetMap;
            }
            set
            {
                if (m_TargetMap != value)
                {
                    m_TargetMap = value;
                    CalculateHue();
                    InvalidateProperties();
                }
            }
        }

        private void CalculateHue()
        {
            if (!m_Marked)
                Hue = 0;
            else if (m_TargetMap == Map.Trammel)
                Hue = (House != null ? 0x47F : 50);
            else if (m_TargetMap == Map.Felucca)
                Hue = (House != null ? 0x66D : 0);
            else if (m_TargetMap == Map.Ilshenar)
                Hue = (House != null ? 0x55F : 1102);
            else if (m_TargetMap == Map.Malas)
                Hue = (House != null ? 0x55F : 1102);
        }

        // log non frineds marking in a house
        public void LogMark(Mobile m)
        {
            // log non frineds marking in a house
            try
            {
                ArrayList regions = Region.FindAll(m.Location, m.Map);
                for (int ix = 0; ix < regions.Count; ix++)
                {
                    if (regions[ix] is Regions.HouseRegion == false)
                        continue;

                    Regions.HouseRegion hr = regions[ix] as Regions.HouseRegion;
                    BaseHouse bh = hr.House;

                    if (bh != null)
                    {
                        if (bh.IsFriend(m) == false)
                        {
                            LogHelper Logger = new LogHelper("mark.log", false, true);
                            Logger.Log(LogType.Mobile, m);
                            Logger.Log(LogType.Item, this);
                            Logger.Finish();
                        }
                    }
                }
            }
            catch (Exception ex) { LogHelper.LogException(ex); }
        }

        public void Mark(Mobile m)
        {
            m_Marked = true;

            // log non frineds marking in a house
            LogMark(m);

            bool setDesc = false;
            if (Core.RuleSets.AOSRules())
            {
                m_House = BaseHouse.FindHouseAt(m);

                if (m_House == null)
                {
                    m_Target = m.Location;
                    m_TargetMap = m.Map;
                }
                else
                {
                    HouseSign sign = m_House.Sign;

                    if (sign != null)
                        m_Description = sign.Name;
                    else
                        m_Description = null;

                    if (m_Description == null || (m_Description = m_Description.Trim()).Length == 0)
                        m_Description = "an unnamed house";

                    setDesc = true;

                    int x = m_House.BanLocation.X;
                    int y = m_House.BanLocation.Y + 2;
                    int z = m_House.BanLocation.Z;

                    Map map = m_House.Map;

                    if (map != null && !Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.requireSurface))
                        z = map.GetAverageZ(x, y);

                    m_Target = new Point3D(x, y, z);
                    m_TargetMap = map;
                }
            }
            else
            {
                m_House = null;
                m_Target = m.Location;
                m_TargetMap = m.Map;
            }

            if (!setDesc)
            {
                Region region = Region.Find(m_Target, m_TargetMap);

                if (region != m_TargetMap.DefaultRegion && region.Name != null && region.Name.Length > 0)
                    m_Description = region.Name;
            }

            CalculateHue();
            InvalidateProperties();
        }

        private const string RuneFormat = "a recall rune for {0}";

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_Marked)
            {
                string desc;

                if ((desc = m_Description) == null || (desc = desc.Trim()).Length == 0)
                    desc = "an unknown location";

                if (m_TargetMap == Map.Malas)
                    list.Add((House != null ? 1062454 : 1060804), RuneFormat, desc); // ~1_val~ (Malas)[(House)]
                else if (m_TargetMap == Map.Felucca)
                    list.Add((House != null ? 1062452 : 1060805), RuneFormat, desc); // ~1_val~ (Felucca)[(House)]
                else if (m_TargetMap == Map.Trammel)
                    list.Add((House != null ? 1062453 : 1060806), RuneFormat, desc); // ~1_val~ (Trammel)[(House)]
                else
                    list.Add((House != null ? "{0} ({1})(House)" : "{0} ({1})"), String.Format(RuneFormat, desc), m_TargetMap);
            }
            else
            {
                list.Add("an unmarked recall rune");
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Marked)
            {
                string desc;

                if ((desc = m_Description) == null || (desc = desc.Trim()).Length == 0)
                    desc = "an unknown location";

                if (m_TargetMap == Map.Malas)
                    LabelTo(from, (House != null ? 1062454 : 1060804), String.Format(RuneFormat, desc)); // ~1_val~ (Malas)[(House)]
                else if (m_TargetMap == Map.Felucca)
                    // Adam: While we are one facet, there is no need to display [Felucca]
                    //LabelTo( from, (House != null ? 1062452 : 1060805), String.Format( RuneFormat, desc ) ); // ~1_val~ (Felucca)[(House)]
                    LabelTo(from, String.Format(RuneFormat, desc)); // ~1_val~ (Felucca)[(House)]
                else if (m_TargetMap == Map.Trammel)
                    LabelTo(from, (House != null ? 1062453 : 1060806), String.Format(RuneFormat, desc)); // ~1_val~ (Trammel)[(House)]
                else
                    LabelTo(from, (House != null ? "{0} ({1})(House)" : "{0} ({1})"), String.Format(RuneFormat, desc), m_TargetMap);
            }
            else
            {
                LabelTo(from, "an unmarked recall rune");
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            int number;

            if (!IsChildOf(from.Backpack))
            {
                number = 1042001; // That must be in your pack for you to use it.
            }
            else if (House != null)
            {
                number = 1062399; // You cannot edit the description for this rune.
            }
            else if (m_Marked)
            {
                number = 501804; // Please enter a description for this marked object.

                from.Prompt = new RenamePrompt(this);
            }
            else
            {
                number = 501805; // That rune is not yet marked.
            }

            from.SendLocalizedMessage(number);
        }

        private class RenamePrompt : Prompt
        {
            private RecallRune m_Rune;

            public RenamePrompt(RecallRune rune)
            {
                m_Rune = rune;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Rune.House == null && m_Rune.Marked)
                {
                    m_Rune.Description = text;
                    from.SendLocalizedMessage(1010474); // The etching on the rune has been changed.
                }
            }
        }

        [Constructable]
        public RecallRune()
            : base(0x1F14)
        {
            Weight = 1.0;
            CalculateHue();
        }

        public RecallRune(Serial serial)
            : base(serial)
        {
        }
    }
}