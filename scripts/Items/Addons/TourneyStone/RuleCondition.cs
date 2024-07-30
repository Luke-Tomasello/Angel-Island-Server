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

/* Scripts/Items/Addons/TourneyStone/RuleCondition.cs
 * ChangeLog :
 * 	02/25/07, weaver
 *		Initial creation (code moved out of TournamentStoneAddon.cs)
 *
 */

using System;
using System.Collections;

namespace Server.Items
{


    // RULE CONDITION =============================
    public abstract class RuleCondition
    {
        private Rule m_Rule;

        private int m_Quantity;
        private string m_Property;
        private string m_PropertyVal;
        private string m_ItemProperty;
        private Type m_ItemType;
        private bool m_Configurable;
        private int m_Limit;

        public Rule Rule
        {
            get { return m_Rule; }
            set { m_Rule = value; }
        }

        public int Quantity
        {
            get { return m_Quantity; }
            set { m_Quantity = value; }
        }
        public Type ItemType
        {
            get { return m_ItemType; }
            set { m_ItemType = value; }
        }
        public string Property
        {
            get { return m_Property; }
            set { m_Property = value; }
        }
        public string PropertyVal
        {
            get { return m_PropertyVal; }
            set { m_PropertyVal = value; }
        }
        public string ItemProperty
        {
            get { return m_ItemProperty; }
            set { m_ItemProperty = value; }
        }
        public bool Configurable
        {
            get { return m_Configurable; }
            set { m_Configurable = value; }
        }
        public int Limit
        {
            get { return m_Limit; }
            set { m_Limit = value; }
        }

        public virtual bool Guage(object o)
        {
            ArrayList empty = new ArrayList();
            return Guage(o, ref empty);
        }

        public abstract bool Guage(object o, ref ArrayList Fallthroughs);

        public RuleCondition()
        {
            m_ItemType = null;
            Quantity = 0;

            Property = "";
            PropertyVal = "";

            ItemProperty = "";
            Configurable = false;

            Limit = 1;
        }
    }
    // END CONDITION ==============================


}