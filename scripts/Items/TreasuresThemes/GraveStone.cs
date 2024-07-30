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

/* Scripts/Items/TreasureThemes/GraveStone.cs
 * CHANGELOG
 *  10/4/23, Yoar
 *      Merged all gravestone types into one
 *	04/07/05, Kitaras	
 *		Initial Creation
 */

namespace Server.Items
{
    [TypeAlias("Server.Items.BaseGraveStone")]
    public abstract class BaseGravestone : Item, IStoneEngravable
    {
        private string m_Description;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value;
                InvalidateProperties();
            }
        }

        public BaseGravestone(int itemID)
            : base(itemID)
        {
            Weight = 93.0;
            Name = "";
        }

        public BaseGravestone(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Description);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Description = reader.ReadString();
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Description != null && m_Description.Length > 0)
                LabelTo(from, m_Description);

            base.OnSingleClick(from);
        }

        #region IStoneEngravable

        bool IStoneEngravable.IsEngravable { get { return true; } }

        bool IStoneEngravable.OnEngrave(Mobile from, string text)
        {
            from.SendMessage("You engrave the gravestone.");

            Name = text;

            return true;
        }

        #endregion
    }

    [Flipable]
    [TypeAlias(
        "Server.Items.GraveStone1",
        "Server.Items.GraveStone2",
        "Server.Items.GraveStone3",
        "Server.Items.GraveStone4")]
    public class Gravestone : BaseGravestone
    {
        [Constructable]
        public Gravestone()
            : this(Utility.RandomMinMax(0x1165, 0x1182))
        {
        }

        [Constructable]
        public Gravestone(int itemID)
            : base(itemID)
        {
        }

        public Gravestone(Serial serial)
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

        public void Flip()
        {
            if (ItemID >= 0x1165 && ItemID <= 0x1182)
            {
                if ((ItemID % 2) == 0)
                    ItemID--;
                else
                    ItemID++;
            }
        }
    }
}