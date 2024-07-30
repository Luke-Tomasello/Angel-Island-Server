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

/* Scripts\Items\Skill Items\Musical Instruments\AwardInstrument.cs
 * ChangeLog
 *  12/8/21, Adam
 *      Initial checkin.
 *      Replaces the individual AwardHarp, AwardLapHarp, and AwardLute
 */

using System;

public enum AwardInstrumentType
{
    Harp,
    LapHarp,
    Lute
}

namespace Server.Items
{
    public class AwardInstrument : RazorInstrument
    {
        private int m_place = 0;
        private int m_year = 0;
        private const string harp = "harp";
        private const string lapHarp = "lapharp";
        private const string lute = "lute";

        [Constructable]
        public AwardInstrument()
            // default to a Harp
            : base(0xEB1, 0x43, 0x44)
        {
            InstrumentType = AwardInstrumentType.Harp;  // set the default instrument
            m_year = DateTime.UtcNow.Year;                 // record year given
            Name = GetName();                           // initialize the name
            LootType = LootType.Newbied;                // newbied
        }

        public AwardInstrument(Serial serial)
            : base(serial)
        {
        }



        [CommandProperty(AccessLevel.GameMaster)]
        public AwardInstrumentType InstrumentType
        {
            get
            {
                switch (ItemID)
                {
                    default:
                    case 0xEB1:
                        return AwardInstrumentType.Harp;
                    case 0xEB2:
                        return AwardInstrumentType.LapHarp;
                    case 0xEB3:
                        return AwardInstrumentType.Lute;
                }
            }
            set
            {
                switch (value)
                {
                    case AwardInstrumentType.Harp:
                        Weight = 35.0;
                        SuccessSound = 0x43;
                        FailureSound = 0x44;
                        ItemID = 0xEB1;
                        base.ConfigureInstrument(harp);
                        break;
                    case AwardInstrumentType.LapHarp:
                        Weight = 10.0;
                        SuccessSound = 0x45;
                        FailureSound = 0x46;
                        ItemID = 0xEB2;
                        base.ConfigureInstrument(lapHarp);
                        break;
                    case AwardInstrumentType.Lute:
                        SuccessSound = 0x4C;
                        FailureSound = 0x4D;
                        Weight = 5.0;
                        ItemID = 0xEB3;
                        base.ConfigureInstrument(lute);
                        break;
                }
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int Place { get { return m_place; } set { m_place = value; ModelInitialize(); InvalidateProperties(); } }
        [CommandProperty(AccessLevel.GameMaster)]
        public int Year { get { return m_year; } set { m_year = value; } }
        public string GetName()
        {
            string title = null;
            switch (m_place)
            {
                default: title = "Loser"; break;
                case 0: title = "Honorable Mention"; break;
                case 1: title = "Winner"; break;
                case 2: title = "2nd Place"; break;
                case 3: title = "3rd Place"; break;
            }
            string name = string.Format("Angel Island Battle of the Bards {0} {1}", m_year, title);

            return name;
        }
        private CraftResource GetResource()
        {
            CraftResource resource;
            switch (m_place)
            {
                default:
                case 0: resource = CraftResource.Iron; break;
                case 1: resource = CraftResource.Valorite; break;
                case 2: resource = CraftResource.Verite; break;
                case 3: resource = CraftResource.Agapite; break;
            }

            return resource;
        }
        private void ModelInitialize()
        {
            Name = GetName();
            Resource = GetResource();
        }

        public override void OnSingleClick(Mobile from)
        {
            if (this.Name != null)
                LabelTo(from, this.Name);
            if (base.Author != null)
                LabelTo(from, base.Author);
            if (base.SongName != null)
                LabelTo(from, base.SongName);
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.WriteEncodedInt((int)version); // version

            switch (version)
            {
                case 1:
                    writer.WriteEncodedInt(m_place);
                    writer.WriteEncodedInt(m_year);
                    break;
                case 0:
                    break;
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    m_place = reader.ReadEncodedInt();
                    m_year = reader.ReadEncodedInt();
                    break;
                case 0:
                    break;
            }
        }
    }
}