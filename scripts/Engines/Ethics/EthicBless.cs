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

/* Scripts\Engines\Ethics\EthicBless.cs
 * Changelog:
 *  1/10/23, Yoar
 *      Initial commit
 */

using Server.Items;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Ethics
{
    public static class EthicBless
    {
        public static TimeSpan Duration = TimeSpan.FromMinutes(30.0);

        private static readonly Dictionary<Item, InternalTimer> m_Registry = new Dictionary<Item, InternalTimer>();

        public static Dictionary<Item, InternalTimer> Registry { get { return m_Registry; } }

        public static void Initialize()
        {
            if (!Core.Ethics)
                return;

            EventSink.WorldSave += EventSink_OnWorldSave;

            EthicBlessPersistence.EnsureInstance();
        }

        private static void EventSink_OnWorldSave(WorldSaveEventArgs e)
        {
            Defrag();
        }

        public static void HeroBless(Item item)
        {
            Attach(item, true, Duration);
        }

        public static void EvilBless(Item item)
        {
            Attach(item, false, Duration);
        }

        #region Helper Methods

        public static DateTime GetExpireHero(Item item)
        {
            DateTime expire;

            return (GetEffect(item, out expire) == Ethic.Hero ? expire : DateTime.MinValue);
        }

        public static DateTime GetExpireEvil(Item item)
        {
            DateTime expire;

            return (GetEffect(item, out expire) == Ethic.Evil ? expire : DateTime.MinValue);
        }

        public static void SetExpireHero(Item item, DateTime expire)
        {
            if (expire == DateTime.MinValue)
                Detach(item);
            else
                Attach(item, true, expire - DateTime.UtcNow);
        }

        public static void SetExpireEvil(Item item, DateTime expire)
        {
            if (expire == DateTime.MinValue)
                Detach(item);
            else
                Attach(item, false, expire - DateTime.UtcNow);
        }

        public static void AddProperty(Item item, ObjectPropertyList list)
        {
            DateTime expire;

            Ethic ethic = GetEffect(item, out expire);

            if (ethic != null)
            {
                int minutes = Math.Max(0, (int)Math.Ceiling((expire - DateTime.UtcNow).TotalMinutes));

                if (ethic == Ethic.Hero)
                    list.Add(1045118, minutes.ToString()); // Holy item [Blessed] - minutes remaining: ~1_val~
                else
                    list.Add(1045119, minutes.ToString()); // Unholy item [Blessed] - minutes remaining: ~1_val~
            }
        }

        public static void LabelTo(Item item, Mobile from)
        {
            DateTime expire;

            Ethic ethic = GetEffect(item, out expire);

            if (ethic != null)
            {
                int minutes = Math.Max(0, (int)Math.Ceiling((expire - DateTime.UtcNow).TotalMinutes));

                if (ethic == Ethic.Hero)
                    item.LabelTo(from, 1045118, minutes.ToString()); // Holy item [Blessed] - minutes remaining: ~1_val~
                else
                    item.LabelTo(from, 1045119, minutes.ToString()); // Unholy item [Blessed] - minutes remaining: ~1_val~
            }
        }

        public static void AddEquipmentInfoAttribute(Item item, ArrayList attrs)
        {
            DateTime expire;

            if (GetEffect(item, out expire) != null)
            {
                int minutes = Math.Max(0, (int)Math.Ceiling((expire - DateTime.UtcNow).TotalMinutes));

                attrs.Add(new EquipInfoAttribute(1038021, minutes)); // Blessed
            }
        }

        #endregion

        public static Ethic GetBlessedFor(Item item)
        {
            DateTime expire;

            return GetEffect(item, out expire);
        }

        public static Ethic GetEffect(Item item, out DateTime expire)
        {
            InternalTimer timer;

            if (m_Registry.TryGetValue(item, out timer))
            {
                expire = timer.NextTick;

                if (timer.Hero)
                    return Ethic.Hero;
                else
                    return Ethic.Evil;
            }

            expire = DateTime.MinValue;
            return null;
        }

        public static void Attach(Item item, bool hero, TimeSpan duration)
        {
            Detach(item);

            (m_Registry[item] = new InternalTimer(item, hero, duration)).Start();

            item.LootType = LootType.UnStealable | LootType.UnLootable; // blessed without the blessed label
        }

        public static void Detach(Item item)
        {
            InternalTimer timer;

            if (m_Registry.TryGetValue(item, out timer))
            {
                timer.Stop();
                m_Registry.Remove(item);
            }

            item.LootType = LootType.Regular;
        }

        public static void Expire(Item item)
        {
            Mobile m = item.RootParent as Mobile;

            if (m != null)
                m.SendLocalizedMessage(1045117); // The blessing fades

            Detach(item);
        }

        public static void Defrag()
        {
            List<Item> toRemove = new List<Item>();

            foreach (KeyValuePair<Item, InternalTimer> kvp in m_Registry)
            {
                if (kvp.Key.Deleted)
                {
                    kvp.Value.Stop();
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (Item item in toRemove)
                m_Registry.Remove(item);
        }

        public class InternalTimer : Timer
        {
            private Item m_Owner;
            private bool m_Hero;

            public bool Hero { get { return m_Hero; } }

            public InternalTimer(Item item, bool hero, TimeSpan delay)
                : base(delay)
            {
                m_Owner = item;
                m_Hero = hero;
            }

            protected override void OnTick()
            {
                Expire(m_Owner);
            }
        }
    }
    [Aliases("Server.Ethics.EthicsBlessPersistance")]
    public class EthicBlessPersistence : Item, IPersistence
    {
        private static EthicBlessPersistence m_Instance;
        public Item GetInstance() { return m_Instance; }
        public static void EnsureInstance()
        {
            if (m_Instance == null)
                m_Instance = new EthicBlessPersistence();
        }

        public override string DefaultName
        {
            get { return "Ethic Bless Persistence - Internal"; }
        }

        private EthicBlessPersistence()
            : base(1)
        {
            Movable = false;
        }

        public EthicBlessPersistence(Serial serial)
            : base(serial)
        {
            m_Instance = this;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)EthicBless.Registry.Count);

            foreach (KeyValuePair<Item, EthicBless.InternalTimer> kvp in EthicBless.Registry)
            {
                writer.Write((Item)kvp.Key);
                writer.Write((bool)kvp.Value.Hero);
                writer.WriteDeltaTime(kvp.Value.NextTick);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            Item item = reader.ReadItem();
                            bool hero = reader.ReadBool();
                            DateTime expireDate = reader.ReadDeltaTime();

                            if (item != null)
                                EthicBless.Attach(item, hero, expireDate - DateTime.UtcNow);
                        }

                        break;
                    }
            }
        }
    }
}