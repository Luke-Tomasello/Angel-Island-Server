using Server.Spells;
using System;

namespace Server.Items
{
    /// <summary>
    /// Base class for all throwable incendiary devices (Firebomb, etc.).
    /// Handles arming, throwing, and fire field creation.
    /// </summary>
    public abstract class BaseIncendiaryDevice : Item
    {
        private bool m_Armed;
        private int m_FuelUnits;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Armed
        {
            get => m_Armed;
            set
            {
                m_Armed = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FuelUnits
        {
            get => m_FuelUnits;
            set
            {
                m_FuelUnits = value;
                InvalidateProperties();
            }
        }

        public virtual bool HeatSeeking => false;
        public virtual int ThrowRange => 8;
        public virtual TimeSpan BurnDuration => TimeSpan.FromSeconds(8.0);

        protected virtual int ThrowItemID => 0x1C19;   // Flaming bomb graphic
        protected virtual int ThrowSound => 0x11D;

        #region Constructors

        protected BaseIncendiaryDevice(int itemID, int fuelUnits)
            : base(itemID)
        {
            Weight = 1.0;
            m_FuelUnits = fuelUnits;
        }

        public BaseIncendiaryDevice(Serial serial)
            : base(serial)
        {
        }

        #endregion

        #region Usage

        public virtual void Ignite(Mobile from)
        {
            if (m_Armed)
            {
                from?.SendMessage("It is already ignited.");
                return;
            }

            m_Armed = true;
            InvalidateProperties();

            from?.SendMessage("The volatile mixture ignites.");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, m_Armed ? "(ignited)" : "(volatile)");
        }

        #endregion

        #region Throwing

        protected virtual void PlayThrowEffect(Mobile from, object target)
        {
            if (from == null || target == null)
                return;

            if (target is Mobile mobile)
            {
                from.MovingParticles(mobile, ThrowItemID, 3, 0, false, false, 0, 0, 9502, 6014, ThrowSound, EffectLayer.Waist, 0);
            }
            else if (target is IPoint3D p)
            {
                IEntity to = new Entity(Serial.Zero, new Point3D(p), from.Map);
                Effects.SendMovingParticles(from, to, ThrowItemID, 3, 0, false, false, 0, 0, 9502, 6014, ThrowSound, EffectLayer.Waist, 0);
            }
        }

        #endregion

        #region Fire Creation

        protected virtual void CreateFirePattern(Mobile from, Point3D loc, Map map)
        {
            if (from == null || map == null)
                return;

            Effects.PlaySound(loc, map, 0x20C); // Fire field sound

            new IncendiaryFireFieldItem(0x398C, loc, from, map, BurnDuration);
        }

        #endregion

        #region Serialization

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(m_Armed);
            writer.Write(m_FuelUnits);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_Armed = reader.ReadBool();
            m_FuelUnits = reader.ReadInt();
        }

        #endregion

        #region Nested Fire Field

        private class IncendiaryFireFieldItem : Item
        {
            private Timer m_Timer;
            private DateTime m_End;
            private Mobile? m_Caster;

            public override bool BlocksFit => true;

            public IncendiaryFireFieldItem(int itemID, Point3D loc, Mobile caster, Map map, TimeSpan duration)
                : base(itemID)
            {
                Movable = false;
                Light = LightType.Circle300;

                MoveToWorld(loc, map);

                m_Caster = caster;
                m_End = DateTime.UtcNow + duration;

                m_Timer = new InternalTimer(this);
                m_Timer.Start();
            }

            public IncendiaryFireFieldItem(Serial serial) : base(serial) { }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();
                m_Timer?.Stop();
            }

            public override bool OnMoveOver(Mobile m)
            {
                Burn(m);
                return true;
            }

            private void Burn(Mobile m)
            {
                if (m == null || m_Caster == null)
                    return;

                if (SpellHelper.ValidIndirectTarget(m_Caster, m) && m_Caster.CanBeHarmful(m, false))
                {
                    m_Caster.DoHarmful(m);
                    AOS.Damage(m, m_Caster, 2, new DamageContext { Source = this });
                    m.PlaySound(0x208);
                }
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);
                writer.Write(0);

                writer.Write(m_Caster);
                writer.WriteDeltaTime(m_End);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);
                int version = reader.ReadInt();

                m_Caster = reader.ReadMobile();
                m_End = reader.ReadDeltaTime();

                m_Timer = new InternalTimer(this);
                m_Timer.Start();
            }

            private class InternalTimer : Timer
            {
                private readonly IncendiaryFireFieldItem m_Item;

                public InternalTimer(IncendiaryFireFieldItem item)
                    : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
                {
                    m_Item = item;
                    Priority = TimerPriority.FiftyMS;
                }

                protected override void OnTick()
                {
                    if (m_Item.Deleted)
                    {
                        Stop();
                        return;
                    }

                    if (DateTime.UtcNow > m_Item.m_End)
                    {
                        m_Item.Delete();
                        Stop();
                        return;
                    }

                    foreach (Mobile m in m_Item.GetMobilesInRange(0))
                        m_Item.Burn(m);
                }
            }
        }

        #endregion

        #region Target Helper

        protected class IncendiaryTarget
        {
            private readonly Point3D m_Location;
            private readonly Map m_Map;
            private readonly Mobile? m_Mobile;

            public IncendiaryTarget(Point3D location, Map map)
            {
                m_Location = location;
                m_Map = map;
            }

            public IncendiaryTarget(Mobile mobile)
            {
                m_Mobile = mobile;
                if (mobile != null)
                {
                    m_Location = mobile.Location;
                    m_Map = mobile.Map;
                }
            }

            public bool Resolve(bool heatSeeking, out Point3D location, out Map map)
            {
                if (heatSeeking && m_Mobile?.Deleted == false && m_Mobile.Map != null)
                {
                    location = m_Mobile.Location;
                    map = m_Mobile.Map;
                    return true;
                }

                location = m_Location;
                map = m_Map;
                return map != null;
            }
        }

        #endregion
    }
}
