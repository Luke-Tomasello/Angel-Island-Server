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

/* Scripts\Engines\CoreManagement\MultiBoxManagementConsole.cs
 * ChangeLog
 * 1/16/22: Adam
 *  initial creation
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

namespace Server.Engines.Consoles
{
    public class MultiBoxManagementConsole : BaseConsole
    {
        [Constructable]
        public MultiBoxManagementConsole()
            : base("MultiBox Management Console")
        {
        }

        public MultiBoxManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double MultiBoxDelay
        {
            get => CoreAI.MultiBoxDelay;
            set => CoreAI.MultiBoxDelay = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double MultiBoxPlayerStopped
        {
            get => CoreAI.MultiBoxPlayerStopped;
            set => CoreAI.MultiBoxPlayerStopped = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double MultiBoxCensure
        {
            get => CoreAI.MultiBoxCensure;
            set => CoreAI.MultiBoxCensure = value;
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