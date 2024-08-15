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

/* Items/Skill Items/Lumberjack/Log.cs
 * CHANGELOG:
 *	11/26/21, Yoar
 *	    Added FlipableAttribute to derived log types.
 *	11/20/21, Yoar
 *	    Adjusted chopping skill requirements.
 *	11/17/21, Yoar
 *	    Added OnSingleClick overrides to display the proper wood names.
 *	11/14/21, Yoar
 *	    - Added BaseLog base class for wooden logs.
 *	    - Added logs for the ML wood types.
 */

using System;

namespace Server.Items
{
    public abstract class BaseLog : Item, IAxe
    {
        public static bool NewWoodTypes { get { return (Core.RuleSets.AngelIslandRules() || Core.SiegeII_CFG); } }

        private CraftResource m_Resource;

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; InvalidateProperties(); }
        }

        public BaseLog()
            : this(1)
        {
        }

        public BaseLog(int amount)
            : this(CraftResource.RegularWood, amount)
        {
        }

        public BaseLog(CraftResource resource)
            : this(resource, 1)
        {
        }

        public BaseLog(CraftResource resource, int amount)
            : base(0x1BDD)
        {
            Stackable = true;
            Weight = 2.0;
            Amount = amount;

            m_Resource = resource;
            Hue = CraftResources.GetHue(resource);
        }

        public BaseLog(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

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

        public virtual bool TryCreateBoards(Mobile from, double skill, Item item)
        {
            if (Deleted || !from.CanSee(this))
                return false;

            if (from.Skills.Carpentry.Value < skill && from.Skills.Lumberjacking.Value < skill)
            {
                item.Delete();
                from.SendLocalizedMessage(1072652); // You cannot work this strange and unusual wood.
                return false;
            }

            base.ScissorHelper(item as Scissors, from, item, 1, false);
            return true;
        }

        public abstract bool Axe(Mobile from, BaseAxe axe);
    }

    [FlipableAttribute(0x1bdd, 0x1be0)]
    public class Log : BaseLog, ICommodity
    {
        string ICommodity.Description
        {
            get { return string.Format(Amount == 1 ? "{0} log" : "{0} logs", Amount); }
        }

        [Constructable]
        public Log()
            : this(1)
        {
        }

        [Constructable]
        public Log(int amount)
            : base(CraftResource.RegularWood, amount)
        {
        }

        public Log(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Log(amount), amount);
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 0.0, new Board()))
                return false;

            return true;
        }
    }

    [FlipableAttribute(0x1bdd, 0x1be0)]
    public class HeartwoodLog : BaseLog, ICommodity
    {
        string ICommodity.Description
        {
            get { return string.Format(Amount == 1 ? "{0} heartwood log" : "{0} heartwood logs", Amount); }
        }

        [Constructable]
        public HeartwoodLog()
            : this(1)
        {
        }

        [Constructable]
        public HeartwoodLog(int amount)
            : base(CraftResource.Heartwood, amount)
        {
        }

        public HeartwoodLog(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "a heartwood log" : string.Format("{0} heartwood logs", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new HeartwoodLog(amount), amount);
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 90.0, new HeartwoodBoard()))
                return false;

            return true;
        }
    }

    [FlipableAttribute(0x1bdd, 0x1be0)]
    public class BloodwoodLog : BaseLog, ICommodity
    {
        string ICommodity.Description
        {
            get { return string.Format(Amount == 1 ? "{0} bloodwood log" : "{0} bloodwood logs", Amount); }
        }

        [Constructable]
        public BloodwoodLog()
            : this(1)
        {
        }

        [Constructable]
        public BloodwoodLog(int amount)
            : base(CraftResource.Bloodwood, amount)
        {
        }

        public BloodwoodLog(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "a bloodwood log" : string.Format("{0} bloodwood logs", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new BloodwoodLog(amount), amount);
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 95.0, new BloodwoodBoard()))
                return false;

            return true;
        }
    }

    [FlipableAttribute(0x1bdd, 0x1be0)]
    public class FrostwoodLog : BaseLog, ICommodity
    {
        string ICommodity.Description
        {
            get { return string.Format(Amount == 1 ? "{0} frostwood log" : "{0} frostwood logs", Amount); }
        }

        [Constructable]
        public FrostwoodLog()
            : this(1)
        {
        }

        [Constructable]
        public FrostwoodLog(int amount)
            : base(CraftResource.Frostwood, amount)
        {
        }

        public FrostwoodLog(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "a frostwood log" : string.Format("{0} frostwood logs", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new FrostwoodLog(amount), amount);
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 99.0, new FrostwoodBoard()))
                return false;

            return true;
        }
    }

    [FlipableAttribute(0x1bdd, 0x1be0)]
    public class OakLog : BaseLog, ICommodity
    {
        string ICommodity.Description
        {
            get { return string.Format(Amount == 1 ? "{0} oak log" : "{0} oak logs", Amount); }
        }

        [Constructable]
        public OakLog()
            : this(1)
        {
        }

        [Constructable]
        public OakLog(int amount)
            : base(CraftResource.OakWood, amount)
        {
        }

        public OakLog(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "an oak log" : string.Format("{0} oak logs", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new OakLog(amount), amount);
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 65.0, new OakBoard()))
                return false;

            return true;
        }
    }

    [FlipableAttribute(0x1bdd, 0x1be0)]
    public class AshLog : BaseLog, ICommodity
    {
        string ICommodity.Description
        {
            get { return string.Format(Amount == 1 ? "{0} ash log" : "{0} ash logs", Amount); }
        }

        [Constructable]
        public AshLog()
            : this(1)
        {
        }

        [Constructable]
        public AshLog(int amount)
            : base(CraftResource.AshWood, amount)
        {
        }

        public AshLog(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "an ash log" : string.Format("{0} ash logs", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new AshLog(amount), amount);
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 75.0, new AshBoard()))
                return false;

            return true;
        }
    }

    [FlipableAttribute(0x1bdd, 0x1be0)]
    public class YewLog : BaseLog, ICommodity
    {
        string ICommodity.Description
        {
            get { return string.Format(Amount == 1 ? "{0} yew log" : "{0} yew logs", Amount); }
        }

        [Constructable]
        public YewLog()
            : this(1)
        {
        }

        [Constructable]
        public YewLog(int amount)
            : base(CraftResource.YewWood, amount)
        {
        }

        public YewLog(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Amount == 1 ? "a yew log" : string.Format("{0} yew logs", Amount));
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new YewLog(amount), amount);
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

        public override bool Axe(Mobile from, BaseAxe axe)
        {
            if (!TryCreateBoards(from, 80.0, new YewBoard()))
                return false;

            return true;
        }
    }
}