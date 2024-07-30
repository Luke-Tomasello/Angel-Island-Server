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

/* Items/Deeds/ClothingBlessDeed.cs
 * ChangeLog:
 *	02/25/05, Adam
 *		remove references to 'Insured' (no more insurance)
 *		reuse the  flag as 'PlayerCrafted' (item.cs)
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Targeting;

namespace Server.Items
{
    public class ClothingBlessTarget : Target // Create our targeting class (which we derive from the base target class)
    {
        private ClothingBlessDeed m_Deed;

        public ClothingBlessTarget(ClothingBlessDeed deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is BaseClothing)
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
                else if (item.LootType != LootType.Regular)
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
                        item.LootType = LootType.Blessed;
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
    }

    public class ClothingBlessDeed : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public ClothingBlessDeed()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "a clothing bless deed";
            LootType = LootType.Blessed;
        }

        public ClothingBlessDeed(Serial serial)
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
            if (Deleted)
                return;

            if (!IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendLocalizedMessage(1005018); // What item would you like to bless? (Clothes Only)
                from.Target = new ClothingBlessTarget(this); // Call our target
            }
        }
    }
}