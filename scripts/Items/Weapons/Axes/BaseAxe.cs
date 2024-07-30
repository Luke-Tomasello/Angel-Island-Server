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

/* Items/Games/Backgammon.cs
 * CHANGELOG:
 *  3/29/23, Yoar
 *      Merge Pub5 moves with RunUO
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	11/14/21, Yoar
 *	    Added IAxe interface.
 */

using Server.Engines.Harvest;
using System.Collections;

namespace Server.Items
{
    public interface IAxe
    {
        bool Axe(Mobile from, BaseAxe axe);
    }

    public abstract class BaseAxe : BaseMeleeWeapon, IUsesRemaining
    {
        public override int DefHitSound { get { return 0x232; } }
        public override int DefMissSound { get { return 0x23A; } }

        public override SkillName DefSkill { get { return SkillName.Swords; } }
        public override WeaponType DefType { get { return WeaponType.Axe; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Slash2H; } }

        public virtual HarvestSystem HarvestSystem { get { return Lumberjacking.System; } }

        private int m_UsesRemaining;
        private bool m_ShowUsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowUsesRemaining
        {
            get { return m_ShowUsesRemaining; }
            set { m_ShowUsesRemaining = value; InvalidateProperties(); }
        }

        public BaseAxe(int itemID)
            : base(itemID)
        {
            m_UsesRemaining = 150;
        }

        public BaseAxe(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (HarvestSystem == null)
                return;

            if (IsChildOf(from.Backpack) || Parent == from)
                HarvestSystem.BeginHarvesting(from, this);
            else
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (HarvestSystem != null)
                BaseHarvestTool.AddContextMenuEntries(from, this, list, HarvestSystem);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((bool)m_ShowUsesRemaining);

            writer.Write((int)m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_ShowUsesRemaining = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_UsesRemaining = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (m_UsesRemaining < 1)
                            m_UsesRemaining = 150;

                        break;
                    }
            }
        }

        public override void OnHit(Mobile attacker, Mobile defender)
        {
            base.OnHit(attacker, defender);
#if false
            // see BaseWeapon
            if (false && DoPub5Move(attacker, defender) && Engines.ConPVP.DuelContext.AllowSpecialAbility(attacker, "Concussion Blow", false))
            {
                StatMod mod = defender.GetStatMod("Concussion");

                if (mod == null)
                {
                    defender.SendMessage("You receive a concussion blow!");
                    defender.AddStatMod(new StatMod(StatType.Int, "Concussion", -(defender.RawInt / 2), TimeSpan.FromSeconds(30.0)));

                    attacker.SendMessage("You deliver a concussion blow!");
                    attacker.PlaySound(0x308);
                }
            }
#endif
        }
    }
}