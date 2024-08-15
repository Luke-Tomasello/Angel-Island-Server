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

/* Scripts/Commands/Abstracted/Implementors/ObjectConditional.cs
 * CHANGELOG
 *  6/4/23, Yoar
 *      CheckCondition now returns true if (1) the type matches and (2) no conditions where given
 *  6/4/23, Yoar
 *      Fixed CheckCondition so that it properly deals with OR conditions
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Targeting;
using System;
using System.Collections;
using System.Reflection;

namespace Server.Commands
{
    public class ObjectConditional
    {
        private Type m_Type;
        private PropertyCondition[][] m_Conditions;

        private static Type typeofItem = typeof(Item);
        private static Type typeofMobile = typeof(Mobile);

        public Type Type
        {
            get { return m_Type; }
        }

        public bool IsItem
        {
            get { return (m_Type == null || m_Type == typeofItem || m_Type.IsSubclassOf(typeofItem)); }
        }

        public bool IsMobile
        {
            get { return (m_Type == null || m_Type == typeofMobile || m_Type.IsSubclassOf(typeofMobile)); }
        }

        public static readonly ObjectConditional Empty = new ObjectConditional(null, new PropertyCondition[0][]);

        public static void Initialize()
        {
            Server.CommandSystem.Register("Condition", AccessLevel.Administrator, new CommandEventHandler(Condition_OnCommand));
        }

        [Usage("Condition <condition>")]
        [Description("Checks that the given condition matches a targeted object.")]
        private static void Condition_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(Condition_OnTarget), e.Arguments);
        }

        private static void Condition_OnTarget(Mobile from, object obj, object state)
        {
            string[] args = (string[])state;
            ObjectConditional cond = Parse(from, ref args);

            from.SendMessage(cond.CheckCondition(obj).ToString());
        }

        public bool CheckCondition(object obj)
        {
            if (m_Type == null)
                return true; // null type means no condition

            Type objType = obj.GetType();

            if (objType != m_Type && !objType.IsSubclassOf(m_Type))
                return false;

            if (m_Conditions.Length == 0)
                return true; // no conditions specified

            for (int i = 0; i < m_Conditions.Length; ++i)
            {
                PropertyCondition[] conditions = m_Conditions[i];

                bool res = true;

                for (int j = 0; res && j < conditions.Length; ++j)
                    res = conditions[j].CheckCondition(obj);

                if (res)
                    return true;
            }

            return false; // all conditions false
        }

        public static ObjectConditional Parse(Mobile from, ref string[] args)
        {
            string[] conditionArgs = null;

            for (int i = 0; i < args.Length; ++i)
            {
                if (Insensitive.Equals(args[i], "where"))
                {
                    string[] origArgs = args;

                    args = new string[i];

                    for (int j = 0; j < args.Length; ++j)
                        args[j] = origArgs[j];

                    conditionArgs = new string[origArgs.Length - i - 1];

                    for (int j = 0; j < conditionArgs.Length; ++j)
                        conditionArgs[j] = origArgs[i + j + 1];

                    break;
                }
            }

            if (conditionArgs == null || conditionArgs.Length == 0)
                return ObjectConditional.Empty;

            int index = 0;

            Type type = ScriptCompiler.FindTypeByName(conditionArgs[index++], true);

            if (type == null)
                throw new Exception(string.Format("No type with that name ({0}) was found.", conditionArgs[0]));

            PropertyInfo[] props = type.GetProperties();

            ArrayList allConditions = new ArrayList();
            ArrayList currentConditions = null;

            while (index < conditionArgs.Length)
            {
                string cur = conditionArgs[index];

                bool logicalNot = false;

                if (Insensitive.Equals(cur, "not") || cur == "!")
                {
                    logicalNot = true;
                    ++index;

                    if (index >= conditionArgs.Length)
                        throw new Exception("Improperly formatted object conditional.");
                }
                else if (Insensitive.Equals(cur, "or") || cur == "||")
                {
                    if (currentConditions != null)
                    {
                        allConditions.Add(currentConditions);
                        currentConditions = null;
                    }

                    ++index;

                    continue;
                }

                string prop = conditionArgs[index++];

                if (index >= conditionArgs.Length)
                    throw new Exception("Improperly formatted object conditional.");

                string oper = conditionArgs[index++];

                if (index >= conditionArgs.Length)
                    throw new Exception("Improperly formatted object conditional.");

                string arg = conditionArgs[index++];

                if (currentConditions == null)
                    currentConditions = new ArrayList();

                currentConditions.Add(new PropertyCondition(from, type, props, prop, oper, arg, logicalNot));
            }

            if (currentConditions != null)
                allConditions.Add(currentConditions);

            PropertyCondition[][] conditions = new PropertyCondition[allConditions.Count][];

            for (int i = 0; i < conditions.Length; ++i)
                conditions[i] = (PropertyCondition[])(((ArrayList)allConditions[i]).ToArray(typeof(PropertyCondition)));

            return new ObjectConditional(type, conditions);
        }

        public ObjectConditional(Type type, PropertyCondition[][] conditions)
        {
            m_Type = type;
            m_Conditions = conditions;
        }
    }

    public enum ConditionOperator
    {
        // all
        Equality,
        Inequality,

        // numerical
        Greater,
        GreaterEqual,
        Lesser,
        LesserEqual,

        // strings
        EqualityInsensitive,
        InequalityInsensitive,
        StartsWith,
        StartsWithInsensitive,
        EndsWith,
        EndsWithInsensitive,
        Contains,
        ContainsInsensitive
    }

    public class PropertyCondition
    {
        private PropertyInfo[] m_PropertyInfoChain;
        private ConditionOperator m_Operator;
        private Mobile m_From;
        private object m_Argument;
        private bool m_LogicalNot;

        public PropertyCondition(Mobile from, Type type, PropertyInfo[] props, string prop, string oper, string arg, bool logicalNot)
        {
            m_From = from;
            m_LogicalNot = logicalNot;

            string failReason = "";
            m_PropertyInfoChain = Properties.GetPropertyInfoChain(from, type, prop, true, ref failReason);

            if (m_PropertyInfoChain == null)
                throw new Exception(failReason);

            /*for ( int i = 0; i < props.Length; ++i )
			{
				PropertyInfo check = props[i];

				if ( !Insensitive.Equals( check.Name, prop ) )
					continue;

				m_Property = check;
				break;
			}

			if ( m_Property == null )
				throw new Exception( string.Format( "No property with the name ({0}) was found on type ({1}).", prop, type.Name ) );

			CPA attr = Properties.GetCPA( m_Property );

			if ( attr == null )
				throw new Exception( string.Format( "No property with the name ({0}) was found on type ({1}).", prop, type.Name ) );

			if ( from.AccessLevel < attr.ReadLevel )
				throw new Exception( string.Format( "Getting this property ({0}) requires at least {1} access level.", prop, Mobile.GetAccessLevelName( attr.ReadLevel ) ) );*/

            string error = Properties.ConstructFromString(m_PropertyInfoChain[m_PropertyInfoChain.Length - 1].PropertyType, null, arg, ref m_Argument);

            if (error != null)
                throw new Exception(error);

            switch (oper)
            {
                case "=":
                case "==":
                case "is": m_Operator = ConditionOperator.Equality; break;

                case "!=": m_Operator = ConditionOperator.Inequality; break;

                case ">": m_Operator = ConditionOperator.Greater; break;
                case "<": m_Operator = ConditionOperator.Lesser; break;

                case ">=": m_Operator = ConditionOperator.GreaterEqual; break;
                case "<=": m_Operator = ConditionOperator.LesserEqual; break;

                case "==~":
                case "~==":
                case "=~":
                case "~=":
                case "is~":
                case "~is": m_Operator = ConditionOperator.EqualityInsensitive; break;

                case "!=~":
                case "~!=": m_Operator = ConditionOperator.InequalityInsensitive; break;

                case "starts": m_Operator = ConditionOperator.StartsWith; break;

                case "starts~":
                case "~starts": m_Operator = ConditionOperator.StartsWithInsensitive; break;

                case "ends": m_Operator = ConditionOperator.EndsWith; break;

                case "ends~":
                case "~ends": m_Operator = ConditionOperator.EndsWithInsensitive; break;

                case "contains": m_Operator = ConditionOperator.Contains; break;

                case "contains~":
                case "~contains": m_Operator = ConditionOperator.ContainsInsensitive; break;
            }

            if (m_Operator != ConditionOperator.Equality && m_Operator != ConditionOperator.Inequality)
            {
                if (m_Argument != null && !(m_Argument is IComparable))
                    throw new Exception(string.Format("This property ({0}) is not comparable.", prop));
            }
        }

        public string AsString(object obj)
        {
            if (obj == null)
                return "";

            if (obj is string)
                return (string)obj;

            return obj.ToString();
        }

        public bool Equality(object obj)
        {
            if (obj == null && m_Argument == null)
                return true;

            if (obj == null || m_Argument == null)
                return false;

            return obj.Equals(m_Argument);
        }

        public int CompareWith(object obj)
        {
            if (obj == null && m_Argument == null)
                return 0;

            if (obj == null)
                return -1;

            if (m_Argument == null)
                return 1;

            if (!(obj is IComparable))
                throw new Exception(string.Format("This property ({0}) returned an incomparable object of type {1}.", m_PropertyInfoChain[m_PropertyInfoChain.Length - 1].Name, obj.GetType()));

            return ((IComparable)obj).CompareTo(m_Argument);
        }

        public bool CheckCondition(object obj)
        {
            bool ret;

            string failReason = null;
            PropertyInfo endProp = Properties.GetPropertyInfo(ref obj, m_PropertyInfoChain, ref failReason);

            if (endProp == null)
                return false;

            object current = endProp.GetValue(obj, null);

            switch (m_Operator)
            {
                case ConditionOperator.Equality:
                    ret = Equality(current); break;

                case ConditionOperator.Inequality:
                    ret = !Equality(current); break;

                case ConditionOperator.Greater:
                    ret = (CompareWith(current) > 0); break;

                case ConditionOperator.GreaterEqual:
                    ret = (CompareWith(current) >= 0); break;

                case ConditionOperator.Lesser:
                    ret = (CompareWith(current) < 0); break;

                case ConditionOperator.LesserEqual:
                    ret = (CompareWith(current) <= 0); break;

                case ConditionOperator.EqualityInsensitive:
                    ret = (Insensitive.Equals(AsString(current), AsString(m_Argument))); break;

                case ConditionOperator.InequalityInsensitive:
                    ret = (!Insensitive.Equals(AsString(current), AsString(m_Argument))); break;

                case ConditionOperator.StartsWith:
                    ret = (AsString(current).StartsWith(AsString(m_Argument))); break;

                case ConditionOperator.StartsWithInsensitive:
                    ret = (Insensitive.StartsWith(AsString(current), AsString(m_Argument))); break;

                case ConditionOperator.EndsWith:
                    ret = (AsString(current).EndsWith(AsString(m_Argument))); break;

                case ConditionOperator.EndsWithInsensitive:
                    ret = (Insensitive.EndsWith(AsString(current), AsString(m_Argument))); break;

                case ConditionOperator.Contains:
                    ret = (AsString(current).IndexOf(AsString(m_Argument)) >= 0); break;

                case ConditionOperator.ContainsInsensitive:
                    ret = (Insensitive.Contains(AsString(current), AsString(m_Argument))); break;

                default: return false;
            }

            if (m_LogicalNot)
                ret = !ret;

            return ret;
        }
    }
}