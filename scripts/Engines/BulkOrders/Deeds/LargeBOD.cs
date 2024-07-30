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

/* Scripts/Engines/BulkOrders/Deeds/LargeBOD.cs
 * CHANGELOG:
 *  11/22/23, Yoar
 *      Added BaseBOD base class
 *  11/7/21, Yoar
 *      Added Empty getter
 *  10/31/21, Yoar
 *      Added BulkOrderType getter
 *  10/14/21, Yoar
 *      Bulk Order System overhaul:
 *      - LargeBOD now implements the IBulkOrderDeed interface.
 *      - Disabled reward generation.
 */

namespace Server.Engines.BulkOrders
{
    [TypeAlias("Scripts.Engines.BulkOrders.LargeBOD")]
    public abstract class LargeBOD : BaseBOD
    {
        private LargeBulkEntry[] m_Entries;

        public LargeBulkEntry[] Entries { get { return m_Entries; } set { m_Entries = value; InvalidateProperties(); } }

        public LargeBOD(int amountMax, bool requireExeptional, BulkMaterialType material, LargeBulkEntry[] entries)
            : base(amountMax, requireExeptional, material)
        {
            m_Entries = entries;
        }

        public override bool IsEmpty()
        {
            if (m_Entries.Length == 0)
                return false; // invalid

            for (int i = 0; i < m_Entries.Length; i++)
            {
                if (m_Entries[i].Amount != 0)
                    return false;
            }

            return true;
        }

        public override bool IsComplete()
        {
            if (AmountMax <= 0 || m_Entries.Length == 0)
                return false; // invalid

            for (int i = 0; i < m_Entries.Length; i++)
            {
                if (m_Entries[i].Amount < AmountMax)
                    return false;
            }

            return true;
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            base.AddNameProperty(list);

            list.Add(1060655); // large bulk order
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            for (int i = 0; i < m_Entries.Length; ++i)
                list.Add(1060658 + i, "#{0}\t{1}", m_Entries[i].Details.Number, m_Entries[i].Amount); // ~1_val~: ~2_val~
        }

        public virtual void Randomize()
        {
            SmallBulkEntry[] entries = System.GetRandomLargeEntries();

            if (entries.Length == 0)
                return;

            AmountMax = Utility.RandomList(10, 15, 20, 20);

            if (System.UsesQuality(entries[0].Type))
                RequireExceptional = (Utility.RandomDouble() < 0.825);
            else
                RequireExceptional = false;

            if (System.UsesMaterial(entries[0].Type))
                Material = System.RandomMaterial(100.0);
            else
                Material = BulkMaterialType.None;

            Entries = LargeBulkEntry.ConvertEntries(this, entries);
        }

        public override void Randomize(Mobile from)
        {
            Randomize(); // randomize regardless of skill
        }

        public override void DisplayTo(Mobile from)
        {
            from.SendGump(new LargeBODGump(from, this));
        }

        public void BeginCombine(Mobile from)
        {
            if (!IsComplete())
                from.Target = new LargeBODTarget(this);
            else
                from.SendLocalizedMessage(1045166); // The maximum amount of requested items have already been combined to this deed.
        }

        public void EndCombine(Mobile from, object o)
        {
            if (o is Item && ((Item)o).IsChildOf(from.Backpack))
            {
                if (o is SmallBOD)
                {
                    SmallBOD small = (SmallBOD)o;

                    LargeBulkEntry entry = null;

                    for (int i = 0; entry == null && i < m_Entries.Length; ++i)
                    {
                        if (m_Entries[i].Details.Type == small.Type)
                            entry = m_Entries[i];
                    }

                    if (entry == null)
                    {
                        from.SendLocalizedMessage(1045160); // That is not a bulk order for this large request.
                    }
                    else if (RequireExceptional && !small.RequireExceptional)
                    {
                        from.SendLocalizedMessage(1045161); // Both orders must be of exceptional quality.
                    }
                    else if (Material != BulkMaterialType.None && small.Material != Material)
                    {
                        from.SendLocalizedMessage(System.GetMaterialMessage(false));
                    }
                    else if (AmountMax != small.AmountMax)
                    {
                        from.SendLocalizedMessage(1045163); // The two orders have different requested amounts and cannot be combined.
                    }
                    else if (small.AmountCur < small.AmountMax)
                    {
                        from.SendLocalizedMessage(1045164); // The order to combine with is not completed.
                    }
                    else if (entry.Amount >= AmountMax)
                    {
                        from.SendLocalizedMessage(1045166); // The maximum amount of requested items have already been combined to this deed.
                    }
                    else
                    {
                        entry.Amount += small.AmountCur;
                        small.Delete();

                        from.SendLocalizedMessage(1045165); // The orders have been combined.

                        from.SendGump(new LargeBODGump(from, this));

                        if (!IsComplete())
                            BeginCombine(from);
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1045159); // That is not a bulk order.
                }
            }
            else
            {
                from.SendLocalizedMessage(1045158); // You must have the item in your backpack to target it.
            }
        }

        public LargeBOD(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_Entries.Length);

            for (int i = 0; i < m_Entries.Length; ++i)
                m_Entries[i].Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        if (version < 1)
                        {
                            AmountMax = reader.ReadInt();
                            RequireExceptional = reader.ReadBool();
                            Material = (BulkMaterialType)reader.ReadInt();
                        }

                        m_Entries = new LargeBulkEntry[reader.ReadInt()];

                        for (int i = 0; i < m_Entries.Length; ++i)
                            m_Entries[i] = new LargeBulkEntry(this, reader);

                        break;
                    }
            }
        }
    }
}