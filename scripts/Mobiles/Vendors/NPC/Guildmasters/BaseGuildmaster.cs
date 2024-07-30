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

/* Scripts/Mobiles/Vendors/NPC/Guildmasters/BaseGuildmaster.cs
 * ChangeLog
 *	1/22/08, Adam
 *		Add callback to the PlayerMobile to handle Join and Resign
 *	5/26/04, mith
 *		Changed JoinGameAge from 2 days to 0.
 */

using Server.Items;
using Server.Network;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public abstract class BaseGuildmaster : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override bool IsActiveVendor { get { return false; } }

        public override bool ClickTitle { get { return false; } }

        public virtual int JoinCost { get { return 500; } }

        public virtual TimeSpan JoinAge { get { return TimeSpan.FromDays(0.0); } }
        public virtual TimeSpan JoinGameAge { get { return TimeSpan.FromDays(0.0); } }
        public virtual TimeSpan QuitAge { get { return TimeSpan.FromDays(7.0); } }
        public virtual TimeSpan QuitGameAge { get { return TimeSpan.FromDays(4.0); } }

        public override void InitSBInfo()
        {
        }

        public virtual bool CheckCustomReqs(PlayerMobile pm)
        {
            return true;
        }

        public virtual void SayGuildTo(Mobile m)
        {
            SayTo(m, 1008055 + (int)NpcGuild);
        }

        public virtual void SayWelcomeTo(Mobile m)
        {
            SayTo(m, 1008054); // Welcome to the guild! Thou shalt find that fellow members shall grant thee lower prices in shops.
        }

        public virtual void SayPriceTo(Mobile m)
        {
            m.Send(new MessageLocalizedAffix(Serial, Body, MessageType.Regular, SpeechHue, 3, 1008052, Name, AffixType.Append, JoinCost.ToString(), ""));
        }

        public virtual bool WasNamed(string speech)
        {
            string name = this.Name;

            return (name != null && Insensitive.StartsWith(speech, name));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 2))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!e.Handled && from is PlayerMobile && from.InRange(this.Location, 2) && WasNamed(e.Speech))
            {
                PlayerMobile pm = (PlayerMobile)from;

                if (e.HasKeyword(0x0004)) // *join* | *member*
                {
                    if (pm.NpcGuild == this.NpcGuild)
                        SayTo(from, 501047); // Thou art already a member of our guild.
                    else if (pm.NpcGuild != NpcGuild.None)
                        SayTo(from, 501046); // Thou must resign from thy other guild first.
                    // Although you must still be in the Thieves Guild to steal from players, you will not be removed from the guild for collecting a murder count, nor are there any skill or time requirements to meet before joining.
                    // https://www.uoguide.com/Siege_Perilous
                    else if (!Core.RuleSets.SiegeStyleRules() && (pm.GameTime < JoinGameAge || (pm.Created + JoinAge) > DateTime.UtcNow))
                        SayTo(from, 501048); // You are too young to join my guild...
                    else if (CheckCustomReqs(pm))
                        SayPriceTo(from);

                    e.Handled = true;
                }
                else if (e.HasKeyword(0x0005)) // *resign* | *quit*
                {
                    if (pm.NpcGuild != this.NpcGuild)
                    {
                        SayTo(from, 501052); // Thou dost not belong to my guild!
                    }
                    else if ((pm.NpcGuildJoinTime + QuitAge) > DateTime.UtcNow || (pm.NpcGuildGameTime + QuitGameAge) > pm.GameTime)
                    {
                        SayTo(from, 501053); // You just joined my guild! You must wait a week to resign.
                    }
                    else
                    {
                        SayTo(from, 501054);  // I accept thy resignation.
                        pm.OnNpcGuildResign();  // setup the membership
                    }

                    e.Handled = true;
                }
            }

            base.OnSpeech(e);
        }

        public override bool OnGoldGiven(Mobile from, Gold dropped)
        {
            if (from is PlayerMobile && dropped.Amount == JoinCost)
            {
                PlayerMobile pm = (PlayerMobile)from;

                if (pm.NpcGuild == this.NpcGuild)
                {
                    SayTo(from, 501047); // Thou art already a member of our guild.
                }
                else if (pm.NpcGuild != NpcGuild.None)
                {
                    SayTo(from, 501046); // Thou must resign from thy other guild first.
                }
                else if (pm.GameTime < JoinGameAge || (pm.Created + JoinAge) > DateTime.UtcNow)
                {
                    SayTo(from, 501048); // You are too young to join my guild...
                }
                else if (CheckCustomReqs(pm))
                {
                    SayWelcomeTo(from);
                    pm.OnNpcGuildJoin(this.NpcGuild);
                    dropped.Delete();
                    return true;
                }

                return false;
            }

            return base.OnGoldGiven(from, dropped);
        }

        public BaseGuildmaster(string title)
            : base(title)
        {
            Title = String.Format("the {0} {1}", title, Female ? "guildmistress" : "guildmaster");
        }

        public BaseGuildmaster(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}