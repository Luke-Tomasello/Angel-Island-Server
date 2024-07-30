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

/* Scripts/Engines/RareFactory/DODInstance.cs
 * ChangeLog:
 *  6/8/07, Adam
 *      Add instance versioning.
 *  6/7/07, Adam
 *      Rewrite I/O to correct a logic error that was corrupting the database.
 *          You MUST pass prefixstring=true if you are writing strings with BinaryFileWriter
 *  5/29/07, Adam
 *      - Add exception logging
 *      -  fix exception in DynamicFill() where we were not checking for a null properety value. 
 *          (we were checking to see if it was a string type, but not a null before calling ToString() on it.
 *	13/Mar/2007, weaver
 *		- Modified to stamp RareData.
 *		- Added Expire() and call from .Valid bool accessor.
 *		- Added rare expiration logging
 *	28/Feb/2007, weaver
 *		Initial creation.
 * 
 */

using Server.Diagnostics;
using System;
using System.Reflection;


namespace Server.Engines
{

    // DODInstance (Dynamic Object Definition Instance)
    // 
    // Holds all the meta information for the object 
    // definition (properties and values for the items)
    //

    public class DODInstance
    {

        // Public accessors
        public bool Valid
        {
            get
            {
                if (CurIndex == LastIndex || DateTime.UtcNow > EndDate)
                {
                    Expire();
                    return false;
                }

                if (DateTime.UtcNow < StartDate)
                    return false;

                return true;
            }
        }

        // Name of the rare
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }

        // First index of the rare that'll get dropped
        // eg. 1 (of 10)
        public short StartIndex
        {
            get
            {
                return m_StartIndex;
            }
            set
            {
                m_StartIndex = value;
            }
        }

        // Last index of rare that gets dropped
        // eg. 10 (of 10)

        public short LastIndex
        {
            get
            {
                return m_LastIndex;
            }
            set
            {
                m_LastIndex = value;
            }
        }

        // Current index of DOD rare 

        public short CurIndex
        {
            get
            {
                return m_CurIndex;
            }
            set
            {
                m_CurIndex = value;
            }
        }

        // Start date of DOD rare 

        public DateTime StartDate
        {
            get
            {
                return m_StartDate;
            }
            set
            {
                m_StartDate = value;
            }
        }

        // End date of DOD rare 

        public DateTime EndDate
        {
            get
            {
                return m_EndDate;
            }
            set
            {
                m_EndDate = value;
            }
        }


        // The item behind it!!

        public Item RareTemplate
        {
            get { return World.FindItem(m_Serial); }
            set { m_Serial = value.Serial; }
        }


        // Name of our rare
        private string m_Name;

        // The item we serialise for duplication
        private Serial m_Serial;

        // The start and end indexes of the rare (the last index will
        // reflect the max. no of this rare that will drop)
        private short m_CurIndex;
        private short m_LastIndex;
        private short m_StartIndex;

        private DateTime m_StartDate;
        private DateTime m_EndDate;


        public DODInstance(Item item)
        {
            m_Serial = item.Serial;
            m_StartDate = new DateTime();
            m_EndDate = new DateTime();
        }

        public DODInstance(GenericReader bfr)
        {
            Load(bfr);
        }

        public void Load(GenericReader sr)
        {
            // Load instancing data

            // version
            short version = sr.ReadShort();

            switch (version)
            {
                case 0:
                    {
                        m_Name = sr.ReadString();
                        m_StartIndex = sr.ReadShort();
                        m_LastIndex = sr.ReadShort();
                        m_CurIndex = sr.ReadShort();
                        m_StartDate = sr.ReadDateTime();
                        m_EndDate = sr.ReadDateTime();
                        m_Serial = (Serial)sr.ReadInt();
                        break;
                    }
            }

            return;
        }

        public void Save(GenericWriter bfw)
        {
            // Save instancing data

            // Version
            bfw.Write((short)0);

            bfw.Write((string)m_Name);
            bfw.Write((short)m_StartIndex);
            bfw.Write((short)m_LastIndex);
            bfw.Write((short)m_CurIndex);
            bfw.Write((DateTime)m_StartDate);
            bfw.Write((DateTime)m_EndDate);
            bfw.Write((int)m_Serial);
        }

        // Dynamically fill the StartIndex, LastIndex and CurIndex data
        // substituting any "%<data_name>%" strings (eg. %StartIndex%)

        public void DynamicFill(Item item)
        {
            try
            {
                Type t = RareTemplate.GetType();
                PropertyInfo[] allProps = t.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                foreach (PropertyInfo prop in allProps)
                {
                    if (prop.CanRead && prop.CanWrite)
                    {
                        // Is it a string?
                        if (prop.PropertyType == typeof(string))
                        {   // need to check for the null property value
                            object temp = prop.GetValue(RareTemplate, null);
                            if (temp != null)
                            {
                                // Grab the value, substitute and re-set
                                string sVal = temp.ToString();

                                sVal = sVal.Replace("%StartIndex%", m_StartIndex.ToString());
                                sVal = sVal.Replace("%LastIndex%", m_LastIndex.ToString());
                                sVal = sVal.Replace("%CurIndex%", m_CurIndex.ToString());

                                prop.SetValue(item, sVal, null);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }

            // Store rare data invisibly inside the object
            /*item.RareData = ((uint)(LastIndex & 0xFF) << 16) |    // high (3rd byte)
                            ((uint)(StartIndex & 0xFF) << 8) |
                            ((uint)(CurIndex & 0xFF));          // low (1st byte)*/

        }

        // Expire the rare
        public void Expire()
        {
            // Log this deletion before we lose all the associated data :P
            LogHelper lh = new LogHelper("RareExpiration.log", false, true);
            lh.Log(LogType.Item, this.RareTemplate, string.Format("{0}", this.Name));
            lh.Finish();


            // Delete the "in storage" rare 
            this.RareTemplate.Delete();

            // Find it in the group lists + remove
            for (int i = 0; i < RareFactory.DODGroup.Count; i++)
            {
                DODGroup dg = (DODGroup)RareFactory.DODGroup[i];
                for (int ir = 0; ir < dg.DODInst.Count; ir++)
                    if (((DODInstance)dg.DODInst[ir]) == this)
                    {   // There should never be more than one of these right?
                        dg.DODInst.RemoveAt(ir);
                        break;
                    }
            }

            // Find it in the main rare list + remove
            for (int i = 0; i < RareFactory.DODInst.Count; i++)
            {
                DODInstance di = (DODInstance)RareFactory.DODInst[i];
                if (di == this)
                {   // There should never be more than one of these right?
                    RareFactory.DODInst.RemoveAt(i);
                    break;
                }
            }
        }

        // Dump a copy of the object's properties to console
        // (really for debugging purposes)
        public void DumpImage()
        {
            Type t = RareTemplate.GetType();
            try
            {
                PropertyInfo[] allProps = t.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                foreach (PropertyInfo prop in allProps)
                {
                    //Console.WriteLine("[{0}] Property : {1}", m_Name, prop);
                    //Console.WriteLine("[{0}] Value    : {1}", m_Name, prop.GetValue(m_Item, null));
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
        }

    }


}