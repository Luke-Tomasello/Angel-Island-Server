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

/* Scripts/Engines/RareFactory/RareFactory.cs
 * ChangeLog:
 *  6/13/07, Adam
 *      Add an exception hack that returns a boobie prizeinto AcquireRare() until I can get to the 
 *      bottom of some error:
 *    System.NullReferenceException: Object reference not set to an instance of an object. 
 *    at Server.Engines.RareFactory.DupeItem(Item src) 
 *    at Server.Engines.RareFactory.GenerateDODObject(DODInstance dodi) 
 *  6/12/07, Adam
 *      Add lots of sanity checking around ViewingDOD and ViewingDODGroup.
 *      The system uses static indexes to address the gump pages. This system crashes when the indexes
 *      no longer reflect the actual number of elements in the arrays.
 *      I've added exception handling to try and track down how this happens.
 *  6/8/07, Adam
 *      Make group name passed to AcquireRare insensitive to case
 *  6/7/07, Adam
 *      Rewrite I/O to correct a logic error that was corrupting the database.
 *          You MUST pass prefixstring=true if you are writing strings with BinaryFileWriter
 *  6/1/07, Adam
 *      Hide attributes (vanq, accurate, etc) on named items
 *  5/30/07, Adam
 *      - Added a post dupe patch() function:
 *      we cannot have the parent/location artificially set to some container without having called object.AddItem() 
 *      because all counts, weights, etc will be messed up, and this is exactly what happens when you dupe all fields in an item. 
 *      We must therefore 'patch' the Location and Parent post dupe so as to avoid this problem. I.e., 
 *      dest.Parent = null;
 *      dest.Location = new Point3D(0, 0, 0);
 *      - Check RareFactory.InUse in FactorySave() because saving during certain updates (text prompts?) throws exceptions and 
 *          corrupts the Save folder
 *  5/29/07, Adam
 *      - Add exception logging
 *      - minor cleanup to the way the random rare is selected
 *	17/Mar/2007, weaver
 *		- Added public interface stuff for the new RFNewGroupGump() 
 *		- Engine startup message tweak
 *		- New overload for AcquireRare. Can now pass rarity!
 *		- Removed debugging book.
 *	13/Mar/2007, weaver
 *		- Moved object construction step to DupeItem() function so 
 *		easily re-usable
 *		- Modified rare addition so that template object is no 
 *		longer required for rare reproduction and is instead
 *		stored away
 *		- Added log of rare addition
 *	12/Mar/2007, weaver
 *		Added shallow object copy.
 *	12/Mar/2007, weaver
 *		Initial creation.
 * 
 */


using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using System;
using System.Collections;
using System.IO;
using System.Reflection;


namespace Server.Engines
{

    // RareFactory
    //
    // Uses the DODInstance class to instance object definitions
    // and handles requests to load objects, generating them
    // via the instance info it accesses according to the type 
    // passed in the request
    //

    public static class RareFactory
    {

        // Public configuration handling stuff 
        // ------------------------------------------------------

        public static bool InUse;

        public static int ViewingDODIndex;
        public static int ViewingDODGroupIndex;
        public static int ViewingDODGroupPage;


        public static DODInstance ViewingDOD
        {
            get
            {   // adam: redesign the following to be failsafe
                if (m_DODInst.Count == 0 || m_DODGroup.Count == 0)
                    return null;

                if (ViewingDODGroupIndex > m_DODGroup.Count - 1)
                    ViewingDODGroupIndex = m_DODGroup.Count - 1;

                DODGroup dg = m_DODGroup[ViewingDODGroupIndex] as DODGroup;
                if (dg == null)
                    return null;

                if (ViewingDODIndex > dg.DODInst.Count - 1)
                    ViewingDODIndex = dg.DODInst.Count - 1;

                if ((dg.DODInst[ViewingDODIndex] as DODInstance).RareTemplate == null)
                {
                    try { throw new ApplicationException("ViewingDOD: dodinst.RareTemplate == null"); }
                    catch (Exception ex) { LogHelper.LogException(ex); }
                }

                if (dg.DODInst[ViewingDODIndex] is DODInstance)
                    return dg.DODInst[ViewingDODIndex] as DODInstance;
                else
                    return null;
            }
        }

        public static DODGroup ViewingDODGroup
        {
            get
            {
                if (m_DODGroup.Count == 0)
                    return null;

                if (ViewingDODGroupIndex > m_DODGroup.Count - 1)
                    ViewingDODGroupIndex = m_DODGroup.Count - 1;

                if (m_DODGroup[ViewingDODGroupIndex] is DODGroup)
                    return m_DODGroup[ViewingDODGroupIndex] as DODGroup;
                else
                    return null;
            }
        }

        public static ArrayList DODGroup
        {
            get
            {
                return (m_DODGroup);
            }
        }

        public static ArrayList DODInst
        {
            get
            {
                return (m_DODInst);
            }
        }

        public static void ReloadViews(Mobile from)
        {
            from.CloseGump(typeof(RFViewGump));
            from.CloseGump(typeof(RFControlGump));
            from.CloseGump(typeof(RFGroupGump));

            try
            {
                from.SendGump(new RFViewGump());
                from.SendGump(new RFControlGump());
                from.SendGump(new RFGroupGump(ViewingDODGroupPage));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        // ------------------------------------------------------

        // Holds all DOD instances
        private static ArrayList m_DODInst;

        // Holds all DOD groups
        private static ArrayList m_DODGroup;

        private static bool m_Available;

        public static void OnLoad()
        {
            Console.WriteLine("RareFactory Loading...");

            // Read in our DODs and last saved instance counts
            short result = FactoryLoad();
            if (result != 1)
            {
                string emsg = "RareFactory load failed!! ";

                switch (result)
                {
                    case -1:
                        emsg += "Check saves/RareFactory.dat exists and is accessible.";
                        break;
                    case -2:
                        emsg += "DOD load failed. saves/RareFactory.dat potentially corrupt.";
                        break;
                }

                Console.WriteLine(emsg);
            }
            else
            {
                m_Available = true;
            }
        }

        public static void OnSave(WorldSaveEventArgs e)
        {
            if (!m_Available)
            {
                Console.WriteLine("RareFactory unavailable for saving!");
                return;
            }

            Console.WriteLine("RareFactory Saving..");

            // Save the instance counts for each
            FactorySave();

        }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        private static short FactoryLoad()
        {
            m_DODInst = new ArrayList();
            m_DODGroup = new ArrayList();

            string filePath = Path.Combine("Saves/AngelIsland", "RareFactory.dat");
            if (!File.Exists(filePath))
            {
                Console.WriteLine("FactoryLoad() : RareFactory.dat does not exist, assuming new factory");
                return 1;                   // Assume new factory
            }

            using (FileStream sr = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (!sr.CanRead)
                {
                    Console.WriteLine("FactoryLoad() : {0} is not readable but exists!!", filePath);
                    return 0;
                }

                // This is the stream reader we will read our binary DOD data with
                BinaryFileReader bfr = new BinaryFileReader(new BinaryReader(sr));

                bfr.Seek(0, SeekOrigin.Begin);

                try
                {
                    // Just in case we change the save format sometime!
                    short version = ((GenericReader)bfr).ReadShort();

                    int numtoload = ((GenericReader)bfr).ReadShort();
                    Console.WriteLine("FactoryLoad() : Loading {0} Dynamic Object Definition{1}...", numtoload, (numtoload != 1 ? "s" : ""));

                    // Instance the DODs with the bfr pointer (the object constructor handles in file input)
                    for (int i = 0; i < numtoload; i++)
                        m_DODInst.Add(new DODInstance(bfr));

                    numtoload = ((GenericReader)bfr).ReadShort();

                    Console.WriteLine("FactoryLoad() : Loading {0} DOD Group{1}...", numtoload, (numtoload != 1 ? "s" : ""));

                    for (int i = 0; i < numtoload; i++)
                    {
                        DODGroup dg = new DODGroup(bfr);

                        // Save this group list
                        m_DODGroup.Add(dg);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("FactoryLoad() : Caught exception trying to load DODs : {0}", e);
                    LogHelper.LogException(e);
                }

                bfr.Close();

            } // 'using' closes sr

            Console.WriteLine("FactoryLoad() : Complete.");

            return 1;
        }

        private static short FactorySave()
        {

            if (RareFactory.InUse)
            {
                Console.WriteLine("FactorySave() : Rare Factory busy, cannot save");
                return 1;
            }

            // Save DOD information

            Console.WriteLine("FactorySave() : Saving DOD instance definitions");

            if (!Directory.Exists("Saves/AngelIsland"))
                Directory.CreateDirectory("Saves/AngelIsland");

            string filePath = Path.Combine("Saves/AngelIsland", "RareFactory.dat");

            BinaryFileWriter bfw = new BinaryFileWriter(filePath, true);

            // Version
            ((GenericWriter)bfw).Write((short)0);

            // First write out how many we're going to have to save (and hence load in again)
            ((GenericWriter)bfw).Write((short)m_DODInst.Count);

            // Loop through DOD instances and have them write their definitions out to this
            // stream

            for (int i = 0; i < m_DODInst.Count; i++)
            {
                ((DODInstance)m_DODInst[i]).Save(bfw);
            }

            Console.WriteLine("FactorySave() : Saved {0} instance definitions", m_DODInst.Count);


            // Write out our group definitions
            Console.WriteLine("FactorySave() : Saving DOD group definitions");

            // Number of groups
            ((GenericWriter)bfw).Write((short)m_DODGroup.Count);

            for (int i = 0; i < m_DODGroup.Count; i++)
            {
                if (m_DODGroup[i] is DODGroup)
                {
                    ((DODGroup)m_DODGroup[i]).Save(bfw);
                }
            }

            Console.WriteLine("FactorySave() : Saved {0} group definitions", m_DODGroup.Count);

            ((GenericWriter)bfw).Close();

            return 1;
        }

        private static Item GenerateDODObject(DODInstance dodi)
        {

            // Console.WriteLine("GenerateDODObject() : Generating object instance from {0}, rare name {1}", dodi.RareTemplate, dodi.Name);
            Item newitem = RareFactory.DupeItem(dodi.RareTemplate);

            // Make sure it's movable
            newitem.Movable = true;

            // Dynamically fill rare data
            if (dodi.CurIndex == 0)
                dodi.CurIndex = dodi.StartIndex;
            else
                dodi.CurIndex++;

            dodi.DynamicFill(newitem);

            return newitem;
        }

        public static Item AcquireRare(short iRarity)
        {
            return (AcquireRare(iRarity, ""));
        }

        public static Item AcquireRare(short iRarity, string sGroupName)
        {
            Item item = _AcquireRare(iRarity, sGroupName);
            LogHelper lh = new LogHelper("RareAcquired.log", false, true);
            try
            {   // log the acquired rare
                lh.Log(LogType.Item, item, string.Format("type: ({0}).", item.GetType().ToString()));
                lh.Finish();
                return item;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                lh.Finish();
            }

            return (new Rocks());
        }

        private static Item _AcquireRare(short iRarity, string sGroupName)
        {
            try
            {
                // Make sure the RareFactory is available. Send a message back instead of a rare if it is not.

                ArrayList matches = new ArrayList();

                if (m_Available)
                {

                    // Search the DODInstance definitions
                    // for ones matching this type of object

                    for (int i = 0; i < m_DODGroup.Count; i++)
                    {
                        DODGroup dg = (DODGroup)m_DODGroup[i];

                        // Validate group name

                        if (sGroupName != "")
                        {
                            bool match = false;

                            // Parse the group name. If it contains commas, we need to split by comma
                            if (sGroupName.Contains(","))
                            {
                                // Multiple group names
                                string[] gnames = sGroupName.Split(',');
                                for (int gi = 0; gi < gnames.Length; gi++)
                                    if (gnames[gi].ToLower() == dg.Name.ToLower())
                                        match = true;
                            }
                            else
                            {
                                // Single group name
                                if (sGroupName.ToLower() == dg.Name.ToLower())
                                    match = true;
                            }

                            if (match == false)
                                continue;           // next group
                        }


                        for (int di = 0; di < dg.DODInst.Count; di++)
                        {
                            DODInstance dinst = (DODInstance)dg.DODInst[di];
                            if (dinst.Valid)
                            {
                                if (dg.Rarity == iRarity)
                                {
                                    if (dinst.RareTemplate == null)
                                    {
                                        try { throw new ApplicationException("AcquireRare: dinst.RareTemplate == null"); }
                                        catch (Exception ex) { LogHelper.LogException(ex); }
                                    }
                                    matches.Add(dinst);
                                }
                            }
                        }
                    }
                }

                // the boobie prize
                if (!m_Available || matches.Count == 0)
                    return (new Rocks());

                // Choose one of these types randomly			
                // and generate the object (also dynamically
                // fills the object's strings)

                return GenerateDODObject((DODInstance)matches[Utility.Random(matches.Count)]);
            }
            catch (Exception ex)
            {   // failsafe
                LogHelper.LogException(ex);
                return (new Rocks());
            }

        }

        public static void AddRare(Item src)
        {   //Adam: UI version
            AddRare(((DODGroup)m_DODGroup[ViewingDODGroupIndex]), src);
            ViewingDODIndex = ((DODGroup)m_DODGroup[ViewingDODGroupIndex]).DODInst.Count - 1;
        }

        public static DODInstance AddRare(DODGroup group, Item src)
        {
            // Make a copy of the rare
            Item StoredItem = DupeItem(src);

            // Log the fact that we're about to move the item into storage
            LogHelper lh = new LogHelper("RareTemplateCreation.log", false, true);
            lh.Log(LogType.Item, StoredItem);
            lh.Finish();

            // Store the copy away
            StoredItem.MoveToIntStorage();

            // Instance the DOD on this copy
            DODInstance di = new DODInstance(StoredItem);

            // Add a new DOD instance based on the item passed
            m_DODInst.Add(di);

            // Add a reference to our active group
            group.DODInst.Add(di);

            return di;
        }

        public static Item DupeItem(Item src)
        {
            Item copy = src;
            Item new_item = null;

            Type t = copy.GetType();
            ConstructorInfo[] cinfo = t.GetConstructors();
            foreach (ConstructorInfo c in cinfo)
            {
                ParameterInfo[] paramInfo = c.GetParameters();

                if (paramInfo.Length == 0)
                {
                    object[] objParams = new object[0];

                    try
                    {
                        object o = c.Invoke(objParams);

                        if (o != null && o is Item)
                        {
                            new_item = (Item)o;
                            MirrorItem(new_item, copy);
                            Patch(new_item);            // See comments on Patch();
                        }
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogException(e);
                    }
                }
            }

            return new_item;
        }

        //Adam: 
        //  Note1: We cannot have the Parent/Location artificially set to some container without having called object.AddItem() 
        //  because all counts, weights, etc will be messed up, and this is exactly what happen when you dupe all fields in an item. 
        //  We must therefore 'patch' the Location and Parent post dupe so as to avoid this problem.
        //  Note2: We want specially named weapons and armor that do not display all of their attributes, we therefore set
        //  HideAttributes = true to suppress the display
        public static void Patch(Item dest)
        {
            if (dest == null)
                return;

            // kill the phantom parent
            dest.Parent = null;
            dest.Location = new Point3D(0, 0, 0);

            // clear the temp map flag
            dest.IsIntMapStorage = false;

            // now make sure attributes are hidden on this named item
            if (dest.Name != null)
                dest.HideAttributes = true;
        }

        public static void MirrorItem(Item dest, Item src)
        {

            PropertyInfo[] props = src.GetType().GetProperties();
            PropertyInfo[] swaps = new PropertyInfo[2];

            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    if (props[i].CanRead && props[i].CanWrite)
                    {
                        // 8/5/21, Adam: Unfortunatly, Amount and TotalGold have side effects, so they have to be set in reverse order.
                        //  That could be a fix for later, but it's too complex to try to handle before launch. For now I just swap
                        //  the order of the assignment, and that solves the problem.
                        if (props[i].Name == "Amount")
                        {
                            swaps[0] = props[i];
                            continue;
                        }
                        if (props[i].Name == "TotalGold")
                        {
                            swaps[1] = props[i];
                            continue;
                        }

                        //Console.WriteLine( "Setting {0} = {1}", props[i].Name, props[i].GetValue( src, null ) );
                        props[i].SetValue(dest, props[i].GetValue(src, null), null);
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e, "RareFactory() : MirrorItem() : Caught exception : trying to mirror properties...");
                }
            }

            try
            {
                if (swaps[0] != null && swaps[1] != null)
                {
                    swaps[0].SetValue(dest, swaps[0].GetValue(src, null), null);
                    swaps[1].SetValue(dest, swaps[1].GetValue(src, null), null);
                }
            }
            catch (Exception ex)
            {
                Server.Diagnostics.LogHelper.LogException(ex);
            }
        }
    }

}