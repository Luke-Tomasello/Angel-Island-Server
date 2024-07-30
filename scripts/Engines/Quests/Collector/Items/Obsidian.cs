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

using Server.ContextMenus;
using Server.Network;
using Server.Targeting;
using System.Collections;

namespace Server.Engines.Quests.Collector
{
    [TypeAlias("Server.Engines.Quests.Collector.Obsidian")]
    public class ObsidianStatue : Item
    {
        private static readonly string[] m_Names = new string[]
            {
                null,
                "an aggressive cavalier",
                "a beguiling rogue",
                "a benevolent physician",
                "a brilliant artisan",
                "a capricious adventurer",
                "a clever beggar",
                "a convincing charlatan",
                "a creative inventor",
                "a creative tinker",
                "a cunning knave",
                "a dauntless explorer",
                "a despicable ruffian",
                "an earnest malcontent",
                "an exultant animal tamer",
                "a famed adventurer",
                "a fanatical crusader",
                "a fastidious clerk",
                "a fearless hunter",
                "a festive harlequin",
                "a fidgety assassin",
                "a fierce soldier",
                "a fierce warrior",
                "a frugal magnate",
                "a glib pundit",
                "a gnomic shaman",
                "a graceful noblewoman",
                "a idiotic madman",
                "a imaginative designer",
                "an inept conjurer",
                "an innovative architect",
                "an inventive blacksmith",
                "a judicious mayor",
                "a masterful chef",
                "a masterful woodworker",
                "a melancholy clown",
                "a melodic bard",
                "a merciful guard",
                "a mirthful jester",
                "a nervous surgeon",
                "a peaceful scholar",
                "a prolific gardener",
                "a quixotic knight",
                "a regal aristocrat",
                "a resourceful smith",
                "a reticent alchemist",
                "a sanctified priest",
                "a scheming patrician",
                "a shrewd mage",
                "a singing minstrel",
                "a skilled tailor",
                "a squeamish assassin",
                "a stoic swordsman",
                "a studious scribe",
                "a thought provoking writer",
                "a treacherous scoundrel",
                "a troubled poet",
                "an unflappable wizard",
                "a valiant warrior",
                "a wayward fool"
            };

        public static string RandomName(Mobile from)
        {
            int index = Utility.Random(m_Names.Length);
            if (m_Names[index] == null)
                return from.Name;
            else
                return m_Names[index];
        }

        private const int m_Partial = 2;
        private const int m_Completed = 10;

        private int m_Quantity;
        private string m_StatueName;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Quantity
        {
            get { return m_Quantity; }
            set
            {
                if (value <= 1)
                    m_Quantity = 1;
                else if (value >= m_Completed)
                    m_Quantity = m_Completed;
                else
                    m_Quantity = value;

                if (m_Quantity < m_Partial)
                    ItemID = 0x1EA7;
                else if (m_Quantity < m_Completed)
                    ItemID = 0x1F13;
                else
                    ItemID = 0x12CB;

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string StatueName
        {
            get { return m_StatueName; }
            set { m_StatueName = value; InvalidateProperties(); }
        }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        [Constructable]
        public ObsidianStatue()
            : base(0x1EA7)
        {
            Hue = 0x497;

            m_Quantity = 1;
            m_StatueName = "";
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_Quantity < m_Partial)
                list.Add(1055137); // a section of an obsidian statue
            else if (m_Quantity < m_Completed)
                list.Add(1055138); // a partially reconstructed obsidian statue
            else
                list.Add(1055139, m_StatueName); // an obsidian statue of ~1_STATUE_NAME~
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Quantity < m_Partial)
                LabelTo(from, 1055137); // a section of an obsidian statue
            else if (m_Quantity < m_Completed)
                LabelTo(from, 1055138); // a partially reconstructed obsidian statue
            else
                LabelTo(from, 1055139, m_StatueName); // an obsidian statue of ~1_STATUE_NAME~
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            if (from.Alive && m_Quantity >= m_Partial && m_Quantity < m_Completed && IsChildOf(from.Backpack))
                list.Add(new DisassembleEntry(this));
        }

        private class DisassembleEntry : ContextMenuEntry
        {
            private ObsidianStatue m_Obsidian;

            public DisassembleEntry(ObsidianStatue obsidian)
                : base(6142)
            {
                m_Obsidian = obsidian;
            }

            public override void OnClick()
            {
                Mobile from = Owner.From;
                if (!m_Obsidian.Deleted && m_Obsidian.Quantity >= ObsidianStatue.m_Partial && m_Obsidian.Quantity < ObsidianStatue.m_Completed && m_Obsidian.IsChildOf(from.Backpack) && from.CheckAlive())
                {
                    for (int i = 0; i < m_Obsidian.Quantity - 1; i++)
                        from.AddToBackpack(new ObsidianStatue());

                    m_Obsidian.Quantity = 1;
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Quantity < m_Completed)
            {
                if (!IsChildOf(from.Backpack))
                    from.Send(new MessageLocalized(Serial, ItemID, MessageType.Regular, 0x2C, 3, 500309, "", "")); // Nothing Happens.
                else
                    from.Target = new InternalTarget(this);
            }
        }

        private class InternalTarget : Target
        {
            private ObsidianStatue m_Obsidian;

            public InternalTarget(ObsidianStatue obsidian)
                : base(-1, false, TargetFlags.None)
            {
                m_Obsidian = obsidian;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Item targ = targeted as Item;
                if (m_Obsidian.Deleted || m_Obsidian.Quantity >= ObsidianStatue.m_Completed || targ == null)
                    return;

                if (m_Obsidian.IsChildOf(from.Backpack) && targ.IsChildOf(from.Backpack) && targ is ObsidianStatue && targ != m_Obsidian)
                {
                    ObsidianStatue targObsidian = (ObsidianStatue)targ;
                    if (targObsidian.Quantity < ObsidianStatue.m_Completed)
                    {
                        if (targObsidian.Quantity + m_Obsidian.Quantity <= ObsidianStatue.m_Completed)
                        {
                            targObsidian.Quantity += m_Obsidian.Quantity;
                            m_Obsidian.Delete();
                        }
                        else
                        {
                            int delta = ObsidianStatue.m_Completed - targObsidian.Quantity;
                            targObsidian.Quantity += delta;
                            m_Obsidian.Quantity -= delta;
                        }

                        if (targObsidian.Quantity >= ObsidianStatue.m_Completed)
                            targObsidian.StatueName = ObsidianStatue.RandomName(from);

                        from.Send(new AsciiMessage(targObsidian.Serial, targObsidian.ItemID, MessageType.Regular, 0x59, 3, m_Obsidian.Name, "Something Happened."));

                        return;
                    }
                }

                from.Send(new MessageLocalized(m_Obsidian.Serial, m_Obsidian.ItemID, MessageType.Regular, 0x2C, 3, 500309, m_Obsidian.Name, "")); // Nothing Happens.
            }
        }

        public ObsidianStatue(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteEncodedInt(m_Quantity);
            writer.Write((string)m_StatueName);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Quantity = reader.ReadEncodedInt();
            m_StatueName = reader.ReadString();
        }
    }
}