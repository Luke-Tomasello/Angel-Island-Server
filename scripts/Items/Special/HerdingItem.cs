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

/* Items/Special/Holiday/HerdingItem.cs
 * CHANGELOG:
 *  1/4/23, Yoar
 *      Moved Message accessors into HerdingItem so that Spawner can properly copy them.
 *  12/28/23, Yoar
 *      Initial version.
 *      
 *      Can be used in event areas for players to herd certain mobiles.
 */

using Server.Commands;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.Items
{
    [FlipableAttribute(0xE81, 0xE82)]
    public class HerdingItem : BaseStaff, IUsesRemaining
    {
        #region Weapon Attributes
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.CrushingBlow; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Disarm; } }

        public override int OldMinDamage { get { return 3; } }
        public override int OldMaxDamage { get { return 12; } }

        public override int OldStrengthReq { get { return 10; } }
        public override int OldSpeed { get { return 30; } }

        public override int OldDieRolls { get { return 3; } }
        public override int OldDieMax { get { return 4; } }
        public override int OldAddConstant { get { return 0; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 50; } }
        #endregion

        private string m_ConditionStr;
        private ObjectConditional m_ConditionImpl;

        private double m_ReqSkill;
        private double m_MinSkill;
        private double m_MaxSkill;

        private MessageList m_SuccessMessages;
        private MessageList m_FailureMessages;

        private bool m_ConsumeUses;
        private int m_UsesRemaining;
        private string m_BreakMessage;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public string Condition
        {
            get { return m_ConditionStr; }
            set
            {
                if (value != null)
                {
                    try
                    {
                        string[] args = CommandSystem.Split(value);
                        m_ConditionImpl = ObjectConditional.Parse(null, ref args);
                        m_ConditionStr = value;
                    }
                    catch
                    {
                        m_ConditionImpl = null;
                        m_ConditionStr = null;
                    }
                }
                else
                {
                    m_ConditionImpl = null;
                    m_ConditionStr = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ReqSkill
        {
            get { return m_ReqSkill; }
            set { m_ReqSkill = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double MinSkill
        {
            get { return m_MinSkill; }
            set { m_MinSkill = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double MaxSkill
        {
            get { return m_MaxSkill; }
            set { m_MaxSkill = value; }
        }

        //[CommandProperty(AccessLevel.GameMaster)]
        public MessageList SuccessMessages
        {
            get { return m_SuccessMessages; }
            set { }
        }

        #region Success Message Accessors

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry SuccessMessage1 { get { return m_SuccessMessages.Message1; } set { m_SuccessMessages.Message1 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry SuccessMessage2 { get { return m_SuccessMessages.Message2; } set { m_SuccessMessages.Message2 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry SuccessMessage3 { get { return m_SuccessMessages.Message3; } set { m_SuccessMessages.Message3 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry SuccessMessage4 { get { return m_SuccessMessages.Message4; } set { m_SuccessMessages.Message4 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry SuccessMessage5 { get { return m_SuccessMessages.Message5; } set { m_SuccessMessages.Message5 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry SuccessMessage6 { get { return m_SuccessMessages.Message6; } set { m_SuccessMessages.Message6 = value; } }

        #endregion

        //[CommandProperty(AccessLevel.GameMaster)]
        public MessageList FailureMessages
        {
            get { return m_FailureMessages; }
            set { }
        }

        #region Failure Message Accessors

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry FailureMessage1 { get { return m_FailureMessages.Message1; } set { m_FailureMessages.Message1 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry FailureMessage2 { get { return m_FailureMessages.Message2; } set { m_FailureMessages.Message2 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry FailureMessage3 { get { return m_FailureMessages.Message3; } set { m_FailureMessages.Message3 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry FailureMessage4 { get { return m_FailureMessages.Message4; } set { m_FailureMessages.Message4 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry FailureMessage5 { get { return m_FailureMessages.Message5; } set { m_FailureMessages.Message5 = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry FailureMessage6 { get { return m_FailureMessages.Message6; } set { m_FailureMessages.Message6 = value; } }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ConsumeUses
        {
            get { return m_ConsumeUses; }
            set { m_ConsumeUses = value; }
        }

        public bool ShowUsesRemaining
        {
            get { return m_ConsumeUses; }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string BreakMessage
        {
            get { return m_BreakMessage; }
            set { m_BreakMessage = value; }
        }

        [Constructable]
        public HerdingItem()
            : base(0xE81)
        {
            Weight = 4.0;

            m_ReqSkill = 0.0;
            m_MinSkill = 0.0;
            m_MaxSkill = 200.0;

            m_SuccessMessages = new MessageList();
            m_FailureMessages = new MessageList();

            m_ConsumeUses = Core.RuleSets.SiegeRules();
            m_UsesRemaining = 200;
            m_BreakMessage = "You broke your tool.";
        }

        public override void OnSingleClick(Mobile from)
        {
            if (ShowUsesRemaining)
                LabelToAffix(from, 1017323, AffixType.Append, ": " + m_UsesRemaining.ToString()); // Durability

            base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage("What do you wish to instruct to move?");
            from.Target = new FirstTarget(this);
        }

        private class FirstTarget : Target
        {
            private HerdingItem m_Item;

            public FirstTarget(HerdingItem item)
                : base(10, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                BaseCreature bc = targeted as BaseCreature;

                if (bc == null || !m_Item.CheckCondition(bc))
                {
                    from.SendMessage("That is not something you can instruct to move.");
                }
                else
                {
                    from.SendMessage("Where do you wish them to go?");
                    from.Target = new SecondTarget(m_Item, bc);
                }
            }
        }

        private class SecondTarget : Target
        {
            private HerdingItem m_Item;
            private BaseCreature m_Creature;

            public SecondTarget(HerdingItem item, BaseCreature bc)
                : base(10, true, TargetFlags.None)
            {
                m_Item = item;
                m_Creature = bc;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted)
                    return;

                IPoint2D p = targeted as IPoint2D;

                if (p == null)
                    return;

                if (!m_Item.CheckCondition(m_Creature))
                {
                    from.SendMessage("That is not something you can instruct to move.");
                }
                else
                {
                    if (from.Skills[SkillName.Herding].Value < m_Item.ReqSkill || !from.CheckTargetSkill(SkillName.Herding, m_Creature, m_Item.MinSkill, m_Item.MaxSkill, new object[2] { m_Creature, null } /*contextObj*/))
                    {
                        from.SendMessage("You don't seem to be able to persuade them to move.");

                        TextEntry.PublicOverheadMessage(m_Creature, MessageType.Regular, m_Item.FailureMessages.GetRandom());
                    }
                    else
                    {
                        m_Creature.Herder = from;
                        m_Creature.HerdTime = DateTime.UtcNow;
                        m_Creature.TargetLocation = new Point2D(p);

                        from.SendMessage("It walks where it was instructed to.");

                        TextEntry.PublicOverheadMessage(m_Creature, MessageType.Regular, m_Item.SuccessMessages.GetRandom());
                    }

                    m_Item.ConsumeUse(from);
                }
            }
        }

        private bool CheckCondition(object obj)
        {
            if (m_ConditionImpl == null)
                return true;

            bool okay;

            try
            {
                okay = m_ConditionImpl.CheckCondition(obj);
            }
            catch
            {
                okay = false;
            }

            return okay;
        }

        private void ConsumeUse(Mobile from)
        {
            if (m_ConsumeUses && UsesRemaining-- <= 0)
            {
                Delete();

                from.SendMessage(m_BreakMessage);
            }
        }

        public HerdingItem(Serial serial)
            : base(serial)
        {
            m_SuccessMessages = new MessageList();
            m_FailureMessages = new MessageList();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((string)m_ConditionStr);

            writer.Write((double)m_ReqSkill);
            writer.Write((double)m_MinSkill);
            writer.Write((double)m_MaxSkill);

            m_SuccessMessages.Serialize(writer);
            m_FailureMessages.Serialize(writer);

            writer.Write((bool)m_ConsumeUses);
            writer.Write((int)m_UsesRemaining);
            writer.Write((string)m_BreakMessage);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Condition = reader.ReadString();

                        m_ReqSkill = reader.ReadDouble();
                        m_MinSkill = reader.ReadDouble();
                        m_MaxSkill = reader.ReadDouble();

                        m_SuccessMessages.Deserialize(reader);
                        m_FailureMessages.Deserialize(reader);

                        m_ConsumeUses = reader.ReadBool();
                        m_UsesRemaining = reader.ReadInt();
                        m_BreakMessage = reader.ReadString();

                        break;
                    }
            }
        }

        // TODO: Move to TextEntry.cs?
        [PropertyObject]
        public class MessageList
        {
            private TextEntry[] m_Array;

            #region Accessors

            [CommandProperty(AccessLevel.GameMaster)]
            public TextEntry Message1 { get { return GetAt(0); } set { SetAt(0, value); } }

            [CommandProperty(AccessLevel.GameMaster)]
            public TextEntry Message2 { get { return GetAt(1); } set { SetAt(1, value); } }

            [CommandProperty(AccessLevel.GameMaster)]
            public TextEntry Message3 { get { return GetAt(2); } set { SetAt(2, value); } }

            [CommandProperty(AccessLevel.GameMaster)]
            public TextEntry Message4 { get { return GetAt(3); } set { SetAt(3, value); } }

            [CommandProperty(AccessLevel.GameMaster)]
            public TextEntry Message5 { get { return GetAt(4); } set { SetAt(4, value); } }

            [CommandProperty(AccessLevel.GameMaster)]
            public TextEntry Message6 { get { return GetAt(5); } set { SetAt(5, value); } }

            private TextEntry GetAt(int index)
            {
                if (index < 0 || index >= m_Array.Length)
                    return TextEntry.Empty;

                return m_Array[index];
            }

            private void SetAt(int index, TextEntry value)
            {
                if (index < 0 || index >= m_Array.Length)
                    return;

                m_Array[index] = value;
            }

            #endregion

            public MessageList()
            {
                m_Array = new TextEntry[6];
            }

            public TextEntry GetRandom()
            {
                int count = 0;

                for (int i = 0; i < m_Array.Length; i++)
                {
                    if (m_Array[i] != TextEntry.Empty)
                        count++;
                }

                if (count <= 0)
                    return TextEntry.Empty;

                int rnd = Utility.Random(count);

                for (int i = 0; i < m_Array.Length; i++)
                {
                    if (m_Array[i] != TextEntry.Empty)
                    {
                        if (rnd == 0)
                            return m_Array[i];
                        else
                            rnd--;
                    }
                }

                return TextEntry.Empty;
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write((byte)0); // version

                writer.Write((int)m_Array.Length);

                for (int i = 0; i < m_Array.Length; i++)
                    writer.Write((string)m_Array[i].ToString());
            }

            public void Deserialize(GenericReader reader)
            {
                byte version = reader.ReadByte();

                int count = reader.ReadInt();

                for (int i = 0; i < count; i++)
                {
                    TextEntry te = TextEntry.Parse(reader.ReadString());

                    if (i < m_Array.Length)
                        m_Array[i] = te;
                }
            }

            public override string ToString()
            {
                return "...";
            }
        }
    }
}