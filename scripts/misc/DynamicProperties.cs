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

/* Scripts/Misc/DynamicProperties.cs
 * ChangeLog:
 *	06/18/07 Taran Kain
 *		Added some Uses, fixed FindUse to support derived Properties
 *  1/26/07 Adam
 *      - Initial Creation
 *      - new dynamic property system
 */

using Server.Commands;
using Server.Diagnostics;
using System;
using System.Collections;
using System.Reflection;
namespace Server.Items
{
    public enum Use
    {
        None,
        Quip,               // Spoken when someone tried to 'peace' them
        IsGuardian,         // I am a Guardian (like pirate chest guardians)
        SABonus,
        BPBonus,
    }

    public class Property : Item
    {
        public string Text
        {
            get { return Name; }
            set { Name = value; }
        }

        public Use Use
        {
            get { return (Use)Hue; }
            set { Hue = (int)value; }
        }

        [Constructable]
        public Property(Use type, string text)
            : base(0x1F14)
        {
            Weight = 0.0;                   // properties have no weight
            LootType = LootType.Newbied;    // not dropped
            Visible = false;                // not seen
            if (text != null) Name = text;
            if (type != Use.None) Hue = (int)type;
        }

        public static bool FindUse(Mobile m, Use use)
        {
            if (m == null || m.Deleted == true)
                return false;

            foreach (Item ix in m.Items)
                if (ix is Property)
                    if ((ix as Property).Use == use)
                        return true;

            return false;
        }

        public Property(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class TimedProperty : Property
    {
        public TimedProperty(Use type, string text, TimeSpan duration)
            : base(type, text)
        {
            Timer.DelayCall(duration, new TimerCallback(Delete));
        }

        public TimedProperty(Serial s)
            : base(s)
        {
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

    public class TimedSet : Property
    {
        private Item m_item;
        private DateTime m_SetTime;
        private ArrayList m_lines;
        private Timer m_Timer;

        public TimedSet(Item item, DateTime SetTime, string[] lines)
            : base(Use.None, null)
        {
            m_item = item;
            m_SetTime = SetTime;
            m_lines = new ArrayList(lines);

            m_Timer = new SetTimer(this, item, m_lines, m_SetTime);
            m_Timer.Start();
        }

        public TimedSet(Serial serial)
            : base(serial)
        {
        }

        private class SetTimer : Timer
        {
            TimedSet m_Owner;          // owns this timer
            private Item m_Item;        // item we will set values on
            private ArrayList m_lines;  // properties and values

            public SetTimer(TimedSet owner, Item item, ArrayList lines, DateTime time)
                : base(time - DateTime.UtcNow)
            {
                m_Owner = owner;
                m_Item = item;
                m_lines = lines;
                Priority = TimerPriority.OneMinute;
            }

            // after this fires, the array of properties and their values is cleared.
            // This clearing of the properties array prevents the strings from being saved, and prevents the timer from restarting
            protected override void OnTick()
            {
                try
                {
                    if (m_Item != null && m_Item.Deleted == false)
                    {
                        // Reflect type
                        PropertyInfo[] allProps = m_Item.GetType().GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
                        foreach (string ix in m_lines)
                        {
                            if (ix is string == false)
                                continue;

                            string lSide = ix;

                            int ndx = lSide.IndexOf(' ');
                            if (ndx == -1)
                                continue;

                            string rSide = lSide;

                            lSide = lSide.Substring(0, ndx).Trim();     // carve out the property name
                            rSide = rSide.Substring(ndx).Trim();        // carve out the property value

                            foreach (PropertyInfo prop in allProps)
                            {
                                if (prop is PropertyInfo)
                                    if (prop.Name.ToLower() == lSide.ToLower())
                                    {
                                        object toSet = null;
                                        Properties.ConstructFromString(prop.PropertyType, m_Item, rSide, ref toSet);
                                        prop.SetValue(m_Item, toSet, null);
                                    }
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("Caught non-fatal exception in class TimedSet : Property: " + exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
                finally
                {   // final cleanup.
                    m_lines.Clear();
                    m_Item = null;
                    this.Stop();
                    m_Owner.Delete();
                }
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(m_item);
            writer.WriteDeltaTime(m_SetTime);
            writer.Write((short)m_lines.Count);
            for (int ix = 0; ix < m_lines.Count; ++ix)
                writer.Write((string)m_lines[ix]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_item = reader.ReadItem();
            m_SetTime = reader.ReadDeltaTime();

            short count = reader.ReadShort();
            m_lines = new ArrayList(count);
            for (short ix = 0; ix < count; ++ix)
                m_lines.Add(reader.ReadString());

            if (count > 0)
            {
                m_Timer = new SetTimer(this, m_item, m_lines, m_SetTime);
                m_Timer.Start();
            }
        }
    }

    public abstract class OnBeforeDeath : Property
    {
        public OnBeforeDeath(Use use, string text)
            : base(use, text)
        {
        }

        public OnBeforeDeath(Serial serial)
            : base(serial)
        {
        }

        public abstract void Process(Mobile m);

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

    public abstract class OnPeace : Property
    {
        public OnPeace(Use use, string text)
            : base(use, text)
        {
        }

        public OnPeace(Serial serial)
            : base(serial)
        {
        }

        public abstract void Say(Mobile m, bool random);

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

    public class Quip : OnPeace
    {
        [Constructable]
        public Quip(string text)
            : base(Use.Quip, text)
        {
        }

        public Quip(Serial serial)
            : base(serial)
        {
        }

        public override void Say(Mobile m, bool random)
        {
            if (m == null || m.Deleted == true)
                return;

            ArrayList temp = new ArrayList();
            foreach (Item ix in m.Items)
                if (ix is OnPeace)
                    if ((ix as OnPeace).Use == Use.Quip)
                        temp.Add(ix);

            if (temp.Count == 0)
                return;

            string sx = (temp[Utility.Random(temp.Count)] as Quip).Name;
            if (sx != null && sx.Length > 0)
                m.Say(sx);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}