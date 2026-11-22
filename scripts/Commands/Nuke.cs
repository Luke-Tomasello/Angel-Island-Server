/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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


/* Scripts/Commands/Nuke.cs
 * CHANGELOG:
 *  History before 11/21/2026 (the one-time patches, reports and tests Nuke carried since 2005): svn r1428 and earlier,
 *      or this file's GitHub history.
 *  11/21/2026, Adam (standing commands only)
 *      Nuke keeps its standing commands; the one-time patches, reports and tests (about 230) are gone:
 *          Run <dll> [args]    run a dynamic patch from Patches\ against the live world
 *          DressAdam           dress the caller (Owner only) in Adam's outfit
 *          BuildAdam           target a player: all skills 100; str, dex, int, fame and karma 30000
 *          BuildWarrior        target a player: Swords, Tactics, Anatomy, Healing, Parry, Resist, Lumberjacking at 100;
 *                              str 100, dex 100, int 25
 *          BuildMage           target a player: Swords, Tactics, Anatomy, Healing, Magery, Resist, Meditation at 100;
 *                              str 90, dex 35, int 100
 *          MakeMurderer        target a player: a murderer (5 counts), or innocent again (0) if already one
 *      BuildWarrior and BuildMage resurrect and cure the player first, and clear every other skill.
 *      BuildWarrior trained Magery and Meditation (a copy of BuildMage's skills): it trains Parry and Lumberjacking.
 *      Commands are a table keyed by the method's own name (nameof), matched in any case; the usage lists them.
 *      Moved out: CopyLayers / RestoreLayers to PlayerHistory (player history, Doppelganger); ExpInfo to Patches001_100
 *      (PatchV92); SetAllSkills and RemoveAllStaff to Patches101_200 (the patches that use them).
 *  11/21/2026, Adam (Nuke Run: a fresh copy every run)
 *      [Nuke Run loads the DLL's bytes (and its .pdb) into a new collectible AssemblyLoadContext each run, unloaded once
 *      nothing uses it: the file is never locked, a rebuilt harness copies into Patches\ and runs at once, and every run
 *      starts with fresh static state. It loaded with Assembly.LoadFrom, which kept the first copy and locked the file
 *      until a restart (a rebuilt harness failed to copy, silently, and the old code ran).
 *  11/5/2026, Adam (DressAdam)
 *      Add [Nuke DressAdam: dresses the caller (Owner only) in Adam's outfit (captured by the AdamOutfit harness). Gear worn
 *      or held goes to the backpack, else the bank box, else it is deleted; nothing is dropped.
 *  10/11/2026, Adam (Nuke Run)
 *      [Nuke Run <name> [args...] loads Patches\<name>.dll and calls its one public static int? Run(Mobile from,
 *      string[] args) against the live world, reporting the result like the patch runner. DynPatchTest is a read-only
 *      test and template.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Commands
{
    public class Nuke
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Nuke", AccessLevel.Administrator, new CommandEventHandler(Nuke_OnCommand));
        }

        // [Nuke <command>: any case. Add a command as { nameof(Method), Method } (the name is the method's own name).
        private static readonly Dictionary<string, Action<CommandEventArgs>> m_Commands = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Run", e => LoadDll(e) },
            { nameof(DressAdam), DressAdam },
            { nameof(BuildAdam), BuildAdam },
            { nameof(BuildWarrior), BuildWarrior },
            { nameof(BuildMage), BuildMage },
            { nameof(MakeMurderer), MakeMurderer },
        };

        [Usage("Nuke <command> [arguments...]")]
        [Description("Staff tools: run a dynamic patch (Run), dress Adam, build test characters.")]
        private static void Nuke_OnCommand(CommandEventArgs e)
        {
            string usage = string.Format("Usage: Nuke {0} [arguments...]", string.Join("|", m_Commands.Keys));
            if (e.Arguments.Length < 1)
            {
                e.Mobile.SendMessage(usage);
                return;
            }

            if (!m_Commands.TryGetValue(e.Arguments[0], out Action<CommandEventArgs>? command))
            {
                e.Mobile.SendMessage("Nuke does not implement the command: {0}", e.Arguments[0]);
                e.Mobile.SendMessage(usage);
                return;
            }

            try
            {
                command(e);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        #region Build Player
        private static void BuildAdam(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to construct...");
            e.Mobile.Target = new BuildAdamTarget(); // Call our target
        }
        public class BuildAdamTarget : Target
        {
            public BuildAdamTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    // all skills 100
                    Server.Skills skills = player.Skills;
                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = 100.0;

                    // now for the stats
                    player.RawDex = 0;
                    player.RawInt = 0;
                    player.RawStr = 0;
                    player.Stam = player.RawDex = 30000;
                    player.Mana = player.RawInt = 30000;
                    player.Hits = player.RawStr = 30000;

                    player.Karma = 30000;
                    player.Fame = 30000;
                    from.SendMessage("Adam built.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        // Adam's official outfit, captured from Adam Ant (0x00013237) by the AdamOutfit harness, 11/5/2026. Dresses the caller
        //  (Owner only, no target): everything worn or held (not the backpack, bank box, hair, beard or mount) goes to the
        //  backpack, else the bank box, else it is deleted (never dropped), then the outfit is created and equipped.
        private static void DressAdam(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m.AccessLevel < AccessLevel.Owner)
            {
                m.SendMessage("DressAdam is for the Owner.");
                return;
            }

            int packed = 0, banked = 0, deleted = 0;
            List<Item> worn = m.Items.Where(i => i != null && !i.Deleted && i.Layer != Layer.Backpack && i.Layer != Layer.Bank
                && i.Layer != Layer.Hair && i.Layer != Layer.FacialHair && i.Layer != Layer.Mount).ToList();
            foreach (Item item in worn)
            {
                if (m.Backpack != null && m.Backpack.TryDropItem(m, item, false))
                    packed++;
                else if (m.BankBox != null && m.BankBox.TryDropItem(m, item, false))
                    banked++;
                else
                {
                    item.Delete();
                    deleted++;
                }
            }

            // Resource first (it resets Hue and rescales durability), then Quality, hit points (max first), and Hue last
            void Wear(Item item)
            {
                if (!m.EquipItem(item))
                {
                    m.AddToBackpack(item);
                    m.SendMessage("Could not equip the {0}; it is in your backpack.", item.GetType().Name);
                }
            }
            {   // Katana: exceptional valorite, crafted by Adam
                var item = new Katana();
                item.Resource = CraftResource.Valorite;
                item.Quality = WeaponQuality.Exceptional;
                item.MaxHitPoints = 49;
                item.HitPoints = 49;
                item.Hue = 2219;
                item.PlayerCrafted = true;
                item.Crafter = m;
                item.Identified = true;
                Wear(item);
            }
            {   // Sandals
                var item = new Sandals();
                item.MaxHitPoints = 45;
                item.HitPoints = 45;
                item.Hue = 1644;
                item.LootType = LootType.Newbied;
                Wear(item);
            }
            {   // ChainLegs: gold
                var item = new ChainLegs();
                item.Resource = CraftResource.Gold;
                item.MaxHitPoints = 87;
                item.HitPoints = 86;
                item.Hue = 2213;
                Wear(item);
            }
            {   // BloodDrenchedBandana
                var item = new BloodDrenchedBandana();
                item.MaxHitPoints = 45;
                item.HitPoints = 44;
                item.LootType = LootType.Newbied;
                Wear(item);
            }
            {   // ChainChest: gold
                var item = new ChainChest();
                item.Resource = CraftResource.Gold;
                item.MaxHitPoints = 90;
                item.HitPoints = 89;
                item.Hue = 2213;
                Wear(item);
            }
            {   // RingmailArms: gold
                var item = new RingmailArms();
                item.Resource = CraftResource.Gold;
                item.MaxHitPoints = 61;
                item.HitPoints = 61;
                item.Hue = 2213;
                Wear(item);
            }
            {   // Kilt
                var item = new Kilt();
                item.MaxHitPoints = 45;
                item.HitPoints = 45;
                item.Hue = 1;
                item.LootType = LootType.Newbied;
                Wear(item);
            }

            m.SendMessage("Adam is dressed. Previous gear: {0} to the backpack, {1} to the bank box, {2} deleted (no room).", packed, banked, deleted);
        }
        private static void BuildMage(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to construct...");
            e.Mobile.Target = new BuildMageTarget(); // Call our target
        }

        private static void NormalizePlayer(Mobile m)
        {
            if (m.Dead)
                m.Resurrect();
            //m.Blessed = false;
            //m.AccessLevel = AccessLevel.Player;
            m.BodyValue = 400;

            if (m.Poison != null)
                m.CurePoison(m);

            if (!Core.RuleSets.MortalisRules())
            {
                if (m is PlayerMobile pm)
                    pm.Mortal = false;
            }
        }
        public class BuildMageTarget : Target
        {
            public BuildMageTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    NormalizePlayer(player);

                    // first clear all the skills
                    Server.Skills skills = player.Skills;
                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = 0.0;

                    // build us a mage
                    skills[SkillName.Swords].Base = 100.0;
                    skills[SkillName.Anatomy].Base = 100.0;
                    skills[SkillName.Tactics].Base = 100.0;
                    skills[SkillName.Healing].Base = 100.0;
                    skills[SkillName.Magery].Base = 100.0;
                    skills[SkillName.MagicResist].Base = 100.0;
                    skills[SkillName.Meditation].Base = 100.0;

                    // now for the stats
                    player.RawDex = 0;
                    player.RawInt = 0;
                    player.RawStr = 0;
                    player.Stam = player.RawDex = 35;
                    player.Mana = player.RawInt = 100;
                    player.Hits = player.RawStr = 90;

                    from.SendMessage("Mage built.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        private static void BuildWarrior(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to construct...");
            e.Mobile.Target = new BuildWarriorTarget(); // Call our target
        }
        public class BuildWarriorTarget : Target
        {
            public BuildWarriorTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    NormalizePlayer(player);

                    // first clear all the skills
                    Server.Skills skills = player.Skills;
                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = 0.0;

                    // build us a warrior
                    skills[SkillName.Swords].Base = 100.0;
                    skills[SkillName.Anatomy].Base = 100.0;
                    skills[SkillName.Tactics].Base = 100.0;
                    skills[SkillName.Healing].Base = 100.0;
                    skills[SkillName.Parry].Base = 100.0;
                    skills[SkillName.MagicResist].Base = 100.0;
                    skills[SkillName.Lumberjacking].Base = 100.0;

                    // now for the stats
                    player.RawDex = 0;
                    player.RawInt = 0;
                    player.RawStr = 0;
                    player.Stam = player.RawDex = 100;
                    player.Mana = player.RawInt = 25;
                    player.Hits = player.RawStr = 100;

                    from.SendMessage("Warrior built.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        #endregion Build Player
        #region Make Murderer / Innocent
        private static void MakeMurderer(CommandEventArgs e)
        {

            e.Mobile.SendMessage("Target player to make/unmake a murderer...");
            e.Mobile.Target = new MakeMurdererTarget(); // Call our target
        }
        public class MakeMurdererTarget : Target
        {
            public MakeMurdererTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is PlayerMobile player)
                {
                    if (player.ShortTermMurders + player.LongTermMurders >= 5)
                    {
                        player.ShortTermMurders = player.LongTermMurders = 0;
                        from.SendMessage("Murder counts cleared.");
                    }
                    else
                    {
                        player.ShortTermMurders = player.LongTermMurders = 5;
                        from.SendMessage("Murder counts set.");
                    }

                    from.SendMessage("Done.");
                }
                else
                {
                    from.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        #endregion Make Murderer / Innocent
        #region Load DLL
        // [Nuke Run <dll-name> [arguments...]]
        //  Loads <ExeDirectory>/Patches/<dll-name>.dll and calls its one public static method
        //      public static int? Run(Mobile from, string[] args)
        //  A dynamic patch is a PatchV-style implementation run against the live, already-loaded world
        //  without rebuilding or restarting the server (see DynPatchTest). Like a PatchV, it returns the
        //  number of objects updated, or null when it does not apply to this shard/server.
        //  Live world only: no PreWorldLoad, no WorldScrubber, no patch history or FeatureBits bookkeeping.
        //  Dynamic patches are trusted, disposable admin code. Each run loads a fresh copy (its bytes, into its own
        //  collectible AssemblyLoadContext): the file is never locked, so a rebuilt DLL runs at once, and every run starts
        //  with fresh static state. The copy unloads once nothing uses it (a timer it scheduled keeps it until it fires).
        //  Its types must never live in the world or a save (they couldn't load at the next start anyway).
        private static bool LoadDll(CommandEventArgs e)
        {
            if (e.Arguments.Length < 2)
            {
                e.Mobile.SendMessage("Usage: Nuke Run <dll-name> [arguments...]");
                return false;
            }

            // a bare file name only: no paths, no traversal
            string name = e.Arguments[1];
            if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z0-9_][A-Za-z0-9_.\-]*$") || name.Contains(".."))
            {
                e.Mobile.SendMessage("Nuke Run: invalid dll name '{0}'. Give a bare name from the Patches folder, e.g. DynPatchTest.", e.Arguments[1]);
                return false;
            }

            string directory = Path.GetFullPath(Path.Combine(Core.ExeDirectory, "Patches"));
            string path = Path.GetFullPath(Path.Combine(directory, name + ".dll"));
            if (!string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase))
            {
                e.Mobile.SendMessage("Nuke Run: invalid dll name '{0}'.", e.Arguments[1]);
                return false;
            }

            if (!File.Exists(path))
            {
                e.Mobile.SendMessage("Nuke Run: {0} not found.", path);
                return false;
            }

            string[] args = e.Arguments.Skip(2).ToArray();
            Console.WriteLine("Nuke Run: {0} invoked {1} with [{2}]", e.Mobile, path, string.Join(", ", args));

            System.Reflection.MethodInfo method;
            // a fresh copy each run, from its bytes: GMN Core and the other assemblies it uses resolve to the server's own
            //  (the default context), so it works on the live world's types
            System.Runtime.Loader.AssemblyLoadContext context =
                new System.Runtime.Loader.AssemblyLoadContext(string.Format("Nuke Run {0} {1:HHmmss.fff}", name, DateTime.Now), isCollectible: true);
            try
            {
                System.Reflection.Assembly assembly;
                string pdb = Path.ChangeExtension(path, ".pdb");
                using (MemoryStream image = new MemoryStream(File.ReadAllBytes(path)))
                using (MemoryStream? symbols = File.Exists(pdb) ? new MemoryStream(File.ReadAllBytes(pdb)) : null)
                    assembly = context.LoadFromStream(image, symbols);

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    foreach (Exception le in ex.LoaderExceptions)
                        if (le != null)
                            Console.WriteLine("Nuke Run: {0} loader exception: {1}", name, le.Message);
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                List<System.Reflection.MethodInfo> candidates = new List<System.Reflection.MethodInfo>();
                foreach (Type type in types)
                {
                    if (!type.IsPublic)
                        continue;

                    System.Reflection.MethodInfo m = type.GetMethod("Run",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly,
                        null, new Type[] { typeof(Mobile), typeof(string[]) }, null);

                    if (m != null && m.ReturnType == typeof(int?))
                        candidates.Add(m);
                }

                if (candidates.Count == 0)
                {
                    e.Mobile.SendMessage("Nuke Run: {0} has no public static int? Run(Mobile, string[]).", name);
                    context.Unload();
                    return false;
                }

                if (candidates.Count > 1)
                {
                    e.Mobile.SendMessage("Nuke Run: {0} has {1} public static int? Run(Mobile, string[]) entry points ({2}); expected one.",
                        name, candidates.Count, string.Join(", ", candidates.Select(c => c.DeclaringType.FullName)));
                    context.Unload();
                    return false;
                }

                method = candidates[0];
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Nuke Run: cannot load {0}: {1}", path, ex.Message);
                Console.WriteLine("Nuke Run: cannot load {0}", path);
                LogHelper.LogException(ex);
                context.Unload();
                return false;
            }

            int? result;
            try
            {
                result = (int?)method.Invoke(null, new object[] { e.Mobile, args });
            }
            catch (Exception ex)
            {
                Exception inner = (ex is System.Reflection.TargetInvocationException && ex.InnerException != null) ? ex.InnerException : ex;
                e.Mobile.SendMessage("Nuke Run: {0}.Run threw {1}: {2}", method.DeclaringType.FullName, inner.GetType().Name, inner.Message);
                Console.WriteLine("Nuke Run: {0}.Run ({1}) threw:", method.DeclaringType.FullName, path);
                Console.WriteLine(inner);
                LogHelper.LogException(inner);
                return false;
            }
            finally
            {
                // unloads once nothing uses it (its scheduled timers, if any, keep it until they've run)
                context.Unload();
            }

            // same interpretation as the normal patch runner
            string outcome = result.HasValue
                ? string.Format("Nuke Run: {0} patched with {1} objects updated", name, result.Value)
                : string.Format("Nuke Run: {0} did not apply to this shard/server", name);
            e.Mobile.SendMessage(outcome);
            Console.WriteLine(outcome);
            return true;
        }
        #endregion Load DLL
    }
}