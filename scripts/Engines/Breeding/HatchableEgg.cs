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

/* Scripts\Engines\Breeding\HatchableEgg.cs
 * Changelog:
 * 10/20/21, Yoar: Breeding System overhaul
 *      Initial version. BaseHatchableEgg defines a base class for all hatchable eggs
 *      obtained through the breeding system. HatchableEgg defines a default hatchable
 *      egg that may be used for every type of (egg-laying) creature.
 */

using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Engines.Breeding
{
    public enum HatchState : byte
    {
        Rustle,
        Crack,
        Beak,
        Split,
        Hatch
    }

    public interface IEgg
    {
        bool IsFertile { get; }
    }

    public abstract class BaseHatchableEgg : CookableFood, IEgg
    {
        private static readonly List<BaseHatchableEgg> m_Eggs = new List<BaseHatchableEgg>();

        public static List<BaseHatchableEgg> Eggs { get { return m_Eggs; } }

        public static void Initialize()
        {
            new InternalTimer().Start();
        }

        private class InternalTimer : Timer
        {
            public InternalTimer()
                : base(TimeSpan.Zero, TimeSpan.FromMinutes(5.0))
            {
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                for (int i = m_Eggs.Count - 1; i >= 0; i--)
                {
                    BaseHatchableEgg egg = m_Eggs[i];

                    if (!egg.Slice())
                        m_Eggs.RemoveAt(i);
                }
            }
        }

        public override double DefaultWeight { get { return 8.0; } }

        public virtual TimeSpan HatchTime
        {
            get
            {
                double days = Utility.RandomMinMax(5, 7);

                if (Core.UOTC_CFG)
                    days /= 360; // speed up 360x

                return TimeSpan.FromDays(days);
            }
        }

        public virtual int HealthMax { get { return 24; } }
        public virtual int HealthInit { get { return 5; } }
        public virtual string BeakMessage { get { return "You can see a beak!"; } }

        private BaseCreature m_Chick;
        private DateTime m_Birthdate;
        private int m_Health;

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Chick
        {
            get { return m_Chick; }
            set { m_Chick = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime Birthdate
        {
            get { return m_Birthdate; }
            set { m_Birthdate = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan HatchesIn
        {
            get
            {
                TimeSpan ts = m_Birthdate - DateTime.UtcNow;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
            set { m_Birthdate = DateTime.UtcNow + value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Health
        {
            get { return m_Health; }
            set
            {
                if (value > HealthMax)
                    value = HealthMax;

                m_Health = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Registered
        {
            get { return m_Eggs.Contains(this); }
            set
            {
                if (value)
                {
                    if (!m_Eggs.Contains(this))
                        m_Eggs.Add(this);
                }
                else
                {
                    m_Eggs.Remove(this);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Hatch
        {
            get { return false; }
            set
            {
                if (value)
                    HatchEnd();
            }
        }

        // note: chick may be null if still born
        public BaseHatchableEgg(BaseCreature chick)
            : base(0x1365, 80)
        {
            Hue = 1150;
            m_Chick = chick;
            m_Birthdate = DateTime.UtcNow + HatchTime;
            m_Health = HealthInit;

            if (m_Chick != null)
            {
                m_Chick.MoveToIntStorage();
                m_Chick.Maturity = Maturity.Egg;
            }

            m_Eggs.Add(this);
        }

        private bool Slice()
        {
            if (m_Chick == null || m_Chick.Deleted)
                return false;

            Map map = this.Map;

            if (map == null)
                return true; // do nothing

            TimeSpan ts = this.HatchesIn;

            if (ts == TimeSpan.Zero && Health > 0)
            {
                int clients = 0;

                foreach (NetState ns in map.GetClientsInRange(this.GetWorldLocation()))
                    clients++;

                // wait until someone's around to actually begin hatching
                if (clients != 0)
                {
                    DoDelayed(TimeSpan.Zero, TimeSpan.FromSeconds(1.0), Hatch_OnTick, HatchState.Rustle);
                    return false; // we're done! remove us from the list
                }
            }

            double hours = ts.TotalHours;

            if (Utility.RandomDouble() < (hours * hours / 324)) // (ts.TotalHours / 18) ^ 2
                DoDelayed(TimeSpan.FromMinutes(Utility.RandomDouble() * 4.0), CheckRustle, null);

            // adjust health accordingly
            if (RootParent is Mobile || IsLockedDown)
                Health++;
            else
                Health--;

            if (Health < -5)
            {
                // we die
                m_Chick.Delete();
                m_Birthdate = DateTime.MinValue;

                return false; // remove us from tick list
            }

            return true;
        }

        private void CheckRustle(object obj)
        {
            if (this.Deleted || this.Map == null)
                return;

            Rustle();
        }

        private void Rustle()
        {
            PublicOverheadMessage(MessageType.Regular, 0, true, "*rustles*");
            Effects.PlaySound(this.GetWorldLocation(), this.Map, Utility.RandomBool() ? 826 : 827);
        }

        private void Hatch_OnTick(object obj)
        {
            if (this.Deleted || this.Map == null || m_Chick == null || m_Chick.Deleted || !(obj is HatchState))
            {
                StopTimer();
                return;
            }

            switch ((HatchState)obj)
            {
                case HatchState.Rustle:
                    {
                        if (Utility.RandomDouble() < 0.40)
                            Rustle();

                        if (Utility.RandomDouble() < 0.05)
                            DoDelayed(TimeSpan.FromSeconds(Utility.RandomDouble() + 2.0), TimeSpan.FromSeconds(2.0), Hatch_OnTick, HatchState.Crack);

                        break;
                    }
                case HatchState.Crack:
                    {
                        if (Utility.RandomDouble() < 0.70)
                        {
                            Effects.PlaySound(this.GetWorldLocation(), this.Map, Utility.RandomList(828, 829));
                            PublicOverheadMessage(MessageType.Regular, 0, true, "You notice some cracks!");
                        }

                        if (Utility.RandomDouble() < 0.20)
                            DoDelayed(TimeSpan.FromSeconds(Utility.RandomDouble() + 4.0), TimeSpan.FromSeconds(1.0), Hatch_OnTick, HatchState.Beak);

                        break;
                    }
                case HatchState.Beak:
                    {
                        if (Utility.RandomDouble() < 0.40)
                        {
                            Effects.PlaySound(this.GetWorldLocation(), this.Map, Utility.RandomList(828, 829));
                            PublicOverheadMessage(MessageType.Regular, 0, true, BeakMessage);
                        }

                        if (Utility.RandomDouble() < 0.10)
                            DoDelayed(TimeSpan.FromSeconds(Utility.RandomDouble() + 3.0), Hatch_OnTick, HatchState.Split);

                        break;
                    }
                case HatchState.Split:
                    {
                        Effects.PlaySound(this.GetWorldLocation(), this.Map, Utility.RandomList(308, 309));
                        PublicOverheadMessage(MessageType.Regular, 0, true, "The egg splits open!");

                        DoDelayed(TimeSpan.FromSeconds(Utility.RandomDouble() * 0.5), Hatch_OnTick, HatchState.Hatch);

                        break;
                    }
                case HatchState.Hatch:
                    {
                        HatchEnd();
                        break;
                    }
                default:
                    {
                        StopTimer();
                        break;
                    }
            }
        }

        private void HatchEnd()
        {
            if (this.Deleted || this.Map == null || m_Chick == null || m_Chick.Deleted)
                return;

            m_Chick.RetrieveMobileFromIntStorage(this.GetWorldLocation(), this.Map);

            BreedingSystem.OnHatch(m_Chick);

            this.Delete();
        }

        private Timer m_Timer;

        private void DoDelayed(TimeSpan delay, TimerStateCallback callback, object state)
        {
            DoDelayed(delay, TimeSpan.Zero, callback, state);
        }

        private void DoDelayed(TimeSpan delay, TimeSpan interval, TimerStateCallback callback, object state)
        {
            StopTimer();

            if (interval == TimeSpan.Zero)
                m_Timer = Timer.DelayCall(delay, callback, state);
            else
                m_Timer = Timer.DelayCall(delay, interval, callback, state);
        }

        private void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        public override void OnAfterDelete()
        {
            if (m_Chick != null && m_Chick.Map == Map.Internal && m_Chick.IsIntMapStorage)
                m_Chick.Delete();

            StopTimer();

            m_Eggs.Remove(this);
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            InspectEgg(from, this);
        }

        public static void InspectEgg(Mobile from, IEgg egg)
        {
            Item eggItem = egg as Item;

            if (eggItem == null || eggItem.Map == null || !from.InRange(eggItem.GetWorldLocation(), 1))
                return;

            if (eggItem.Parent != null)
                from.SendMessage("The egg is in too awkward of a position to inspect it.");
            else if (!HasLight(from))
                from.SendMessage("It is too dark too discern anything about the egg.");
            else if (egg.IsFertile)
                from.SendMessage("You shine light through the egg...  You notice the shadow of en embryo!");
            else
                from.SendMessage("You shine light through the egg...  But it appears completely clear.");
        }

        private static bool HasLight(Mobile from)
        {
            BaseLight light;

            if ((light = from.FindItemOnLayer(Layer.OneHanded) as BaseLight) != null && light.Burning)
                return true;

            if ((light = from.FindItemOnLayer(Layer.TwoHanded) as BaseLight) != null && light.Burning)
                return true;

            return false;
        }

        #region IEgg

        bool IEgg.IsFertile { get { return (m_Chick != null && !m_Chick.Deleted); } }

        #endregion

        public BaseHatchableEgg(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((Mobile)m_Chick);
            writer.Write((DateTime)m_Birthdate);
            writer.Write((int)m_Health);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = PeekInt(reader);

            if (version < 1)
            {
                /* ChickenEgg used to derive from "Server.Items.Eggs".
                 * So, if this is a ChickenEgg, it means that we just peeked the version of Eggs.
                 * Since ChickenEgg no longer derives from Eggs, we must consume its version.
                 */
                if (World.LoadingType == "Server.Mobiles.ChickenEgg")
                    reader.ReadInt();

                return; // old egg, class insertion
            }

            reader.ReadInt(); // consume version

            m_Chick = reader.ReadMobile() as BaseCreature;
            m_Birthdate = reader.ReadDateTime();
            m_Health = reader.ReadInt();

            if (version < 2 && m_Chick != null && m_Chick.SpawnerTempMob)
            {
                m_Chick.MoveToIntStorage();
                m_Chick.SpawnerTempMob = false;
            }

            if (m_Chick != null)
                m_Eggs.Add(this);
        }

        private static int PeekInt(GenericReader reader)
        {
            int result = reader.ReadInt();
            reader.Seek(-4, System.IO.SeekOrigin.Current);
            return result;
        }
    }

    public class HatchableEgg : BaseHatchableEgg
    {
        private static string GetName(BaseCreature chick)
        {
            if (chick != null)
            {
                string name = chick.Name;

                if (name != null && (
                    (name.StartsWith("a ") && name.Length > 2) ||
                    (name.StartsWith("an ") && name.Length > 3)))
                {
                    return String.Format("{0} egg", name);
                }
            }

            return "an egg";
        }

        [Constructable]
        public HatchableEgg()
            : this(null)
        {
        }

        public HatchableEgg(BaseCreature chick)
            : base(chick)
        {
            Name = GetName(chick);
        }

        public override Food Cook()
        {
            return null;
        }

        public HatchableEgg(Serial serial)
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
    }
}