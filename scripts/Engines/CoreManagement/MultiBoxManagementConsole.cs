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

/* Scripts\Engines\CoreManagement\MultiBoxManagementConsole.cs
 * ChangeLog
 * 1/16/22: Adam
 *  initial creation
 */

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class MultiBoxManagementConsole : Item
    {
        [Constructable]
        public MultiBoxManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 0x31;
            Name = "MultiBox Management Console";
        }

        public MultiBoxManagementConsole(Serial serial)
            : base(serial)
        {
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double MultiBoxDelay
        {
            get
            {
                return CoreAI.MultiBoxDelay;
            }
            set
            {
                CoreAI.MultiBoxDelay = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double MultiBoxPlayerStopped
        {
            get
            {
                return CoreAI.MultiBoxPlayerStopped;
            }
            set
            {
                CoreAI.MultiBoxPlayerStopped = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double MultiBoxCensure
        {
            get
            {
                return CoreAI.MultiBoxCensure;
            }
            set
            {
                CoreAI.MultiBoxCensure = value;
            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
            {
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            }
        }
    }
}