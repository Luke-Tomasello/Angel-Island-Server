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

/* Scripts/Items/Farming/FarmablePumpkin.cs
 * CHANGELOG
 * 	8/20/23, Yoar
 * 		Merge with RunUO
 */

namespace Server.Items
{
    public class FarmablePumpkin : FarmableCrop
    {
        public static int GetCropID()
        {
            return Utility.Random(3166, 3);
        }

        public override Item GetCropObject()
        {
            Pumpkin pumpkin = new Pumpkin();

            pumpkin.ItemID = Utility.Random(3178, 3);

            return pumpkin;
        }

        public override int GetPickedID()
        {
            return Utility.Random(3166, 3);
        }

        [Constructable]
        public FarmablePumpkin()
            : base(GetCropID())
        {
        }

        public FarmablePumpkin(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}