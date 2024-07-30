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

/* Scripts/Engines/AI/AI/CouncilMemberAI.cs
 * ChangeLog:
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *  8/13/06, Kit
 *		Update to use new DmgDoesntSlowMovement call vs old DamageSlowsMovement setting.
 *	6/01/05 Kit
 *	ported councilAI to new system.
 */


using Server.Spells;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.NPC;
using System;
using static Server.Utility;

namespace Server.Mobiles
{
    public class CouncilMemberAI : HumanMageAI
    {

        private TimeSpan TimeBetweenCouncilSpell
        {
            get
            {
                return TimeSpan.FromSeconds(Utility.Random(5, 7));
            }
        }

        private DateTime m_NextCouncilSpell = DateTime.UtcNow;

        public CouncilMemberAI(BaseCreature m)
            : base(m)
        {
            CanRunAI = false;
            UsesPotions = false; //CanDrinkPots = false;
        }

        public override bool SmartAI
        {
            get { return false; }
        }

        public bool CastPoisonWave()
        {
            IPooledEnumerable eable = m_Mobile.GetMobilesInRange(4);
            foreach (Mobile m in eable)
            {
                if (m != null && m is PlayerMobile && m.Alive && m.AccessLevel == AccessLevel.Player && !m.Hidden && m.Poison == null)
                {
                    PlayerMobile pm = (PlayerMobile)m;
                    if (pm.IOBEquipped && pm.IOBAlignment == IOBAlignment.Council)
                        return false;

                    return true;
                }
            }
            eable.Free();

            return false;
        }

        public bool CastRevelationWave()
        {
            IPooledEnumerable eable = m_Mobile.GetMobilesInRange(4);
            foreach (Mobile m in eable)
            {
                if (m != null && m is PlayerMobile && m.Alive && m.AccessLevel == AccessLevel.Player && m.Hidden)
                {
                    PlayerMobile pm = (PlayerMobile)m;
                    if (pm.IOBEquipped && pm.IOBAlignment == IOBAlignment.Council)
                        return false;

                    return true;
                }
            }
            eable.Free();

            return false;
        }


        public override Spell ChooseSpell(Mobile c)
        {
            // tamable solution ---------------
            Mobile com = m_Mobile.Combatant;
            Spell spell = null;

            if (com != null && com is BaseCreature && DateTime.UtcNow >= m_NextCouncilSpell)
            {
                if (CastRevelationWave())
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I'm gunna cast Revelation Wave!");
                    m_NextCouncilSpell = DateTime.UtcNow + TimeBetweenCouncilSpell;
                    return new RevelationWaveSpell(m_Mobile, null);
                }

                if (CastPoisonWave())
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I'm gunna cast Poison Wave!");
                    m_NextCouncilSpell = DateTime.UtcNow + TimeBetweenCouncilSpell;
                    return new PoisonWaveSpell(m_Mobile, null);
                }
            }
            // end tamable solution ---------------		

            if (!SmartAI)
            {
                if (!m_Mobile.Summoned && ScaleByMagery(HealChance) > Utility.RandomDouble())
                {
                    if (m_Mobile.Hits < (m_Mobile.HitsMax - 50))
                        return new GreaterHealSpell(m_Mobile, null);
                    else if (m_Mobile.Hits < (m_Mobile.HitsMax - 10))
                        return new HealSpell(m_Mobile, null);
                }

                return GetRandomDamageSpell(c);
            }

            return spell;
        }

        public override bool DoActionCombat(MobileInfo info)
        {
            base.DoActionCombat(info: info);
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