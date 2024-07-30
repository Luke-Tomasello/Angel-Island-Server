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

/* Items/Skill Items/Carpenter Items/Board.cs
 * CHANGELOG:
 *	11/26/21, Yoar
 *	    Added FlipableAttribute to derived board types.
 *	11/17/21, Yoar
 *	    Added OnSingleClick overrides to display the proper wood names.
 *	11/14/21, Yoar
 *	    - Added BaseBoard base class for wooden boards.
 *	    - Added boards for the ML wood types.
 */

using System;

namespace Server.Items
{
    public abstract class BaseBoard : Item
    {
        private CraftResource m_Resource;

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; InvalidateProperties(); }
        }

        public BaseBoard()
            : this(1)
        {
        }

        public BaseBoard(int amount)
            : this(CraftResource.RegularWood, amount)
        {
        }

        public BaseBoard(CraftResource resource)
            : this(resource, 1)
        {
        }

        public BaseBoard(CraftResource resource, int amount)
            : base(0x1BD7)
        {
            Stackable = true;
            Weight = 0.1;
            Amount = amount;

            m_Resource = resource;
            Hue = CraftResources.GetHue(resource);
        }

        public BaseBoard(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            writer.Write((int)m_Resource);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = PeekInt(reader);

            if (version == 0)
            {
                m_Resource = CraftResource.RegularWood;

                return; // old version, class insertion
            }

            reader.ReadInt(); // consume version

            switch (version)
            {
                case 1:
                    {
                        m_Resource = (CraftResource)reader.ReadInt();
                        break;
                    }
            }

            BaseCraftableItem.PatchResourceHue(this, m_Resource);
        }

        private static int PeekInt(GenericReader reader)
        {
            int result = reader.ReadInt();
            reader.Seek(-4, System.IO.SeekOrigin.Current);
            return result;
        }
    }

    [FlipableAttribute(0x1BD7, 0x1BDA)]
    public class Board : BaseBoard, ICommodity
    {
        string ICommodity.Description
        {
            get { return String.Format(Amount == 1 ? "{0} board" : "{0} boards", Amount); }
        }

        [Constructable]
        public Board()
            : this(1)
        {
        }

        [Constructable]
        public Board(int amount)
            : base(CraftResource.RegularWood, amount)
        {
        }

        public Board(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Board(amount), amount);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [FlipableAttribute(0x1BD7, 0x1BDA)]
    public class HeartwoodBoard : BaseBoard, ICommodity
    {
        string ICommodity.Description
        {
            get { return String.Format(Amount == 1 ? "{0} heartwood board" : "{0} heartwood boards", Amount); }
        }

        [Constructable]
        public HeartwoodBoard()
            : this(1)
        {
        }

        [Constructable]
        public HeartwoodBoard(int amount)
            : base(CraftResource.Heartwood, amount)
        {
        }

        public HeartwoodBoard(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "a heartwood board" : String.Format("{0} heartwood boards", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new HeartwoodBoard(amount), amount);
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

    [FlipableAttribute(0x1BD7, 0x1BDA)]
    public class BloodwoodBoard : BaseBoard, ICommodity
    {
        string ICommodity.Description
        {
            get { return String.Format(Amount == 1 ? "{0} bloodwood board" : "{0} bloodwood boards", Amount); }
        }

        [Constructable]
        public BloodwoodBoard()
            : this(1)
        {
        }

        [Constructable]
        public BloodwoodBoard(int amount)
            : base(CraftResource.Bloodwood, amount)
        {
        }

        public BloodwoodBoard(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "a bloodwood board" : String.Format("{0} bloodwood boards", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new BloodwoodBoard(amount), amount);
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

    [FlipableAttribute(0x1BD7, 0x1BDA)]
    public class FrostwoodBoard : BaseBoard, ICommodity
    {
        string ICommodity.Description
        {
            get { return String.Format(Amount == 1 ? "{0} frostwood board" : "{0} frostwood boards", Amount); }
        }

        [Constructable]
        public FrostwoodBoard()
            : this(1)
        {
        }

        [Constructable]
        public FrostwoodBoard(int amount)
            : base(CraftResource.Frostwood, amount)
        {
        }

        public FrostwoodBoard(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "a frostwood board" : String.Format("{0} frostwood boards", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new FrostwoodBoard(amount), amount);
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

    [FlipableAttribute(0x1BD7, 0x1BDA)]
    public class OakBoard : BaseBoard, ICommodity
    {
        string ICommodity.Description
        {
            get { return String.Format(Amount == 1 ? "{0} oak board" : "{0} oak boards", Amount); }
        }

        [Constructable]
        public OakBoard()
            : this(1)
        {
        }

        [Constructable]
        public OakBoard(int amount)
            : base(CraftResource.OakWood, amount)
        {
        }

        public OakBoard(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "an oak board" : String.Format("{0} oak boards", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new OakBoard(amount), amount);
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

    [FlipableAttribute(0x1BD7, 0x1BDA)]
    public class AshBoard : BaseBoard, ICommodity
    {
        string ICommodity.Description
        {
            get { return String.Format(Amount == 1 ? "{0} ash board" : "{0} ash boards", Amount); }
        }

        [Constructable]
        public AshBoard()
            : this(1)
        {
        }

        [Constructable]
        public AshBoard(int amount)
            : base(CraftResource.AshWood, amount)
        {
        }

        public AshBoard(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "an ash board" : String.Format("{0} ash boards", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new AshBoard(amount), amount);
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

    [FlipableAttribute(0x1BD7, 0x1BDA)]
    public class YewBoard : BaseBoard, ICommodity
    {
        string ICommodity.Description
        {
            get { return String.Format(Amount == 1 ? "{0} yew board" : "{0} yew boards", Amount); }
        }

        [Constructable]
        public YewBoard()
            : this(1)
        {
        }

        [Constructable]
        public YewBoard(int amount)
            : base(CraftResource.YewWood, amount)
        {
        }

        public YewBoard(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "a yew board" : String.Format("{0} yew boards", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new YewBoard(amount), amount);
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