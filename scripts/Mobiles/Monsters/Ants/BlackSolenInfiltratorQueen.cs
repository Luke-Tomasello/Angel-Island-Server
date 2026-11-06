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

/* ./Scripts/Mobiles/Monsters/Ants/BlackSolenInfiltratorQueen.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
*/

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a solen infiltrator corpse")] // TODO: Corpse name?
    public class BlackSolenInfiltratorQueen : BaseCreature
    {
        [Constructable]
        public BlackSolenInfiltratorQueen()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a black solen infiltrator";
            Body = 807;
            BaseSoundID = 959;
            Hue = 0x453;

            SetStr(326, 350);
            SetDex(141, 165);
            SetInt(96, 120);

            SetHits(151, 162);

            SetDamage(10, 15);

            SetSkill(SkillName.MagicResist, 90.0);
            SetSkill(SkillName.Tactics, 90.0);
            SetSkill(SkillName.Wrestling, 90.0);

            Fame = 6500;
            Karma = -6500;

            VirtualArmor = 50;
        }

        public override int GetAngerSound()
        {
            return 0x259;
        }

        public override int GetIdleSound()
        {
            return 0x259;
        }

        public override int GetAttackSound()
        {
            return 0x195;
        }

        public override int GetHurtSound()
        {
            return 0x250;
        }

        public override int GetDeathSound()
        {
            return 0x25B;
        }

        public BlackSolenInfiltratorQueen(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=112&utm_source=chatgpt.com
            // 250 - 300 Gold. Special: 4-16 Zoogi Fungus, Picnic Basket
            if (Spawning)
            {
                PackGold(250, 300);
            }
            else
            {
                PackItem(new ZoogiFungus(Utility.RandomMinMax(4, 16)));
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