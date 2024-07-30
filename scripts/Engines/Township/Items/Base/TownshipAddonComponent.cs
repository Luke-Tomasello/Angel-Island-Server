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

/* Engines/Township/Items/Base/TownshipAddonComponent.cs
 * CHANGELOG:
 * 8/22/23, Yoar
 *	    Initial version.
 */

using Server.Items;
using System.Collections;

namespace Server.Township
{
    /*
	 * This class can be inserted into any class inheriting from Item
	 * and serializing with a version less than 128.
	 */
    public class TownshipAddonComponent : AddonComponent
    {
        [Constructable]
        public TownshipAddonComponent(int itemID)
            : base(itemID)
        {
            Movable = false;
        }

        [Constructable]
        public TownshipAddonComponent(int itemID, int count)
            : this(Utility.Random(itemID, count))
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Addon is ITownshipItem && ((ITownshipItem)Addon).HitsMax > 0)
                TownshipItemHelper.Inspect(from, (ITownshipItem)Addon);
        }

        public override void OnDoubleClick(Mobile from)
        {
            // 9/8/23, Yoar: Disabled double-click attack
#if false
            if (Addon is ITownshipItem && ((ITownshipItem)Addon).HitsMax > 0 && from.Warmode)
            {
                if (!from.InRange(this.GetWorldLocation(), 2))
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                else
                    TownshipItemHelper.BeginDamageDelayed(from, (ITownshipItem)Addon);
            }
            else
#endif
            {
                base.OnDoubleClick(from);
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            TownshipItemHelper.AddContextMenuEntries(this, from, list);
        }

        public TownshipAddonComponent(Serial serial)
            : base(serial)
        {
        }
        private const byte mask = 0x80;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x80); // version
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
                            break;
                        }
                }
            }
        }
    }
}