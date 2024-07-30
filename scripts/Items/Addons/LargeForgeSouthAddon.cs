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

/* Scripts/Items/Addons/LargeSouthForgeAddon.cs
* ChangeLog
*  7/30/06, Kit
*		Add Forge Bellows, Rise of the animated forges!
*/

using Server.Diagnostics;
using System;

namespace Server.Items
{
    public class LargeForgeSouthAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new LargeForgeSouthDeed(); } }

        [Constructable]
        public LargeForgeSouthAddon()
        {
            AddComponent(new ForgeBellows(6522), 0, 0, 0);
            AddComponent(new ForgeComponent(0x197E), 1, 0, 0);
            AddComponent(new ForgeComponent(0x19A2), 2, 0, 0);
            AddComponent(new ForgeBellows(6558), 3, 0, 0);
        }

        public LargeForgeSouthAddon(Serial serial)
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
            //reset all graphics back to base animation, incase we saved and crashed, or went down
            //before bellow timer could reset graphics.
            try
            {
                ((AddonComponent)this.Components[0]).ItemID = 6522;
                ((AddonComponent)this.Components[1]).ItemID = 0x197E;
                ((AddonComponent)this.Components[2]).ItemID = 0x19A2;
                ((AddonComponent)this.Components[3]).ItemID = 6558;
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                Console.WriteLine("Exception caught in LargeSouthforge addon Deserialization");
                System.Console.WriteLine(exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }
    }

    public class LargeForgeSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeForgeSouthAddon(); } }
        public override int LabelNumber { get { return 1044332; } } // large forge (south)

        [Constructable]
        public LargeForgeSouthDeed()
        {
        }

        public LargeForgeSouthDeed(Serial serial)
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