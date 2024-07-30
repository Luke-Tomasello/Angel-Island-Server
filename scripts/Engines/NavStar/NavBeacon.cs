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

/* Scripts/Engines/NavStar/NavBeacon.cs
 * CHANGELOG
 * 3/26/2024, Adam,
 *   Complete rewrite. of the NavStar system. Smaller (works,) and easier to understand
 * 9/12/21, Adam
 *  Add objective to beacon properties so that we can determine direction to travel without hard-coding it in the code.
 *      That is, there will be one beacon tagged as the 'objective', all beacons will lead in that direction.
 * 11/30/05, Kit
 *		Set Movable to false, added On/Off active switch, set all path values default -1, changed to admin access only
 * 11/18/05, Kit
 * 	Initial Creation
 */

using System.Collections.Generic;

namespace Server.Items
{

    public class NavBeacon : Item
    {
        public static void Initialize()
        {   // easy, walk around the drop beacons at your feet
            CommandSystem.Register("DropBeacon", AccessLevel.Owner, new CommandEventHandler(DropBeacon_OnCommand));
        }
        private static Dictionary<string, List<NavBeacon>> m_registry = new();
        public static Dictionary<string, List<NavBeacon>> Registry { get { return m_registry; } }

        #region FindObjective
        #region TODO?
        public NavBeacon FindObjective()
        {
            return FindObjective(this);
        }
        public NavBeacon FindObjective(NavBeacon beacon)
        {
            foreach (KeyValuePair<string, List<NavBeacon>> kvp in Registry)
            {
                if (kvp.Value.Contains(beacon))
                {
                    foreach (NavBeacon check in kvp.Value)
                    {
                        //TODO
                        //if (check.Objective == true)
                        //return check;
                    }
                }
            }

            return null;
        }
        #endregion TODO?
        public static NavBeacon FindObjective(Mobile m, string dest)
        {
            if (!string.IsNullOrEmpty(dest) && Registry.ContainsKey(dest))
                foreach (NavBeacon beacon in Registry[dest])
                    // TODO
                    ;// if (beacon.Objective == true)
                     //return beacon;

            return null;
        }

        #endregion FindObjective

        #region Props
        private byte m_LinkCount;   // for debugging. Tell us how many mobs are using us
        [CommandProperty(AccessLevel.Seer, AccessLevel.Seer)]
        public byte LinkCount { get { return m_LinkCount; } set { m_LinkCount = value; } }

        [CommandProperty(AccessLevel.Seer)]
        public bool Active
        {
            get { return base.IsRunning; }
            set { base.IsRunning = value; }
        }
        private string m_Journey;
        [CommandProperty(AccessLevel.Seer)]
        public string Journey { get { return m_Journey; } set { m_Journey = value; } }
        private int m_RingID;
        [CommandProperty(AccessLevel.Seer)]
        public int Ring { get { return m_RingID; } set { m_RingID = value; } }
        #endregion Props

        [Constructable]
        public NavBeacon(string journey, int beaconID)
            : base(0x1ECD)
        {
            Name = "NavBeacon";
            Weight = 0.0;
            Hue = 0x47E;
            Visible = false;
            Movable = false;
            Active = true;

            m_Journey = journey;
            m_RingID = beaconID;

            RegisterBeacon();
        }
        public override Item Dupe(int amount)
        {
            Item new_item = new NavBeacon(m_Journey, m_RingID);
            return base.Dupe(new_item, amount);
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, string.Format("{0}: Ring {1}", m_Journey, m_RingID));
            if (Debug)
                LabelTo(from, string.Format("{0} links here", m_LinkCount));
            if (Active)
                LabelTo(from, "[Active]");
            else
                LabelTo(from, "[Disabled]");
        }
        [CommandProperty(AccessLevel.Administrator)]
        public override bool Debug
        {
            get
            {
                return base.Debug;
            }
            set
            {
                base.Debug = value;
            }
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public override void OnDelete()
        {
            UnregisterBeacon();
            base.OnDelete();
        }

        public NavBeacon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)5);

            // version 5, remove m_objective

            // version 4
            // use base Item for running state

            // version 3
            writer.Write(m_Journey);
            writer.Write(m_RingID);

            // version 2 - obsolete in version 5
            //writer.Write(m_objective);

            // version 1 - obsolete in version 4
            //writer.Write(m_Running);

            //WriteNavArray(writer, NavArray);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 5:
                case 4:
                case 3:
                    {
                        m_Journey = reader.ReadString();
                        m_RingID = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        if (version < 5)
                            /*m_objective = */
                            reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 4)
                            /*m_Running = */
                            reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 3)
                        {
                            ///*NavArray = */ReadNavArray(reader);
                            int size = reader.ReadInt();
                            int[,] newBA = new int[size, 1];

                            for (int i = 0; i < size; i++)
                            {
                                newBA[i, 0] = reader.ReadInt();
                            }
                        }
                        break;
                    }

            }

            // register our beacons for easy (and quick) access
            if (version > 2)
                RegisterBeacon();
        }
        public void RegisterBeacon()
        {
            System.Diagnostics.Debug.Assert(m_Journey != null);

            if (Registry.ContainsKey(m_Journey))
            {
                if (!Registry[m_Journey].Contains(this))
                    Registry[m_Journey].Add(this);
            }
            else
            {
                Registry.Add(m_Journey, new List<NavBeacon>());
                Registry[m_Journey].Add(this);
            }
        }
        public void UnregisterBeacon()
        {
            if (m_Journey == null)
                return;

            if (Registry.ContainsKey(m_Journey))
            {
                if (Registry[m_Journey].Contains(this))
                    Registry[m_Journey].Remove(this);

                if (Registry[m_Journey].Count == 0)
                    Registry.Remove(m_Journey);
            }
        }

        [Usage("DropBeacon <journey name> <ring number>")]
        [Description("Drops a beacon at your feet for the specified Journey with the specified Ring number.")]
        public static void DropBeacon_OnCommand(CommandEventArgs e)
        {
            try
            {
                new NavBeacon(e.GetString(0), e.GetInt32(1)).MoveToWorld(e.Mobile.Location, e.Mobile.Map);
            }
            catch
            {
                e.Mobile.SendMessage("Usage: DropBeacon <journey name> <ring number>");
            }
        }

    }

}