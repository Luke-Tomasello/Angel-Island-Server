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

/* Scripts\Items\Special\KukuiNut.cs
 * Changelog:
 * 12/17/21, Yoar
 *      Now calling "base.OnDroppedToMobile(from, target)".
 * 11/26/21, Yoar
 *      Now overriding Dupe.
 * 11/26/21, Yoar
 *      Now overriding OnDroppedToMobile instead of DropToMobile.
 * 10/22/21, Yoar
 *      Added TC insta-growth functionality.
 * 10/20/21, Yoar: Breeding System overhaul
 *      - Kukui nuts are now stackable.
 *      - Moved eating logic to this file so that it may work on a variety of creatures.
 *      - The number of eaten kukui nuts (0-3) is now serialized on BaseCreature.
 *	05/09/06, Kit
 *		Initial creation limited one time item for reverting a pet from bonded back to unbonded.
 */

using Server.Diagnostics;
using Server.Engines.Breeding;
using Server.Mobiles;
using System;

namespace Server.Items
{
    public class KukuiNut : Item
    {
        public override double DefaultWeight { get { return 1.0; } }

        [Constructable]
        public KukuiNut()
            : this(1)
        {
        }

        [Constructable]
        public KukuiNut(int amount)
            : base(0xF8B)
        {
            Name = "a kukui nut";
            Hue = 541;
            Stackable = true;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new KukuiNut(), amount);
        }

        public override bool OnDroppedToMobile(Mobile from, Mobile target)
        {
            BaseCreature pet = target as BaseCreature;

            if (pet != null && !pet.IsDeadPet && pet.ControlMaster == from && pet.EatsKukuiNuts)
            {
                string message = null;

                if (pet.BreedingParticipant)
                {
                    message = "The magical properties of the kukui nut didn't seem to affect your pet.";

                    if (Core.UOTC_CFG && pet.Maturity >= Maturity.Egg && pet.Maturity <= Maturity.Adult)
                    {
                        BreedingSystem.SetMaturity(pet, (Maturity)((int)pet.Maturity + 1));

                        pet.FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist);

                        message = "Your pet has grown up!";
                    }
                }
                else
                {
                    switch (++pet.KukuiNuts)
                    {
                        case 1: message = "The magical properties of 3 kukui nuts will make your pet fertile."; break;
                        case 2: message = "Fertile pets may be bred, but they also grow older and will become weak in old age."; break;
                        case 3:
                            {
                                message = "Your pet is now fertile.";

                                pet.BreedingParticipant = true;
                                pet.Birthdate = DateTime.UtcNow;
                                pet.Maturity = Maturity.Adult;

                                pet.FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist);

                                // log those that optin so there can be no fibbiing later when their pet gets old
                                LogHelper logger = new LogHelper("DragonBreedingOptIn.log", false);
                                logger.Log(LogType.Mobile, from, "I am opting in to the breeding system.");
                                logger.Log(LogType.Mobile, pet, "My master has opted in to the breeding system.");
                                logger.Finish();

                                break;
                            }
                    }
                }

                if (pet.Body.IsAnimal)
                    pet.Animate(3, 5, 1, true, false, 0);
                else if (pet.Body.IsMonster)
                    pet.Animate(17, 5, 1, true, false, 0);

                pet.SayTo(from, "*Munches on the kukui nut*");

                if (message != null)
                    from.SendMessage(message);

                Consume();

                return this.Deleted; // bounce if *not* deleted
            }

            return base.OnDroppedToMobile(from, target);
        }

        public KukuiNut(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1)
                this.Stackable = true;
        }
    }
}