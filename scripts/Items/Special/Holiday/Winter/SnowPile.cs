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

/* Scripts/Items/Special/Holiday/Winter/SnowPile.cs
 * ChangeLog
 *    11/27/21, Yoar
 *        Merged with RunUO.
 *    12/25/04, Adam
 *        First time check-in.
 */

using Server.Targeting;
using System;

namespace Server.Items
{
    public class SnowPile : Item
    {
        [Constructable]
        public SnowPile()
            : base(0x913)
        {
            Hue = 0x481;
            Weight = 1.0;
        }

        public override int LabelNumber { get { return 1005578; } } // a pile of snow

        public SnowPile(Serial serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042010); // You must have the object in your backpack to use it.
            }
            else if (from.Mounted)
            {
                from.SendLocalizedMessage(1010097); // You cannot use this while mounted.
            }
            else if (from.CanBeginAction(typeof(SnowPile)))
            {
                from.SendLocalizedMessage(1005575); // You carefully pack the snow into a ball...
                from.Target = new SnowTarget(from, this);
            }
            else
            {
                from.SendLocalizedMessage(1005574); // The snow is not ready to be packed yet.  Keep trying.
            }
        }

        private class InternalTimer : Timer
        {
            private Mobile m_From;

            public InternalTimer(Mobile from)
                : base(TimeSpan.FromSeconds(5.0))
            {
                m_From = from;
            }

            protected override void OnTick()
            {
                m_From.EndAction(typeof(SnowPile));
            }
        }

        private class SnowTarget : Target
        {
            private Mobile m_Thrower;
            private Item m_Snow;

            public SnowTarget(Mobile thrower, Item snow)
                : base(10, false, TargetFlags.None)
            {
                m_Thrower = thrower;
                m_Snow = snow;
            }

            private static bool StartThrow(Mobile from, Mobile targ)
            {
                if (from.AccessLevel >= AccessLevel.GameMaster)
                    return true;

                Container pack = targ.Backpack;

#if RunUO
                if (from.Region.IsPartOf(typeof(Engines.ConPVP.SafeZone)) || targ.Region.IsPartOf(typeof(Engines.ConPVP.SafeZone)))
                {
                    from.SendMessage("You may not throw snow here.");
                    return false;
                }
                else if (pack == null || pack.FindItemByType(typeof(SnowPile)) == null)
                {
                    from.SendLocalizedMessage(1005577); // You can only throw a snowball at something that can throw one back.
                    return false;
                }
                else if (!from.BeginAction(typeof(SnowPile)))
#else
                if (!from.BeginAction(typeof(SnowPile)))
#endif
                {
                    from.SendLocalizedMessage(1005574); // The snow is not ready to be packed yet.  Keep trying.
                    return false;
                }

                new InternalTimer(from).Start();
                return true;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target == from)
                {
                    from.SendLocalizedMessage(1005576); // You can't throw this at yourself.
                }
                else if (target is Mobile)
                {
                    Mobile targ = (Mobile)target;

                    if (StartThrow(from, targ))
                    {
                        from.PlaySound(0x145);

                        from.Animate(9, 1, 1, true, false, 0);

                        targ.SendLocalizedMessage(1010572); // You have just been hit by a snowball!
                        from.SendLocalizedMessage(1010573); // You throw the snowball and hit the target!

                        Effects.SendMovingEffect(from, targ, 0x36E4, 7, 0, false, true, m_Snow.Hue - 1, 0);
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1005577); // You can only throw a snowball at something that can throw one back.
                }
            }
        }

        // old SnowPile behavior
#if old
        public override void OnSingleClick(Mobile from)
        {
            this.LabelTo(from, 1005578); // a pile of snow
        }

        private DateTime m_NextAbilityTime;

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042010); //You must have the object in your backpack to use it. 
                return;
            }
            else
            {
                if (DateTime.UtcNow >= m_NextAbilityTime)
                {
                    from.Target = new SnowTarget(from, this);
                    from.SendLocalizedMessage(1005575); // You carefully pack the snow into a ball... 
                }
                else
                {
                    from.SendLocalizedMessage(1005574);
                }
            }
        }

        private class SnowTarget : Target
        {
            private Mobile m_Thrower;
            private SnowPile m_Snow;

            public SnowTarget(Mobile thrower, SnowPile snow)
                : base(10, false, TargetFlags.None)
            {
                m_Thrower = thrower;
                m_Snow = snow;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target == from)
                    from.SendLocalizedMessage(1005576);
                else if (target is Mobile)
                {
                    Mobile m = (Mobile)target;
                    from.PlaySound(0x145);
                    from.Animate(9, 1, 1, true, false, 0);
                    from.SendLocalizedMessage(1010573); // You throw the snowball and hit the target! 
                    m.SendLocalizedMessage(1010572); // You have just been hit by a snowball! 
                    Effects.SendMovingEffect(from, m, 0x36E4, 7, 0, false, true, 0x480, 0);
                    m_Snow.m_NextAbilityTime = DateTime.UtcNow + TimeSpan.FromSeconds(5.0);
                }
                else
                {
                    from.SendLocalizedMessage(1005577);
                }
            }
        }
#endif
    }
}
// created on 16/11/2002 at 19:27