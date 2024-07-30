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

/* Scripts\Items\Misc\ThrowableItem.cs
 * ChangeLog
 *    3/1/22, Yoar
 *		Initial version.
 */

using Server.Targeting;
using System;

namespace Server.Items
{
    /*
	 * This class can be inserted into any class inheriting from Item
	 * and serializing with a version less than 128.
	 */
    public abstract class BaseThrowableItem : Item
    {
        public virtual int ThrowRange { get { return 10; } }
        public virtual int Timeout { get { return 5; } } // in seconds
        public virtual int ThrowItemID { get { return -1; } }
        public virtual int ThrowSpeed { get { return 7; } }
        public virtual bool FixedDirection { get { return false; } }
        public virtual bool Explodes { get { return true; } }
        public virtual bool DelayedHit { get { return false; } }

        public BaseThrowableItem(int itemID)
            : base(itemID)
        {
        }

        public BaseThrowableItem(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!this.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from.Mounted)
            {
                from.SendLocalizedMessage(1010097); // You cannot use this while mounted.
            }
            else
            {
                object toLock = GetLock(from);

                if (!from.CanBeginAction(toLock))
                {
                    from.SendLocalizedMessage(501789); // You must wait before trying again.
                }
                else
                {
                    OnAim(from);

                    from.Target = new ThrowTarget(this, toLock);
                }
            }
        }

        public virtual object GetLock(Mobile from)
        {
            return this.GetType();
        }

        public virtual void OnAim(Mobile from)
        {
        }

        private class ThrowTarget : Target
        {
            private BaseThrowableItem m_ToThrow;
            private object m_ToLock;

            public ThrowTarget(BaseThrowableItem toThrow, object toLock)
                : base(toThrow.ThrowRange, false, TargetFlags.None)
            {
                m_ToThrow = toThrow;
                m_ToLock = toLock;
                CheckLOS = true;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted == from)
                {
                    from.SendLocalizedMessage(1005576); // You can't throw this at yourself.
                }
                else if (!(targeted is Mobile))
                {
                    from.SendMessage("You can only throw this at something that can throw one back.");
                }
                else if (!m_ToThrow.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
                else if (from.Mounted)
                {
                    from.SendLocalizedMessage(1010097); // You cannot use this while mounted.
                }
                else if (!from.BeginAction(m_ToLock))
                {
                    from.SendLocalizedMessage(501789); // You must wait before trying again.
                }
                else
                {
                    new ReleaseLockTimer(from, m_ToLock, TimeSpan.FromSeconds(m_ToThrow.Timeout)).Start();

                    Mobile targ = (Mobile)targeted;

                    from.Animate(9, 1, 1, true, false, 0);

                    Effects.SendMovingEffect(from, targ, m_ToThrow.GetThrowItemID(), m_ToThrow.ThrowSpeed, 0, m_ToThrow.FixedDirection, m_ToThrow.Explodes, m_ToThrow.Hue, 0);

                    if (m_ToThrow.DelayedHit)
                        new ThrowTimer(m_ToThrow, from, targ, 7).Start();
                    else
                        m_ToThrow.DoHit(from, targ);
                }
            }
        }

        private int GetThrowItemID()
        {
            int itemID = this.ThrowItemID;

            if (itemID < 0)
                itemID = this.ItemID;

            return itemID;
        }

        private class ThrowTimer : Timer
        {
            private BaseThrowableItem m_ToThrow;
            private Mobile m_From;
            private Mobile m_Targ;

            public ThrowTimer(BaseThrowableItem toThrow, Mobile from, Mobile targ, int speed)
                : base(CalculateDelay(from, targ, speed))
            {
                m_ToThrow = toThrow;
                m_From = from;
                m_Targ = targ;
            }

            protected override void OnTick()
            {
                m_ToThrow.DoHit(m_From, m_Targ);
            }

            private static TimeSpan CalculateDelay(IPoint3D src, IPoint3D trg, int speed)
            {
                int dx = trg.X - src.X;
                int dy = trg.Y - src.Y;

                return TimeSpan.FromSeconds(0.2 * Math.Sqrt(dx * dx + dy * dy) / speed);
            }
        }

        private void DoHit(Mobile from, Mobile targ)
        {
            OnThrow(from, targ);

            ConsumeCharge(from);
        }

        public virtual void OnThrow(Mobile from, Mobile targ)
        {
        }

        public virtual void ConsumeCharge(Mobile from)
        {
            Consume();
        }

        private class ReleaseLockTimer : Timer
        {
            private Mobile m_From;
            private object m_ToLock;

            public ReleaseLockTimer(Mobile from, object toLock, TimeSpan delay)
                : base(delay)
            {
                m_From = from;
                m_ToLock = toLock;
            }

            protected override void OnTick()
            {
                m_From.EndAction(m_ToLock);
            }
        }
        private const byte mask = 0x80;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x80); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            if ((Utility.PeekByte(reader) & mask) == 0)
                return; // old version

            int version = reader.ReadByte();
        }
    }
}