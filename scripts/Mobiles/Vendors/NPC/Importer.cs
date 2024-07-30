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

/* Scripts/Mobiles/Vendors/NPC/Importer.cs
 * ChangeLog
 *  12/03/06 Taran Kain
 *      Set Female = false. No trannies!
 *  1/4/05, Froste
 *      Changed the title from "the importer" to "the mystic importer"
 *  10/18/04, Froste
 *      Modified Restock to use OnRestock() because it's fixed now
 *  10/11/04, Froste
 *      Created this modified version of Mage.cs
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/29/04, mith
 *		Modified Restock to use OnRestockReagents() to restock 100 of each item instead of only 20.
 */

using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class Importer : BaseVendor
    {
        public ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        //public override NpcGuild NpcGuild{ get{ return NpcGuild.MagesGuild; } }

        [Constructable]
        public Importer()
            : base("the mystic importer")
        {
            /*			SetSkill( SkillName.EvalInt, 65.0, 88.0 );
			 *			SetSkill( SkillName.Inscribe, 60.0, 83.0 );
			 *			SetSkill( SkillName.Magery, 64.0, 100.0 );
			 *			SetSkill( SkillName.Meditation, 60.0, 83.0 );
			 *			SetSkill( SkillName.MagicResist, 65.0, 88.0 );
			 *			SetSkill( SkillName.Wrestling, 36.0, 68.0 );
			 */
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBImporter());
        }

        /*		public override VendorShoeType ShoeType
		 *		{
		 *			get{ return Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals; }
		 *		}
		 */
        public override void InitBody()
        {
            InitStats(100, 100, 25);

            SpeechHue = Utility.RandomSpeechHue();
            Hue = Utility.RandomSkinHue();

            NameHue = CalcInvulNameHue();

            Female = false;
            Body = 0x190;
            Name = NameList.RandomName("male");

        }

        public override void InitOutfit()
        {
            //			base.InitOutfit();

            //			AddItem( new Server.Items.Robe( Utility.RandomBlueHue() ) );
            AddItem(new Server.Items.GnarledStaff());

            if (Utility.RandomBool())
                AddItem(new Shoes(Utility.RandomBlueHue()));
            else
                AddItem(new Sandals(Utility.RandomBlueHue()));

            Item EvilMageRobe = new Robe();
            EvilMageRobe.Hue = 0x1;
            EvilMageRobe.LootType = LootType.Newbied;
            AddItem(EvilMageRobe);

            Item EvilWizHat = new WizardsHat();
            EvilWizHat.Hue = 0x1;
            EvilWizHat.LootType = LootType.Newbied;
            AddItem(EvilWizHat);

            Item Bracelet = new GoldBracelet();
            Bracelet.LootType = LootType.Newbied;
            AddItem(Bracelet);

            Item Ring = new GoldRing();
            Ring.LootType = LootType.Newbied;
            AddItem(Ring);

            Item hair = new LongHair();
            hair.Hue = 0x47E;
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            if (!this.Female)
            {
                Item beard = new MediumLongBeard();
                beard.Hue = 0x47E;
                beard.Movable = false;
                beard.Layer = Layer.FacialHair;
                AddItem(beard);
            }

        }

        public override void Restock()
        {
            base.LastRestock = DateTime.UtcNow;

            IBuyItemInfo[] buyInfo = this.GetBuyInfo();

            foreach (IBuyItemInfo bii in buyInfo)
                bii.OnRestock(); // change bii.OnRestockReagents() to OnRestock()
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 2))
                return true;

            return base.HandlesOnSpeech(from);
        }

        /*      public override void OnSpeech(SpeechEventArgs e)
		 *    {
		 *        base.OnSpeech( e );
		 *         this.Say("Leave these halls before it is too late!");
		 *    }
		 */

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is ContextMenus.VendorBuyEntry)
                    list.RemoveAt(i--);
                else if (list[i] is ContextMenus.VendorSellEntry)
                    list.RemoveAt(i--);
            }
        }

        public Importer(Serial serial)
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

            NameHue = CalcInvulNameHue();
        }

    }
}