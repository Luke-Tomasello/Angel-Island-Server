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

/* Scripts/Lights/Braizer.cs
 * CHANGELOG
 *	04/07/05, Kitaras	
 *		Added moveable flag to new constructor for treasure loot braizers
 */
using System;

namespace Server.Items
{
    public class Brazier : BaseLight
    {
        public override int LitItemID { get { return 0xE31; } }

        [Constructable]
        public Brazier()
            : this(false) //default normal braziers unable to move
        {
        }

        [Constructable]
        public Brazier(bool canmove)
            : base(0xE31)
        {
            Movable = canmove;
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = true;
            Light = LightType.Circle225;
            Weight = 20.0;
        }

        public Brazier(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}