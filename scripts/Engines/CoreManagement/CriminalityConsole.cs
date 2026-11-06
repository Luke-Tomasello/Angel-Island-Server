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

/* Scripts\Engines\CoreManagement\CriminalityConsole.cs
 * CHANGELOG:
 *  3/18/2026, Adam
 *      Initial commit.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

namespace Server.Engines.Consoles
{
    public class CriminalityConsole : BaseConsole
    {
        [Constructable]
        public CriminalityConsole()
            : base("Game Systems Console")
        {
        }

        public CriminalityConsole(Serial serial)
            : base(serial)
        {
        }

        #region Criminal Justice System

        [CommandProperty(AccessLevel.Administrator)]
        public int CriminalGrayMinutes
        {
            get => CoreAI.CriminalGrayMinutes;
            set => CoreAI.CriminalGrayMinutes = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int CriminalWhackSeconds
        {
            get => CoreAI.CriminalWhackSeconds;
            set => CoreAI.CriminalWhackSeconds = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int CriminalTravelMinutes
        {
            get => CoreAI.CriminalTravelMinutes;
            set => CoreAI.CriminalTravelMinutes = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int HarmfulGrayMinutes
        {
            get => CoreAI.HarmfulGrayMinutes;
            set => CoreAI.HarmfulGrayMinutes = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int HarmfulPvPGrayMinutes
        {
            get => CoreAI.HarmfulPvPGrayMinutes;
            set => CoreAI.HarmfulPvPGrayMinutes = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int HarmfulWhackSeconds
        {
            get => CoreAI.HarmfulWhackSeconds;
            set => CoreAI.HarmfulWhackSeconds = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int HarmfulTravelMinutes
        {
            get => CoreAI.HarmfulTravelMinutes;
            set => CoreAI.HarmfulTravelMinutes = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int AFKGrayMinutes
        {
            get => CoreAI.AFKGrayMinutes;
            set => CoreAI.AFKGrayMinutes = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int AFKWhackSeconds
        {
            get => CoreAI.AFKWhackSeconds;
            set => CoreAI.AFKWhackSeconds = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int AFKTravelMinutes
        {
            get => CoreAI.AFKTravelMinutes;
            set => CoreAI.AFKTravelMinutes = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int ThievingGrayMinutes
        {
            get => CoreAI.ThievingGrayMinutes;
            set => CoreAI.ThievingGrayMinutes = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int ThievingWhackSeconds
        {
            get => CoreAI.ThievingWhackSeconds;
            set => CoreAI.ThievingWhackSeconds = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int ThievingTravelMinutes
        {
            get => CoreAI.ThievingTravelMinutes;
            set => CoreAI.ThievingTravelMinutes = value;
        }

        #endregion

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}