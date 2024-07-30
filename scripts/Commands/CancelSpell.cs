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

/* Scripts/Commands/CancelSpell.cs
*	ChangeLog:
*	4/17/04 Creation by smerX
*		Created to provide easier access to certain game features
*/
using Server.Spells;

namespace Server.Commands
{
    public class CancelSpell
    {

        public static void Initialize()
        {
            Server.CommandSystem.Register("CancelSpell", AccessLevel.Player, new CommandEventHandler(CancelSpell_OnCommand));
        }

        [Usage("CancelSpell")]
        [Description("Cancels the spell currently being cast.")]
        private static void CancelSpell_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            ISpell i = m.Spell;

            if (i != null && i.IsCasting)
            {
                Spell s = (Spell)i;
                s.Disturb(DisturbType.EquipRequest, true, false);
                m.SendMessage("You snap yourself out of concentration.");
                m.FixedEffect(0x3735, 6, 30);
                return;
            }

            else
            {
                m.SendMessage("You must be casting a spell to use this feature.");
                return;
            }

        }
    }
}