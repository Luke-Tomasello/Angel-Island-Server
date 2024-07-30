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

/* Scripts/Items/Misc/OilCloth.cs
 * ChangeLog:
 *  8/14/2023, Adam
 *      Call ConsumeUse() instead of Consume() when wiping off Savage paint or removing a Disguise
 *  4/25/23, Yoar
 *      - Oil cloth is no longer stackable
 *      - Added Uses
 *      - The default RunUO behavior of OilCLoth is a mixture of the
 *        pre-AOS behavior and the AOS behavior... Added distinction
 *        between the two eras.
 *	11/29/05, erlein
 *		Altered to HueMod back to default (-1) rather than alter body type.
 * 
 */

using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.Items
{
    public class OilCloth : Item, IScissorable, IDyable
    {
        public override int LabelNumber { get { return 1041498; } } // oil cloth

        private int m_Uses;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses
        {
            get { return m_Uses; }
            set { m_Uses = value; }
        }

        [Constructable]
        public OilCloth()
            : base(0x175D)
        {
            Weight = 1.0;
            Hue = 2001;
            m_Uses = 80;
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            Hue = sender.DyedHue;

            return true;
        }

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
                return false;

            base.ScissorHelper(scissors, from, new Bandage(), 1);

            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(OnTarget));
                //from.SendLocalizedMessage(1005424); // Select the weapon or armor you wish to use the cloth on.
                from.SendMessage("Select the object you wish to use the cloth on.");
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public void OnTarget(Mobile from, object obj)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (obj is BaseWeapon && !Core.RuleSets.AOSRules()) // pre-AOS
            {
                BaseWeapon weapon = (BaseWeapon)obj;

                if (weapon.RootParent != from)
                {
                    from.SendLocalizedMessage(1005425); // You may only wipe down items you are holding or carrying.
                }
                else if (weapon.Corrosion == 0)
                {
                    from.LocalOverheadMessage(Network.MessageType.Regular, 0x3B2, 1005422); // Hmmmm... this does not need to be cleaned.
                }
                else
                {
                    weapon.PoisonCharges = 0;

                    if (weapon.Corrosion < 2)
                        weapon.Corrosion = 0;
                    else
                        weapon.Corrosion -= 2;

                    if (weapon.Corrosion > 0)
                    {
                        from.SendLocalizedMessage(1005423); // You have removed some of the caustic substance, but not all.
                    }
                    else
                    {
                        if (ConsumeUse())
                            from.SendLocalizedMessage(1010496); // You have cleaned the item, but you have used up the rag.
                        else
                            from.SendLocalizedMessage(1010497); // You have cleaned the item.
                    }
                }
            }
            else if (obj is BaseWeapon) // AOS
            {
                BaseWeapon weapon = (BaseWeapon)obj;

                if (weapon.RootParent != from)
                {
                    from.SendLocalizedMessage(1005425); // You may only wipe down items you are holding or carrying.
                }
                else if (weapon.Poison == null || weapon.PoisonCharges <= 0)
                {
                    from.LocalOverheadMessage(Network.MessageType.Regular, 0x3B2, 1005422); // Hmmmm... this does not need to be cleaned.
                }
                else
                {
                    weapon.PoisonCharges = 0;

                    // TODO: Verify
                    if (ConsumeUse())
                        from.SendLocalizedMessage(1079809); // Your oil cloth is destroyed.
                    else
                        from.SendLocalizedMessage(1079810); // You wipe away the poison.
                }
            }
            else if (obj == from && obj is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)obj;

                if (SavagePlayer(pm))
                {
                    pm.SavagePaintExpiration = TimeSpan.Zero;

                    pm.HueMod = -1;
                    pm.BodyMod = 0;

                    pm.Delta(MobileDelta.Body);

                    from.SendLocalizedMessage(1040006); // You wipe away all of your body paint.

                    ConsumeUse();
                }
                else if (DisgusiedPlayer(pm))
                {
                    DisguiseGump.StopTimer(pm);
                    DisguiseGump.OnDisguiseExpire(pm);
                    from.SendMessage("you discreetly remove your disguise");

                    ConsumeUse();
                }
                else
                {
                    from.LocalOverheadMessage(Network.MessageType.Regular, 0x3B2, 1005422); // Hmmmm... this does not need to be cleaned.
                }
            }
            else
            {
                from.SendLocalizedMessage(1005426); // The cloth will not work on that.
            }
        }
        private static bool SavagePlayer(object o)
        {
            return o is PlayerMobile pm && (pm.BodyMod == 183 || pm.BodyMod == 184 || pm.HueMod == 0);
        }
        private static bool DisgusiedPlayer(object o)
        {
            return o is PlayerMobile pm && DisguiseGump.IsDisguised(pm);
        }
        public bool ConsumeUse()
        {
            if (--m_Uses <= 0)
            {
                Consume();
                return true;
            }

            return false;
        }

        public OilCloth(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            writer.WriteEncodedInt(m_Uses);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Uses = reader.ReadEncodedInt();
                        break;
                    }
            }

            // Previous version of OilCloth is stackable. Item uses don't make
            // sense in the context of a stack. Therefore set the uses to 1.
            if (version < 1)
                m_Uses = 1;
        }
    }
}