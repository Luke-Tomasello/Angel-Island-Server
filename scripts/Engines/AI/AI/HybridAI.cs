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

/* Scripts\Engines\AI\AI\HybridAI.cs
 * ChangeLog
 *	7/11/10, adam
 *		o major reorganization of AI
 *			o push most smart-ai logic from the advanced magery classes down to baseAI so that we can use potions and bandages from 
 *				the new advanced melee class
 *			o remove plasma's preferred focus logic as it pertains to 'memory' .. here we should have simply Remember()'ed the 
 *				ConstantFocus mob instead of all the special cases
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *  12/17/08, Adam
 *		Initial creation
 */

using Server.Spells;
using System;
using static Server.Utility;

namespace Server.Mobiles
{
    public class HybridAI : BaseHybridAI
    {
        public HybridAI(BaseCreature guard)
            : base(guard)
        {

        }

        public override bool DoActionCombat(MobileInfo info)
        {
            bool dac = base.DoActionCombat(info: info);

            if (m_Mobile.Spell == null && DateTime.UtcNow >= m_Mobile.NextSpellTime)
            {
                if (m_Mobile.Spell == null || !(m_Mobile.Spell as Spell).Cast())
                    EquipWeapon();
            }
            else if (m_Mobile.Spell is Spell && ((Spell)m_Mobile.Spell).State == SpellState.Sequencing)
                EquipWeapon();

            return dac;
        }

        public override bool DoActionWander()
        {
            bool daw = base.DoActionWander();

            if (m_Mobile.Spell == null && DateTime.UtcNow >= m_Mobile.NextSpellTime)
            {
                if (m_Mobile.Spell == null || !(m_Mobile.Spell as Spell).Cast())
                    EquipWeapon();
            }
            else if (m_Mobile.Spell is Spell && ((Spell)m_Mobile.Spell).State == SpellState.Sequencing)
                EquipWeapon();

            return daw;
        }

        public override bool PreferMagic()
        {   // if we are a warrior and we have low mana, move in!
            if (IsAllowed(FightStyle.Melee) /*&& m_Mobile.Mana < m_Mobile.ManaMax * .30*/)
                return false;
            else
                return true;
        }

        #region Serialize
        private SaveFlags m_flags;

        [Flags]
        private enum SaveFlags
        {   // 0x00 - 0x800 reserved for version
            unused = 0x1000
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;                                // current version (up to 4095)
            m_flags = m_flags | (SaveFlags)version;         // save the version and flags
            writer.Write((int)m_flags);

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_flags = (SaveFlags)reader.ReadInt();              // grab the version an flags
            int version = (int)(m_flags & (SaveFlags)0xFFF);    // maskout the version

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
            switch (version)
            {
                default: break;
            }

        }
        #endregion Serialize
    }
}