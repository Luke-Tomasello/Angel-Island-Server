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

/* ./Scripts/Items/Clothing/BrethrenClothing.cs
 *	ChangeLog :
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 20 lines removed.
*/

/* Scripts/Items/Clothing/BrethrenClothing.cs
 * Created 7/20/04 by mith
 * 	3/23/23, Yoar
 * 		CanEquip: Added 'BodyMod == 0' check so that only human body values can be considered savage kin.
 *  2/13/05, Froste
 *      Commented out line in OrcishKinMask Deserialize that prevented natural colored masks from staying natural
 *  2/2/05, Darva
 *		Removed karma penalty from orcish iob.
 *  1/15/05, Adam
 *		Remove the forced setting of the Dyable bool in Deserialize (is handled in baseclothing.cs)
 *  1/15/05, Froste
 *      Set new Dyable bool to false in the creation code and Deserialize
 *	11/10/04, Adam
 *		for legacy objects, force the IOBAlignment = IOBAlignment.XXX in Deserialize
 *	7/23/04, mith
 *		made PirateHat un-dyeable.
 */

namespace Server.Items
{
    public class BloodDrenchedBandana : BaseHat
    {

        [Constructable]
        public BloodDrenchedBandana()
            : base(0x1540, 0x66C)
        {
            Weight = 1.0;
            Name = "blood drenched bandana";
            IOBAlignment = IOBAlignment.Council;
            Dyable = false;
        }

        public BloodDrenchedBandana(Serial serial)
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

            // for legacy objects
            IOBAlignment = IOBAlignment.Council;
        }
    }

    public class PirateHat : BaseHat
    {

        [Constructable]
        public PirateHat()
            : base(0x171B, 0x66C)
        {
            Weight = 1.0;
            Name = "a pirate hat";
            IOBAlignment = IOBAlignment.Pirate;
            Dyable = false;
        }

        public PirateHat(Serial serial)
            : base(serial)
        {
        }

        /*public override bool Dye( Mobile from, DyeTub sender )
		{
			from.SendLocalizedMessage( sender.FailMessage );
			return false;
		}*/

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            // for legacy objects
            IOBAlignment = IOBAlignment.Pirate;
        }
    }

    public class OrcishKinMask : BaseHat
    {

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }

        [Constructable]
        public OrcishKinMask()
            : this(0x8A4)
        {
        }

        [Constructable]
        public OrcishKinMask(int hue)
            : base(0x141B, hue)
        {
            Weight = 2.0;
            Name = "a mask of orcish kin";
            IOBAlignment = IOBAlignment.Orcish;
            Dyable = false;
        }

        public override bool CanEquip(Mobile m)
        {
            if (!base.CanEquip(m))
                return false;

            // Ai uses HUE value and not the BodyMod as there is no sitting graphic
            if ((m.BodyMod == 183 || m.BodyMod == 184) || (m.BodyMod == 0 && m.HueMod == 0))
            {
                m.SendLocalizedMessage(1061629); // You can't do that while wearing savage kin paint.
                return false;
            }

            return true;
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);

        }

        public OrcishKinMask(Serial serial)
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

            //if (Hue != 0x8A4)
            //	Hue = 0x8A4;

            // for legacy objects
            IOBAlignment = IOBAlignment.Orcish;
        }
    }

    public class BrigandKinBandana : BaseHat
    {

        [Constructable]
        public BrigandKinBandana()
            : this(0)
        {
        }

        [Constructable]
        public BrigandKinBandana(int hue)
            : base(0x1540, hue)
        {
            Weight = 1.0;
            Name = "brigand kin bandana";
            IOBAlignment = IOBAlignment.Brigand;
            Dyable = false;
        }

        public BrigandKinBandana(Serial serial)
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

            // for legacy objects
            IOBAlignment = IOBAlignment.Brigand;
        }
    }

    public class BrigandKinBoots : BaseShoes
    {
        [Constructable]
        public BrigandKinBoots()
            : this(0)
        {
        }

        [Constructable]
        public BrigandKinBoots(int hue)
            : base(0x170B, hue)
        {
            Weight = 3.0;
            Name = "brigand kin boots";
            IOBAlignment = IOBAlignment.Brigand;
            Dyable = false;
        }

        public BrigandKinBoots(Serial serial)
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

            // for legacy objects
            IOBAlignment = IOBAlignment.Brigand;
        }
    }
}