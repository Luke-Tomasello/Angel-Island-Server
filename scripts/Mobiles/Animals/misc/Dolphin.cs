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

/* ./Scripts/Mobiles/Animals/Misc/Dolphin.cs
 *	ChangeLog :
 *	5/24/23, Yoar
 *	    Dolphins now jump!
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a dolphin corpse")]
    public class Dolphin : BaseCreature
    {
        [Constructable]
        public Dolphin()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a dolphin";
            Body = 0x97;
            BaseSoundID = 0x8A;

            SetStr(21, 49);
            SetDex(66, 85);
            SetInt(96, 110);

            SetHits(15, 27);

            SetDamage(3, 6);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 19.2, 29.0);
            SetSkill(SkillName.Wrestling, 19.2, 29.0);

            Fame = 500;
            Karma = 2000;

            VirtualArmor = 16;
            CanSwim = true;
            CantWalkLand = true;
        }

        public override int Meat { get { return 1; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                Jump();
        }

        public virtual void Jump()
        {
            if (Utility.RandomBool())
                Animate(3, 16, 1, true, false, 0);
            else
                Animate(4, 20, 1, true, false, 0);
        }

        public override void OnThink()
        {
            if (Utility.RandomDouble() < .005) // slim chance to jump
                Jump();

            base.OnThink();
        }

        public Dolphin(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}