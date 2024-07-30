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

/* Scripts\Spells\Seventh\MassDispel.cs
 * ChangeLog:
 * 7/16/10, adam
 *		o call new OnBeforeDispel() to allow dispelled creatures to do something.
 *		Spirit Speak bonus:
 *			You gain up to 50pts extra dispel protection against npcs, however, it'll never exceed the 
 *				difficulty of dispelling a demon.  
 *			Everything will have at least a 22% chance to be dispelled by an NPC.  
 *			Players are unaffected by this bonus.
 *			Dispel Difficulty Adjustment (NPCs Only) = Difficulty + (SpiritSpeak/2).   
 *			If difficulty > 125, difficulty = 125.  
 *  7/23/06, Kit
 *		made targeted version work on genies.
 *  6/01/05, Kit
 *		Increased targeted mass dispell chance to 99% vs anything else besides demons which is now 80%
 *		made internal target public for AI targeting purposes
 *	4/27/05, Kit
 *		changed to if single mobile is targeted acts as a greater dispell(+25% percent chance of dispelling)
 *		if area is targeted acts per normal with area effect
 *	4/26/04, Adam
 *		Fixed dispelChance. Was backwards
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
*/

using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System.Collections;

namespace Server.Spells.Seventh
{
    public class MassDispelSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Mass Dispel", "Vas An Ort",
                SpellCircle.Seventh,
                263,
                9002,
                Reagent.Garlic,
                Reagent.MandrakeRoot,
                Reagent.BlackPearl,
                Reagent.SulfurousAsh
            );

        public MassDispelSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(object target)
        {

            if (!Caster.CanSee(target))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }

            bool singlemob = false;

            BaseCreature to;

            IPoint3D p = target as IPoint3D;
            if (target == null)
                return;

            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                SpellHelper.GetSurfaceTop(ref p);

                ArrayList targets = new ArrayList();

                Map map = Caster.Map;

                if (target is BaseCreature && (((BaseCreature)target).Summoned || target is Genie) && !((BaseCreature)target).IsAnimatedDead && Caster.CanBeHarmful((Mobile)target, false))
                {
                    to = (BaseCreature)p;
                    singlemob = true;
                    double dChance;

                    // players don't have trouble dispelling the summons of a summoner (magery+spirit speak)
                    if (Caster is PlayerMobile || to.ControlMaster == null)
                        dChance = (108 + ((100 * (Caster.Skills.Magery.Value - to.DispelDifficulty)) / (to.DispelFocus * 2))) / 100;
                    else
                    {
                        double difficulty = to.DispelDifficulty + to.ControlMaster.Skills.SpiritSpeak.Value / 2.0;
                        if (difficulty > 125) difficulty = 125;
                        dChance = (108 + ((100 * (Caster.Skills.Magery.Value - difficulty)) / (to.DispelFocus * 2))) / 100;
                    }

                    if (dChance > 0.99) dChance = 0.99;
                    //Console.WriteLine(dChance);

                    if (dChance > Utility.RandomDouble())
                    {
                        Effects.SendLocationParticles(EffectItem.Create(to.Location, to.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
                        Effects.PlaySound(to, to.Map, 0x201);
                        to.OnBeforeDispel(Caster);
                        to.Delete();
                    }
                    else
                    {
                        to.FixedEffect(0x3779, 10, 20);
                        Caster.SendLocalizedMessage(1010084); // The creature resisted the attempt to dispel it!
                        Caster.DoHarmful(to);					// and now he's pissed at you
                    }
                    FinishSequence();
                }

                if (map != null && singlemob == false)
                {
                    IPooledEnumerable eable = map.GetMobilesInRange(new Point3D(p), 8);

                    foreach (Mobile m in eable)
                    {
                        if (m is BaseCreature && ((BaseCreature)m).Summoned && !((BaseCreature)m).IsAnimatedDead && Caster.CanBeHarmful(m, false))
                            targets.Add(m);
                    }

                    eable.Free();
                }

                for (int i = 0; i < targets.Count; ++i)
                {
                    Mobile m = (Mobile)targets[i];

                    BaseCreature bc = m as BaseCreature;

                    if (bc == null)
                        continue;

                    double dispelChance;

                    // players don't have trouble dispelling the summons of a summoner (magery+spirit speak)
                    if (Caster is PlayerMobile || bc.ControlMaster == null)
                        dispelChance = (50.0 + ((100 * (Caster.Skills.Magery.Value - bc.DispelDifficulty)) / (bc.DispelFocus * 2))) / 100;
                    else
                    {
                        double difficulty = bc.DispelDifficulty + bc.ControlMaster.Skills.SpiritSpeak.Value / 2.0;
                        if (difficulty > 125) difficulty = 125;
                        dispelChance = (50.0 + ((100 * (Caster.Skills.Magery.Value - difficulty)) / (bc.DispelFocus * 2))) / 100;
                    }

                    if (dispelChance > Utility.RandomDouble())
                    {
                        Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
                        Effects.PlaySound(m, m.Map, 0x201);
                        bc.OnBeforeDispel(Caster);
                        m.Delete();
                    }
                    else
                    {
                        m.FixedEffect(0x3779, 10, 20);
                        Caster.SendLocalizedMessage(1010084); // The creature resisted the attempt to dispel it!
                        Caster.DoHarmful(m);					// and now he's pissed at you
                    }
                }
            }

            FinishSequence();
        }

        public class InternalTarget : Target
        {
            private MassDispelSpell m_Owner;

            public InternalTarget(MassDispelSpell owner)
                : base(12, true, TargetFlags.Harmful)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                IPoint3D p = o as IPoint3D;

                if (p != null)
                    m_Owner.Target(p);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}