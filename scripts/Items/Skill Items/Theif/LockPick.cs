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

/* Scripts/Items/Skill Items/Theif/LockPick.cs
 * CHANGELOG:
 *  5/21/23, Yoar
 *      Rewrote lockpick delay as an action lock
 *	01/10/05 - Pix
 *		Made it so you can only use a lockpick every 3.5 seconds.
 *		If you use a lockpick in < 1.0 second, you break the lockpick.
 */

using Server.Targeting;
using System;

namespace Server.Items
{
    public interface ILockpickable : IPoint2D
    {
        int LockLevel { get; set; }
        bool Locked { get; set; }
        Mobile Picker { get; set; }
        int MaxLockLevel { get; set; }
        int RequiredSkill { get; set; }

        void LockPick(Mobile from);
    }

    [FlipableAttribute(0x14fc, 0x14fb)]
    public class Lockpick : Item
    {
        [Constructable]
        public Lockpick()
            : this(1)
        {
        }

        [Constructable]
        public Lockpick(int amount)
            : base(0x14FC)
        {
            Stackable = true;
            Amount = amount;
            Weight = 0.1;
        }

        public Lockpick(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Lockpick(), amount);
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

        public override void OnDoubleClick(Mobile from)
        {
            from.SendLocalizedMessage(502068); // What do you want to pick?
            from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private Lockpick m_Item;

            public InternalTarget(Lockpick item)
                : base(1, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted)
                    return;

                if (targeted is ILockpickable)
                {
                    Item item = (Item)targeted;
                    from.Direction = from.GetDirectionTo(item);

                    if (((ILockpickable)targeted).Locked)
                    {
                        if (!from.BeginAction(typeof(Lockpick)))
                        {
                            from.SendLocalizedMessage(500119); // You must wait to perform another action.
                            return;
                        }

                        from.PlaySound(0x241);

                        new InternalTimer(from, (ILockpickable)targeted, m_Item).Start();
                    }
                    else
                    {
                        // The door is not locked
                        from.SendLocalizedMessage(502069); // This does not appear to be locked
                    }
                }
                else
                {
                    from.SendLocalizedMessage(501666); // You can't unlock that!
                }
            }

            private class InternalTimer : Timer
            {
                private Mobile m_From;
                private ILockpickable m_Item;
                private Lockpick m_Lockpick;

                public InternalTimer(Mobile from, ILockpickable item, Lockpick lockpick)
                    : base(TimeSpan.FromSeconds(3.0))
                {
                    m_From = from;
                    m_Item = item;
                    m_Lockpick = lockpick;
                    Priority = TimerPriority.TwoFiftyMS;
                }

                protected void BrokeLockPickTest()
                {
                    // When failed, a 25% chance to break the lockpick
                    if (Utility.Random(4) == 0)
                    {
                        Item item = (Item)m_Item;

                        // You broke the lockpick.
                        item.SendLocalizedMessageTo(m_From, 502074);

                        m_From.PlaySound(0x3A4);
                        m_Lockpick.Consume();
                    }
                }

                protected override void OnTick()
                {
                    m_From.EndAction(typeof(Lockpick));

                    if (!(m_Item is Item))
                        return; // sanity

                    if (!m_From.InRange(m_Lockpick.GetWorldLocation(), 2) || !m_From.InLOS(m_Lockpick))
                        return;

                    if (!m_From.InRange(((Item)m_Item).GetWorldLocation(), 1))
                        return;

                    if (m_Item.LockLevel == 0 || m_Item.LockLevel == -255)
                    {
                        // LockLevel of 0 means that the door can't be picklocked
                        // LockLevel of -255 means it's magic locked
                        ((Item)m_Item).SendLocalizedMessageTo(m_From, 502073); // This lock cannot be picked by normal means
                        return;
                    }

                    if (m_From.Skills[SkillName.Lockpicking].Value < m_Item.RequiredSkill)
                    {
                        ((Item)m_Item).SendLocalizedMessageTo(m_From, 502072); // You don't see how that lock can be manipulated.
                        return;
                    }

                    if (m_From.CheckTargetSkill(SkillName.Lockpicking, m_Item, m_Item.LockLevel, m_Item.MaxLockLevel, new object[2] /*contextObj*/))
                    {
                        // Success! Pick the lock!
                        ((Item)m_Item).SendLocalizedMessageTo(m_From, 502076); // The lock quickly yields to your skill.
                        m_From.PlaySound(0x4A);
                        m_Item.LockPick(m_From);
                    }
                    else
                    {
                        // The player failed to pick the lock
                        BrokeLockPickTest();
                        ((Item)m_Item).SendLocalizedMessageTo(m_From, 502075); // You are unable to pick the lock.
                    }
                }
            }
        }
    }
}