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

/* Scripts/Items/Skill Items/Tailor Items/Misc/DyeTub.cs
 * ChangeLog:
 *  11/27/23, Yoar
 *      Added UsesRemaining
 */

using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public interface IDyable
    {
        bool Dye(Mobile from, DyeTub sender);
    }

    public class DyeTub : Item
    {
        private bool m_Redyable;
        private int m_DyedHue;
        private int m_UsesRemaining;

        public virtual CustomHuePicker CustomHuePicker { get { return null; } }

        public virtual bool AllowRunebooks
        {
            get { return false; }
        }

        public virtual bool AllowFurniture
        {
            get { return false; }
        }

        public virtual bool AllowStatuettes
        {
            get { return false; }
        }

        public virtual bool AllowLeather
        {
            get { return false; }
        }

        public virtual bool AllowDyables
        {
            get { return true; }
        }

        public virtual bool LimitedUses
        {
            get { return (m_UsesRemaining > 0); }
        }

        public virtual bool ReplaceOnEmptied
        {
            get { return false; }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_UsesRemaining);

            writer.Write((bool)m_Redyable);
            writer.Write((int)m_DyedHue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_UsesRemaining = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Redyable = reader.ReadBool();
                        m_DyedHue = reader.ReadInt();

                        break;
                    }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Redyable
        {
            get
            {
                return m_Redyable;
            }
            set
            {
                m_Redyable = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DyedHue
        {
            get
            {
                return m_DyedHue;
            }
            set
            {
                if (m_Redyable)
                {
                    m_DyedHue = value;
                    Hue = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get
            {
                return m_UsesRemaining;
            }
            set
            {
                m_UsesRemaining = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public DyeTub()
            : base(0xFAB)
        {
            Weight = 10.0;
            m_Redyable = true;
        }

        public DyeTub(Serial serial)
            : base(serial)
        {
        }

        public void SetDyedHue(int hue)
        {
            Hue = m_DyedHue = hue;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (LimitedUses)
                list.Add(1060584, m_UsesRemaining.ToString()); // uses remaining: ~1_val~
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (LimitedUses)
                LabelToAffix(from, 1017323, AffixType.Append, ": " + m_UsesRemaining.ToString()); // Durability
        }

        public virtual void ConsumeUse(Mobile from)
        {
            if (--m_UsesRemaining <= 0)
            {
                if (ReplaceOnEmptied)
                    ReplaceWith(new DyeTub());
                else
                    Delete();
            }
        }

        // Select the clothing to dye.
        public virtual int TargetMessage { get { return 500859; } }

        // You can not dye that.
        public virtual int FailMessage { get { return 1042083; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.SendLocalizedMessage(TargetMessage);
                from.Target = new InternalTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        private class InternalTarget : Target
        {
            private DyeTub m_Tub;

            public InternalTarget(DyeTub tub)
                : base(1, false, TargetFlags.None)
            {
                m_Tub = tub;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item)
                {
                    Item item = (Item)targeted;

                    if (item is IDyable && m_Tub.AllowDyables)
                    {
                        if (!from.InRange(m_Tub.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                            from.SendLocalizedMessage(500446); // That is too far away.
                        else if (item.Parent is Mobile)
                            from.SendLocalizedMessage(500861); // Can't Dye clothing that is being worn.
                        else if (((IDyable)item).Dye(from, m_Tub))
                        {
                            from.PlaySound(0x23E);

                            if (m_Tub.LimitedUses)
                                m_Tub.ConsumeUse(from);
                        }
                    }
                    else if ((FurnitureAttribute.Check(item) || (item is PotionKeg)) && m_Tub.AllowFurniture)
                    {
                        if (!from.InRange(m_Tub.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                        {
                            from.SendLocalizedMessage(500446); // That is too far away.
                        }
                        else
                        {
                            bool okay = (item.IsChildOf(from.Backpack));

                            if (!okay)
                            {
                                if (item.Parent == null)
                                {
                                    BaseHouse house = BaseHouse.FindHouseAt(item);

                                    if (house == null || !house.IsLockedDown(item))
                                        from.SendLocalizedMessage(501022); // Furniture must be locked down to paint it.
                                    else if (!house.IsCoOwner(from))
                                        from.SendLocalizedMessage(501023); // You must be the owner to use this item.
                                    else
                                        okay = true;
                                }
                                else
                                {
                                    from.SendLocalizedMessage(1048135); // The furniture must be in your backpack to be painted.
                                }
                            }

                            if (okay)
                            {
                                item.Hue = m_Tub.DyedHue;
                                from.PlaySound(0x23E);

                                if (m_Tub.LimitedUses)
                                    m_Tub.ConsumeUse(from);
                            }
                        }
                    }
                    else if ((item is Runebook || item is RecallRune) && m_Tub.AllowRunebooks)
                    {
                        if (!from.InRange(m_Tub.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                        {
                            from.SendLocalizedMessage(500446); // That is too far away.
                        }
                        else if (!item.Movable)
                        {
                            from.SendLocalizedMessage(1049776); // You cannot dye runes or runebooks that are locked down.
                        }
                        else
                        {
                            item.Hue = m_Tub.DyedHue;
                            from.PlaySound(0x23E);

                            if (m_Tub.LimitedUses)
                                m_Tub.ConsumeUse(from);
                        }
                    }
                    else if (item is MonsterStatuette && m_Tub.AllowStatuettes)
                    {
                        if (!from.InRange(m_Tub.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                        {
                            from.SendLocalizedMessage(500446); // That is too far away.
                        }
                        else if (!item.Movable)
                        {
                            from.SendLocalizedMessage(1049779); // You cannot dye statuettes that are locked down.
                        }
                        else
                        {
                            item.Hue = m_Tub.DyedHue;
                            from.PlaySound(0x23E);

                            if (m_Tub.LimitedUses)
                                m_Tub.ConsumeUse(from);
                        }
                    }
                    else if ((item is BaseArmor && (((BaseArmor)item).MaterialType == ArmorMaterialType.Leather || ((BaseArmor)item).MaterialType == ArmorMaterialType.Studded)) && m_Tub.AllowLeather)
                    {
                        if (!from.InRange(m_Tub.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                        {
                            from.SendLocalizedMessage(500446); // That is too far away.
                        }
                        else if (!item.Movable)
                        {
                            from.SendLocalizedMessage(1042419); // You may not dye leather items which are locked down.
                        }
                        else if (item.Parent is Mobile)
                        {
                            from.SendLocalizedMessage(500861); // Can't Dye clothing that is being worn.
                        }
                        else
                        {
                            item.Hue = m_Tub.DyedHue;
                            from.PlaySound(0x23E);

                            if (m_Tub.LimitedUses)
                                m_Tub.ConsumeUse(from);
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(m_Tub.FailMessage);
                    }
                }
                else
                {
                    from.SendLocalizedMessage(m_Tub.FailMessage);
                }
            }
        }
    }
}