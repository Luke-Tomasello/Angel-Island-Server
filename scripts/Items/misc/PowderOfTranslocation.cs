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

using Server.Network;
using Server.Spells;
using Server.Targeting;

/*Changelog
 * 03/14/05, Lego
 *      made so it has to be in backpack to use.
 * 1/26/05 Darva,
 *		Prevented using in houses, dungeons, etc.
 * 1/25/05 Darva
 *		Taken from new RunUO 1.0.0, first checkin.
 *		Modified to charge moonstones.
 */

namespace Server.Items
{
    public class PowderOfTranslocation : Item
    {
        [Constructable]
        public PowderOfTranslocation()
            : this(1)
        {
        }

        [Constructable]
        public PowderOfTranslocation(int amount)
            : base(0x26B8)
        {
            Stackable = true;
            Weight = 0.1;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new PowderOfTranslocation(amount), amount);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.Target = new InternalTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

        }

        private class InternalTarget : Target
        {
            private PowderOfTranslocation m_Powder;

            public InternalTarget(PowderOfTranslocation powder)
                : base(-1, false, TargetFlags.None)
            {
                m_Powder = powder;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Powder.Deleted)
                    return;

                if (!from.InRange(m_Powder.GetWorldLocation(), 2))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                }
                else if (targeted is Moonstone)
                {
                    Moonstone stone = (Moonstone)targeted;
                    if (!from.CanSee(stone))
                    {
                        from.SendLocalizedMessage(500237); // Target can not be seen.
                    }
                    else if (!SpellHelper.CheckTravel(from, TravelCheckType.Mark))
                    {
                    }
                    else if (SpellHelper.CheckMulti(from.Location, from.Map, !Core.RuleSets.AOSRules()))
                    {
                        from.SendLocalizedMessage(501942); // That location is blocked.
                    }
                    else if (!stone.IsChildOf(from.Backpack))
                    {
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, true, "That must be in your pack");
                    }
                    else
                    {
                        stone.Mark(from);

                        from.PlaySound(0x1FA);
                        Effects.SendLocationEffect(from, from.Map, 14201, 16);
                        m_Powder.Amount = m_Powder.Amount - 1;
                        if (m_Powder.Amount <= 0)
                            m_Powder.Delete();
                    }
                }
                else
                {
                    from.SendMessage("Powder of translocation has no effect on this item"); // Powder of translocation has no effect on this item.
                }
            }
        }

        public PowderOfTranslocation(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}