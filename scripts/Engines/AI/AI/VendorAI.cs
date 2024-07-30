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

/* Scripts/Engines/AI/AI/VendorAI.cs
 * CHANGELOG
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Network;
using System;

//
// This is a first simple AI
//
//

namespace Server.Mobiles
{
    public class VendorAI : BaseAI
    {
        public VendorAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool DoActionWander()
        {
            m_Mobile.DebugSay(DebugFlags.AI, "I'm fine");

            if (m_Mobile.Combatant != null)
            {

                m_Mobile.DebugSay(DebugFlags.AI, "{0} is attacking me", m_Mobile.Combatant.Name);

                m_Mobile.Say(Utility.RandomList(1005305, 501603));

                // On siege vendors fight back.
                // In the vendor class we will trap OnActionCombat and switch to Melee AI
                //	we do this switch so as not to rewrite additional complexities into either melee or vendor AIs
                //	We will also trap OnActionWander in the vendor class to return the AI to standard vendor AI
                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisRules())
                {
                    m_Mobile.FocusMob = m_Mobile.Combatant;
                    Action = ActionType.Combat;
                }
                else
                    Action = ActionType.Flee;
            }
            else
            {
                if (m_Mobile.FocusMob != null)
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "{0} has talked to me", m_Mobile.FocusMob.Name);

                    Action = ActionType.Interact;
                }
                else
                {
                    m_Mobile.Warmode = false;

                    base.DoActionWander();
                }
            }

            return true;
        }

        public override bool DoActionInteract()
        {
            Mobile customer = m_Mobile.FocusMob;

            if (m_Mobile.Combatant != null)
            {

                m_Mobile.DebugSay(DebugFlags.AI, "{0} is attacking me", m_Mobile.Combatant.Name);

                m_Mobile.Say(Utility.RandomList(1005305, 501603));

                Action = ActionType.Flee;

                return true;
            }

            if (customer == null || customer.Deleted || customer.Map != m_Mobile.Map)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "My customer has disapeared");
                m_Mobile.FocusMob = null;

                Action = ActionType.Wander;
            }
            else
            {
                if (customer.InRange(m_Mobile, m_Mobile.RangeFight))
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "I am with {0}", customer.Name);

                    m_Mobile.Direction = m_Mobile.GetDirectionTo(customer);
                }
                else
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "{0} is gone", customer.Name);

                    m_Mobile.FocusMob = null;

                    Action = ActionType.Wander;
                }
            }

            return true;
        }

        public override bool DoActionGuard()
        {
            m_Mobile.FocusMob = m_Mobile.Combatant;
            return base.DoActionGuard();
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(m_Mobile, 4))
                return true;

            return base.HandlesOnSpeech(from);
        }

        // Temporary 
        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            Mobile from = e.Mobile;

            if (m_Mobile is BaseVendor && from.InRange(m_Mobile, 4) && !e.Handled)
            {
                if (m_Mobile.Combatant != null)
                {
                    e.Handled = true;

                    // I am too busy fighting to deal with thee!
                    m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                }
                else if (e.HasKeyword(0x14D)) // *vendor sell*
                {
                    e.Handled = true;

                    ((BaseVendor)m_Mobile).VendorSell(from);
                    m_Mobile.FocusMob = from;
                }
                else if (e.HasKeyword(0x3C)) // *vendor buy*
                {
                    e.Handled = true;

                    ((BaseVendor)m_Mobile).VendorBuy(from);
                    m_Mobile.FocusMob = from;
                }
                else if (WasNamed(e.Speech) && (e.HasKeyword(0x171)/*buy*/ || e.HasKeyword(0x177)/*sell*/))
                {
                    e.Handled = true;

                    if (e.HasKeyword(0x177)) // *sell*
                        ((BaseVendor)m_Mobile).VendorSell(from);
                    else if (e.HasKeyword(0x171)) // *buy*
                        ((BaseVendor)m_Mobile).VendorBuy(from);

                    m_Mobile.FocusMob = from;
                }
            }
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