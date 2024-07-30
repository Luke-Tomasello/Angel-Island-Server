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

/* Engines/Guilds/Guildstone.cs
 * ChangeLog:
 *  4/30/23, Adam
 *      Added a NoResign property. This keeps the player in the guild indefinitely. 
 *  4/15/23, Yoar
 *      Added GuildAlignment, AlignmentState getters/setters
 *	4/25/08, Adam
 *		Add new NewPlayerGuild flag to indicate this guild may be selected from available guilds to auto-add players.
 *		Change from Guild.Peaceful to guild.NewPlayerGuild when deciding auto adding
 *  12/21/07, Pix
 *      Added no counting flag for guilds.
 *	12/6/07, Adam
 *      Add error messages if a GM attempts to switch a guild to Peaceful and it's at War etc.
 *	12/4/07, Adam
 *		Add support for peaceful guilds (no notoriety)
 *  2/8/07, Adam
 *      Add back MoveGuildstoneGump for home owners.
 *      Who took this out?
 *	2/05/07, Adam
 *      - Make use of new MoveItemToIntStorage, and RetrieveItemFromIntStorage for those times when a guild stone is moved.
 *      - remove 'internal' state variable
 *	2/05/07, Pix
 *		Changed to use changed GuildRestorationDeed functionality.
 *	6/14/06, Adam
 *		Exposed the Internal property so that GMs can force a player to drop a held guildstone
 *	6/10/06, Pix
 *		Added IOBAlignment property.
 *	2/10/06, Adam
 *		Redesign the Guildstone 'move' logic so that the stone is no longer converted into a deed, 
 *			but instead carried as a zero weight item on the player. A new deed type is now put in the 
 *			player's backpack that simply extracts the guildstone from the player.
 *		This change was made because of a fundamental incompatibility of the old 'guild stone deed' 
 *			model and the new freeze dry system.
 *	3/12/05, mith
 *		PlaceStone(): Logic copied from GuildstoneDeed.cs to allow stone placement
 *	3/11/05, Adam
 *		Comment out code that allowed anyone to walk into a house the deed a guild stone
 *	3/10/05, mith
 *		Added m_Deed to serialize/deserialize.
 *		Removed context menu, replaced with MoveGuildstoneGump.
 *		Make name dynamic based on whether it's a deed or not ("a guildstone"/"a guildstone deed").
 *	3/9/05, mith
 *		Added GetContextMenuEntries(), OnPrepareMove(), and MoveEntry()
 *			All routines used to re-deed Guildstone by A) Guildmaster B) GameMaster or C) House Owner
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Factions;
using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    public class Guildstone : Item
    {
        private Guild m_Guild;
        public override int LabelNumber { get { return 1041429; } } // a guildstone

        [CommandProperty(AccessLevel.Counselor)]
        public Guild Guild
        {
            get
            {
                return m_Guild;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool GuildWarRing
        {
            get
            {
                if (m_Guild != null)
                {
                    return m_Guild.GuildWarRing;
                }
                else
                {
                    return false;
                }

            }
            set
            {
                if (m_Guild != null)
                    m_Guild.GuildWarRing = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Item TownshipStone
        {
            get
            {
                if (m_Guild != null && m_Guild.TownshipStone != null)
                {
                    return m_Guild.TownshipStone;
                }
                else
                {
                    return null;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public IOBAlignment IOBAlignment
        {
            get
            {
                if (Guild != null)
                {
                    return Guild.IOBAlignment;
                }
                else
                {
                    return IOBAlignment.None;
                }
            }
            set
            {
                if (Guild != null)
                {
                    Guild.IOBAlignment = value;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public bool FixedGuildmaster
        {
            get
            {
                if (Guild != null)
                {
                    return Guild.FixedGuildmaster;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (Guild != null)
                {
                    Guild.FixedGuildmaster = value;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public bool NoResign
        {
            get
            {
                if (Guild != null)
                {
                    return Guild.NoResign;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (Guild != null)
                {
                    Guild.NoResign = value;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public PlayerMobile Guildmaster
        {
            get
            {
                if (Guild != null)
                {
                    return (PlayerMobile)Guild.Leader;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (Guild != null)
                {
                    Guild.Leader = value;
                }
            }
        }

        [CommandProperty(AccessLevel.Administrator, AccessLevel.Counselor)]
        public bool IsNoCountingGuild
        {
            get
            {
                if (m_Guild != null)
                    return m_Guild.IsNoCountingGuild;
                else
                    return false;
            }
            set
            {
                if (m_Guild != null)
                {
                    m_Guild.IsNoCountingGuild = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NewPlayerGuild
        {
            get { return m_Guild.NewPlayerGuild; }
            set { m_Guild.NewPlayerGuild = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Peaceful
        {
            get
            {
                if (m_Guild != null)
                    return m_Guild.Peaceful;
                else
                    return false;
            }
            set
            {
                int errors = 0;

                // make sure this setting is valid and send the GM a message if it is not
                if (value == true && Guild != null)
                {
                    if (m_Guild.IOBAlignment != IOBAlignment.None)
                    {
                        this.SendSystemMessage("You may not create a Peaceful guild that is also aligned.");
                        errors++;
                    }

                    if (m_Guild.GuildWarRing == true)
                    {
                        this.SendSystemMessage("You may not create a Peaceful guild that belongs to the war ring.");
                        errors++;
                    }

                    if (m_Guild.Allies.Count > 0 || m_Guild.AllyDeclarations.Count > 0 || m_Guild.AllyInvitations.Count > 0)
                    {
                        this.SendSystemMessage("You may not create a Peaceful guild that wishes to ally.");
                        errors++;
                    }

                    if (m_Guild.Enemies.Count > 0 || m_Guild.WarDeclarations.Count > 0 || m_Guild.WarInvitations.Count > 0)
                    {
                        this.SendSystemMessage("You may not create a Peaceful guild that wishes to war.");
                        errors++;
                    }
                }

                if (m_Guild != null && errors == 0)
                    m_Guild.Peaceful = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public AlignmentType GuildAlignment
        {
            get { return (m_Guild == null ? AlignmentType.None : m_Guild.Alignment); }
            set
            {
                if (m_Guild != null)
                    m_Guild.Alignment = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AlignmentState AlignmentState
        {
            get { return AlignmentState.GetState(GuildAlignment); }
            set { }
        }

        public Guildstone(Guild g)
            : base(0xED4)
        {
            m_Guild = g;

            Movable = false;
        }

        public Guildstone(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 3;

            writer.Write((int)version);

            if (version < 3)
                writer.Write((bool)false);

            writer.Write(m_Guild);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {   // data removed, skip load in case 2
                        goto case 1;
                    }
                case 2:
                    {
                        bool dmy = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Guild = reader.ReadGuild() as Guild;

                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }

            if (m_Guild == null)
                this.Delete();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Guild != null)
            {
                string name;

                if ((name = m_Guild.Name) == null || (name = name.Trim()).Length <= 0)
                    name = "(unnamed)";

                list.Add(1060802, Utility.FixHtml(name)); // Guild name: ~1_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            string name;

            if (m_Guild == null)
                name = "(unfounded)";
            else if ((name = m_Guild.Name) == null || (name = name.Trim()).Length <= 0)
                name = "(unnamed)";

            this.LabelTo(from, name);
        }

        public override void OnAfterDelete()
        {
            if (m_Guild != null && !m_Guild.Disbanded)
                m_Guild.Disband();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Guild == null || m_Guild.Disbanded && IsStaffOwned == false)
            {
                Delete();
            }
            else if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (m_Guild.Accepted.Contains(from))
            {
                #region Factions
                PlayerState guildState = PlayerState.Find(m_Guild.Leader);
                PlayerState targetState = PlayerState.Find(from);

                Faction guildFaction = (guildState == null ? null : guildState.Faction);
                Faction targetFaction = (targetState == null ? null : targetState.Faction);

                if (guildFaction != targetFaction || (targetState != null && targetState.IsLeaving))
                    return;

                if (guildState != null && targetState != null)
                    targetState.LeaveBegin = guildState.LeaveBegin;
                #endregion

                m_Guild.Accepted.Remove(from);
                m_Guild.AddMember(from);

                GuildGump.EnsureClosed(from);
                from.SendGump(new GuildGump(from, m_Guild));
            }
            else if (from.AccessLevel < AccessLevel.GameMaster && !m_Guild.IsMember(from))
            {
                BaseHouse house = BaseHouse.FindHouseAt(this);

                if (house != null && from.Alive && house.IsOwner(from))
                {
                    GuildGump.EnsureClosed(from);
                    from.SendGump(new MoveGuildstoneGump(from, this));
                }
                else
                    from.Send(new MessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, 501158, "", "")); // You are not a member ...
            }
            else
            {
                GuildGump.EnsureClosed(from);
                from.SendGump(new GuildGump(from, m_Guild));
            }
        }

        public bool PlaceStone(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(from);

            if (house == null)
            {
                from.SendLocalizedMessage(501138); // You can only place a guildstone in a house.
            }
            else if (house.FindGuildstone() != null)
            {
                from.SendLocalizedMessage(501142);//Only one guildstone may reside in a given house.
            }
            else if (!house.IsOwner(from))
            {
                from.SendLocalizedMessage(501141); // You can only place a guildstone in a house you own!
            }
            else
            {
                if (from.Map != Map.Internal)
                {
                    RetrieveItemFromIntStorage(from.Location, from.Map);
                    return true;
                }
                else
                {
                    //should never get here - it's a safety catch.
                    return false;
                }
            }

            return false;
        }

        public void OnPrepareMove(Mobile from)
        {
            if (from is PlayerMobile)
            {   // Adam: FixedGuildmaster denotes a special, usually staff-owned, guild stone. A staff member must move it
                if (!FixedGuildmaster || from.AccessLevel > AccessLevel.Player)
                {
                    // move the stone to storage and place a special deed to recover the stone in the players backpack
                    Item deed = new GuildRestorationDeed(this.m_Guild);
                    if (MoveToIntStorage() && from.Backpack.CheckHold(from, deed, true, false, 0, 0))
                    {
                        from.Backpack.DropItem(deed);
                        from.SendMessage("A guild deed for the {0} has been placed in your backpack.", m_Guild.Name);
                    }
                }
                else
                {
                    from.SendMessage("{0} is staff-owned, and as such, it may not be moved in this way.", m_Guild.Name);
                }
            }
        }
    }
}