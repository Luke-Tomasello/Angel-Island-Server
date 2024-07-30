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

/* Scripts/Items/Farming/FarmableFlax.cs
 * CHANGELOG
 * 	8/20/23, Yoar
 * 		Merge with RunUO
 */

namespace Server.Items
{
    public class FarmableFlax : FarmableCrop
    {
        public static int GetCropID()
        {
            return Utility.Random(6809, 3);
        }

        public override Item GetCropObject()
        {
            Flax flax = new Flax();

            flax.ItemID = Utility.Random(6812, 2);

            return flax;
        }

        public override int GetPickedID()
        {
            return 3254;
        }

        [Constructable]
        public FarmableFlax() : base(GetCropID())
        {
        }

        public FarmableFlax(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}