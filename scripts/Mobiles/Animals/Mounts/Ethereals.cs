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

using Server.Items;
using Server.Spells;
using System;

namespace Server.Mobiles
{
    public class EtherealMount : Item, IMount, IMountItem, Engines.VeteranRewards.IRewardItem
    {
        private int m_MountedID;
        private int m_RegularID;
        private Mobile m_Rider;
        private bool m_IsRewardItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get { return m_IsRewardItem; }
            set { m_IsRewardItem = value; }
        }

        [Constructable]
        public EtherealMount(int itemID, int mountID)
            : base(itemID)
        {
            m_MountedID = mountID;
            m_RegularID = itemID;
            m_Rider = null;

            Layer = Layer.Invalid;

            LootType = LootType.Blessed;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MountedID
        {
            get
            {
                return m_MountedID;
            }
            set
            {
                if (m_MountedID != value)
                {
                    m_MountedID = value;

                    if (m_Rider != null)
                        ItemID = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RegularID
        {
            get
            {
                return m_RegularID;
            }
            set
            {
                if (m_RegularID != value)
                {
                    m_RegularID = value;

                    if (m_Rider == null)
                        ItemID = value;
                }
            }
        }

        public EtherealMount(Serial serial)
            : base(serial)
        {
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            else if (m_IsRewardItem && !Engines.VeteranRewards.RewardSystem.CheckIsUsableBy(from, this, null))
                return;
            else if (!from.CanBeginAction(typeof(BaseMount)))
                from.SendLocalizedMessage(1040024); // You are still too dazed from being knocked off your mount to ride!
            else if (from.Mounted)
                from.SendLocalizedMessage(1005583); // Please dismount first.
            else if (from.IsBodyMod && !from.Body.IsHuman)
                from.SendLocalizedMessage(1061628); // You can't do that while polymorphed.
            else if (Multis.DesignContext.Check(from))
                new EtherealSpell(this, from).Cast();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((bool)m_IsRewardItem);

            writer.Write((int)m_MountedID);
            writer.Write((int)m_RegularID);
            writer.Write(m_Rider);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_IsRewardItem = reader.ReadBool();
                        goto case 0;
                    }
                case 1: reader.ReadInt(); goto case 0;
                case 0:
                    {
                        m_MountedID = reader.ReadInt();
                        m_RegularID = reader.ReadInt();
                        m_Rider = reader.ReadMobile();

                        if (m_MountedID == 0x3EA2)
                            m_MountedID = 0x3EAA;

                        break;
                    }
            }
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            Rider = null;//get off, move to pack

            return DeathMoveResult.RemainEquiped;
        }

        public static void Dismount(Mobile m)
        {
            IMount mount = m.Mount;

            if (mount != null)
                mount.Rider = null;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Rider
        {
            get
            {
                return m_Rider;
            }
            set
            {
                if (value != m_Rider)
                {
                    if (value == null)
                    {
                        Internalize();
                        UnmountMe();

                        m_Rider = value;
                    }
                    else
                    {
                        if (m_Rider != null)
                            Dismount(m_Rider);

                        Dismount(value);

                        m_Rider = value;

                        MountMe();
                    }
                }
            }
        }

        public void UnmountMe()
        {
            Container bp = m_Rider.Backpack;

            ItemID = m_RegularID;
            Layer = Layer.Invalid;
            Movable = true;

            if (Hue == 0x4001)
                Hue = 0;

            if (bp != null)
            {
                bp.DropItem(this);
            }
            else
            {
                Point3D loc = m_Rider.Location;
                Map map = m_Rider.Map;

                if (map == null || map == Map.Internal)
                {
                    loc = m_Rider.LogoutLocation;
                    map = m_Rider.LogoutMap;
                }

                MoveToWorld(loc, map);
            }
        }

        public void MountMe()
        {
            ItemID = m_MountedID;
            Layer = Layer.Mount;
            Movable = false;

            if (Hue == 0)
                Hue = 0x4001;

            ProcessDelta();
            m_Rider.ProcessDelta();
            m_Rider.EquipItem(this);
            m_Rider.ProcessDelta();
            ProcessDelta();
        }

        public IMount Mount
        {
            get
            {
                return this;
            }
        }

        private class EtherealSpell : Spell
        {
            private static SpellInfo m_Info = new SpellInfo("Ethereal Mount", "", SpellCircle.Second, 230);

            private EtherealMount m_Mount;
            private Mobile m_Rider;

            public EtherealSpell(EtherealMount mount, Mobile rider)
                : base(rider, null, m_Info)
            {
                m_Rider = rider;
                m_Mount = mount;
            }

            public override bool ClearHandsOnCast { get { return false; } }
            public override bool RevealOnCast { get { return false; } }

            public override TimeSpan GetCastRecovery()
            {
                return TimeSpan.Zero;
            }

            public override TimeSpan GetCastDelay()
            {
                return TimeSpan.FromSeconds(5.0);
            }

            public override int GetMana()
            {
                return 0;
            }

            public override bool ConsumeReagents()
            {
                return true;
            }

            public override bool CheckFizzle()
            {
                return false;
            }

            public override bool CheckDisturb(DisturbType type, bool checkFirst, bool resistable)
            {
                if (type == DisturbType.EquipRequest || type == DisturbType.UseRequest/* || type == DisturbType.Hurt*/ )
                    return false;

                return true;
            }

            public override void OnDisturb(DisturbType type, bool message)
            {
                if (message)
                    Caster.SendLocalizedMessage(1049455); // You have been disrupted while attempting to summon your ethereal mount!

                //m_Mount.UnmountMe();
            }

            public override void OnCast()
            {
                if (!m_Mount.Deleted && m_Mount.Rider == null && m_Mount.IsChildOf(m_Rider.Backpack))
                    m_Mount.Rider = m_Rider;

                FinishSequence();
            }
        }
    }

    public class EtherealHorse : EtherealMount
    {
        public override int LabelNumber { get { return 1041298; } } // Ethereal Horse Statuette

        [Constructable]
        public EtherealHorse()
            : base(0x20DD, 0x3EAA)
        {
        }

        public EtherealHorse(Serial serial)
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

            if (Name == "an ethereal horse")
                Name = null;

            if (ItemID == 0x2124)
                ItemID = 0x20DD;
        }
    }

    public class EtherealLlama : EtherealMount
    {
        public override int LabelNumber { get { return 1041300; } } // Ethereal Llama Statuette

        [Constructable]
        public EtherealLlama()
            : base(0x20F6, 0x3EAB)
        {
        }

        public EtherealLlama(Serial serial)
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

            if (Name == "an ethereal llama")
                Name = null;
        }
    }

    public class EtherealOstard : EtherealMount
    {
        public override int LabelNumber { get { return 1041299; } } // Ethereal Ostard Statuette

        [Constructable]
        public EtherealOstard()
            : base(0x2135, 0x3EAC)
        {
        }

        public EtherealOstard(Serial serial)
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

            if (Name == "an ethereal ostard")
                Name = null;
        }
    }

    public class EtherealRidgeback : EtherealMount
    {
        public override int LabelNumber { get { return 1049747; } } // Ethereal Ridgeback Statuette

        [Constructable]
        public EtherealRidgeback()
            : base(0x2615, 0x3E9A)
        {
        }

        public EtherealRidgeback(Serial serial)
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

            if (Name == "an ethereal ridgeback")
                Name = null;
        }
    }

    public class EtherealUnicorn : EtherealMount
    {
        public override int LabelNumber { get { return 1049745; } } // Ethereal Unicorn Statuette

        [Constructable]
        public EtherealUnicorn()
            : base(0x25CE, 0x3E9B)
        {
        }

        public EtherealUnicorn(Serial serial)
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

            if (Name == "an ethereal unicorn")
                Name = null;
        }
    }

    public class EtherealBeetle : EtherealMount
    {
        public override int LabelNumber { get { return 1049748; } } // Ethereal Beetle Statuette

        [Constructable]
        public EtherealBeetle()
            : base(0x260F, 0x3E97)
        {
        }

        public EtherealBeetle(Serial serial)
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

            if (Name == "an ethereal beetle")
                Name = null;
        }
    }

    public class EtherealKirin : EtherealMount
    {
        public override int LabelNumber { get { return 1049746; } } // Ethereal Ki-Rin Statuette

        [Constructable]
        public EtherealKirin()
            : base(0x25A0, 0x3E9C)
        {
        }

        public EtherealKirin(Serial serial)
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

            if (Name == "an ethereal kirin")
                Name = null;
        }
    }

    public class EtherealSwampDragon : EtherealMount
    {
        public override int LabelNumber { get { return 1049749; } } // Ethereal Swamp Dragon Statuette

        [Constructable]
        public EtherealSwampDragon()
            : base(0x2619, 0x3E98)
        {
            Name = "an ethereal swamp dragon";
        }

        public EtherealSwampDragon(Serial serial)
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

            if (Name == "an ethereal swamp dragon")
                Name = null;
        }
    }
}