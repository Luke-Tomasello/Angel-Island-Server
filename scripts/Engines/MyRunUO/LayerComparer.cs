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

/* Scripts/Engines/MyRunUO/LayerComparer.cs
 * Changelog:
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using System.Collections;

namespace Server.Engines.MyRunUO
{
    public class LayerComparer : IComparer
    {
        private static Layer PlateArms = (Layer)255;
        private static Layer ChainTunic = (Layer)254;
        private static Layer LeatherShorts = (Layer)253;

        private static Layer[] m_DesiredLayerOrder = new Layer[]
        {
            Layer.Cloak,
            Layer.Bracelet,
            Layer.Ring,
            Layer.Shirt,
            Layer.Pants,
            Layer.InnerLegs,
            Layer.Shoes,
            LeatherShorts,
            Layer.Arms,
            Layer.InnerTorso,
            LeatherShorts,
            PlateArms,
            Layer.MiddleTorso,
            Layer.OuterLegs,
            Layer.Neck,
            Layer.Waist,
            Layer.Gloves,
            Layer.OuterTorso,
            Layer.OneHanded,
            Layer.TwoHanded,
            Layer.FacialHair,
            Layer.Hair,
            Layer.Helm
        };

        private static int[] m_TranslationTable;

        public static int[] TranslationTable
        {
            get { return m_TranslationTable; }
        }

        static LayerComparer()
        {
            m_TranslationTable = new int[256];

            for (int i = 0; i < m_DesiredLayerOrder.Length; ++i)
                m_TranslationTable[(int)m_DesiredLayerOrder[i]] = m_DesiredLayerOrder.Length - i;
        }

        public static bool IsValid(Item item)
        {
            return (m_TranslationTable[(int)item.Layer] > 0);
        }

        public static readonly IComparer Instance = new LayerComparer();

        public LayerComparer()
        {
        }

        public Layer Fix(int itemID, Layer oldLayer)
        {
            if (itemID == 0x1410 || itemID == 0x1417) // platemail arms
                return PlateArms;

            if (itemID == 0x13BF || itemID == 0x13C4) // chainmail tunic
                return ChainTunic;

            if (itemID == 0x1C08 || itemID == 0x1C09) // leather skirt
                return LeatherShorts;

            if (itemID == 0x1C00 || itemID == 0x1C01) // leather shorts
                return LeatherShorts;

            return oldLayer;
        }

        public int Compare(object x, object y)
        {
            Item a = (Item)x;
            Item b = (Item)y;

            Layer aLayer = a.Layer;
            Layer bLayer = b.Layer;

            aLayer = Fix(a.ItemID, aLayer);
            bLayer = Fix(b.ItemID, bLayer);

            return m_TranslationTable[(int)bLayer] - m_TranslationTable[(int)aLayer];
        }
    }
}