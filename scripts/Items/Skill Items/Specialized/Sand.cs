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

using System;

namespace Server.Items
{
    [FlipableAttribute(0x11EA, 0x11EB)]
    public class Sand : Item, ICommodity
    {
        string ICommodity.Description
        {
            get
            {
                return string.Format(Amount == 1 ? "{0} sand" : "{0} sand", Amount);
            }
        }

        public override int LabelNumber { get { return 1044626; } } // sand

        [Constructable]
        public Sand()
            : this(1)
        {
        }

        [Constructable]
        public Sand(int amount)
            : base(0x11EA)
        {
            Name = "sand";
            Stackable = false;
            Weight = 1.0;
        }

        public Sand(Serial serial)
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