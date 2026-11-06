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

using Server.Items;

namespace Server.Engines.Consoles
{
    [FlipableAttribute(0x1F14, 0x1F15, 0x1F16, 0x1F17)]
    public abstract class BaseConsole : Item
    {
        /// <summary>
        /// Base constructor for all consoles.
        /// Applies shared console identity (itemID, name, hue).
        /// </summary>
        protected BaseConsole(string name)
            : base(0x1F14)
        {
            Name = name;
            Hue = Utility.RandomSpecialHue(name);

            // Movable = false;   // Uncomment if you want all consoles non-movable by default
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected BaseConsole(Serial serial)
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