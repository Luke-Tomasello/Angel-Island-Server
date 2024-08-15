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

/* Items/Body Parts/Head.cs
 * CHANGELOG:
 *  5/29/23, Yoar
 *      Added GuildName, GuildAbbreviation props
 *  5/22/23, Yoar
 *      Added GuildAlignment
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *  3/14/22, Yoar
 *      Cleanups.
 *	3/6/0, Adam,
 *		Add a notion of FriendlyFire so that we can track when a player was killed by a shared account
 *		(so we can deny a bounty)
 *  01/20/06 Taran Kain
 *		Changed cast in loading PlayerMobile to make sure we're not creating an invalid cast
 *	5/16/04, Pixie
 *		Head now contains information for Bounty system.
 */

using Server.Engines.Alignment;
using Server.Mobiles;
using System;

namespace Server.Items
{
    public enum HeadType : byte
    {
        Regular,
        Duel,
        Tournament
    }

    public class Head : Item, ICarvable
    {
        public override TimeSpan DecayTime { get { return TimeSpan.FromMinutes(15.0); } }

        private PlayerMobile m_Player;
        private bool m_FriendlyFire;
        private HeadType m_HeadType;
        private AlignmentType m_GuildAlignment;

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Player
        {
            get { return m_Player; }
            set { m_Player = value; RecalculateName(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildName
        {
            get { return (m_Player == null ? null : m_Player.GuildName); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildAbbreviation
        {
            get { return (m_Player == null ? null : m_Player.GuildAbbreviation); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsPlayerHead
        {
            get { return (m_Player != null); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool FriendlyFire
        {
            get { return m_FriendlyFire; }
            set { m_FriendlyFire = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HeadType HeadType
        {
            get { return m_HeadType; }
            set { m_HeadType = value; RecalculateName(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AlignmentType GuildAlignment
        {
            get { return m_GuildAlignment; }
            set { m_GuildAlignment = value; RecalculateName(); }
        }

        [Constructable]
        public Head()
            : this(null, null, HeadType.Regular)
        {
        }

        [Constructable]
        public Head(string playerName)
            : this(playerName, null, HeadType.Regular)
        {
        }

        public Head(string playerName, PlayerMobile player)
            : this(playerName, player, HeadType.Regular)
        {
        }

        public Head(string playerName, PlayerMobile player, HeadType headType)
            : base(0x1DA0)
        {
            Weight = 1.0;
            m_Player = player;
            m_FriendlyFire = (player != null && player.Corpse is Corpse && ((Corpse)player.Corpse).FriendlyFire); // were we killed by a shared account?
            m_HeadType = headType;
            m_GuildAlignment = AlignmentSystem.Find(player);
            RecalculateName(playerName);
        }

        public Head(Serial serial)
            : base(serial)
        {
        }

        private void RecalculateName()
        {
            RecalculateName(m_Player?.Name);
        }

        private void RecalculateName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                Name = null;
            }
            else
            {
                string format;

                switch (m_HeadType)
                {
                    default:
                    case HeadType.Regular: format = "the head of {0}"; break;
                    case HeadType.Duel: format = "the head of {0}, taken in a duel"; break;
                    case HeadType.Tournament: format = "the head of {0}, taken in a tournament"; break;
                }

                Name = string.Format(format, playerName);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)5); // version

            // version 5
            writer.Write((byte)m_GuildAlignment);

            // version 4
            writer.Write((byte)m_HeadType);

            // version 2
            writer.Write((bool)m_FriendlyFire);

            // version 1
            writer.Write((Mobile)m_Player);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 5:
                    {
                        m_GuildAlignment = (AlignmentType)reader.ReadByte();
                        goto case 4;
                    }
                case 4:
                    {
                        m_HeadType = (HeadType)reader.ReadByte();
                        goto case 3;
                    }
                case 3:
                case 2:
                    {
                        m_FriendlyFire = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        if (version >= 3 || reader.ReadInt() == 1)
                        {
                            // don't want to use C-style hard cast here, as ReadMobile can return a valid Mobile that's not a PlayerMobile
                            // as cast will just make it null, in that case
                            // Note from adam: It's also possible that it's a PM that's loaded, but a *different* PM.. don't know how to handle this atm. Not critical.
                            m_Player = reader.ReadMobile() as PlayerMobile;
                        }

                        if (version < 3)
                        {
                            // Yoar: we don't need to have a separate creation date, simply use the one defined in Item!
                            reader.ReadDateTime();
                        }

                        break;
                    }
            }

            if (version < 3 && this.Weight == 2.0)
                this.Weight = 1.0; // fix weight
        }

        #region ICarvable Members

        void ICarvable.Carve(Mobile from, Item item)
        {
            Point3D loc = this.Location;
            if (this.ParentContainer != null)
            {
                if (this.ParentMobile != null)
                {
                    if (this.ParentMobile != from)
                    {
                        from.SendMessage("You can't carve that there");
                        return;
                    }

                    loc = this.ParentMobile.Location;
                }
                else
                {
                    loc = this.ParentContainer.Location;
                    if (!from.InRange(loc, 1))
                    {
                        from.SendMessage("That is too far away.");
                        return;
                    }
                }
            }

            //add blood
            Blood blood = new Blood(Utility.Random(0x122A, 5), Utility.Random(15 * 60, 5 * 60));
            blood.MoveToWorld(loc, Map);
            //add brain			//add skull
            if (Player == null)
            {
                if (this.ParentContainer == null)
                {
                    new Brain().MoveToWorld(loc, Map);
                    new Skull().MoveToWorld(loc, Map);
                }
                else
                {
                    this.ParentContainer.DropItem(new Brain());
                    this.ParentContainer.DropItem(new Skull());
                }
            }
            else
            {
                if (this.ParentContainer == null)
                {
                    new Brain(Player.Name).MoveToWorld(loc, Map);
                    new Skull(Player.Name).MoveToWorld(loc, Map);
                }
                else
                {
                    this.ParentContainer.DropItem(new Brain(Player.Name));
                    this.ParentContainer.DropItem(new Skull(Player.Name));
                }
            }

            this.Delete();
        }

        #endregion
    }
}