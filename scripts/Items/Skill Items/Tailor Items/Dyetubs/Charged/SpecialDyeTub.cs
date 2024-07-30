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

/* Scripts/Items/Skill Items/Tailor Items/Misc/Charged/SpecialDyeTub.cs
 * ChangeLog:
 *  9/20/21, Yoar
 *      - Renamed the tones "Reds" -> "Red", "Blues" -> "Blue", etc.
 *      - Disabled metal tones. They'd cause a labeling problem for the "Yellow" tone.
 *  9/19/21, Yoar
 *      - Moved the SpecialDyeTub class from "\SpecialDyeTub.cs" to "\Charged\SpecialDyeTubCharged.cs".
 *      - Now derives from the DyeTubCharged class.
 *      - Rewrote color toning mechanic. Added ITonable interface so that we can make other dye tubs tonable as well.
 *      (*) Below is the changelog from "\SpecialDyeTub.cs"
 *  2/4/07, Adam
 *      Add back old style SpecialDyeTub as SpecialDyeTubClassic
 *  01/04/07, plasma
 *      Added two read only properties that indicate if a tub can be lightened/darkened
 *	10/16/05, erlein
 *		Altered use of "Prepped" to define whether tub has been darkened or lightened already.
 *		Added appropriate deserialization to handle old tubs.
 *	10/15/05, erlein
 *		Added checks to ensure dye tub and targetted clothing is in backpack.
 *		Added stack handling for dying of multiple color swatches.
 *		Added check to ensure only clothing is targetted in dying process.
 *	10/15/05, erlein
 *		Initial creation (complete re-write).
 */

using Server.Targeting;
using System;

namespace Server.Items
{
    public class SpecialDyeTub : DyeTubCharged, ITonable
    {
        public override int LabelNumber { get { return 1041285; } } // Special Dye Tub

        public override bool ReplaceOnEmptied { get { return true; } }

        [Constructable]
        public SpecialDyeTub()
            : this(0, 1)
        {
        }

        [Constructable]
        public SpecialDyeTub(int hue)
            : this(hue, 1)
        {
        }

        [Constructable]
        public SpecialDyeTub(int hue, int uses)
            : base(hue, uses)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            DyeTubToning.Examine(from, this.DyedHue);

            base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 1))
            {
                from.SendLocalizedMessage(500859); // Select the clothing to dye.
                from.BeginTarget(1, false, TargetFlags.None, OnTarget);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        private void OnTarget(Mobile from, object targeted)
        {
            if (targeted is ColorSwatch)
            {
                ColorSwatch cs = (ColorSwatch)targeted;

                if (!from.InRange(GetWorldLocation(), 1) || !from.InRange(cs.GetWorldLocation(), 1))
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                    return;
                }

                if (cs.Amount > 1)
                {
                    cs.Consume();

                    cs = new ColorSwatch();
                    cs.Stackable = false;

                    from.AddToBackpack(cs);
                }
                else
                {
                    cs.Stackable = false;
                }

                cs.StoredColorName = DyeTubToning.FormatDescription(this.DyedHue);
                cs.Hue = this.DyedHue;

                from.PlaySound(0x23E);

                if (LimitedUses)
                    ConsumeUse(from);
            }
            else if (targeted is BaseClothing)
            {
                Item item = (Item)targeted;

                if (!from.InRange(this.GetWorldLocation(), 1) || !from.InRange(item.GetWorldLocation(), 1))
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                }
                if (!item.IsChildOf(from.Backpack))
                {
                    from.SendMessage("The item you are dying must be in your backpack.");
                }
                else if (((IDyable)item).Dye(from, this))
                {
                    from.PlaySound(0x23E);

                    if (LimitedUses)
                        ConsumeUse(from);
                }
            }
            else
            {
                from.SendLocalizedMessage(1042083); // You can not dye that.
            }
        }

        #region ITonable

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanDarken { get { return DyeTubToning.CanDarken(this.DyedHue); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanLighten { get { return DyeTubToning.CanLighten(this.DyedHue); } }

        public bool DarkenMix()
        {
            if (DyeTubToning.CanDarken(this.DyedHue))
            {
                SetDyedHue(DyedHue + 1);
                return true;
            }

            return false;
        }

        public bool LightenMix()
        {
            if (DyeTubToning.CanLighten(this.DyedHue))
            {
                SetDyedHue(DyedHue - 1);
                return true;
            }

            return false;
        }

        #endregion

        public SpecialDyeTub(Serial serial)
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
                case 0:
                    {
                        reader.ReadString(); // m_StoredColorName
                        reader.ReadString(); // m_StoredColorNamePrefix
                        SetDyedHue((int)reader.ReadShort()); // m_StoredColor
                        reader.ReadBool(); // m_Prepped
                        this.UsesRemaining = (int)reader.ReadShort();
                        break;
                    }
            }
        }
    }

    public interface ITonable
    {
        bool CanDarken { get; }
        bool CanLighten { get; }

        bool DarkenMix();
        bool LightenMix();
    }

    public static class DyeTubToning
    {
        public struct ColorInfo
        {
            public static readonly ColorInfo Empty = new ColorInfo();

            private int m_BaseHue;
            private int m_Shades;
            private string m_Label;

            public int BaseHue { get { return m_BaseHue; } }
            public int Shades { get { return m_Shades; } }
            public string Label { get { return m_Label; } }

            public ColorInfo(int baseHue, int shades, string label)
            {
                m_BaseHue = baseHue;
                m_Shades = shades;
                m_Label = label;
            }
        }

        // Special colors are defined by their starting index and the number of colors in the set
        private static ColorInfo[] m_ColorTable = new ColorInfo[]
            {
                // special dye tub colors
                new ColorInfo( 1230, 6, "Violet" ),          // Violet           1230 - 1235 (6)
                new ColorInfo( 1501, 8, "Tan" ),             // Tan              1501 - 1508 (8)
                new ColorInfo( 2013, 5, "Brown" ),           // Brown            2012 - 2017 (5)
                new ColorInfo( 1303, 6, "Dark Blue" ),       // Dark Blue        1303 - 1308 (6)
                new ColorInfo( 1420, 7, "Forest Green" ),    // Forest Green     1420 - 1426 (7)
                new ColorInfo( 1619, 8, "Pink" ),            // Pink             1619 - 1626 (8)
                new ColorInfo( 1640, 5, "Crimson" ),         // Crimson          1640 - 1644 (5) - renamed from "Red" to "Crimson"
                new ColorInfo( 2001, 5, "Olive" ),           // Olive            2001 - 2005 (5)

                // leather dye tub colors
                /* Yoar: I would like to add Red, Blue, Green and Yellow to craftable special dyes.
                 * However, since Gold has the same base hue as Yellow, any special dye tub that is
                 * crafted for the color Yellow, will actually display the "Gold" tag. This is
                 * because Gold appears earlier in this array than Yellow.
                 * Since we're not adding metal dyes to special dye tubs, let's simply disable them.
                 */
#if false
                new ColorInfo( 2419, 6, "Dull Copper" ),     // Dull Copper      2419 - 2424 (6)
                new ColorInfo( 2406, 7, "Shadow Iron" ),     // Shadow Iron      2406 - 2412 (7)
                new ColorInfo( 2413, 6, "Copper" ),          // Copper           2413 - 2418 (6)
                new ColorInfo( 2414, 5, "Bronze" ),          // Bronze           2414 - 2418 (5)
                new ColorInfo( 2213, 6, "Gold" ),            // Gold             2213 - 2218 (6)
                new ColorInfo( 2425, 6, "Agapite" ),         // Agapite          2425 - 2430 (6)
                new ColorInfo( 2207, 6, "Verite" ),          // Verite           2207 - 2212 (6)
                new ColorInfo( 2219, 6, "Valorite" ),        // Valorite         2219 - 2224 (6)
#endif
                new ColorInfo( 2113, 6, "Red" ),             // Red              2113 - 2118 (6)
                new ColorInfo( 2119, 6, "Blue" ),            // Blue             2119 - 2124 (6)
                new ColorInfo( 2126, 5, "Green" ),           // Green            2126 - 2130 (5)
                new ColorInfo( 2213, 6, "Yellow" ),          // Yellow           2213 - 2218 (6)
            };

        public static ColorInfo[] ColorTable { get { return m_ColorTable; } }

        public static ColorInfo GetColor(int hue)
        {
            for (int i = 0; i < m_ColorTable.Length; i++)
            {
                ColorInfo clr = m_ColorTable[i];

                if (hue >= clr.BaseHue && hue < clr.BaseHue + clr.Shades)
                    return clr;
            }

            return ColorInfo.Empty;
        }

        public static string GetPrefix(int hue)
        {
            ColorInfo clr = GetColor(hue);

            int index = hue - clr.BaseHue;

            if (index < 0 || index >= clr.Shades)
                return "a"; // results in "a shade of ..."

            int perc = index * 100 / (clr.Shades - 1);

            if (perc == 100)
                return "the darkest";
            else if (perc >= 75)
                return "a very dark";
            else if (perc >= 50)
                return "a dark";
            else if (perc >= 25)
                return "a light";
            else if (perc > 0)
                return "a very light";
            else
                return "the lightest";
        }

        public static string FormatDescription(int hue)
        {
            ColorInfo clr = GetColor(hue);

            if (clr.Label != null)
                return String.Format("{0} shade of {1}", GetPrefix(hue), clr.Label);

            return null;
        }

        // Say what colour it is
        public static void Examine(Mobile from, int hue)
        {
            string name = FormatDescription(hue);

            if (String.IsNullOrEmpty(name))
                from.SendMessage("This tub has not yet been used to make any dye.");
            else
                from.SendMessage("You examine the tub and note it is {0}.", name);
        }

        public static bool CanDarken(int hue)
        {
            ColorInfo clr = GetColor(hue);

            return (clr.Shades != 0 && hue != clr.BaseHue + clr.Shades - 1);
        }

        public static bool CanLighten(int hue)
        {
            ColorInfo clr = GetColor(hue);

            return (clr.Shades != 0 && hue != clr.BaseHue);
        }
    }
}