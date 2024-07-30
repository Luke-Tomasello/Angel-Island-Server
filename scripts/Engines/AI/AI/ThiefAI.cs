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

/* Scripts/Engines/AI/AI/ThiefAI.cs
 * CHANGELOG
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *	1/21/05, Pix
 *		Removed code that was leading to an exception.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using System;
using static Server.Utility;

//
// This is a first simple AI
//
//

namespace Server.Mobiles
{
    public class ThiefAI : BaseAI
    {
        public ThiefAI(BaseCreature m)
            : base(m)
        {
        }

        //private Item m_toDisarm;
        public override bool DoActionWander()
        {
            m_Mobile.DebugSay(DebugFlags.AI, "I have no combatant");

            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {

                m_Mobile.DebugSay(DebugFlags.AI, "I have detected {0}, attacking", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
                return true;
            }
            else
            {
                base.DoActionWander();
            }

            return true;
        }

        public override bool DoActionCombat(MobileInfo info)
        {
            /* Pix - commented this out because it leads to a crash in code it calls.
			Mobile combatant = info.combatant;

			if (combatant == null || info.gone || info.dead || info.hidden || info.fled)
			{
				m_Mobile.DebugSay( "My combatant is gone, so my guard is up" );

				Action = ActionType.Guard;

				return true;
			}

			if ( WalkMobileRange( combatant, 1, true, m_Mobile.RangeFight, m_Mobile.RangeFight ) )
			{
				m_Mobile.Direction = m_Mobile.GetDirectionTo( combatant );
				if ( m_toDisarm == null )
				{
					m_toDisarm = combatant.FindItemOnLayer( Layer.OneHanded );
				}
				if ( m_toDisarm == null )
				{
					m_toDisarm = combatant.FindItemOnLayer( Layer.TwoHanded );
				}
				if ( m_toDisarm != null && m_toDisarm.IsChildOf( m_Mobile.Backpack ) )
				{
					m_toDisarm = combatant.FindItemOnLayer( Layer.OneHanded );
					if ( m_toDisarm == null )
					{
						m_toDisarm = combatant.FindItemOnLayer( Layer.TwoHanded );
					}
				}
				if ( !m_Mobile.DisarmReady && m_Mobile.Skills[SkillName.Wrestling].Value >= 80.0 && m_Mobile.Skills[SkillName.ArmsLore].Value >= 80.0 && m_toDisarm != null )
				{
					EventSink.InvokeDisarmRequest( new DisarmRequestEventArgs( m_Mobile ) );
				}
				if ( m_toDisarm != null && m_toDisarm.IsChildOf( combatant.Backpack ) && m_Mobile.NextSkillTime <= DateTime.UtcNow && (m_toDisarm.LootType != LootType.Blessed && m_toDisarm.LootType != LootType.Newbied) )
				{
					m_Mobile.DebugSay( "Trying to steal from combatant." );
					m_Mobile.UseSkill( SkillName.Stealing );
					if ( m_Mobile.Target != null )
					{
						m_Mobile.Target.Invoke( m_Mobile, m_toDisarm );
					}
				}
				else if ( m_toDisarm == null && m_Mobile.NextSkillTime <= DateTime.UtcNow )
				{
					Container cpack = combatant.Backpack;
					Item steala = cpack.FindItemByType( typeof ( Bandage ) );
					if ( steala != null ) 
					{
						m_Mobile.DebugSay( "Trying to steal from combatant." );
						m_Mobile.UseSkill( SkillName.Stealing );
						if ( m_Mobile.Target != null )
						{
							m_Mobile.Target.Invoke( m_Mobile, steala );
						}
					}
					Item stealb = cpack.FindItemByType( typeof ( Nightshade ) );
					if ( stealb != null ) 
					{
					m_Mobile.DebugSay( "Trying to steal from combatant." );
						m_Mobile.UseSkill( SkillName.Stealing );
						if ( m_Mobile.Target != null )
						{
							m_Mobile.Target.Invoke( m_Mobile, stealb );
						}
					}
					Item stealc = cpack.FindItemByType( typeof ( BlackPearl ) );
					if ( stealc != null ) 
					{
						m_Mobile.DebugSay( "Trying to steal from combatant." );
						m_Mobile.UseSkill( SkillName.Stealing );
						if ( m_Mobile.Target != null )
						{
							m_Mobile.Target.Invoke( m_Mobile, stealc );
						}
					}
					Item steald = cpack.FindItemByType( typeof ( MandrakeRoot ) );
					if ( steald != null ) 
					{
						m_Mobile.DebugSay( "Trying to steal from combatant." );
						m_Mobile.UseSkill( SkillName.Stealing );
						if ( m_Mobile.Target != null )
						{
							m_Mobile.Target.Invoke( m_Mobile, steald );
						}
					}
					else if ( steala == null && stealb == null && stealc == null && steald == null )
					{
						if ( m_Mobile.Debug )
							m_Mobile.DebugSay( "I am going to flee from {0}", combatant.Name );

						Action = ActionType.Flee;
					}
				}
			}
			else
			{
				if ( m_Mobile.Debug )
					m_Mobile.DebugSay( "I should be closer to {0}", combatant.Name );
			}

			if ( m_Mobile.Hits < m_Mobile.HitsMax * 20/100 )
			{
				// We are low on health, should we flee?

				bool flee = false;

				if ( m_Mobile.Hits < combatant.Hits )
				{
					// We are more hurt than them

					int diff = combatant.Hits - m_Mobile.Hits;

					flee = ( Utility.Random( 0, 100 ) > (10 + diff) ); // (10 + diff)% chance to flee
				}
				else
				{
					flee = Utility.Random( 0, 100 ) > 10; // 10% chance to flee
				}

				if ( flee )
				{
					if ( m_Mobile.Debug )
						m_Mobile.DebugSay( "I am going to flee from {0}", combatant.Name );

					Action = ActionType.Flee;
				}
			}

			return true;
			*/
            return base.DoActionCombat(info: info);
        }

        public override bool DoActionGuard()
        {
            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {

                m_Mobile.DebugSay(DebugFlags.AI, "I have detected {0}, attacking", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
            }
            else
            {
                base.DoActionGuard();
            }

            return true;
        }

        public override bool DoActionFlee()
        {
            if (m_Mobile.Hits > m_Mobile.HitsMax / 2)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am stronger now, so I will continue fighting");
                Action = ActionType.Combat;
            }
            else
            {
                m_Mobile.FocusMob = m_Mobile.Combatant;
                base.DoActionFlee();
            }

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