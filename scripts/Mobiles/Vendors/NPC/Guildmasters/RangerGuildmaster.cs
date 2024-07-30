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

namespace Server.Mobiles
{
    public class RangerGuildmaster : BaseGuildmaster
    {
        public override NpcGuild NpcGuild { get { return NpcGuild.RangersGuild; } }

        [Constructable]
        public RangerGuildmaster()
            : base("ranger")
        {
            SetSkill(SkillName.AnimalLore, 64.0, 100.0);
            SetSkill(SkillName.Camping, 75.0, 98.0);
            SetSkill(SkillName.Hiding, 75.0, 98.0);
            SetSkill(SkillName.MagicResist, 75.0, 98.0);
            SetSkill(SkillName.Tactics, 65.0, 88.0);
            SetSkill(SkillName.Archery, 90.0, 100.0);
            SetSkill(SkillName.Tracking, 90.0, 100.0);
            SetSkill(SkillName.Stealth, 60.0, 83.0);
            SetSkill(SkillName.Fencing, 36.0, 68.0);
            SetSkill(SkillName.Herding, 36.0, 68.0);
            SetSkill(SkillName.Swords, 45.0, 68.0);
        }

        public RangerGuildmaster(Serial serial)
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