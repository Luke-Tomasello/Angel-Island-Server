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

/* Scripts\Engines\AngelIsland\Mobiles\Vendors\NPC\PrisonLiaisonOfficer.cs
 *  6/26/2023, Adam
 *      Created
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class PrisonLiaisonOfficer : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }


        [Constructable]
        public PrisonLiaisonOfficer()
            : base("the prison liaison officer")
        {
            Hue = Utility.RandomSkinHue();
        }

        public override void InitSBInfo()
        {

            m_SBInfos.Add(new SBPrisonLiaisonOfficer());

        }

        public override void InitBody()
        {
            if (this.Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            AddItem(new LongPants(0x322));
            AddItem(new Shoes(Utility.GetRandomHue()));
            AddItem(new FancyShirt(0x47E));
            AddItem(new GoldRing());
            AddItem(new FloppyHat(Utility.GetRandomHue()));
            Runebook runebook = new Runebook();
            runebook.Hue = Utility.RandomNondyedHue();
            runebook.Name = "Visitors";
            runebook.LootType = LootType.Newbied;
            AddItem(runebook);
        }

        public PrisonLiaisonOfficer(Serial serial)
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