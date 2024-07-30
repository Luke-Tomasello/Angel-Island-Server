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

/* Engines/ChampionSpawn/ChampMobileFactoryGump.cs
		10/27/07, plasma
 *		Initial creation
*/


using Server.Engines.ChampionSpawn;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Items
{


    public class ChampMobileFactoryGump : Gump
    {
        private ChampEngine m_ChampSpawn;                               //Champion spawn
        private List<Spawner> m_SpawnerMemory;                  //Memory list

        public ChampEngine ChampSpawn
        {
            get { return m_ChampSpawn; }
        }

        //Base ctor for new gump
        public ChampMobileFactoryGump(ChampEngine spawner)
            : this(spawner, null)
        {

        }

        //Ctor to continue an prevoius gump's state
        public ChampMobileFactoryGump(ChampEngine champSpawner, List<Spawner> memory)
            : base(50, 50)
        {
            m_ChampSpawn = champSpawner;

            //If a previous state was passed, restore it, else create a new memory list
            if (memory == null)
            {
                m_SpawnerMemory = new List<Spawner>();
                //Populate memory list with contents of the champ spawn's item collection
                foreach (Item i in m_ChampSpawn.Items)
                    if (i is Spawner)
                        m_SpawnerMemory.Add(i as Spawner);
            }
            else
            {
                m_SpawnerMemory = memory;
            }

            AddPage(0);

            AddBackground(0, 0, 348, 350, 5054);

            AddLabel(85, 1, 0, "Mobile Factory Spawner List");

            AddButton(310, 325, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
            AddLabel(272, 325, 0x384, "Cancel");

            AddButton(140, 325, 0xFBD, 0xFBF, 2, GumpButtonType.Reply, 0);
            AddLabel(173, 325, 0x384, "Refresh");

            AddButton(5, 325, 0xFB7, 0xFB9, 1, GumpButtonType.Reply, 0);
            AddLabel(38, 325, 0x384, "Okay");

            for (int i = 0; i < 13; i++)
            {
                AddButton(5, (22 * i) + 20, 0xFA5, 0xFA7, 4 + (i * 4), GumpButtonType.Reply, 0);        //Add
                AddButton(38, (22 * i) + 20, 0xFB1, 0xFB3, 5 + (i * 4), GumpButtonType.Reply, 0);       //Delete

                //If a spawner with this index exists and the mobile is not on the internal map then add a selected button
                if (i < m_SpawnerMemory.Count && (m_SpawnerMemory[i].TemplateMobile != null && m_SpawnerMemory[i].TemplateMobile.Map != Map.Internal))
                    AddButton(71, (22 * i) + 20, 0xFA9, 0xFAA, 6 + (i * 4), GumpButtonType.Reply, 0);       //Show/hide template mobile (selected)
                                                                                                            //Else just add the normal button
                else
                    AddButton(71, (22 * i) + 20, 0xFA8, 0xFAA, 6 + (i * 4), GumpButtonType.Reply, 0);       //Show/hide template mobile

                AddButton(310, (22 * i) + 20, 0xFAB, 0xFAD, 7 + (i * 4), GumpButtonType.Reply, 0);  //view spawner properties

                AddImageTiled(108, (22 * i) + 20, 199, 23, 0x52);
                AddImageTiled(108, (22 * i) + 21, 197, 21, 0xBBC);

                string str = string.Empty;

                //read the spawner data from the memory list
                if (i < m_SpawnerMemory.Count)
                {
                    str = m_SpawnerMemory[i].Serial + " ";
                    if (m_SpawnerMemory[i].TemplateMobile != null)
                        str += m_SpawnerMemory[i].TemplateMobile.GetType().Name;
                }

                AddLabel(112, (22 * i) + 21, 0, str);
            }
        }


        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_ChampSpawn.Deleted)
                return;

            switch (info.ButtonID)
            {
                case 0: // Closed 
                    {
                        //Will not apply any changes
                        break;
                    }
                case 1: // Okay 
                    {
                        //Update original spawner list with any changes
                        //First delete anything that no longer appears in the list
                        List<Spawner> tempList = new List<Spawner>();
                        foreach (Item i in m_ChampSpawn.Items)
                            if (i is Spawner && !m_SpawnerMemory.Contains(i as Spawner))
                                tempList.Add(i as Spawner);

                        foreach (Spawner s in tempList)
                        {
                            m_ChampSpawn.RemoveItem(s);
                            if (s.TemplateMobile != null)
                                s.TemplateMobile.Delete();
                            s.Delete();
                        }

                        tempList.Clear();

                        //Now add any new spawners
                        foreach (Spawner s in m_SpawnerMemory)
                            if (!m_ChampSpawn.Items.Contains(s))
                            {
                                s.Running = false;
                                m_ChampSpawn.AddItem(s);
                            }

                        break;
                    }
                case 2:
                    {
                        state.Mobile.SendGump(new ChampMobileFactoryGump(m_ChampSpawn, m_SpawnerMemory));
                        break;
                    }
                default:
                    {
                        //Calculate which button was pressed on what row
                        int buttonID = info.ButtonID - 4;
                        int index = buttonID / 4;
                        int type = buttonID % 4;

                        if (type == 0) // Add/replace spawner
                        {
                            state.Mobile.Target = new SpawnerTarget(m_SpawnerMemory, m_ChampSpawn, index);
                            return;
                        }
                        else if (index < m_SpawnerMemory.Count)
                        {
                            if (type == 1) // Remove items
                            {
                                m_SpawnerMemory.RemoveAt(index);
                            }
                            else if (type == 2) //Show/hide template mobile
                            {
                                Mobile m = m_SpawnerMemory[index].TemplateMobile;
                                if (m != null)
                                {
                                    if (m.Map != Map.Internal)
                                    {
                                        m.Map = Map.Internal;
                                    }
                                    else
                                    {
                                        m.Location = state.Mobile.Location;
                                        m.Map = state.Mobile.Map;
                                    }

                                }
                            }
                            else if (type == 3) //Show spawner properties
                            {
                                {
                                    state.Mobile.SendGump(new PropertiesGump(state.Mobile, (m_SpawnerMemory[index])));
                                    state.Mobile.SendGump(new SpawnerGump((m_SpawnerMemory[index])));
                                }
                            }
                        }
                        //Re-show the champ factory gump
                        state.Mobile.SendGump(new ChampMobileFactoryGump(m_ChampSpawn, m_SpawnerMemory));

                        break;
                    }
            }
        }


        private class SpawnerTarget : Target
        {
            List<Spawner> m_SpawnerMemory = new List<Spawner>();
            ChampEngine m_Champ = null;
            int m_toReplace = 0;

            public SpawnerTarget(List<Spawner> spawnerMemory, ChampEngine champ, int toReplace)
                : base(-1, true, TargetFlags.None)
            {
                m_SpawnerMemory = spawnerMemory;
                m_Champ = champ;
                m_toReplace = toReplace;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Spawner)
                {
                    Spawner s = (Spawner)o;
                    //Check it doesn't already exist (should be impossible)
                    if (m_SpawnerMemory.Contains(s))
                        from.SendMessage(string.Format("The champ already contains this spawner!"));

                    if (m_toReplace >= m_SpawnerMemory.Count && m_SpawnerMemory.Count == 13)
                    {
                        from.SendMessage(string.Format("The champ's mobile factory cannot hold any more spawners!"));
                    }
                    else
                    {
                        s.Running = false;
                        //Remove the current spawner first if neccessary
                        if (m_toReplace < m_SpawnerMemory.Count)
                            m_SpawnerMemory.RemoveAt(m_toReplace);

                        m_SpawnerMemory.Add(s);
                    }
                }
                else
                {
                    from.SendMessage("Target needs to be a spawner!!");
                }
                from.SendGump(new ChampMobileFactoryGump(m_Champ, m_SpawnerMemory));
            }
        }


    }

}