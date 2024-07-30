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

/* Scripts\Items\Misc\RottenTomato.cs
 * ChangeLog
 *    2/0/22, Adam
 *		Initial creation.
 */

using Server.Targeting;
using System;

namespace Server.Items
{
    public class RottenTomato : Item
    {
        [Constructable]
        public RottenTomato()
            : base(0x9D0)
        {
            Weight = 1.0;
            Stackable = true;
            Name = "rotten tomato";
        }

        public RottenTomato(Serial serial)
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
                from.SendMessage("You must have the rotten tomato in your backpack to use it.");
            }
            else if (from.Mounted)
            {
                from.SendLocalizedMessage(1010097); // You cannot use this while mounted.
            }
            else if (from.CanBeginAction(typeof(RottenTomato)))
            {
                from.SendMessage("You take careful aim with the rotten tomato.");
                from.Target = new TomatoTarget(from, this);
            }
            else
            {
                from.SendLocalizedMessage(501789); // You must wait before trying again.
            }
        }
        public override Item Dupe(int amount)
        {
            return base.Dupe(new RottenTomato(), amount);
        }
        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, string.Format("{3}{2}{0}{1}", this.Name, (Amount > 1) ? "'s" : "", (Amount == 1) ? "a " : "", (Amount > 1) ? Amount.ToString() + " " : ""));
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
                m_From.EndAction(typeof(RottenTomato));
            }
        }

        private class TomatoTarget : Target
        {
            private Mobile m_thrower;
            private Item m_tomato;

            public TomatoTarget(Mobile thrower, Item tomato)
                : base(10, false, TargetFlags.None)
            {
                m_thrower = thrower;
                m_tomato = tomato;
            }

            private static bool StartThrow(Mobile from, Mobile targ)
            {
                if (from.AccessLevel >= AccessLevel.GameMaster)
                    return true;

                Container pack = targ.Backpack;

                if (!from.BeginAction(typeof(RottenTomato)))
                {
                    from.SendLocalizedMessage(501789); // You must wait before trying again.
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
                        from.PlaySound(458); /*splat!*/
                        from.PlaySound(307);

                        from.Animate(9, 1, 1, true, false, 0);

                        targ.SendMessage("You have just been hit by a rotten tomato!");
                        from.SendMessage("You throw the rotten tomato and hit the target!");

                        Effects.SendMovingEffect(from, targ, 0x36E4, 7, 0, false, true, 33, 0);

                        m_tomato.Consume();
                        //m_tomato.Delete();
                    }
                }
                else
                {
                    from.SendMessage("You can only throw a rotten tomato at something that can throw one back.");
                }
            }
        }
    }
}