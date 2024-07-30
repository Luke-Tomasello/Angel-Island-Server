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

/* Scripts/Mobiles/Townfolk/SeekerOfAdventure.cs
 * ChangeLog
 *	05/18/06, Adam
 *		- rewrite to elimnate named locations and replace with Point locations.
 *		- Add many new and dangerous places to go
 */

using Server.Items;

namespace Server.Mobiles
{
    public class SeekerOfAdventure : BaseEscortable
    {
        private static Point3D[] m_Dungeons = new Point3D[]
            {
                new Point3D(5456,1862,0),	// "Covetous"
				new Point3D(5202,599,0),		// "Deceit" 
				new Point3D(5501,570,59),	// "Despise"
				new Point3D(5243,1004,0),	// "Destard" 
				new Point3D(5905,22,44),		// "Hythloth"
				new Point3D(5395,126,0),		// "Shame"
				new Point3D(5825,599,0),		// "Wrong"

				// new places!!
				new Point3D(633,1486,0),		// "Yew Orc fort"
				new Point3D(4619,1334,0),	// "the pirate stronghold on Moonglow island"
				new Point3D(635,860,0),		// "the Militia stronghold in Yew"
				new Point3D(964,722,0),		// "the savage stronghold"
				new Point3D(1380,1487,10),	// "Britain Graveyard"
				new Point3D(2667,2084,5),	// "the pirate stronghold at Buc's Den"
				new Point3D(3011,3526,15),	// "the brigand stronghold"
				new Point3D(5166,244,15),	// "the Council stronghold"
			};

        public override Point3D[] GetPossibleDestinations()
        {
            return m_Dungeons;
        }

        [Constructable]
        public SeekerOfAdventure()
        {
            Title = "the seeker of adventure";
        }

        public override bool ClickTitle { get { return false; } } // Do not display 'the seeker of adventure' when single-clicking

        public override void InitOutfit()
        {
            if (Female)
                AddItem(new FancyDress(GetRandomHue()));
            else
                AddItem(new FancyShirt(GetRandomHue()));

            int lowHue = GetRandomHue();

            AddItem(new ShortPants(lowHue));

            if (Female)
                AddItem(new ThighBoots(lowHue));
            else
                AddItem(new Boots(lowHue));

            if (!Female)
                AddItem(new BodySash(lowHue));

            AddItem(new Cloak(GetRandomHue()));

            AddItem(new Longsword());

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new TwoPigTails(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new KrisnaHair(Utility.RandomHairHue())); break;
            }

            if (!Core.RuleSets.SiegeStyleRules())
                PackGold(100, 150);
        }

        public SeekerOfAdventure(Serial serial)
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
    }
}