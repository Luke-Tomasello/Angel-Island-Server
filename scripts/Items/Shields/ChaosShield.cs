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

/* Items/Shields/ChaosShield.cs
 * CHANGELOG:
 *  7/11/23, Yoar
 *      Removed STR requirement from Order/Chaos shields.
 *      https://web.archive.org/web/20010721225156if_/http://uo.stratics.com:80/
 *      
 *      In Pub21, Order/Chaos shields became craftable and lost their special functionality. Let's
 *      use this publish to condition the STR requirement. We'll also use the AutoPoof property to
 *      check whether this is supposed to be a special Order/Chaos shield or just an ordinary
 *      shield.
 *      https://www.uoguide.com/Publish_21
 *  4/15/23, Yoar
 *      Order/Chaos shields are now also usable by Order/Chaos guild-aligned players.
 *  1/21/22, Adam
 *      We now drop these chaos/order shields as rares from the harrower.
 *      As rares, we also set AutoPoof false and instead display a message that they must join a so aligned guild.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Guilds;

namespace Server.Items
{
    public class ChaosShield : BaseShield, IAlignmentItem
    {
        #region Alignment

        AlignmentType IAlignmentItem.GuildAlignment
        {
            get { return AlignmentType.Chaos; }
        }

        bool IAlignmentItem.EquipRestricted
        {
            get { return true; }
        }

        #endregion

        public override int InitMinHits { get { return 100; } }
        public override int InitMaxHits { get { return 125; } }

        public override int ShieldStrReq { get { return ((PublishInfo.Publish < 21 && m_autoPoof) ? 0 : 95); } }

        public override int ShieldDexReq { get { return 0; } }
        public override int ShieldIntReq { get { return 0; } }
        public override int ArmorBase { get { return 32; } }

        private bool m_autoPoof = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AutoPoof
        {
            get { return m_autoPoof; }
            set { m_autoPoof = value; }
        }

        [Constructable]
        public ChaosShield()
            : base(0x1BC3)
        {
            if (!Core.RuleSets.AOSRules())
                LootType = LootType.Newbied;

            Weight = 5.0;
        }

        public ChaosShield(Serial serial)
            : base(serial)
        {
        }

        // old name removed, see base class

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        m_autoPoof = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    {
                        writer.Write(m_autoPoof);
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }

        public override bool OnEquip(Mobile from)
        {
            return Validate(from) && base.OnEquip(from);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (Validate(Parent as Mobile))
                base.OnSingleClick(from);
        }

        public bool Validate(Mobile m)
        {
            if (!Guild.OrderChaosEnabled || m == null || !m.Player || m.AccessLevel != AccessLevel.Player || Core.RuleSets.AOSRules())
                return true;

            Guild g = m.Guild as Guild;

            if (g == null || g.Type != GuildType.Chaos)
            {
                if (m_autoPoof)
                {
                    m.FixedEffect(0x3728, 10, 13);
                    Delete();
                }
                else
                {
                    m.SendMessage("Sign up with a guild of chaos if thou art interested in equipping this shield.");
                }

                return false;
            }

            return true;
        }
    }
}