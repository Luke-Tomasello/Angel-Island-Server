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

/* Scripts/Mobiles/Special/Christmas/WildHunt/OdinRaven.cs
 * ChangeLog
 *  1/2/24, Yoar
 *		Initial Version.
 */

using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles.WildHunt
{
    [CorpseName("a raven corpse")]
    public class OdinRaven : BaseCreature
    {
        [Constructable]
        public OdinRaven()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a raven";
            Body = 5;
            BaseSoundID = 0xD1;
            Hue = 0x901;

            SetStr(150);
            SetDex(150);
            SetInt(100);

            SetHits(150);

            SetDamage(8, 14);

            SetSkill(SkillName.MagicResist, 50.0);
            SetSkill(SkillName.Tactics, 70.0);
            SetSkill(SkillName.Wrestling, 70.0);

            Fame = 1500;
            Karma = 0;

            VirtualArmor = 30;

            Register(this);
        }

        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override int Meat { get { return 1; } }
        public override MeatType MeatType { get { return MeatType.Bird; } }
        public override int Feathers { get { return 36; } }

        public override void OnThink()
        {
            CheckWatch();

            base.OnThink();
        }

        #region Watch

        public static int WatchRange = 4;
        public static int WatchThreshold = 570;
        public const double DebuffOffset = 7.0;
        public static TimeSpan WatchCooldown = TimeSpan.FromSeconds(10.0);

        private static readonly Memory m_WatchCooldowns = new Memory();
        private static readonly Memory m_MessageMemory = new Memory();

        private readonly Dictionary<Mobile, SkillName> m_Watched = new Dictionary<Mobile, SkillName>();

        public Dictionary<Mobile, SkillName> Watched { get { return m_Watched; } }

        public bool IsWatching(Mobile m)
        {
            return m_Watched.ContainsKey(m);
        }

        public void CheckWatch()
        {
            if (!m_WatchCooldowns.Recall(this))
            {
                m_WatchCooldowns.Remember(this, WatchCooldown.TotalSeconds);

                DoWatch();
            }
        }

        public void DoWatch()
        {
            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in GetMobilesInRange(WatchRange))
            {
                if (!IsWatching(m) && WildHunt.IsValidHarmful(this, m))
                    list.Add(m);
            }

            if (list.Count == 0)
                return;

            WildHunt.SortByDistance(Location, list);

            for (int i = 0; i < list.Count; i++)
            {
                Mobile m = list[i];

                Skill skill = WatchSkill(m);

                if (skill != null)
                {
                    ApplyDebuff(m, skill.SkillName);
                    break;
                }
            }
        }

        private static Skill WatchSkill(Mobile m)
        {
            int totalFixed = 0;

            for (int i = 0; i < m.Skills.Length; i++)
            {
                int skillFixed = m.Skills[i].Fixed;

                if (skillFixed >= WatchThreshold)
                    totalFixed += skillFixed;
            }

            if (totalFixed == 0)
                return null;

            int rnd = Utility.Random(totalFixed);

            for (int i = 0; i < m.Skills.Length; i++)
            {
                int skillFixed = m.Skills[i].Fixed;

                if (skillFixed >= WatchThreshold)
                {
                    if (rnd < skillFixed)
                        return m.Skills[i];
                    else
                        rnd -= skillFixed;
                }
            }

            return null;
        }

        public void ApplyDebuff(Mobile m, SkillName skill)
        {
            RemoveDebuff(m);

            m.AddSkillMod(GetSkillMod(skill));

            if (!m_MessageMemory.Recall(m))
            {
                m_MessageMemory.Remember(m, 60.0);

                m.SendMessage("You feel uneasy...");
            }

            m_Watched[m] = skill;
        }

        public void RemoveDebuff(Mobile m)
        {
            SkillName skill;

            if (m_Watched.TryGetValue(m, out skill))
            {
                m.RemoveSkillMod(GetSkillMod(skill));

                m_Watched.Remove(m);
            }
        }

        public void ProcessDebuffs()
        {
            List<Mobile> toRemove = new List<Mobile>();

            foreach (KeyValuePair<Mobile, SkillName> kvp in m_Watched)
            {
                Mobile m = kvp.Key;

                if (!m.Alive || m.IsDeadBondedPet || Map != m.Map || !InRange(m, RangePerception) || !CanSee(m))
                {
                    m.RemoveSkillMod(GetSkillMod(kvp.Value));

                    toRemove.Add(m);
                }
            }

            foreach (Mobile m in toRemove)
                m_Watched.Remove(m);
        }

        public void ClearDebuffs()
        {
            foreach (KeyValuePair<Mobile, SkillName> kvp in m_Watched)
                kvp.Key.RemoveSkillMod(GetSkillMod(kvp.Value));

            m_Watched.Clear();
        }

        private readonly Dictionary<SkillName, SkillMod> m_ModCache = new Dictionary<SkillName, SkillMod>();

        private SkillMod GetSkillMod(SkillName skill)
        {
            SkillMod mod;

            if (!m_ModCache.TryGetValue(skill, out mod))
                m_ModCache[skill] = mod = new DefaultSkillMod(skill, true, -DebuffOffset);

            return mod;
        }

        #endregion

        #region Raven Registry

        private static readonly List<OdinRaven> m_Registry = new List<OdinRaven>();
        private static Timer m_Timer;

        public static List<OdinRaven> Registry { get { return m_Registry; } }
        public static Timer Timer { get { return m_Timer; } }

        public static void Register(OdinRaven raven)
        {
            if (!m_Registry.Contains(raven))
                m_Registry.Add(raven);

            StartTimer();
        }

        public static void Unregister(OdinRaven raven)
        {
            m_Registry.Remove(raven);

            if (m_Registry.Count == 0)
                StopTimer();
        }

        private static void Slice()
        {
            for (int i = m_Registry.Count - 1; i >= 0; i--)
                m_Registry[i].ProcessDebuffs();

            if (m_Registry.Count == 0)
                StopTimer();
        }

        private static void StartTimer()
        {
            StopTimer();

            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0), Slice);
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

        public override void GenerateLoot()
        {
            if (Spawning)
            {
                PackItem(new BlackPearl(4));
            }
            else
            {
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            ClearDebuffs();

            Unregister(this);
        }

        public OdinRaven(Serial serial)
            : base(serial)
        {
            Register(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}