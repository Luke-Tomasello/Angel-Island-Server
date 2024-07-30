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

/* Scripts/Engines/ChampionSpawn/Modes/ChampSummon.cs
 *	ChangeLog:
  *	07/23/08, weaver
 *		Added Free() before return in IPooledEnumerable loop.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *	02/11/2007, plasma
 *		Updated altar references to use new prop
 *		Updated graphics checks to use IsHealthy()
 *	11/01/2006, plasma
 *		Added speaker item to allow altar or champ to talk
 * 10/28/2006, plasma
 *		Initial creation
 * 
 **/


using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Engines.ChampionSpawn
{
    public class ChampSummon : ChampEngine
    {
        // members
        private DateTime m_LastSpeech;                      // Determines when the summon alter can next shout stuff
        private SummonTimer m_SummonTimer;              //Controls effects and summon procedure
        private bool m_Summoning;                                   // prevents multiple summon attempts		
        private Item m_Speaker;                                     // who speaks.. altar or champ


        // Override graphics  prop to make sure they arent added or removed during a summon
        // also toggles speech from altar to champ item if gfx are switched off
        [CommandProperty(AccessLevel.GameMaster)]
        public override ChampGFX ChampGFX
        {
            get { return m_ChampGFX; }
            set
            {
                if (m_ChampGFX == value || m_Summoning)
                    return;

                bool bPentagram = (m_ChampGFX & ChampGFX.Altar) != 0 && (value & ChampGFX.Altar) != 0;
                bool bPlatform = (m_ChampGFX & ChampGFX.Platform) != 0 && (value & ChampGFX.Platform) != 0;
                m_ChampGFX = value;

                if (bPentagram || bPlatform)
                {
                    // Switch gfx on
                    if (m_Graphics != null) m_Graphics.Delete();
                    m_Graphics = new ChampGraphics(this);
                    m_Graphics.UpdateLocation();
                    m_Speaker = (Item)m_Graphics.Altar;
                }
                else
                {
                    // switch em off!. and delete. and stuff.
                    if (m_Graphics != null)
                        m_Graphics.Delete();

                    m_Graphics = null;

                    m_Speaker = (Item)this;
                }
            }
        }

        [Constructable]
        public ChampSummon()
            : base()
        {
            // just switch on gfx
            //Graphics = true;
            SetBool(ChampGFX.Altar, true);
            m_LastSpeech = DateTime.UtcNow;
        }

        public ChampSummon(Serial serial)
            : base(serial)
        {
        }

        // #region serialize

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_LastSpeech = DateTime.UtcNow;
                        // as this champ is always "active" even with no spawn, 
                        // we need to check if there is a spawn on and if not stop the slice
                        if (Level == 0 && Kills == 0 && m_Monsters.Count == 0)
                        {
                            StopSlice();
                        }
                        if (GetBool(ChampGFX.Altar))
                            m_Speaker = (Item)m_Graphics.Altar;
                        else
                            m_Speaker = this;

                        break;
                    }
            }
        }
        // #endregion

        protected override void Activate()
        {
            // override this to stop the spawn starting on activate so we can have summons
        }

        public override void Restart()
        {
            // override this to blank, restart timers don't work on summon spawns!
        }

        protected override void AdvanceLevel()
        {
            if (IsFinalLevel)
            {
                // if this is the champion level (champ finished) we need to make sure
                // the spawn is still set to active after the level up so a new summon can take place
                base.AdvanceLevel();
                base.Running = true;
            }
            else
            {
                base.AdvanceLevel();
            }
        }

        //Plasma - code for summoning champions via laying an item 
        //as a sacirfice as defined in requirements array
        public override bool HandlesOnMovement { get { return true; } }
        public override bool HandlesOnSpeech { get { return true; } }

        //Plasma: Make the altar speak to the players to indicate it's in summon mode!
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);
            if (m is PlayerMobile && !m_Summoning && m.AccessLevel == AccessLevel.Player
                    && m_Graphics.IsHealthy())
            {
                //Check speech delay so altar doesn't go crazy! 
                //pla: 08/26 changed delay to 12
                if ((m_LastSpeech + TimeSpan.FromSeconds(12)) < DateTime.UtcNow)
                {
                    //make sure champ hasn't already been started					
                    if (base.Running && Kills <= Lvl_MaxKills / 4 && Level == 0 && m.InRange(Location, 8))
                    {
                        // reset delay
                        m_LastSpeech = DateTime.UtcNow;
                        switch (Utility.Random(5))
                        {
                            case 1: m_Speaker.PublicOverheadMessage(0, 0x3B2, false, "You dare disturb the Champion's slumber?"); break;
                            case 2: m_Speaker.PublicOverheadMessage(0, 0x3B2, false, "Turn back whilst you still can, foolish mortals!"); break;
                            case 3: m_Speaker.PublicOverheadMessage(0, 0x3B2, false, "Offer a sacrifice, or begone from here!"); break;
                            case 4: m_Speaker.PublicOverheadMessage(0, 0x3B2, false, "You dare to challenge the Champions?"); break;
                        }
                    }
                }
            }
        }

        //Plasma : this handles the player's speech for a summon attempt 		
        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);
            //Check this is a summon spawn which hasn't been "started"			
            if (!m_Summoning && base.Running && Kills <= Lvl_MaxKills / 4 &&
                e.Mobile.InLOS(Location) && e.Mobile.InRange(Location, 5) && e.Mobile is PlayerMobile)
            {
                //Clean up string
                string s = e.Speech.Trim();
                s = s.ToLower();

                //now check speech and call the summon dependant on spawn type
                if (s.Length > 0)
                {
                    if (s == "i offer this sacrifice in your name barracoon")
                    {
                        SummonStart(ChampLevelData.SpawnTypes.VerminHorde);
                    }
                    else if (s == "i offer this sacrifice in your name mephitis")
                    {
                        SummonStart(ChampLevelData.SpawnTypes.Arachnid);
                    }
                    else if (s == "i offer this sacrifice in your name neira")
                    {
                        SummonStart(ChampLevelData.SpawnTypes.UnholyTerror);
                    }
                    else if (s == "i offer this sacrifice in your name rikktor")
                    {
                        SummonStart(ChampLevelData.SpawnTypes.ColdBlood);
                    }
                    else if (s == "i offer this sacrifice in your name semidar")
                    {
                        SummonStart(ChampLevelData.SpawnTypes.Abyss);
                    }
                }
            }
        }

        //Plasma :  This code is called from OnSpeech() and is repsonsible for
        //finding a viable sacirifce and if so to start the summoning
        protected void SummonStart(ChampLevelData.SpawnTypes ChampType)
        {
            //now check the floor for the appropriate sacrifice
            SummonReq Req = null;
            ArrayList Reqs = new ArrayList();

            //Check through requirements array and add all for this type of spawn
            for (int i = 0; i < m_SummonReqs.GetLength(0); ++i)
                if (m_SummonReqs[i].ChampType == ChampType)
                    Reqs.Add(m_SummonReqs[i]);

            //Now search the floor for appropriate sacrifice(s)
            if (Reqs.Count > 0)
            {
                //pla: 08/26 changed range to 1
                IPooledEnumerable eable = m_Speaker.GetItemsInRange(1);
                foreach (Item item in eable)
                {
                    for (int count = 0; count < Reqs.Count; ++count)
                    {
                        Req = ((SummonReq)Reqs[count]);
                        if (item.GetType().IsAssignableFrom(Req.ItemType) && item.Amount >= Req.Amount /*  || item.GetType().IsSubclassOf( Req.ItemType ) */ )
                        {
                            //Found sacrifice, make it non-movable whilst the summon is in progress
                            item.Movable = false;
                            m_SummonTimer = null;
                            //Start summon timer
                            m_Summoning = true;
                            m_SummonTimer = new SummonTimer(item, Req.ChampType, this, GetBool(ChampGFX.Altar));
                            eable.Free();
                            return;
                        }
                    }
                }
                eable.Free();
            }
            this.PublicOverheadMessage(0, 0x3B2, false, "The Champion does not see a worthy sacrifice");
        }

        //Plasma :  This timer(!!) is used as a simple vairable recurring delay
        //to control the effects and make it much more interesting for the players.
        //NOTE - this timer is prevented from being called more than once due to bool  m_Summoning
        protected class SummonTimer : Timer
        {
            int cycle = 0;                                          //What's going on at the moment
            Item Sacrifice;                                         //Sarcrifice item
            ChampLevelData.SpawnTypes ChampType;        //Spawn type
            ChampSummon Champ;                      //Spawn
            private bool bGraphics;

            public SummonTimer(Item sacrifice, ChampLevelData.SpawnTypes cst, ChampSummon cs, bool gfx)
                : base(TimeSpan.FromSeconds(3))
            {
                Sacrifice = sacrifice; ChampType = cst; Champ = cs; bGraphics = gfx;
                Start();
            }

            protected override void OnTick()
            {
                //This is where all the action happens for the summon
                switch (cycle)
                {
                    case 0:
                        {
                            // Stage 0 -- Let the players know something is happening!
                            Effects.PlaySound(Champ.Location, Champ.Map, 0x2F3);
                            IPooledEnumerable eable = Champ.GetMobilesInRange(15);
                            foreach (Mobile m in eable)
                                if (m is PlayerMobile)
                                    m.SendMessage("You feel the ground begin to shake beneath your feet!");
                            eable.Free();

                            ++cycle;
                            Delay = TimeSpan.FromSeconds(4);
                            Start();
                            break;
                        }
                    case 1:
                        {

                            // Stage 1 -- champion considers the sacrifice
                            Champ.m_Speaker.PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, false, "The Champion considers your sacrifice...");

                            if (Sacrifice != null)
                            {
                                //flamestrike and delete sacrifice
                                Effects.SendLocationParticles(EffectItem.Create(Sacrifice.Location, Sacrifice.Map, EffectItem.DefaultDuration), 0x3709, 30, 30, 3000);
                                Effects.PlaySound(Sacrifice.Location, Sacrifice.Map, 0x225);
                                Sacrifice.Delete();
                            }

                            //Next cycle..
                            ++cycle;
                            Delay = TimeSpan.FromSeconds(Utility.Random(5, 10));
                            Start();

                            break;
                        }
                    case 2:
                        {

                            //Show accept message
                            Champ.m_Speaker.PublicOverheadMessage(0, 0x3B2, false, "The Champion accepts your challenge!");

                            // Do flame strikes on corners of altar in paris						
                            if (Champ.m_Graphics != null)
                                if (Champ.GetBool(ChampGFX.Altar) && Champ.m_Graphics.IsHealthy())
                                {
                                    Effects.SendLocationParticles(EffectItem.Create(new Point3D(Champ.m_Graphics.Altar.X + 3, Champ.m_Graphics.Altar.Y + 3, Champ.m_Graphics.Altar.Z + 15), Champ.m_Graphics.Altar.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                                    Effects.SendLocationParticles(EffectItem.Create(new Point3D(Champ.m_Graphics.Altar.X, Champ.m_Graphics.Altar.Y, Champ.m_Graphics.Altar.Z + 15), Champ.m_Graphics.Altar.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                                }
                            Effects.PlaySound(Champ.Location, Champ.Map, 0x81);
                            ++cycle;
                            Delay = TimeSpan.FromMilliseconds(300);
                            Start();
                            break;
                        }
                    case 3:
                        {
                            //Other pair...
                            if (Champ.m_Graphics != null)
                                if (Champ.GetBool(ChampGFX.Altar) && Champ.m_Graphics.IsHealthy())
                                {
                                    Effects.SendLocationParticles(EffectItem.Create(new Point3D(Champ.m_Graphics.Altar.X + 3, Champ.m_Graphics.Altar.Y, Champ.m_Graphics.Altar.Z + 15), Champ.m_Graphics.Altar.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                                    Effects.SendLocationParticles(EffectItem.Create(new Point3D(Champ.m_Graphics.Altar.X, Champ.m_Graphics.Altar.Y + 3, Champ.m_Graphics.Altar.Z + 15), Champ.m_Graphics.Altar.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                                }

                            //Clear the existing spawn out

                            Champ.m_Summoning = false;
                            Champ.SpawnType = ChampType;
                            break;
                        }
                }
            }
        }


        //Plasma: class responsible for holidng summon requirement data
        //For each speciif champion
        protected class SummonReq
        {
            public Type ItemType;               // item type to sacrifice
            public int Amount;                  // amount of the item
            public ChampLevelData.SpawnTypes ChampType; // spawn type this relates to

            //public SummonReq(){}			
            public SummonReq(Type item, int amnt, ChampLevelData.SpawnTypes cst)
            {
                //Assign properties
                ItemType = item;
                Amount = amnt;
                ChampType = cst;
            }
        }

        //Plasma : summoning requirements data array
        private static SummonReq[] m_SummonReqs = new SummonReq[]
        {
            new SummonReq( typeof(CheeseWedge),    1,       ChampLevelData.SpawnTypes.VerminHorde),
            new SummonReq( typeof(CheeseWheel),     1,      ChampLevelData.SpawnTypes.VerminHorde),
            new SummonReq( typeof(SpidersSilk),         1000 ,  ChampLevelData.SpawnTypes.Arachnid),
            new SummonReq( typeof(Bone),                    100,        ChampLevelData.SpawnTypes.UnholyTerror),
            new SummonReq( typeof(BloodVial),               100,        ChampLevelData.SpawnTypes.Abyss),

			//Rikktor accepts any type of gem, but they are not derrived from a base so each must be added
			new SummonReq( typeof(Amber),                  500,        ChampLevelData.SpawnTypes.ColdBlood),
            new SummonReq( typeof(Amethyst),            500,        ChampLevelData.SpawnTypes.ColdBlood),
            new SummonReq( typeof(Citrine),                 500,        ChampLevelData.SpawnTypes.ColdBlood),
            new SummonReq( typeof(Diamond),             500,        ChampLevelData.SpawnTypes.ColdBlood),
            new SummonReq( typeof(Emerald),             500,        ChampLevelData.SpawnTypes.ColdBlood),
            new SummonReq( typeof(Ruby),                    500,        ChampLevelData.SpawnTypes.ColdBlood),
            new SummonReq( typeof(Sapphire),                500,        ChampLevelData.SpawnTypes.ColdBlood),
            new SummonReq( typeof(StarSapphire),        500,        ChampLevelData.SpawnTypes.ColdBlood),
            new SummonReq( typeof(Tourmaline),          500,        ChampLevelData.SpawnTypes.ColdBlood)
        };


        public override void OnSingleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                // this is a gm, allow normal text from base and champ indicator
                LabelTo(from, "Summon Champ");
                base.OnSingleClick(from);
            }
        }
    }
}