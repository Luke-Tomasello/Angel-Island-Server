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

/* Scripts\Items\Misc\Gold.cs
 * ChangeLog
 *  1/8/08, Adam
 *		- Add new OnItemLifted() handler to invoke our WealthTracker system
 *		- InvokeWealthTracker
 */

using Server.Diagnostics;			// log helper
using System;

namespace Server.Items
{
    public class Gold : Item
    {
        [Constructable]
        public Gold()
            : this(1)
        {
        }

        [Constructable]
        public Gold(int amountFrom, int amountTo)
            : this(Utility.Random(amountFrom, amountTo - amountFrom))
        {
        }

        [Constructable]
        public Gold(int amount)
            : base(0xEED)
        {
            Stackable = true;
            Weight = 0.02;
            Amount = amount;
        }

        public Gold(Serial serial)
            : base(serial)
        {
        }

        public override int GetDropSound()
        {
            if (Amount <= 1)
                return 0x2E4;
            else if (Amount <= 5)
                return 0x2E5;
            else
                return 0x2E6;
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            // see if this item has already been Audited
            if (Audited == false)
            { // if not track it
                WealthTrackerEventArgs e = new WealthTrackerEventArgs(AuditType.GoldLifted, this, this.Parent, from);
                try
                {
                    EventSink.InvokeWealthTracker(e);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }
            base.OnItemLifted(from, item);
        }

        protected override void OnAmountChange(int oldValue)
        {
            TotalGold = (TotalGold - oldValue) + Amount;
        }

        public override void UpdateTotals()
        {
            base.UpdateTotals();

            SetTotalGold(this.Amount);
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Gold(amount), amount);
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