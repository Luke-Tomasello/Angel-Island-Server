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

/* Scripts\Engines\CTFSystem\CTFSpeech.cs
 * CHANGELOG:
 * 4/10/10, adam
 *		initial framework.
 */

using System;

namespace Server.Engines
{
    public partial class CTFControl : Server.Items.CustomRegionControl
    {
        // player context data
        private class PlayerContextData
        {
            private enum IntNdx { Points, RespawnSeconds, SaveMana }
            private int[] m_IntData = { 0, 0, 0 };
            private Point3D m_PreGameLocation = Point3D.Zero;
            public Point3D Location { get { return m_PreGameLocation; } }
            private Map m_PreGameMap;
            public Map Map { get { return m_PreGameMap; } }
            private Mobile m_Mobile;
            public Mobile Mobile { get { return m_Mobile; } }
            private RespawnTimer m_RespawnTimer = null;
            private CTFControl m_ctfc;
            public CTFControl CTFControl { get { return m_ctfc; } }

            public int Points { get { return m_IntData[(int)IntNdx.Points]; } set { m_IntData[(int)IntNdx.Points] = value; } }
            public int RespawnSeconds { get { return m_IntData[(int)IntNdx.RespawnSeconds]; } set { m_IntData[(int)IntNdx.RespawnSeconds] = value; } }
            public int SaveMana { get { return m_IntData[(int)IntNdx.SaveMana]; } set { m_IntData[(int)IntNdx.SaveMana] = value; } }

            public PlayerContextData(Mobile m, CTFControl ctfc)
            {
                m_Mobile = m;                       // player that owns this context
                m_PreGameLocation = m.Location;     // starting location of the player (pregame)
                m_PreGameMap = m.Map;               // starting map of the player (pregame)
                m_ctfc = ctfc;                      // CTF Controller
            }

            public PlayerContextData(GenericReader reader)
            {
                Deserialize(reader);
            }

            public void Respawn(int seconds)
            {   // countdown
                RespawnSeconds = seconds;

                if (m_RespawnTimer != null && m_RespawnTimer.Running)
                {
                    m_RespawnTimer.Stop();
                    m_RespawnTimer.Flush();
                }

                m_RespawnTimer = new RespawnTimer(TimeSpan.FromSeconds(5), this, TimeSpan.FromSeconds(1));
                m_RespawnTimer.Start();
            }

            public class RespawnTimer : Timer
            {
                private PlayerContextData m_pcd;
                private int m_round;
                public RespawnTimer(TimeSpan delay, PlayerContextData pcd, TimeSpan tick)
                    : base(delay, tick)
                {
                    Priority = TimerPriority.TwoFiftyMS;
                    m_pcd = pcd;
                    m_round = m_pcd.CTFControl.Round;           // if it changes, we know the round is over
                }

                private bool RoundOver()
                {
                    if (m_pcd != null && m_pcd.CTFControl != null)
                        if (m_round == m_pcd.CTFControl.Round)
                            return false;

                    return true;
                }

                protected override void OnTick()
                {
                    if (m_pcd == null || m_pcd.Mobile == null)
                        return;

                    if (m_pcd.RespawnSeconds <= 0 || RoundOver())
                    {
                        this.Stop();
                        this.Flush();
                        if (m_pcd != null && m_pcd.CTFControl != null && m_pcd.CTFControl.Deleted == false)
                        {
                            m_pcd.CTFControl.RespawnMobile(m_pcd.Mobile);
                        }
                    }
                    else if (m_pcd.RespawnSeconds == m_pcd.CTFControl.RespawnSeconds)
                    {
                        m_pcd.CTFControl.Beep(m_pcd.Mobile, m_pcd.CTFControl.BeepSoundID);
                        m_pcd.Mobile.SendMessage(m_pcd.CTFControl.SystemMessageColor, "Respawn in {0}", m_pcd.RespawnSeconds);
                        m_pcd.RespawnSeconds--;
                    }
                    else
                    {
                        m_pcd.CTFControl.Beep(m_pcd.Mobile, m_pcd.CTFControl.BeepSoundID);
                        m_pcd.Mobile.SendMessage(m_pcd.CTFControl.SystemMessageColor, "{0}", m_pcd.RespawnSeconds);
                        m_pcd.RespawnSeconds--;
                    }
                }
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write((int)3); // version 

                // version 3 - save pregame map
                writer.Write(m_PreGameMap);

                // version 2 - save the mobile
                writer.Write(m_Mobile);
                writer.Write(m_ctfc);

                // write the plater's return location
                writer.Write(m_PreGameLocation);

                // write int data
                writer.Write((int)m_IntData.Length);
                foreach (int id in m_IntData)
                    writer.Write(id);
            }

            public void Deserialize(GenericReader reader)
            {
                int version = reader.ReadInt();

                switch (version)
                {
                    case 3:
                        {
                            m_PreGameMap = reader.ReadMap();
                            goto case 2;
                        }

                    case 2:
                        {
                            m_Mobile = reader.ReadMobile();
                            m_ctfc = (CTFControl)reader.ReadItem();
                            goto case 1;
                        }
                    case 1:
                        {
                            // read the player's return location
                            m_PreGameLocation = reader.ReadPoint3D();

                            // read the int data
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                                m_IntData[ix] = reader.ReadInt();
                            goto case 0;
                        }
                    case 0:
                        {
                            goto default;
                        }

                    default:
                        break;
                }

                // do we need a res?
                if (RespawnSeconds > 0 && m_Mobile != null && m_Mobile.Deleted == false && !m_Mobile.Alive)
                {   // use the remaining seconds - have a 10 second delay to accomodate for shard up
                    m_RespawnTimer = new RespawnTimer(TimeSpan.FromSeconds(10), this, TimeSpan.FromSeconds(1));
                    m_RespawnTimer.Start();
                }
            }
        }
    }
}