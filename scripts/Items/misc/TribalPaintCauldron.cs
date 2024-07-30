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

/* Scripts/Items/Misc/TribalPaintCauldron.cs
 * Changelog
 *  6/18/2023, Adam
 *      created. 
 *      Allows Savage aligned to have easy access to paint for role play purposes
 */
using Server.Mobiles;
using System;

namespace Server.Items
{
    public class TribalPaintCauldron : BaseAddon
    {
        public override int LabelNumber { get { return 1040000; } } // savage kin paint

        [Constructable]
        public TribalPaintCauldron()
            : base()
        {
            ItemID = 0x975; // Cauldron
            Movable = false;
            Visible = true;
            // paint
            AddonComponent paint = new AddonComponent(0x970);
            paint.Hue = 2101;
            paint.Movable = false;
            paint.Visible = true;
            paint.Name = "tribal paint";
            AddComponent(paint, 0, 0, 8);
        }

        public TribalPaintCauldron(Serial serial)
            : base(serial)
        {
        }
        public override void OnComponentDoubleClick(AddonComponent c, Mobile from)
        {
            OnDoubleClick(from);
        }
        public override void OnDoubleClick(Mobile from)
        {
            PlayerMobile pm = (from as PlayerMobile);
            if (pm != null && pm.InRange(this.Location, 2))
                if (pm.GuildAlignment == Engines.Alignment.AlignmentType.Savage)
                {
                    if (Factions.Sigil.ExistsOn(from))
                    {
                        from.SendLocalizedMessage(1010465); // You cannot disguise yourself while holding a sigil.
                    }
                    else if (Engines.Alignment.TheFlag.ExistsOn(from))
                    {
                        from.SendMessage("You cannot disguise yourself while holding a flag.");
                    }
                    else if (!from.CanBeginAction(typeof(Spells.Fifth.IncognitoSpell)))
                    {
                        from.SendLocalizedMessage(501698); // You cannot disguise yourself while incognitoed.
                    }
                    else if (!from.CanBeginAction(typeof(Spells.Seventh.PolymorphSpell)))
                    {
                        from.SendLocalizedMessage(501699); // You cannot disguise yourself while polymorphed.
                    }
                    //else if ( Spells.Necromancy.TransformationSpell.UnderTransformation( from ) )
                    //{
                    //from.SendLocalizedMessage( 501699 ); // You cannot disguise yourself while polymorphed.
                    //}
                    else if (from.HueMod != -1 || from.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
                    {
                        from.SendLocalizedMessage(501605); // You are already disguised.
                    }
                    else
                    {
                        if (!Core.RuleSets.AnyAIShardRules())
                            from.BodyMod = (from.Female ? 184 : 183);
                        else
                            from.HueMod = 0;

                        from.Delta(MobileDelta.Body);

                        pm.SavagePaintExpiration = TimeSpan.FromDays(7.0);

                        from.SendLocalizedMessage(1042537); // You now bear the markings of the savage tribe.  Your body paint will last about a week or you can remove it with an oil cloth.
                    }
                }
                else
                {
                    from.SendMessage("You must be of Savage alignment to use that.");
                }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}