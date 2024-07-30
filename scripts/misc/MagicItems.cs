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

/* Scripts/Misc/MagicItems.cs
 * ChangeLog:
 *  3/29/23, Yoar
 *      Charges are now displayed in the single-click name suffix
 *	3/25/23, Yoar
 *		Implementation for pre-AOS magic effects
 *		https://wiki.stratics.com/index.php?title=UO:Items_With_Magical_Charges
 */

using Server.Items;
using Server.Spells;
using Server.Targeting;
using System;

namespace Server
{
    public enum MagicItemEffect : sbyte
    {
        None = -1,

        // First circle
        Clumsy,
        CreateFood,
        Feeblemind,
        Heal,
        MagicArrow,
        NightSight,
        ReactiveArmor,
        Weaken,

        // Second circle
        Agility,
        Cunning,
        Cure,
        Harm,
        MagicTrap,
        RemoveTrap,
        Protection,
        Strength,

        // Third circle
        Bless,
        Fireball,
        MagicLock,
        Poison,
        Telekinesis,
        Teleport,
        Unlock,
        WallOfStone,

        // Fourth circle
        ArchCure,
        ArchProtection,
        Curse,
        FireField,
        GreaterHeal,
        Lightning,
        ManaDrain,
        Recall,

        // Fifth circle
        BladeSpirits,
        DispelField,
        Incognito,
        MagicReflect,
        MindBlast,
        Paralyze,
        PoisonField,
        SummonCreature,

        // Sixth circle
        Dispel,
        EnergyBolt,
        Explosion,
        Invisibility,
        Mark,
        MassCurse,
        ParalyzeField,
        Reveal,

        // Seventh circle
        ChainLightning,
        EnergyField,
        FlameStrike,
        GateTravel,
        ManaVampire,
        MassDispel,
        MeteorSwarm,
        Polymorph,

        // Eighth circle
        Earthquake,
        EnergyVortex,
        Resurrection,
        AirElemental,
        SummonDaemon,
        EarthElemental,
        FireElemental,
        WaterElemental,

        // Other
        Identification,
        Restoration,
    }

    public interface IMagicItem
    {
        MagicItemEffect MagicEffect { get; set; }
        int MagicCharges { get; set; }
    }

    public static class MagicItems
    {
        public static void OnUse(Mobile from, Item item, bool targetSelf = false)
        {
            IMagicItem magicItem = item as IMagicItem;

            if (magicItem == null)
                return;

            if (item.Parent != from && item.Layer != Layer.Invalid)
            {
                from.SendLocalizedMessage(502641); // You must equip this item to use it.
                return;
            }

            if (!from.CanBeginAction(GetLock(from, item)))
            {
                if (item is BaseWand)
                    from.SendLocalizedMessage(1070860); // You must wait a moment for the wand to recharge.
                else
                    from.SendLocalizedMessage(1072306); // You must wait a moment for it to recharge.

                return;
            }

            if (magicItem.MagicCharges <= 0)
            {
                from.SendLocalizedMessage(1019073); // This item is out of charges.
                return;
            }

            switch (magicItem.MagicEffect)
            {
                case MagicItemEffect.Identification:
                    {
                        from.Target = new ItemIDTarget(item);

                        break;
                    }
                default:
                    {
                        int spellID = (int)magicItem.MagicEffect;

                        if (spellID < 64)
                        {
                            if (targetSelf)
                                CastSpellTarget(spellID, from, item, from);
                            else
                                CastSpell(spellID, from, item);
                        }

                        break;
                    }
            }
        }

        public static void OnHit(Mobile attacker, Mobile defender, Item item)
        {
            IMagicItem magicItem = item as IMagicItem;

            if (magicItem == null || item.Parent != attacker || !attacker.CanBeginAction(GetLock(attacker, item)) || magicItem.MagicCharges <= 0)
                return;

            switch (magicItem.MagicEffect)
            {
                default:
                    {
                        int spellID = (int)magicItem.MagicEffect;

                        if (spellID < 64)
                            CastSpellTarget(spellID, attacker, item, defender);

                        break;
                    }
            }
        }

        private static bool CastSpell(int spellID, Mobile from, Item item)
        {
            Spell spell = SpellRegistry.NewSpell(spellID, from, item);

            if (spell == null)
                return false;

            bool oldMovable = item.Movable;

            item.Movable = false;
            spell.Cast();
            item.Movable = oldMovable;

            return true;
        }

        private static bool CastSpellTarget(int spellID, Mobile from, Item item, object target)
        {
            from.TargetLocked = true;

            // remember our existing target
            Target oldTarget = from.Target;
            from.Target = null;

            bool result = CastSpell(spellID, from, item);

            if (from.Target != null)
                from.Target.Invoke(from, target);

            // if somehow our target remains up, ensure that we deal with it
            Target remainingTarget = from.Target;

            // reset our existing target
            from.Target = null;
            from.Target = oldTarget;

            from.TargetLocked = false;

            if (remainingTarget != null)
                from.Target = remainingTarget;

            return result;
        }

        public static int GetLabel(MagicItemEffect effect)
        {
            EffectInfo info = GetInfo(effect);

            if (info == null)
                return 0;

            return info.Label;
        }

        public static string GetOldSuffix(MagicItemEffect effect, int charges)
        {
            EffectInfo info = GetInfo(effect);

            if (info == null)
                return String.Empty;

            string name = info.OldName;

            if (name == null && !Server.Text.Cliloc.Lookup.TryGetValue(info.Label, out name))
                return String.Empty;

            return String.Format("{0} (charges: {1})", name.ToLower(), charges);
        }

        public static void ConsumeCharge(Mobile from, Item item)
        {
            IMagicItem magicItem = item as IMagicItem;

            if (magicItem == null || magicItem.MagicCharges <= 0)
                return;

            if (--magicItem.MagicCharges == 0)
                from.SendLocalizedMessage(1019073); // This item is out of charges.

            TimeSpan delay = GetLockDelay(from, item);

            if (delay != TimeSpan.Zero)
                BeginLock(from, GetLock(from, item), delay);
        }

        private static readonly EffectInfo[] m_Table;

        static MagicItems()
        {
            m_Table = new EffectInfo[0x7F];

            // First circle
            m_Table[00] = new EffectInfo(1015164, 1017326, "Clumsiness"); // Clumsy
            m_Table[01] = new EffectInfo(1015165); // Create Food
            m_Table[02] = new EffectInfo(1015166, 1017327, "Feeblemindedness"); // Feeblemind
            m_Table[03] = new EffectInfo(1015011, 1017329); // Heal
            m_Table[04] = new EffectInfo(1015167, 1060492, "Burning"); // Magic Arrow
            m_Table[05] = new EffectInfo(1015168, 1017324, "Night Eyes"); // Night Sight
            m_Table[06] = new EffectInfo(1015169); // Reactive Armor
            m_Table[07] = new EffectInfo(1015170, 1017328, "Weakness"); // Weaken

            // Second circle
            m_Table[08] = new EffectInfo(1015005, 1017331); // Agility
            m_Table[09] = new EffectInfo(1015172, 1017332); // Cunning
            m_Table[10] = new EffectInfo(1015023, 1017338); // Cure
            m_Table[11] = new EffectInfo(1015173, 1017334, "Wounding"); // Harm
            m_Table[12] = new EffectInfo(1015174); // Magic Trap
            m_Table[13] = new EffectInfo(1002136); // Remove Trap
            m_Table[14] = new EffectInfo(1015176, 1017325); // Protection
            m_Table[15] = new EffectInfo(1015014, 1017333); // Strength

            // Third circle
            m_Table[16] = new EffectInfo(1015178, 1017336); // Bless
            m_Table[17] = new EffectInfo(1015179, 1060487, "Daemon's Breath"); // Fireball
            m_Table[18] = new EffectInfo(1015180); // Magic Lock
            m_Table[19] = new EffectInfo(1015018); // Poison
            m_Table[20] = new EffectInfo(1015181); // Telekinesis
            m_Table[21] = new EffectInfo(1015182, 1017337); // Teleport
            m_Table[22] = new EffectInfo(1015183); // Unlock
            m_Table[23] = new EffectInfo(1015184); // Wall of Stone

            // Fourth circle
            m_Table[24] = new EffectInfo(1015186); // Arch Cure
            m_Table[25] = new EffectInfo(1015187); // Arch Protection
            m_Table[26] = new EffectInfo(1015188, 1017335, "Evil"); // Curse
            m_Table[27] = new EffectInfo(1015189); // Fire Field
            m_Table[28] = new EffectInfo(1015012, 1017330); // Greater Heal
            m_Table[29] = new EffectInfo(1015190, 1060491, "Thunder"); // Lightning
            m_Table[30] = new EffectInfo(1015191, 1017339, "Mage's Bane"); // Mana Drain
            m_Table[31] = new EffectInfo(1015192); // Recall

            // Fifth circle
            m_Table[32] = new EffectInfo(1015194); // Blade Spirits
            m_Table[33] = new EffectInfo(1015195); // Dispel Field
            m_Table[34] = new EffectInfo(1015196); // Incognito
            m_Table[35] = new EffectInfo(1015197, 0, "Reflection"); // Magic Reflection
            m_Table[36] = new EffectInfo(1015198); // Mind Blast
            m_Table[37] = new EffectInfo(1015199, 1017340, "Ghoul's Touch"); // Paralyze
            m_Table[38] = new EffectInfo(1015200); // Poison Field
            m_Table[39] = new EffectInfo(1015201); // Summon Creature

            // Sixth circle
            m_Table[40] = new EffectInfo(1015203); // Dispel
            m_Table[41] = new EffectInfo(1015204); // Energy Bolt
            m_Table[42] = new EffectInfo(1015027); // Explosion
            m_Table[43] = new EffectInfo(1015205, 1017347); // Invisibility
            m_Table[44] = new EffectInfo(1015206); // Mark
            m_Table[45] = new EffectInfo(1015207); // Mass Curse
            m_Table[46] = new EffectInfo(1015208); // Paralyze Field
            m_Table[47] = new EffectInfo(1015209); // Reveal

            // Seventh circle
            m_Table[48] = new EffectInfo(1015211); // Chain Lightning
            m_Table[49] = new EffectInfo(1015212); // Energy Field
            m_Table[50] = new EffectInfo(1015213); // Flamestrike
            m_Table[51] = new EffectInfo(1015214); // Gate Travel
            m_Table[52] = new EffectInfo(1015215); // Mana Vampire
            m_Table[53] = new EffectInfo(1015216); // Mass Dispel
            m_Table[54] = new EffectInfo(1015217); // Meteor Swarm
            m_Table[55] = new EffectInfo(1015218); // Polymorph

            // Eighth circle
            m_Table[56] = new EffectInfo(1015220); // Earthquake
            m_Table[57] = new EffectInfo(1015221); // Energy Vortex
            m_Table[58] = new EffectInfo(1015222); // Resurrection
            m_Table[59] = new EffectInfo(1015223); // Air Elemental
            m_Table[60] = new EffectInfo(1015224); // Summon Daemon
            m_Table[61] = new EffectInfo(1015225); // Earth Elemental
            m_Table[62] = new EffectInfo(1015226); // Fire Elemental
            m_Table[63] = new EffectInfo(1015227); // Water Elemental

            // Other
            m_Table[64] = new EffectInfo(1002094, 1017350, "Identification"); // Item Identification
            m_Table[65] = new EffectInfo(1075867, 1017348, "Restoration"); // Restoration
        }

        private static EffectInfo GetInfo(MagicItemEffect effect)
        {
            int index = (int)effect;

            if (index < 0 || index >= m_Table.Length)
                return null;

            return m_Table[index];
        }

        private class EffectInfo
        {
            public readonly int Label;
            public readonly int ChargeLabel;
            public readonly string OldName;

            public EffectInfo(int label, int chargeLabel = 0, string oldName = null)
            {
                Label = label;
                ChargeLabel = chargeLabel;
                OldName = oldName;
            }
        }

        private class ItemIDTarget : Target
        {
            private Item m_Item;

            public ItemIDTarget(Item item)
                : base(6, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                ConsumeCharge(from, m_Item);

                ItemIdentification.IdentifyItem(from, targeted);
            }
        }

        public static object GetLock(Mobile from, Item item)
        {
#if false
            return item;
#else
            return typeof(BaseWand); // lock ALL magic items
#endif
        }

        public static TimeSpan GetLockDelay(Mobile from, Item item)
        {
            IMagicItem magicItem = item as IMagicItem;

            if (magicItem == null)
                return TimeSpan.Zero;

            if (item is BaseWeapon && !((BaseWeapon)item).HitMagicEffect)
            {
                switch (magicItem.MagicEffect)
                {
                    case MagicItemEffect.Identification:
                        {
                            return TimeSpan.Zero;
                        }
                    default:
                        {
                            return TimeSpan.FromSeconds(4.0);
                        }
                }
            }

            return TimeSpan.Zero;
        }

        public static void BeginLock(Mobile from, object toLock, TimeSpan delay)
        {
            from.BeginAction(toLock);

            (new LockTimer(from, toLock, delay)).Start();
        }

        private class LockTimer : Timer
        {
            private Mobile m_From;
            private object m_Lock;

            public LockTimer(Mobile from, object locked, TimeSpan delay)
                : base(delay)
            {
                m_From = from;
                m_Lock = locked;
            }

            protected override void OnTick()
            {
                m_From.EndAction(m_Lock);
            }
        }
    }
}