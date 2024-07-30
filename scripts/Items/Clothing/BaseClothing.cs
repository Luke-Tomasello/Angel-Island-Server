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

/* Scripts/Items/Clothing/BaseClothing.cs
 * CHANGELOG
 *  9/18/23, Yoar
 *      Blessed clothing can no longer be damaged (SP only). Trammies rejoice!
 *  4/20/23, Yoar
 *      Reworked old-school naming
 *  4/7/23, Yoar
 *      Implemented IMagicEquip.
 *      Removed old magic effect code.
 *  1/10/23, Yoar
 *      Added Ethic bless handles
 * 8/12/22, Adam
 *      cleanup Save Flags usage.
 * 04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 * 3/14/22, Yoar
 *      Added ValidateMobile static method for compatibility with RunUO. Doesn't do anything yet.
 * 9/07/21, Yoar
 *      Now displaying the maker's mark in old-style single-clicks.
 * 8/30/21, adam (add GetArticle() to OnSingleClick)
 *      don't add the article 'a' to something like "a magical wizard's hat"
 * 3/8/2016, Adam
 *		o looks like several magic clothes/jewlery were never tested. Fixing them now:
 *		o remove animation/sound from nightsight and spell reflect. these should be stealth
 *		o Add NightSightEffectTimer so that we can decrement the charges on the item.
 *		o Break apart the application of magic properties from OnAdded() to ApplyMagic().
 *			ApplyMagic() is now called called from PlayerMobile.OnLogin() to active magic properties.
 *		o rewrite magic reflect so that toggle on/off works (use RunUO + AOS logic)
 * 6/10/10, Adam
 *		o Port OnHit logic to that of RunUO 2.0
 *		o clothing is now deleted when it is destroyed
 *		o clothing now absorbs a little bit of damage as per RunUO 2.0
 * 4/1/10, adam
 *		Add support for save/restore hue (for CTF colorizing)
 * 5/2/08, Adam
 *		Update use of HideAttributes to be simpler.
 * 5/1/08, Adam
 *		Use HideAttributes filtering to hide the [Exceptional] tag.
 *	01/04/07, Pix
 *		Fixed stat-effect items.
 *	01/02/07, Pix
 *		Stat-effect magic items no longer stack.
 *  7/20/06, Rhiannon
 *		Fixed order of precedence bug when setting hitpoints of exceptional and low quality clothing during deserialization.
 *  07/20/06, Rhiannon
 *		Added special case for cloth gloves to Scissor() so they produce white cloth when undyed.
 *  06/26/06, Kit
 *		Added region spell checks to all magic clothing effects, follow region casting rules!!
 *	6/22/06, Pix
 *		Added special message in CanEquip for Outcast alignment
 *	6/15/06, Pix
 *		Clarified IOB refusal message in CanEquip.
 *	06/06/06, Pix
 *		Changes for Kin System
 *	11/10/05, erlein
 *		Removed SaveHue property and added deserialization to pack out old data.
 *	10/7/05, erlein
 *		Modified deserialization so that non-exceptional, non-charged clothing is not made regular loottype.
 *	10/7/05, erlein
 *		Fixed deserialization to not include clothing with magic charges in its newbification.
 *	10/7/05, erlein
 *		Fixed clothing wear so there's a hitpoint check.
 *	10/7/05, erlein
 *		Altered clothing wear so that the piece is not destroyed but instead made tattered and unwearable upon full depletion of hitpoints.
 *	10/7/05, Adam
 *		In order to keep players from farming new characters for newbie clothes
 *		we are moving this valuable resource into the hands of crafters.
 *		Exceptionally crafted clothes are now newbied. They do however wear out.
 *	10/6/05, erlein
 *		Added ranged weapon type check in OnHit() so also performs consistently higher damage
 *		than other weapon types (along with bladed, which did originally).
 *	10/04/05, Pix
 *		Changed OnAdded for IOB item equipping to use new GetIOBName() function.
 *	2/10/05, erlein
 *		Added HitPoints, MaxHitPoints and related code.
 *	9/18/05, Adam
 *		Add Scissorable attribute
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 1 lines removed.
 *  1/15/05, Adam
 *		add serialization of the new dyable attr (version 5 of baseclothing)
 *  01/15/05, Froste
 *      Added a Dyable bool to all items, added a check for m_Dyable in the Dye override, added to deserialize for legacy items created in loot.cs
 *	01/04/05, Pix
 *		Changed IOB requirement from 36 hours to 10 days
 *	12/19/04, Adam
 *		1. In IsMagicAllowed() change BaseHat to Boots to match the possible values
 *			returned from Loot.RandomClothingOrJewelry()
 *			This fixes the "boots" returned as a dead magic item.
 *		2. In SetRandomMagicEffect() change NewMagicType to use Utility.RandomList()
 *		3. In SetRandomMagicEffect() change NewLevel to use Utility.RandomMinMax(MinLevel, MaxLevel)
 *	11/11/04, Adam
 *		Make sure m is PlayerMobile before casting!
 *  11/10/04, Froste
 *      Normalized IOB messages to lowercase, normal sentence structure
 *	11/10/04, Adam
 *		Backout IOB naming as it is now handled elsewhere (Loot)
 *	11/07/04, Darva
 *		Added display of iob type on single click.
 *	11/07/04, Pigpen
 *		Updated OnAdded and OnRemoved to reflect new mechanics of IOBSystem.
 *	11/05/04, Pigpen
 *		Added IOBAlignment prop. for IOBsystem.
 *  8/9/04 - Pixie
 *		Explicitly cleaned up timers.
 *	7/25/04 smerX
 *		A new timer is initiated onAdded
 *  7/6/04 - Pixie
 *		Added cunning, agility, strength, feeblemind, clumsy, weaken, curse, nightsight clothing
 *		Made effects stackable.
 *	05/11/2004 - Pulse
 *	Completed changes to implement magic clothing.
 *	changes include:
 *		* several new properties: magic type, number of charges, and identified flag
 *		* updated GetProperties and OnSingleClick to include magic properties
 *		* MagicEffect enumeration for various available spell types
 *		* MagicEffectTimer class to implement spell timing effects and control charge usage
 *		* an IsMagicAllowed function that runs a case statement to determine whether or not magic can be set on an item
 *			All clothing items can be made magic through the [props command for Game Master or higher access level
 *			but internal routines for setting item magic relies on IsMagicAllowed result
 *		* SetMagic and SetRandomMagicEffect to allow setting an existing clothing item to some
 *			type of magic and level
 *		* "Apply" routines for the various magic effects
 *		* an AddStatBonus routine used by the Bless effect.
 */

using Server.Engines.Craft;
using Server.Factions;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections;
using System.Linq;

namespace Server.Items
{
    public enum ClothingQuality
    {
        Low,
        Regular,
        Exceptional
    }

    public interface IArcaneEquip
    {
        bool IsArcane { get; }
        int CurArcaneCharges { get; set; }
        int MaxArcaneCharges { get; set; }
    }

    public abstract class BaseClothing : Item, IDyable, IScissorable, IFactionItem, ICraftable, IMagicEquip /* todo, IWearableDurability*/
    {
        #region Factions
        private FactionItem m_FactionState;

        public FactionItem FactionItemState
        {
            get { return m_FactionState; }
            set
            {
                m_FactionState = value;

                if (m_FactionState == null)
                    Hue = 0;

                LootType = (m_FactionState == null ? LootType.Regular : LootType.Blessed);
            }
        }
        #endregion

        #region Ethics
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime EthicBlessHero
        {
            get { return Ethics.EthicBless.GetExpireHero(this); }
            set { Ethics.EthicBless.SetExpireHero(this, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime EthicBlessEvil
        {
            get { return Ethics.EthicBless.GetExpireEvil(this); }
            set { Ethics.EthicBless.SetExpireEvil(this, value); }
        }
        #endregion

        private MakersMark m_Crafter;
        private ClothingQuality m_Quality;
        protected /*private*/ CraftResource m_Resource;
        private bool m_Identified;
        private IOBAlignment m_IOBAlignment;    //Pigpen - Addition for IOB System
        private bool m_Dyable = true;           // Froste - Addition for better dye control
        private bool m_Scissorable = true;      // Adam - Addition for better Scissor control
        private DateTime m_Expiration;

        #region HUE_MANAGEMENT
        // Restore the old hue. Used for CTF and other events where we colorize the player
        private int m_EventHue = (int)-1;
        public bool EventHue { get { return m_EventHue != -1; } }
        public void PushHue(int newHue) { if (m_EventHue == (int)-1) { m_EventHue = Hue; Hue = newHue; } }
        public void PopHue() { if (m_EventHue != (int)-1) { Hue = m_EventHue; m_EventHue = (int)-1; } }
        #endregion


        // erl: additions for clothing wear
        // ..

        private short m_HitPoints;
        private short m_MaxHitPoints;

        public virtual int InitMinHits { get { return 40; } }
        public virtual int InitMaxHits { get { return 50; } }

        // ..

        [CommandProperty(AccessLevel.GameMaster)]
        public MakersMark Crafter
        {
            get { return m_Crafter; }
            set { m_Crafter = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ClothingQuality Quality
        {
            get { return m_Quality; }
            set { m_Quality = value; InvalidateProperties(); }
        }

        public virtual CraftResource DefaultResource { get { return CraftResource.None; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; Hue = CraftResources.GetHue(m_Resource); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Identified
        {
            get { return m_Identified; }
            set { m_Identified = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]  //Pigpen - Addition for IOB System
        public IOBAlignment IOBAlignment
        {
            get { return m_IOBAlignment; }
            set { m_IOBAlignment = value; }
        }

        [CommandProperty(AccessLevel.Seer)]
        public DateTime Expiration
        {
            get { return m_Expiration; }
            set { m_Expiration = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)] // Froste - Addition for better dye control
        public bool Dyable
        {
            get { return m_Dyable; }
            set { m_Dyable = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)] // Adam - Addition for better Scissor control
        public bool Scissorable
        {
            get { return m_Scissorable; }
            set { m_Scissorable = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)] // erl - for clothing wear
        public int MaxHitPoints
        {
            get { return m_MaxHitPoints; }
            set { m_MaxHitPoints = (short)value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)] // erl - for clothing wear
        public int HitPoints
        {
            get
            {
                return m_HitPoints;
            }
            set
            {
                if (value != m_HitPoints && MaxHitPoints > 0)
                {
                    m_HitPoints = (short)value;

                    if (m_HitPoints < 0)
                    {
                        if (Parent is IEntity parent)
                            parent.Notify(notification: Notification.Destroyed, this);
                        Delete();
                    }

                    if (m_HitPoints > MaxHitPoints)
                        m_HitPoints = (short)MaxHitPoints;

                    InvalidateProperties();

                    if (m_HitPoints == (m_MaxHitPoints / 10))
                    {
                        if (Parent is IEntity parent)
                            parent.Notify(notification: Notification.ClothingStatus, this, "Your clothing is severely damaged.");
                    }
                }
            }
        }

        // Yoar: compatibility with RunUO - doesn't do anything yet
        public static void ValidateMobile(Mobile m)
        {
        }

        public BaseClothing(int itemID, Layer layer)
            : this(itemID, layer, 0)
        {
        }

        public BaseClothing(int itemID, Layer layer, int hue)
            : base(itemID)
        {
            Layer = layer;
            Hue = hue;
            m_Quality = ClothingQuality.Regular;
            m_Identified = true;
            m_IOBAlignment = IOBAlignment.None;     //Pigpen - Addition for IOB System
            m_Dyable = true;                            //Froste - Addition for dye control
            m_Scissorable = true;                       // Adam - Addition for better Scissor control
            m_Expiration = DateTime.MinValue;           // null

            // erl: added for clothing wear
            m_HitPoints = m_MaxHitPoints = (short)Utility.RandomMinMax(InitMinHits, InitMaxHits);
        }

        public BaseClothing(Serial serial)
            : base(serial)
        {
        }

        public override bool CheckPropertyConfliction(Mobile m)
        {
            if (base.CheckPropertyConfliction(m))
                return true;

            if (Layer == Layer.Pants)
                return (m.FindItemOnLayer(Layer.InnerLegs) != null);

            if (Layer == Layer.Shirt)
                return (m.FindItemOnLayer(Layer.InnerTorso) != null);

            return false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Crafter != null)
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~

            #region Factions
            if (m_FactionState != null)
                list.Add(1041350); // faction item
            #endregion

            if (m_Quality == ClothingQuality.Exceptional)
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

            #region Factions
            if (m_FactionState != null)
                attrs.Add(new EquipInfoAttribute(1041350)); // faction item
            #endregion

            Ethics.EthicBless.AddEquipmentInfoAttribute(this, attrs);

            if (m_Quality == ClothingQuality.Exceptional)
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

            if (!HideAttributes && m_Quality == ClothingQuality.Exceptional)
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.An;

                prefix += "exceptional ";
            }

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
            if (parent is PlayerMobile)
            {
                PlayerMobile Wearer = (PlayerMobile)parent;

                if (this.IOBAlignment != IOBAlignment.None) //Pigpen - Addition for IOB System
                {
                    ((PlayerMobile)parent).IOBEquipped = false;
                }
            }

            if (parent is Mobile)
            {
                Mobile m = (Mobile)parent;

                MagicEquipment.OnRemoved(m, this);
            }
        }

        // 9/18/23, Yoar: Blessed clothing can no longer be damaged (SP only). Trammies rejoice!
        public bool Invulnerable { get { return (Core.RuleSets.SiegeRules() && LootType.HasFlag(LootType.Blessed)); } }

        // erl: Added to control clothing wear

        public virtual int OnHit(BaseWeapon weapon, int damageTaken)
        {
            int Absorbed = Utility.RandomMinMax(1, 4);

            damageTaken -= Absorbed;

            if (damageTaken < 0)
                damageTaken = 0;

            if (!Invulnerable && 25 > Utility.Random(100)) // 25% chance to lower durability
            {
                if (Core.RuleSets.AOSRules() /*&& m_AosClothingAttributes.SelfRepair > Utility.Random(10)*/)
                {
                    HitPoints += 2;
                }
                else
                {
                    int wear;

                    // difference from RunUO: we say Slashing & Ranged do more top clothes than Bashing
                    if (weapon.Type == WeaponType.Slashing || weapon.Type == WeaponType.Ranged)
                        wear = Absorbed / 2;
                    else
                        wear = Utility.Random(2);

                    if (wear > 0 && m_MaxHitPoints > 0)
                    {
                        if (m_HitPoints >= wear)
                        {
                            HitPoints -= wear;
                            wear = 0;
                        }
                        else
                        {
                            wear -= HitPoints;
                            HitPoints = 0;
                        }

                        if (wear > 0)
                        {
                            if (m_MaxHitPoints > wear)
                            {
                                MaxHitPoints -= wear;

                                if (Parent is IEntity parent)
                                    parent.Notify(notification: Notification.ClothingStatus, this, "Your clothing is severely damaged.");
                            }
                            else
                            {
                                if (Parent is IEntity parent)
                                    parent.Notify(notification: Notification.Destroyed, this);
                                Delete();
                            }
                        }
                    }
                }
            }

            return damageTaken;
        }

        // erl: Added to prevent worn out clothing from being equipped

        public override bool OnEquip(Mobile from)
        {
            if (HitPoints == 0)
            {
                from.SendMessage("You feel that this clothing is too tattered to be worn.");
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            if (!Ethics.Ethic.CheckTrade(from, to, newOwner, this))
                return false;

            return base.AllowSecureTrade(from, to, newOwner, accepted);
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

        public void SetRandomMagicEffect(int MinLevel, int MaxLevel)
        {
            if (IsMagicAllowed())
            {
                if (MinLevel < 1 || MaxLevel > 3)
                    return;

                m_MagicEffect = (MagicEquipEffect)Utility.RandomList((int)MagicEquipEffect.SpellReflection,
                    (int)MagicEquipEffect.Invisibility, (int)MagicEquipEffect.Bless,
                    (int)MagicEquipEffect.Agility, (int)MagicEquipEffect.Cunning,
                    (int)MagicEquipEffect.Strength, (int)MagicEquipEffect.NightSight);

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
        }

        public bool IsMagicAllowed()
        {
            return Loot.MagicClothingTypes.Contains(GetType());
        }

        public virtual bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;
            else if (RootParent is Mobile && from != RootParent)
                return false;

            if (m_Dyable == false) // Added check for new Dyable bool
            {
                from.SendLocalizedMessage(sender.FailMessage);
                return false;
            }
            else
            {
                Hue = sender.DyedHue;

                return true;
            }
        }

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack.
                return false;
            }

            if (Ethics.Ethic.IsImbued(this))
            {
                from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
                return false;
            }

            CraftSystem system = DefTailoring.CraftSystem;

            CraftItem item = system.CraftItems.SearchFor(GetType());

            if (item != null && m_Scissorable && item.Resources.Count == 1 && item.Resources.GetAt(0).Amount >= 2)
            {
                try
                {
                    Type resourceType = null;

                    if (this is BaseShoes)
                    {
                        CraftResourceInfo info = CraftResources.GetInfo(((BaseShoes)this).Resource);

                        if (info != null && info.ResourceTypes.Length > 0)
                            resourceType = info.ResourceTypes[0];
                    }

                    // Undyed cloth gloves, when cut, produce 0-hued cloth.
                    if (this is ClothGloves && this.Hue == 1001)
                        this.Hue = 0;

                    // event dyed cloth, when cut, produce 0-hued cloth
                    if (this.m_EventHue != -1)
                        PopHue();

                    if (resourceType == null)
                        resourceType = item.Resources.GetAt(0).ItemType;

                    Item res = (Item)Activator.CreateInstance(resourceType);


                    ScissorHelper(scissors, from, res, PlayerCrafted ? (item.Resources.GetAt(0).Amount / 2) : 1);

                    res.LootType = LootType.Regular;

                    return true;
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }

            from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            return false;
        }

        #region ICraftable Members

        public virtual int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            Quality = (ClothingQuality)quality;

            if (makersMark)
                Crafter = from;

            if (DefaultResource != CraftResource.None)
            {
                Type resourceType = typeRes;

                if (resourceType == null)
                    resourceType = craftItem.Resources.GetAt(0).ItemType;

                Resource = CraftResources.GetFromType(resourceType);
            }
            #region Gloves
            else if ((this is BaseGloves) && (resHue == 0))
            {
                this.Hue = 1001;
                // Rhi: The default color for cloth gloves should be white, 
                // not the leather gloves color, which is what it will be if the hue is 0.
            }
            #endregion
            else
            {
                Hue = resHue;
            }

            CraftContext context = craftSystem.GetContext(from);

            if (context != null && context.DoNotColor)
                Hue = 0;

            #region Hitpoints
            // erl: give clothing initial hitpoint values
            int iMax = this.InitMaxHits;
            int iMin = this.InitMinHits;

            if (this.Quality >= ClothingQuality.Exceptional)
            {
                // Add 50% to both
                iMax = (iMax * 3) / 2; // Fixed order of precedence bug
                iMin = (iMin * 3) / 2;

                if (Core.RuleSets.AngelIslandRules())
                {
                    // make exceptional clothes newbied
                    this.LootType = LootType.Newbied;
                }
            }
            else if (this.Quality == ClothingQuality.Low)
            {
                // Lose 20% to both
                iMax = (iMax * 4) / 5;
                iMin = (iMin * 4) / 5;
            }

            this.HitPoints = this.MaxHitPoints = (short)Utility.RandomMinMax(iMin, iMax);
            #endregion

            return quality;
        }

#if old
		else if (item is BaseClothing)
					{
						BaseClothing clothing = (BaseClothing)item;
						clothing.Quality = (ClothingQuality)quality;
						endquality = quality;

						if (makersMark)
							clothing.Crafter = from;

						// Adam: one day we can obsolete and and use item.PlayerCrafted
						// erl: 10 Nov 05: that day is today!
						// clothing.SaveHue = true;

						if (item is BaseShoes)
						{
							BaseShoes shoes = (BaseShoes)item;

							if (shoes.Resource != CraftResource.None)
							{
								Type resourceType = typeRes;

								if (resourceType == null)
									resourceType = Ressources.GetAt(0).ItemType;

								shoes.Resource = CraftResources.GetFromType(resourceType);

								CraftContext context = craftSystem.GetContext(from);

								if (context != null && context.DoNotColor)
									shoes.Hue = 0;
							}
							else
							{
								shoes.Hue = resHue;
							}
						}
						else if ((item is BaseGloves) && (resHue == 0))
						{
							clothing.Hue = 1001;
							// Rhi: The default color for cloth gloves should be white, 
							// not the leather gloves color, which is what it will be if the hue is 0.
						}
						else
						{
							clothing.Hue = resHue;
						}

						// erl: give clothing initial hitpoint values
						// ..

						int iMax = clothing.InitMaxHits;
						int iMin = clothing.InitMinHits;

						if (clothing.Quality == ClothingQuality.Exceptional)
						{

							// Add 50% to both

							iMax = (iMax * 3) / 2; // Fixed order of precedence bug
							iMin = (iMin * 3) / 2;

							// make exceptional clothes newbied

							clothing.LootType = LootType.Newbied;
						}
						else if (clothing.Quality == ClothingQuality.Low)
						{
							// Lose 20% to both

							iMax = (iMax * 4) / 5; // Fixed order of precedence bug
							iMin = (iMin * 4) / 5;
						}

						clothing.HitPoints = clothing.MaxHitPoints = (short)Utility.RandomMinMax(iMin, iMax);

						// ..

					}
#endif

        #endregion

        #region Save Flags
        [Flags]
        enum SaveFlags
        {
            None = 0x0,
            HasExpiration = 0x01,
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
            if (version >= 10)
                sf = (SaveFlags)reader.ReadInt();
            return sf;
        }

        private void WriteSaveFlags(GenericWriter writer)
        {
            SetFlag(SaveFlags.HasExpiration, m_Expiration != DateTime.MinValue ? true : false);
            writer.Write((int)m_SaveFlags);
        }
        #endregion Save Flags

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)13);                  // version
            WriteSaveFlags(writer);                 // always follows version

            // version 10
            if (GetFlag(SaveFlags.HasExpiration) == true)
            {   // items that expire are usually event items that expire after the event
                //	expiration: sets alignment to none, charges to 0, region list to null
                writer.Write(m_Expiration);
            }

            // version 9
            writer.Write(m_EventHue);

            // version 8
            writer.Write((short)m_HitPoints);       // erl - added for clothing wear
            writer.Write((short)m_MaxHitPoints);    // erl - added for clothing wear
            writer.Write((bool)m_Scissorable);      // Adam - Addition for better Scissor control
            writer.Write((bool)m_Dyable);           // Adam - Save the dyable attr
            writer.Write((int)m_IOBAlignment);      // Pigpen - Addition for IOB System
            writer.Write((sbyte)m_MagicEffect);
            writer.Write((int)m_MagicCharges);
            writer.Write((bool)m_Identified);

            m_Crafter.Serialize(writer);
            writer.Write((int)m_Quality);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_SaveFlags = ReadSaveFlags(reader, version);

            switch (version)
            {
                case 13:
                case 12:
                case 11:
                case 10:
                    {   // adam: add flags and support for special expiring event items
                        if (GetFlag(SaveFlags.HasExpiration))
                            m_Expiration = reader.ReadDeltaTime();
                        else
                            m_Expiration = DateTime.MinValue;

                        goto case 9;
                    }
                case 9:
                    {
                        m_EventHue = reader.ReadInt();
                        goto case 8;
                    }
                case 8: //erl - added to handle packing out of SaveHue property
                    {
                        goto case 7;
                    }
                case 7: //erl - added for clothing wear
                    {
                        m_HitPoints = reader.ReadShort();
                        m_MaxHitPoints = reader.ReadShort();
                        goto case 6;
                    }
                case 6: //Adam - Addition for Scissorable attribute
                    {
                        m_Scissorable = reader.ReadBool();
                        goto case 5;
                    }
                case 5: //Adam - Addition for Dyable attribute
                    {
                        m_Dyable = reader.ReadBool();
                        goto case 4;
                    }
                case 4: //Pigpen - Addition for IOB System
                    {
                        m_IOBAlignment = (IOBAlignment)reader.ReadInt();
                        goto case 3;
                    }
                case 3:
                    {
                        if (version >= 12)
                            m_MagicEffect = (MagicEquipEffect)reader.ReadSByte();
                        else
                            m_MagicEffect = LegacyMagicEffect(reader.ReadInt());

                        m_MagicCharges = reader.ReadInt();
                        m_Identified = reader.ReadBool();
                        goto case 2;
                    }
                case 2:
                    {
                        // erl: this is the old SaveHue flag, which will no longer
                        // exist for anything over version 7... made obsolete by PlayerCrafted

                        if (version < 8)
                            PlayerCrafted = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        if (version >= 11)
                            m_Crafter.Deserialize(reader);
                        else
                            m_Crafter = reader.ReadMobile();

                        m_Quality = (ClothingQuality)reader.ReadInt();
                        break;
                    }
                case 0:
                    {
                        m_Quality = ClothingQuality.Regular;
                        break;
                    }
            }

            if (version < 5)                // Adam - addition for dye control
            {                               // Allow for other non-dyable clothes outside the IOB system
                if (m_IOBAlignment != IOBAlignment.None)
                    m_Dyable = false;
            }

            if (version < 7)
            {
                // erl: this pre-dates hit point additions, so calculate values
                // ..

                // Check the quality of the piece. If it's exceptional or low, we want
                // the piece's hitpoint to reflect this

                int iMax = InitMaxHits;
                int iMin = InitMinHits;

                if (Quality == ClothingQuality.Exceptional)
                {
                    // Add 50% to both

                    iMax = (iMax * 3) / 2; // Fixed order of precedence bug
                    iMin = (iMin * 3) / 2;
                }
                else if (Quality == ClothingQuality.Low)
                {
                    // Lose 20% to both

                    iMax = (iMax * 4) / 5; // Fixed order of precedence bug
                    iMin = (iMin * 4) / 5;
                }

                m_HitPoints = m_MaxHitPoints = (short)Utility.RandomMinMax(iMin, iMax);

            }

            // adam: To keep players from farming new characters for newbie clothes
            //	we are moving this valuable resource into the hands of crafters.
            if (version <= 7)
            {
                if (Quality == ClothingQuality.Exceptional && MagicCharges == 0)
                {
                    // make exceptional clothes newbied
                    LootType = LootType.Newbied;
                }
                else if (MagicCharges > 0)
                {
                    // erl: explicitly change these pieces so they aren't newbied
                    LootType = LootType.Regular;
                }
            }

            if (version < 13 && Invulnerable)
                HitPoints = MaxHitPoints;
        }

        public static MagicEquipEffect LegacyMagicEffect(int value)
        {
            switch (value)
            {
                case 0: return MagicEquipEffect.None;
                case 1: return MagicEquipEffect.SpellReflection;
                case 2: return MagicEquipEffect.Invisibility;
                case 3: return MagicEquipEffect.Bless;
                case 4: return MagicEquipEffect.Agility;
                case 5: return MagicEquipEffect.Cunning;
                case 6: return MagicEquipEffect.Strength;
                case 7: return MagicEquipEffect.NightSight;
                case 8: return MagicEquipEffect.Curse;
                case 9: return MagicEquipEffect.Clumsiness;
                case 10: return MagicEquipEffect.Feeblemind;
                case 11: return MagicEquipEffect.Weakness;
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