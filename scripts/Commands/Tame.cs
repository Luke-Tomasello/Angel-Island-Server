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

/* Scripts/Commands/Tame.cs
 * Changelog
 *  4/10/10, Adam
 *		Initial version.
 */
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    class Tame
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Tame", AccessLevel.GameMaster, new CommandEventHandler(Tame_OnCommand));
        }

        public static void Tame_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the creature to tame.");
            e.Mobile.Target = new TameFemaleTarget();
        }

        public class TameFemaleTarget : Target
        {
            public TameFemaleTarget()
                : base(10, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                BaseCreature pet = targeted as BaseCreature;

                if (pet != null)
                {
                    if (pet.ControlMaster != null)
                    {
                        from.SendMessage("That creature is already tame.");
                        return;
                    }
                    if (pet.Tamable == false && pet is not Golem)
                    {
                        from.SendMessage("that creature cannot be tamed.");
                        return;
                    }
                    pet.ControlMaster = from;
                    pet.Controlled = true;
                    pet.ControlOrder = OrderType.Follow;
                    from.SendMessage(string.Format("That creature had no choice but to accept you as {0} master.", pet.Female ? "her" : "his"));
                }
            }
        }
    }
}