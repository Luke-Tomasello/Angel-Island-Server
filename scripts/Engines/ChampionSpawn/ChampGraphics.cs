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

/* Scripts/Engines/ChampionSpawn/ChampGraphics.cs
 *	ChangeLog:
 *  02/11/2007, Plasma
 *      -Changed altar to private and added public IsHealthy function
 *       that checks the altar and platform have not been deleted.
 *      -Added altar public get prop for ChampSummon 
 *	11/01/2006, plasma
 *		Changed skull location to be based on altar location
 *	10/28/2006, plasma
 *		Initial creation
 * 
 **/
using Server.Diagnostics;
using System;
using System.Collections;

namespace Server.Engines.ChampionSpawn
{
    // plasma : this class handles all aspects of the champion spawn's
    // altar and skull graphics.
    public class ChampGraphics
    {
        private const int m_xMaxRedSkulls = 16; // maximum amount of skulls that fit on platform
        private const int m_xMaxWhiteSkulls = 4;    // 		
        private ChampAltar m_Altar;                 // altar item		// pla: 02/11/07 changed to private
        private ArrayList m_RedSkulls;          // red skull list
        private ArrayList m_WhiteSkulls;        // white skull list
        private ChampEngine m_Champ;            // ChampEngine object 
        private ChampPlatform m_Platform;       // platform item
        private int m_AltarHue;                     // allows different base hues

        // ctor 
        public ChampGraphics(ChampEngine champ)
        {
            // Create new objects
            m_Champ = champ;
            if (champ.GetBool(ChampGFX.Platform))
            {
                m_Platform = new ChampPlatform(false, m_Champ);
                m_Platform.Visible = true;
                //m_Platform.Hue = 0;
            }
            if (champ.GetBool(ChampGFX.Altar))
            {
                m_Altar = new ChampAltar(false, m_Champ);
                m_Altar.Visible = true;
                m_AltarHue = 0;
                // these are only available if we have an alter
                m_RedSkulls = new ArrayList();
                m_WhiteSkulls = new ArrayList();
            }

            // move to location of champ spawn
            UpdateLocation();
        }

        /// <summary>
        /// pla: 02/11/2007
        /// Added prop to expose the altar object
        /// </summary>
        public ChampAltar Altar
        {
            get { return m_Altar; }
        }

        // Cosntructor called from ChampEngie.Deserialize() to recreate gfx
        public ChampGraphics(ChampEngine champ, GenericReader reader)
        {
            int ver = reader.ReadInt();

            switch (ver)
            {
                case 0:
                    {
                        //Alows re-creation of serialised graphics		
                        m_Champ = champ;
                        m_Platform = reader.ReadItem() as ChampPlatform;
                        m_Altar = reader.ReadItem() as ChampAltar;
                        m_RedSkulls = reader.ReadItemList();
                        m_WhiteSkulls = reader.ReadItemList();
                        m_AltarHue = reader.ReadInt();
                        //rehue
                        m_Altar.Hue = m_AltarHue;
                        break;
                    }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0);       // version
            writer.Write(m_Platform);
            writer.Write(m_Altar);
            writer.WriteItemList(m_RedSkulls, true);
            writer.WriteItemList(m_WhiteSkulls, true);

            //make sure hue is upto date
            if (m_Altar.Hue != m_AltarHue && m_Altar.Hue != 0x26) // if altar hue has changed then update
                m_AltarHue = m_Altar.Hue;

            writer.Write((int)m_AltarHue);
        }

        // Deletes all graphics from world
        public void Delete()
        {
            if (m_Altar != null)
            {
                m_Altar.Delete();
                m_Altar = null;
            }

            if (m_Platform != null)
            {
                m_Platform.Delete();
                m_Platform = null;
            }

            if (m_RedSkulls != null)
            {
                foreach (Item i in m_RedSkulls)
                    i.Delete();
                m_RedSkulls.Clear();
            }

            if (m_WhiteSkulls != null)
            {
                foreach (Item i in m_WhiteSkulls)
                    i.Delete();
                m_WhiteSkulls.Clear();
            }
        }

        /// <summary>
        /// Plasma: 02/11/07
        /// This method returns true if the altar and platform are both
        /// non-null and not deleted
        /// </summary>
        /// <returns>bool</returns>
        public bool IsHealthy()
        {

            bool need_altar = m_Champ.GetBool(ChampGFX.Altar);
            bool need_platform = m_Champ.GetBool(ChampGFX.Platform);
            bool have_altar = m_Altar != null && m_Altar.Deleted == false;
            bool have_platform = m_Platform != null && m_Platform.Deleted == false;

            if (need_altar && need_platform)
                return have_altar && have_platform;

            if (need_altar)
                return have_altar;

            if (need_platform)
                return have_platform;

            return false; ;
        }

        // Update function to re-calculate skulls / altar
        public void Update()
        {
            if (m_Champ.Deleted)
                return;

            // this is the main function that gets called all the time from the champ
            // responsible for keeping all the skulls and altar hue up to date.

            // Is this not the final level?
            if (!m_Champ.IsFinalLevel)
            {
                if (!(m_Altar != null))
                    return;

                if (m_Altar.Hue == 0x26)                // if altar is "champ red" then reset it 
                    m_Altar.Hue = m_AltarHue;
                else if (m_Altar.Hue != m_AltarHue) // if altar hue has changed then store this data
                    m_AltarHue = m_Altar.Hue;

                try
                {
                    // caluclate how many red skulls per level
                    double RedsPerLevel = (double)m_xMaxRedSkulls / (double)(m_Champ.SpawnLevels.Count - 1);
                    // now how many kills per red
                    double KillsPerRed = (RedsPerLevel >= 1 ? ((double)m_Champ.Lvl_MaxKills / (double)RedsPerLevel) : m_Champ.Lvl_MaxKills);
                    // and white
                    double KillsPerWhite = (KillsPerRed >= 1 ? KillsPerRed / 5 : 1);

                    // Calculate how many reds to reach this level
                    double BaseReds = (RedsPerLevel * (double)m_Champ.Level);

                    // Calculate how many reds into the current level and set skulls				
                    SetRedSkullCount((int)Math.Floor(BaseReds + (m_Champ.Kills / KillsPerRed)));

                    // Now calculate how far to the next red we are..
                    if (KillsPerRed >= 1 && m_Champ.Kills > KillsPerRed)
                    {
                        double whites = (double)m_Champ.Kills - (Math.Floor((double)m_Champ.Kills / KillsPerRed) * KillsPerRed) / KillsPerWhite;
                        SetWhiteSkullCount((int)whites);
                    }
                    else
                    {
                        // math would have to be heavier to support whites across different levels for 1 red (not to mention useless)
                        // so we just have the whites represet the level progress rather than the red skull
                        SetWhiteSkullCount((int)Math.Floor(m_Champ.Kills / KillsPerWhite));
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    Console.WriteLine("Exception caught in ChampGraphics.Update.  Please send to plasma");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }


            }
            else
            {
                // Champ is on!  We want no skulls and a hued altar
                if (m_WhiteSkulls.Count > 0)
                    SetWhiteSkullCount(0);

                if (m_RedSkulls.Count > 0)
                    SetRedSkullCount(0);

                if (m_Altar.Hue != 0x26)
                    m_Altar.Hue = 0x26;
            }
        }

        // Update function to re-calculate map locations on move
        public void UpdateLocation()
        {
            if (m_Champ.Deleted)
                return;

            if (m_Platform != null)
            {
                m_Platform.Location = m_Champ.Location;
                //m_Platform.Z -= 5;
                m_Platform.Map = m_Champ.Map;
            }
            if (m_Altar != null)
            {
                m_Altar.Location = m_Champ.Location;
                m_Altar.Z += m_Champ.GetBool(ChampGFX.Platform) ? 5 : 0;
                m_Altar.Map = m_Champ.Map;
            }
            if (m_RedSkulls != null)
            {
                for (int i = 0; i < m_RedSkulls.Count; ++i)
                {
                    ((Item)m_RedSkulls[i]).Location = GetRedSkullLocation(i);
                    ((Item)m_RedSkulls[i]).Map = m_Champ.Map;
                };
            }
            if (m_WhiteSkulls != null)
            {
                for (int i = 0; i < m_WhiteSkulls.Count; ++i)
                {
                    ((Item)m_WhiteSkulls[i]).Location = GetWhiteSkullLocation(i);
                    ((Item)m_WhiteSkulls[i]).Map = m_Champ.Map;
                }
            }

        }

        private void IncreaseRed()
        {
            if (m_RedSkulls.Count >= m_xMaxRedSkulls)
                return;

            // Add new red skull
            Item skull = new Item(0x1854);
            skull.Hue = 0x26;
            skull.Movable = false;
            skull.Light = LightType.Circle150;
            skull.Visible = true;
            m_RedSkulls.Add(skull);
            skull.MoveToWorld(GetRedSkullLocation(m_RedSkulls.Count), m_Champ.Map);

        }

        private void IncreaseWhite()
        {
            if (m_WhiteSkulls.Count >= m_xMaxWhiteSkulls)
                return;

            // Add new white skull
            Item skull = new Item(0x1854);
            skull.Movable = false;
            skull.Light = LightType.Circle150;
            skull.Visible = true;
            m_WhiteSkulls.Add(skull);
            skull.MoveToWorld(GetWhiteSkullLocation(m_WhiteSkulls.Count),
                m_Champ.Map);

            // Play effects
            Effects.PlaySound(skull.Location, skull.Map, 0x29);
            Effects.SendLocationEffect(new Point3D(skull.X + 1, skull.Y + 1, skull.Z), skull.Map, 0x3728, 10);

        }

        private void SetRedSkullCount(int num)
        {
            // add/remove skulls as neccesary
            if (num > m_RedSkulls.Count && num <= m_xMaxRedSkulls)
            {
                while (m_RedSkulls.Count < num)
                    IncreaseRed();
            }
            else if (num < m_RedSkulls.Count && num >= 0)
            {
                while (m_RedSkulls.Count > num)
                    DecreaseRed();
            }
        }

        private void SetWhiteSkullCount(int num)
        {
            // add/remove skulls as neccesary
            if (num > m_WhiteSkulls.Count && num <= m_xMaxWhiteSkulls)
            {
                while (m_WhiteSkulls.Count < num)
                    IncreaseWhite();
            }
            else if (num < m_WhiteSkulls.Count && num >= 0)
            {
                while (m_WhiteSkulls.Count > num)
                    DecreaseWhite();
            }
        }

        private void DecreaseRed()
        {
            if (m_RedSkulls.Count == 0)
                return;

            ((Item)m_RedSkulls[m_RedSkulls.Count - 1]).Delete();
            m_RedSkulls.RemoveAt(m_RedSkulls.Count - 1);
        }

        private void DecreaseWhite()
        {
            if (m_WhiteSkulls.Count == 0)
                return;

            ((Item)m_WhiteSkulls[m_WhiteSkulls.Count - 1]).Delete();
            m_WhiteSkulls.RemoveAt(m_WhiteSkulls.Count - 1);
        }

        // skull location retrieval methods.  Extracted from old code.
        private Point3D GetRedSkullLocation(int index)
        {
            int x, y;

            if (index < 5)
            {
                x = index - 2;
                y = -2;
            }
            else if (index < 9)
            {
                x = 2;
                y = index - 6;
            }
            else if (index < 13)
            {
                x = 10 - index;
                y = 2;
            }
            else
            {
                x = -2;
                y = 14 - index;
            }

            return new Point3D(m_Altar.X + x, m_Altar.Y + y, m_Altar.Z);
        }

        // Extracted from old code
        private Point3D GetWhiteSkullLocation(int index)
        {
            int x, y;

            switch (index)
            {
                default:
                case 0: x = -1; y = -1; break;
                case 1: x = 1; y = -1; break;
                case 2: x = 1; y = 1; break;
                case 3: x = -1; y = 1; break;
            }

            return new Point3D(m_Altar.X + x, m_Altar.Y + y, m_Altar.Z);
        }

    }
}