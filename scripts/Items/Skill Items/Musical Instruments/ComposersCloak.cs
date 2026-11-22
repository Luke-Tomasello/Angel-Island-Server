/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts/Items/Skill Items/Musical Instruments/ComposersCloak.cs
 * ChangeLog
 *  11/17/26, Adam (-uomc composer start)
 *      Created. A cloak a new composer starts with on the Music Conservatory. When put on, it takes its wearer's
 *      composer title as its name ("Neophyte Composer" .. "Legendary Composer", Titles.ComposerTitle); a wearer who
 *      isn't a composer (not in the bards' guild) leaves the name as it was. Worn while the title changes level, it
 *      renames itself then too (BaseClothing.OnWearerTitleChanged).
 */

using Server.Misc;

namespace Server.Items
{
    [Flipable(0x1515, 0x1530)]
    public class ComposersCloak : BaseCloak
    {
        [Constructable]
        public ComposersCloak()
            : this(0)
        {
        }

        [Constructable]
        public ComposersCloak(int hue)
            : base(0x1515, hue)
        {
            Name = "a composer's cloak";
            Weight = 5.0;
        }

        public ComposersCloak(Serial serial)
            : base(serial)
        {
        }

        public override bool OnEquip(Mobile from)
        {
            if (!base.OnEquip(from))
                return false;

            TakeTitle(from);
            return true;
        }

        // the wearer's composer title changed level while it was worn (PlayerMobile.NotifyTitleChange)
        public override void OnWearerTitleChanged(Mobile wearer)
        {
            TakeTitle(wearer);
        }

        private void TakeTitle(Mobile m)
        {
            string? title = Titles.ComposerTitle(m);
            if (title != null && title != Name)
                Name = title;
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
