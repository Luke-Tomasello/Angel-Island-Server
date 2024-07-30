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

/* Scripts\Items\misc\StackableItem.cs
 * ChangeLog:
 *  3/21/2024, Adam: 
 *      Created
 */
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class StackableItem : Item, IScissorable
    {
        private const int FacingOne = 0;
        private const int FacingOneStacked = 1;
        private const int FacingTwo = 2;
        private const int FacingTwoStacked = 3;

        public class GraphicObjects
        {
            private List<int> m_ids = new();
            public List<int> Ids { get { return m_ids; } }
            public GraphicObjects(int facing1)
            {
                m_ids.Add(facing1);
            }
            public GraphicObjects(int facing1, int facing1Stacked)
            {
                m_ids.Add(facing1);
                m_ids.Add(facing1Stacked);
            }
            public GraphicObjects(int facing1, int facing1Stacked, int facing2, int facing2Stacked)
            {
                m_ids.Add(facing1);
                m_ids.Add(facing1Stacked);
                m_ids.Add(facing2);
                m_ids.Add(facing2Stacked);
            }
        }
        GraphicObjects m_graphicObjects = null;
        private string m_plural;
        [Constructable]
        public StackableItem(string name, int facing1)
            : base(facing1)
        {
            m_graphicObjects = new(facing1);
            Construct(name);
        }

        [Constructable]
        public StackableItem(string name, int facing1, int facing1Stacked)
            : base(facing1)
        {
            m_graphicObjects = new(facing1, facing1Stacked);
            Construct(name);
        }

        [Constructable]
        public StackableItem(string name, int facing1, int facing1Stacked, int facing2, int facing2Stacked)
            : base(facing1)
        {
            m_graphicObjects = new(facing1, facing1Stacked, facing2, facing2Stacked);
            Construct(name);
        }

        public StackableItem(string name, int[] table)
            : this(name, table[0])
        {
            switch (table.Length)
            {
                case 4:
                    {
                        m_graphicObjects = new(table[0], table[1], table[2], table[3]);
                        break;
                    }
                case 2:
                    {
                        m_graphicObjects = new(table[0], table[1]);
                        break;
                    }
                default:
                case 1:
                    {
                        m_graphicObjects = new(table[0]);
                        break;
                    }
            }

            Construct(name);
        }
        private void Construct(string name)
        {
            string[] tokens = name.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 2)
                m_plural = tokens[1];
            else
                m_plural = string.Empty;

            name = tokens[0];

            Stackable = true;
            Name = name;
        }
        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted)
                return false;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack.
                return false;
            }

            if (!from.CanSee(this))
            {
                from.SendLocalizedMessage(502800); // You can't see that.
                return false;
            }

            if (Amount == 1)
            {
                from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
                return false;
            }

            StackableItem new_item = null;
            int amount = Amount - 1;
            if (m_graphicObjects == null) { System.Diagnostics.Debug.Assert(false); return false; }
            for (int ix = 0; ix < amount; ix++)
            {
                // construct
                if (m_graphicObjects.Ids.Count == 4)
                    new_item = new StackableItem(Name, m_graphicObjects.Ids[0], m_graphicObjects.Ids[1], m_graphicObjects.Ids[2], m_graphicObjects.Ids[3]);
                if (m_graphicObjects.Ids.Count == 2)
                    new_item = new StackableItem(Name, m_graphicObjects.Ids[0], m_graphicObjects.Ids[1]);
                if (m_graphicObjects.Ids.Count == 1)
                    new_item = new StackableItem(Name, m_graphicObjects.Ids[0]);
                // check and do
                if (!from.Backpack.CheckHold(from, new_item, message: true))
                    new_item.MoveToWorld(from.Location, from.Map);
                else
                    from.Backpack.DropItem(new_item);
                // decrement
                Amount--;
            }

            return true;
        }
        public StackableItem(Serial serial)
            : base(serial)
        {
        }
        public override Item Dupe(int amount)
        {
            if (m_graphicObjects == null) { System.Diagnostics.Debug.Assert(false); return null; }
            if (m_graphicObjects.Ids.Count == 4)
                return base.Dupe(new StackableItem(Name, m_graphicObjects.Ids[0], m_graphicObjects.Ids[1], m_graphicObjects.Ids[2], m_graphicObjects.Ids[3]), amount);
            if (m_graphicObjects.Ids.Count == 2)
                return base.Dupe(new StackableItem(Name, m_graphicObjects.Ids[0], m_graphicObjects.Ids[1]), amount);
            if (m_graphicObjects.Ids.Count == 1)
                return base.Dupe(new StackableItem(Name, m_graphicObjects.Ids[0]), amount);
            else { System.Diagnostics.Debug.Assert(false); return null; }
        }
        protected override void OnAmountChange(int old_value)
        {
            int new_value = Amount;
            bool going_to_1 = new_value == 1 && old_value > 1;
            bool going_multi = new_value > 1 && old_value == 1;

            if (m_graphicObjects == null)
            {   // this is normal during construction. I.e., calling the base during construction sets the amount, but we haven't
                //  yet initialized this table
                return;
            }
            else if (m_graphicObjects.Ids.Count == 1)
            {
                // nothing to do
            }
            else if (m_graphicObjects.Ids.Count == 2)
            {
                if (going_multi)
                {
                    if (ItemID == m_graphicObjects.Ids[FacingOne])              // South/1 Single
                        ItemID = m_graphicObjects.Ids[FacingOneStacked];        // South/1 Stack
                    else
                        ;// error
                }
                else if (going_to_1)
                {
                    if (ItemID == m_graphicObjects.Ids[FacingOneStacked])       // South/1 Stack
                        ItemID = m_graphicObjects.Ids[FacingOne];               // South/1 Single
                    else
                        ;// error
                }
                else
                    ;// error
            }
            else if (m_graphicObjects.Ids.Count == 4)
            {
                if (going_multi)
                {
                    if (ItemID == m_graphicObjects.Ids[FacingOne])              // South/1 Single
                        ItemID = m_graphicObjects.Ids[FacingOneStacked];        // South/1 Stack
                    else if (ItemID == m_graphicObjects.Ids[FacingTwo])         // East/2 Single
                        ItemID = m_graphicObjects.Ids[FacingTwoStacked];        // East/2 Stack
                    else
                        ;// error
                }
                else if (going_to_1)
                {
                    if (ItemID == m_graphicObjects.Ids[FacingOneStacked])       // South/1 Stack
                        ItemID = m_graphicObjects.Ids[FacingOne];               // South/1 Single
                    else if (ItemID == m_graphicObjects.Ids[FacingTwoStacked])  // East/2 Stack
                        ItemID = m_graphicObjects.Ids[FacingTwo];               // East/2 single
                    else
                        ;// error
                }
                else
                    ;// error
            }
        }
        public override bool EquivelentItemID(int itemID)
        {
            return (m_graphicObjects.Ids.Contains(itemID));
        }
        public override string OldSchoolName()
        {
            string oldSchoolName = base.OldSchoolName();

            if (Amount != 1)
                oldSchoolName += m_plural;

            return oldSchoolName;
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            // version 0
            writer.Write(m_graphicObjects.Ids.Count);
            foreach (int id in m_graphicObjects.Ids)
                writer.Write(id);
            writer.Write(m_plural);
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
                        switch (count)
                        {
                            case 4:
                                {
                                    m_graphicObjects = new(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
                                    break;
                                }
                            case 2:
                                {
                                    m_graphicObjects = new(reader.ReadInt(), reader.ReadInt());
                                    break;
                                }
                            case 1:
                                {
                                    m_graphicObjects = new(reader.ReadInt());
                                    break;
                                }
                        }
                        m_plural = reader.ReadString();
                        break;
                    }
            }
        }
    }
}