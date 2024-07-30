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

/* Scripts\Items\Skill Items\Tailor Items\Dyetubs\Charged\LeatherArmorDyeTub.cs
 * CHANGELOG:
 *  9/19/21, Yoar
 *      Now derives from the DyeTubCharged class.
 *      Can no longer dye leather armor that is locked down in your house.
 *  12/20/06, Adam
 *      Unfortunately certain 'bone' armor is using leather as the resource;
 *        therefore we make an explicit type check.
 *	6/14/06, Adam
 *		Add the color to the 'name'
 *	6/9/06, Adam
 *		Initial Version
 */

using Server.Targeting;
using System;

namespace Server.Items
{
    public class LeatherArmorDyeTub : DyeTubCharged
    {
        public override string DefaultName
        {
            get
            {
                switch (DyedHue)
                {
                    case 0x8AB: return "leather armor dye tub (Valorite)";
                    case 0x89F: return "leather armor dye tub (Verite)";
                    case 0x979: return "leather armor dye tub (Agapite)";
                    case 0x8A5: return "leather armor dye tub (Gold)";
                    case 0x972: return "leather armor dye tub (Bronze)";
                    case 0x96D: return "leather armor dye tub (Copper)";
                    case 0x966: return "leather armor dye tub (Shadow Iron)";
                    case 0x973: return "leather armor dye tub (Dull Copper)";
                    default: return "leather armor dye tub";
                }
            }
        }

        // different resource metal types and weight table
        private static readonly int[] m_Table = new int[]
            {
                0x8AB,												// Valorite,	1 in 36 chance
				0x89F,0x89F,										// Verite,		2 in 36 chance
				0x979,0x979,0x979,									// Agapite,		3 in 36 chance
				0x8A5,0x8A5,0x8A5,0x8A5,							// Gold,		4 in 36 chance
				0x972,0x972,0x972,0x972,0x972,						// Bronze,		5 in 36 chance
				0x96D,0x96D,0x96D,0x96D,0x96D,0x96D,				// Copper,		6 in 36 chance
				0x966,0x966,0x966,0x966,0x966,0x966,0x966,			// ShadowIron,	7 in 36 chance
				0x973,0x973,0x973,0x973,0x973,0x973,0x973,0x973,	// DullCopper,	8 in 36 chance
			};

        [Constructable]
        public LeatherArmorDyeTub()
            : this(m_Table[Utility.Random(m_Table.Length)], 100)
        {
        }

        [Constructable]
        public LeatherArmorDyeTub(int hue)
            : this(hue, 100)
        {
        }

        [Constructable]
        public LeatherArmorDyeTub(int hue, int uses)
            : base(hue, uses)
        {
        }

        public LeatherArmorDyeTub(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2: break; // version 2 derives from ChargedDyeTub
                case 1:
                    {
                        this.UsesRemaining = reader.ReadInt();
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.SendMessage("Target the leather armor to dye.");
                from.BeginTarget(1, false, TargetFlags.None, OnTarget);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        private static bool IsLeatherArmor(BaseArmor ba)
        {
            if (ba == null)
                return false;

            if (Array.IndexOf(m_LeatherArmorTypes, ba.GetType()) == -1)
                return false;

            CraftResource res = ba.Resource;

            return res == CraftResource.RegularLeather ||
                res == CraftResource.SpinedLeather ||
                res == CraftResource.HornedLeather ||
                res == CraftResource.BarbedLeather;
        }

        private static readonly Type[] m_LeatherArmorTypes = new Type[]
            {
                typeof( LeatherCap ),
                typeof( FemaleLeatherChest ),   typeof( FemaleStuddedChest ),
                typeof( LeatherArms ),          typeof( StuddedArms ),
                typeof( LeatherBustierArms ),   typeof( StuddedBustierArms ),
                typeof( LeatherChest ),         typeof( StuddedChest ),
                typeof( LeatherGloves ),        typeof( StuddedGloves ),
                typeof( LeatherGorget ),        typeof( StuddedGorget ),
                typeof( LeatherLegs ),          typeof( StuddedLegs ),
                typeof( LeatherShorts ),
                typeof( LeatherSkirt ),
            };

        private void OnTarget(Mobile from, object targeted)
        {
            BaseArmor ba = targeted as BaseArmor;

            if (IsLeatherArmor(ba))
            {
                if (!ba.IsChildOf(from.Backpack))
                {
                    from.SendMessage("The item must be in your backpack to be dyed.");
                }
                else
                {
                    ba.Hue = this.DyedHue;

                    from.PlaySound(0x23E);

                    if (LimitedUses)
                        ConsumeUse(from);
                }
            }
            else
            {
                from.SendMessage("That is not leather armor.");
            }
        }
    }
}