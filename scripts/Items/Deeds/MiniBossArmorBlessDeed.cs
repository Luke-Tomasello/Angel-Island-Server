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

/* Items/Deeds/MiniBossArmorBlessDeed.cs
 * ChangeLog:
 *  10/10/21, Adam
 *      Remove the nuking of the MeditationAllowance
 *	09/4/21, Adam
 *		created.
 */

using Server.Gumps;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Items
{
    public class MiniBossArmorBlessTarget : Target // Create our targeting class (which we derive from the base target class)
    {
        private MiniBossArmorBlessDeed m_Deed;

        public MiniBossArmorBlessTarget(MiniBossArmorBlessDeed deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (CheckTarget(target))
            {
                Item item = (Item)target;
                if (m_Deed.Deleted)
                {
                    from.SendMessage("That deed is no longer valid.");
                }
                else if (item.GetFlag(LootType.Blessed) || item.BlessedFor == from /*|| (Mobile.InsuranceEnabled && item.Insured)*/ ) // Check if its already newbied (blessed)
                {
                    from.SendLocalizedMessage(1045113); // That item is already blessed
                }
                else if (!item.GetFlag(LootType.Regular))
                {
                    from.SendLocalizedMessage(1045114); // You can not bless that item
                }
                else
                {
                    if (item.RootParent != from) // Make sure its in their pack or they are wearing it
                    {
                        from.SendLocalizedMessage(500509); // You cannot bless that object
                    }
                    else
                    {
                        BaseArmor ba = target as BaseArmor;
                        ba.LootType = LootType.Blessed;
                        ba.ProtectionLevel = ArmorProtectionLevel.Regular;
                        ba.DurabilityLevel = ArmorDurabilityLevel.Indestructible;
                        ba.Quality = ArmorQuality.Regular;
                        ba.Identified = true;
                        from.SendLocalizedMessage(1010026); // You bless the item....

                        m_Deed.Delete(); // Delete the bless deed
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(500509); // You cannot bless that object
            }
        }

        private bool CheckTarget(object target)
        {
            // must be armor
            if (target is BaseArmor == false)
                return false;

            BaseArmor ba = target as BaseArmor;

            // must be one of the special hues
            List<int> hues = new List<int>
            {
                1645,   // hellish
                1109,   // unholy
                1367,   // WyrmSkin
                1364,   // ArcticStorm
                2101,   // CorpseSkin
                1236,   // DreadSteel
            };

            if (hues.Contains(ba.Hue) == false)
                return false;

            // cannot be player crafted
            if (ba.PlayerCrafted == true)
                return false;

            return true;
        }
    }

    public class MiniBossArmorBlessDeed : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public MiniBossArmorBlessDeed()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "a mini boss armor bless deed";
            LootType = LootType.Blessed;
        }

        public MiniBossArmorBlessDeed(Serial serial)
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
            LootType = LootType.Blessed;

            int version = reader.ReadInt();
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from) // Override double click of the deed to call our target
        {
            if (!IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendGump(new MiniBossArmorBlessGump(from, this));
            }
        }
    }
}