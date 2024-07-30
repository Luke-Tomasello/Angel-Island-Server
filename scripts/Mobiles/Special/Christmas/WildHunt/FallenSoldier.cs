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

/* Scripts/Mobiles/Special/Christmas/WildHunt/FallenSoldier.cs
 * ChangeLog
 *  1/2/24, Yoar
 *		Initial Version.
 */

using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles.WildHunt
{
    public interface IRitual : IEntity
    {
        int CurProgress { get; set; }
        int MaxProgress { get; }

        void Complete();
    }

    public class FallenSoldier : Item, IRitual
    {
        public override string DefaultName { get { return "a fallen solder"; } }

        public override bool Decays { get { return true; } }
        public override TimeSpan DecayTime { get { return TimeSpan.FromMinutes(14.0); } }

        private int m_CurProgress;

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurProgress
        {
            get { return m_CurProgress; }
            set { m_CurProgress = value; }
        }

        public int MaxProgress { get { return 30; } }

        [Constructable]
        public FallenSoldier()
            : base(Utility.Random(0xECA, 9))
        {
            Hue = 0x4001;
            Movable = false;

            Register(this);
        }

        public void Complete()
        {
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

            Mobile toSpawn;

            if (Utility.Random(100) < 66)
                toSpawn = new WildHuntWarrior(false);
            else
                toSpawn = new WildHuntMage(false);

            if (toSpawn is IResurrected)
                ((IResurrected)toSpawn).Resurrected = true;

            toSpawn.MoveToWorld(Location, Map);

            Delete();
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Unregister(this);
        }

        public FallenSoldier(Serial serial)
            : base(serial)
        {
            Register(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_CurProgress);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_CurProgress = reader.ReadInt();
                        break;
                    }
            }
        }

        #region Ritual Registry

        public static TimeSpan AtrophyDelay = TimeSpan.FromSeconds(5.0);

        private static readonly List<IRitual> m_Registry = new List<IRitual>();
        private static Timer m_Timer;

        private static readonly Memory m_AtrophyMemory = new Memory();

        public static List<IRitual> Registry { get { return m_Registry; } }
        public static Timer Timer { get { return m_Timer; } }

        public static void Register(IRitual ritual)
        {
            if (!m_Registry.Contains(ritual))
                m_Registry.Add(ritual);

            StartTimer();
        }

        public static void Unregister(IRitual ritual)
        {
            m_Registry.Remove(ritual);

            if (m_Registry.Count == 0)
                StopTimer();
        }

        private static void Slice()
        {
            for (int i = m_Registry.Count - 1; i >= 0; i--)
            {
                IRitual ritual = m_Registry[i];

                if (ritual.CurProgress > 0 && ritual.CurProgress < ritual.MaxProgress && m_AtrophyMemory.Recall(ritual) == null)
                {
                    m_AtrophyMemory.Remember(ritual, AtrophyDelay.TotalSeconds);

                    ritual.CurProgress--;
                }
            }

            if (m_Registry.Count == 0)
                StopTimer();
        }

        private static void StartTimer()
        {
            StopTimer();

            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), Slice);
        }

        private static void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        #endregion
    }
}