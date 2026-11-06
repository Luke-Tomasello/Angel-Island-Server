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

/* Engines/CoreManagement/PeaceManagementConsole.cs
 * ChangeLog
 * 12/26/25, Adam
 *  Added Blocking properties for all passive hooks
 *	12/23/25, Adam
 *		Created.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

namespace Server.Engines.Consoles
{
    [NoSort]
    public class PeaceManagementConsole : BaseConsole
    {
        [Constructable]
        public PeaceManagementConsole()
            : base("Skill Management Console")
        {
        }

        public PeaceManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Owner)]
        public static double PacifyDamageScaleKnob
        {
            get => CoreAI.PacifyDamageScaleKnob;
            set => CoreAI.PacifyDamageScaleKnob = value;
        }

        [CommandProperty(AccessLevel.Owner)]
        public static double PacifyRobustnessBiasKnob
        {
            get => CoreAI.PacifyRobustnessBiasKnob;
            set => CoreAI.PacifyRobustnessBiasKnob = value;
        }

        [CommandProperty(AccessLevel.Owner)]
        public static double PacifyMusicCenterKnob
        {
            get => CoreAI.PacifyMusicCenterKnob;
            set => CoreAI.PacifyMusicCenterKnob = value;
        }

        [CommandProperty(AccessLevel.Owner)]
        public static double PacifyMusicDampenStrengthKnob
        {
            get => CoreAI.PacifyMusicDampenStrengthKnob;
            set => CoreAI.PacifyMusicDampenStrengthKnob = value;
        }

        [CommandProperty(AccessLevel.Owner)]
        public static double PacifyTimeWeakeningKnob
        {
            get => CoreAI.PacifyTimeWeakeningKnob;
            set => CoreAI.PacifyTimeWeakeningKnob = value;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
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