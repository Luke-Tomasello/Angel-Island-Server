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

/* Scripts\Items\Special\Rares\Scarecrow.cs
 * ChangeLog:
 *  7/14/2023, Adam
 *      Initial creation
 */

namespace Server.Items.Rares
{
    [TypeAlias("Server.Items.Rares.Scarecrow")]
    [FlipableAttribute(0x1E34 /*S facing*/, 0x1E35 /*E Facing*/)]
    public class ScarecrowEast : Item // we use an Item here since it's one component, which allows us to 'flip' it.
    {
        [Constructable]
        public ScarecrowEast()
            : base(0x1E35 /*E Facing*/)
        {
        }

        public ScarecrowEast(Serial serial)
            : base(serial)
        {
        }
        public override void OnAfterStolen(Mobile from)
        {
            base.OnAfterStolen(from);
            if (from != null && from.Backpack != null)
            {
                from.Backpack.AddItem(new ScarecrowEastDeed());
                this.Delete();
            }
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

    [FlipableAttribute(0x1E34 /*S facing*/, 0x1E35 /*E Facing*/)]
    public class ScarecrowSouth : Item // we use an Item here since it's one component, which allows us to 'flip' it.
    {
        [Constructable]
        public ScarecrowSouth()
            : base(0x1E34 /*S Facing*/)
        {
        }

        public ScarecrowSouth(Serial serial)
            : base(serial)
        {
        }
        public override void OnAfterStolen(Mobile from)
        {
            base.OnAfterStolen(from);
            if (from != null && from.Backpack != null)
            {
                from.Backpack.AddItem(new ScarecrowSouthDeed());
                this.Delete();
            }
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