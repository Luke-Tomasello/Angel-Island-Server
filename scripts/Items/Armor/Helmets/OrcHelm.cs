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

/* Scripts/Items/Armor/Helmets/OrcHelm.cs
 * ChangeLog
 *  11/11/2023, Adam (EvilOrcHelm)
 *      Fix a bug that was dropping both Dex and Int
 *      Fix: You will lose ten points in dexterity or intelligence, whichever is higher.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 10 lines removed.
 *	11/10/04, Adam
 *		for legacy objects, force the IOBAlignment = IOBAlignment.Orcish in Deserialize for the OrcishKinHelm
 *	11/10/04, Froste 
 *      Normalized Orcish Kin Helm to lowercase
 *	9/16/04, Pigpen
 * 		Add OrcishKinHelm variant of this helm.
 */

using System;

namespace Server.Items
{
    public class OrcHelm : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 50; } }

        // public override int AosStrReq  { get { return 30; } }
        public override int StrReq { get { return 10; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 20; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        [Constructable]
        public OrcHelm()
            : base(0x1F0B)
        {
            Weight = 1;
        }

        public OrcHelm(Serial serial)
            : base(serial)
        {
        }

        // old name removed, see base class

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

    public class EvilOrcHelm : OrcHelm
    {

        public override int StrReq { get { return 30; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        #region Stat Mods
        public void AddStatMods(Mobile m)
        {
            if (m == null)
                return;

            string modName = this.Serial.ToString();
            // When equipped, your strength is increased by ten points (even over skill cap of 100). 
            // You will lose ten points in dexterity or intelligence, whichever is higher.
            StatMod strMod = new StatMod(StatType.Str, string.Format("[Magic Hat] +Str {0}", modName), +10, TimeSpan.Zero);
            m.AddStatMod(strMod);

            if (m.Dex > m.Int)
            {
                StatMod dexMod = new StatMod(StatType.Dex, string.Format("[Magic Hat] -Dex {0}", modName), -10, TimeSpan.Zero);
                m.AddStatMod(dexMod);
            }
            else
            {
                StatMod intMod = new StatMod(StatType.Int, string.Format("[Magic Hat] -Int {0}", modName), -10, TimeSpan.Zero);
                m.AddStatMod(intMod);
            }
        }

        public void RemoveStatMods(Mobile m)
        {
            if (m == null)
                return;

            string modName = this.Serial.ToString();

            m.RemoveStatMod(string.Format("[Magic Hat] +Str {0}", modName));
            m.RemoveStatMod(string.Format("[Magic Hat] -Dex {0}", modName));
            m.RemoveStatMod(string.Format("[Magic Hat] -Int {0}", modName));
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);
            if (parent is Mobile)
            {
                AddStatMods(parent as Mobile);
                Misc.Titles.AwardKarma(parent as Mobile, -25, true);        // TODO: no idea what the karma loss should be here
            }
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);
            RemoveStatMods(parent as Mobile);
        }
        #endregion

        /* Evil Orc Helm
		 * These items appeared during the orc/savage scenario in June/July 2001. They are found as loot on Orc Lords. 
		 * When equipped, your strength is increased by ten points (even over skill cap of 100). 
		 * You will lose ten points in dexterity or intelligence, whichever is higher. 
		 * You will also suffer karma loss. Stats return to normal when helm is unequipped. Karma loss will happen each time you put the helm on again.
		 */
        [Constructable]
        public EvilOrcHelm()
            : base()
        {
            Weight = 1;

            // hue can be verified here 
            // http://webcache.googleusercontent.com/search?q=cache:vIZmrS632BEJ:www.runuo.com/forums/script-support/25902-evil-orc-helm.html+%22Evil+Orc+Helm%22+hue&cd=5&hl=en&ct=clnk&gl=us
            Hue = 0x96D;
        }

        public EvilOrcHelm(Serial serial)
            : base(serial)
        {
        }

        public override string OldName
        {
            get
            {
                // name & look can be verified here
                // http://www.iceweasel.net/wzl_guild/semi/
                return "evil orc helm";
            }
        }

        public override Article OldArticle
        {
            get { return Article.An; }
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

            AddStatMods(Parent as Mobile);
        }
    }

    public class OrcishKinHelm : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 50; } }

        // public override int AosStrReq  { get { return 30; } }
        public override int StrReq { get { return 10; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 20; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        [Constructable]
        public OrcishKinHelm()
            : base(0x1F0B)
        {
            Weight = 1;
            Name = "orcish kin helm";
            IOBAlignment = IOBAlignment.Orcish;
        }

        public OrcishKinHelm(Serial serial)
            : base(serial)
        {
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

            // for legacy objects
            IOBAlignment = IOBAlignment.Orcish;
        }

    }
}