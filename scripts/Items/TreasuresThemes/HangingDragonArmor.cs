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

/* Scripts/Items/TreasureThemes/HangingDragonArmor.cs
 * CHANGELOG
 *	04/07/05, Kitaras	
 *		Initial Creation
 */

namespace Server.Items
{
    [Flipable(5095, 5096)]
    public class HangingDragonChest : Item
    {


        [Constructable]
        public HangingDragonChest()
            : base(5095)
        {
            Weight = 20.0;
            Hue = 1645;
            Name = "hanging dragonscale chest";

        }

        public HangingDragonChest(Serial serial)
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

    [Flipable(5093, 5094)]
    public class HangingDragonLegs : Item
    {


        [Constructable]
        public HangingDragonLegs()
            : base(5093)
        {
            Weight = 20.0;
            Hue = 1645;
            Name = "hanging dragonscale legs";

        }

        public HangingDragonLegs(Serial serial)
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

    [Flipable(5097, 5098)]
    public class HangingDragonArms : Item
    {


        [Constructable]
        public HangingDragonArms()
            : base(5097)
        {
            Weight = 20.0;
            Hue = 1645;
            Name = "hanging dragonscale arms";

        }

        public HangingDragonArms(Serial serial)
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