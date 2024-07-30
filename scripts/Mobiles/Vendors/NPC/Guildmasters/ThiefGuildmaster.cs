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

/* Scripts/Mobiles/Vendors/NPC/Guildmasters/ThiefGuildmaster.cs
 * ChangeLog
 *	5/26/04, mith
 *		Changed character age requirement from 7 days to 40 hours.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class ThiefGuildmaster : BaseGuildmaster
    {
        public override NpcGuild NpcGuild { get { return NpcGuild.ThievesGuild; } }

        public override TimeSpan JoinAge
        {
            get
            {
                //on siege, there is no age requirement to join the thieves guild
                if (Core.RuleSets.SiegeStyleRules()) { return TimeSpan.FromDays(0.0); }

                return TimeSpan.FromHours(40.0);
            }
        }

        [Constructable]
        public ThiefGuildmaster()
            : base("thief")
        {
            SetSkill(SkillName.DetectHidden, 75.0, 98.0);
            SetSkill(SkillName.Hiding, 65.0, 88.0);
            SetSkill(SkillName.Lockpicking, 85.0, 100.0);
            SetSkill(SkillName.Snooping, 90.0, 100.0);
            SetSkill(SkillName.Poisoning, 60.0, 83.0);
            SetSkill(SkillName.Stealing, 90.0, 100.0);
            SetSkill(SkillName.Fencing, 75.0, 98.0);
            SetSkill(SkillName.Stealth, 85.0, 100.0);
            SetSkill(SkillName.RemoveTrap, 85.0, 100.0);
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            if (Utility.RandomBool())
                AddItem(new Server.Items.Kryss());
            else
                AddItem(new Server.Items.Dagger());
        }

        public override bool CheckCustomReqs(PlayerMobile pm)
        {
            //no requirements on siege to join the theives guild
            if (Core.RuleSets.SiegeStyleRules()) return true;

            if (pm.LongTermMurders > 0)
            {
                SayTo(pm, 501050); // This guild is for cunning thieves, not oafish cutthroats.
                return false;
            }
            else if (pm.Skills[SkillName.Stealing].Base < 60.0)
            {
                SayTo(pm, 501051); // You must be at least a journeyman pickpocket to join this elite organization.
                return false;
            }

            return true;
        }

        public override void SayWelcomeTo(Mobile m)
        {
            SayTo(m, 1008053); // Welcome to the guild! Stay to the shadows, friend.
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

            if (!e.Handled && from is PlayerMobile && from.InRange(this.Location, 2) && e.HasKeyword(0x1F)) // *disguise*
            {
                PlayerMobile pm = (PlayerMobile)from;

                if (pm.NpcGuild == NpcGuild.ThievesGuild)
                    SayTo(from, 501839); // That particular item costs 700 gold pieces.
                else
                    SayTo(from, 501838); // I don't know what you're talking about.

                e.Handled = true;
            }

            base.OnSpeech(e);
        }

        public override bool OnGoldGiven(Mobile from, Gold dropped)
        {
            if (from is PlayerMobile && dropped.Amount == 700)
            {
                PlayerMobile pm = (PlayerMobile)from;

                if (pm.NpcGuild == NpcGuild.ThievesGuild)
                {
                    from.AddToBackpack(new DisguiseKit());

                    dropped.Delete();
                    return true;
                }
            }

            return base.OnGoldGiven(from, dropped);
        }

        public ThiefGuildmaster(Serial serial)
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