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

/* Scripts/Engines/ChampionSpawn/Modes/ChampInvasion.cs
 *	ChangeLog:
 *  9/21/21, Adam
 *      Add the Adam Ant champ spawn
 *      Add support for in ChampEngine for the special "Doppelganger" spawns. 
 *	10/28/2006, plasma
 *		Initial creation
 * 
 **/

namespace Server.Engines.ChampionSpawn
{
    // This is the town Invasion champion spawn, automated by the AES
    public class ChampInvasion : ChampEngine
    {
        // Members
        private TownInvasionAES m_aesMonitor;                   // external AES spawn monitor

        // props
        public TownInvasionAES AESMonitor                  // and a prop for it
        {
            get { return m_aesMonitor; }
            set { m_aesMonitor = value; }
        }

        [Constructable]
        public ChampInvasion()
            : base()
        {
            // pick a random champ
            PickChamp();
            // switch off  gfx and restart timer
            //Graphics = false;
            m_ChampGFX = ChampGFX.None;
            m_bRestart = false;
        }

        protected override void AdvanceLevel()
        {
            // has champ just been completed?
            if (IsFinalLevel)
            {
                UpdateMonitors("FinalLevel");
                // tell AES that the champ is over
                if (m_aesMonitor != null && m_aesMonitor.Deleted == false)
                {
                    UpdateMonitors("ChampComplete");
                    m_aesMonitor.ChampComplete = true;
                }
            }
            base.AdvanceLevel();
        }
        public ChampInvasion(Serial serial)
            : base(serial)
        {
        }

        // #region serialize

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
            writer.Write(m_aesMonitor);  //AES 			
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_aesMonitor = reader.ReadItem() as TownInvasionAES;
                        break;
                    }
            }
        }
        // #endregion

        public ChampLevelData.SpawnTypes PickChamp()
        {
            // Currently the invasions randomly pick one of the 5 main big skull giving champs
            switch (Utility.Random(6))
            {
                case 0:
                    {
                        return SpawnType = ChampLevelData.SpawnTypes.Abyss;
                    }
                case 1:
                    {
                        return SpawnType = ChampLevelData.SpawnTypes.Arachnid;
                    }
                case 2:
                    {
                        return SpawnType = ChampLevelData.SpawnTypes.ColdBlood;
                    }
                case 3:
                    {
                        return SpawnType = ChampLevelData.SpawnTypes.UnholyTerror;
                    }
                case 4:
                    {
                        return SpawnType = ChampLevelData.SpawnTypes.VerminHorde;
                    }
                case 5:
                    {
                        return SpawnType = ChampLevelData.SpawnTypes.Doppelganger;
                    }
            }

            return ChampLevelData.SpawnTypes.None;
        }

        public override void OnSingleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                // this is a gm, allow normal text from base and champ indicator
                LabelTo(from, "Invasion Champ");
                base.OnSingleClick(from);
            }
        }
    }
}