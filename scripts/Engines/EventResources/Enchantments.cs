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

/* Server/Engines/EventResources/Enchantments.cs
 * CHANGELOG:
 *  12/9/23, Yoar
 *      Initial version.
 */

using Server.Items;

namespace Server.Engines.EventResources
{
    public abstract class BaseEnchantment
    {
        private int m_Weight;

        public int Weight { get { return m_Weight; } }

        public BaseEnchantment(int weight)
        {
            m_Weight = weight;
        }

        public abstract bool Validate(Item item);
        public abstract void Enchant(Item item);
    }

    public class SlayerEnchantment : BaseEnchantment
    {
        public SlayerEnchantment(int weight)
            : base(weight)
        {
        }

        public override bool Validate(Item item)
        {
            return (item is BaseWeapon);
        }

        public override void Enchant(Item item)
        {
            if (item is BaseWeapon)
                ((BaseWeapon)item).Slayer = BaseRunicTool.GetRandomSlayer();
        }
    }

    public class MagicEnchantment : BaseEnchantment
    {
        private int m_ChargesMin;
        private int m_ChargesMax;
        private MagicItemEffect[] m_Effects;

        public MagicEnchantment(int weight, int charges, params MagicItemEffect[] effects)
            : this(weight, charges, charges, effects)
        {
        }

        public MagicEnchantment(int weight, int chargesMin, int chargesMax, params MagicItemEffect[] effects)
            : base(weight)
        {
            m_ChargesMin = chargesMin;
            m_ChargesMax = chargesMax;
            m_Effects = effects;
        }

        public override bool Validate(Item item)
        {
            return (item is IMagicItem);
        }

        public override void Enchant(Item item)
        {
            if (item is IMagicItem)
            {
                ((IMagicItem)item).MagicEffect = RandomEffect();
                ((IMagicItem)item).MagicCharges = Utility.RandomMinMax(m_ChargesMin, m_ChargesMax);
            }
        }

        private MagicItemEffect RandomEffect()
        {
            if (m_Effects.Length == 0)
                return MagicItemEffect.None;

            return m_Effects[Utility.Random(m_Effects.Length)];
        }
    }
}