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

/* Scripts\Engines\Breeding\BreedingSim.cs
 * Changelog:
 * 10/20/21, Yoar: Breeding System overhaul
 *      Initial version. Simulates the following breeding process:
 *      
 *      Let's say I want to maximize one gene, e.g. Physique. I start with 4 dragons. Then I do:
 *      1. Mate the 2 strongest 3 times (+3 babbies).
 *      2. Mate the 2 weakest 3 times (+3 babbies).
 *      3. Tame 2 more dragons (+2 fresh stock).
 *      4. Of the 8 new dragons, select the best 4 and wait for them to grow up.
 *      Repeat!
 */

using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.Breeding
{
    public static class BreedingSim
    {
        private const string FileName = "BreedingSim.log";

        public static void Initialize()
        {
            CommandSystem.Register("BreedingSim", AccessLevel.Administrator, new CommandEventHandler(BreedingSim_OnCommand));
        }

        [Usage("BreedingSim <type> <gene> <batchSize> <reproductiveFactor> <iterations>")]
        [Description("Runs a breeding simulation: Optimizes a given gene over several generations. Results are exported to \"" + FileName + "\".")]
        private static void BreedingSim_OnCommand(CommandEventArgs e)
        {
            DateTime start = DateTime.UtcNow;

            e.Mobile.SendMessage("Running breeding sim...");

            OutResult res;

            try
            {
                res = Run(
                    ScriptCompiler.FindTypeByName(GetOrDefault(e.Length >= 1, e.GetString, 0, "Dragon")),
                    GetOrDefault(e.Length >= 2, e.GetString, 1, "Physique"),
                    GetOrDefault(e.Length >= 3, e.GetInt32, 2, 4),
                    GetOrDefault(e.Length >= 4, e.GetInt32, 3, 3),
                    GetOrDefault(e.Length >= 5, e.GetInt32, 4, 20));
            }
            catch (Exception exc)
            {
                e.Mobile.SendMessage("Error: {0}", exc);
                return;
            }

            e.Mobile.SendMessage(string.Format("Done! It took {0:F3} seconds. Results are exported to " + FileName + ".", (DateTime.UtcNow - start).TotalSeconds));
            e.Mobile.SendMessage(string.Format("Max. value of {0:F3} after {1} iteration{2}.", res.Value, res.Iter, res.Iter == 1 ? "" : "s"));
        }

        private delegate T ArgGetter<T>(int index);

        private static T GetOrDefault<T>(bool condition, ArgGetter<T> getter, int index, T defaultValue)
        {
            if (condition)
                return getter(index);
            else
                return defaultValue;
        }

        private static OutResult Run(Type type, string gene, int batchSize, int reproductiveFactor, int iterations)
        {
            if (type == null)
                throw new ArgumentNullException("The value of \"type\" cannot be null.");
            if (!typeof(BaseCreature).IsAssignableFrom(type))
                throw new ArgumentException(string.Format("Type \"{0}\" does not derive from \"BaseCreature\".", type.Name));
            if (!Genetics.HasGene(type, gene))
                throw new ArgumentException(string.Format("Type \"{0}\" does not define the gene \"{1}\".", type.Name, gene));
            if (batchSize < 2)
                throw new ArgumentException(string.Format("The value of \"batchSize\" must be greater than or equal to 2.", type.Name));
            if (reproductiveFactor < 1)
                throw new ArgumentException(string.Format("The value of \"reproductiveFactor\" must be greater than or equal to 1.", type.Name));
            if (iterations < 0)
                throw new ArgumentException(string.Format("The value of \"iterations\" must be greater than or equal to 0.", type.Name));

            GeneComparer comp = new GeneComparer(gene);

            BaseCreature[] batch = new BaseCreature[batchSize];

            int outIter = -1;
            double outValue = double.MinValue;

            using (TextWriter writer = File.CreateText(Path.Combine("Logs", FileName)))
            {
                writer.WriteLine(DateTime.UtcNow);

                writer.WriteLine("type={0}", type.Name);
                writer.WriteLine("gene={0}", gene);
                writer.WriteLine("batchSize={0}", batchSize);
                writer.WriteLine("reproductiveFactor={0}", reproductiveFactor);
                writer.WriteLine("iterations={0}", iterations);

                writer.Write("iter");

                for (int i = 0; i < batch.Length; i++)
                    writer.Write("\tmob{0}", i + 1);

                writer.WriteLine();

                for (int i = 0; i < iterations; i++)
                {
                    List<BaseCreature> nextGen = new List<BaseCreature>();

                    int momIdx = -1;

                    // create offspring
                    for (int j = batch.Length - 1; j >= 0; j--)
                    {
                        if (batch[j] != null)
                        {
                            if (momIdx == -1)
                            {
                                momIdx = j; // we have a mom, now look for a dad...
                                continue;
                            }

                            for (int k = 0; k < reproductiveFactor; k++)
                            {
                                BaseCreature child = Construct(type);

                                if (child != null)
                                {
                                    Genetics.Recombination(child, batch[momIdx], batch[j]);

                                    nextGen.Add(child);
                                }
                            }

                            momIdx = -1;
                        }
                    }

                    // for every child, also add a fresh specimen
                    for (int j = nextGen.Count - 1; j >= 0; j--)
                    {
                        BaseCreature fresh = Construct(type);

                        if (fresh != null)
                            nextGen.Add(fresh);
                    }

                    // sort next generation by gene value in ascending order
                    nextGen.Sort(comp);

                    int index = nextGen.Count - 1;

                    // create a new batch with the best specimen
                    for (int j = batch.Length - 1; j >= 0; j--)
                    {
                        if (batch[j] != null)
                            batch[j].Delete();

                        if (index >= 0)
                            batch[j] = nextGen[index--];
                        else
                            batch[j] = Construct(type);
                    }

                    // delete the remaining specimen
                    while (index >= 0)
                        nextGen[index--].Delete();

                    // sort the batch by gene value in ascending order
                    Array.Sort(batch, comp);

                    double[] results = new double[batch.Length];

                    // collect results
                    for (int j = 0; j < batch.Length; j++)
                    {
                        if (batch[j] != null)
                            results[j] = Genetics.GetValue(batch[j], gene);
                    }

                    // write results
                    writer.WriteLine(string.Format("{0}\t{1}", i + 1, string.Join("\t", results)));

                    double value = results[results.Length - 1];

                    if (value > outValue)
                    {
                        outIter = i + 1;
                        outValue = value;
                    }
                }
            }

            for (int i = 0; i < batch.Length; i++)
            {
                if (batch[i] != null)
                    batch[i].Delete();
            }

            return new OutResult(outIter, outValue);
        }

        private static BaseCreature Construct(Type type)
        {
            try
            {
                return (BaseCreature)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                Console.WriteLine("Genetics Error: {0}", e);
                return null;
            }
        }

        private class GeneComparer : IComparer<BaseCreature>
        {
            private string m_Gene;

            public GeneComparer(string gene)
            {
                m_Gene = gene;
            }

            public int Compare(BaseCreature a, BaseCreature b)
            {
                return Genetics.GetValue(a, m_Gene).CompareTo(Genetics.GetValue(b, m_Gene));
            }
        }

        private struct OutResult
        {
            public int Iter;
            public double Value;

            public OutResult(int iter, double value)
            {
                Iter = iter;
                Value = value;
            }
        }
    }
}