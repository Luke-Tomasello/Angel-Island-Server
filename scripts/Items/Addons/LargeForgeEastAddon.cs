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

/* Scripts/Items/Addons/LargeEastForgeAddon.cs
* ChangeLog
*  7/30/06, Kit
*		Add Forge Bellows, Rise of the animated forges!
*/

using Server.Diagnostics;
using System;

namespace Server.Items
{

    public class LargeForgeEastAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new LargeForgeEastDeed(); } }

        [Constructable]
        public LargeForgeEastAddon()
        {
            AddComponent(new ForgeBellows(6534), 0, 0, 0);
            AddComponent(new ForgeComponent(0x198A), 0, 1, 0);
            AddComponent(new ForgeComponent(0x1996), 0, 2, 0);
            AddComponent(new ForgeBellows(6546), 0, 3, 0);
        }

        public LargeForgeEastAddon(Serial serial)
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
                if (this.Serial < 0x60000000)
                {   // if the serial is >= 0x60000000, we are restoring from a backup and linkages have not been fixed up
                    ((AddonComponent)this.Components[0]).ItemID = 6534;
                    ((AddonComponent)this.Components[1]).ItemID = 0x198A;
                    ((AddonComponent)this.Components[2]).ItemID = 0x1996;
                    ((AddonComponent)this.Components[3]).ItemID = 6546;
                }
                
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                Console.WriteLine("Exception caught in Large East forge addon Deserialization");
                System.Console.WriteLine(exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }

    }

    public class LargeForgeEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeForgeEastAddon(); } }
        public override int LabelNumber { get { return 1044331; } } // large forge (east)

        [Constructable]
        public LargeForgeEastDeed()
        {
        }

        public LargeForgeEastDeed(Serial serial)
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