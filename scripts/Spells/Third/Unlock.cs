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

/* Change Log
 * 4/1/22, Yoar
 *      Fixed call to m_Owner.Target.
 *      Now correctly passing the object 'o' instead of zero :)
 * 12/9/21, Yoar
 *      Merged with RunUO
 *      Added IMagicUnlockable interface
 * 1/11/05, Darva
 *		Made magic unlock work on all magic locked chests.
 */

using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Third
{
    public interface IMagicUnlockable : ILockpickable
    {
        void OnMagicUnlock(Mobile from);
    }

    public class UnlockSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Unlock Spell", "Ex Por",
                SpellCircle.Third,
                215,
                9001,
                Reagent.Bloodmoss,
                Reagent.SulfurousAsh
            );

        public UnlockSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(object o)
        {
            if (!(o is IPoint3D))
                return;

            IPoint3D loc = (o is Item ? ((Item)o).GetWorldLocation() : (IPoint3D)o);

            if (CheckSequence())
            {
                SpellHelper.Turn(Caster, o);

                Effects.SendLocationParticles(EffectItem.Create(new Point3D(loc), Caster.Map, EffectItem.DefaultDuration), 0x376A, 9, 32, 5024);

                Effects.PlaySound(loc, Caster.Map, 0x1FF);

                if (o is Mobile)
                    Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503101); // That did not need to be unlocked.
                else if (!(o is IMagicUnlockable))
                    Caster.SendLocalizedMessage(501666); // You can't unlock that!
                else
                {
                    IMagicUnlockable targ = (IMagicUnlockable)o;

                    if (targ is Item && Multis.BaseHouse.CheckSecured((Item)targ))
                        Caster.SendLocalizedMessage(503098); // You cannot cast this on a secure item.
                    else if (!targ.Locked)
                        Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503101); // That did not need to be unlocked.
                    else if (targ.LockLevel == 0)
                        Caster.SendLocalizedMessage(501666); // You can't unlock that!
                    else
                    {
                        int level = (int)(Caster.Skills[SkillName.Magery].Value * 0.8) - 4;

#if !RunUO
                        // AI Custom: 100% success chance to unlock magically locked target
                        if (targ.LockLevel == -255)
                        {
                            targ.Locked = false;
                        }
                        else if (level >= targ.RequiredSkill && !(targ is TreasureMapChest && ((TreasureMapChest)targ).Level > 2))
#else
                        if (level >= targ.RequiredSkill && !(targ is TreasureMapChest && ((TreasureMapChest)targ).Level > 2))
#endif
                        {
                            targ.Locked = false;

#if RunUO
                            if (targ.LockLevel == -255)
                                targ.LockLevel = targ.RequiredSkill - 10;
#else
                            // AI Custom: If, somehow, the target became un-lockpickable, make it lockpickable again
                            if (targ.LockLevel == 0)
                                targ.LockLevel = -1;
#endif

                            targ.OnMagicUnlock(Caster);
                        }
                        else
                            Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503099); // My spell does not seem to have an effect on that lock.
                    }
                }
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private UnlockSpell m_Owner;

            public InternalTarget(UnlockSpell owner)
                : base(12, false, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                m_Owner.Target(o);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}