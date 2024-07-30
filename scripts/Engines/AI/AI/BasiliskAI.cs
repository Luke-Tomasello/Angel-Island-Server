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

/* Scripts/Engines/AI/AI/BasiliskAI.cs
 * CHANGELOG
 *  9/28/21, Adam
 *      Created.
 *      Overview:
 *      A Basilisk is a familiar
 *      Scenario: familiars intuit their masters wishes. Example: when the Basilisk (a familiar)
 *      is following her master, and her master hides, the basilisk will also hide.
 */

using System;
using static Server.Utility;

namespace Server.Mobiles
{
    public class BasiliskAI : MageAI
    {

        public BasiliskAI(BaseCreature m)
            : base(m)
        {
        }

        public bool IsFamiliar { get { return m_Mobile != null && !m_Mobile.Deleted && m_Mobile.Controlled && m_Mobile.ControlMaster != null && m_Mobile.IsBonded; } }

        public override void OnMasterUseSkill(SkillName name)
        {
            if (IsFamiliar && m_Mobile.ControlOrder == OrderType.Follow)
            {
                if (name == SkillName.Hiding)
                    m_Mobile.Hidden = m_Mobile.ControlMaster.Hidden;
            }
            return;
        }
        public override void OnMasterUseSpell(Type name)
        {
            if (IsFamiliar && m_Mobile.ControlOrder == OrderType.Follow)
            {
                if (name.Name == "InvisibilitySpell")
                    m_Mobile.Hidden = m_Mobile.ControlMaster.Hidden;
            }
            return;
        }

        public override bool DoOrderCome()
        {
            return base.DoOrderCome();
        }
        public override bool DoActionWander()
        {
            return base.DoActionWander();
        }
        public override bool DoActionCombat(MobileInfo info)
        {
            return base.DoActionCombat(info: info);
        }
        public override bool DoActionGuard()
        {
            base.DoActionGuard();
            return true;
        }
        public override bool DoActionFlee()
        {
            return base.DoActionFlee();
        }

        #region Serialize
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            // this base class was inserted, so no serialization
            //  if you need to serialize data, consider doing it in the derived class
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
        #endregion Serialize
    }
}