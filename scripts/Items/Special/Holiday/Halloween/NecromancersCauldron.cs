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

/* Scripts/Items/Special/Holiday/Halloween/NecromancersCauldron.cs
 * CHANGELOG
 *  12/11/23, Yoar
 *      Added Pagen ingredients + recipes
 *  10/18/23, Yoar
 *      Initial commit.
 */

using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    [Flipable(0x974, 0x975)]
    public class NecromancersCauldron : Item
    {
        private IngredientFlag m_Ingredients;
        private Item m_Liquid;
        private Timer m_CookTimer;
        private Timer m_DrinkTimer;

        [CommandProperty(AccessLevel.GameMaster)]
        public IngredientFlag Ingredients
        {
            get { return m_Ingredients; }
            set { m_Ingredients = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Liquid
        {
            get { return m_Liquid; }
            set { m_Liquid = value; }
        }

        [Constructable]
        public NecromancersCauldron()
            : base(0x975)
        {
            Movable = false;

            m_Liquid = new InternalItem(this);
            m_Liquid.Z = 8;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Liquid != null && !m_Liquid.Visible)
            {
                uint mask = (uint)m_Ingredients;

                for (int i = 0; mask != 0 && i < 32; i++, mask >>= 1)
                {
                    if ((mask & 0x1) != 0)
                    {
                        IngredientInfo info = IngredientInfo.Lookup((IngredientFlag)(0x1 << i));

                        if (info != null)
                            LabelTo(from, info.Name);
                    }
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            else if (CountIngredients() < 2)
                BeginAddIngredient(from);
            else if (m_Liquid != null && !m_Liquid.Visible && m_CookTimer == null)
                BeginCook(from);
        }

        private void BeginAddIngredient(Mobile from)
        {
            from.SendMessage("Target an ingredient to add to the cauldron or target the cauldron to empty it.");
            from.Target = new IngredientTarget(this);
        }

        private void BeginCook(Mobile from)
        {
            Effects.PlaySound(GetWorldLocation(), Map, 0x20);

            if (m_CookTimer != null)
                m_CookTimer.Stop();

            (m_CookTimer = new CookTimer(this, from)).Start();
        }

        private void EndCook(Mobile from)
        {
            if (m_CookTimer != null)
            {
                m_CookTimer.Stop();
                m_CookTimer = null;
            }

            if (m_Liquid != null)
                m_Liquid.Visible = true;

            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You cook up a foul concoction.");
        }

        private void OnDrink(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            else if (GetEffect(from) != EffectType.None)
                from.SendLocalizedMessage(502173); // You are already under a similar effect.
            else if (m_DrinkTimer == null)
                BeginDrink(from);
        }

        private void BeginDrink(Mobile from)
        {
            from.PlaySound(0x30);

            if (m_DrinkTimer != null)
                m_DrinkTimer.Stop();

            (m_DrinkTimer = new DrinkTimer(this, from)).Start();
        }

        private void EndDrink(Mobile from)
        {
            if (m_DrinkTimer != null)
            {
                m_DrinkTimer.Stop();
                m_DrinkTimer = null;
            }

            EffectType effect = RecipeInfo.GetEffect(m_Ingredients);

            if (effect == EffectType.None)
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "These ingredients don't mix very well!");
                from.PlaySound(from.Female ? 0x334 : 0x446);
            }
            else
            {
                BeginEffect(from, effect);

                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You feel... Different.");
                from.FixedParticles(0x373A, 1, 15, 9913, 1157, 7, EffectLayer.Head);
                from.PlaySound(0x1E1);
            }

            m_Ingredients = IngredientFlag.None;

            if (m_Liquid != null)
                m_Liquid.Visible = false;
        }

        private int CountIngredients()
        {
            int count = 0;
            uint mask = (uint)m_Ingredients;

            for (int i = 0; mask != 0 && i < 32; i++, mask >>= 1)
            {
                if ((mask & 0x1) != 0)
                    count++;
            }

            return count;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            if (m_Liquid != null)
                m_Liquid.Location += Location - oldLocation;
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (m_Liquid != null)
                m_Liquid.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Liquid != null)
                m_Liquid.Delete();
        }

        private class IngredientTarget : Target
        {
            private NecromancersCauldron m_Cauldron;

            public IngredientTarget(NecromancersCauldron cauldron)
                : base(-1, false, TargetFlags.None)
            {
                m_Cauldron = cauldron;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.InRange(m_Cauldron.GetWorldLocation(), 2) || !from.InLOS(m_Cauldron))
                    return;

                if (targeted == m_Cauldron)
                {
                    if (m_Cauldron.Ingredients != IngredientFlag.None)
                    {
                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You empty the cauldron.");
                        m_Cauldron.Ingredients = IngredientFlag.None;
                    }
                }
                else
                {
                    Item item = targeted as Item;

                    IngredientFlag flag = IngredientInfo.FindByType(targeted.GetType());
                    IngredientInfo info = IngredientInfo.Lookup(flag);

                    if (item == null || flag == IngredientFlag.None || info == null)
                    {
                        from.SendMessage("You can't add that to the cauldron!");
                    }
                    else if (!item.IsChildOf(from.Backpack))
                    {
                        from.SendMessage("That must be in your pack for you to use it.");
                    }
                    else if (m_Cauldron.Ingredients.HasFlag(flag))
                    {
                        from.SendMessage("That ingredient has already been added to the cauldron.");
                    }
                    else if (m_Cauldron.CountIngredients() >= 2)
                    {
                        from.SendMessage("The cauldron has two ingredients in it already.");
                    }
                    else
                    {
                        m_Cauldron.Ingredients |= flag;
                        item.Consume();

                        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, String.Format("You add one {0} to the cauldron.", info.Name));
                        Effects.PlaySound(m_Cauldron.GetWorldLocation(), m_Cauldron.Map, 0x42);

                        if (m_Cauldron.CountIngredients() < 2)
                            m_Cauldron.BeginAddIngredient(from);
                    }
                }
            }
        }

        private class CookTimer : Timer
        {
            private NecromancersCauldron m_Cauldron;
            private Mobile m_From;

            public CookTimer(NecromancersCauldron cauldron, Mobile from)
                : base(TimeSpan.FromSeconds(3.0))
            {
                m_Cauldron = cauldron;
                m_From = from;
            }

            protected override void OnTick()
            {
                m_Cauldron.EndCook(m_From);
            }
        }

        private class DrinkTimer : Timer
        {
            private NecromancersCauldron m_Cauldron;
            private Mobile m_From;

            public DrinkTimer(NecromancersCauldron cauldron, Mobile from)
                : base(TimeSpan.FromSeconds(2.0))
            {
                m_Cauldron = cauldron;
                m_From = from;
            }

            protected override void OnTick()
            {
                m_Cauldron.EndDrink(m_From);
            }
        }

        private class InternalItem : Item
        {
            public override string DefaultName { get { return "foul concoction"; } }

            private NecromancersCauldron m_Cauldron;

            public InternalItem(NecromancersCauldron cauldron)
                : base(0x970)
            {
                Hue = 1158;
                Movable = false;
                Visible = false;

                m_Cauldron = cauldron;
            }

            public override void OnDoubleClick(Mobile from)
            {
                if (!Visible || m_Cauldron == null)
                    return;

                m_Cauldron.OnDrink(from);
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version

                writer.Write((Item)m_Cauldron);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                m_Cauldron = reader.ReadItem() as NecromancersCauldron;
            }
        }

        public NecromancersCauldron(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((uint)m_Ingredients);
            writer.Write((Item)m_Liquid);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Ingredients = (IngredientFlag)reader.ReadUInt();
            m_Liquid = reader.ReadItem();
        }

        private class RecipeInfo
        {
            private static readonly RecipeInfo[] m_Table = new RecipeInfo[]
                {
                    /* Necro Recipes */

                    new RecipeInfo(EffectType.PoisonResistance, IngredientFlag.NoxCrystal | IngredientFlag.PigIron),

                    new RecipeInfo(EffectType.StatBuffStr, IngredientFlag.GraveDust | IngredientFlag.PigIron),
                    new RecipeInfo(EffectType.StatBuffDex, IngredientFlag.GraveDust | IngredientFlag.Batwing),
                    new RecipeInfo(EffectType.StatBuffInt, IngredientFlag.GraveDust | IngredientFlag.DaemonBlood),

                    new RecipeInfo(EffectType.SkillBuffTactics, IngredientFlag.DaemonBlood | IngredientFlag.Batwing),
                    new RecipeInfo(EffectType.SkillBuffEvalInt, IngredientFlag.DaemonBlood | IngredientFlag.NoxCrystal),

                    /* Pagan Recipes */

                    new RecipeInfo(EffectType.PoisonResistance, IngredientFlag.Blackmoor | IngredientFlag.WyrmsHeart),

                    new RecipeInfo(EffectType.StatBuffStr, IngredientFlag.Pumice | IngredientFlag.Brimstone),
                    new RecipeInfo(EffectType.StatBuffDex, IngredientFlag.Pumice | IngredientFlag.Blackmoor),
                    new RecipeInfo(EffectType.StatBuffInt, IngredientFlag.Pumice | IngredientFlag.EyeOfNewt),

                    new RecipeInfo(EffectType.SkillBuffTactics, IngredientFlag.DragonsBlood | IngredientFlag.Pumice),
                    new RecipeInfo(EffectType.SkillBuffEvalInt, IngredientFlag.DragonsBlood | IngredientFlag.BloodSpawn),
                };

            public static RecipeInfo[] Table { get { return m_Table; } }

            public static EffectType GetEffect(IngredientFlag ingredients)
            {
                for (int i = 0; i < m_Table.Length; i++)
                {
                    if (m_Table[i].Ingredients == ingredients)
                        return m_Table[i].Effect;
                }

                return EffectType.None;
            }

            private EffectType m_Effect;
            private IngredientFlag m_Ingredients;

            public EffectType Effect { get { return m_Effect; } }
            public IngredientFlag Ingredients { get { return m_Ingredients; } }

            public RecipeInfo(EffectType effect, IngredientFlag ingredients)
            {
                m_Effect = effect;
                m_Ingredients = ingredients;
            }
        }

        [Flags]
        public enum IngredientFlag : uint
        {
            None = 0x0000,

            // necro regs
            Batwing = 0x0001,
            DaemonBlood = 0x0002,
            GraveDust = 0x0004,
            NoxCrystal = 0x0008,
            PigIron = 0x0010,

            // pagan regs
            Blackmoor = 0x0020,
            BloodSpawn = 0x0040,
            Brimstone = 0x0080,
            DragonsBlood = 0x0100,
            EyeOfNewt = 0x0200,
            Obsidian = 0x0400,
            Pumice = 0x0800,
            WyrmsHeart = 0x1000,
        }

        private class IngredientInfo
        {
            private static readonly IngredientInfo[] m_Table = new IngredientInfo[]
                {
                    // necro regs
                    new IngredientInfo(typeof(BatWing), "batwing"),
                    new IngredientInfo(typeof(DaemonBlood), "daemon blood"),
                    new IngredientInfo(typeof(GraveDust), "grave dust"),
                    new IngredientInfo(typeof(NoxCrystal), "nox crystal"),
                    new IngredientInfo(typeof(PigIron), "pig iron"),

                    // pagan regs
                    new IngredientInfo(typeof(Blackmoor), "blackmoor"),
                    new IngredientInfo(typeof(BloodSpawn), "blood spawn"),
                    new IngredientInfo(typeof(Brimstone), "brimstone"),
                    new IngredientInfo(typeof(DragonsBlood), "dragon's blood"),
                    new IngredientInfo(typeof(EyeOfNewt), "eye of newt"),
                    new IngredientInfo(typeof(Obsidian), "obsidian"),
                    new IngredientInfo(typeof(Pumice), "pumice"),
                    new IngredientInfo(typeof(WyrmsHeart), "wyrm's heart"),
                };

            public static IngredientInfo[] Table { get { return m_Table; } }

            public static IngredientInfo Lookup(IngredientFlag reagent)
            {
                uint mask = (uint)reagent;

                for (int i = 0; mask != 0 && i < 32 && i < m_Table.Length; i++, mask >>= 1)
                {
                    if ((mask & 0x1) != 0)
                        return m_Table[i];
                }

                return null;
            }

            public static IngredientFlag FindByType(Type itemType)
            {
                if (itemType == null)
                    return IngredientFlag.None;

                for (int i = 0; i < m_Table.Length; i++)
                {
                    if (m_Table[i].ItemType == itemType)
                        return (IngredientFlag)(0x1 << i);
                }

                return IngredientFlag.None;
            }

            private Type m_ItemType;
            private string m_Name;

            public Type ItemType { get { return m_ItemType; } }
            public string Name { get { return m_Name; } }

            public IngredientInfo(Type itemType, string name)
            {
                m_ItemType = itemType;
                m_Name = name;
            }
        }

        public enum EffectType
        {
            None,

            PoisonResistance,

            StatBuffStr,
            StatBuffDex,
            StatBuffInt,

            SkillBuffTactics,
            SkillBuffEvalInt,
        }

        public static TimeSpan EffectDuration = TimeSpan.FromMinutes(10.0);

        private abstract class EffectInfo
        {
            private static readonly EffectInfo[] m_Table = new EffectInfo[]
                {
                    new DummyEffect(),

                    new DummyEffect(),

                    new StatEffect(StatType.Str, +10),
                    new StatEffect(StatType.Dex, +10),
                    new StatEffect(StatType.Int, +10),

                    new SkillEffect(SkillName.Tactics, +20.0),
                    new SkillEffect(SkillName.EvalInt, +20.0),
                };

            public static EffectInfo[] Table { get { return m_Table; } }

            public static EffectInfo Lookup(EffectType effect)
            {
                int index = (int)effect;

                if (index >= 0 && index < m_Table.Length)
                    return m_Table[index];

                return null;
            }

            public EffectInfo()
            {
            }

            public abstract void BeginEffect(Mobile m);

            public abstract void EndEffect(Mobile m);
        }

        private class DummyEffect : EffectInfo
        {
            public DummyEffect()
                : base()
            {
            }

            public override void BeginEffect(Mobile m)
            {
            }

            public override void EndEffect(Mobile m)
            {
            }
        }

        private class StatEffect : EffectInfo
        {
            private StatType m_Stat;
            private int m_Value;

            public StatEffect(StatType stat, int value)
                : base()
            {
                m_Stat = stat;
                m_Value = value;
            }

            public override void BeginEffect(Mobile m)
            {
                m.AddStatMod(new StatMod(m_Stat, String.Format("NecroCauldron [{0}]", m_Stat.ToString()), m_Value, TimeSpan.Zero));
            }

            public override void EndEffect(Mobile m)
            {
                m.RemoveStatMod(String.Format("NecroCauldron [{0}]", m_Stat.ToString()));
            }
        }

        private class SkillEffect : EffectInfo
        {
            private SkillName m_Skill;
            private double m_Value;

            public SkillEffect(SkillName skill, double value)
                : base()
            {
                m_Skill = skill;
                m_Value = value;
            }

            public override void BeginEffect(Mobile m)
            {
                m.AddSkillMod(new InternalSkillMod(m_Skill, true, m_Value));
            }

            public override void EndEffect(Mobile m)
            {
                List<SkillMod> toRemove = new List<SkillMod>();

                foreach (SkillMod mod in m.SkillMods)
                {
                    if (mod.Skill == m_Skill && mod.GetType() == typeof(InternalSkillMod))
                        toRemove.Add(mod);
                }

                foreach (SkillMod mod in toRemove)
                    m.RemoveSkillMod(mod);
            }

            private sealed class InternalSkillMod : DefaultSkillMod
            {
                public InternalSkillMod(SkillName skill, bool relative, double value)
                    : base(skill, relative, value)
                {
                }
            }
        }

        private static readonly Dictionary<Mobile, EffectTimer> m_EffectTimers = new Dictionary<Mobile, EffectTimer>();

        public static Dictionary<Mobile, EffectTimer> EffectTimers { get { return m_EffectTimers; } }

        public static void BeginEffect(Mobile m, EffectType effect)
        {
            EndEffect(m);

            EffectInfo info = EffectInfo.Lookup(effect);

            if (info != null)
                info.BeginEffect(m);

            (m_EffectTimers[m] = new EffectTimer(m, effect)).Start();
        }

        public static void EndEffect(Mobile m)
        {
            EffectTimer timer;

            if (m_EffectTimers.TryGetValue(m, out timer))
            {
                EffectInfo info = EffectInfo.Lookup(timer.Effect);

                if (info != null)
                    info.EndEffect(m);

                timer.Stop();
                m_EffectTimers.Remove(m);
            }
        }

        public static EffectType GetEffect(Mobile m)
        {
            EffectTimer timer;

            if (m_EffectTimers.TryGetValue(m, out timer))
                return timer.Effect;

            return EffectType.None;
        }

        public class EffectTimer : Timer
        {
            private Mobile m_Mobile;
            private EffectType m_Effect;

            public EffectType Effect { get { return m_Effect; } }

            public EffectTimer(Mobile m, EffectType effect)
                : base(EffectDuration)
            {
                m_Mobile = m;
                m_Effect = effect;
            }

            protected override void OnTick()
            {
                EndEffect(m_Mobile);
            }
        }
    }
}