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

/* scripts\Engines\CoreManagement\AlignmentConsole.cs
 * CHANGELOG:
 *  4/14/23, Yoar
 *      Initial commit.
 */

using Server.Items;
using System;

namespace Server.Engines.Alignment
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class AlignmentConsole : Item
    {
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan ChangeAlignmentDelay
        {
            get { return AlignmentConfig.ChangeAlignmentDelay; }
            set { AlignmentConfig.ChangeAlignmentDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool StrongholdNotoriety
        {
            get { return AlignmentConfig.StrongholdNotoriety; }
            set { AlignmentConfig.StrongholdNotoriety = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan TraitorCooldown
        {
            get { return AlignmentConfig.TraitorCooldown; }
            set { AlignmentConfig.TraitorCooldown = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TitleDisplay TitleDisplay
        {
            get { return AlignmentConfig.TitleDisplay; }
            set { AlignmentConfig.TitleDisplay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool KillPointsEnabled
        {
            get { return AlignmentConfig.KillPointsEnabled; }
            set { AlignmentConfig.KillPointsEnabled = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan KillAwardCooldown
        {
            get { return AlignmentConfig.KillAwardCooldown; }
            set { AlignmentConfig.KillAwardCooldown = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public CreatureAllegiance CreatureAllegiance
        {
            get { return AlignmentConfig.CreatureAllegiance; }
            set { AlignmentConfig.CreatureAllegiance = value; }
        }

        [Constructable]
        public AlignmentConsole()
            : base(0x1F14)
        {
            Hue = 1254;
            Name = "Alignment Settings Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public AlignmentConsole(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadEncodedInt();
        }
    }
}