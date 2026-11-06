/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts/Mobiles/Monsters/Mammal/Melee/FluffyBunny.cs
 * ChangeLog
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 1 lines removed.
 *	First time checkin
 */

using System;

namespace Server.Mobiles
{
    [CorpseName("a fluffy bunny corpse")]
    public class FluffyBunny : BaseCreature
    {
        [Constructable]
        public FluffyBunny()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a fluffy bunny";
            Body = 205;
            Hue = 0x481;
            BardImmune = true;

            SetStr(50, 60);
            SetDex(2000);
            SetInt(1000);

            SetHits(1800); // 2000
            SetStam(500);
            SetMana(0);

            SetDamage(8, 11); // 11, 17?


            SetSkill(SkillName.MagicResist, 200.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 1000;
            Karma = 0;

            VirtualArmor = 4;

            DelayBeginTunnel();
        }

        public class BunnyHole : Item
        {
            public BunnyHole()
                : base(0x913)
            {
                Movable = false;
                Hue = 1;
                Name = "a mysterious rabbit hole";

                Timer.DelayCall(TimeSpan.FromSeconds(40.0), new TimerCallback(Delete));
            }

            public BunnyHole(Serial serial)
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

                Delete();
            }
        }

        public virtual void DelayBeginTunnel()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(3.5), new TimerCallback(BeginTunnel));
        }

        public virtual void BeginTunnel()
        {
            if (Deleted)
                return;

            new BunnyHole().MoveToWorld(Location, Map);

            Frozen = true;
            Say("* The bunny begins to dig a tunnel back to its underground lair *");
            PlaySound(0x247);

            Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(Delete));
        }

        public override int Meat { get { return 1; } }
        public override MeatType MeatType { get { return MeatType.Ribs; } }
        public override int Hides { get { return 0; } }

        public FluffyBunny(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=21
            // 0 Gold
            // Special: None
            // Cut Up: 1 Raw Rib

            if (Spawning)
            {
                // No gold
            }
            else
            {
                // No loot
            }
        }

        public override int GetAttackSound()
        {
            return 0xC9;
        }

        public override int GetHurtSound()
        {
            return 0xCA;
        }

        public override int GetDeathSound()
        {
            return 0xCB;
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

            DelayBeginTunnel();
        }
    }
}