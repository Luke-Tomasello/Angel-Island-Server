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

/* Scripts/Commands/Breed.cs
 * Changelog
 *  10/20/21, Yoar: Breeding System overhaul
 *      Repurposed the [breed command for the updated Breeding System:
 *      It's now a GM command only and it breeds any two targeted creatures.
 *  4/4/07, Adam
 *      comment out the registration for [breed
 *	12/07/06 Taran Kain
 *		Initial version.
 */

using Server.Engines.Breeding;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public static class BreedCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("Breed", AccessLevel.GameMaster, new CommandEventHandler(Breed_OnCommand));
        }

        [Usage("Breed")]
        [Description("Breeds two creatures.")]
        private static void Breed_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the first parent.");
            e.Mobile.Target = new BreedFirstTarget();
        }

        private class BreedFirstTarget : Target
        {
            public BreedFirstTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                BaseCreature bc = targeted as BaseCreature;

                if (bc == null)
                {
                    from.SendMessage("You must select an animal to breed!");
                }
                else
                {
                    from.SendMessage("Target the second parent.");
                    from.Target = new BreedSecondTarget(bc);
                }
            }
        }

        private class BreedSecondTarget : Target
        {
            private BaseCreature m_First;

            public BreedSecondTarget(BaseCreature first)
                : base(-1, false, TargetFlags.None)
            {
                m_First = first;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                BaseCreature bc = targeted as BaseCreature;

                if (bc == null)
                {
                    from.SendMessage("You must select an animal to breed!");
                }
                else
                {
                    if (!m_First.Female && bc.Female)
                    {
                        BaseCreature temp = m_First;

                        m_First = bc;
                        bc = temp;
                    }

                    BreedingSystem.MatedWith(m_First, bc, false);
                }
            }
        }
    }
}