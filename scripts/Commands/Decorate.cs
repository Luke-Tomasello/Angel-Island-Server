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

/* Scripts/Commands/Decorate.cs
 * CHANGELOG
 *  9/20/22, Adam (Generate)
 *      1. 'Decorate' isn't quite granular enough:
 *          Add the optional parameter to Generate to allow callers to specify a specific file to work with.
 *          For example, after Decorate, we call:
 *          Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), "_champion teleporters.cfg", DecoMode.delete, maps: new Map[] { Map.Felucca });
 *          to delete champ spawners
 *      2. Add List Only mode where no world changes take place, but a list of proposed changes are returned.
 *      3. We no longer 'skip Trammel (notes 2/23/11, adam)
 *          we now wipe all maps, then only regenerate Map.Felucca
 *          I.e., kill Map.Trammel, Map.Ilshenar, Map.Malas, and Map.Tokuno
 *      4. Add both types(to include[only] in Decorate), and exclude(to exclude from decorate)
 *          I.e., types specifies only these types will be included, while exclude removed these types from decorate.
 *          For example, this mechanism allows you to decorate only with spawners. Or, you can also decorate with everything excluding Spawners and Teleporters.
 *          Very important for a customized shard.
 *  8/30/22, Adam
 *      Replace usage of ObjectNames with ObjectNamesRaw
 *      Note: Not sure Decorate is even useful given our new spawner model. Should probably decommission it.
 *	2/23/11, adam
 *		redesigned based on RunUO 2.0
 *		o) Make a 'wipe' pass first so as to clear any incorrect items. For instance, AI used LibraryBookcases instead of FullBookcases.
 *		o) now deletes N items from a destination as previous Decorate commands would just shove the new item there on top of the old item
 *		o) explicitly 'skip' trammel so we can compare old (AI 1.0) and new deco
 *	3/18/07, Pix
 *		Commented out all Decorate_OnCommand references to Trammel, Malas, Ilshenar.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using Server.Engines.Quests.Haven;
using Server.Engines.Quests.Necro;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Server.Commands
{
    public class Decorate
    {
        public enum DecoMode
        {
            add,
            delete,
            skip
        }

        public static void Initialize()
        {
            CommandSystem.Register("Decorate", AccessLevel.Owner, new CommandEventHandler(Decorate_OnCommand));
        }

        [Usage("Decorate")]
        [Description("Generates world decoration.")]
        private static void Decorate_OnCommand(CommandEventArgs e)
        {
            m_Mobile = e.Mobile;
            m_Count = 0;

            m_Mobile.SendMessage("Generating world decoration, please wait.");

            // first wipe existing deco.
            //	we do this to update deco that may be wrong (librarybookcase vs fullbookcase)
            //	or has the wrong attributes (LightCircle for example.)
            //  It also kills deco on maps we don't use
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.delete, maps: new Map[] { Map.Trammel });                  // skip trammel deco so we can compare to new felucca
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.delete, maps: new Map[] { Map.Felucca });
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Trammel"), DecoMode.delete, maps: new Map[] { Map.Trammel });                    // skip trammel deco so we can compare to new felucca
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), DecoMode.delete, maps: new Map[] { Map.Felucca });
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Ilshenar"), DecoMode.delete, maps: new Map[] { Map.Ilshenar });
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Malas"), DecoMode.delete, maps: new Map[] { Map.Malas });
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Tokuno"), DecoMode.delete, maps: new Map[] { Map.Tokuno });

            m_Mobile.SendMessage("World generating complete. {0} items were wiped.", m_Count);
            m_Count = 0;

            Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.skip, maps: new Map[] { Map.Trammel });                  // skip trammel deco so we can compare to new felucca
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.add, maps: new Map[] { Map.Felucca });
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Trammel"), DecoMode.skip, maps: new Map[] { Map.Trammel });                    // skip trammel deco so we can compare to new felucca
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), DecoMode.add, maps: new Map[] { Map.Felucca });
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Ilshenar"), DecoMode.skip, maps: new Map[] { Map.Ilshenar });                   // sure deco this, we sometimes use these areas
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Malas"), DecoMode.skip, maps: new Map[] { Map.Malas });                        // not at this time (waste of items)
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Tokuno"), DecoMode.skip, maps: new Map[] { Map.Tokuno });                      // not at this time (waste of items)

            m_Mobile.SendMessage("World generating complete. {0} items were generated.", m_Count);
        }

        public static Dictionary<Item, Tuple<Point3D, Map, bool>>
            Generate(string folder, DecoMode mode, string file = "*.cfg", bool listOnly = false,
            Type[] types = null, Type[] exclude = null, List<Rectangle2D> boundingRect = null, params Map[] maps)
        {
            Dictionary<Item, Tuple<Point3D, Map, bool>> changeLog = new();
            if (!Directory.Exists(folder))
                return changeLog;

            string[] files = Directory.GetFiles(folder, file);

            for (int i = 0; i < files.Length; ++i)
            {
                ArrayList list = DecorationList.ReadAll(files[i]);

                for (int j = 0; j < list.Count; ++j)
                {
                    Dictionary<Item, Tuple<Point3D, Map, bool>> tmp = new();
                    m_Count += ((DecorationList)list[j]).Generate(mode, maps, listOnly, tmp, types, exclude, boundingRect);
                    foreach (KeyValuePair<Item, Tuple<Point3D, Map, bool>> kvp in tmp)
                        if (mode == DecoMode.add && kvp.Value.Item3)
                            if (changeLog.ContainsKey(kvp.Key))
                                ;
                            else
                                changeLog.Add(kvp.Key, kvp.Value);
                        else if (mode == DecoMode.delete && !kvp.Value.Item3)
                            if (changeLog.ContainsKey(kvp.Key))
                                ;
                            else
                                changeLog.Add(kvp.Key, kvp.Value);
                }
            }

            return changeLog;
        }

        private static Mobile m_Mobile;
        private static int m_Count;
    }

    public class DecorationList
    {
        private Type m_Type;
        private int m_ItemID;
        private string[] m_Params;
        private ArrayList m_Entries;

        public DecorationList()
        {
        }

        private static Type typeofStatic = typeof(Static);
        private static Type typeofLocalizedStatic = typeof(LocalizedStatic);
        private static Type typeofBaseDoor = typeof(BaseDoor);
        private static Type typeofAnkhWest = typeof(AnkhWest);
        private static Type typeofAnkhNorth = typeof(AnkhNorth);
        private static Type typeofBeverage = typeof(BaseBeverage);
        private static Type typeofLocalizedSign = typeof(LocalizedSign);
        private static Type typeofMarkContainer = typeof(MarkContainer);
        private static Type typeofWarningItem = typeof(WarningItem);
        private static Type typeofHintItem = typeof(HintItem);
        private static Type typeofCannon = typeof(Cannon);
        private static Type typeofSerpentPillar = typeof(SerpentPillar);

        public Item Construct()
        {
            Item item;

            try
            {
                if (m_Type == typeofStatic)
                {
                    item = new Static(m_ItemID);
                }
                else if (m_Type == typeofLocalizedStatic)
                {
                    int labelNumber = 0;

                    for (int i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWith("LabelNumber"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                            {
                                labelNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                                break;
                            }
                        }
                    }

                    item = new LocalizedStatic(m_ItemID, labelNumber);
                }
                else if (m_Type == typeofLocalizedSign)
                {
                    int labelNumber = 0;

                    for (int i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWith("LabelNumber"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                            {
                                labelNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                                break;
                            }
                        }
                    }

                    item = new LocalizedSign(m_ItemID, labelNumber);
                }
                else if (m_Type == typeofAnkhWest || m_Type == typeofAnkhNorth)
                {
                    bool bloodied = false;

                    for (int i = 0; !bloodied && i < m_Params.Length; ++i)
                        bloodied = (m_Params[i] == "Bloodied");

                    if (m_Type == typeofAnkhWest)
                        item = new AnkhWest(bloodied);
                    else
                        item = new AnkhNorth(bloodied);
                }
                else if (m_Type == typeofMarkContainer)
                {
                    bool bone = false;
                    bool locked = false;
                    Map map = Map.Malas;

                    for (int i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i] == "Bone")
                        {
                            bone = true;
                        }
                        else if (m_Params[i] == "Locked")
                        {
                            locked = true;
                        }
                        else if (m_Params[i].StartsWith("TargetMap"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                map = Map.Parse(m_Params[i].Substring(++indexOf));
                        }
                    }

                    MarkContainer mc = new MarkContainer(bone, locked);

                    mc.TargetMap = map;
                    mc.Description = "strange location";

                    item = mc;
                }
                else if (m_Type == typeofHintItem)
                {
                    int range = 0;
                    int messageNumber = 0;
                    string messageString = null;
                    int hintNumber = 0;
                    string hintString = null;
                    TimeSpan resetDelay = TimeSpan.Zero;

                    for (int i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWith("Range"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                range = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("WarningString"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                messageString = m_Params[i].Substring(++indexOf);
                        }
                        else if (m_Params[i].StartsWith("WarningNumber"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                messageNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("HintString"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                hintString = m_Params[i].Substring(++indexOf);
                        }
                        else if (m_Params[i].StartsWith("HintNumber"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                hintNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("ResetDelay"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                resetDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                        }
                    }

                    HintItem hi = new HintItem(m_ItemID, range, messageNumber, hintNumber);

                    hi.WarningString = messageString;
                    hi.HintString = hintString;
                    hi.ResetDelay = resetDelay;

                    item = hi;
                }
                else if (m_Type == typeofWarningItem)
                {
                    int range = 0;
                    int messageNumber = 0;
                    string messageString = null;
                    TimeSpan resetDelay = TimeSpan.Zero;

                    for (int i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWith("Range"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                range = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("WarningString"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                messageString = m_Params[i].Substring(++indexOf);
                        }
                        else if (m_Params[i].StartsWith("WarningNumber"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                messageNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("ResetDelay"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                resetDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                        }
                    }

                    WarningItem wi = new WarningItem(m_ItemID, range, messageNumber);

                    wi.WarningString = messageString;
                    wi.ResetDelay = resetDelay;

                    item = wi;
                }
                else if (m_Type == typeofCannon)
                {
                    CannonDirection direction = CannonDirection.North;

                    for (int i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWith("CannonDirection"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                direction = (CannonDirection)Enum.Parse(typeof(CannonDirection), m_Params[i].Substring(++indexOf), true);
                        }
                    }

                    item = new Cannon(direction);
                }
                else if (m_Type == typeofSerpentPillar)
                {
                    string word = null;
                    Rectangle2D destination = new Rectangle2D();

                    for (int i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWith("Word"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                word = m_Params[i].Substring(++indexOf);
                        }
                        else if (m_Params[i].StartsWith("DestStart"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                destination.Start = Point2D.Parse(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("DestEnd"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                destination.End = Point2D.Parse(m_Params[i].Substring(++indexOf));
                        }
                    }

                    item = new SerpentPillar(word, destination);
                }
                else if (m_Type.IsSubclassOf(typeofBeverage))
                {
                    BeverageType content = BeverageType.Liquor;
                    bool fill = false;

                    for (int i = 0; !fill && i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWith("Content"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                            {
                                content = (BeverageType)Enum.Parse(typeof(BeverageType), m_Params[i].Substring(++indexOf), true);
                                fill = true;
                            }
                        }
                    }

                    if (fill)
                        item = (Item)Activator.CreateInstance(m_Type, new object[] { content });
                    else
                        item = (Item)Activator.CreateInstance(m_Type);
                }
                else if (m_Type.IsSubclassOf(typeofBaseDoor))
                {
                    DoorFacing facing = DoorFacing.WestCW;

                    for (int i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWith("Facing"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                            {
                                facing = (DoorFacing)Enum.Parse(typeof(DoorFacing), m_Params[i].Substring(++indexOf), true);
                                break;
                            }
                        }
                    }

                    item = (Item)Activator.CreateInstance(m_Type, new object[] { facing });
                }
                else
                {
                    item = (Item)Activator.CreateInstance(m_Type);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Bad type: {0}", m_Type), e);
            }

            if (item is BaseAddon)
            {
                if (item is MaabusCoffin)
                {
                    MaabusCoffin coffin = (MaabusCoffin)item;

                    for (int i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWith("SpawnLocation"))
                        {
                            int indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                coffin.SpawnLocation = Point3D.Parse(m_Params[i].Substring(++indexOf));
                        }
                    }
                }
                else if (m_ItemID > 0)
                {
                    ArrayList comps = ((BaseAddon)item).Components;

                    for (int i = 0; i < comps.Count; ++i)
                    {
                        AddonComponent comp = comps[i] as AddonComponent;

                        if (comp == null)
                            continue;

                        if (comp.Offset == Point3D.Zero)
                            comp.ItemID = m_ItemID;
                    }
                }
            }
            else if (item is BaseLight)
            {
                bool unlit = false, unprotected = false;

                for (int i = 0; i < m_Params.Length; ++i)
                {
                    if (!unlit && m_Params[i] == "Unlit")
                        unlit = true;
                    else if (!unprotected && m_Params[i] == "Unprotected")
                        unprotected = true;

                    if (unlit && unprotected)
                        break;
                }

                if (!unlit)
                    ((BaseLight)item).Ignite();
                if (!unprotected)
                    ((BaseLight)item).Protected = true;

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (item is Spawner)
            {
                Spawner sp = (Spawner)item;

                sp.NextSpawn = TimeSpan.Zero;

                for (int i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWith("Spawn"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.ObjectNamesRaw.Add(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MinDelay"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.MinDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MaxDelay"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.MaxDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("NextSpawn"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.NextSpawn = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Count"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.Count = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Team"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.Team = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("HomeRange"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.HomeRange = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Running"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.Running = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Group"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.Group = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                }
            }
            else if (item is RecallRune)
            {
                RecallRune rune = (RecallRune)item;

                for (int i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWith("Description"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            rune.Description = m_Params[i].Substring(++indexOf);
                    }
                    else if (m_Params[i].StartsWith("Marked"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            rune.Marked = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("TargetMap"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            rune.TargetMap = Map.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Target"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            rune.Target = Point3D.Parse(m_Params[i].Substring(++indexOf));
                    }
                }
            }
            else if (item is SkillTeleporter)
            {
                SkillTeleporter tp = (SkillTeleporter)item;

                for (int i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWith("Skill"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Skill = (SkillName)Enum.Parse(typeof(SkillName), m_Params[i].Substring(++indexOf), true);
                    }
                    else if (m_Params[i].StartsWith("RequiredFixedPoint"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Required = Utility.ToInt32(m_Params[i].Substring(++indexOf)) * 0.01;
                    }
                    else if (m_Params[i].StartsWith("Required"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Required = Utility.ToDouble(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MessageString"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MessageString = m_Params[i].Substring(++indexOf);
                    }
                    else if (m_Params[i].StartsWith("MessageNumber"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MessageNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("PointDest"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.PointDest = Point3D.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MapDest"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MapDest = Map.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Creatures"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Pets = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SourceEffect"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SourceEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("DestEffect"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.DestEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SoundID"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SoundID = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Delay"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Delay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }
                }

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (item is KeywordTeleporter)
            {
                KeywordTeleporter tp = (KeywordTeleporter)item;

                for (int i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWith("Substring"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Substring = m_Params[i].Substring(++indexOf);
                    }
                    else if (m_Params[i].StartsWith("Keyword"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Keyword = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Range"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Range = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("PointDest"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.PointDest = Point3D.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MapDest"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MapDest = Map.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Creatures"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Pets = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SourceEffect"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SourceEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("DestEffect"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.DestEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SoundID"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SoundID = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Delay"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Delay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }
                }

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (item is Teleporter)
            {
                Teleporter tp = (Teleporter)item;

                for (int i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWith("PointDest"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.PointDest = Point3D.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MapDest"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MapDest = Map.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Creatures"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Pets = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SourceEffect"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SourceEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("DestEffect"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.DestEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SoundID"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SoundID = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Delay"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Delay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }
                }

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (item is FillableContainer)
            {
                FillableContainer cont = (FillableContainer)item;

                for (int i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWith("ContentType"))
                    {
                        int indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            cont.ContentType = (FillableContentType)Enum.Parse(typeof(FillableContentType), m_Params[i].Substring(++indexOf), true);
                    }
                }

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (m_ItemID > 0)
            {
                item.ItemID = m_ItemID;
            }

            item.Movable = false;

            for (int i = 0; i < m_Params.Length; ++i)
            {
                if (m_Params[i].StartsWith("Light"))
                {
                    int indexOf = m_Params[i].IndexOf('=');

                    if (indexOf >= 0)
                        item.Light = (LightType)Enum.Parse(typeof(LightType), m_Params[i].Substring(++indexOf), true);
                }
                else if (m_Params[i].StartsWith("Hue"))
                {
                    int indexOf = m_Params[i].IndexOf('=');

                    if (indexOf >= 0)
                    {
                        int hue = Utility.ToInt32(m_Params[i].Substring(++indexOf));

                        if (item is DyeTub)
                            ((DyeTub)item).DyedHue = hue;
                        else
                            item.Hue = hue;
                    }
                }
                else if (m_Params[i].StartsWith("Name"))
                {
                    int indexOf = m_Params[i].IndexOf('=');

                    if (indexOf >= 0)
                        item.Name = m_Params[i].Substring(++indexOf);
                }
                else if (m_Params[i].StartsWith("Amount"))
                {
                    int indexOf = m_Params[i].IndexOf('=');

                    if (indexOf >= 0)
                    {
                        // Must supress stackable warnings

                        bool wasStackable = item.Stackable;

                        item.Stackable = true;
                        item.Amount = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        item.Stackable = wasStackable;
                    }
                }
            }

            return item;
        }

        private Queue m_DeleteQueue = new Queue();

        private void FindItems(int itemID, List<Item> list, int x, int y, int z, Map map)
        {
            if (Utility.World.InaccessibleMapLoc(new Point3D(x, y, z), map) == true)
            {   // RunUO 2.6 can access these locations, we cannot.
                //  we need to upgrade our Map.cs and likely Sector.cs

                // too spammy - will handle later
                //Utility.ConsoleOut(string.Format("Incompatible Map Location: {0}: ({1}). ", new Point3D(x, y, z), map), ConsoleColor.Red);
            }

            IPooledEnumerable eable;
            eable = map.GetItemsInRange(new Point3D(x, y, z), 0);
            foreach (Item item in eable)
                if (item == null || item.Deleted || item.ItemID != itemID)
                    continue;
                else
                    list.Add(item);
            eable.Free();
        }

        private bool FindItemAndDeleteOld(int x, int y, int z, Map map, Item srcItem, List<Item> deleteList, bool listOnly = false)
        {
            if (Utility.World.InaccessibleMapLoc(new Point3D(x, y, z), map) == true)
            {   // RunUO 2.6 can access these locations, we cannot.
                //  we need to upgrade our Map.cs and likely Sector.cs

                // spammy - will address later
                //Utility.ConsoleOut(string.Format("Incompatible Map Location: {0}: ({1}). ", new Point3D(x, y, z), map), ConsoleColor.Red);
            }

            int itemID = srcItem.ItemID;
            bool res = false;

            IPooledEnumerable eable;

            if (srcItem is BaseDoor)
            {
                eable = map.GetItemsInRange(new Point3D(x, y, z), 1);

                foreach (Item item in eable)
                {
                    if (!(item is BaseDoor))
                        continue;

                    BaseDoor bd = (BaseDoor)item;
                    Point3D p;
                    int bdItemID;

                    if (bd.Open)
                    {
                        p = new Point3D(bd.X - bd.Offset.X, bd.Y - bd.Offset.Y, bd.Z - bd.Offset.Z);
                        bdItemID = bd.ClosedID;
                    }
                    else
                    {
                        p = bd.Location;
                        bdItemID = bd.ItemID;
                    }

                    if (p.X != x || p.Y != y)
                        continue;

                    if (item.Z == z && bdItemID == itemID)
                        res = true;
                    else if (Math.Abs(item.Z - z) < 8)
                        m_DeleteQueue.Enqueue(item);
                }
            }
            else if ((TileData.ItemTable[itemID & 0x3FFF].Flags & TileFlag.LightSource) != 0)
            {
                eable = map.GetItemsInRange(new Point3D(x, y, z), 0);

                LightType lt = srcItem.Light;
                string srcName = srcItem.ItemData.Name;

                foreach (Item item in eable)
                {
                    if (item.Z == z)
                    {
                        if (item.ItemID == itemID)
                        {
                            if (item.Light != lt)
                                m_DeleteQueue.Enqueue(item);
                            else
                                res = true;
                        }
                        else if ((item.ItemData.Flags & TileFlag.LightSource) != 0 && item.ItemData.Name == srcName)
                        {
                            m_DeleteQueue.Enqueue(item);
                        }
                    }
                }
            }
            else if (srcItem is Teleporter || srcItem is FillableContainer || srcItem is BaseBook)
            {
                eable = map.GetItemsInRange(new Point3D(x, y, z), 0);

                Type type = srcItem.GetType();

                foreach (Item item in eable)
                {
                    if (item.Z == z && item.ItemID == itemID)
                    {
                        if (item.GetType() != type)
                            m_DeleteQueue.Enqueue(item);
                        else
                            res = true;
                    }
                }
            }
            else
            {
                eable = map.GetItemsInRange(new Point3D(x, y, z), 0);

                foreach (Item item in eable)
                {
                    if (item.Z == z && item.ItemID == itemID)
                    {
                        eable.Free();
                        return true;
                    }
                }
            }

            eable.Free();


            while (m_DeleteQueue.Count > 0)
            {
                foreach (Item record in m_DeleteQueue)
                    deleteList.Add(record);
                if (listOnly == false)
                    ((Item)m_DeleteQueue.Dequeue()).Delete();
            }

            return res;
        }

        public int Generate(Decorate.DecoMode mode, Map[] maps, bool listOnly = false, Dictionary<Item, Tuple<Point3D, Map, bool>> ChangeLog = null, Type[] types = null, Type[] exclude = null, List<Rectangle2D> boundingRect = null)
        {
            int count = 0;
            Item template = null;
            LogHelper log = null;
            bool m_ListOnly = listOnly;

            if (ChangeLog == null)
                ChangeLog = new Dictionary<Item, Tuple<Point3D, Map, bool>>();

            if (mode != Decorate.DecoMode.skip)
                log = new LogHelper(mode == Decorate.DecoMode.delete ? "deco_delete.txt" : "deco_add.txt", false, true);

            for (int i = 0; i < m_Entries.Count; ++i)
            {
                DecorationEntry entry = (DecorationEntry)m_Entries[i];
                Point3D loc = entry.Location;
                string extra = entry.Extra;

                for (int j = 0; j < maps.Length; ++j)
                {
                    // some maps are not loaded - like tokuno
                    if (maps[j] == null)
                        continue;

                    if (Utility.World.InaccessibleMapLoc(loc, maps[j]) == true)
                    {   // RunUO 2.6 can access these locations, we cannot.
                        //  we need to upgrade our Map.cs and likely Sector.cs

                        // too spammy - will handle later
                        //Utility.ConsoleOut(string.Format("Incompatible Map Location: {0}: ({1}). ", loc, maps[j]), ConsoleColor.Red);
                    }

                    if (template == null)
                        template = Construct();

                    // we're filtering on 'type' (to keep)
                    if (template != null && types != null)
                    {
                        foreach (Type type in types)
                            if (type.IsAssignableFrom(template.GetType()))
                                goto KeepIt;
                        template.Delete();
                        template = null;
                    }
                KeepIt:

                    // we're filtering on 'type' (to remove)
                    if (template != null && exclude != null)
                    {
                        foreach (Type type in exclude)
                            if (template != null)
                                if (type.IsAssignableFrom(template.GetType()))
                                {
                                    template.Delete();
                                    template = null;
                                }
                    }

                    // we're filtering on boundingRect
                    if (template != null && boundingRect != null)
                        foreach (Rectangle2D rect in boundingRect)
                            if (!rect.Contains(loc))
                            {
                                template.Delete();
                                template = null;
                                break;
                            }

                    if (template == null)
                        continue;

                    if (mode == Decorate.DecoMode.delete)
                    {
                        List<Item> list = new List<Item>();
                        FindItems(template.ItemID, list, loc.X, loc.Y, loc.Z, maps[j]);
                        foreach (Item temp in list)
                        {
                            // record what is being deleted
                            ChangeLog.Add(temp, new Tuple<Point3D, Map, bool>(temp.Location, maps[j], mode == Decorate.DecoMode.add));
                            string sx = string.Format("Deleting a {0}: ID {3} at {1} in {2} ({4})", temp, temp.Location, maps[j], temp.ItemID, temp.GetRawOldName());
                            log.Log(sx);
                            if (m_ListOnly == false)
                                temp.Delete();
                        }
                    }
                    else if (mode == Decorate.DecoMode.skip)
                    {   // do nothing

                        if (template != null)
                            if (m_ListOnly == false)
                                template.Delete();
                        template = null;
                    }
                    else if (mode == Decorate.DecoMode.add)
                    {
                        List<Item> deleteList = new List<Item>();
                        bool found = FindItemAndDeleteOld(loc.X, loc.Y, loc.Z, maps[j], template, deleteList, listOnly);
                        if (found)
                        {   // we found what we were looking for at this location

                            // record what is being added (or found in this case)
                            if (ChangeLog.ContainsKey(template))
                            {
                                foreach (var tt in ChangeLog)
                                    if (tt.Key == template)
                                        ;
                            }

                            else
                                ChangeLog.Add(template, new Tuple<Point3D, Map, bool>(loc, maps[j], mode == Decorate.DecoMode.add));

                            if (m_ListOnly == true)
                            {
                                template.Delete();
                                template = null;
                            }
                        }
                        else
                        {
                            // record what is being added
                            if (ChangeLog.ContainsKey(template))
                                ;
                            else
                                ChangeLog.Add(template, new Tuple<Point3D, Map, bool>(loc, maps[j], mode == Decorate.DecoMode.add));

                            if (m_ListOnly == true)
                            {
                                template.Delete();
                                template = null;
                            }
                            else
                            {
                                string sx = string.Format("Adding: {0}: ID {3} at {1} in {2} ({4})", template, loc, maps[j], template.ItemID, template.GetRawOldName());
                                log.Log(sx);
                                template.MoveToWorld(loc, maps[j]);
                            }

                            ++count;

                            if (template is BaseDoor)
                            {
                                IPooledEnumerable eable = maps[j].GetItemsInRange(loc, 1);

                                Type itemType = template.GetType();

                                foreach (Item link in eable)
                                {
                                    if (link != template && link.Z == template.Z && link.GetType() == itemType)
                                    {
                                        ((BaseDoor)template).Link = (BaseDoor)link;
                                        ((BaseDoor)link).Link = (BaseDoor)template;
                                        break;
                                    }
                                }

                                eable.Free();
                            }
                            else if (template is MarkContainer)
                            {
                                try { ((MarkContainer)template).Target = Point3D.Parse(extra); }
                                catch { }
                            }

                            template = null;
                        }
                    }
                }
            }

            if (template != null)
                if (m_ListOnly == false)    // Items are created for mode delete too. we delete it here
                    template.Delete();

            if (log != null)
                log.Finish();

            return count;
        }

        public static ArrayList ReadAll(string path)
        {
            using (StreamReader ip = new StreamReader(path))
            {
                ArrayList list = new ArrayList();

                for (DecorationList v = Read(ip); v != null; v = Read(ip))
                    list.Add(v);

                return list;
            }
        }

        private static string[] m_EmptyParams = new string[0];

        public static DecorationList Read(StreamReader ip)
        {
            string line;

            while ((line = ip.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length > 0 && !line.StartsWith("#"))
                    break;
            }

            if (string.IsNullOrEmpty(line))
                return null;

            DecorationList list = new DecorationList();

            int indexOf = line.IndexOf(' ');

            list.m_Type = ScriptCompiler.FindTypeByName(line.Substring(0, indexOf++), true);

            if (list.m_Type == null)
            {
                // no need to throw an exception here since we know certain tokuno and other objects won't be found.
                // just report the error and continue
                LogHelper log = new LogHelper("deco_error.txt", false, true);
                log.Log(string.Format("Type not found for header: '{0}'", line));
                log.Finish();
                return Read(ip);
                //throw new ArgumentException(string.Format("Type not found for header: '{0}'", line));
            }

            line = line.Substring(indexOf);
            indexOf = line.IndexOf('(');
            if (indexOf >= 0)
            {
                list.m_ItemID = Utility.ToInt32(line.Substring(0, indexOf - 1));

                string parms = line.Substring(++indexOf);

                if (line.EndsWith(")"))
                    parms = parms.Substring(0, parms.Length - 1);

                list.m_Params = parms.Split(';');

                for (int i = 0; i < list.m_Params.Length; ++i)
                    list.m_Params[i] = list.m_Params[i].Trim();
            }
            else
            {
                list.m_ItemID = Utility.ToInt32(line);
                list.m_Params = m_EmptyParams;
            }

            list.m_Entries = new ArrayList();

            while ((line = ip.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                    break;

                if (line.StartsWith("#"))
                    continue;

                list.m_Entries.Add(new DecorationEntry(line));
            }

            return list;
        }
    }

    public class DecorationEntry
    {
        private Point3D m_Location;
        private string m_Extra;

        public Point3D Location { get { return m_Location; } }
        public string Extra { get { return m_Extra; } }

        public DecorationEntry(string line)
        {
            string x, y, z;

            Pop(out x, ref line);
            Pop(out y, ref line);
            Pop(out z, ref line);

            m_Location = new Point3D(Utility.ToInt32(x), Utility.ToInt32(y), Utility.ToInt32(z));
            m_Extra = line;
        }

        public void Pop(out string v, ref string line)
        {
            int space = line.IndexOf(' ');

            if (space >= 0)
            {
                v = line.Substring(0, space++);
                line = line.Substring(space);
            }
            else
            {
                v = line;
                line = "";
            }
        }
    }
}