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

/* Scripts/Misc/MagicEquipment.cs
 * ChangeLog:
 *	3/30/23, Yoar
 *		Implementation for pre-AOS magic equipment effects
 *		https://wiki.stratics.com/index.php?title=UO:Items_With_Magical_Charges
 *		
 *		Unlike "magic item" effects, we do not make use of the base spell system here.
 *		This is because:
 *		1) Equipment effects remain active while the magic equipment is worn. A single
 *		   spell cast when the item is equipped won't do.
 *		2) The behavior is reportedly slightly different from normal spells. For
 *		   example, some effects are applied in "stealth" without playing the effects
 *		   that are normally played by the spell.
 */

using Server.Engines.ConPVP;
using Server.Spells;
using Server.Spells.Second;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public enum MagicEquipEffect : sbyte
    {
        None = -1,

        NightSight,
        Protection,
        Clumsiness,
        Feeblemind,
        Weakness,
        Healing,
        GreaterHealing,
        Agility,
        Cunning,
        Strength,
        Harm,
        Curse,
        Bless,
        Teleport,
        Curing,
        ManaDrain,
        Paralyzation,
        Invisibility,
        Restoration,
        Identification,
        SpellReflection,
    }

    public interface IMagicEquip
    {
        MagicEquipEffect MagicEffect { get; set; }
        int MagicCharges { get; set; }
    }

    public static class MagicEquipment
    {
        public static bool DoEffects = false;
        public static int StatOffset = 10;

        private static MagicEffect[] m_Effects = new MagicEffect[]
            {
                new NightSightEffect(),
                new ProtectionEffect(),
                new ClumsinessEffect(),
                new FeeblemindEffect(),
                new WeaknessEffect(),
                new HealingEffect(),
                new GreaterHealingEffect(),
                new AgilityEffect(),
                new CunningEffect(),
                new StrengthEffect(),
                new HarmEffect(),
                new CurseEffect(),
                new BlessEffect(),
                new TeleportEffect(),
                new CuringEffect(),
                new ManaDrainEffect(),
                new ParalyzationEffect(),
                new InvisibilityEffect(),
                new RestorationEffect(),
                new IdentificationEffect(),
                new MagicReflectionEffect(),
            };

        private static MagicEffect GetEffect(MagicEquipEffect effect)
        {
            int index = (int)effect;

            if (index >= 0 && index < m_Effects.Length)
                return m_Effects[index];

            return null;
        }

        public static int GetLabel(MagicEquipEffect effect)
        {
            MagicEffect e = GetEffect(effect);

            if (e != null)
                return e.Label;

            return 0;
        }

        public static string GetOldSuffix(MagicEquipEffect effect, int charges)
        {
            MagicEffect e = GetEffect(effect);

            if (e == null)
                return string.Empty;

            string name = e.OldName;

            if (name == null && !Server.Text.Cliloc.Lookup.TryGetValue(e.Label, out name))
                return string.Empty;

            return string.Format("{0} (charges: {1})", name.ToLower(), charges);
        }

        public static void OnAdded(Mobile m, Item item)
        {
            if (DuelContext.IsActivelyDueling(m))
                return;

            IMagicEquip equip = item as IMagicEquip;

            if (equip == null || equip.MagicCharges <= 0)
                return;

            MagicEffect e = GetEffect(equip.MagicEffect);

            if (e == null)
                return;

            TimeSpan duration = TimeSpan.Zero;

            if (e.OnAdded(m, item, ref duration))
            {
                equip.MagicCharges--;

                Register(m, item, equip.MagicEffect, duration);
            }
            else if (equip.MagicEffect == MagicEquipEffect.SpellReflection)
            {
                m.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
            }
        }

        public static void OnRemoved(Mobile m, Item item)
        {
            Unregister(item);
        }

        public static bool OnUse(Mobile m, Item item)
        {
            if (DuelContext.IsActivelyDueling(m))
                return false;

            IMagicEquip equip = item as IMagicEquip;

            if (equip == null || equip.MagicCharges <= 0)
                return false;

            MagicEffect e = GetEffect(equip.MagicEffect);

            if (e == null)
                return false;

            return e.OnUse(m, item);
        }

        public static void RemoveTimer(Mobile m, MagicEquipEffect effect)
        {
            foreach (Item item in m.Items)
            {
                MagicEffectContext context;

                if (m_Registry.TryGetValue(item, out context) && context.Effect == effect)
                {
                    // stop timer without removing the effect
                    context.StopTimer();

                    m_Registry.Remove(item);

                    break;
                }
            }
        }

        private class NightSightEffect : MagicEffect
        {
            public static int LightLevel = 25;

            public NightSightEffect()
                : base(1015008, 1017324, "Night Eyes") // Nightsight | invisibility charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromMinutes(Utility.RandomMinMax(15, 25));

                if (!LightCycle.UnderEffect(m))
                {
                    LightCycle.BeginEffect(m, LightLevel);

                    if (DoEffects)
                    {
                        m.FixedParticles(0x376A, 9, 32, 5007, EffectLayer.Waist);
                        m.PlaySound(0x1E3);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                LightCycle.EndEffect(m);
            }
        }

        private class ProtectionEffect : MagicEffect
        {
            public ProtectionEffect()
                : base(1015176, 1017325) // Protection | protection charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(Utility.RandomMinMax(90, 150));

                if (ProtectionSpell.Registry.ContainsKey(m) && m.BeginAction(typeof(DefensiveSpell)))
                {
                    double value = 50.0;

                    ProtectionSpell.Registry.Add(m, value);

                    if (DoEffects)
                    {
                        m.FixedParticles(0x375A, 9, 20, 5016, EffectLayer.Waist);
                        m.PlaySound(0x1ED);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                ProtectionSpell.Registry.Remove(m);
                DefensiveSpell.Nullify(m);
            }
        }

        private class ClumsinessEffect : MagicEffect
        {
            public ClumsinessEffect()
                : base(1015164, 1017326, "Clumsiness") // Clumsy | clumsiness charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(120.0);

                if (CanAddStatMod(m, StatType.Dex, -StatOffset))
                {
                    SpellHelper.AddStatCurse(m, m, StatType.Dex, StatOffset, TimeSpan.Zero);

                    if (DoEffects)
                    {
                        m.FixedParticles(0x3779, 10, 15, 5002, EffectLayer.Head);
                        m.PlaySound(0x1DF);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                RemoveStatMod(m, StatType.Dex, -StatOffset);
            }
        }

        private class FeeblemindEffect : MagicEffect
        {
            public FeeblemindEffect()
                : base(1015166, 1017327, "Feeblemindedness") // Feeblemind | feeblemind charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(120.0);

                if (CanAddStatMod(m, StatType.Int, -StatOffset))
                {
                    SpellHelper.AddStatCurse(m, m, StatType.Int, StatOffset, TimeSpan.Zero);

                    if (DoEffects)
                    {
                        m.FixedParticles(0x3779, 10, 15, 5004, EffectLayer.Head);
                        m.PlaySound(0x1E4);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                RemoveStatMod(m, StatType.Int, -StatOffset);
            }
        }

        private class WeaknessEffect : MagicEffect
        {
            public WeaknessEffect()
                : base(1015170, 1017328, "Weakness") // Weaken | weakness charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(120.0);

                if (CanAddStatMod(m, StatType.Str, -StatOffset))
                {
                    SpellHelper.AddStatCurse(m, m, StatType.Str, StatOffset, TimeSpan.Zero);

                    if (DoEffects)
                    {
                        m.FixedParticles(0x3779, 10, 15, 5009, EffectLayer.Waist);
                        m.PlaySound(0x1E6);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                RemoveStatMod(m, StatType.Str, -StatOffset);
            }
        }

        private class HealingEffect : MagicEffect
        {
            public static int HealMin = 6;
            public static int HealMax = 10;

            public HealingEffect()
                : base(1015011, 1017329) // Heal | healing charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(Utility.RandomMinMax(15, 25));

                m.Heal(Utility.RandomMinMax(HealMin, HealMax));

                if (DoEffects)
                {
                    m.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
                    m.PlaySound(0x1F2);
                }

                return true;
            }
        }

        private class GreaterHealingEffect : MagicEffect
        {
            public static int HealMin = 21;
            public static int HealMax = 30;

            public GreaterHealingEffect()
                : base(1015012, 1017330) // Greater Heal | greater healing charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(Utility.RandomMinMax(25, 35));

                m.Heal(Utility.RandomMinMax(HealMin, HealMax));

                if (DoEffects)
                {
                    m.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
                    m.PlaySound(0x202);
                }

                return true;
            }
        }

        private class AgilityEffect : MagicEffect
        {
            public AgilityEffect()
                : base(1015005, 1017331) // Agility | agility charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(120.0);

                if (CanAddStatMod(m, StatType.Dex, StatOffset))
                {
                    SpellHelper.AddStatBonus(m, m, StatType.Dex, StatOffset, TimeSpan.Zero);

                    if (DoEffects)
                    {
                        m.FixedParticles(0x375A, 10, 15, 5010, EffectLayer.Waist);
                        m.PlaySound(0x1e7);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                RemoveStatMod(m, StatType.Dex, StatOffset);
            }
        }

        private class CunningEffect : MagicEffect
        {
            public CunningEffect()
                : base(1015172, 1017332) // Cunning | cunning charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(120.0);

                if (CanAddStatMod(m, StatType.Int, StatOffset))
                {
                    SpellHelper.AddStatBonus(m, m, StatType.Int, StatOffset, TimeSpan.Zero);

                    if (DoEffects)
                    {
                        m.FixedParticles(0x375A, 10, 15, 5011, EffectLayer.Head);
                        m.PlaySound(0x1EB);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                RemoveStatMod(m, StatType.Int, StatOffset);
            }
        }

        private class StrengthEffect : MagicEffect
        {
            public StrengthEffect()
                : base(1015014, 1017333) // Strength | strength charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(120.0);

                if (CanAddStatMod(m, StatType.Str, StatOffset))
                {
                    SpellHelper.AddStatBonus(m, m, StatType.Str, StatOffset, TimeSpan.Zero);

                    if (DoEffects)
                    {
                        m.FixedParticles(0x375A, 10, 15, 5017, EffectLayer.Waist);
                        m.PlaySound(0x1EE);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                RemoveStatMod(m, StatType.Str, StatOffset);
            }
        }

        private class HarmEffect : MagicEffect
        {
            public static int DamageMin = 1;
            public static int DamageMax = 7;

            public HarmEffect()
                : base(1015173, 1017334, "Wounding") // Harm | harm charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(Utility.RandomMinMax(15, 25));

                if (DoEffects)
                {
                    m.FixedParticles(0x374A, 10, 15, 5013, EffectLayer.Waist);
                    m.PlaySound(0x1F1);
                }

                m.Damage(Utility.RandomMinMax(DamageMin, DamageMax), m, null);

                return true;
            }
        }

        private class CurseEffect : MagicEffect
        {
            public CurseEffect()
                : base(1015188, 1017335, "Evil") // Curse | curse charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(120.0);

                if (CanAddStatMod(m, StatType.Str, -StatOffset) && CanAddStatMod(m, StatType.Dex, -StatOffset) && CanAddStatMod(m, StatType.Int, -StatOffset))
                {
                    SpellHelper.AddStatCurse(m, m, StatType.Str, StatOffset, TimeSpan.Zero); SpellHelper.DisableSkillCheck = true;
                    SpellHelper.AddStatCurse(m, m, StatType.Dex, StatOffset, TimeSpan.Zero);
                    SpellHelper.AddStatCurse(m, m, StatType.Int, StatOffset, TimeSpan.Zero); SpellHelper.DisableSkillCheck = false;

                    if (DoEffects)
                    {
                        m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);
                        m.PlaySound(0x1E1);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                RemoveStatMod(m, StatType.Str, -StatOffset);
                RemoveStatMod(m, StatType.Dex, -StatOffset);
                RemoveStatMod(m, StatType.Int, -StatOffset);
            }
        }

        private class BlessEffect : MagicEffect
        {
            public BlessEffect()
                : base(1015178, 1017336) // Bless | bless charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(120.0);

                if (CanAddStatMod(m, StatType.Str, StatOffset) && CanAddStatMod(m, StatType.Dex, StatOffset) && CanAddStatMod(m, StatType.Int, StatOffset))
                {
                    SpellHelper.AddStatBonus(m, m, StatType.Str, StatOffset, TimeSpan.Zero); SpellHelper.DisableSkillCheck = true;
                    SpellHelper.AddStatBonus(m, m, StatType.Dex, StatOffset, TimeSpan.Zero);
                    SpellHelper.AddStatBonus(m, m, StatType.Int, StatOffset, TimeSpan.Zero); SpellHelper.DisableSkillCheck = false;

                    if (DoEffects)
                    {
                        m.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                        m.PlaySound(0x1EA);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                RemoveStatMod(m, StatType.Str, StatOffset);
                RemoveStatMod(m, StatType.Dex, StatOffset);
                RemoveStatMod(m, StatType.Int, StatOffset);
            }
        }

        private class TeleportEffect : MagicEffect
        {
            public TeleportEffect()
                : base(1015182, 1017337) // Teleport | Teleport Charges: ~1_val~
            {
            }

            public override bool OnUse(Mobile m, Item item)
            {
                m.Target = new InternalTarget(item);

                return true;
            }

            private class InternalTarget : Target
            {
                private Item m_Item;

                public InternalTarget(Item item)
                    : base(12, true, TargetFlags.None)
                {
                    m_Item = item;
                }

                protected override void OnTarget(Mobile from, object o)
                {
                    if (m_Item.Parent != from && !m_Item.IsChildOf(from.Backpack))
                        return;

                    IPoint3D p = o as IPoint3D;

                    if (p == null)
                        return;

                    IMagicEquip magicEquip = m_Item as IMagicEquip;

                    if (magicEquip == null || magicEquip.MagicEffect != MagicEquipEffect.Teleport || magicEquip.MagicCharges <= 0)
                        return;

                    if (Target(from, p))
                        magicEquip.MagicCharges--;
                }
            }

            private static bool Target(Mobile m, IPoint3D p)
            {
                IPoint3D orig = p;
                Map map = m.Map;

                SpellHelper.GetSurfaceTop(ref p);

                if (Factions.Sigil.ExistsOn(m))
                {
                    m.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
                }
                else if (Engines.Alignment.TheFlag.ExistsOn(m))
                {
                    m.SendMessage("You can't do that while carrying the flag.");
                }
                else if (Server.Misc.WeightOverloading.IsOverloaded(m))
                {
                    m.SendLocalizedMessage(502359, "", 0x22); // Thou art too encumbered to move.
                }
                else if (!SpellHelper.CheckTravel(m, TravelCheckType.TeleportFrom))
                {
                }
                else if (!SpellHelper.CheckTravel(m, map, new Point3D(p), TravelCheckType.TeleportTo))
                {
                }
                else if (map == null || !map.CanSpawnLandMobile(p.X, p.Y, p.Z))
                {
                    m.SendLocalizedMessage(501942); // That location is blocked.
                }
                else if (SpellHelper.CheckMulti(new Point3D(p), map))
                {
                    m.SendLocalizedMessage(501942); // That location is blocked.
                }
                else
                {
                    SpellHelper.Turn(m, orig);

                    Point3D from = m.Location;
                    Point3D to = new Point3D(p);

                    m.Location = to;
                    m.ProcessDelta();

                    Effects.SendLocationParticles(EffectItem.Create(from, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                    Effects.SendLocationParticles(EffectItem.Create(to, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

                    m.PlaySound(0x1FE);

                    return true;
                }

                return false;
            }
        }

        private class CuringEffect : MagicEffect
        {
            public CuringEffect()
                : base(1015023, 1017338) // Cure | curing charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(Utility.RandomMinMax(15, 25));

                Poison p = m.Poison;

                if (!m.Incurable && p != null)
                {
                    int chanceToCure = 10000;

                    chanceToCure += (15 * 500) / 2;
                    chanceToCure += (p.Level + 1) * 1750;
                    chanceToCure /= 100;

                    if (chanceToCure > Utility.Random(100) && m.CurePoison(m))
                        m.SendLocalizedMessage(1010059); // You have been cured of all poisons.
                }

                if (DoEffects)
                {
                    m.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                    m.PlaySound(0x1E0);
                }

                return true;
            }
        }

        private class ManaDrainEffect : MagicEffect
        {
            public ManaDrainEffect()
                : base(1015191, 1017339, "Mage's Bane") // Mana Drain | mana drain charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(Utility.RandomMinMax(25, 35));

                if (m.Mana >= 100)
                    m.Mana -= Utility.Random(1, 100);
                else
                    m.Mana -= Utility.Random(1, m.Mana);

                if (DoEffects)
                {
                    m.FixedParticles(0x374A, 10, 15, 5032, EffectLayer.Head);
                    m.PlaySound(0x1F8);
                }

                return true;
            }
        }

        private class ParalyzationEffect : MagicEffect
        {
            public ParalyzationEffect()
                : base(1015199, 1017340, "Ghoul's Touch") // Paralyze | paralyzation charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(Utility.RandomMinMax(10, 15));

                if (!m.Paralyzed)
                {
                    m.Paralyzed = true;

                    if (DoEffects)
                    {
                        m.PlaySound(0x204);
                        m.FixedEffect(0x376A, 6, 1);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                m.Paralyzed = false;
            }
        }

        private class InvisibilityEffect : MagicEffect
        {
            public InvisibilityEffect()
                : base(1015205, 1017347) // Invisibility | invisibility charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(120.0);

                if (!m.Hidden)
                {
                    if (DoEffects)
                    {
                        Effects.SendLocationParticles(EffectItem.Create(new Point3D(m.X, m.Y, m.Z + 16), m.Map, EffectItem.DefaultDuration), 0x376A, 10, 15, 5045);
                        m.PlaySound(0x3C4);
                    }

                    m.Hidden = true;
                    m.Combatant = null;
                    m.Warmode = false;

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                m.RevealingAction();
            }
        }

        private class RestorationEffect : MagicEffect
        {
            public static int DelayMin = 1;
            public static int DelayMax = 3;

            public RestorationEffect()
                : base(1075867, 1017348, "Restoration") // Restoration | restoration charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(Utility.RandomMinMax(DelayMin, DelayMax));

                if (m.Hits < m.HitsMax)
                {
                    m.Hits++;

                    return true;
                }

                return false;
            }
        }

        private class IdentificationEffect : MagicEffect
        {
            public IdentificationEffect()
                : base(1002094, 1017350, "Identification") // Item Identification | identification charges: ~1_val~
            {
            }

            public override bool OnUse(Mobile m, Item item)
            {
                m.BeginTarget(6, false, TargetFlags.None, OnTarget, item);

                return true;
            }

            private static void OnTarget(Mobile from, object targeted, object state)
            {
                Item item = (Item)state;

                IMagicEquip equip = item as IMagicEquip;

                if (item.Parent != from || equip == null || equip.MagicCharges <= 0)
                    return;

                if (targeted is Item)
                {
                    ItemIdentification.IdentifyItem(from, (Item)targeted);

                    equip.MagicCharges--;
                }
            }
        }

        /* Spell Reflect Enchantment[edit]
         * Once a spell reflect item is equipped, it will immediately apply a charge of magic reflect to the bearer. No animation is played for the reflect charge, 
         * so it is applied with "stealth" and can be very useful into tricking your opponent. Regardless of whether this charge of reflect is removed from the bearer, 
         * it will continue to refresh the magic reflect charges every 5 seconds. This means that even if you are not cast on, you are using up the charges of magic reflect. 
         * In the case the magic reflect charge is removed (by a player or monster casting a spell on you), the next charge will not be immediately applied - 
         * it will be applied on the next 5 second interval.
         * Example: Magic reflect item is equipped. 2 seconds pass, then reflect is removed by another player casting harm. An ebolt is cast on the bearer 1 second later, 
         * it goes through and damages the player. 2 seconds later, a new magic reflect charge is applied.
         * This can be worked around by de-equipping and re-equipping the magic reflect item, which will take 2 seconds each time due to the action delay (1 second).
         * http://wiki.uosecondage.com/PvP_Guide
         */
        private class MagicReflectionEffect : MagicEffect
        {
            public static int Absorb = 11; // absorb 11 circles

            public MagicReflectionEffect()
                : base(1015197, 1017371, "Reflection") // Magic Reflection | spell reflection charges: ~1_val~
            {
            }

            public override bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                duration = TimeSpan.FromSeconds(5.0);

                if (m.BeginAction(typeof(DefensiveSpell)))
                {
                    m.MagicDamageAbsorb = Absorb;

                    if (DoEffects)
                    {
                        m.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);
                        m.PlaySound(0x1E9);
                    }

                    return true;
                }

                return false;
            }

            public override void OnRemoved(Mobile m, Item item)
            {
                m.MagicDamageAbsorb = 0;

                m.EndAction(typeof(DefensiveSpell));
            }
        }

        private abstract class MagicEffect
        {
            private int m_Label;
            private int m_ChargeLabel;
            private string m_OldName;

            public int Label { get { return m_Label; } }
            public int ChargeLabel { get { return m_ChargeLabel; } }
            public string OldName { get { return m_OldName; } }

            public MagicEffect(int spellLabel, int chargeLabel)
                : this(spellLabel, chargeLabel, null)
            {
            }

            public MagicEffect(int label, int chargeLabel, string oldName)
            {
                m_Label = label;
                m_ChargeLabel = chargeLabel;
                m_OldName = oldName;
            }

            public virtual bool OnAdded(Mobile m, Item item, ref TimeSpan duration)
            {
                return false;
            }

            public virtual void OnRemoved(Mobile m, Item item)
            {
            }

            public virtual bool OnUse(Mobile m, Item item)
            {
                return false;
            }

            protected static bool CanAddStatMod(Mobile m, StatType type, int offset)
            {
                if (offset == 0)
                    return false; // sanity

                string name = GetStatModName(type);

                if (name == null)
                    return false; // sanity

                StatMod mod = m.GetStatMod(name);

                if (mod == null)
                    return true;

                return (offset > 0 ? (mod.Offset < 0 || mod.Offset < offset) : (mod.Offset > 0 || mod.Offset > offset));
            }

            protected static void RemoveStatMod(Mobile m, StatType type, int offset)
            {
                string name = GetStatModName(type);

                if (name == null)
                    return; // sanity

                StatMod mod = m.GetStatMod(name);

                if (mod != null && mod.Offset == offset)
                    m.RemoveStatMod(name);
            }

            private static string GetStatModName(StatType type)
            {
                switch (type)
                {
                    case StatType.Str: return "[Magic] Str Offset";
                    case StatType.Dex: return "[Magic] Dex Offset";
                    case StatType.Int: return "[Magic] Int Offset";
                }

                return null;
            }
        }

        private static readonly Dictionary<Item, MagicEffectContext> m_Registry = new Dictionary<Item, MagicEffectContext>();

        private static void Register(Mobile m, Item item, MagicEquipEffect effect, TimeSpan interval)
        {
            Unregister(item);

            m_Registry[item] = new MagicEffectContext(m, item, effect, interval);
        }

        private static void Unregister(Item item)
        {
            MagicEffectContext context;

            if (m_Registry.TryGetValue(item, out context))
            {
                context.Expire();

                m_Registry.Remove(item);
            }
        }

        private class MagicEffectContext
        {
            private Mobile m_Mobile;
            private Item m_Item;
            private MagicEquipEffect m_Effect;
            private Timer m_Timer;

            public MagicEquipEffect Effect { get { return m_Effect; } }

            public MagicEffectContext(Mobile m, Item item, MagicEquipEffect effect, TimeSpan interval)
            {
                m_Mobile = m;
                m_Item = item;
                m_Effect = effect;

                ResetTimer(interval);
            }

            public void Expire()
            {
                StopTimer();

                MagicEffect e = GetEffect(m_Effect);

                if (e != null)
                    e.OnRemoved(m_Mobile, m_Item);
            }

            public void ResetTimer(TimeSpan interval)
            {
                if (m_Timer != null && m_Timer.Interval != interval)
                    StopTimer();

                if (m_Timer == null && interval != TimeSpan.Zero)
                    m_Timer = Timer.DelayCall(interval, interval, OnTick);
            }

            public void StopTimer()
            {
                if (m_Timer != null)
                {
                    m_Timer.Stop();
                    m_Timer = null;
                }
            }

            public void OnTick()
            {
                if (m_Mobile.Deleted || m_Item.Deleted || m_Item.Parent != m_Mobile || m_Item.Map == null || m_Item.Map == Map.Internal)
                {
                    Unregister(m_Item);
                    return;
                }

                IMagicEquip equip = m_Item as IMagicEquip;

                if (equip == null || equip.MagicCharges <= 0 || equip.MagicEffect != m_Effect)
                {
                    Unregister(m_Item);
                    return;
                }

                MagicEffect e = GetEffect(equip.MagicEffect);

                if (e == null)
                {
                    Unregister(m_Item);
                    return;
                }

                TimeSpan duration = TimeSpan.Zero;

                e.OnAdded(m_Mobile, m_Item, ref duration); // we're either keeping the effect up or re-applying the effect

                equip.MagicCharges--;

                ResetTimer(duration);
            }
        }
    }
}