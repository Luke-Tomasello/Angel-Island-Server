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

/* Changelog
 * Scripts/Items/Deeds/CommodityDeed.cs
 *  4/14/22, Yoar
 *      Removed Name override.
 *      Cleaned up OnSingleClick.
 *	12/28/05, weaver
 *		Added public accessors to Type, CommodityAmount and Description.
 *	12/23/05 TK
 *		Added null check to SetOldVersionCommodity
 *	12/23/05 - Pix
 *		Changed the way CommodityDeeds work.  Now instead of referencing an
 *		internalized item, they consume the commodity and store its type, amount,
 *		and description.
 * 
 *	01/28/05 - Taran Kain
 *		Added override property Name to return m_Commodity.Description, so that it would
 *		properly work with vendor sell list.
 */

using Server.Targeting;
using System;

namespace Server.Items
{
    public interface ICommodity
    {
        string Description { get; }
        //int DescriptionNumber { get; }
    }

    public class CommodityDeed : Item
    {
        //private Item m_Commodity;
        private Type m_Type;
        private int m_Amount;
        private int m_LabelNumber;
        private string m_Description = "a commodity deed";

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Commodity
        {
            get
            {
                Item commodity = null;
                try
                {
                    if (m_Type != null)
                    {
                        System.Reflection.ConstructorInfo ci = m_Type.GetConstructor(new Type[0]);
                        if (ci != null)
                            commodity = ci.Invoke(null) as Item;

                        if (commodity != null)
                        {
                            commodity.Amount = m_Amount;
                        }
                    }
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                return commodity;
                //return m_Commodity;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Type Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CommodityAmount
        {
            get { return m_Amount; }
            set { m_Amount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }


        public bool SetCommodity(Item item)
        {
            InvalidateProperties();

            if (m_Type == null && item is ICommodity)
            {
                m_Type = item.GetType();
                m_Amount = item.Amount;
                m_LabelNumber = item.LabelNumber;
                m_Description = ((ICommodity)item).Description;

                InvalidateProperties();

                return true;
            }
            //			if ( m_Commodity == null && item is ICommodity )
            //			{
            //				m_Commodity = item;
            //				m_Commodity.Internalize();
            //				InvalidateProperties();
            //
            //				return true;
            //			}
            else
            {
                return false;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            //OLD version 0
            //writer.Write( m_Commodity );

            writer.Write(this.m_Amount);
            if (this.m_Amount > 0)
            {
                writer.Write(this.m_Type.Name);
                writer.Write(this.m_LabelNumber);
                writer.Write(this.m_Description);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Amount = reader.ReadInt();
                        if (m_Amount > 0)
                        {
                            string sType = reader.ReadString();
                            m_Type = ScriptCompiler.FindTypeByName(sType);
                            m_LabelNumber = reader.ReadInt();
                            m_Description = reader.ReadString();
                            Hue = 0x592;
                        }
                        break;
                    }
                case 0:
                    {
                        //m_Commodity = reader.ReadItem();
                        m_oldversionCommodity = reader.ReadItem();
                        Timer.DelayCall(TimeSpan.FromSeconds(3.0), new TimerCallback(SetOldVersionCommodityInfo));

                        break;
                    }
            }
        }

        private Item m_oldversionCommodity;
        private void SetOldVersionCommodityInfo()
        {
            SetCommodity(m_oldversionCommodity);
            if (m_oldversionCommodity != null)
                m_oldversionCommodity.Delete();
        }

        public CommodityDeed(Item commodity)
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = 0x47;

            //m_Commodity = commodity;
            if (commodity != null)
            {
                m_Type = commodity.GetType();
                m_Amount = commodity.Amount;
                m_LabelNumber = commodity.LabelNumber;
                if (commodity is ICommodity)
                    m_Description = ((ICommodity)commodity).Description;
            }
        }

        [Constructable]
        public CommodityDeed()
            : this(null)
        {
        }

        public CommodityDeed(Serial serial)
            : base(serial)
        {
        }

        public override void OnDelete()
        {
            //if ( m_Commodity != null )
            //	m_Commodity.Delete();

            base.OnDelete();
        }

        public override int LabelNumber { get { return m_Type == null ? 1047016 : 1047017; } }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Type != null)
            {
                list.Add(1060658, "#{0}\t{1}", m_LabelNumber, m_Amount); // ~1_val~: ~2_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Type != null)
            {
                LabelTo(from, m_Description);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            int number;

            BankBox box = from.BankBox;

            //if ( m_Commodity != null )
            if (m_Type != null)
            {
                if (box != null && IsChildOf(box))
                {
                    number = 1047031; // The commodity has been redeemed.

                    Item commodity = null;
                    System.Reflection.ConstructorInfo ci = m_Type.GetConstructor(new Type[0]);
                    if (ci != null)
                        commodity = ci.Invoke(null) as Item;

                    if (commodity != null)
                    {
                        commodity.Amount = m_Amount;

                        box.DropItem(commodity);

                        Delete();
                    }
                    else
                    {
                        from.SendMessage("Error with Commodity Deed - please contact GM.");
                    }
                }
                else
                {
                    number = 1047024; // To claim the resources ....
                }
            }
            else if (box == null || !IsChildOf(box))
            {
                number = 1047026; // That must be in your bank box to use it.
            }
            else
            {
                number = 1047029; // Target the commodity to fill this deed with.

                from.Target = new InternalTarget(this);
            }

            from.SendLocalizedMessage(number);
        }

        private class InternalTarget : Target
        {
            private CommodityDeed m_Deed;

            public InternalTarget(CommodityDeed deed)
                : base(3, false, TargetFlags.None)
            {
                m_Deed = deed;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Deed.Deleted)
                    return;

                int number;

                if (m_Deed.Commodity != null)
                {
                    number = 1047028; // The commodity deed has already been filled.
                }
                else if (targeted is Item)
                {
                    BankBox box = from.BankBox;

                    if (box != null && m_Deed.IsChildOf(box) && ((Item)targeted).IsChildOf(box))
                    {
                        if (m_Deed.SetCommodity((Item)targeted))
                        {
                            number = 1047030; // The commodity deed has been filled.
                            ((Item)targeted).Delete();
                            m_Deed.Hue = 0x592;
                        }
                        else
                        {
                            number = 1047027; // That is not a commodity the bankers will fill a commodity deed with.
                        }
                    }
                    else
                    {
                        number = 1047026; // That must be in your bank box to use it.
                    }
                }
                else
                {
                    number = 1047027; // That is not a commodity the bankers will fill a commodity deed with.
                }

                from.SendLocalizedMessage(number);
            }
        }
    }
}