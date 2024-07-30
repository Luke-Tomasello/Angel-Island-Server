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

/* Scripts\Items\Skill Items\Tailor Items\Dyetubs\Charged\PotionKegDyeTub.cs
 * CHANGELOG:
 *  9/19/21, Yoar
 *      Now derives from the DyeTubCharged class.
 *	4/30/05 - Pix
 *		Assigned Name property so that it will show up properly in a vendor's list.
 *	9/29/04 - Pixie
 *		Initial Version
 */

using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    public class PotionKegDyeTub : DyeTubCharged
    {
        public override string DefaultName { get { return "potion keg dye tub"; } }

        [Constructable]
        public PotionKegDyeTub()
            : this(0, 10)
        {
        }

        [Constructable]
        public PotionKegDyeTub(int hue)
            : this(hue, 10)
        {
        }

        [Constructable]
        public PotionKegDyeTub(int hue, int uses)
            : base(hue, uses)
        {
            Redyable = true;
        }

        public PotionKegDyeTub(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2: break; // version 2 derives from ChargedDyeTub
                case 1:
                    {
                        this.UsesRemaining = reader.ReadInt();
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.SendMessage("Target the potion keg to paint.");
                from.BeginTarget(1, false, TargetFlags.None, OnTarget);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        private void OnTarget(Mobile from, object targeted)
        {
            if (targeted is PotionKeg)
            {
                PotionKeg keg = (PotionKeg)targeted;

                if (!from.InRange(this.GetWorldLocation(), 1) || !from.InRange(keg.GetWorldLocation(), 1))
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                }
                else
                {
                    bool okay = keg.IsChildOf(from.Backpack);

                    if (!okay)
                    {
                        if (keg.Parent == null)
                        {
                            BaseHouse house = BaseHouse.FindHouseAt(keg);

                            if (house == null || !house.IsLockedDown(keg))
                                from.SendMessage("The potion keg must be locked down to paint it.");
                            else if (!house.IsCoOwner(from))
                                from.SendLocalizedMessage(501023); // You must be the owner to use this item.
                            else
                                okay = true;
                        }
                        else
                        {
                            from.SendMessage("The potion keg must be in your backpack to be painted.");
                        }
                    }

                    if (okay)
                    {
                        keg.Hue = this.DyedHue;

                        from.PlaySound(0x23E);

                        if (LimitedUses)
                            ConsumeUse(from);
                    }
                }
            }
            else
            {
                from.SendMessage("That is not a potion keg.");
            }
        }
    }
}