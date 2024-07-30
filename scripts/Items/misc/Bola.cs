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

/* Items/Misc/Bola.cs
 * CHANGELOG
 *  1/3/24, Yoar (BolaImmune)
 *      Added BaseCreature.BolaImmune. If true, the creature cannot be bola'd.
 *  5/31/23, Yoar
 *      Added bola success chance based on thrower's tactics skill
 *      Seems to be era accurate
 *      https://wiki.stratics.com/index.php?title=UO:Publish_Notes_from_2002-07-12
 *      Bolas now have a chance to be recovered for all shards
 *  5/15/23, Yoar
 *      Added configurable bola lock delays
 *      Added LockTimer with increased priority
 *  4/2/23, Yoar
 *      Added 'IsAngelIsland' switch to enable/disable Angel Island Entangle ability.
 *  04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 *	09/09/05 TK
 *		Serialized crafter, uses and quality.
 *	09/06/05 TK
 *		Returned ability to dismount creatures using bolas. Difficulty is still a factor but is 25% easier than tying someone up with it.
 *  09/01/05 TK
 *		Added creature's name to emotes
 *  08/31/05 Taran Kain
 *		Added RevealingAction()s, and made a StandingDelay check a la Archery. Thrower must stand still for 0.5 sec or it will automatically miss.
 *	08/30/05 Taran Kain
 *		Changed bolas from a dismounting tool to a creature-freezing tool. They now tie up critter's feet so that they can't move, but can still cast and attack.
 */

using Server.Engines.Craft;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Items
{
    public class Bola : Item, ICraftable
    {
        private WeaponQuality m_Quality;
        private int m_Uses;
        private MakersMark m_Crafter;

        [Constructable]
        public Bola()
            : base(0x26AC)
        {
            Weight = 4.0;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MakersMark Crafter
        {
            get { return m_Crafter; }
            set { m_Crafter = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponQuality Quality
        {
            get { return m_Quality; }
            set { m_Quality = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses
        {
            get { return m_Uses; }
            set { m_Uses = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxUses
        {
            get
            {
                switch (Quality)
                {
                    case WeaponQuality.Low: return 2;
                    case WeaponQuality.Regular: return 4;
                    case WeaponQuality.Exceptional: return 6;
                    default: return 0;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double StandingDelay
        {
            get
            {
                return 0.5;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1040019); // The bola must be in your pack to use it.
            }
            else if (!from.CanBeginAction(typeof(Bola)))
            {
                from.SendLocalizedMessage(1049624); // You have to wait a few moments before you can use another bola!
            }
            else if (from.Target is BolaTarget)
            {
                from.SendLocalizedMessage(1049631); // This bola is already being used.
            }
            else if (from.FindItemOnLayer(Layer.OneHanded) != null || from.FindItemOnLayer(Layer.TwoHanded) != null)
            {
                from.SendLocalizedMessage(1040015); // Your hands must be free to use this
            }
            else if (from.Mounted)
            {
                from.SendLocalizedMessage(1040016); // You cannot use this while riding a mount
            }
            else
            {
                from.Target = new BolaTarget(this);
                from.LocalOverheadMessage(MessageType.Emote, 0x3B2, 1049632); // * You begin to swing the bola...*
                from.NonlocalOverheadMessage(MessageType.Emote, 0x3B2, 1049633, from.Name); // ~1_NAME~ begins to menacingly swing a bola...
                from.RevealingAction();
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            int number;

            if (Name == null)
            {
                number = LabelNumber;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            ArrayList attrs = new ArrayList();

            if (Quality == WeaponQuality.Exceptional)
                attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));

            EquipmentInfo eqInfo = new EquipmentInfo(number, m_Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));

            from.Send(new DisplayEquipmentInfo(this, eqInfo));
        }

#if false
        private static void ReleaseBolaLock(object state)
        {
            ((Mobile)state).EndAction(typeof(Bola));
        }

        private static void ReleaseMountLock(object state)
        {
            ((Mobile)state).EndAction(typeof(BaseMount));
        }
#endif

        public static void BeginBolaLock(Mobile m)
        {
            m.BeginAction(typeof(Bola));

            (new LockTimer(m, typeof(Bola), TimeSpan.FromSeconds(CoreAI.BolaThrowLock))).Start();
        }

        public static void BeginMountLock(Mobile m)
        {
            m.BeginAction(typeof(BaseMount));

            (new LockTimer(m, typeof(BaseMount), TimeSpan.FromSeconds(CoreAI.BolaMountLock))).Start();
        }

        private class LockTimer : Timer
        {
            private Mobile m_Mobile;
            private object m_ToLock;

            public LockTimer(Mobile mobile, object toLock, TimeSpan delay)
                : base(delay)
            {
                if (Priority > TimerPriority.TwentyFiveMS)
                    Priority = TimerPriority.TwentyFiveMS;

                m_Mobile = mobile;
                m_ToLock = toLock;
            }

            protected override void OnTick()
            {
                m_Mobile.EndAction(m_ToLock);
            }
        }

        private static void RestoreCurrentSpeed(object state)
        {
            object[] states = (object[])state;
            Mobile bc = (Mobile)states[0];
            Bola b = (Bola)states[1];

            bc.RevealingAction();
            bc.CantWalkLand = false;

            b.Visible = true;

            if (b.Checkbreak())
                bc.NonlocalOverheadMessage(MessageType.Emote, 0x3B2, true, "*" + bc.Name + " frees itself by ripping apart the bola*");
            else
                bc.NonlocalOverheadMessage(MessageType.Emote, 0x3B2, true, "*" + bc.Name + " frees itself from the bola*");
        }

        private static bool VerifyThrow(Bola bola, Mobile from, Mobile to)
        {
            if (!bola.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1040019); // The bola must be in your pack to use it.
            }
            else if (from.FindItemOnLayer(Layer.OneHanded) != null || from.FindItemOnLayer(Layer.TwoHanded) != null)
            {
                from.SendLocalizedMessage(1040015); // Your hands must be free to use this
            }
            else if (from.Mounted)
            {
                from.SendLocalizedMessage(1040016); // You cannot use this while riding a mount
            }
            else if (!from.CanSee(to) || !from.InLOS(to))
            {
                from.SendLocalizedMessage(1042060); // You cannot see that target!
            }
            else if ((Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()) && !(to is BaseCreature))
            {
                from.SendLocalizedMessage(1049629); // You cannot throw a bola at that.
            }
            else if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisRules() && !to.Mounted)
            {
                from.SendLocalizedMessage(1049628); // You have no reason to throw a bola at that.
            }
            else if (to is BaseCreature && ((BaseCreature)to).BolaImmune)
            {
                from.SendLocalizedMessage(1049629); // You cannot throw a bola at that.
            }
            else if (!from.CanBeHarmful(to))
            {
            }
            else
            {
                return true;
            }

            return false;
        }

        private static void FinishThrow(object state)
        {
            object[] states = (object[])state;

            Mobile from = (Mobile)states[0];
            Mobile to = (Mobile)states[1];
            Bola b = (Bola)states[2];

            if (!VerifyThrow(b, from, to))
            {
                from.EndAction(typeof(Bola));
                return;
            }

            from.RevealingAction();

            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
            {
                #region AI/MO Difficulty Check

                double diff = b.GetDifficultyFor(to);

                if (to.Mounted)
                    diff -= 25.0; // easier to knock someone off a horse than it is to tie them up

                if (!from.CheckTargetSkill(SkillName.Tactics, to, diff - 25.0, diff + 25.0, new object[2] { to, null } /*contextObj*/) || DateTime.UtcNow < (from.LastMoveTime + TimeSpan.FromSeconds(StandingDelay)))
                {
                    from.SendMessage("You throw the bola but miss!");

                    Map map = to.Map;

                    if (map == null || b.Checkbreak())
                    {
                        b.Delete();
                    }
                    else
                    {
                        int x = to.X + Utility.RandomMinMax(-2, +2);
                        int y = to.Y + Utility.RandomMinMax(-2, +2);
                        int z = to.Z;

                        Point3D p = new Point3D(x, y, z);

                        if (map.CanFit(p, 16) || map.CanFit(p = new Point3D(p, map.GetAverageZ(x, y)), 16))
                            b.MoveToWorld(p, map);
                        else
                            b.Delete();
                    }

                    BeginBolaLock(from);
                    return;
                }

                #endregion
            }

            if ((Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()) && !to.Mounted)
            {
                #region AI/MO Freeze Target

                b.MoveToWorld(to.Location, to.Map);
                b.Visible = false;

                to.CantWalkLand = true;
                to.NonlocalOverheadMessage(MessageType.Emote, 0x3B2, true, "*" + to.Name + " becomes entangled in the bola*");

                double duration = 7.0 + from.Skills[SkillName.Anatomy].Value * 0.1 + from.Dex * 0.1 - to.Str / 150.0;

                Timer.DelayCall(TimeSpan.FromSeconds(duration), new TimerStateCallback(RestoreCurrentSpeed), new object[] { to, b });

                #endregion
            }
            else if (to.Mounted && PublishInfo.Publish < 16.0 && Utility.RandomDouble() >= from.Skills[SkillName.Tactics].Value / 100.0)
            {
                from.SendLocalizedMessage(1040022); // You fail to knock the rider from their mount.

                if (b.Checkbreak())
                    b.Delete();
                else
                    b.MoveToWorld(to.Location, to.Map);
            }
            else if (to.Mounted)
            {
                IMount mt = to.Mount;

                if (mt != null)
                    mt.Rider = null;

                to.SendLocalizedMessage(1040023); // You have been knocked off of your mount!

                if (PublishInfo.Publish >= 16.0)
                    to.Damage(1, from, b);

                BeginMountLock(to);

                if (b.Checkbreak())
                    b.Delete();
                else
                    b.MoveToWorld(to.Location, to.Map);
            }

            BeginBolaLock(from);
        }

        private bool Checkbreak()
        {
            // TODO: The following is AI/MO behavior. What is the era-accurate behavior?
            if (++Uses >= MaxUses || Utility.RandomDouble() < 0.30)
            {
                Delete();
                return true;
            }

            return false;
        }

        private class BolaTarget : Target
        {
            private Bola m_Bola;

            public BolaTarget(Bola bola)
                : base(8, false, TargetFlags.Harmful)
            {
                m_Bola = bola;
            }

            protected override void OnTarget(Mobile from, object obj)
            {
                if (m_Bola.Deleted)
                    return;

                if (obj is Mobile)
                {
                    Mobile to = (Mobile)obj;

                    if (!VerifyThrow(m_Bola, from, to))
                        return;

                    if (from.BeginAction(typeof(Bola)))
                    {
                        from.DoHarmful(to);

                        from.Direction = from.GetDirectionTo(to);
                        from.Animate(11, 5, 1, true, false, 0);
                        from.MovingEffect(to, 0x26AC, 10, 0, false, false);

                        Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(FinishThrow), new object[] { from, to, m_Bola });
                    }
                    else
                    {
                        from.SendLocalizedMessage(1049624); // You have to wait a few moments before you can use another bola!
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1049629); // You cannot throw a bola at that.
                }
            }
        }

        public Bola(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Bola(amount), amount);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2);

            m_Crafter.Serialize(writer);
            writer.Write((int)m_Quality);
            writer.Write((int)m_Uses);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                case 1:
                    {
                        if (version >= 2)
                            m_Crafter.Deserialize(reader);
                        else
                            m_Crafter = reader.ReadMobile();

                        m_Quality = (WeaponQuality)reader.ReadInt();
                        m_Uses = reader.ReadInt();
                        break;
                    }
            }
        }

        #region Difficulty

        public static bool IsMageryCreature(BaseCreature bc)
        {
            return (bc != null && bc.AI == AIType.AI_Mage && bc.Skills[SkillName.Magery].Base > 5.0);
        }

        public static bool IsFireBreathingCreature(BaseCreature bc)
        {
            if (bc == null)
                return false;

            return bc.HasBreath;
        }

        public static bool IsPoisonImmune(BaseCreature bc)
        {
            return (bc != null && bc.PoisonImmune != null);
        }

        public static int GetPoisonLevel(BaseCreature bc)
        {
            if (bc == null)
                return 0;

            Poison p = bc.HitPoison;

            if (p == null)
                return 0;

            return p.Level + 1;
        }

        public double GetDifficultyFor(Mobile targ)
        {
            double val = targ.Hits + targ.Stam + targ.Mana;

            for (int i = 0; i < targ.Skills.Length; i++)
                val += targ.Skills[i].Base;

            if (val > 700)
                val = 700 + ((val - 700) / 3.66667);

            BaseCreature bc = targ as BaseCreature;

            if (IsMageryCreature(bc))
                val += 100;

            if (IsFireBreathingCreature(bc))
                val += 100;

            if (IsPoisonImmune(bc))
                val += 100;

            if (targ is VampireBat || targ is VampireBatFamiliar)
                val += 100;

            if (targ is WraithRiderWarrior)
                val += 400;

            if (targ is WraithRiderMage)
                val += 300;

            if (targ is BaseHealer)
                val += 800;

            val += GetPoisonLevel(bc) * 20;

            val /= 10;

            if (m_Quality == WeaponQuality.Exceptional)
            {
                val -= 15.0; // 30% bonus for exceptional
            }

            val -= 10.0; // peacemake has 10 less difficulty, that's what we're goin for here

            return val;
        }

        #endregion

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            Quality = (WeaponQuality)quality;

            if (makersMark)
                Crafter = from;

            return quality;
        }

        #endregion
    }
}