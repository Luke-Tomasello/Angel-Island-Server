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

/********************************************************
 * TO DO
 * Finish the old style display never finished by Pixie.
 * See BaseClothes for a completed version
 ********************************************************
 */

/* Scripts/Items/Jewels/BaseJewel.cs
 * CHANGE LOG
 *  4/20/23, Yoar
 *      Reworked old-school naming
 *  4/7/23, Yoar
 *      Implemented IMagicEquip.
 *      Removed old magic effect code.
 * 04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 * 9/30/21, Adam,
 *      Turn off special event items
 *      **if (attrs.Count == 0 && Name != null && m_Crafter == null)**
 *      Removed as it was preventing jewelry with names from displaying the name
 * 3/8/2016, Adam
 *		o looks like several magic clothes/jewlery were never tested. Fixing them now:
 *		o essentially all the changes for this date in BaseClothing
 *		o Add expiration and region checks for special event jewelry
 * 5/2/08, Adam
 *		Update use of HideAttributes to be simpler.
 * 5/1/08, Adam
 *		Use HideAttributes filtering to hide the [Exceptional] tag.
 *	01/04/07, Pix
 *		Fixed stat-effect items.
 *	01/02/07, Pix
 *		Stat-effect magic items no longer stack.
 *  06/26/06, Kit
 *		Added region spell checks to all magic jewlery effects, follow region casting rules!!
 *	8/18/05, erlein
 *		Added code necessary to support maker's mark and exceptional chance.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *	12/19/04, Adam
 *		1. In SetRandomMagicEffect() change NewMagicType to use explicit Utility.RandomList()
 *		2. In SetRandomMagicEffect() change NewLevel to use Utility.RandomMinMax(MinLevel, MaxLevel)
 *  8/9/04 - Pixie
 *		Explicitly cleaned up timers.
 *	7/25/04 smerX
 *		A new timer is initiated OnAdded
 *  7/6/04 - Pixie
 *		Added cunning, agility, strength, feeblemind, clumsy, weaken, curse, nightsight jewelry
 *	6/25/04 - Pixie
 *		Fixed jewelry so that they didn't spawn outside of the appropriate range
 *		(bracelets were spawning with teleport and rings/bracelets were spawning
 *		as unidentified magic items but when id'ed didn't have a property)
// 05/11/2004 - Pulse
//	Completed changes to implement magic jewelry.
//	changes include:
//		* several new properties: magic type, number of charges, and identified flag
//		* updated GetProperties and OnSingleClick to include magic properties
//		* JewelMagicEffect enumeration for various available spell types
//		* MagicEffectTimer class to implement spell timing effects and control charge usage
//		* All jewelry items can be made magic through the [props command for Game Master or higher access level
//		* SetMagic and SetRandomMagicEffect to allow setting an existing jewelry item to some
//			type of magic and level
//		* "Apply" routines for the various magic effects
//		* an AddStatBonus routine used by the Bless effect.
*/

using Server.Diagnostics;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public enum GemType
    {
        None,
        StarSapphire,
        Emerald,
        Sapphire,
        Ruby,
        Citrine,
        Amethyst,
        Tourmaline,
        Amber,
        Diamond
    }

    // AI only
    public enum JewelQuality
    {
        Low,
        Regular,
        Exceptional
    }

    public abstract class BaseJewel : Item, ICraftable, IMagicEquip
    {
        //private AosAttributes m_AosAttributes;
        //private AosElementAttributes m_AosResistances;
        //private AosSkillBonuses m_AosSkillBonuses;
        private CraftResource m_Resource;
        private GemType m_GemType;
        private bool m_Identified;
        private IOBAlignment m_IOBAlignment;
        private MakersMark m_Crafter;
        private JewelQuality m_Quality;
        private DateTime m_Expiration;
        private List<int> m_Regions = new List<int>();  // Create a list of allowed regions.

        /*
				[CommandProperty( AccessLevel.GameMaster )]
				public AosAttributes Attributes
				{
					get{ return m_AosAttributes; }
					set{}
				}

				[CommandProperty( AccessLevel.GameMaster )]
				public AosElementAttributes Resistances
				{
					get{ return m_AosResistances; }
					set{}
				}

				[CommandProperty( AccessLevel.GameMaster )]
				public AosSkillBonuses SkillBonuses
				{
					get{ return m_AosSkillBonuses; }
					set{}
				}
		*/

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; Hue = CraftResources.GetHue(m_Resource); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public GemType GemType
        {
            get { return m_GemType; }
            set { m_GemType = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Identified
        {
            get { return m_Identified; }
            set { m_Identified = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public IOBAlignment IOBAlignment
        {
            get { return m_IOBAlignment; }
            set { m_IOBAlignment = value; }
        }

        /// <summary>
        /// Adam: add a list of region IDs in which this item may be used.
        /// Since lists of integers aren't really supported by the properties gump, we manipulate it as a string
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public string RegionLock
        {
            get
            {
                if (m_Regions != null && m_Regions.Count > 0)
                {
                    String sx = null;
                    foreach (int ix in m_Regions)
                        sx += string.Format("{0}, ", ix);
                    return sx.TrimEnd(new char[] { ' ', ',' });
                }
                else
                    return null;
            }
            set
            {
                List<int> temp = new List<int>();
                if (value != null)
                {
                    char[] delimiterChars = { ' ', ',' };
                    string[] words = value.Split(delimiterChars);
                    foreach (string sx in words)
                        if (sx.Length > 0)
                            temp.Add(Int32.Parse(sx));

                    //okay, we didn't crash, update our master.
                    m_Regions.Clear();
                    foreach (int jx in temp)
                        m_Regions.Add(jx);
                }
            }
        }

        public bool CheckRegion()
        {
            if (Parent is Mobile == false)
                return false;

            // if not specified, all regions match
            if (m_Regions.Count == 0)
                return true;

            Mobile from = Parent as Mobile;
            Map map = from.Map;
            bool got_it = false;

            if (map != null)
            {
                try
                {
                    ArrayList reglist = Region.FindAll(from.Location, map);

                    foreach (Region rx in reglist)
                    {
                        if (rx is Region)
                        {
                            if (rx is HouseRegion)
                            {   // don't allow use in a house even if the underlying region is a match
                                return false;
                            }
#if false
                            else if (rx is SensoryRegion)
                            {   // return the id
                                if (m_Regions.Contains(rx.UId))
                                    got_it = true;
                            }
#endif
                            else if (rx is StaticRegion)
                            {   // sure, why not
                                if (m_Regions.Contains(rx.UId))
                                    got_it = true;
                            }
                            else if (rx != map.DefaultRegion)
                            {   // sure, why not
                                if (m_Regions.Contains(rx.UId))
                                    got_it = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }

            return got_it;
        }

        [CommandProperty(AccessLevel.Seer)]
        public DateTime Expiration
        {
            get { return m_Expiration; }
            set { m_Expiration = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MakersMark Crafter
        {
            get
            {
                return m_Crafter;
            }
            set
            {
                m_Crafter = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public JewelQuality Quality
        {
            get { return m_Quality; }
            set { m_Quality = value; InvalidateProperties(); }
        }

        public virtual int BaseGemTypeNumber { get { return 0; } }

        public override int LabelNumber
        {
            get
            {
                if (m_GemType == GemType.None)
                    return base.LabelNumber;

                return BaseGemTypeNumber + (int)m_GemType - 1;
            }
        }

        public virtual int ArtifactRarity { get { return 0; } }

        public BaseJewel(int itemID, Layer layer)
            : base(itemID)
        {
            //m_AosAttributes = new AosAttributes( this );
            //m_AosResistances = new AosElementAttributes( this );
            //m_AosSkillBonuses = new AosSkillBonuses( this );
            m_Resource = CraftResource.Iron;
            m_GemType = GemType.None;
            m_MagicCharges = 0;
            m_Identified = true;
            m_IOBAlignment = IOBAlignment.None;
            Layer = layer;
            m_Expiration = DateTime.MinValue;           // null
        }

        public override bool CanEquip(Mobile m)
        {
            if (!Ethics.Ethic.CheckEquip(m, this))
                return false;

            if (m.AccessLevel < AccessLevel.GameMaster)
            {
                #region Kin
                if ((m != null) && (m is PlayerMobile))
                {
                    PlayerMobile pm = (PlayerMobile)m;

                    if (Core.RuleSets.MortalisRules() || Core.RuleSets.AngelIslandRules())
                        if (this.IOBAlignment != IOBAlignment.None)
                        {
                            if (pm.IOBEquipped == true)
                            {
                                pm.SendMessage("You cannot equip more than one item of brethren at a time.");
                                return false;
                            }
                            if (pm.IOBAlignment != this.IOBAlignment)
                            {
                                if (pm.IOBAlignment == IOBAlignment.None)
                                {
                                    pm.SendMessage("You cannot equip a kin item without your guild aligning itself to a kin.");
                                }
                                else if (pm.IOBAlignment == IOBAlignment.OutCast)
                                {
                                    pm.SendMessage("You cannot equip a kin item while you are outcast from your kin.");
                                }
                                else
                                {
                                    pm.SendMessage("You cannot equip items of another kin.");
                                }
                                return false;
                            }
                        }
                }
                #endregion
            }

            return base.CanEquip(m);
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile)
            {
                Mobile from = (Mobile)parent;

                MagicEquipment.OnAdded(from, this);
            }
        }

        public override void OnRemoved(object parent)
        {
            if (parent is Mobile)
            {
                Mobile m = (Mobile)parent;

                MagicEquipment.OnRemoved(m, this);
            }
        }

        public void SetRandomMagicEffect(int MinLevel, int MaxLevel)
        {
            if (MinLevel < 1 || MaxLevel > 3)
                return;

            if (this is BaseRing)
            {
                m_MagicEffect = (MagicEquipEffect)Utility.RandomList((int)MagicEquipEffect.SpellReflection,
                    (int)MagicEquipEffect.Invisibility, (int)MagicEquipEffect.Bless,
                    (int)MagicEquipEffect.Teleport, (int)MagicEquipEffect.Agility,
                    (int)MagicEquipEffect.Cunning, (int)MagicEquipEffect.Strength,
                    (int)MagicEquipEffect.NightSight);
            }
            else
            {
                // no teleporting for non-rings
                m_MagicEffect = (MagicEquipEffect)Utility.RandomList((int)MagicEquipEffect.SpellReflection,
                    (int)MagicEquipEffect.Invisibility, (int)MagicEquipEffect.Bless,
                    /*(int)MagicEquipEffect.Teleport,*/ (int)MagicEquipEffect.Agility,
                    (int)MagicEquipEffect.Cunning, (int)MagicEquipEffect.Strength,
                    (int)MagicEquipEffect.NightSight);
            }

            int NewLevel = Utility.RandomMinMax(MinLevel, MaxLevel);
            switch (NewLevel)
            {
                case 1:
                    m_MagicCharges = Utility.Random(1, 5);
                    break;
                case 2:
                    m_MagicCharges = Utility.Random(4, 11);
                    break;
                case 3:
                    m_MagicCharges = Utility.Random(9, 20);
                    break;
                default:
                    // should never happen
                    m_MagicCharges = 0;
                    break;
            }

            Identified = false;
        }

        public BaseJewel(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            /*
						m_AosSkillBonuses.GetProperties( list );

						int prop;

						if ( (prop = ArtifactRarity) > 0 )
							list.Add( 1061078, prop.ToString() ); // artifact rarity ~1_val~

						if ( (prop = m_AosAttributes.WeaponDamage) != 0 )
							list.Add( 1060401, prop.ToString() ); // damage increase ~1_val~%

						if ( (prop = m_AosAttributes.DefendChance) != 0 )
							list.Add( 1060408, prop.ToString() ); // defense chance increase ~1_val~%

						if ( (prop = m_AosAttributes.BonusDex) != 0 )
							list.Add( 1060409, prop.ToString() ); // dexterity bonus ~1_val~

						if ( (prop = m_AosAttributes.EnhancePotions) != 0 )
							list.Add( 1060411, prop.ToString() ); // enhance potions ~1_val~%

						if ( (prop = m_AosAttributes.CastRecovery) != 0 )
							list.Add( 1060412, prop.ToString() ); // faster cast recovery ~1_val~

						if ( (prop = m_AosAttributes.CastSpeed) != 0 )
							list.Add( 1060413, prop.ToString() ); // faster casting ~1_val~

						if ( (prop = m_AosAttributes.AttackChance) != 0 )
							list.Add( 1060415, prop.ToString() ); // hit chance increase ~1_val~%

						if ( (prop = m_AosAttributes.BonusHits) != 0 )
							list.Add( 1060431, prop.ToString() ); // hit point increase ~1_val~

						if ( (prop = m_AosAttributes.BonusInt) != 0 )
							list.Add( 1060432, prop.ToString() ); // intelligence bonus ~1_val~

						if ( (prop = m_AosAttributes.LowerManaCost) != 0 )
							list.Add( 1060433, prop.ToString() ); // lower mana cost ~1_val~%

						if ( (prop = m_AosAttributes.LowerRegCost) != 0 )
							list.Add( 1060434, prop.ToString() ); // lower reagent cost ~1_val~%

						if ( (prop = m_AosAttributes.Luck) != 0 )
							list.Add( 1060436, prop.ToString() ); // luck ~1_val~

						if ( (prop = m_AosAttributes.BonusMana) != 0 )
							list.Add( 1060439, prop.ToString() ); // mana increase ~1_val~

						if ( (prop = m_AosAttributes.RegenMana) != 0 )
							list.Add( 1060440, prop.ToString() ); // mana regeneration ~1_val~

						if ( (prop = m_AosAttributes.ReflectPhysical) != 0 )
							list.Add( 1060442, prop.ToString() ); // reflect physical damage ~1_val~%

						if ( (prop = m_AosAttributes.RegenStam) != 0 )
							list.Add( 1060443, prop.ToString() ); // stamina regeneration ~1_val~

						if ( (prop = m_AosAttributes.RegenHits) != 0 )
							list.Add( 1060444, prop.ToString() ); // hit point regeneration ~1_val~

						if ( (prop = m_AosAttributes.SpellChanneling) != 0 )
							list.Add( 1060482 ); // spell channeling

						if ( (prop = m_AosAttributes.SpellDamage) != 0 )
							list.Add( 1060483, prop.ToString() ); // spell damage increase ~1_val~%

						if ( (prop = m_AosAttributes.BonusStam) != 0 )
							list.Add( 1060484, prop.ToString() ); // stamina increase ~1_val~

						if ( (prop = m_AosAttributes.BonusStr) != 0 )
							list.Add( 1060485, prop.ToString() ); // strength bonus ~1_val~

						if ( (prop = m_AosAttributes.WeaponSpeed) != 0 )
							list.Add( 1060486, prop.ToString() ); // swing speed increase ~1_val~%
			*/

            if (m_Crafter != null)
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~

            if (m_Quality == JewelQuality.Exceptional)
                list.Add(1060636); // exceptional
        }

        public override void OnSingleClick(Mobile from)
        {
            if (this.HideAttributes == true || (Name == null && UseOldNames))
            {
                base.OnSingleClick(from);
                return;
            }

            ArrayList attrs = new ArrayList();

            if (DisplayLootType)
            {
                if (GetFlag(LootType.Blessed))
                    attrs.Add(new EquipInfoAttribute(1038021)); // blessed
                else if (GetFlag(LootType.Cursed))
                    attrs.Add(new EquipInfoAttribute(1049643)); // cursed
            }

            if (m_Quality == JewelQuality.Exceptional)
                attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));

            if (m_Identified)
            {
                if (m_MagicEffect != MagicEquipEffect.None)
                    attrs.Add(new EquipInfoAttribute(MagicEquipment.GetLabel(m_MagicEffect), m_MagicCharges));
            }
            else if (m_MagicEffect != MagicEquipEffect.None)
            {
                attrs.Add(new EquipInfoAttribute(1038000)); // Unidentified
            }

            int number;

            if (Name == null)
            {
                number = LabelNumber;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
                return;

            EquipmentInfo eqInfo = new EquipmentInfo(number, m_Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));
            from.Send(new DisplayEquipmentInfo(this, eqInfo));
        }

        public override string GetOldPrefix(ref Article article)
        {
            string prefix = "";

            if (m_Identified)
            {
                // there are no identifyable prefixes
            }
            else if (!HideAttributes && m_MagicEffect != MagicEquipEffect.None)
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.A;

                prefix += "magic ";
            }

            return prefix;
        }

        public override string GetOldSuffix()
        {
            string suffix = "";

            if (m_Identified)
            {
                if (!HideAttributes && m_MagicEffect != MagicEquipEffect.None)
                {
                    if (suffix.Length == 0)
                        suffix += " of ";
                    else
                        suffix += " and ";

                    suffix += MagicEquipment.GetOldSuffix(m_MagicEffect, m_MagicCharges);
                }
            }

            if (m_Crafter != null)
                suffix += " crafted by " + m_Crafter.Name;

            return suffix;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (MagicEquipment.OnUse(from, this))
                return;

            base.OnDoubleClick(from);
        }

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            Type resourceType = typeRes;

            if (resourceType == null)
                resourceType = craftItem.Resources.GetAt(0).ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            CraftContext context = craftSystem.GetContext(from);

            if (context != null && context.DoNotColor)
                Hue = 0;

            if (1 < craftItem.Resources.Count)
            {
                resourceType = craftItem.Resources.GetAt(1).ItemType;

                if (resourceType == typeof(StarSapphire))
                    GemType = GemType.StarSapphire;
                else if (resourceType == typeof(Emerald))
                    GemType = GemType.Emerald;
                else if (resourceType == typeof(Sapphire))
                    GemType = GemType.Sapphire;
                else if (resourceType == typeof(Ruby))
                    GemType = GemType.Ruby;
                else if (resourceType == typeof(Citrine))
                    GemType = GemType.Citrine;
                else if (resourceType == typeof(Amethyst))
                    GemType = GemType.Amethyst;
                else if (resourceType == typeof(Tourmaline))
                    GemType = GemType.Tourmaline;
                else if (resourceType == typeof(Amber))
                    GemType = GemType.Amber;
                else if (resourceType == typeof(Diamond))
                    GemType = GemType.Diamond;
            }

            if (Core.RuleSets.AngelIslandRules())
            {
                if (makersMark)
                    this.Crafter = from;

                this.Quality = (JewelQuality)quality;
            }

            return 1;
        }

#if old
		else if (item is BaseJewel)
					{
						BaseJewel jewel = (BaseJewel)item;

						Type resourceType = typeRes;
						endquality = quality;

						if (resourceType == null)
							resourceType = Ressources.GetAt(0).ItemType;

						jewel.Resource = CraftResources.GetFromType(resourceType);

						if (1 < Ressources.Count)
						{
							resourceType = Ressources.GetAt(1).ItemType;

							if (resourceType == typeof(StarSapphire))
								jewel.GemType = GemType.StarSapphire;
							else if (resourceType == typeof(Emerald))
								jewel.GemType = GemType.Emerald;
							else if (resourceType == typeof(Sapphire))
								jewel.GemType = GemType.Sapphire;
							else if (resourceType == typeof(Ruby))
								jewel.GemType = GemType.Ruby;
							else if (resourceType == typeof(Citrine))
								jewel.GemType = GemType.Citrine;
							else if (resourceType == typeof(Amethyst))
								jewel.GemType = GemType.Amethyst;
							else if (resourceType == typeof(Tourmaline))
								jewel.GemType = GemType.Tourmaline;
							else if (resourceType == typeof(Amber))
								jewel.GemType = GemType.Amber;
							else if (resourceType == typeof(Diamond))
								jewel.GemType = GemType.Diamond;
						}

						if (makersMark)
							jewel.Crafter = from;

						jewel.Quality = (JewelQuality)quality;
					}
#endif

        #endregion

        #region Save Flags
        [Flags]
        enum SaveFlags
        {
            None = 0x0,
            HasExpiration = 0x01,
            HasIOBAlignment = 0x02,
            HasRegionLock = 0x04,
        }

        private SaveFlags m_SaveFlags = SaveFlags.None;

        private void SetFlag(SaveFlags flag, bool value)
        {
            if (value)
                m_SaveFlags |= flag;
            else
                m_SaveFlags &= ~flag;
        }

        private bool GetFlag(SaveFlags flag)
        {
            return ((m_SaveFlags & flag) != 0);
        }

        private SaveFlags ReadSaveFlags(GenericReader reader, int version)
        {
            SaveFlags sf = SaveFlags.None;
            if (version >= 6)
                sf = (SaveFlags)reader.ReadInt();
            return sf;
        }

        private SaveFlags WriteSaveFlags(GenericWriter writer)
        {
            m_SaveFlags = SaveFlags.None;
            SetFlag(SaveFlags.HasExpiration, m_Expiration != DateTime.MinValue ? true : false);
            SetFlag(SaveFlags.HasIOBAlignment, m_IOBAlignment != IOBAlignment.None ? true : false);
            SetFlag(SaveFlags.HasRegionLock, m_Regions.Count > 0 ? true : false);
            writer.Write((int)m_SaveFlags);
            return m_SaveFlags;
        }
        #endregion Save Flags

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)8);                   // version
            m_SaveFlags = WriteSaveFlags(writer);   // always follows version

            // version 6
            if (GetFlag(SaveFlags.HasIOBAlignment))
                writer.Write((int)m_IOBAlignment);

            if (GetFlag(SaveFlags.HasExpiration))
                writer.WriteDeltaTime(m_Expiration);

            if (GetFlag(SaveFlags.HasRegionLock))
            {
                writer.Write((short)m_Regions.Count);
                foreach (int ix in m_Regions)
                    writer.Write(ix);
            }

            // earlier versions
            m_Crafter.Serialize(writer);
            writer.Write((short)m_Quality);

            writer.Write((sbyte)m_MagicEffect);
            writer.Write((int)m_MagicCharges);
            writer.Write((bool)m_Identified);

            writer.WriteEncodedInt((int)m_Resource);
            writer.WriteEncodedInt((int)m_GemType);

            // removed in version 4
            //m_AosAttributes.Serialize( writer );
            //m_AosResistances.Serialize( writer );
            //m_AosSkillBonuses.Serialize( writer );
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_SaveFlags = ReadSaveFlags(reader, version);

            switch (version)
            {
                case 8:
                case 7:
                case 6:
                    {   // adam: add flags and support for special expiring event items
                        if (GetFlag(SaveFlags.HasIOBAlignment))
                            m_IOBAlignment = (IOBAlignment)reader.ReadInt();
                        else
                            m_IOBAlignment = IOBAlignment.None;

                        if (GetFlag(SaveFlags.HasExpiration))
                            m_Expiration = reader.ReadDeltaTime();
                        else
                            m_Expiration = DateTime.MinValue;

                        if (GetFlag(SaveFlags.HasRegionLock))
                        {
                            short count = reader.ReadShort();
                            for (int ix = 0; ix < count; ix++)
                                m_Regions.Add(reader.ReadInt());
                        }
                        goto case 5;
                    }
                case 5:
                    {
                        // erl: New "crafted by" and quality properties

                        if (version >= 7)
                            m_Crafter.Deserialize(reader);
                        else
                            m_Crafter = reader.ReadMobile();

                        m_Quality = (JewelQuality)reader.ReadShort();
                        goto case 4;
                    }
                case 4:
                    {
                        // remove AOS crap
                        // see case 1 below
                        goto case 3;
                    }
                case 3:
                    {
                        if (version >= 8)
                            m_MagicEffect = (MagicEquipEffect)reader.ReadSByte();
                        else
                            m_MagicEffect = LegacyMagicEffect(reader.ReadInt());

                        m_MagicCharges = reader.ReadInt();
                        m_Identified = reader.ReadBool();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Resource = (CraftResource)reader.ReadEncodedInt();
                        m_GemType = (GemType)reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        // pack these out of furture versions.
                        if (version < 4)
                        {
                            AosAttributes dmy_AosAttributes;
                            AosElementAttributes dmy_AosResistances;
                            AosSkillBonuses dmy_AosSkillBonuses;
                            dmy_AosAttributes = new AosAttributes(this, reader);
                            dmy_AosResistances = new AosElementAttributes(this, reader);
                            dmy_AosSkillBonuses = new AosSkillBonuses(this, reader);

                            if (Core.RuleSets.AOSRules() && Parent is Mobile)
                                dmy_AosSkillBonuses.AddTo((Mobile)Parent);

                            int strBonus = dmy_AosAttributes.BonusStr;
                            int dexBonus = dmy_AosAttributes.BonusDex;
                            int intBonus = dmy_AosAttributes.BonusInt;

                            if (Parent is Mobile && (strBonus != 0 || dexBonus != 0 || intBonus != 0))
                            {
                                Mobile m = (Mobile)Parent;

                                string modName = Serial.ToString();

                                if (strBonus != 0)
                                    m.AddStatMod(new StatMod(StatType.Str, modName + "Str", strBonus, TimeSpan.Zero));

                                if (dexBonus != 0)
                                    m.AddStatMod(new StatMod(StatType.Dex, modName + "Dex", dexBonus, TimeSpan.Zero));

                                if (intBonus != 0)
                                    m.AddStatMod(new StatMod(StatType.Int, modName + "Int", intBonus, TimeSpan.Zero));
                            }
                        }

                        if (Parent is Mobile)
                            ((Mobile)Parent).CheckStatTimers();

                        break;
                    }
                case 0:
                    {
                        // pack these out of furture versions.
                        if (version < 4)
                        {
                            AosAttributes dmy_AosAttributes;
                            AosElementAttributes dmy_AosResistances;
                            AosSkillBonuses dmy_AosSkillBonuses;
                            dmy_AosAttributes = new AosAttributes(this);
                            dmy_AosResistances = new AosElementAttributes(this);
                            dmy_AosSkillBonuses = new AosSkillBonuses(this);
                        }

                        break;
                    }
            }

            if (version < 2)
            {
                m_Resource = CraftResource.Iron;
                m_GemType = GemType.None;
            }

            if (version < 5)
            {
                m_Quality = JewelQuality.Regular;
            }
        }

        public static MagicEquipEffect LegacyMagicEffect(int value)
        {
            switch (value)
            {
                case 0: return MagicEquipEffect.None;
                case 1: return MagicEquipEffect.SpellReflection;
                case 2: return MagicEquipEffect.Invisibility;
                case 3: return MagicEquipEffect.Bless;
                case 4: return MagicEquipEffect.Teleport;
                case 5: return MagicEquipEffect.Agility;
                case 6: return MagicEquipEffect.Cunning;
                case 7: return MagicEquipEffect.Strength;
                case 8: return MagicEquipEffect.NightSight;
                case 9: return MagicEquipEffect.Curse;
                case 10: return MagicEquipEffect.Clumsiness;
                case 11: return MagicEquipEffect.Feeblemind;
                case 12: return MagicEquipEffect.Weakness;
            }

            return MagicEquipEffect.None;
        }

        #region IMagicEquip

        private MagicEquipEffect m_MagicEffect = MagicEquipEffect.None;
        private int m_MagicCharges;

        [CommandProperty(AccessLevel.GameMaster)]
        public MagicEquipEffect MagicEffect
        {
            get { return m_MagicEffect; }
            set { m_MagicEffect = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MagicCharges
        {
            get { return m_MagicCharges; }
            set { m_MagicCharges = value; }
        }

        #endregion
    }
}