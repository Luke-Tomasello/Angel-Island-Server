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

/* scripts\Items\misc\ForSaleSign.cs
 *	ChangeLog :
 *	1/23/2024, Adam
 *		Created. 
 *		For sale signs for the custom housing area
 */

using Server.Mobiles;
using Server.Multis;
using Server.Multis.StaticHousing;
using Server.Targeting;

namespace Server.Items
{
    public class ForSaleSign : Item
    {
        private StaticDeed m_staticHouseDeed = null;
        [CommandProperty(AccessLevel.Administrator)]
        public StaticDeed Deed
        {
            get { return m_staticHouseDeed; }
            set
            {
                if (value != null && value is not StaticDeed)
                {
                    this.SendSystemMessage("That is not a static deed");
                    return;
                }

                if (m_staticHouseDeed != null)
                {
                    m_staticHouseDeed.IsIntMapStorage = false;
                    m_staticHouseDeed.Delete();
                }

                if (value != null)
                {
                    m_staticHouseDeed = new StaticDeed(value.HouseID);
                    m_staticHouseDeed.MoveToIntStorage();
                    this.SendSystemMessage("Updated.");
                }
            }
        }
        [Constructable]
        public ForSaleSign()
            : base(0xBD1)
        {
            Movable = false;
            Visible = true;
            Hue = 0x738;
            Name = "For Sale";
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            if (m_staticHouseDeed == null)
                LabelTo(from, "Coming soon!");
            else if (m_staticHouseDeed.Name != null)
            {
                int price = RealEstateBroker.ComputeHousingMarkupForSiege(m_staticHouseDeed.Price);
                LabelTo(from, m_staticHouseDeed.Name.Replace("deed to a ", ""));
                LabelTo(from, "Price {0}", price.ToString("N0"));
            }
            else
                LabelTo(from, "Error. Please page a GM.");
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel < AccessLevel.GameMaster)
                return;

            from.SendMessage("Target a house sign to mate with...");
            from.Target = new ForSaleSignTarget(this);
        }
        public class ForSaleSignTarget : Target
        {
            ForSaleSign m_Sign;
            public ForSaleSignTarget(ForSaleSign sign)
                : base(17, true, TargetFlags.None)
            {
                m_Sign = sign;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is HouseSign hs && hs.Structure is StaticHouse sh)
                {
                    StaticDeed temp = (StaticDeed)sh.Sign.Structure.GetDeed();
                    m_Sign.Deed = temp;    // these signs dupe the deed, so we need to delete the temp one
                    temp.Delete();
                }
                else
                {
                    from.SendMessage("That is not a static house sign");
                    return;
                }
            }
        }
        public ForSaleSign(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
            writer.Write(m_staticHouseDeed);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_staticHouseDeed = (StaticDeed)reader.ReadItem();
        }
    }
}