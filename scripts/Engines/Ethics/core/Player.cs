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

using Server.Mobiles;
using System;

namespace Server.Ethics
{
    public class PlayerCollection : System.Collections.ObjectModel.Collection<Player>
    {
    }

    [PropertyObject]
    public class Player
    {
        public static Player Find(Mobile mob)
        {
            return Find(mob, false);
        }

        public static Player Find(Mobile mob, bool inherit)
        {
            PlayerMobile pm = mob as PlayerMobile;

            if (pm == null)
            {
                if (inherit && mob is BaseCreature)
                {
                    BaseCreature bc = mob as BaseCreature;

                    if (bc != null && bc.Controlled)
                        pm = bc.ControlMaster as PlayerMobile;
                    else if (bc != null && bc.Summoned)
                        pm = bc.SummonMaster as PlayerMobile;
                }

                if (pm == null)
                    return null;
            }

            Player pl = pm.EthicPlayer;

            if (pl != null && !pl.Ethic.IsEligible(pl.Mobile))
                pm.EthicPlayer = pl = null;

            return pl;
        }

        private Ethic m_Ethic;
        private Mobile m_Mobile;

        private int m_Power;
        private int m_History;

        private Mobile m_Steed;
        private Mobile m_Familiar;

        private DateTime m_Shield;

        public Ethic Ethic { get { return m_Ethic; } }
        public Mobile Mobile { get { return m_Mobile; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int Power { get { return m_Power; } set { m_Power = value; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int History { get { return m_History; } set { m_History = value; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Mobile Steed { get { return m_Steed; } set { m_Steed = value; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Mobile Familiar { get { return m_Familiar; } set { m_Familiar = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsShielded
        {
            get
            {
                if (m_Shield == DateTime.MinValue)
                    return false;

                if (DateTime.UtcNow < (m_Shield + TimeSpan.FromHours(1.0)))
                    return true;

                FinishShield();
                return false;
            }
        }

        public void BeginShield()
        {
            m_Shield = DateTime.UtcNow;
        }

        public void FinishShield()
        {
            m_Shield = DateTime.MinValue;
        }

        public Player(Ethic ethic, Mobile mobile)
        {
            m_Ethic = ethic;
            m_Mobile = mobile;

            // Old Ethics say: A Hero will start with on sphere 1 and have 10 lifeforce, but also say:
            //	"The more advanced the player, the higher initial values of sphere and lifeforce the evil will start with."
            //	so if you are Evil we will give you 10 + your skill total / 20. For a 7xGM this would be 10 + 35 or 45 life force with a sphere of 4
            //	if you are Hero you will get the standard 10 .. wtf
            // This is Question(3) on the message boards
            m_Power = (Core.OldEthics) ?
                ethic == Ethics.Ethic.Hero ? 10 : 10 + (PlayerMobile.ComputeSkillTotal(mobile) / 20) :
                5;
            m_History = m_Power;
        }

        public void CheckAttach()
        {
            if (m_Ethic.IsEligible(m_Mobile))
                Attach();
        }

        public void Attach()
        {
            if (m_Mobile is PlayerMobile)
                (m_Mobile as PlayerMobile).EthicPlayer = this;

            m_Ethic.Players.Add(this);
        }

        public void Detach()
        {
            if (m_Mobile is PlayerMobile)
                (m_Mobile as PlayerMobile).EthicPlayer = null;

            m_Ethic.Players.Remove(this);
        }

        public Player(Ethic ethic, GenericReader reader)
        {
            m_Ethic = ethic;

            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        m_Mobile = reader.ReadMobile();

                        m_Power = reader.ReadEncodedInt();
                        m_History = reader.ReadEncodedInt();

                        m_Steed = reader.ReadMobile();
                        m_Familiar = reader.ReadMobile();

                        m_Shield = reader.ReadDeltaTime();

                        break;
                    }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_Mobile);

            writer.WriteEncodedInt(m_Power);
            writer.WriteEncodedInt(m_History);

            writer.Write(m_Steed);
            writer.Write(m_Familiar);

            writer.WriteDeltaTime(m_Shield);
        }
    }
}