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

/* ./Scripts/Mobiles/Monsters/Humanoid/Melee/HordeMinion.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 3 lines removed.
*/

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a horde minion corpse")]
    public class HordeMinion : BaseCreature
    {
        [Constructable]
        public HordeMinion()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a horde minion";
            Body = 776;
            BaseSoundID = 357;

            SetStr(16, 40);
            SetDex(31, 60);
            SetInt(11, 25);

            SetHits(10, 24);

            SetDamage(5, 10);

            SetSkill(SkillName.MagicResist, 10.0);
            SetSkill(SkillName.Tactics, 0.1, 15.0);
            SetSkill(SkillName.Wrestling, 25.1, 40.0);

            Fame = 500;
            Karma = -500;

            VirtualArmor = 18;

            AddItem(new LightSource());
        }

        public override int GetIdleSound()
        {
            return 338;
        }

        public override int GetAngerSound()
        {
            return 338;
        }

        public override int GetDeathSound()
        {
            return 338;
        }

        public override int GetAttackSound()
        {
            return 406;
        }

        public override int GetHurtSound()
        {
            return 194;
        }

        public HordeMinion(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackItem(new Bone(3));
                // TODO: Body parts
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // 02 2002 - no page
                    // http://web.archive.org/web/20020806230925/uo.stratics.com/hunters/hordedaemon.shtml
                    // body parts
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                        switch (Utility.Random(6))
                        {
                            case 0: PackItem(new Head()); break;
                            case 1: PackItem(new Torso()); break;
                            case 2: PackItem(new LeftArm()); break;
                            case 3: PackItem(new LeftLeg()); break;
                            case 4: PackItem(new RightArm()); break;
                            case 5: PackItem(new RightLeg()); break;
                        }
                    }
                }
                else
                {
                    PackItem(new Bone(3));
                    // TODO: Body parts
                }
            }
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