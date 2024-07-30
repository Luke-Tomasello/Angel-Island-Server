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

/* Scripts/Engines/Music/MusicCompositionBook.cs
 * Changelog
*  1/31/22, Adam
*		Display the Name property instead of the page count.
 *  1/12/22, Adam
 *		Initial creation.
 */

using Server.Items;

namespace Server.Misc
{
    public class MusicCompositionBook : TanBook
    {
        [Constructable]
        public MusicCompositionBook()
            : this(100, true)
        {
        }

        [Constructable]
        public MusicCompositionBook(int pageCount, bool writable)
            : base(pageCount, writable)
        {
            Name = "music composition book";
        }
#if false
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.CheckAlive())
            {
                list.Add(new AddSheetMusicEntry(from, this));
            }

            Multis.SetSecureLevelEntry.AddTo(from, this, list);
        }
        private class AddSheetMusicEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private MusicCompositionBook m_musicBox;

            public AddSheetMusicEntry(Mobile from, MusicCompositionBook musicbox)
                : base(0175/*add*/)
            {
                m_From = from;
                m_musicBox = musicbox;
            }

            public override void OnClick()
            {
                if (m_From.CheckAlive())
                {
                    m_From.SendMessage("Target the music you would like to add...");
                    m_From.Target = new AddMusicTarget(m_musicBox); // Call our target
                }
            }
        }
        public class AddMusicTarget : Target // Create our targeting class (which we derive from the base target class)
        {
            MusicCompositionBook m_musicBox;
            public AddMusicTarget(MusicCompositionBook musicbox)
                : base(5, true, TargetFlags.None)
            {
                m_musicBox = musicbox;
            }
            protected override void OnTarget(Mobile from, object target)
            {   // buyer added for clarity only
                Mobile buyer = from;
                if (target is RolledUpSheetMusic rsm)
                {
                    if (rsm.Deleted == false)
                    {
                       
                    }
                }
                else
                {
                    from.SendMessage("That is not rolled up sheet music.");
                    return;
                }
            }
        }
#endif
        public MusicCompositionBook(Serial serial)
            : base(serial)
        {
        }
        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, "{0}", Name);
            LabelTo(from, "{0} by {1}", Title, Author);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }
    }
}