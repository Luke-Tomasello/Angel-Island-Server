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

/* Scripts\Items\Special\Rares\Containers\BaseWaterContainer.cs
 * Changelog:
 *  11/15/23, Yoar
 *      Additional chop checks, copied from HarvestTarget
 *	8/18/23, Yoar
 *		Merged from RunUO
 */

using System;

namespace Server.Items
{
    public abstract class BaseWaterContainer : BaseContainer, IHasQuantity, IChopable
    {
        public abstract int EmptyID { get; }
        public abstract int FilledID { get; }
        public abstract int MaxQuantity { get; }

        public override int DefaultGumpID { get { return 0x3E; } }

        public override bool HasAccess(Mobile from)
        {
            return base.HasAccess(from) && Utility.DefaultTownshipAndHouseAccess(this, from);
        }

        private int m_Quantity;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsEmpty
        {
            get { return (m_Quantity <= 0); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull
        {
            get { return (m_Quantity >= MaxQuantity); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Quantity
        {
            get { return m_Quantity; }
            set
            {
                if (value != m_Quantity)
                {
                    m_Quantity = Math.Max(0, Math.Min(MaxQuantity, value));

                    if (!IsLockedDown && !IsSecure)
                        Movable = IsEmpty;

                    ItemID = IsEmpty ? EmptyID : FilledID;

                    if (!IsEmpty)
                    {
                        IEntity rootParent = RootParent as IEntity;

                        if (rootParent != null && rootParent.Map != null && rootParent.Map != Map.Internal)
                            MoveToWorld(rootParent.Location, rootParent.Map);
                    }

                    InvalidateProperties();
                }
            }
        }

        public BaseWaterContainer(bool filled)
            : base(0)
        {
            if (filled)
                Quantity = 1;
            else
                ItemID = EmptyID;
        }

        public override void Open(Mobile from)
        {
            if (IsEmpty)
                base.Open(from);
        }

        public override bool DisplaysContent { get { return IsEmpty; } }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            if (!IsEmpty)
                return false;

            return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
        }

        public void OnChop(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (IsLockedDown || IsSecure)
            {
                from.SendLocalizedMessage(1010019); // Locked down resources cannot be used!
            }
            // 11/15/23, Yoar: Additional chop checks, copied from HarvestTarget
            else if (!IsChildOf(from.Backpack) && !Movable)
            {
                from.SendLocalizedMessage(500462); // You can't destroy that while it is here.
            }
            else if (Engines.Harvest.HarvestTarget.ExplodingContainerExploit(from, this))
            {
                from.SendMessage("You can't destroy that while it still contains items.");
            }
            //
            else
            {
                from.SendLocalizedMessage(500461); // You destroy the item.
                Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
                Destroy();
            }
        }

        public override void OnRelease()
        {
            // if we're filled with water, we remain immovable
            if (!IsEmpty)
                Movable = false;
        }

        public BaseWaterContainer(Serial serial)
            : base(serial)
        {
        }
        private const byte mask = 0x80;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x80); // version

            // version 0x80
            writer.Write((int)m_Quantity);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version;

            if ((Utility.PeekByte(reader) & mask) == 0)
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
                            m_Quantity = reader.ReadInt();
                            break;
                        }
                }
            }
        }
    }
}