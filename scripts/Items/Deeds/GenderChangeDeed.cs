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

/* Items/Deeds/GenderChangeDeed.cs
 * ChangeLog:
 *	11/16/04 Darva
 *		Created file
 *		Made it change your gender when double clicked, removing all facial hair.
 */

namespace Server.Items
{
    public class GenderChangeDeed : Item
    {
        [Constructable]
        public GenderChangeDeed()
            : base(0x14F0)
        {
            base.Weight = 1.0;
            base.Name = "a gender change deed";
        }

        public GenderChangeDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); //This must be in your backpack
            }
            else if (from.BodyMod != 0)
            {
                from.SendMessage("You must be in your normal form to change your gender.");
            }
            else
            {

                Body body;
                if (from.Female == false)
                {
                    body = new Body(401);
                }
                else
                {
                    body = new Body(400);
                }
                from.Body = body;
                from.Female = !from.Female;
                if (from.Beard != null)
                    from.Beard.Delete();
                from.SendMessage("Your gender has been changed.");
                BaseArmor.ValidateMobile(from);
                this.Delete();
            }

        }
    }
}