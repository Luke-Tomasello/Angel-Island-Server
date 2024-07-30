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

/* Server/Engines/EventResources/Stahlrim/Items/Stahlrim.cs
 * CHANGELOG:
 *  12/11/23, Yoar
 *      Initial version.
 */

using System;

namespace Server.Items
{
    [FlipableAttribute(0x1BF2, 0x1BEF)]
    public class Stahlrim : Item, ICommodity
    {
        string ICommodity.Description
        {
            get
            {
                return String.Format("{0} stahlrim", Amount);
            }
        }

        public override string DefaultName { get { return "stahlrim"; } }

        [Constructable]
        public Stahlrim()
            : this(1)
        {
        }

        [Constructable]
        public Stahlrim(int amount)
            : base(0x1BF2)
        {
            Hue = CraftResources.GetHue(CraftResource.Stahlrim);
            Stackable = true;
            Amount = amount;
            Weight = 0.1;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Stahlrim(), amount);
        }

        public Stahlrim(Serial serial)
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