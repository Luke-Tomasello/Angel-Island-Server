using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.Items
{
    /// <summary>
    /// Firebomb - Throwable incendiary device made from various liquid containers.
    /// Supports different fuel sources (bottles, jugs, casks) with varying fire patterns.
    /// </summary>
    public class Firebomb : BaseIncendiaryDevice
    {
        public static bool FireBombHeatSeeking = false;

        private int m_MaxFuelUnits;
        private Type m_SourceType;           // Original beverage type used to create this bomb
        private Mobile? m_Thrower;            // Who threw this bomb (for ownership/credit)
        private IncendiaryTarget? m_DetonationTarget;

        #region Constructors

        [Constructable]
        public Firebomb()
            : this(1)
        {
        }

        [Constructable]
        public Firebomb(int fuelUnits)
            : this(fuelUnits, fuelUnits, typeof(BeverageBottle), 0x99B)
        {
        }

        public Firebomb(int fuelUnits, int maxFuelUnits, Type sourceType, int itemID)
            : base(itemID, fuelUnits)
        {
            m_MaxFuelUnits = maxFuelUnits;
            m_SourceType = sourceType;

            Name = "firebomb";
            Weight = 2.0;
        }

        public Firebomb(Serial serial)
            : base(serial)
        {
        }

        #endregion

        #region Properties

        [CommandProperty(AccessLevel.GameMaster)]
        public string SourceType => m_SourceType?.Name ?? "(unknown)";

        public override bool HeatSeeking => FireBombHeatSeeking;

        public override TimeSpan BurnDuration
        {
            get
            {
                if (m_MaxFuelUnits <= 0)
                    return TimeSpan.FromSeconds(8.0);

                double scale = (double)FuelUnits / m_MaxFuelUnits;
                double seconds = Math.Max(1.0, 8.0 * scale);

                return TimeSpan.FromSeconds(seconds);
            }
        }

        private bool IsFromJug => m_SourceType == typeof(Jug);
        private bool IsFromCask => m_SourceType == typeof(Cask);

        #endregion

        #region Usage

        public override void OnDoubleClick(Mobile from)
        {
            if (Armed || Deleted)
                return;

            // if firebomb throw delay is > 0, check it
            if (CoreAI.ExplosionPotionThrowDelay > 0.0)
            {
                if (from is PlayerMobile pm)
                {
                    if (pm.CanThrowNow() == false)
                    {
                        from.SendMessage("You fail to arm the firebomb.");
                        return;
                    }
                }
            }

            m_Thrower = from;
            Armed = true;

            //reset from's swingtimer
            BaseWeapon? weapon = from.Weapon as BaseWeapon;
            if (weapon != null)
            {
                from.NextCombatTime = DateTime.UtcNow + weapon.GetDelay(from);
            }

            // we will also reveal on target.
            from.RevealingAction();

            from.PlaySound(0x47); // ignition sound

            from.Target = new InternalTarget(this);

            // Auto-detonate after 3 seconds if not thrown
            Timer.DelayCall(TimeSpan.FromSeconds(3.0), Explode, new IncendiaryTarget(from));
        }

        #endregion

        #region Detonation

        private void Explode(object state)
        {
            if (Deleted)
                return;

            IncendiaryTarget? target = m_DetonationTarget ?? state as IncendiaryTarget;

            if (target == null || m_Thrower?.Deleted == true)
            {
                Delete();
                return;
            }

            if (target.Resolve(HeatSeeking, out Point3D loc, out Map map))
                CreateFirePattern(m_Thrower, loc, map);

            m_DetonationTarget = null;
            Delete();
        }
        private static readonly string[] m_FirebombIgnitionMessages =
        {
            "Somebody set up us the bomb!", // easter egg!
            "The volatile mixture ignites in your hands!",
            "You accidentally ignite the firebomb!",
            "The unstable mixture erupts into flame!",
            "The firebomb ignites prematurely!",
            "You mishandle the volatile mixture and ignite it!",
            "The liquor-soaked cloth bursts into flame!",
            "The improvised firebomb detonates in your grasp!",
            "You set off the firebomb!",
            "The volatile contents erupt violently!",
            "The firebomb flares to life in your hands!",
            "You fumble with the firebomb and ignite it!",
            "The unstable incendiary mixture combusts unexpectedly!",
            "You have ignited the firebomb!",
            "The firebomb explodes in a burst of flame!",
            "You are engulfed in flames!",
            "You burn yourself while handling the firebomb!",
            "The volatile device ignites unexpectedly!"
        };
        public void Misuse(Mobile from)
        {
            if (from == null || from.Deleted)
                return;

            Point3D loc = from.Location;
            Map map = from.Map;

            from.SendAsciiMessage(Utility.Random(m_FirebombIgnitionMessages));

            if (map != null)
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(loc, map, EffectItem.DefaultDuration),
                    0x3709,
                    10,
                    30,
                    5052);

                Effects.PlaySound(loc, map, 0x208);
            }

            AOS.Damage(from, from, GetMisuseDamage(), new DamageContext { Source = this });

            Delete();
        }
        private int GetMisuseDamage()
        {
            int min;
            int max;

            if (IsFromCask)
            {
                min = 30;
                max = 50;
            }
            else if (IsFromJug)
            {
                min = 15;
                max = 25;
            }
            else
            {
                min = 8;
                max = 14;
            }

            double scale = 1.0;

            if (m_MaxFuelUnits > 0)
                scale = (double)FuelUnits / (double)m_MaxFuelUnits;

            scale = Math.Max(0.20, Math.Min(1.0, scale));

            min = Math.Max(1, (int)(min * scale));
            max = Math.Max(min, (int)(max * scale));

            return Utility.RandomMinMax(min, max);
        }
        protected override void CreateFirePattern(Mobile from, Point3D loc, Map map)
        {
            if (IsFromCask)
            {
                // Large area fire
                base.CreateFirePattern(from, loc, map);
                for (int i = 0; i < 4; i++)
                    base.CreateFirePattern(from, GetRandomAdjacentLocation(loc), map);
            }
            else if (IsFromJug)
            {
                // Medium spread
                base.CreateFirePattern(from, loc, map);
                base.CreateFirePattern(from, GetRandomAdjacentLocation(loc), map);
            }
            else
            {
                // Standard small firebomb
                base.CreateFirePattern(from, loc, map);
            }
        }

        private static Point3D GetRandomAdjacentLocation(Point3D loc)
        {
            return Utility.Random(8) switch
            {
                0 => new Point3D(loc.X + 1, loc.Y, loc.Z),
                1 => new Point3D(loc.X - 1, loc.Y, loc.Z),
                2 => new Point3D(loc.X, loc.Y + 1, loc.Z),
                3 => new Point3D(loc.X, loc.Y - 1, loc.Z),
                4 => new Point3D(loc.X + 1, loc.Y + 1, loc.Z),
                5 => new Point3D(loc.X - 1, loc.Y - 1, loc.Z),
                6 => new Point3D(loc.X + 1, loc.Y - 1, loc.Z),
                _ => new Point3D(loc.X - 1, loc.Y + 1, loc.Z)
            };
        }

        #endregion

        #region Factory Method

        public static Firebomb? CreateFrom(BaseBeverage beverage)
        {
            if (beverage == null || beverage.Quantity <= 0)
                return null;

            return new Firebomb(
                Math.Max(1, beverage.Quantity),
                beverage.MaxQuantity,
                beverage.GetType(),
                beverage.ItemID);
        }

        #endregion

        #region Serialization

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(m_SourceType?.FullName);
            writer.Write(m_MaxFuelUnits);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            string? typeName = reader.ReadString();
            m_SourceType = ScriptCompiler.FindTypeByFullName(typeName);
            m_MaxFuelUnits = reader.ReadInt();
        }

        #endregion

        #region Target

        private class InternalTarget : Target
        {
            private readonly Firebomb m_Firebomb;

            public InternalTarget(Firebomb firebomb)
                : base(firebomb.ThrowRange, true, TargetFlags.None)
            {
                m_Firebomb = firebomb;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Firebomb?.Deleted != false)
                    return;

                // Also reveal in double-click/arm
                from.RevealingAction();

                // throw timing
                if (from is PlayerMobile pm && CoreAI.ExplosionPotionThrowDelay > 0)
                {
                    pm.SetThrowDelay(CoreAI.ExplosionPotionThrowDelay);
                }

                m_Firebomb.PlayThrowEffect(from, targeted);

                if (targeted is Mobile mobile)
                    m_Firebomb.SetDetonationTarget(new IncendiaryTarget(mobile));
                else if (targeted is IPoint3D p)
                    m_Firebomb.SetDetonationTarget(new IncendiaryTarget(new Point3D(p), from.Map));
            }
        }

        protected void SetDetonationTarget(IncendiaryTarget target)
        {
            m_DetonationTarget = target;
        }

        #endregion
    }
}
