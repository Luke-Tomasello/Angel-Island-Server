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

/* Items/Misc/FishingScales.cs
 * CHANGELOG:
 *  7/26/23, Yoar
 *      Reworked. Now displays the catch date.
 *  11/30/21, Yoar
 *      Initial version.
 */

using Server.Network;
using Server.Targeting;
using System;

namespace Server.Items
{
    public class FishingScales : Item
    {
        [Constructable]
        public FishingScales()
            : base(0x1852)
        {
            Name = "fishing scales";
            Weight = 4.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 1))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else
            {
                from.SendLocalizedMessage(502431); // What would you like to weigh?
                from.Target = new InternalTarget(this);
            }
        }

        private class InternalTarget : Target
        {
            private FishingScales m_Scales;

            public InternalTarget(FishingScales scales)
                : base(1, false, TargetFlags.None)
            {
                m_Scales = scales;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted == m_Scales)
                {
                    m_Scales.PublicOverheadMessage(MessageType.Regular, 0, false, "It cannot weight itself.");
                }
                else if (targeted is Fish)
                {
                    m_Scales.PublicOverheadMessage(MessageType.Regular, 0, false, "'Tis but a small fish...");
                }
                else if (targeted is BigFish)
                {
                    BigFish bigFish = (BigFish)targeted;

                    string format;

                    if (bigFish.Weight >= 225.0)
                        format = "This fish is truly colossal! It weighs {0:F1} stones!";
                    else if (bigFish.Weight >= 200.0)
                        format = "This fish weighs a whopping {0:F1} stones!";
                    else if (bigFish.Weight >= 150.0)
                        format = "This fish weighs a massive {0:F1} stones.";
                    else if (bigFish.Weight >= 100.0)
                        format = "This fish weighs a hefty {0:F1} stones.";
                    else if (bigFish.Weight >= 50.0)
                        format = "This fish weighs a typical {0:F1} stones.";
                    else if (bigFish.Weight >= 20.0)
                        format = "This fish weighs a puny {0:F1} stones.";
                    else if (bigFish.Weight > 1.0)
                        format = "This itsy bitsy fish weighs but {0:F1} stones.";
                    else
                        format = "This fish is lighter than a feather.";

                    m_Scales.PublicOverheadMessage(MessageType.Regular, 0, false, String.Format(format, bigFish.Weight));

                    if (!String.IsNullOrEmpty(bigFish.Fisher) || bigFish.Caught != DateTime.MinValue)
                    {
                        string message = "This fish was caught";

                        if (!String.IsNullOrEmpty(bigFish.Fisher))
                            message += " by " + bigFish.Fisher;

                        if (bigFish.Caught != DateTime.MinValue)
                            message += " on " + bigFish.Caught.ToShortDateString();

                        message += ".";

                        m_Scales.PublicOverheadMessage(MessageType.Regular, 0, false, message);
                    }
                }
                else
                {
                    m_Scales.PublicOverheadMessage(MessageType.Regular, 0, false, "That is not a fish!");
                }
            }
        }

        public FishingScales(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        if (version < 1)
                            reader.ReadDateTime(); // cut-off date

                        break;
                    }
            }
        }
    }
}