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

/* Engines/Township/Resources/Marble.cs
 * CHANGELOG:
 * 3/23/22, Yoar
 *	    Initial version.
 */

using System;

namespace Server.Items
{
    public class Marble : Item, ICommodity
    {
        public const string ResourceName = "marble";
        public const string ResourceMessage = "You do not have sufficient marble to make that.";

        string ICommodity.Description
        {
            get { return string.Format("{0} high quality marble", Amount); }
        }

        [Constructable]
        public Marble()
            : base(0x1779)
        {
            Weight = 10.0;
            Hue = 2425; // placeholder - looks identical to agapite granite
            Name = "high quality marble";
        }

        public Marble(Serial serial)
            : base(serial)
        {
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