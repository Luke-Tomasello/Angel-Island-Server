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

/* ChangeLog:
 * 7/16/10, adam
 *		o call new OnBeforeDispel() to allow dispelled creatures to do something.
 *		Spirit Speak bonus:
 *			You gain up to 50pts extra dispel protection against npcs, however, it'll never exceed the 
 *				difficulty of dispelling a demon.  
 *			Everything will have at least a 22% chance to be dispelled by an NPC.  
 *			Players are unaffected by this bonus.
 *			Dispel Difficulty Adjustment (NPCs Only) = Difficulty + (SpiritSpeak/2).   
 *			If difficulty > 125, difficulty = 125.  
 * 2010.06.12 - Pix
 *      Fixed dispel-on-self to work like a real spell.
 * 2010.05.24, Pix
 *      Added if target==caster, then remove magic effects on caster.
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Items;
using Server.Mobiles;
using Server.Spells.Fifth;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Targeting;
using System;

namespace Server.Spells.Sixth
{
    public class DispelSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Dispel", "An Ort",
                SpellCircle.Sixth,
                218,
                9002,
                Reagent.Garlic,
                Reagent.MandrakeRoot,
                Reagent.SulfurousAsh
            );

        public DispelSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(Mobile m)
        {
            Type t = m.GetType();
            bool dispellable = false;

            if (m is BaseCreature)
            {
                dispellable = ((BaseCreature)m).Summoned && !((BaseCreature)m).IsAnimatedDead;
            }
            else if (m == Caster)
            {
                dispellable = true;
            }

            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (!dispellable)
            {
                Caster.SendLocalizedMessage(1005049); // That cannot be dispelled.
            }
            else if (CheckHSequence(m))
            {
                if (m == Caster)
                {
                    //2010.05.24 - Pix - New functionality - if caster casts dispel on himself, void all
                    // magic effects.

                    //Protection
                    if (ProtectionSpell.Registry.Contains(Caster))
                    {
                        ProtectionSpell.Registry.Remove(Caster);
                    }
                    //Reactive Armor
                    Caster.MeleeDamageAbsorb = 0;
                    //Reflect
                    Caster.MagicDamageAbsorb = 0;

                    //restart defensivespell "timer"
                    DefensiveSpell.Nullify(Caster);

                    //NightSight
                    if (!Caster.CanBeginAction(typeof(LightCycle)))
                    {
                        Caster.EndAction(typeof(LightCycle));
                        Caster.LightLevel = 0;
                    }

                    //Strength
                    string name = String.Format("[Magic] {0} Offset", StatType.Str);
                    StatMod mod = Caster.GetStatMod(name);
                    if (mod != null /*&& mod.Offset > 0*/)
                    {
                        Caster.RemoveStatMod(name);
                    }
                    //Agility
                    name = String.Format("[Magic] {0} Offset", StatType.Dex);
                    mod = Caster.GetStatMod(name);
                    if (mod != null /*&& mod.Offset > 0*/)
                    {
                        Caster.RemoveStatMod(name);
                    }
                    //Cunning
                    name = String.Format("[Magic] {0} Offset", StatType.Int);
                    mod = Caster.GetStatMod(name);
                    if (mod != null /*&& mod.Offset > 0*/)
                    {
                        Caster.RemoveStatMod(name);
                    }
                    //Bless - handled by above three

                    //Incognito
                    if (!Caster.CanBeginAction(typeof(IncognitoSpell)))
                    {
                        if (Caster is PlayerMobile)
                            ((PlayerMobile)Caster).SetHairMods(-1, -1);

                        Caster.BodyMod = 0;
                        Caster.HueMod = -1;
                        Caster.NameMod = null;
                        Caster.EndAction(typeof(IncognitoSpell));

                        BaseArmor.ValidateMobile(Caster);
                    }

                    //ArchProtection
                    if (!Caster.CanBeginAction(typeof(ArchProtectionSpell)))
                    {
                        Caster.VirtualArmorMod = 0;
                        Caster.EndAction(typeof(ArchProtectionSpell));
                    }

                    //Polymorph
                    if (!Caster.CanBeginAction(typeof(PolymorphSpell)))
                    {
                        Caster.BodyMod = 0;
                        Caster.HueMod = -1;
                        Caster.EndAction(typeof(PolymorphSpell));

                        BaseArmor.ValidateMobile(Caster);
                    }

                    //Invisibility (shouldn't ever happen, but what the fuck, might as well put it in!)
                    if (Caster.AccessLevel <= AccessLevel.Player) //make sure if we're staff, don't reveal
                    {
                        Caster.Hidden = false;
                    }

                    Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
                    Effects.PlaySound(m, m.Map, 0x201);

                    Caster.SendMessage("All beneficial magic effects have been stripped from you.");
                }
                else //Normal dispel dispellable creature stuff:
                {
                    SpellHelper.Turn(Caster, m);

                    BaseCreature bc = m as BaseCreature;

                    double dispelChance = 0;

                    if (bc != null)
                    {
                        // players don't have trouble dispelling the summons of a summoner (magery+spirit speak)
                        if (Caster is PlayerMobile || bc.ControlMaster == null)
                            dispelChance = (50.0 + ((100 * (Caster.Skills.Magery.Value - bc.DispelDifficulty)) / (bc.DispelFocus * 2))) / 100;
                        else
                        {
                            double difficulty = bc.DispelDifficulty + bc.ControlMaster.Skills.SpiritSpeak.Value / 2.0;
                            if (difficulty > 125) difficulty = 125;
                            dispelChance = (50.0 + ((100 * (Caster.Skills.Magery.Value - difficulty)) / (bc.DispelFocus * 2))) / 100;
                        }
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
                    }
                }
            }

            FinishSequence();
        }

        public class InternalTarget : Target
        {
            private DispelSpell m_Owner;

            public InternalTarget(DispelSpell owner)
                : base(12, false, TargetFlags.Harmful)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Mobile)
                {
                    m_Owner.Target((Mobile)o);
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}