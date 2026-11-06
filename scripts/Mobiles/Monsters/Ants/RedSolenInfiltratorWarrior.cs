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

/* ./Scripts/Mobiles/Monsters/Ants/RedSolenInfiltratorWarrior.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
*/

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a solen infiltrator corpse")]
    public class RedSolenInfiltratorWarrior : BaseCreature
    {
        [Constructable]
        public RedSolenInfiltratorWarrior()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a red solen infiltrator";
            Body = 782;
            BaseSoundID = 959;

            SetStr(206, 230);
            SetDex(121, 145);
            SetInt(66, 90);

            SetHits(96, 107);

            SetDamage(5, 15);

            SetSkill(SkillName.MagicResist, 80.0);
            SetSkill(SkillName.Tactics, 80.0);
            SetSkill(SkillName.Wrestling, 80.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 40;
        }

        public override int GetAngerSound()
        {
            return 0xB5;
        }

        public override int GetIdleSound()
        {
            return 0xB5;
        }

        public override int GetAttackSound()
        {
            return 0x289;
        }

        public override int GetHurtSound()
        {
            return 0xBC;
        }

        public override int GetDeathSound()
        {
            return 0xE4;
        }

        public RedSolenInfiltratorWarrior(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=117
            // 200 - 225 Gold. 1-4 Gems
            // Special: 3-13 Zoogi Fungus, Picnic Basket

            if (Spawning)
            {
                PackGold(200, 225);
            }
            else
            {
                PackGem(1, 4);
                PackItem(new ZoogiFungus(Utility.RandomMinMax(3, 13)));

                if (Utility.RandomDouble() < 0.02) // 2%
                    SolenHelper.PackPicnicBasket(this);
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