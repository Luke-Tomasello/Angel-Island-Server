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

/* Scripts/Items/Special/Holiday/Halloween/BurialGown.cs
 * CHANGELOG
 *  10/5/23, Yoar
 *      Initial commit.
 */

namespace Server.Items
{
    public class BurialGown : PlainDress, IScissorable
    {
        public override string DefaultName { get { return "a burial gown"; } }

        [Constructable]
        public BurialGown()
            : this(Utility.RandomList(1102, 1109, 1110))
        {
        }

        [Constructable]
        public BurialGown(int hue)
            : base(hue)
        {
        }

        public BurialGown(Serial serial)
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

        // Not craftable, but can be cut up
        public new bool Scissor(Mobile from, Scissors scissors)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack.
            }
            else if (Ethics.Ethic.IsImbued(this))
            {
                from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            }
            else
            {
                base.ScissorHelper(scissors, from, new Cloth(), 1);
                return true;
            }

            return false;
        }
    }
}