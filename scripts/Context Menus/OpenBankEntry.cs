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

/* ChangeLog
	*	01/03/07, plasma
 *		Remove all duel challenge system code
 *	10/15/07, Pix
 *		Fixed expired AggressorInfos from preventing banking.
 *	8/26/07, Pix
 *		Prevent duelers from accessing their bankbox.
 * 01/05/05, Darva
 * Added code to prevent players from opening their bank box for 2 minutes after
 * 	Any successful steal.
 *
*/

using Server.Items;
namespace Server.ContextMenus
{
    public class OpenBankEntry : ContextMenuEntry
    {
        private Mobile m_Banker;

        public OpenBankEntry(Mobile from, Mobile banker)
            : base(6105, 12)
        {
            m_Banker = banker;
        }

        public override void OnClick()
        {
            if (!Owner.From.CheckAlive())
                return;

            if (Owner.From.Criminal)
            {
                m_Banker.Say(500378); // Thou art a criminal and cannot access thy bank box.
            }
            else if (Server.SkillHandlers.Stealing.HasHotItem(Owner.From))
            {
                m_Banker.Say("Thou'rt a thief and cannot access thy bank box.");
            }
            else if (m_Banker.IsAggressor(Owner.From, player: true))
            {
                m_Banker.Say("Thou seemest too busy fighting and cannot access thy bank box");
            }
            else
            {
                BankBox box = this.Owner.From.BankBox;

                if (box != null)
                    box.Open();
            }
        }
    }
}