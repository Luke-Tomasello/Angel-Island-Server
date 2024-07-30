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

/* Scripts\Items\Special\Rares\Containers\Bucket.cs
 * Changelog:
 *	8/18/23, Yoar
 *		Merged from RunUO
 */

namespace Server.Items
{
    public class Bucket : BaseWaterContainer
    {
        public override int DefaultGumpID { get { return 0x3E; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(33, 36, 109, 112); }
        }

        public override int EmptyID { get { return 0x14E0; } }
        public override int FilledID { get { return 0x2004; } }
        public override int MaxQuantity { get { return 25; } }

        public override CraftResource DefaultResource { get { return CraftResource.RegularWood; } }

        [Constructable]
        public Bucket()
            : this(false)
        {
        }

        [Constructable]
        public Bucket(bool filled)
            : base(filled)
        {
        }

        public Bucket(Serial serial)
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