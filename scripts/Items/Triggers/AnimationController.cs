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

/* Items/Triggers/AnimationController.cs
 * CHANGELOG:
 * 	7/13/23, Yoar
 * 		Initial version.
 */

using Server.Mobiles;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    public class AnimationController : Item, ITriggerable
    {
        public enum MonsterAnimationType
        {
            Walk,
            Idle1,
            Die1,
            Die2,
            Attack1,
            Attack2,
            Attack3,

            GetHit = 10,
            Pillage,

            BlockRight = 15,
            BlockLeft,
            Idle2,
            Fidget,
        }

        public enum HumanAnimationType
        {
            Walk,
            WalkStaff,
            Run,
            RunStaff,
            Idle1,
            Idle2,
            Fidget_Yawn_Stretch,
            CombatIdle1,
            CombatIdle2,
            AttackSlash1H,
            AttackPierce1H,
            AttackBash1H,
            AttackBash2H,
            AttackSlash2H,
            AttackPierce2H,
            CombatAdvance_1H,
            Spell1,
            Spell2,
            AttackBow,
            AttackCrossbow,
            GetHit_Fr_Hi,
            Die_Hard_Fwd,
            Die_Hard_Back,
            Horse_Walk,
            Horse_Run,
            Horse_Idle,
            Horse_Attack1H_SlashRight,
            Horse_AttackBow,
            Horse_AttackCrossbow,
            Horse_Attack2H_SlashRight,
            Block_Shield_Hand,
            Punch_Punch_Jab,
            Bow_Lesser,
            Salute_Armed1h,
            Ingest_Eat,
        }

        public override string DefaultName { get { return "Animation Controller"; } }

        private int m_Action;
        private int m_FrameCount;
        private int m_RepeatCount;
        private bool m_Forward;
        private bool m_Repeat;
        private int m_Delay;

        private IEntity m_Target;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Action
        {
            get { return m_Action; }
            set { m_Action = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MonsterAnimationType MonsterAnimation
        {
            get { return (MonsterAnimationType)m_Action; }
            set { m_Action = (int)value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HumanAnimationType HumanAnimation
        {
            get { return (HumanAnimationType)m_Action; }
            set { m_Action = (int)value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FrameCount
        {
            get { return m_FrameCount; }
            set { m_FrameCount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RepeatCount
        {
            get { return m_RepeatCount; }
            set { m_RepeatCount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Forward
        {
            get { return m_Forward; }
            set { m_Forward = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Repeat
        {
            get { return m_Repeat; }
            set { m_Repeat = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; }
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Spawner TargetSpawner { get { return m_Target as Spawner; } set { m_Target = value; } }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile TargetMobile { get { return m_Target as Mobile; } set { m_Target = value; } }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TargetNull { get { return (m_Target == null); } set { if (value) m_Target = null; } }

        [CopyableAttribute(CopyType.Copy)]  // CopyProperties will copy this one and not the DoNotCopy ones
        public IEntity Target { get { return m_Target; } set { m_Target = value; } }

        [Constructable]
        public AnimationController()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_FrameCount = 5;
            m_RepeatCount = 1;
            m_Forward = true;
            m_Repeat = false;
            m_Delay = 0;
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public void OnTrigger(Mobile from)
        {
            Queue<Mobile> queue = new Queue<Mobile>();
            if (m_Target is Mobile)
                queue.Enqueue(m_Target as Mobile);
            else if (m_Target is Spawner spawner && spawner.Objects != null)
                foreach (object o in spawner.Objects)
                    if (o is Mobile)
                        queue.Enqueue(o as Mobile);

            if (queue.Count == 0)
                queue.Enqueue(from);

            while (queue.Count > 0)
            {
                Mobile target = queue.Dequeue();

                if (target != null)
                    target.Animate(m_Action, m_FrameCount, m_RepeatCount, m_Forward, m_Repeat, m_Delay);
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
                list.Add(new TriggerSystem.ActivateCME(((ITriggerable)this).CanTrigger(from)));
        }

        #region ITriggerable

        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return true;
        }

        void ITriggerable.OnTrigger(Mobile from)
        {
            OnTrigger(from);
        }

        #endregion

        public AnimationController(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            if (m_Target is Item)
                writer.Write(m_Target as Item);
            else
                writer.Write(m_Target as Mobile);

            writer.Write((int)m_Action);
            writer.Write((int)m_FrameCount);
            writer.Write((int)m_RepeatCount);
            writer.Write((bool)m_Forward);
            writer.Write((bool)m_Repeat);
            writer.Write((int)m_Delay);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Target = ReadEntity(reader);

                        goto case 0;
                    }
                case 0:
                    {
                        m_Action = reader.ReadInt();
                        m_FrameCount = reader.ReadInt();
                        m_RepeatCount = reader.ReadInt();
                        m_Forward = reader.ReadBool();
                        m_Repeat = reader.ReadBool();
                        m_Delay = reader.ReadInt();

                        break;
                    }
            }
        }

        private IEntity ReadEntity(GenericReader reader)
        {
            return World.FindEntity(reader.ReadInt());
        }
    }
}