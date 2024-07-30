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

/* Scripts/Items/Special/Holiday/Halloween/HolyHandGrenade.cs
 * CHANGELOG
 *  10/13/23, Yoar
 *      Renamed to "consecrated water"
 *  10/6/23, Yoar
 *      Initial commit.
 */

using Server.Engines.Alignment;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class HolyHandGrenade : Item
    {
#if false
        public override string DefaultName { get { return "holy hand grenade"; } }

        public override string OldName { get { return "holy hand grenade%s"; } }
        public override Article OldArticle { get { return Article.The; } }
#else
        public override string DefaultName { get { return "consecrated water"; } }
#endif

        public static int ExplodeTicks = 3;
        public static int ExplodeRange = 4;
        public static int EffectChance = 15;
        public static int MinDamage = 90;
        public static int MaxDamage = 120;
        public static int CooldownDuration = 10;

        private ThrowTimer m_ThrowTimer;
        private ExplodeTimer m_ExplodeTimer;
        private List<Mobile> m_Users;

        [Constructable]
        public HolyHandGrenade()
            : this(1)
        {
        }

        [Constructable]
        public HolyHandGrenade(int amount)
            : base(0xF0E)
        {
            Hue = 1169;
            Stackable = true;
            Amount = amount;
            m_Users = new List<Mobile>();
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new HolyHandGrenade(), amount);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (m_ExplodeTimer == null && InCooldown(from))
            {
#if false
                from.SendMessage("You must wait before using another holy hand grenade.");
#else
                from.SendMessage("You must wait before using another bottle of consecrated water.");
#endif
            }
            else if (!BasePotion.HasFreeHand(from))
            {
                from.SendLocalizedMessage(502172); // You must have a free hand to drink a potion.
            }
            else
            {
                if (Amount > 1)
                    Unstack();

                BeginThrow(from);
                StartExplodeTimer(from);

                if (CooldownDuration > 0)
                    BeginCooldown(from);
            }
        }

        private void BeginThrow(Mobile from)
        {
            if (!m_Users.Contains(from))
                m_Users.Add(from);

            from.Target = new ThrowTarget(this);
        }

        private void Throw(Mobile from, object targeted)
        {
            IPoint3D p = targeted as IPoint3D;

            if (p == null)
                return;

            Map map = from.Map;

            if (map == null)
                return;

            m_Users.Remove(from);

            from.RevealingAction();

            Effects.SendMovingEffect(from, EffectItem.Create(new Point3D(p), map, EffectItem.DefaultDuration), ItemID, 7, 0, false, false, Math.Max(0, Hue - 1), 0);

            if (Amount > 1)
                Unstack();

            Visible = false;
            MoveToWorld(new Point3D(p), map);
            StartThrowTimer(from, TimeSpan.FromSeconds(1.0));
        }

        private void EndThrow()
        {
            StopThrowTimer();

            Visible = true;
        }

        private void OnTick(Mobile from, int tick)
        {
            if (tick <= ExplodeTicks)
            {
                object parent = FindParent();

                int num = tick;

                if (num == 3 && ExplodeTicks == 3 && Utility.RandomDouble() < 0.05)
                    num = 5; // 1-2-5

                if (parent is Item)
                    ((Item)parent).PublicOverheadMessage(MessageType.Regular, 0x31, false, num.ToString());
                else if (parent is Mobile)
                    ((Mobile)parent).PublicOverheadMessage(MessageType.Regular, 0x31, false, num.ToString());
            }
            else
            {
                Explode(from);
            }
        }

        private void Explode(Mobile from)
        {
            Map map = Map;

            if (map == null)
            {
                Delete();
                return;
            }

            Visible = true;

            Point3D loc = GetWorldLocation();

            List<Point3D> effectLocs = new List<Point3D>();

            effectLocs.Add(loc);

            for (int y = loc.Y - ExplodeRange; y <= loc.Y + ExplodeRange; y++)
            {
                for (int x = loc.X - ExplodeRange; x <= loc.X + ExplodeRange; x++)
                {
                    if (Utility.Random(100) < EffectChance)
                    {
                        int zSurf;

                        map.GetTopSurface(new Point3D(x, y, loc.Z + 10), out zSurf);

                        effectLocs.Add(new Point3D(x, y, Math.Max(loc.Z, zSurf)));
                    }
                }
            }

            List<Mobile> targets = new List<Mobile>();

            foreach (Mobile m in GetMobilesInRange(ExplodeRange))
            {
                BaseCreature bc = m as BaseCreature;

                if (bc != null && IsUndead(bc))
                    targets.Add(m);
            }

            Effects.SendLocationParticles(EffectItem.Create(loc, map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
            Effects.PlaySound(loc, map, 0x299);

            foreach (Point3D effectLoc in effectLocs)
            {
                Effects.SendLocationParticles(EffectItem.Create(effectLoc, map, EffectItem.DefaultDuration), 0x376A, 9, 32, 0, 0, 5030, 0);
                Effects.PlaySound(loc, map, 0x202);
            }

            foreach (Mobile target in targets)
            {
                if (from != null)
                    from.DoHarmful(target);

                target.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                target.PlaySound(0x1E0);

                target.Damage(Utility.RandomMinMax(MinDamage, MaxDamage), from, this);
            }

            Delete();
        }

        private object FindParent()
        {
            Mobile heldBy = HeldBy;

            if (heldBy != null && heldBy.Holding == this)
                return heldBy;

            object root = RootParent;

            if (root != null)
                return root;

            if (!Visible && m_ThrowTimer != null && m_ThrowTimer.From != null)
                return m_ThrowTimer.From;

            return this;
        }

        private void Unstack()
        {
            Item stack = new HolyHandGrenade(Amount - 1);

            stack.Location = Location;
            stack.Map = Map;

            if (Parent is Mobile)
                ((Mobile)Parent).AddItem(stack);
            else if (Parent is Item)
                ((Item)Parent).AddItem(stack);

            Amount = 1;
        }

        private static bool IsUndead(BaseCreature bc)
        {
            if (bc.GuildAlignment == AlignmentType.Undead)
                return true;

            SlayerEntry entry = SlayerGroup.GetEntryByName(SlayerName.Silver);

            if (entry != null && InTypeList(entry.Types, bc.GetType()))
                return true;

            return false;
        }

        private static bool InTypeList(Type[] types, Type type)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].IsAssignableFrom(type))
                    return true;
            }

            return false;
        }

        private class ThrowTarget : Target
        {
            private HolyHandGrenade m_Item;

            public HolyHandGrenade Item { get { return m_Item; } }

            public ThrowTarget(HolyHandGrenade item)
                : base(8, true, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted || !from.InRange(m_Item.GetWorldLocation(), 2) || !from.InLOS(m_Item))
                    return;

                m_Item.Throw(from, targeted);
            }
        }

        private void StartThrowTimer(Mobile from, TimeSpan delay)
        {
            StopThrowTimer();

            (m_ThrowTimer = new ThrowTimer(this, from, delay)).Start();
        }

        private void StopThrowTimer()
        {
            if (m_ThrowTimer != null)
            {
                m_ThrowTimer.Stop();
                m_ThrowTimer = null;
            }
        }

        private class ThrowTimer : Timer
        {
            private HolyHandGrenade m_Item;
            private Mobile m_From;

            public Mobile From { get { return m_From; } }

            public ThrowTimer(HolyHandGrenade item, Mobile from, TimeSpan delay)
                : base(delay)
            {
                m_Item = item;
                m_From = from;
            }

            protected override void OnTick()
            {
                if (m_Item.Deleted)
                    return;

                m_Item.EndThrow();
            }
        }

        private void StartExplodeTimer(Mobile from)
        {
            StopExplodeTimer();

            (m_ExplodeTimer = new ExplodeTimer(this, from)).Start();
        }

        private void StopExplodeTimer()
        {
            if (m_ExplodeTimer != null)
            {
                m_ExplodeTimer.Stop();
                m_ExplodeTimer = null;
            }
        }

        private class ExplodeTimer : Timer
        {
            private HolyHandGrenade m_Item;
            private Mobile m_From;
            private int m_Tick;

            public ExplodeTimer(HolyHandGrenade item, Mobile from)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Item = item;
                m_From = from;
            }

            protected override void OnTick()
            {
                if (m_Item.Deleted)
                    return;

                m_Item.OnTick(m_From, ++m_Tick);
            }
        }

        private static readonly Dictionary<Mobile, Timer> m_CooldownTimers = new Dictionary<Mobile, Timer>();

        private static bool InCooldown(Mobile m)
        {
            return m_CooldownTimers.ContainsKey(m);
        }

        private static void BeginCooldown(Mobile m)
        {
            EndCooldown(m);

            (m_CooldownTimers[m] = new CooldownTimer(m)).Start();
        }

        private static void EndCooldown(Mobile m)
        {
            Timer timer;

            if (m_CooldownTimers.TryGetValue(m, out timer))
            {
                timer.Stop();
                m_CooldownTimers.Remove(m);
            }
        }

        private class CooldownTimer : Timer
        {
            private Mobile m_Mobile;

            public CooldownTimer(Mobile m)
                : base(TimeSpan.FromSeconds(CooldownDuration))
            {
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                EndCooldown(m_Mobile);
            }
        }

        public override void OnAfterDelete()
        {
            StopThrowTimer();
            StopExplodeTimer();

            foreach (Mobile m in m_Users)
            {
                ThrowTarget target = m.Target as ThrowTarget;

                if (target != null && target.Item == this)
                    Target.Cancel(m);
            }

            m_Users.Clear();
        }

        public HolyHandGrenade(Serial serial)
            : base(serial)
        {
            m_Users = new List<Mobile>();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((bool)(m_ThrowTimer != null));
            writer.Write((bool)(m_ExplodeTimer != null));
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            bool explode = false;

            switch (version)
            {
                case 0:
                    {
                        if (reader.ReadBool())
                            Visible = true;

                        if (reader.ReadBool())
                            explode = true;

                        break;
                    }
            }

            if (explode)
                Delete();
        }
    }
}