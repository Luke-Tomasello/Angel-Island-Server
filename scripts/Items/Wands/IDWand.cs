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

/* Scripts/Items/Wands/IDWand.cs
 * CHANGE LOG
 *	04/14/07, weaver
 *		Centralised identifaction routine into ItemIdentification.cs.
 *	11/04/05, weaver
 *		Added check and report on whether item identified is player crafted.
 *	07/13/05, weaver
 *		Added EnchantedScroll for SDrop system.
 *  07/06/04, Pix
 *		Changed charges to 30-50
 *  06/05/04, Pix
 *		Merged in 1.0RC0 code.
 *	05/11/04, Pulse
 *		Added "is BaseJewel" and "is BaseClothing" conditions to the OnWandTarget method to
 *		implement identifying of magic jewelry and clothing
 */
using System;

namespace Server.Items
{
    [Obsolete("Use Wand class instead")]
    public class IDWand : BaseWand
    {
        [Constructable]
        public IDWand()
            : base(MagicItemEffect.Identification, 30, 50)
        {
        }

        public IDWand(Serial serial)
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