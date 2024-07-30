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

/*
 * Scripts/Gumps/PetRessurectGump.cs
 * CHANGE LOG
 *  9/17/21 - Yoar
 *	    Added SuffersSkillLoss, CheckApplySkillLoss methods to deal with
 *	    pet skill loss in other situations.
 *	10/7/04 - Pixie
 *		Added warning message to pet owner for when the pet will take
 *		skill loss on resurrection.
// 5/25/2004 - Pulse
//		Changed OnResponse() to reduce a ressurected pet's skills by 10%
//		Prior to this change the skills were reduced .1  if the owner resd 
//		and .2 if resd by someone else.
*/

using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Gumps
{
    public class PetResurrectGump : Gump
    {
        private BaseCreature m_Pet;

        public PetResurrectGump(Mobile from, BaseCreature pet)
            : base(50, 50)
        {
            from.CloseGump(typeof(PetResurrectGump));

            bool bStatLoss = SuffersSkillLoss(pet);

            m_Pet = pet;

            AddPage(0);

            AddBackground(10, 10, 265, bStatLoss ? 250 : 140, 0x242C);

            AddItem(205, 40, 0x4);
            AddItem(227, 40, 0x5);

            AddItem(180, 78, 0xCAE);
            AddItem(195, 90, 0xCAD);
            AddItem(218, 95, 0xCB0);

            AddHtmlLocalized(30, 30, 150, 75, 1049665, false, false); // <div align=center>Wilt thou sanctify the resurrection of:</div>
            AddHtml(30, 70, 150, 25, String.Format("<div align=CENTER>{0}</div>", pet.Name), true, false);

            if (bStatLoss)
                AddHtml(30, 105, 150, 105, String.Format("<div align=CENTER>{0}</div>", SkillLossWarning), true, false);

            AddButton(40, bStatLoss ? 215 : 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
            AddButton(110, bStatLoss ? 215 : 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_Pet.Deleted || !m_Pet.IsBonded || !m_Pet.IsDeadPet)
                return;

            Mobile from = state.Mobile;

            if (info.ButtonID == 1)
            {
                if (m_Pet.Map == null || !Utility.CanFit(m_Pet.Map, m_Pet.Location, 16, Utility.CanFitFlags.requireSurface))
                {
                    from.SendLocalizedMessage(503256); // You fail to resurrect the creature.
                    return;
                }

                m_Pet.PlaySound(0x214);
                m_Pet.FixedEffect(0x376A, 10, 16);
                m_Pet.ResurrectPet();

                CheckApplySkillLoss(m_Pet);
            }
        }

        public static double SkillLossPerc = 0.1; // reduce all skills on the pet by 10%
        public static string SkillLossWarning = "Your pet lacks the ability to return to the living without suffering skill loss at this time.";

        public static bool SuffersSkillLoss(BaseCreature pet)
        {
            return pet.BondedDeadPetStatLossTime > DateTime.UtcNow;
        }

        public static void CheckApplySkillLoss(BaseCreature pet)
        {
            //double decreaseAmount;

            //if (from == m_Pet.ControlMaster)
            //    decreaseAmount = 0.1;
            //else
            //    decreaseAmount = 0.2;

            //for (int i = 0; i < m_Pet.Skills.Length; ++i)   //Decrease all skills on pet.
            //    m_Pet.Skills[i].Base -= decreaseAmount;

            if (SuffersSkillLoss(pet))
            {
                for (int i = 0; i < pet.Skills.Length; i++)
                    pet.Skills[i].Base -= (pet.Skills[i].Base * SkillLossPerc);
            }
        }
    }
}