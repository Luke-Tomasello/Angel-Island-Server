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

/* Scripts\Mobiles\Townfolk\PlayerClone.cs
 * CHANGELOG:
 * 8/5/2024. Adam
 *   First time check in
 */

using Server.Engines.Alignment;
using Server.Factions;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class PlayerClone : BaseTownsFolk
    {
        public static List<PlayerClone> Instances = new();
        private DateTime m_DateCreated;     // (PlayerToCopy.Account as Accounting.Account).Created.ToString()
        private DateTime m_LastLogin;       // (PlayerToCopy.Account as Accounting.Account).LastLogin.ToString()
        private string m_GuildAlignment;    // alignment
        private string m_GuildName;         // guild 
        private string m_GuildAbbr;
        private bool m_Murderer;

        public override bool AlwaysMurderer { get { return m_Murderer; } }

        [Constructable]
        public PlayerClone(DateTime created, DateTime last_login, string guild_alignment, string guild_name, string guild_abbr, bool murderer)
            : base(null)    // title is set by the caller
        {
            m_DateCreated = created;
            m_LastLogin = last_login;
            m_GuildAlignment = guild_alignment;
            m_GuildName = guild_name;
            m_GuildAbbr = guild_abbr;
            m_Murderer = murderer;

            Instances.Add(this);
            BlockDamage = true;
            IsStaffOwned = true;
        }
        public PlayerClone(Serial serial)
            : base(serial)
        {
            Instances.Add(this);
        }
        public override void Delete()
        {
            Instances.Remove(this);
            base.Delete();
        }

        public override void OnDelete()
        {
            base.OnDelete();
        }
        public override void OnDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {
            if (from is Mobile m)
            {
                m.ApplyPoison(this, Poison.Lethal);
                m.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
                m.PlaySound(0x474);
            }
            base.OnDamage(amount, from, willKill, source_weapon);
        }
        public override void OnSingleClick(Mobile from)
        {
            if ((Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()) && AccessLevel == AccessLevel.Player && DisableHiddenSelfClick && Hidden && from == this)
                return;

            // incognito/disguise
            if (NameMod != null)
            {
                int hue;

                if (NameHue != -1)
                    hue = NameHue;
                else
                {
                    int notoriety = Notoriety.Compute(from, this);
                    hue = Notoriety.GetHue(notoriety);

                    //PIX: if they're looking innocent, see if there
                    // are any ill-effects from beneficial actions
                    if (notoriety == Notoriety.Innocent)
                    {
                        int namehue = Notoriety.GetBeneficialHue(from, this);
                        if (namehue != 0)
                        {
                            hue = namehue;
                        }
                    }
                }

                PrivateOverheadMessage(MessageType.Label, hue, AsciiClickMessage, Name, from.NetState);
            }
            else
            {
                if (m_GuildAlignment != null)
                {
                    string text = string.Format("[{0}]", m_GuildAlignment);
                    PrivateOverheadMessage(MessageType.Regular, SpeechHue, true, text, from.NetState);
                }
             
                if(m_GuildName != null)
                {
                    string text = string.Format("[{0}, {1}]", m_GuildName, m_GuildAbbr);
                    PrivateOverheadMessage(MessageType.Regular, SpeechHue, true, text, from.NetState);
                }

                base.OnSingleClick(from);
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            writer.Write(m_DateCreated);
            writer.Write(m_LastLogin);
            writer.Write(m_GuildAlignment);
            writer.Write(m_GuildName);
            writer.Write(m_GuildAbbr);
            writer.Write(m_Murderer);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_DateCreated = reader.ReadDateTime();
                        m_LastLogin = reader.ReadDateTime();
                        m_GuildAlignment = reader.ReadString();
                        m_GuildName = reader.ReadString();
                        m_GuildAbbr = reader.ReadString();
                        m_Murderer = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}
