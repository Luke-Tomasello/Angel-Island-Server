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

/* Scripts/Items/Addons/HouseLadder.cs
 * ChangeLog
 *	8/9/23, Yoar
 *		Moved from Scripts/Items/Addons/HouseLadder into its own source file
 */

using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    public abstract class BaseChoiceAddonDeed : BaseAddonDeed
    {
        public virtual TextEntry ChoiceText { get { return "Select your choice from the menu below."; } }
        public abstract TextEntry[] Choices { get; }

        protected int m_Type;

        [Constructable]
        public BaseChoiceAddonDeed()
            : base()
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            }
            else
            {
                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump(this));
            }
        }

        private void SendTarget(Mobile m)
        {
            base.OnDoubleClick(m);
        }

        public BaseChoiceAddonDeed(Serial serial)
            : base(serial)
        {
        }
        private const byte mask = 0x80;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x80); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = Utility.PeekByte(reader);

            if ((version & mask) == 0)
            {
                if (World.LoadingType == "Server.Items.HouseLadderDeed" || World.LoadingType == "Server.Items.FortLadderDeed")
                    reader.ReadByte();

                return; // old version
            }

            reader.ReadByte(); // consume version
        }

        private class InternalGump : Gump
        {
            private BaseChoiceAddonDeed m_Deed;

            public InternalGump(BaseChoiceAddonDeed deed)
                : base(60, 36)
            {
                m_Deed = deed;

                AddPage(0);

                AddBackground(0, 0, 273, 324, 0x13BE);
                AddImageTiled(10, 10, 253, 20, 0xA40);
                AddImageTiled(10, 40, 253, 244, 0xA40);
                AddImageTiled(10, 294, 253, 20, 0xA40);
                AddAlphaRegion(10, 10, 253, 304);
                AddButton(10, 294, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(45, 296, 60, 20, 1060051, 0x7FFF, false, false); // CANCEL
                TextEntry.AddHtmlText(this, 14, 12, 273, 20, m_Deed.ChoiceText, false, false, 0x7FFF, 0xFFFFFF);

                AddPage(1);

                int page = 1;

                for (int i = 0; i < m_Deed.Choices.Length; i++)
                {
                    if (i != 0 && (i % 10) == 0)
                    {
                        AddButton(190, 294, 0xFA5, 0xFA6, 0, GumpButtonType.Page, page + 1);
                        AddHtmlLocalized(225, 294, 50, 20, 1044045, 0x7FFF, false, false); // NEXT PAGE

                        AddPage(++page);

                        AddButton(105, 294, 0xFAE, 0xFAF, 0, GumpButtonType.Page, page - 1);
                        AddHtmlLocalized(140, 294, 50, 20, 1044044, 0x7FFF, false, false); // PREV PAGE
                    }

                    AddButton(19, 49 + 24 * (i % 10), 0x845, 0x846, i + 1, GumpButtonType.Reply, 0);
                    TextEntry.AddHtmlText(this, 44, 47 + 24 * (i % 10), 213, 20, m_Deed.Choices[i], false, false, 0x7FFF, 0xFFFFFF);
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (m_Deed == null || m_Deed.Deleted || info.ButtonID == 0)
                    return;

                if (info.ButtonID >= 1 && info.ButtonID <= m_Deed.Choices.Length)
                {
                    m_Deed.m_Type = info.ButtonID - 1;
                    m_Deed.SendTarget(sender.Mobile);
                }
            }
        }
    }
}