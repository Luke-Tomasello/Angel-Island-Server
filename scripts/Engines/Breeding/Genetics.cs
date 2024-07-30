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

/* Scripts\Engines\Breeding\Genetics.cs
 * Changelog:
 * 10/22/21, Yoar
 *      Changed MinVariance logic: Its added variance no longer "spills over" BreedMax.
 *      This makes obtaining high-value genes harder.
 * 10/21/21, Yoar
 *      Added "params string[] genes" argument to InitGenes, SetGenes to specify exactly
 *      which genes need to be set.
 * 10/20/21, Yoar: Breeding System overhaul
 *      Initial version. This class deals with genetics. It contains cached instances of
 *      PropertyInfo for the appropriate creature types. Using these cached instances,
 *      we can quickly get/set gene values from any type of creature using reflection.
 *      Additionally, gene initializing/mixing was moved from BaseCreature.cs to here.
 */

using Server.Commands;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Server.Engines.Breeding
{
    public static class Genetics
    {
        private static readonly TypeConverter m_DoubleConv = TypeDescriptor.GetConverter(typeof(double));

        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> m_Cache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public static void Initialize()
        {
            TargetCommands.Register(new SetGenesCommand());
        }

        private class SetGenesCommand : BaseCommand
        {
            public SetGenesCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "SetGenes" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "SetGenesCommand <Init|SpawnMin|SpawnMax|BreedMin|BreedMax>";
                Description = "Set the genes of the targeted creature.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (!(obj is BaseCreature))
                {
                    LogFailure("That is not a creature.");
                    return;
                }

                SetType type;

                if (!Enum.TryParse(e.GetString(0), true, out type))
                {
                    LogFailure(String.Format("Invalid key \"{0}\".", e.GetString(0)));
                    return;
                }

                SetGenes((BaseCreature)obj, type);

                AddResponse("Done.");
            }
        }

        public static bool HasGene(Type type, string gene)
        {
            return GetProperty(type, gene) != null;
        }

        public static double GetValue(object obj, string gene)
        {
            return GetValue(obj, gene, 0.0);
        }

        public static double GetValue(object obj, string gene, double defaultValue)
        {
            double value;

            if (TryGetValue(obj, gene, out value))
                return value;

            return defaultValue;
        }

        public static bool TryGetValue(object obj, string gene, out double value)
        {
            PropertyInfo prop = GetProperty(obj.GetType(), gene);

            if (prop != null)
            {
                try
                {
                    value = Convert.ToDouble(prop.GetValue(obj, null));
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Genetics Error: {0}", e);
                }
            }

            value = 0.0;
            return false;
        }

        public static bool SetValue(object obj, string gene, double value)
        {
            PropertyInfo prop = GetProperty(obj.GetType(), gene);

            if (prop != null)
            {
                try
                {
                    prop.SetValue(obj, m_DoubleConv.ConvertTo(value, prop.PropertyType), null);
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Genetics Error: {0}", e);
                }
            }

            return false;
        }

        private static PropertyInfo GetProperty(Type type, string gene)
        {
            if (!typeof(BaseCreature).IsAssignableFrom(type) || type.IsAbstract)
                return null;

            Dictionary<string, PropertyInfo> dict;

            if (!m_Cache.TryGetValue(type, out dict))
                m_Cache[type] = dict = new Dictionary<string, PropertyInfo>();

            PropertyInfo prop;

            if (dict.TryGetValue(gene, out prop))
                return prop;

            try
            {
                foreach (PropertyInfo check in type.GetProperties())
                {
                    GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(check, typeof(GeneAttribute), true);

                    if (attr != null && attr.Name == gene)
                        return dict[gene] = check;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Genetics Error: {0}", e);
            }

            return null;
        }

        public enum SetType : byte
        {
            Init,
            SpawnMin,
            SpawnMax,
            BreedMin,
            BreedMax,
        }

        public static void InitGenes(BaseCreature bc, params string[] genes)
        {
            SetGenes(bc, SetType.Init, genes);
        }

        public static void SetGenes(BaseCreature bc, SetType type, params string[] genes)
        {
            bool filter = (genes != null && genes.Length != 0);

            try
            {
                PropertyInfo[] props = bc.GetType().GetProperties();

                for (int i = 0; i < props.Length; i++)
                {
                    PropertyInfo prop = props[i];

                    GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(prop, typeof(GeneAttribute), true);

                    if (attr == null)
                        continue;

                    if (filter && Array.IndexOf(genes, attr.Name) == -1)
                        continue;

                    double value;

                    switch (type)
                    {
                        default:
                        case SetType.Init:
                            {
                                value = attr.SpawnMin + Utility.RandomDouble() * (attr.SpawnMax - attr.SpawnMin);
                                break;
                            }
                        case SetType.SpawnMin: value = attr.SpawnMin; break;
                        case SetType.SpawnMax: value = attr.SpawnMax; break;
                        case SetType.BreedMin: value = attr.BreedMin; break;
                        case SetType.BreedMax: value = attr.BreedMax; break;
                    }

                    prop.SetValue(bc, m_DoubleConv.ConvertTo(value, prop.PropertyType), null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Genetics Error: {0}", e);
            }
        }

        public static void Recombination(BaseCreature child, BaseCreature mom, BaseCreature dad)
        {
            Type childsType = child.GetType();
            Type momsType = mom.GetType();
            Type dadsType = dad.GetType();

            try
            {
                PropertyInfo[] childsProps, momsProps, dadsProps;

                childsProps = childsType.GetProperties();

                if (momsType == childsType)
                    momsProps = childsProps;
                else
                    momsProps = momsType.GetProperties();

                if (dadsType == childsType)
                    dadsProps = childsProps;
                else if (dadsType == momsType)
                    dadsProps = momsProps;
                else
                    dadsProps = dadsType.GetProperties();

                for (int i = 0; i < childsProps.Length; i++)
                {
                    PropertyInfo prop = childsProps[i];

                    GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(prop, typeof(GeneAttribute), true);

                    if (attr == null)
                        continue;

                    if (momsType != childsType && Array.IndexOf(momsProps, prop) == -1)
                        continue;

                    if (dadsType != childsType && Array.IndexOf(dadsProps, prop) == -1)
                        continue;

                    double low = Convert.ToDouble(prop.GetValue(mom, null));
                    double high = Convert.ToDouble(prop.GetValue(dad, null));

                    if (low > high)
                    {
                        double t = low;
                        low = high;
                        high = t;
                    }

                    double lowVar = attr.LowFactor * (attr.BreedMax - attr.BreedMin);
                    double highVar = attr.HighFactor * (attr.BreedMax - attr.BreedMin);

                    double remainder = attr.MinVariance - (highVar + lowVar);

                    if (remainder > 0.0)
                    {
                        double toAdd = Math.Max(0.0, Math.Min(attr.BreedMax - high, remainder / 2.0));

                        highVar += toAdd;
                        lowVar += (remainder - toAdd);
                    }

                    if (prop.PropertyType == typeof(int) && attr.MinVariance > 0.0)
                    {
                        if (lowVar > 0.0 && lowVar < 1.0)
                            lowVar = 1.0;

                        if (highVar > 0.0 && highVar < 1.0)
                            highVar = 1.0;
                    }

                    double lowRange = low - lowVar;
                    double highRange = high + highVar;

                    if (lowRange > highRange)
                    {
                        double t = lowRange;
                        lowRange = highRange;
                        highRange = t;
                    }

                    double childVal = lowRange + Utility.RandomDouble() * (highRange - lowRange);

                    if (childVal < attr.BreedMin)
                        childVal = attr.BreedMin;

                    if (childVal > attr.BreedMax)
                        childVal = attr.BreedMax;

                    prop.SetValue(child, m_DoubleConv.ConvertTo(childVal, prop.PropertyType), null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Genetics Error: {0}", e);
            }
        }
    }
}