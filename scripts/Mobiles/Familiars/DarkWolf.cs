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

/* ./Scripts/Mobiles/Familiars/DarkWolf.cs
 *	ChangeLog :
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
*/

using System;

namespace Server.Mobiles
{
    [CorpseName("a dark wolf corpse")]
    public class DarkWolfFamiliar : BaseFamiliar
    {
        public DarkWolfFamiliar()
        {
            Name = "a dark wolf";
            Body = 99;
            Hue = 0x901;
            BaseSoundID = 0xE5;

            SetStr(100);
            SetDex(90);
            SetInt(90);

            SetHits(60);
            SetStam(90);
            SetMana(0);

            SetDamage(5, 10);

            SetSkill(SkillName.Wrestling, 85.1, 90.0);
            SetSkill(SkillName.Tactics, 50.0);

            ControlSlots = 1;
        }

        private DateTime m_NextRestore;

        public override void OnThink()
        {
            base.OnThink();

            if (DateTime.UtcNow < m_NextRestore)
                return;

            m_NextRestore = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);

            Mobile caster = ControlMaster;

            if (caster == null)
                caster = SummonMaster;

            if (caster != null)
                ++caster.Stam;
        }

        public DarkWolfFamiliar(Serial serial)
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