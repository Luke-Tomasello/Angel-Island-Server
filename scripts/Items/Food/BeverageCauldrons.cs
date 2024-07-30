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

/* Scripts\Items\Food\BeverageCauldrons.cs
 * Changelog
 *  12/19/2023, Yoar
 *      Implemented OnDoubleClick
 *      Attempts to fill a container with the contents of the cauldron
 *  12/6/2023, Adam
 *      created. 
 */

using Server.Network;

namespace Server.Items
{
    public class BaseBeverageCauldron : BaseAddon
    {
        public BaseBeverageCauldron(int item_id, int beverage_hue, string name, bool has_fire)
            : base()
        {
            ItemID = item_id; // Cauldron South or East
            Movable = false;
            Visible = true;
            // beverage graphics
            AddonComponent beverage_gfx = new AddonComponent(0x970);
            beverage_gfx.Hue = beverage_hue;
            beverage_gfx.Movable = false;
            beverage_gfx.Visible = true;
            beverage_gfx.Name = name;
            AddComponent(beverage_gfx, 0, 0, 8);
            if (has_fire)
            {
                AddonComponent fire = new AddonComponent(0xDE3);
                fire.Movable = false;
                fire.Visible = true;
                AddComponent(fire, 0, 0, 0);
            }
        }
        public virtual bool ValidateUse(Mobile from, bool message)
        {
            if (Deleted)
                return false;

            if (!Movable && !Fillable && false)
            {
                Multis.BaseHouse house = Multis.BaseHouse.FindHouseAt(this);

                if (house == null || !house.IsLockedDown(this))
                {
                    if (message)
                        from.SendLocalizedMessage(502946, "", 0x59); // That belongs to someone else.

                    return false;
                }
            }

            if (from.Map != Map || !from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
            {
                if (message)
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.

                return false;
            }

            return true;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsEmpty
        {
            get { return false; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual BeverageType Content
        {
            get { return BeverageType.Ale; }
            set
            {
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Quantity
        {
            get { return 100; }
            set
            {

            }
        }
        public virtual bool Fillable { get { return false; } }
        public override void OnComponentDoubleClick(AddonComponent c, Mobile from)
        {
            OnDoubleClick(from);
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            if (IsEmpty || !ValidateUse(from, true))
                return;

            BaseBeverage toFill = null;

            if (from.Backpack != null)
            {
                foreach (BaseBeverage bev in from.Backpack.FindItemsByType<BaseBeverage>())
                {
                    if (bev.Fillable && bev.Quantity == 0)
                    {
                        toFill = bev;
                        break;
                    }
                }
            }

            if (toFill == null)
            {
                from.SendMessage("You need a container to pour this beverage in.");
                return;
            }

            toFill.Content = Content;
            toFill.Poison = null;
            toFill.Poisoner = null;
            toFill.Quantity = toFill.MaxQuantity;

            from.SendMessage("You fill the container with the contents of the cauldron.");
        }
        public BaseBeverageCauldron(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class MulledWineCauldron : BaseBeverageCauldron
    {
        private static int[] facings = new int[] { 0x975 /*east*/, 0x974 /*south*/};
        [Constructable]
        public MulledWineCauldron()
            : base(item_id: facings[0], beverage_hue: 0x485, name: "mulled wine", has_fire: true)
        {
        }
        public MulledWineCauldron(int facing)
            : base(facings[facing], beverage_hue: 0x485, name: "mulled wine", has_fire: true)
        {
        }
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new MulledWineCauldronDeed(); } }

        public MulledWineCauldron(Serial serial)
            : base(serial)
        {
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public override BeverageType Content
        {
            get { return BeverageType.Wine; }
            set
            {
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class MulledWineCauldronDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new MulledWineCauldron(m_Type); } }
        public override string DefaultName { get { return "mulled wine cauldron"; } }
        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "mulled wine cauldron (east)",
                "mulled wine cauldron (south)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public MulledWineCauldronDeed()
        {
        }

        public MulledWineCauldronDeed(Serial serial)
            : base(serial)
        {
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
        }
    }
}