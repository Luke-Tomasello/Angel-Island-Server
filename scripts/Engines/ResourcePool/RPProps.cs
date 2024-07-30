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

/* Scripts/Engines/ResourcePool/ResourcePool.cs
 * ChangeLog
 * 11/16/21, Yoar
 *      Integrated ML wood types into the BBS.
 * 11/16/21, Yoar
 *      BBS overhaul.
 *  06/02/05 TK
 *		Fixed having Leather types in RDRedirects instead of Hides
 *  04/02/05 TK
 *		Added special leather and cloth redirect types
 *	03/02/05 Taran Kain
 *		Created
 */

using Server.Items;

namespace Server.Engines.ResourcePool
{
    [NoSort]
    [PropertyObject]
    public class RPProps
    {
        private static RPProps m_Instance;

        public static RPProps Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new RPProps();

                return m_Instance;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Arrows { get { return ResourcePool.GetResource(typeof(Arrow)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Bolts { get { return ResourcePool.GetResource(typeof(Bolt)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Shafts { get { return ResourcePool.GetResource(typeof(Shaft)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Feathers { get { return ResourcePool.GetResource(typeof(Feather)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Iron { get { return ResourcePool.GetResource(typeof(IronIngot)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData DullCopper { get { return ResourcePool.GetResource(typeof(DullCopperIngot)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData ShadowIron { get { return ResourcePool.GetResource(typeof(ShadowIronIngot)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Copper { get { return ResourcePool.GetResource(typeof(CopperIngot)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Bronze { get { return ResourcePool.GetResource(typeof(BronzeIngot)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Gold { get { return ResourcePool.GetResource(typeof(GoldIngot)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Agapite { get { return ResourcePool.GetResource(typeof(AgapiteIngot)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Verite { get { return ResourcePool.GetResource(typeof(VeriteIngot)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Valorite { get { return ResourcePool.GetResource(typeof(ValoriteIngot)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Cloth { get { return ResourcePool.GetResource(typeof(Cloth)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Leather { get { return ResourcePool.GetResource(typeof(Leather)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData SpinedLeather { get { return ResourcePool.GetResource(typeof(SpinedLeather)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData HornedLeather { get { return ResourcePool.GetResource(typeof(HornedLeather)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData BarbedLeather { get { return ResourcePool.GetResource(typeof(BarbedLeather)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData Boards { get { return ResourcePool.GetResource(typeof(Board)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData OakBoards { get { return ResourcePool.GetResource(typeof(OakBoard)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData AshBoards { get { return ResourcePool.GetResource(typeof(AshBoard)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData YewBoards { get { return ResourcePool.GetResource(typeof(YewBoard)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData HeartwoodBoards { get { return ResourcePool.GetResource(typeof(HeartwoodBoard)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData BloodwoodBoards { get { return ResourcePool.GetResource(typeof(BloodwoodBoard)); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public static ResourceData FrostwoodBoards { get { return ResourcePool.GetResource(typeof(FrostwoodBoard)); } set { } }

        private RPProps()
        {
        }

        public override string ToString()
        {
            return "...";
        }
    }
}