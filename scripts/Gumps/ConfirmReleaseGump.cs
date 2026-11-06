/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* ChangeLog:
 * 8/30/21, Adam: (pet Release handling)
 *      you shouldn't need a Loyalty check to release a pet. 
 *	7/9/05, Pix
 *		Added kick to pet->Obey() while releasing, so pet always releases.
 *	4/21/04, Adam
 *		1. Remove duplicate implementation of ConfirmReleaseGump and replace with a second constructor.
 *		2. Remove specific knowledge of TreeOfKnowledge and replaced with a generic rangeCheck flag
 *  4/20/05 smerX
 *		Expanded for use with TreeOfKnowledge.cs
 */


using Server.Diagnostics;
using Server.Mobiles;
using System;

namespace Server.Gumps
{
    public class ConfirmReleaseGump : Gump
    {
        private Mobile m_From;
        private BaseCreature m_Pet;
        private bool m_RangeCheck;

        public ConfirmReleaseGump(Mobile from, BaseCreature pet)
            : this(from, pet, true)
        {

        }

        public ConfirmReleaseGump(Mobile from, BaseCreature pet, bool rangeCheck)
            : base(50, 50)
        {
            m_From = from;
            m_Pet = pet;
            m_RangeCheck = rangeCheck;

            m_From.CloseGump(typeof(ConfirmReleaseGump));

            AddPage(0);

            AddBackground(0, 0, 270, 120, 5054);
            AddBackground(10, 10, 250, 100, 3000);

            if (pet.Body.IsHuman)
                AddHtml(20, 15, 230, 60, string.Format("Are you sure you want to release {0}", pet.Name), true, true);
            else
                AddHtmlLocalized(20, 15, 230, 60, 1046257, true, true); // Are you sure you want to release your pet?

            AddButton(20, 80, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 80, 75, 20, 1011011, false, false); // CONTINUE

            AddButton(135, 80, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(170, 80, 75, 20, 1011012, false, false); // CANCEL
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            try
            {
                if (info.ButtonID == 2)
                {   // 8/30/21, Adam: you shouldn't need a Loyalty check to release a pet. 
                    if (!m_Pet.Deleted && m_Pet.Controlled && m_From == m_Pet.ControlMaster && m_From.CheckAlive() /*&& m_Pet.CheckControlChance(m_From)*/)
                    {
                        if (m_Pet.Map == m_From.Map)
                        {
                            if (m_RangeCheck == true && !m_Pet.InRange(m_From, 14))
                            {
                                m_From.SendMessage("You are too far away from your pet.");
                                return;
                            }

                            // no warning here, they're doing something fucked up anyway.
                            // before we release, check to see if this is a malicious act, and...
                            //  if so, flag the owners as criminal.
                            if (ReleasingEvilMonsterRule(m_Pet.ControlMaster, m_Pet))
                            {
                                if (Core.RuleSets.MortalisRules())
                                {
                                    // on mortalis, we just whack the pet.
                                    m_Pet.Region.MakeGuard(m_Pet);
                                }
                                else
                                {
                                    m_Pet.ControlMaster.CriminalAction(message: true); // You've committed a criminal act!!
                                }
                            }

                            if (m_Pet.OnBeforeRelease(m_Pet.ControlMaster))
                            {
                                m_Pet.ControlTarget = null;
                                m_Pet.ControlOrder = OrderType.Release;
                                //Pix: added kick to Obey because when we added the ability to release a 'lost'
                                // pet, if there were no players around, the pet would not release until a player
                                // got in range of the pet.  This wasn't an issue before because there was 
                                // a range check involved so there was always a player around when releasing the pet.
                                if (m_Pet.AIObject != null)
                                    m_Pet.AIObject.Obey();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        public bool ReleasingEvilMonsterRule(Mobile player, Mobile beast)
        {
            // Special pet release rule logic

            if (player == null || beast == null)
                return false;

            Region pr = player.Region;
            Region br = beast.Region;

            // Player must be in a guarded region
            bool playerInGuarded = (pr is Regions.GuardedRegion) && pr.IsGuarded;

            // Beast must be in a guarded region
            bool beastInGuarded = (br is Regions.GuardedRegion) && br.IsGuarded;

            // Must be the same region
            bool sameRegion = (pr != null && pr == br);

            // Must be a creature
            BaseCreature? bc = beast as BaseCreature;
            bool beastIsCreature = (bc != null);

            // Must be alive and "evil/aggressive" per your rule
            bool beastIsEvil = beastIsCreature && bc.Alive && bc.FightMode.HasFlag(FightMode.All);

            if (!playerInGuarded)
                return false;

            if (!beastInGuarded)
                return false;

            if (!sameRegion)
                return false;

            return beastIsEvil;
        }

    }
}