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

/* Scripts\Items\Skill Items\Tailor Items\Dyetubs\Charged\TeleporterDyeTub.cs
 * CHANGELOG:
 *  9/19/21, Yoar
 *      Now derives from the DyeTubCharged class.
 *	9/14/06 - Pixie
 *		Initial Version
 */

using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    public class TeleporterAddonDyeTub : DyeTubCharged
    {
        public override string DefaultName { get { return "teleporter dye tub"; } }

        public override CustomHuePicker CustomHuePicker { get { return CustomHuePicker.LeatherDyeTub; } }

        [Constructable]
        public TeleporterAddonDyeTub()
            : this(0, 10)
        {
        }

        [Constructable]
        public TeleporterAddonDyeTub(int hue)
            : this(hue, 10)
        {
        }

        [Constructable]
        public TeleporterAddonDyeTub(int hue, int uses)
            : base(hue, uses)
        {
            Redyable = true;
        }

        public TeleporterAddonDyeTub(Serial serial)
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
                from.SendMessage("Target the teleporter to paint.");
                from.BeginTarget(1, false, TargetFlags.None, OnTarget);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        private void OnTarget(Mobile from, object targeted)
        {
            if (targeted is TeleporterAC)
            {
                TeleporterAC tp = (TeleporterAC)targeted;

                if (!from.InRange(this.GetWorldLocation(), 1) || !from.InRange(tp.GetWorldLocation(), 1))
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                }
                else
                {
                    bool okay = false;

                    BaseHouse house = BaseHouse.FindHouseAt(tp);

                    if (house == null)
                        from.SendMessage("The house seems to be missing.");
                    else if (!house.IsCoOwner(from))
                        from.SendLocalizedMessage(501023); // You must be the owner to use this item.
                    else
                        okay = true;

                    if (okay)
                    {
                        tp.Hue = this.DyedHue;

                        from.PlaySound(0x23E);

                        if (LimitedUses)
                            ConsumeUse(from);
                    }
                }
            }
            else
            {
                from.SendMessage("That is not a teleporter.");
            }
        }
    }
}