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

/* Scripts\Engines\EventResources\Dragonglass\Items\Dragonglass.cs
 * CHANGELOG:
 *  4/26/2024, Adam
 *      Initial version.
 */

using System;

namespace Server.Items
{
    public class Dragonglass : BaseReagent, ICommodity
    {
        string ICommodity.Description
        {
            get
            {
                return string.Format("{0} dragonglass", Amount);
            }
        }
        public override string DefaultName { get { return "dragonglass"; } }

        [Constructable]
        public Dragonglass()
            : this(1)
        {
        }

        [Constructable]
        public Dragonglass(int amount)
            : base(0xF91, amount)
        {
            Hue = CraftResources.GetHue(CraftResource.Dragonglass);
            Stackable = true;
            Amount = amount;
            Weight = 0.1;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Dragonglass(), amount);
        }

        public Dragonglass(Serial serial)
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