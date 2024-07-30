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

/* Scripts/Items/Jewels/Ring.cs
 * CHANGELOG:
 * 4/7/23, Yoar
 *  BaseJewel now implements IMagicEquip.
 *  Removed old teleport ring code.
 * 3/11/2016, Adam,
 *	-- teleport ring --
 *	o remove explicit clearing of magic type when charges reaches zero. None of the other magic items do this.
 *	o cleanup the region check fo teleport ring
 * 06/26/06, Kit
 *	Added msg to fail of teleport items because of region setting "The magic normally within this object seems absent."
 * 9/23/05, Adam
 *	Add SpellHelper and Region checks so that teleport rings follow the same rules
 *	as the spell.	
 * 05/11/2004 - Pulse
 * 	Added OnDoubleClick routine with supporting InternalTarget class and Target routine 
 * 	to support the teleport spell if the ring is a teleport ring with charges  
 */

namespace Server.Items
{
    public abstract class BaseRing : BaseJewel
    {
        public override int BaseGemTypeNumber { get { return 1044176; } } // star sapphire ring

        public BaseRing(int itemID)
            : base(itemID, Layer.Ring)
        {
        }

        public BaseRing(Serial serial)
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

    public class GoldRing : BaseRing
    {
        [Constructable]
        public GoldRing()
            : base(0x108a)
        {
            Weight = 0.1;
        }

        public GoldRing(Serial serial)
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

    public class SilverRing : BaseRing
    {
        [Constructable]
        public SilverRing()
            : base(0x1F09)
        {
            Weight = 0.1;
        }

        public SilverRing(Serial serial)
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