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

/* scripts\Engines\CoreManagement\AlignmentConsole.cs
 * CHANGELOG:
 *  4/14/23, Yoar
 *      Initial commit.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using Server.Engines.Alignment;
using System;

namespace Server.Engines.Consoles
{
    public class AlignmentConsole : BaseConsole
    {
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool Enabled
        {
            get => AlignmentSystem.Enabled;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan ChangeAlignmentDelay
        {
            get => AlignmentConfig.ChangeAlignmentDelay;
            set => AlignmentConfig.ChangeAlignmentDelay = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool StrongholdNotoriety
        {
            get => AlignmentConfig.StrongholdNotoriety;
            set => AlignmentConfig.StrongholdNotoriety = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan TraitorCooldown
        {
            get => AlignmentConfig.TraitorCooldown;
            set => AlignmentConfig.TraitorCooldown = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TitleDisplay TitleDisplay
        {
            get => AlignmentConfig.TitleDisplay;
            set => AlignmentConfig.TitleDisplay = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool KillPointsEnabled
        {
            get => AlignmentConfig.KillPointsEnabled;
            set => AlignmentConfig.KillPointsEnabled = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan KillAwardCooldown
        {
            get => AlignmentConfig.KillAwardCooldown;
            set => AlignmentConfig.KillAwardCooldown = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public CreatureAllegiance CreatureAllegiance
        {
            get => AlignmentConfig.CreatureAllegiance;
            set => AlignmentConfig.CreatureAllegiance = value;
        }

        [Constructable]
        public AlignmentConsole()
            : base("Alignment Settings Console")
        {
        }

        public AlignmentConsole(Serial serial)
            : base(serial)
        {
        }

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