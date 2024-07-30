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

/* Scripts/Engines/BulkOrders/Deeds/BaseBOD.cs
 * CHANGELOG:
 *  11/22/23, Yoar
 *      Added BaseBOD base class
 */

namespace Server.Engines.BulkOrders
{
    public abstract class BaseBOD : Item
    {
        public abstract BulkOrderSystem System { get; }

        public override int LabelNumber { get { return 1045151; } } // a bulk order deed
        public override double DefaultWeight { get { return 1.0; } }

        private int m_AmountMax;
        private bool m_RequireExceptional;
        private BulkMaterialType m_Material;

        [CommandProperty(AccessLevel.GameMaster)]
        public int AmountMax
        {
            get { return m_AmountMax; }
            set { m_AmountMax = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RequireExceptional
        {
            get { return m_RequireExceptional; }
            set { m_RequireExceptional = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BulkMaterialType Material
        {
            get { return m_Material; }
            set { m_Material = value; InvalidateProperties(); }
        }

        public BaseBOD(int amountMax, bool requireExceptional, BulkMaterialType material)
            : base(Core.RuleSets.AOSRules() ? 0x2258 : 0x14EF)
        {
            LootType = LootType.Blessed;
            Hue = System.DeedHue;

            m_AmountMax = amountMax;
            m_RequireExceptional = requireExceptional;
            m_Material = material;
        }

        public abstract bool IsEmpty();
        public abstract bool IsComplete();
        public abstract void DisplayTo(Mobile from);
        public abstract void Randomize(Mobile from);

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_RequireExceptional)
                list.Add(1045141); // All items must be exceptional.

            if (m_Material != BulkMaterialType.None)
                list.Add(BulkMaterialInfo.Lookup(m_Material).RequireLabel.Number);

            list.Add(1060656, m_AmountMax.ToString()); // amount to make: ~1_val~
        }

        public override void OnDoubleClickNotAccessible(Mobile from)
        {
            DisplayTo(from);
        }

        public override void OnDoubleClickSecureTrade(Mobile from)
        {
            DisplayTo(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1045156); // You must have the deed in your backpack to use it.
            else
                DisplayTo(from);
        }

        public BaseBOD(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x80); // version

            writer.WriteEncodedInt(m_AmountMax);
            writer.Write((bool)m_RequireExceptional);
            writer.WriteEncodedInt((int)m_Material);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version;

            if ((Utility.PeekByte(reader) & 0x80) == 0)
            {
                version = 0; // old version
            }
            else
            {
                version = reader.ReadByte();

                switch (version)
                {
                    case 0x80:
                        {
                            m_AmountMax = reader.ReadEncodedInt();
                            m_RequireExceptional = reader.ReadBool();
                            m_Material = (BulkMaterialType)reader.ReadEncodedInt();

                            break;
                        }
                }
            }

            if (Weight == 0.0)
                Weight = 1.0;

            if (Core.AOS && ItemID == 0x14EF)
                ItemID = 0x2258;

            if (Parent == null && Map == Map.Internal && Location == Point3D.Zero)
                Delete();
        }
    }
}