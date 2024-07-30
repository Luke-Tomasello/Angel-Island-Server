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

/* Scripts/Mobiles/Special/Christmas/WildHunt/WildHunt.cs
 * ChangeLog
 *  1/1/24, Yoar
 *		Initial Version.
 */

using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles.WildHunt
{
    public static class WildHunt
    {
        #region Names & Titles

        public static string RandomName(bool female)
        {
            return Utility.RandomList(female ? m_FemaleNames : m_MaleNames);
        }

        public static string RandomTitle()
        {
            return Utility.RandomList(m_Titles);
        }

        private static readonly string[] m_FemaleNames = new string[]
            {
                //"Astridr",
                "Hildr",
                "Sigrun",
                "Freya",
                "Eir",
                "Ragnhild",
                //"Brynhildr",
                "Gunnhild",
                "Thyra",
                "Alfhild",
                "Ingrid",
                "Ulfhild",
                "Gudrun",
                "Sigrid",
                "Helga",
                "Karin",
                "Ylva",
                "Liv",
                "Thora",
                "Runa",
                "Svana",
            };

        private static readonly string[] m_MaleNames = new string[]
            {
                "Torvald",
                "Jorund",
                "Hakon",
                "Asger",
                //"Bjorn",
                "Dagmar",
                "Einar",
                "Frode",
                "Gunnar",
                "Halvor",
                "Ivar",
                "Jarl",
                "Kjell",
                "Leif",
                "Magnus",
                "Njord",
                "Olaf",
                "Peder",
                "Roar",
                "Sven",
                "Trygve",
                "Ulf",
                "Vidar",
                "Wulfgar",
                "Yngvar",
                "Arvid",
                "Brandt",
                "Cedric",
                "Dvalin",
                "Erik",
            };

        private static readonly string[] m_Titles = new string[]
            {
                "the Valiant",
                "the Fierce",
                "the Battleborn",
                "the Unyielding",
                "the Wise",
                "the Just",
                "the Fearless",
                "the Bold",
                "the Victorious",
                "the Secret-Keeper",
                "the Shadow-Walker",
                "the Blade-Dancer",
                "the Truth-Seeker",
                "the Justice-Bringer",
                "the Rune-Wielder",
                "the Storm-Rider",
                "the Flame-Keeper",
                "the North-Watcher",
                "the Ancient-Voiced",
                "the Path-Walker",
                "the Stormbringer",
                "the Quiet",
                "the Resolute",
                "the Wanderer",
                "the Guardian",
                "the Peacemaker",
                "the Stern",
                "the Merciful",
                "the Avenger",
                "the Far-Seeing",
                "the Protector",
                "the Brave",
                "the Silent",
                "the Watchful",
                "the Listener",
                "the Swift",
                "the Kind",
                "the Boldhearted",
                "the Unseen",
                "the Defender",
                "the Sage",
                "the Mighty",
                "the Loyal",
                "the Fearbreaker",
                "the Sun-Chaser",
                "the Moon-Caller",
                "the Star-Gazer",
                "the Lightbearer",
                "the Shadowbinder",
                "the Earth-Shaper",
            };

        #endregion

        #region Outfit

        public static Item SetImmovable(Item item)
        {
            item.Movable = false;
            return item;
        }

        public static Item SetItemID(int itemID, Item item)
        {
            item.ItemID = itemID;
            return item;
        }

        public static Item SetHue(int hue, Item item)
        {
            item.Hue = hue;
            return item;
        }

        public static Item SetName(string name, Item item)
        {
            item.Name = name;
            return item;
        }

        public static Item Imbue(int level, Item item)
        {
            if (item is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)item;

                armor.ProtectionLevel = (ArmorProtectionLevel)level;
                armor.DurabilityLevel = (ArmorDurabilityLevel)level;
                armor.HideAttributes = true;
            }
            else if (item is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)item;

                weapon.DamageLevel = (WeaponDamageLevel)level;
                weapon.AccuracyLevel = (WeaponAccuracyLevel)level;
                weapon.DurabilityLevel = (WeaponDurabilityLevel)level;
                weapon.HideAttributes = true;
            }

            return item;
        }

        public static Item SetLayer(Layer layer, Item item)
        {
            item.Layer = layer;
            return item;
        }

        public static BaseShield RandomShield()
        {
            switch (Utility.Random(3))
            {
                default:
                case 0: return new WoodenShield();
                case 1: return new MetalShield();
                case 2: return new BronzeShield();
            }
        }

        public static BaseRanged RandomBow()
        {
            switch (Utility.Random(3))
            {
                default:
                case 0: return new Bow();
                case 1: return new Crossbow();
                case 2: return new HeavyCrossbow();
            }
        }

        public static int RandomHair()
        {
            switch (Utility.Random(3))
            {
                default:
                case 0: return 0x203C; // long hair
                case 1: return 0x203D; // pony tail
                case 2: return 0x2049; // pig tails
            }
        }

        public static int RandomFacialHair()
        {
            switch (Utility.Random(3))
            {
                default:
                case 0: return 0x203E; // long beard
                case 1: return 0x203F; // short beard
                case 2: return 0x2041; // moustache
            }
        }

        #endregion

        public static bool IsValidHarmful(BaseCreature source, Mobile target)
        {
            if (source == target || !target.Alive || target.IsDeadBondedPet || !source.CanSee(target) || !source.InLOS(target) || !source.CanBeHarmful(target))
                return false;

            if (source.IsFriend(target))
                return false;

            if (source.IsEnemy(target))
                return true;

            if (target.Player || IsFollower(target))
                return true;

            return false;
        }

        public static bool IsValidBeneficial(BaseCreature source, Mobile target)
        {
            if (source == target || !target.Alive || target.IsDeadBondedPet || !source.CanSee(target) || !source.InLOS(target) || !source.CanBeBeneficial(target))
                return false;

            if (source.IsEnemy(target))
                return false;

            return true;
        }

        public static bool IsFollower(Mobile m)
        {
            BaseCreature bc = m as BaseCreature;

            return (bc != null && (bc.Controlled || bc.Summoned));
        }

        public static void SortByDistance(Point3D source, List<Mobile> list)
        {
            list.Sort(MobileSorter.Get(source));
        }

        private class MobileSorter : IComparer<Mobile>
        {
            private static MobileSorter m_Instance;

            public static MobileSorter Get(Point3D source)
            {
                if (m_Instance == null)
                    m_Instance = new MobileSorter();

                m_Instance.m_Source = source;

                return m_Instance;
            }

            private Point3D m_Source;

            private MobileSorter()
            {
            }

            int IComparer<Mobile>.Compare(Mobile x, Mobile y)
            {
                int xDist = GetDistanceSquared(m_Source, x.Location);
                int yDist = GetDistanceSquared(m_Source, y.Location);

                return xDist.CompareTo(yDist);
            }

            private static int GetDistanceSquared(Point3D src, Point3D trg)
            {
                int dx = trg.X - src.X;
                int dy = trg.Y - src.Y;

                return (dx * dx + dy * dy);
            }
        }

        public static BaseCreature Construct(Type type, params object[] args)
        {
            if (!typeof(BaseCreature).IsAssignableFrom(type))
                return null;

            BaseCreature bc;

            try
            {
                bc = (BaseCreature)Activator.CreateInstance(type, args);
            }
            catch
            {
                bc = null;
            }

            return bc;
        }

        #region Mob Registry & Hits Scaling

        public static readonly List<BaseCreature> m_Registry = new List<BaseCreature>();

        public static List<BaseCreature> Registry { get { return m_Registry; } }

        public static int HitsScalar { get { return Misc.WinterEventSystem.WildHuntHitsScalar; } }

        public static void Register(BaseCreature bc)
        {
            if (!World.Loading)
                ScaleHits(bc);

            m_Registry.Add(bc);
        }

        public static void Unregister(BaseCreature bc)
        {
            m_Registry.Remove(bc);
        }

        public static void ScaleHits()
        {
            foreach (BaseCreature bc in m_Registry.ToArray()) // sanity - loop over array
                ScaleHits(bc);
        }

        public static void UnscaleHits()
        {
            foreach (BaseCreature bc in m_Registry.ToArray()) // sanity - loop over array
                UnscaleHits(bc);
        }

        private static void ScaleHits(BaseCreature bc)
        {
            bc.HitsMaxSeed = Scale(bc.HitsMaxSeed, HitsScalar);
            bc.Hits = Scale(bc.Hits, HitsScalar);
        }

        private static void UnscaleHits(BaseCreature bc)
        {
            bc.HitsMaxSeed = Unscale(bc.HitsMaxSeed, HitsScalar);
            bc.Hits = Unscale(bc.Hits, HitsScalar);
        }

        private static int Scale(int value, int scalar)
        {
            return scalar * value / 100;
        }

        private static int Unscale(int value, int scalar)
        {
            return 100 * value / scalar;
        }

        #endregion
    }
}