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

/* Scripts/Gumps/Guilds/RecruitTarget.cs
 * Changelog:
 *	1/13/09, Adam
 *		Allow CertificateOfIdentity to be used as a valid player proxy
 *	4/28/06, Pix
 *		Changes for Kin alignment by guild.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Factions;
using Server.Guilds;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Gumps
{
    public class GuildRecruitTarget : Target
    {
        private Mobile m_Mobile;
        private Guild m_Guild;

        public GuildRecruitTarget(Mobile m, Guild guild)
            : base(10, false, TargetFlags.None)
        {
            m_Mobile = m;
            m_Guild = guild;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (GuildGump.BadMember(m_Mobile, m_Guild))
                return;

            if (targeted is Mobile || targeted is Items.CertificateOfIdentity)
            {
                Mobile m = targeted as Mobile;

                PlayerState guildState = PlayerState.Find(m_Guild.Leader);
                PlayerState targetState = PlayerState.Find(m);

                Faction guildFaction = (guildState == null ? null : guildState.Faction);
                Faction targetFaction = (targetState == null ? null : targetState.Faction);

                PlayerMobile pm = targeted as PlayerMobile;
                Items.CertificateOfIdentity coi = null;

                if (targeted is Items.CertificateOfIdentity)
                {
                    coi = targeted as Items.CertificateOfIdentity;
                    if (coi.Mobile != null && coi.Mobile.Deleted == false)
                    {   // reassign
                        m = coi.Mobile as Mobile;
                        pm = coi.Mobile as PlayerMobile;
                    }
                }

                if (coi != null && (coi.Mobile == null || coi.Mobile.Deleted))
                {
                    from.SendMessage("That identity certificate does not represent a player.");
                }
                else if (!m.Player)
                {
                    m_Mobile.SendLocalizedMessage(501161); // You may only recruit players into the guild.
                }
                else if (!m.Alive)
                {
                    m_Mobile.SendLocalizedMessage(501162); // Only the living may be recruited.
                }
                else if (m_Guild.IsBannedMember(m) != Guild.GuildBanType.None)
                {
                    from.SendMessage("That member may not be recruited at this time. They were banned.");
                }
                else if (m_Guild.IsMember(m))
                {
                    m_Mobile.SendLocalizedMessage(501163); // They are already a guildmember!
                }
                #region Kin
                else if (pm != null && !(pm.IOBAlignment == IOBAlignment.None || pm.IOBAlignment == m_Guild.IOBAlignment))
                {
                    m_Mobile.SendMessage("Only non-aligned or same-aligned can be recruited.");
                }
                #endregion
                else if (m_Guild.Candidates.Contains(m))
                {
                    m_Mobile.SendLocalizedMessage(501164); // They are already a candidate.
                }
                else if (m_Guild.Accepted.Contains(m))
                {
                    m_Mobile.SendLocalizedMessage(501165); // They have already been accepted for membership, and merely need to use the Guildstone to gain full membership.
                }
                else if (m.Guild != null)
                {
                    m_Mobile.SendLocalizedMessage(501166); // You can only recruit candidates who are not already in a guild.
                }
                #region Factions
                else if (guildFaction != targetFaction)
                {
                    if (guildFaction == null)
                        m_Mobile.SendLocalizedMessage(1013027); // That player cannot join a non-faction guild.
                    else if (targetFaction == null)
                        m_Mobile.SendLocalizedMessage(1013026); // That player must be in a faction before joining this guild.
                    else
                        m_Mobile.SendLocalizedMessage(1013028); // That person has a different faction affiliation.
                }
                else if (targetState != null && targetState.IsLeaving)
                {
                    // OSI does this quite strangely, so we'll just do it this way
                    m_Mobile.SendMessage("That person is quitting their faction and so you may not recruit them.");
                }
                #endregion
                else if (m_Mobile.AccessLevel >= AccessLevel.GameMaster || m_Guild.Leader == m_Mobile)
                {
                    m_Guild.Accepted.Add(m);
                }
                else
                {
                    m_Guild.Candidates.Add(m);
                }
            }
        }

        protected override void OnTargetFinish(Mobile from)
        {
            if (GuildGump.BadMember(m_Mobile, m_Guild))
                return;

            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildGump(m_Mobile, m_Guild));
        }
    }
}