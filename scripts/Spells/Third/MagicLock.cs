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

using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Third
{
    public class MagicLockSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Magic Lock", "An Por",
                SpellCircle.Third,
                215,
                9001,
                Reagent.Garlic,
                Reagent.Bloodmoss,
                Reagent.SulfurousAsh
            );

        public MagicLockSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(LockableContainer targ)
        {
            if (Multis.BaseHouse.CheckLockedDownOrSecured(targ))
            {
                // You cannot cast this on a locked down item.
                Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 501761);
            }
            else if (targ.Locked || targ.LockLevel == 0)
            {
                // Target must be an unlocked chest.
                Caster.SendLocalizedMessage(501762);
            }
            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, targ);

                Point3D loc = targ.GetWorldLocation();

                Effects.SendLocationParticles(
                    EffectItem.Create(loc, targ.Map, EffectItem.DefaultDuration),
                    0x376A, 9, 32, 5020);

                Effects.PlaySound(loc, targ.Map, 0x1FA);

                // The chest is now locked!
                Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501763);

                targ.LockLevel = -255; // signal magic lock
                targ.Locked = true;
                // 5/9/23, Adam: If this is a Dungeon Treasure Chest, start the decay timer, otherwise dungeon pickers
                //  will not be able to access it.
                if (targ is DungeonTreasureChest dtc)
                    dtc.DecayChest(Caster);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private MagicLockSpell m_Owner;

            public InternalTarget(MagicLockSpell owner)
                : base(12, false, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is LockableContainer)
                    m_Owner.Target((LockableContainer)o);
                else
                    from.SendLocalizedMessage(501762); // Target must be an unlocked chest.
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}