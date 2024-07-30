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

/* Scripts/Engines/BulkOrders/Deeds/SmallBOD.cs
 * CHANGELOG:
 *  11/22/23, Yoar
 *      Added BaseBOD base class
 *  10/31/21, Yoar
 *      Added BulkOrderType getter
 *  10/14/21, Yoar
 *      Bulk Order System overhaul:
 *      - SmallBOD now implements the IBulkOrderDeed interface.
 *      - Disabled reward generation.
 */

using Server.Items;
using System;

namespace Server.Engines.BulkOrders
{
    [TypeAlias("Scripts.Engines.BulkOrders.SmallBOD")]
    public abstract class SmallBOD : BaseBOD
    {
        private int m_AmountCur;
        private Type m_Type;
        private int m_Number;
        private int m_Graphic;

        [CommandProperty(AccessLevel.GameMaster)]
        public int AmountCur { get { return m_AmountCur; } set { m_AmountCur = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Type Type { get { return m_Type; } set { m_Type = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Number { get { return m_Number; } set { m_Number = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Graphic { get { return m_Graphic; } set { m_Graphic = value; } }

        [Constructable]
        public SmallBOD(int amountMax, bool requireExeptional, BulkMaterialType material, int amountCur, Type type, int number, int graphic)
            : base(amountMax, requireExeptional, material)
        {
            m_AmountCur = amountCur;
            m_Type = type;
            m_Number = number;
            m_Graphic = graphic;
        }

        public override bool IsEmpty()
        {
            return (m_AmountCur == 0);
        }

        public override bool IsComplete()
        {
            if (AmountMax <= 0)
                return false; // invalid

            return (m_AmountCur >= AmountMax);
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            base.AddNameProperty(list);

            list.Add(1060654); // small bulk order
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060658, "#{0}\t{1}", m_Number, m_AmountCur); // ~1_val~: ~2_val~
        }

        public virtual void Randomize()
        {
            SmallBulkEntry e = System.GetRandomSmallEntry();

            if (e == null)
                return;

            AmountMax = Utility.RandomList(10, 15, 20);

            if (System.UsesQuality(e.Type))
                RequireExceptional = Utility.RandomBool();
            else
                RequireExceptional = false;

            if (System.UsesMaterial(e.Type))
                Material = System.RandomMaterial(100.0);
            else
                Material = BulkMaterialType.None;

            m_Type = e.Type;
            m_Number = e.Number;
            m_Graphic = e.Graphic;
        }

        public override void Randomize(Mobile from)
        {
            SmallBulkEntry e = System.GetRandomSmallEntry(from);

            if (e == null)
                return;

            double theirSkill = from.Skills[System.Skill].Base;

            if (theirSkill >= 70.1)
                AmountMax = Utility.RandomList(10, 15, 20, 20);
            else if (theirSkill >= 50.1)
                AmountMax = Utility.RandomList(10, 15, 15, 20);
            else
                AmountMax = Utility.RandomList(10, 10, 15, 20);

            if (System.UsesQuality(e.Type) && theirSkill >= 70.1 && System.IsCraftableBy(from, e.Type, true))
                RequireExceptional = (Utility.RandomDouble() < (Math.Min(85.0, theirSkill) + 80.0) / 200.0);
            else
                RequireExceptional = false;

            if (System.UsesMaterial(e.Type) && theirSkill >= 70.1)
                Material = System.RandomMaterial(theirSkill);
            else
                Material = BulkMaterialType.None;

            Type = e.Type;
            Number = e.Number;
            Graphic = e.Graphic;
        }

        public override void DisplayTo(Mobile from)
        {
            from.SendGump(new SmallBODGump(from, this));
        }

        public void BeginCombine(Mobile from)
        {
            if (m_AmountCur < AmountMax)
                from.Target = new SmallBODTarget(this);
            else
                from.SendLocalizedMessage(1045166); // The maximum amount of requested items have already been combined to this deed.
        }

        public void EndCombine(Mobile from, object o)
        {
            if (o is Item && ((Item)o).IsChildOf(from.Backpack))
            {
                Type objectType = o.GetType();

                if (IsComplete())
                {
                    from.SendLocalizedMessage(1045166); // The maximum amount of requested items have already been combined to this deed.
                }
                else if (!IsValidItem((Item)o) || !MatchType(objectType))
                {
                    from.SendLocalizedMessage(1045169); // The item is not in the request.
                }
                else
                {
                    CraftResource resource = GetResource((Item)o);

                    if (Material != BulkMaterialType.None && resource != BulkMaterialInfo.Lookup(Material).Resource)
                    {
                        from.SendLocalizedMessage(System.GetMaterialMessage(true));
                    }
                    else
                    {
                        bool isExceptional = GetExceptional((Item)o);

                        if (RequireExceptional && !isExceptional)
                        {
                            from.SendLocalizedMessage(1045167); // The item must be exceptional.
                        }
                        else
                        {
                            Item item = (Item)o;

                            if (item.Stackable)
                            {
                                int amount = AmountMax - m_AmountCur;

                                if (amount > item.Amount)
                                    amount = item.Amount;

                                item.Consume(amount);
                                AmountCur += amount;
                            }
                            else
                            {
                                item.Delete();
                                AmountCur++;
                            }

                            from.SendLocalizedMessage(1045170); // The item has been combined with the deed.

                            from.SendGump(new SmallBODGump(from, this));

                            if (!IsComplete())
                                BeginCombine(from);
                        }
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(1045158); // You must have the item in your backpack to target it.
            }
        }

        // TODO: Move to BulkOrderSystem?
        private static bool IsValidItem(Item item)
        {
            return true;
        }

        private bool MatchType(Type type)
        {
            if (m_Type == null)
                return false;

            Type[] equivalentTypes = GetEquivalentTypes(m_Type);

            for (int i = 0; i < equivalentTypes.Length; i++)
            {
                if (equivalentTypes[i].IsAssignableFrom(type))
                    return true;
            }

            return false;
        }

        private static readonly Type[][] m_EquivalentTypes = new Type[][]
            {
            };

        private static Type[] GetEquivalentTypes(Type type)
        {
            for (int i = 0; i < m_EquivalentTypes.Length; i++)
            {
                Type[] types = m_EquivalentTypes[i];

                if (types[0] == type)
                    return types;
            }

            return new Type[] { type };
        }

        public static CraftResource GetResource(Item item)
        {
            if (item is BaseArmor)
                return ((BaseArmor)item).Resource;
            else if (item is BaseCraftableItem)
                return ((BaseCraftableItem)item).Resource;
            else if (item is BaseContainer)
                return ((BaseContainer)item).Resource;
            else if (item is BaseInstrument)
                return ((BaseInstrument)item).Resource;
            else if (item is BaseShoes)
                return ((BaseShoes)item).Resource;
            else if (item is BaseWeapon)
                return ((BaseWeapon)item).Resource;
            else
                return CraftResource.None;
        }

        public static bool GetExceptional(Item item)
        {
            if (item is BaseArmor)
                return (((BaseArmor)item).Quality == ArmorQuality.Exceptional);
            else if (item is BaseClothing)
                return (((BaseClothing)item).Quality == ClothingQuality.Exceptional);
            else if (item is BaseCraftableItem)
                return (((BaseCraftableItem)item).Quality == CraftQuality.Exceptional);
            else if (item is BaseContainer)
                return (((BaseContainer)item).Quality == CraftQuality.Exceptional);
            else if (item is BaseInstrument)
                return (((BaseInstrument)item).Quality == InstrumentQuality.Exceptional);
            else if (item is BaseWeapon)
                return (((BaseWeapon)item).Quality == WeaponQuality.Exceptional);
            else
                return false;
        }

        public SmallBOD(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write(m_AmountCur);
            writer.Write(m_Type == null ? null : m_Type.FullName);
            writer.Write(m_Number);
            writer.Write(m_Graphic);
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
                        m_AmountCur = reader.ReadInt();

                        if (version < 1)
                            AmountMax = reader.ReadInt();

                        string type = reader.ReadString();

                        if (type != null)
                            m_Type = ScriptCompiler.FindTypeByFullName(type);

                        m_Number = reader.ReadInt();
                        m_Graphic = reader.ReadInt();

                        if (version < 1)
                        {
                            RequireExceptional = reader.ReadBool();
                            Material = (BulkMaterialType)reader.ReadInt();
                        }

                        break;
                    }
            }
        }
    }
}