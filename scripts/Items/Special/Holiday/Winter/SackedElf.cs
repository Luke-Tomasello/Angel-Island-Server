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

/* Scripts/Items/Special/Holiday/Christmas/SackedElf.cs
  *	ChangeLog:
  *	12/12/23, Yoar
  *		Initial version.
  */

using Server.Commands;
using Server.Mobiles;
using System;
using System.Reflection;

namespace Server.Items
{
    [Flipable(0x1039, 0x1045)]
    public class SackedElf : Item
    {
        public override string DefaultName { get { return "a sacked elf"; } }

        private string m_RewardItem;
        private string m_RewardMessage;

        [CommandProperty(AccessLevel.GameMaster)]
        public string RewardItem
        {
            get { return m_RewardItem; }
            set { m_RewardItem = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RewardMessage
        {
            get { return m_RewardMessage; }
            set { m_RewardMessage = value; }
        }

        [Constructable]
        public SackedElf()
            : base(0x1045)
        {
            Weight = 1.0;

            StartTimer();
        }

        public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
        {
            if (target is ElfHelper)
            {
                Item reward = GetReward();

                if (reward != null)
                {
                    RescuedElf rescued = new RescuedElf();

                    rescued.MoveToWorld(target.Location, target.Map);
                    rescued.Poof();
                    rescued.SayTo(from, "Thank you for saving me, {0}! Here's a little something from Santa's toy factory.", from.Female ? "ma'am" : "mister");

                    Delete();

                    from.AddToBackpack(reward);

                    if (!string.IsNullOrEmpty(m_RewardMessage))
                        from.SendMessage(m_RewardMessage);

                    return true;
                }
            }

            return base.DropToMobile(from, target, p);
        }

        private Item GetReward()
        {
            Type type = SpawnerType.GetType(m_RewardItem);

            if (type == null || !typeof(Item).IsAssignableFrom(type))
                return null;

            ConstructorInfo ctor = type.GetConstructor(new Type[0]);

            if (ctor == null || !Add.IsConstructable(ctor))
                return null;

            try
            {
                return (Item)ctor.Invoke(new object[0]);
            }
            catch
            {
                return null;
            }
        }

        private Timer m_Timer;

        private void StartTimer()
        {
            StopTimer();

            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(15.0), TimeSpan.FromSeconds(15.0), OnTick);
        }

        private void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private void OnTick()
        {
            Mobile m = RootParent as Mobile;

            if (m != null && m.Player && Utility.RandomBool())
                LabelTo(m, m_Messages[Utility.Random(m_Messages.Length)]);
        }

        private static string[] m_Messages = new string[]
            {
                "Help me!",
                "Heeeelp!",
                "Safe me!",
                "Somebody, please safe me!",
                "Can anybody hear me?",
                "I seem to have gotten myself stuck.",
            };

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            StopTimer();
        }

        public SackedElf(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((string)m_RewardItem);
            writer.Write((string)m_RewardMessage);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_RewardItem = reader.ReadString();
            m_RewardMessage = reader.ReadString();

            StartTimer();
        }

        // temporary elf mobile
        private class RescuedElf : ElfPeasant
        {
            protected override bool ThrowsSnowballs { get { return false; } }
            protected override bool HasSnowballFights { get { return false; } }
            protected override bool IsEasilyScared { get { return false; } }

            private Timer m_Timer;

            public RescuedElf()
                : base()
            {
                FightMode = FightMode.None;

                m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(20.0), Delete);
            }

            public void Poof()
            {
                Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);
            }

            public override void OnDelete()
            {
                Poof();

                base.OnDelete();
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Timer != null)
                    m_Timer.Stop();
            }

            public RescuedElf(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.WriteEncodedInt(0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                reader.ReadEncodedInt();

                Delete();
            }
        }
    }
}