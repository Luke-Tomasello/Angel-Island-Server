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

/* Scripts/Mobiles/Animals/Mounts/SeaHorse.cs
 * ChangeLog
 *	5/15/23, Yoar
 *	    Added ability to swim
 *	    Added BaseSoundID
 */

namespace Server.Mobiles
{
    [CorpseName("a sea horse corpse")]
    public class SeaHorse : BaseMount
    {
        [Constructable]
        public SeaHorse()
            : this("a sea horse")
        {
        }

        [Constructable]
        public SeaHorse(string name)
            : base(name, 0x90, 0x3EB3, AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4)
        {
            BaseSoundID = 0x478;

            InitStats(Utility.Random(50, 30), Utility.Random(50, 30), 10);
            Skills[SkillName.MagicResist].Base = 25.0 + (Utility.RandomDouble() * 5.0);
            Skills[SkillName.Wrestling].Base = 35.0 + (Utility.RandomDouble() * 10.0);
            Skills[SkillName.Tactics].Base = 30.0 + (Utility.RandomDouble() * 15.0);

            CantWalkLand = true;
            CanSwim = true;
        }

        public SeaHorse(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1)
            {
                CantWalkLand = true;
                CanSwim = true;
            }
        }
    }
}