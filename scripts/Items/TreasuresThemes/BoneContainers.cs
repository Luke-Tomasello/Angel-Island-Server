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

/* Scripts/Items/TreasureThemes/BoneContainers.cs
 * CHANGELOG
 *  10/4/23, Yoar
 *      Added more kinds + made it flipable
 *  03/28/06 Taran Kain
 *		Override IDyable.Dye() to disable dying
 *		Why do we inherit Bag instead of BaseContainer? Can't change inherit order without killing serialization.
 *	04/01/05, Kitaras	
 *		Initial	Creation
 */

namespace Server.Items
{
    [Flipable(
        0xECA, 0xECB, 0xECA,
        0xECC, 0xECD, 0xECC,
        0xECF, 0xED0, 0xECF,
        0xED1, 0xED2, 0xED1)]
    public class BoneContainer : Bag, IDyable
    {
        public override string DefaultName { get { return "a pile of bones"; } }
        public override int MaxWeight { get { return 0; } }
        public override int DefaultDropSound { get { return 0x42; } }

        [Constructable]
        public BoneContainer()
            : this(Utility.Random(0xECA, 9))
        {
        }

        [Constructable]
        public BoneContainer(int itemID)
            : base()
        {
            ItemID = itemID;
            GumpID = 9;
        }

        public BoneContainer(Serial serial)
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

        #region	IDyable	Members

        public new bool Dye(Mobile from, DyeTub sender)
        {
            from.SendMessage("You cannot dye that.");
            return false;
        }

        #endregion
    }
}