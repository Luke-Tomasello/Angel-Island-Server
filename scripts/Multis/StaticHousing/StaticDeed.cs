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

/* Scripts/Multis/StaticHousing/StaticDeed.cs
 *  Changelog:
 *  6/10/22, Yoar
 *      Removed the 'm_Blueprint' field from StaticDeed. Instead, I added a TC-only
 *      static housing deed: TempStaticDeed. These deeds don't work by HID. Rather,
 *      they carry a ref of the blueprint directly. Used to test static house
 *      captures on TC.
 *  6/5/22, Yoar
 *      Added 'm_Blueprint' field. The value of 'm_Blueprint' determines what static
 *      house is placed using this deed. This way, we can place blueprints directly.
 *  6/3/22, Yoar
 *      Reworked constructor signature - no longer requiring description input.
 *      Instead, get the description from the blueprint.
 *  9/17/21, Yoar
 *      Static housing revamp
 *	8/12/07, Adam
 *		change HouseID read-level access from Admin to GM
 *	6/11/07, Pix
 *		Changed constructor to have the Name set based on the ID.
 *		This is needed so that the deed is displayed correctly in the vendor's list.
 *	06/08/2007, plasma
 *		Initial creation
 */

using Server.Multis.Deeds;
using System;
using HouseBlueprint = Server.Multis.StaticHousing.StaticHouseHelper.HouseBlueprint;

namespace Server.Multis.StaticHousing
{
    public class StaticDeed : HouseDeed
    {
        /* TODO: The deed price is dynamic! If, somehow, the price of the blueprint has changed,
         * then also will the price of the deed change. Can this be exploited?
         * 
         * Example:
         * 1. Player A buys a static house deed for a certain house.
         * 2. The design of that house gets revised and increases in cost.
         * 3. The deed of player A also increases in cost. The deed is now worth more gold!
         *    Can player A sell the deed to a real estate broker for profit?
         */
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price
        {
            get
            {
                HouseBlueprint blueprint = StaticHouseHelper.GetBlueprint(m_HouseID);

                if (blueprint == null)
                    return StaticHouseHelper.PriceError;

                return blueprint.Price;
            }
        }

        private string m_HouseID;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public string HouseID
        {
            get { return m_HouseID; }
            set
            {
                if (m_HouseID != value)
                {
                    m_HouseID = value;

                    HouseBlueprint blueprint = StaticHouseHelper.GetBlueprint(m_HouseID);

                    if (blueprint != null)
                    {
                        this.MultiID = StaticHouseHelper.GetFoundationID(blueprint.Width, blueprint.Height);
                        this.Name = string.Concat("deed to a ", blueprint.Description);
                    }
                    else
                    {
                        this.MultiID = 0x14F0;
                        this.Name = null;
                    }

                    InvalidateProperties();
                }
            }
        }

        [Constructable]
        public StaticDeed(string houseID)
            : base(0x14F0, 0x0, new Point3D(0, 4, 0))
        {
            this.HouseID = houseID;
        }
        public override Item Dupe(int amount)
        {
            StaticDeed new_deed = new StaticDeed(HouseID);
            return base.Dupe(new_deed, amount);
        }
        public StaticDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            HouseBlueprint blueprint = StaticHouseHelper.GetBlueprint(m_HouseID);

            if (blueprint == null)
                return null;

            return new StaticHouse(owner, blueprint);
        }

        public override Rectangle2D[] Area { get { return null; } } // unused

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            else if (!StaticHouseHelper.BlueprintExists(m_HouseID))
                from.SendMessage("Invalid house ID: {0}", m_HouseID);
            else
                from.Target = new HousePlacementTarget(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((string)m_HouseID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    m_HouseID = reader.ReadString();
                    break;
            }
        }
    }

    // temporary static house deed for TC only
    public class TempStaticDeed : HouseDeed
    {
        public override int Price { get { return 0; } } // not applicable

        private HouseBlueprint m_Blueprint;

        public HouseBlueprint Blueprint
        {
            get { return m_Blueprint; }
            set
            {
                if (m_Blueprint != value)
                {
                    m_Blueprint = value;

                    if (m_Blueprint != null)
                    {
                        this.MultiID = StaticHouseHelper.GetFoundationID(m_Blueprint.Width, m_Blueprint.Height);
                        this.Name = string.Concat("deed to a ", m_Blueprint.Description);
                    }
                    else
                    {
                        this.MultiID = 0x14F0;
                        this.Name = null;
                    }

                    InvalidateProperties();
                }
            }
        }

        public TempStaticDeed(HouseBlueprint blueprint)
            : base(0x14F0, 0x0, new Point3D(0, 4, 0))
        {
            this.Blueprint = blueprint;
        }

        public TempStaticDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            if (m_Blueprint == null)
                return null;

            return new StaticHouse(owner, m_Blueprint);
        }

        public override Rectangle2D[] Area { get { return null; } } // unused

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add("Test Center Only");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, "Test Center Only");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            else if (!Core.UOTC_CFG)
                from.SendMessage("This can only be used on Test Center.");
            else if (m_Blueprint == null)
                from.SendMessage("The deed does not contain a static house blueprint.");
            else
                from.Target = new HousePlacementTarget(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            Delete(); // delete the deed
        }
    }
}