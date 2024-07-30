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

using System;

namespace Server.Engines.BulkOrders
{
    public class BOBSmallEntry
    {
        private Type m_ItemType;
        private bool m_RequireExceptional;
        private BODType m_DeedType;
        private BulkMaterialType m_Material;
        private int m_AmountCur, m_AmountMax;
        private int m_Number;
        private int m_Graphic;
        private int m_Price;

        public Type ItemType { get { return m_ItemType; } }
        public bool RequireExceptional { get { return m_RequireExceptional; } }
        public BODType DeedType { get { return m_DeedType; } }
        public BulkMaterialType Material { get { return m_Material; } }
        public int AmountCur { get { return m_AmountCur; } }
        public int AmountMax { get { return m_AmountMax; } }
        public int Number { get { return m_Number; } }
        public int Graphic { get { return m_Graphic; } }
        public int Price { get { return m_Price; } set { m_Price = value; } }

        public Item Reconstruct()
        {
            SmallBOD bod = null;

            if (m_DeedType == BODType.Smith)
                bod = new SmallSmithBOD(m_AmountMax, m_RequireExceptional, m_Material, m_AmountCur, m_ItemType, m_Number, m_Graphic);
            else if (m_DeedType == BODType.Tailor)
                bod = new SmallTailorBOD(m_AmountMax, m_RequireExceptional, m_Material, m_AmountCur, m_ItemType, m_Number, m_Graphic);
            else if (m_DeedType == BODType.Tinker)
                bod = new SmallTinkerBOD(m_AmountMax, m_RequireExceptional, m_Material, m_AmountCur, m_ItemType, m_Number, m_Graphic);
            else if (m_DeedType == BODType.Carpenter)
                bod = new SmallCarpenterBOD(m_AmountMax, m_RequireExceptional, m_Material, m_AmountCur, m_ItemType, m_Number, m_Graphic);

            return bod;
        }

        public BOBSmallEntry(SmallBOD bod)
        {
            m_ItemType = bod.Type;
            m_RequireExceptional = bod.RequireExceptional;

            if (bod is SmallTailorBOD)
                m_DeedType = BODType.Tailor;
            else if (bod is SmallSmithBOD)
                m_DeedType = BODType.Smith;
            else if (bod is SmallTinkerBOD)
                m_DeedType = BODType.Tinker;
            else if (bod is SmallCarpenterBOD)
                m_DeedType = BODType.Carpenter;

            m_Material = bod.Material;
            m_AmountCur = bod.AmountCur;
            m_AmountMax = bod.AmountMax;
            m_Number = bod.Number;
            m_Graphic = bod.Graphic;
        }

        public BOBSmallEntry(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        string type = reader.ReadString();

                        if (type != null)
                            m_ItemType = ScriptCompiler.FindTypeByFullName(type);

                        m_RequireExceptional = reader.ReadBool();

                        m_DeedType = (BODType)reader.ReadEncodedInt();

                        m_Material = (BulkMaterialType)reader.ReadEncodedInt();
                        m_AmountCur = reader.ReadEncodedInt();
                        m_AmountMax = reader.ReadEncodedInt();
                        m_Number = reader.ReadEncodedInt();
                        m_Graphic = reader.ReadEncodedInt();
                        m_Price = reader.ReadEncodedInt();

                        break;
                    }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_ItemType == null ? null : m_ItemType.FullName);

            writer.Write((bool)m_RequireExceptional);

            writer.WriteEncodedInt((int)m_DeedType);
            writer.WriteEncodedInt((int)m_Material);
            writer.WriteEncodedInt((int)m_AmountCur);
            writer.WriteEncodedInt((int)m_AmountMax);
            writer.WriteEncodedInt((int)m_Number);
            writer.WriteEncodedInt((int)m_Graphic);
            writer.WriteEncodedInt((int)m_Price);
        }
    }
}