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

/* Scripts\Items\Skill Items\Tailor Items\Dyetubs\Charged\RunebookDyeTub.cs
 * CHANGELOG:
 *  9/20/21, Yoar
 *      Initial Version
 */

using Server.Targeting;

namespace Server.Items
{
    public class RunebookDyeTub : DyeTubCharged
    {
        public override string DefaultName { get { return "runebook dye tub"; } }

        public override CustomHuePicker CustomHuePicker { get { return CustomHuePicker.LeatherDyeTub; } }

        [Constructable]
        public RunebookDyeTub()
            : this(0, 10)
        {
        }

        [Constructable]
        public RunebookDyeTub(int hue)
            : this(hue, 10)
        {
        }

        [Constructable]
        public RunebookDyeTub(int hue, int uses)
            : base(hue, uses)
        {
            Redyable = true;
        }

        public RunebookDyeTub(Serial serial)
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
                        reader.ReadBool(); // m_IsRewardItem
                        break;
                    }
            }

            if (version < 2)
                this.UsesRemaining = 10;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.SendMessage("Target the runebook to dye.");
                from.BeginTarget(1, false, TargetFlags.None, OnTarget);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        private void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Runebook || targeted is RecallRune)
            {
                Item item = (Item)targeted;

                if (!from.InRange(this.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                }
                else if (!item.Movable)
                {
                    from.SendLocalizedMessage(1049776); // You cannot dye runes or runebooks that are locked down.
                }
                else
                {
                    item.Hue = this.DyedHue;

                    from.PlaySound(0x23E);

                    if (LimitedUses)
                        ConsumeUse(from);
                }
            }
            else
            {
                from.SendMessage("That is not a runebook.");
            }
        }
    }
}