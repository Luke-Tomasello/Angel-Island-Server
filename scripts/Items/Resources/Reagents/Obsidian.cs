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
    [TypeAlias("Server.Engines.Obsidian.Obsidian")]
    public class Obsidian : BaseReagent, ICommodity
    {
        string ICommodity.Description
        {
            get
            {
                return String.Format("{0} obsidian", Amount);
            }
        }

        [Constructable]
        public Obsidian()
            : this(1)
        {
        }

        [Constructable]
        public Obsidian(int amount)
            : base(0xF89, amount)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Obsidian(), amount);
        }

        public Obsidian(Serial serial)
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