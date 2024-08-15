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

/* Scripts/Engines/Spawner/Spawner.cs
 * Changelog:
 *  4/12/2024, Adam (Spawner.ClearPath()))
 *      Renamed to Spawner.ClearPathLand() since it only works for land navigation.
 *  3/14/2024, Adam (CustomMobile())
 *      Disallow custom mobiles from being tamed.
 *  2/5/2024, Adam
 *      Spawner property TemplateGet removed (unreliable)
 *      New commands:
 *      [GetTemplateMob <target spawner>
 *      [GetTemplateItem <target spawner>
 *      Both functions bring the template to your location
 *  12/10/1023, Adam
 *      Undo Christmas event hack to spawner template model.
 *      Hack: Was leaving orphaned template mobiles/items if they are on one map vs another
 *      Fix: Add reference counting on template mobiles/items/loot packs, and carve packs. Now all of these can be shared across multiple spawners.
 *      Note: When the last linking spawner is deleted, the template is deleted, regardless of map.
 *  12/4/2023, Adam (mobiles getting stuck or spawning underwater)
 *      sometimes, the map lays water tiles over gullies/ditches in dungeons (lakes).
 *          Example (5228, 911, -45) / (5228, 911, -65) (Destard)
 *      For land-only mobiles, we avoid these completely. For water capable mobs, we exclude the land tiles under the water
 *  7/26/2023, Adam (SetSkill)
 *      Like the SetProp below, SetSkill allows you to assign skills via the spawner.
 *          Anatomy 600.66 ; Swords ; 100.2
 *          Combined with SetProp, these properties allow you to create 'template' mobiles via the spawner
 *          as opposed to the 'template mobile' model.
 *  7/11/2023, Adam (SetProp)
 *      You can now set a string of properties to set in the spawned object. For example:
 *          movable false ; hue 33
 *  6/25/2023, Adam (Dupe)
 *      Unset and restore item's Parent before and after the dupe.
 *      Duping an item with the parent set results in side effects, including in container overload.
 *  6/25/2023, Adam (Defrag())
 *      since players can places houses on top of spawners, and since spawners, when they cannot find 
 *          a good spawn location, default to the location of a spawner, you can get a scenario where 
 *          all the creatures are spawning in the players house!
 *          To solve this, in Defrag() we check to see if a house has been dropped on us. If so,
 *          we move the spawner to the houses 'ban location'
 *  5/31/2023, Adam (ChangingDungeons)
 *      Add is changing dungeons rule. I.e., don't allow Shame spawn to end up in Wind
 *  5/29/23, Yoar
 *      Added ObjectCount, IsEmpty getters
 *      Added CPAs to ObjectCount, IsEmpty, IsFull so that GMs can read these properties in-game
 *  5/14/2023, Adam (Count)
 *      Refactor how spawners use Count.
 *          The old (RunUO) style used m_Count to denote how many creatures this spawner would spawn
 *          Nerun style spawners have a count per spawner gump entry, (Nerun spawners can contain > 1 creatures on a spawner gump entry)
 *      The refactor does away with the m_Count, and replaces it with a list of counts per spawner gump entry
 *  3/27/23, Adam (RandomPointAdjust)
 *      Modify RandomPointAdjust (used in concentric spawners) to apply gravity to outer edge.
 *      Basically, we don't want a smooth distribution of objects from spawner to the edge (of the spawn radius.)
 *      We want to 'pull' some fraction of objects away from the spawner and distribute them in the outer two bands.
 *      See Also comments: RandomPointAdjust()
 *  12/7/22, Adam (CanOverrideAI)
 *      Add a new CanOverrideAI property to BaseCreature which tells at least the 'Exhibit' Spawner
 *      if this creature may have its AI overridden. By default, the Exhibit spawner
 *          sets the AI to Animal.
 *  11/18/22, Adam: (Respawn())
 *      Add new function ReCalcEnd() to be called after a respawn to recalculate the NextSpawn.
 *  9/22/22, Adam (MaximumSpawnableRect)
 *      Update usage of MaximumSpawnableRect() - was in Geometry.cs, not moved to a static function in Utility.cs 
 *  9/7/22, Adam (SpawnedMobileKilled)
 *      Add public virtual void SpawnedMobileKilled(Mobile m)
 *      Notifies the spawner of this mobile of the death.
 *      This is used by our special 'champ level' PushBackSpawners
 *  9/2/22, Adam(WalkRangeCalc)
 *      Calculates the maximum walking range for a mobile up to 128 tiles from the spawner.
 *      Useful for shopkeepers where you would like them to walk all around the shop without having to set each spawner manually
 *  8/30/22, Adam (Nerun's Distro spawner model)
 *      Major rework of our spawner to support Nerun's Distro spawner model https://github.com/nerun/runuo-nerun-distro
 *      Summary:
 *      Our spawners work as they always have unless you enable the Nerun's compatibility mode.
 *      When the spawner is operating in Nerun's compatibility mode, these are the differences:
 *      1. Something from each cell is always spawned. As opposed to AI spawners where a single object from the spawn list is selected at random.
 *      2. Support for colon delimited cell entries. This means a single spawner gump entry can look like: cat:rat:dog: etc. N of these are spawned, where N is the 'count' for that cell.
 *      3. A 'per cell' count specifier. classic RunUO spawners have a single 'count' field that applies to every spawned item. Nerun's model has a 'count' for each entry (cell)
 *  8/26/22, Adam
 *      Add _defaultSpawnMap() to automatically set the UOSpawnMap based on configuration when a spawner is first created.
 *  8/25/22, Adam (NeedsReview = 0x04)
 *      Update the flag value on this enum
 *  8/24/22, Adam (Concentric spawners)
 *      HomeRange works differently for Concentric spawners.
 *      Normally, HomeRange defines both how far the spawner can throw the creatures (items) AND the RangeHome of the creature (how far they can walk)
 *      This model leads to clumping of creatures. 
 *      Concentric spawners differ in that HomeRange defines ONLY how far the spawner can throw the creatures, but the creatures RangeHome is set
 *      to a default value calculated by the 'circle' logic. Additionally, the creatures Home is set to the place where they were spawned.
 *      This updating happens in OnAfterMobileSpawn(). The result is the creatures are scattered as per usual, but their landing spot becomes their new Home, 
 *      and their RangeHome is restricted. 
 *      The theory is that we should see far less clumping of creatures as their RangeHome will keep them near their spawn point.
 *      Note: Concentric spawners are expensive as they store an array of 360 points around the circle's perimeter.
 *          Concentric spawners work best-and are intended for-spawning large numbers of creatures over vast areas.
 *  8/24/22, Adam (ShouldShardEnable(s))
 *      Add new ShouldShardEnable(s) which is now used in a couple different places to determine what spawners should be respawned on which shards.
 *  8/23/22, Adam (core spawners)
 *      - rename CoreSpawn ==> _CoreSpawn
 *      - rename IsConcentric ==> _IsConcentric
 *      - rename OurWorldSize ==> _OurWorldSize
 *      All of this to have them listed first in the spawner, as they are important to set.
 *      - add _Shard
 *          _Shard defines the shard (Core,Siege,AngelIsland,Renaissance,Mortalis,etc.) for which this spawner applies.
 *          Core is special as it applies to all shards. It is standard OSI spawn
 *          Mortalis for example would apply to only Mortalis shards.
 *      - add ShardConfig enum: to what shard this spawner applies: Core=all shards, AngelIsland=angel island only, etc.
 *      - add _UOSpawnMap: How it was determined this was a 'core' spawner
 *      - add _NeedsReview: Questionable, needs review (should it be 'core' or not?
 *      - revamp OnSingleClick() to display name+_UOSpawnMap, then the labels (core, running, etc.)
 *      - Add SaveFlags to optimize I/O
 *  8/22/22, Adam
 *      Don't try to spawn a template mobile if the m_TemplateMobile has been deleted
 *  5/23/22, Adam (CopyLayers)
 *      Make sure the item to copy has a Parameterless Constructor
 *      i.e., StuddedGlovesOfMining don't have such a constructor
 *  12/12/21, Adam
 *      Add updating of QuickTables when a spawner is deleted.
 *	12/4/21, Yoar
 *		Added GoldDice to configure gold drops.
 *	11/25/21, Yoar
 *		Added CarvePack: Specifies special carvables for creatures generated by this spawner. Works similarly to LootPack.
 *  11/13/21, Adam (ClearPath())
 *      Add 'clear path' checking to an spawn position. 
 *      This prevents spawned creatures from being spawned in areas where thay could not return to the spawner (inaccessable locations.)
 *  11/8/21, Yoar
 *      Added calls to Mobile.OnAfterSpawn, Item.OnAfterSpawn
 *  10/17/21, Adam (EventSpawner)
 *      convert to using GameTime instead of ServerTime.
 *      Also, don't delete the EventSpawner when the event is over, just delete the creatures and disable the spawner.
 *  10/11/21, Adam
 *      Add support for spawning on a boat
 *  7/16/21, Adam
 *      Added two new variables (serialized) and one not.
 *      m_isConcentric (serialized) Tells us if we should try to distribute spawn in concentric circles aound the spawner instead of tossing all spawn out to homerange
 *      m_concentric (not serialized) The counter that shrinks the radius for spawn
 *      m_coreSpawn (serialized) spawners listed here https://uo.stratics.com/hunters/spawn/spawnmap.jpg are considered core spawners. By tagging them, we can find them.
 *	6/27/2021, Adam
 *		Rewrite all the template stuff to actually work and not be crappy-buggy.
 *	3/6/16, Adam
 *		1. Comment out the *display* of the OurWorldSize stuff... not sure why it's here and atleast makes the display ugly
 *		We will look into this later and try and track down the reasoning.
 *		2. Make our event spawners print more useful information, and allow them to take an expiration date.
 *	3/30/10. adam
 *		1. Add EventSpawner class
 *		2. Force a recalc of NextSpawn if Min or Max are changed
 *	2/23/10, Adam
 *		Limit paragons to players using dragons or daemons to farm
 *	5/12/09, Adam
 *		Check for value==null when setting the LootPack
 *	01/14/09, plasma
 *		Added OnAfterMobileSpawn( Mobile )
 *	10/28/08, plasma
 *		Virtualised Spawn()
 *	10/17/08, Adam
 *		In OnDelete make sure to delete all templates!
 *	10/16/08, Adam
 *		- Add a LootPack Item (or container item) that specifies special loot for creatures generated by this spawner. BaseCreature takes care of the actual interpretation of this value.
 *      - Make sure the current lootpack is deleted before a new one is assigned.
 *      - Make sure to remove the loot from the parent container.
 *	4/20/08, Adam
 *		Make GetSpawnPosition() a public function
 *	11/1/07, Adam
 *		When using target mode to setup a Template Item or Mobile, set/clear the SpawnerTempMob or SpawnerTempItem for the template
 *		We need to clear it for the old Template so that it will decay as per usual, and set it for the new one so that it does not 
 *		decay. 
 *		Caution: Don't use anything spawned by a spawner (still on the spawner list) as a template as the spawner will delete it
 *		during a defrag.
 *	8/28/07, Adam
 *		Add a 10% chance to make the spawned creature a Paragon.
 *		The actual decision is in BaseCreature, the spawner only creates the chance.
 *  3/21/07, Adam (removed)
 *      Add DynamicLootTypes for dynamic loot specification.
 *  3/17/07, Adam
 *      Split spawn into a Create function and a Move to world portion.
 *      This is so we can have a public Factory method to create Template Mobiles and Items.
 *  2/26/07, Adam
 *      Make sure to call m_Timer.Flush() to remove any queued ticks.
 *      This is important when setting NextSpawn and there is already a queued tick.
 *  10/19/06, Kit
 *		Made template mobs spawner property be set to spawner they are on.
 *	10/18/06, Adam
 *		- Remove notion of 'Fixed' non-decaying objects
 *		- Refresh() items still on a spawner before decaying (called by ItemDecay in Heartbeat)
 *	9/15/06, Adam
 *		Add function to create a template object only if the Type of the existing template
 *			object has changed. The prevents complex templates from being lost when someone
 *			simply hits 'OK' on the Spawner gump
 *	9/3/06, Adam
 *		Add function to get the Creature List
 *  8/16/06, Kit
 *		Made static copy of mobs copy layers and property of those layers to new destination mob.
 *  7/02/06, Kit
 *		Added, property DynamicCopy, if enabled call InitOutfit/InitBody routines of mob.
 *		Added set NameHue to -1(use notoriety) for vendors spawned with invulnerability turned off.
 *		Fixed bug with namehue being set on all spawners.
 *		Added call to InitOutfit() for normal template spawns to prevent cross dressing.
 *	6/30/06, Adam
 *		- remove evil code nulling templates on every spawn if Enable is not set
 *		- Have TemplateEnable invoke appropriate Template Creation / Destruction code
 *		- Propagate Lifespan for Mobiles and LastMove for items (manages decay/cleanup)
 *		- move template creation/management into Spawner class.
 *		- make sure template is only created on the first object specified.
 *		- make TemplateEnable readable by GMs, but writable by Seers
 *  06/27/06, Kit
 *		Added templated ability to spawners, full range of props setting for spawner entry.
 *		Set via TemplateMobile -> view props, or TemplateItem -> view props, packed out old data.
 *  06/27/06, Kit
 *		Added new property MobVendorInvunerable, for setting invulnerability on vendors.
 *	6/6/06, Adam
 *		Rename Nav --> m_NavDest to follow normal member naming conventions.
 *	1/9/06, Adam
 *		Change FreezeDecay to AccessLevel.Administrator - used for daily rares
 *	12/20/05, Adam
 *		Add many more attributes for defining dynamic creatures.
 *	12/19/05, Adam
 *		Add logging to the Name and Hue attributes.
 *	12/18/05, Adam
 *		Add the attributes Name and Hue to be applied to the spawned Object.
 *		These are Seer access attributes.
 *	12/16/05, Adam
 *		Warn if we're spawning stuff in a house.
 *		This is a problem because the item will be orphaned by the spawner
 *		which can lead to excessive item generation.
 *  12/05/05, Kit
 *		Spawner now calls Think() for creature if creature has a navdestination set.
 *  12/01/05, Kit
 *		Added Serialization for NavDestination
 *  11/29/05 Kit
 *		Added NavDestination property for setting default NavPoint.
 *  11/22/05 TK
 *		Correction MobileDirection to be a separate value from spawner's Item.Direction
 *  11/21/05 Taran Kain
 *      Added MobileDirection property, just a redirect to Item.Direction
 *	3/9/05, Adam
 *		In Defrag() we now check to see if the spawned item is in a HouseRegion.
 *		items in a HouseRegion are assumed to be 'freed' from the spawner and may therefore
 *		be removed from the spawners list of managed items.
 *		We needed this new check because putting an item in your house leaves the parent null which prevents 
 *		the 'fixed' item from decaying and a new item from being spawned. (See previous change)
 *	02/28/05, Adam
 *		1. Add new FreezeDecay property to freeze the decay on spawned items.
 *		2. When someone picks something up, parent is set to non-null. When this happens
 *			We can clear the 'Fixed' flag (item.Fixed = false).
 *		Detailed explanation: 
 *			Items are removed from the spawners list of spawned items during the Defrag() run. 
 *			This Defrag() run is roughly the frequency of the spawner. In the case of daily rares, 
 *			this respawn rate can be days long.
 *			Because spawned rares must necessarily sit on the ground for days and not decay, 
 *			they have a special 'fixed' attribute that prevents them from decaying.
 *			When a player gets a rare home, it will still be fixed until the spawner next runs and 
 *			defrags. When this happens, the 'fixed' attribute will be cleared, and the item will 
 *			decay as usual.
 *	02/28/05, erlein
 *		Now also logs addition and deletion of spawners.
 *	02/27/05, erlein
 *		Added logging of HomeRange and Count property changes.
 *		Now logs all changes to these in /logs/spawnerchange.log
 *	12/29/04, Pix
 *		Now spawned creatures contain the location of the spawner.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using Server.Engines;
using Server.Engines.Alignment;
using Server.Engines.ChampionSpawn;
using Server.Items;
using Server.Items.Triggers;
using Server.Multis;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using static Server.Mobiles.BaseCreature;
using static Server.Utility;

public enum SpawnerGraphics
{
    AllShardsRunningHue = 55,           // yellow
    AllShardsStoppedHue = 33,           // red 
    NerunRunningHue = 70,               // green
    NerunStoppedHue = 1231,             // violet
    CustomRunningHue = 1501,            // orange
    CustomStoppedHue = 1508,            // brown
}

namespace Server.Mobiles
{
    public partial class Spawner : Item
    {
        #region Morph Mode
        private MorphMode m_MorphMode = MorphMode.Default;
        [CommandProperty(AccessLevel.Seer)]
        public MorphMode MorphMode { get { return m_MorphMode; } set { m_MorphMode = value; } }
        #endregion Morph Mode
        #region Sterling System
        private ushort m_SterlingMin = 0;
        private ushort m_SterlingMax = 0;
        [CommandProperty(AccessLevel.Seer)]
        public ushort SterlingMin { get { return m_SterlingMin; } set { m_SterlingMin = value; } }
        [CommandProperty(AccessLevel.Seer)]
        public ushort SterlingMax { get { return m_SterlingMax; } set { m_SterlingMax = value; SetFlag(SpawnerAttribs.DropsSterling, m_SterlingMax > 0); } }
        #endregion Sterling System
        #region Spawner Tools
        public int IndexOf(string text)
        {
            for (int ix = 0; ix < this.ObjectNamesRaw.Count; ix++)
                if ((this.ObjectNamesRaw[ix] as string).ToLower().Contains(text.ToLower()))
                    return ix;
            return -1;
        }
        public int RemoveAtIndex(int index)
        {
            if (index < 0 || index >= this.ObjectNamesRaw.Count)
                return -1;

            if (ObjectNamesRaw[index] is string text && text.Contains(':'))
            {   // multi spawner
                if (text.Contains(text + ":", StringComparison.OrdinalIgnoreCase))
                    ObjectNamesRaw[index] = text.Replace(text + ":", "", StringComparison.OrdinalIgnoreCase);
                else
                    ObjectNamesRaw[index] = text.Replace(text, "", StringComparison.OrdinalIgnoreCase);
            }
            else
                this.ObjectNamesRaw.RemoveAt(index);

            return index;
        }
        public static List<int> Registry = new()
        {   // these are the item ids spawned by spawners
            0xE41,
            0x0,
            0x26AC,
            0xD17,
            0xE3F,
            0xE3C,
            0x9A9,
            0xE73,
            0x18E4,
            0x5B72,
            0x119A,
            0x11AC,
            0xE76,
            0x171A,
            0x171B,
            0x171C,
            0x1713,
            0x1717,
            0x1718,
            0x1714,
            0x1716,
            0x170F,
            0x170B,
            0x1F0B,
            0xE89,
            0x13B4,
            0xF52,
            0xEC4,
            0x13B2,
            0xF3F,
            0x13FF,
            0x1405,
            0xE87,
            0xF0A,
            0xE21,
            0xF0C,
            0xF07,
            0x1727,
            0xF08,
            0xF09,
            0xF0B,
            0xF0D,
            0x13C7,
            0x13CD,
            0x1DB9,
            0x13CB,
            0x13CC,
            0x13C6,
            0x1C02,
            0x13DB,
            0x13DC,
            0x1C0C,
            0x13D5,
            0x13D6,
            0x13DA,
            0xF62,
            0x13FD,
            0x1BFB,
            0xF61,
            0x1401,
            0xF50,
            0x9D0,
            0x1C06,
            0xF43,
            0x1412,
            0x1411,
            0x1413,
            0x1410,
            0x1414,
            0x1415,
            0x1C04,
            0x13B9,
            0x13EC,
            0x13EB,
            0x13F0,
            0x13EE,
            0x13BF,
            0x13BE,
            0xF49,
            0x1B74,
            0x1B7A,
            0x1B76,
            0x143E,
            0x1F09,
            0x1515,
            0x9E9,
            0x9C9,
            0x232A,
            0x9BB,
            0x1BDD,
            0x98C,
            0x2328,
            0x41F6,
            0xA97,
            0xA98,
            0x11A8,
            0x9EC,
            0x993,
            0xF8B,
            0xA26,
            0xE48,
            0x9B5,
            0x1363,
            0xE26,
            0x1914,     // frypan sold by cooks. correctly ignored
            0x9E2,      // this one though has a different graphic id, but looks the same, so we will explicitly ignore it here
            0xFAE,
            0xFF2,
            0xC6A,
            0x1125,
            0x1915,
            0xE77,
            0x14E0,
            0xE83,
            0xC6F,
            0xC63,
            0xC76,
            0xCB6,
            0xC56,
            0x1A9A,
            0xC5E,
            0xC5F,
            0xC62,
            0xC61,
            0xC60,
            0xC54,
            0xC55,
            0x1A9B,
            0xC51,
            0xC58,
            0xC57,
            0x1F4A,
            0x1F38,
            0x1F32,
            0x1F3E,
            0x1F56,
            0xF7A,
            0xF7B,
            0xF8D,
            0xF8C,
            0xF84,
            0xF85,
            0xF88,
            0xF86,
            0x1F03,
            0x1F42,
            0x1F5F,
            0x1F5C,
            0x1F37,
            0x1F57,
            0x1F49,
            0x1F31,
            0x1F69,
            0x1F40,
            0x1F2D,
            0x1EBA,
            0xE14,
            0xE13,
            0x1367,
            0xE81,
            0xF47,
            0x1F50,

        };

        public bool Spawns(int itemId)
        {
            return Registry.Contains(itemId);
        }

        public bool Spawns(string text)
        {
            foreach (string name in this.ObjectNamesRaw)
                if (name.ToLower().Contains(text.ToLower()))
                    return true;
            return false;
        }

        public bool Spawns(Type type, List<Type> exclude = null)
        {   // note: can't be used for RaresSpawners
            foreach (string name in this.ObjectNamesRaw)
            {
                string[] chunks = name.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string typeName in chunks)
                {
                    Type typeFound = ScriptCompiler.FindTypeByName(typeName);
                    if (typeFound != null)
                    {
                        if (exclude != null && exclude.Contains(typeFound))
                            continue;
                        if (type.IsAssignableFrom(typeFound))
                            return true;
                    }
                }
            }

            return false;
        }
        #endregion Spawner Tools
        #region Variables
        public static int EdgeGravity = 1;  // see comments in RandomPointAdjust()
        private int m_Team;
        private int m_HomeRange;
        private int m_WalkRange = -1;                      // -1 indicates we will only use m_HomeRange
        private List<int> m_SlotCount;
        private TimeSpan m_MinDelay;
        private TimeSpan m_MaxDelay;
        private ArrayList m_ObjectNames = new ArrayList(0);
        private ArrayList m_Objects;
        private DateTime m_End;
        private InternalTimer m_Timer;
        private bool m_Group;
        private bool m_DynamicCopy;
        private bool m_CoreSpawn = false;   // 'core' spawn is that spawn shown in https://uo.stratics.com/hunters/spawn/spawnmap.jpg We will use this as a guide to populate our world
        private bool m_Invulnerable = false;
        private WayPoint m_WayPoint;
        private Direction m_MobDirection;
        private string m_NavDest;
        private Mobile m_TemplateMobile;
        private Item m_TemplateItem;
        private Item m_LootPack;
        private Item m_CarvePack;
        private string m_CarveMessage;
        private bool m_CarveOverride;
        private DiceEntry m_GoldDice;
        private UInt32 m_PatchID = 0;
        private int m_GraphicID = 0;        // the spawner will us this alternative GraphicID (ItemID) if supplied.
        private string m_SetProp = null;    // set these properties on the spawned object
        private string m_SetSkill = null;   // set these skills on the spawned object
        private bool m_TemplateInternalize = true;
        private Item m_TriggerLinkPlayers;
        private Item m_TriggerLinkCreature;
        private int m_GoodiesRadius;
        private int m_GoodiesTotalMin;
        private int m_GoodiesTotalMax;
        #endregion
        #region OnMovement
        #region Publish 4 Siege 
        private static bool PUBLISH_4_SIEGE = (Core.RuleSets.SiegeStyleRules()) && PublishInfo.Publish >= 4;
        private Memory m_badGuys = PUBLISH_4_SIEGE ? new Memory() : null;
        public Memory BadGuys { get { return m_badGuys; } }
        #endregion Publish 4 Siege 
        public override bool HandlesOnMovement { get { return true; } }
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            #region Publish 4 Siege 
            /*
             * Publish 4
             * Shopkeeper Changes
             * If a shopkeeper is killed, a new shopkeeper will appear as soon as another player (other than the one that killed it) approaches.
             * http://www.uoguide.com/Publish_4
             */
            if (m != null && m.Player && PUBLISH_4_SIEGE)
            {   // we only have a memory (count > 0) if the spawned NPC died
                if (m_badGuys.Count > 0 && m_badGuys.Recall(m) == false)
                {   // if I need to spawn something and I don't remember this guy (m), spawn it now
                    this.NextSpawn = TimeSpan.Zero;     // now
                    m_badGuys = new Memory();           // we will respawn, no need for this old stale memory
                    this.SendSystemMessage("[Staff Message] Instant spawn override: BaseVendor.OnDeath");
                }
            }
            #endregion Publish 4 Siege 

            // auto showing of 'special spawners (they are color coded) to staff
            if (m != null && m.Player && m.AccessLevel >= AccessLevel.GameMaster)
                if (Core.Debug && (Core.RuleSets.StandardShardRules()))
                {
                    if (this.Visible == false)
                    {
                        this.Visible = true;
                        // auto hide this spawner again in 1 minute after movement stops
                        Timer.DelayCall(TimeSpan.FromSeconds(60), new TimerStateCallback(OnMovementTick), new object[] { this });
                    }
                }

            base.OnMovement(m, oldLocation);
        }
        private void OnMovementTick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is Spawner spawner && spawner.Deleted == false)
            {
                spawner.Visible = false;
            }
        }
        #endregion OnMovement

        #region Templates
        public enum TemplateMode : byte
        {
            CopyProperties,
            Dupe
        }
        //private TemplateMode m_TemplateMode = TemplateMode.CopyProperties;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public virtual TemplateMode TemplateStyle
        {
            get { return GetFlag(SpawnerAttribs.TemplateMode) ? TemplateMode.Dupe : TemplateMode.CopyProperties; }
            set
            {
                TemplateMode old_style = TemplateStyle;
                if (value == TemplateMode.Dupe)
                    SetFlag(SpawnerAttribs.TemplateMode, true);
                else
                    SetFlag(SpawnerAttribs.TemplateMode, false);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public virtual bool TemplateEnabled
        {
            get
            {
                return m_TemplateMobile != null || m_TemplateItem != null;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public virtual bool DynamicCopy
        {
            get
            {
                return m_DynamicCopy;
            }
            set
            {
                m_DynamicCopy = value;
                InvalidateProperties();
            }
        }

        //hold object for templated spawner use.
        //[CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Seer)]
        public virtual Mobile TemplateMobile
        {
            get { return m_TemplateMobile; }
            set
            {
                if (m_TemplateMobile != value)
                {
                    if (value != null && value.Spawner != null)
                    {
                        try
                        {
                            value.Spawner.Objects.Remove(value);
                            value.Spawner = null;
                        }
                        catch
                        {
                            SendSystemMessage("Failed to unlink Mobile from its spawner.");
                            return;
                        }
                    }

                    if (m_TemplateMobile != null)
                    {
                        m_TemplateMobile.SpawnerTempRefCount--;
                        if (CanDeleteTemplate(m_TemplateMobile))
                            m_TemplateMobile.Delete();
                    }

                    m_TemplateMobile = value;

                    if (m_TemplateMobile != null)
                    {
                        m_TemplateMobile.SpawnerTempMob = true;

                        if (m_TemplateInternalize)
                            m_TemplateMobile.MoveToIntStorage(true);

                        m_TemplateMobile.SpawnerTempRefCount++;
                    }
                }
            }
        }

        private bool CustomMobile()
        {
            bool template_mobile = m_TemplateMobile != null;
            bool custom_skills = !string.IsNullOrEmpty(m_SetSkill);
            bool custom_props = !string.IsNullOrEmpty(m_SetProp);
            bool loot_pack = m_LootPack != null;
            bool artifact_pack = m_ArtifactPack != null;
            bool carve_pack = m_CarvePack != null;
            bool loot_string = m_LootString != null;
            if (template_mobile || custom_skills || custom_props || loot_pack || artifact_pack || carve_pack || loot_string || BlockDamage)
                return true;
            return false;
        }
        public enum BoolFlags : byte { Default, True, False }
        private BoolFlags m_Tamable = BoolFlags.Default;
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual BoolFlags Tamable
        {
            get
            {
                if (!CustomMobile() || TamableOverride)
                    return m_Tamable;
                else
                    return BoolFlags.False;
            }
            set
            {
                if (CustomMobile() && !TamableOverride)
                    this.SendSystemMessage("You cannot set the tamability of a custom creature");
                else
                    m_Tamable = value;
            }

        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public bool TamableOverride
        {
            get { return GetFlag(SpawnerAttribs.TamableOverride); }
            set { SetFlag(SpawnerAttribs.TamableOverride, value); }
        }
        //hold object for templated spawner use.
        //[CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Seer)]
        public virtual Item TemplateItem
        {
            get { return m_TemplateItem; }
            set
            {
                if (m_TemplateItem != value)
                {
                    if (value != null && value.Spawner != null)
                    {
                        try
                        {
                            value.Spawner.Objects.Remove(value);
                            value.Spawner = null;
                        }
                        catch
                        {
                            SendSystemMessage("Failed to unlink Item from its spawner.");
                            return;
                        }
                    }

                    if (m_TemplateItem != null)
                    {
                        m_TemplateItem.SpawnerTempRefCount--;
                        if (CanDeleteTemplate(m_TemplateItem))
                            m_TemplateItem.Delete();
                    }

                    m_TemplateItem = value;

                    if (m_TemplateItem != null)
                    {
                        m_TemplateItem.SpawnerTempItem = true;

                        if (m_TemplateInternalize)
                            m_TemplateItem.MoveToIntStorage(true);

                        m_TemplateItem.SpawnerTempRefCount++;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public virtual bool TemplateInternalize
        {   // abused by staff resulting in a mess of crap sprinkled all around the surface map, and incompatible with the freeze dry system.
            get { return m_TemplateInternalize; }
            set { m_TemplateInternalize = value; }
        }

        private int m_TemplateMobileDefinition;
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public virtual int TemplateMobileDefinition
        {
            get { return m_TemplateMobileDefinition; }
            set { m_TemplateMobileDefinition = value; }
        }
        #endregion

        private EquipmentDisposition m_EquipmentDisposition = EquipmentDisposition.None;
        [CommandProperty(AccessLevel.Seer)]
        public EquipmentDisposition MutateEquipment
        {
            get { return m_EquipmentDisposition; }
            set { m_EquipmentDisposition = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item TriggerLinkPlayers
        {
            get { return m_TriggerLinkPlayers; }
            set { m_TriggerLinkPlayers = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Item TriggerLinkCreatures
        {
            get { return m_TriggerLinkCreature; }
            set { m_TriggerLinkCreature = value; }
        }
        #region Goodies Props
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int GoodiesRadius
        {
            get { return m_GoodiesRadius; }
            set { m_GoodiesRadius = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int GoodiesTotalMin
        {
            get { return m_GoodiesTotalMin; }
            set
            {
                m_GoodiesTotalMin = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int GoodiesTotalMax
        {
            get { return m_GoodiesTotalMax; }
            set
            {
                m_GoodiesTotalMax = value;
            }
        }
        #endregion Goodies Props

        [CommandProperty(AccessLevel.Seer)]
        public virtual bool StaticCorpse
        {
            get { return GetFlag(SpawnerAttribs.StaticCorpse); }
            set
            {
                if (GetFlag(SpawnerAttribs.StaticCorpse) != value)
                {
                    SetFlag(SpawnerAttribs.StaticCorpse, value);
                }
            }
        }

        #region GraphicID
        [CommandProperty(AccessLevel.Seer)]
        public virtual int GraphicID { get { return m_GraphicID; } set { m_GraphicID = value; } }
        #endregion GraphicID

        [CommandProperty(AccessLevel.Seer)]
        public virtual string SetProp
        {
            get { return m_SetProp; }
            set
            {
                m_SetProp = value;
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public virtual string SetSkill
        {
            get { return m_SetSkill; }
            set
            {
                m_SetSkill = value;
            }
        }

        #region LootString
        //hold object for templated spawner use.
        private string m_LootString = null;
        [CommandProperty(AccessLevel.Seer)]
        public string LootString
        {
            get { return m_LootString; }
            set
            {
                if (m_LootString != value)
                {
                    if (value != null)
                    {
                        string reason = string.Empty;
                        try
                        {   // make sure it's valid at this level
                            Item result = SpawnEngine.Build<Item>(value, ref reason);
                            if (result == null)
                            {
                                SendSystemMessage(reason);
                                return;
                            }
                            else
                                result.Delete();
                        }
                        catch (Exception ex)
                        {   // don't think we can ever get here as SpawnEngine.Build handles all exceptions
                            SendSystemMessage(ex.Message);
                            return;
                        }
                    }

                    m_LootString = value;
                }
            }
        }
        #endregion LootString

        #region LootStringProps
        //hold object for templated spawner use.
        private string m_LootStringProps = null;
        [CommandProperty(AccessLevel.Seer)]
        public string LootStringProps
        {
            get { return m_LootStringProps; }
            set { m_LootStringProps = value; }
        }
        #endregion LootStringProps

        #region LootPack
        //hold object for templated spawner use.
        //[CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Item LootPack
        {
            get { return m_LootPack; }
            set
            {
                if (m_LootPack != value)
                {
                    if (m_LootPack != null)
                    {
                        m_LootPack.SpawnerTempRefCount--;
                        if (CanDeleteTemplate(m_LootPack))
                            m_LootPack.Delete();
                    }

                    m_LootPack = value;

                    if (m_LootPack != null)
                    {
                        if (m_TemplateInternalize)
                            m_LootPack.MoveToIntStorage(true);

                        m_LootPack.SpawnerTempRefCount++;
                    }
                }
            }
        }
        #endregion

        #region CarvePack
        //carve object for templated spawner use.
        //[CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Item CarvePack
        {
            get { return m_CarvePack; }
            set
            {
                if (m_CarvePack != value)
                {
                    if (m_CarvePack != null)
                    {
                        m_CarvePack.SpawnerTempRefCount--;
                        if (CanDeleteTemplate(m_CarvePack))
                            m_CarvePack.Delete();
                    }

                    m_CarvePack = value;

                    if (m_CarvePack != null)
                    {
                        if (m_TemplateInternalize)
                            m_CarvePack.MoveToIntStorage(true);

                        m_CarvePack.SpawnerTempRefCount++;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string CarveMessage
        {
            get { return m_CarveMessage; }
            set
            {
                m_CarveMessage = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CarveOverride
        {
            get { return m_CarveOverride; }
            set { m_CarveOverride = value; }
        }
        #endregion

        #region Artifacts
        private Item m_ArtifactPack;
        private int m_ArtifactCount;

        //[CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Item ArtifactPack
        {
            get { return m_ArtifactPack; }
            set
            {
                if (m_ArtifactPack != value)
                {
                    if (m_ArtifactPack != null)
                    {
                        m_ArtifactPack.SpawnerTempRefCount--;
                        if (CanDeleteTemplate(m_ArtifactPack))
                            m_ArtifactPack.Delete();
                    }

                    m_ArtifactPack = value;

                    if (m_ArtifactPack != null)
                    {
                        if (m_TemplateInternalize)
                            m_ArtifactPack.MoveToIntStorage(true);

                        m_ArtifactPack.SpawnerTempRefCount++;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int ArtifactCount
        {
            get { return m_ArtifactCount; }
            set
            {
                m_ArtifactCount = value;
            }
        }

        private void DistributeArtifacts(Mobile killed)
        {
            if (m_ArtifactPack == null || m_ArtifactCount <= 0)
                return;

            Dictionary<Mobile, int> dict = new Dictionary<Mobile, int>();
            int totalDamage = 0;

            foreach (DamageEntry de in killed.DamageEntries)
            {
                if (de.Damager == null || de.HasExpired)
                    continue;

                Mobile damager = de.Damager;
                int damage = de.DamageGiven;

                Mobile master = damager.GetDamageMaster(killed);

                if (master != null)
                    damager = master;

                if (damage > 0 && damager.Player)
                {
                    if (dict.ContainsKey(damager))
                        dict[damager] += damage;
                    else
                        dict[damager] = damage;

                    totalDamage += damage;
                }
            }

            if (dict.Count == 0 || totalDamage <= 0)
                return;

            List<Mobile> eligible = new List<Mobile>();
            int curTotal = 0;
            List<Mobile> winners = new List<Mobile>();

            for (int i = 0; i < m_ArtifactCount; i++)
            {
                if (eligible.Count == 0)
                {
                    eligible.AddRange(dict.Keys);
                    curTotal = totalDamage;
                }

                int rnd = Utility.Random(curTotal);

                Mobile winner = null;

                for (int j = 0; winner == null && j < eligible.Count; j++)
                {
                    Mobile m = eligible[j];

                    if (rnd < dict[m])
                        winner = m;
                    else
                        rnd -= dict[m];
                }

                if (winner != null)
                {
                    eligible.Remove(winner);
                    curTotal -= dict[winner];
                    winners.Add(winner);
                }
            }

            foreach (Mobile winner in winners)
            {
                Item[] artifacts = BaseCreature.GenerateTemplateLoot(m_ArtifactPack);

                if (artifacts.Length != 0)
                {
                    // select a random artifact and delete the others
                    int index = Utility.Random(artifacts.Length);

                    for (int i = 0; i < artifacts.Length; i++)
                    {
                        if (i != index)
                            artifacts[i].Delete();
                    }

                    Item artifact = artifacts[index];

                    winner.AddToBackpack(artifact);

                    winner.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special reward has been bestowed on you.

                    LogHelper logger = new LogHelper("SpawnerArtifacts.log", false, true);
                    logger.Log(LogType.Item, this, string.Format("{0} received an artifact: {1} ({2}).", winner, artifact, artifact.GetType().Name));
                    logger.Finish();
                }
            }
        }
        #endregion

        #region Spawner Attribs
        /// <summary>
        /// We have a tool ClassicRespawn which replaces AI Classic spawners with those defined by Nerun's Distro
        /// Nerun's Distro seems to be a well thought out Classic OSI spawn. 
        /// We set the flags below on spawners that have already been processed by the system. Otherwise we would be reprocessing the same spawners over and over again
        ///     should we run the tool a second time.
        /// </summary>
        [Flags]
        public enum SpawnerAttribs : UInt32
        {
            None = 0x0,                                 // no special attributes
            Dummy = 0x01,                               // dummy creature, doesn't move, doesn't take damage, no fight mode
            GuardIgnore = 0x02,                         // Creatures on this spawner are ignored by guards (Town Invasion)
            Running = 0x08,                             // to do - move spawner bool value to bitfield
            m_Group = 0x10,                             // to do - move spawner bool value to bitfield
            DynamicCopy = 0x20,                         // to do - move spawner bool value to bitfield
            IsConcentric = 0x40,                        // to do - move spawner bool value to bitfield
            CoreSpawn = 0x80,                           // to do - move spawner bool value to bitfield
            #region needs to be kept in sync with SpawnerModeAttribs below
            ModeAI = 0x100,                             // ignored if we run the ClassicRespawn again - new AI's Distro spawner
            ModeNeruns = 0x200,                         // ignored if we run the ClassicRespawn again - new Nerun's Distro spawner
            ModeLegacy = 0x400,                         // pre distro model. i.e., original AI spawners
            ModeMulti = 0x800,                          // multi spawners supported (non Nerun) (crow:magpie:raven)
            #endregion needs to be kept in sync with SpawnerModeAttribs below
            Replaced = 0x1000,                          // Set in ClassicRespawn: ignored if we run the ClassicRespawn again - has been replaced by Nerun's Distro
            Kept = 0x2000,                              // Set in ClassicRespawn: ignored if we run the ClassicRespawn again - has been kept in spite of Nerun's Distro
            Debug = 0x4000,                             // turns on spawner debugging messages
            StaticCorpse = 0x8000,                      // kills the mobile and sets the corpse to static - uses the spawners next spawn as the decay time
            External = 0x10000,                         // we are being 'triggered' externally. When this flag is set, we don't cleanup the mobile if this is an event spawner
            TemplateMode = 0x20000,                     // if zero, we are using the default 'CopyProperties' for templates, otherwise it's 'Dupe'
            TamableOverride = 0x40000,                  // We don't normally allow modified creatures to be tamable. Seers must explicitly enable this
            Robot = 0x80000,                            // robot creature - robot AI, not tame, but only wonders and stays (control orders.)
            BlockDamage = 0x100000,                     // creature does not take damage
            Team = 0x200000,                            // team has changed
            MissionCritical = 0x400000,                 // BlockDamage + MissionCritical == Hulk Smash (usually used with a way point.)
            DropsSterling = 0x800000,                  // creature drops sterling
        }
        [Flags]
        public enum SpawnerModeAttribs : UInt32         // needs to be kept in sync with SpawnerAttribs above
        {
            None = 0x0,                                 // no special attributes
            ModeAI = 0x100,                             // ignored if we run the ClassicRespawn again - new AI's Distro spawner
            ModeNeruns = 0x200,                         // ignored if we run the ClassicRespawn again - new Nerun's Distro spawner
            ModeLegacy = 0x400,                         // pre distro model. i.e., original AI spawners
            ModeMulti = 0x800,                          // multi spawners supported (non Nerun) (crow:magpie:raven)
        }
        private SpawnerAttribs m_SpawnerAttribs = SpawnerAttribs.None;          // serialized
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual SpawnerModeAttribs Distro
        {
            get { return (SpawnerModeAttribs)(m_SpawnerAttribs & (SpawnerAttribs.ModeNeruns | SpawnerAttribs.ModeAI | SpawnerAttribs.ModeLegacy | SpawnerAttribs.ModeMulti)); }

            set
            {
                SpawnerModeAttribs oldSpawnerModeAttribs = Distro;
                if (value == SpawnerModeAttribs.ModeNeruns)
                {
                    SetFlag(SpawnerAttribs.ModeNeruns, true);
                    SetFlag(SpawnerAttribs.ModeAI, false);
                    SetFlag(SpawnerAttribs.ModeLegacy, false);
                    SetFlag(SpawnerAttribs.ModeMulti, false);
                }
                else if (value == SpawnerModeAttribs.ModeAI)
                {
                    SetFlag(SpawnerAttribs.ModeAI, true);
                    SetFlag(SpawnerAttribs.ModeNeruns, false);
                    SetFlag(SpawnerAttribs.ModeLegacy, false);
                    SetFlag(SpawnerAttribs.ModeMulti, false);
                }
                else if (value == SpawnerModeAttribs.ModeLegacy)
                {
                    SetFlag(SpawnerAttribs.ModeLegacy, true);
                    SetFlag(SpawnerAttribs.ModeNeruns, false);
                    SetFlag(SpawnerAttribs.ModeAI, false);
                    SetFlag(SpawnerAttribs.ModeMulti, false);
                }
                else if (value == SpawnerModeAttribs.ModeMulti)
                {
                    SetFlag(SpawnerAttribs.ModeMulti, true);
                    SetFlag(SpawnerAttribs.ModeNeruns, false);
                    SetFlag(SpawnerAttribs.ModeAI, false);
                    SetFlag(SpawnerAttribs.ModeLegacy, false);
                }
                else
                {
                    SetFlag(SpawnerAttribs.ModeAI, false);
                    SetFlag(SpawnerAttribs.ModeNeruns, false);
                    SetFlag(SpawnerAttribs.ModeLegacy, false);
                    SetFlag(SpawnerAttribs.ModeMulti, false);
                    Utility.ConsoleWriteLine(string.Format("Error: Invalid spawner designation for spawner {0} in {1}", this, Utility.FileInfo()), ConsoleColor.Red);
                }

                UpdateDisplay();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool ModeAI
        {
            get { return GetFlag(SpawnerAttribs.ModeAI); }
            set
            {
                bool oldModeAI = ModeAI;
                SpawnerModeAttribs oldSpawnerModeAttribs = Distro;
                if (value == true)
                    Distro = SpawnerModeAttribs.ModeAI;
                else
                    Distro &= ~SpawnerModeAttribs.ModeAI;

                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool ModeNeruns
        {
            get { return GetFlag(SpawnerAttribs.ModeNeruns); }
            set
            {
                bool oldModeNeruns = ModeNeruns;
                SpawnerModeAttribs oldSpawnerModeAttribs = Distro;
                if (value == true)
                    Distro = SpawnerModeAttribs.ModeNeruns;
                else
                    Distro &= ~SpawnerModeAttribs.ModeNeruns;

                InvalidateProperties();
            }
        }
        // don't make this a command property until we allow the GM to set the EntryCounts
        //  currently it's only set programmatically where we fill in the EntryCounts
        public virtual bool ModeMulti
        {
            get { return GetFlag(SpawnerAttribs.ModeMulti); }
            set
            {
                bool oldModeMulti = ModeMulti;
                SpawnerModeAttribs oldSpawnerModeAttribs = Distro;
                if (value == true)
                    Distro = SpawnerModeAttribs.ModeMulti;
                else
                    Distro &= ~SpawnerModeAttribs.ModeMulti;

                InvalidateProperties();
            }
        }
        public bool Replaced
        {
            get { return GetFlag(SpawnerAttribs.Replaced); }
            set { SetFlag(SpawnerAttribs.Replaced, value); }
        }
        public bool Kept
        {
            get { return GetFlag(SpawnerAttribs.Kept); }
            set { SetFlag(SpawnerAttribs.Kept, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool GuardIgnore
        {
            get { return GetFlag(SpawnerAttribs.GuardIgnore); }
            set
            {
                SetFlag(SpawnerAttribs.GuardIgnore, value);
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Dummy
        {
            get { return GetFlag(SpawnerAttribs.Dummy); }
            set
            {
                SetFlag(SpawnerAttribs.Dummy, value);
                this.SendSystemMessage(string.Format("training dummy {0}", value.ToString()));
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public new virtual bool Debug
        {
            get { return GetFlag(SpawnerAttribs.Debug); }
            set
            {
                SetFlag(SpawnerAttribs.Debug, value);
                this.SendSystemMessage(string.Format("base.Debug set to {0}", value.ToString()));
            }
        }
        public bool GetFlag(SpawnerModeAttribs flag)
        {
            return (((SpawnerModeAttribs)m_SpawnerAttribs & flag) != 0);
        }
        public void SetFlag(SpawnerModeAttribs flag, bool value)
        {
            if (value)
                m_SpawnerAttribs |= (SpawnerAttribs)flag;
            else
                m_SpawnerAttribs &= ~((SpawnerAttribs)flag);
        }
        public bool GetFlag(SpawnerAttribs flag)
        {
            return ((m_SpawnerAttribs & flag) != 0);
        }
        public void SetFlag(SpawnerAttribs flag, bool value)
        {
            if (value)
                m_SpawnerAttribs |= flag;
            else
                m_SpawnerAttribs &= ~flag;
        }
        #endregion Spawner Attribs
        #region Shard Config
        [Flags]
        public enum ShardConfig : UInt32
        {
            None = 0x0,                                                         // error
            Siege = 0x01,                                                       // Siege is Core spawn (no special spawn)
            AngelIsland = 0x02,                                                 // AngelIsland + special spawn
            Renaissance = 0x04,                                                 // Renaissance
            Mortalis = 0x08,                                                    // Mortalis is Siege spawn (no special spawn) but maybe some 'Mortalis only' spawns
            AllShards = 0x10 | Siege | AngelIsland | Mortalis | Renaissance,    // non-core, but good for all shards
            Core = 0x100,                                                       // standard UO spawn https://uo.stratics.com/hunters/spawn/spawnmap.jpg good for all shards
        }
        private ShardConfig m_Shard = ShardConfig.AngelIsland;
        private string m_Source = string.Empty;
        private bool m_NeedsReview = false;
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool NeedsReview
        {
            get { return m_NeedsReview; }
            set
            {
                m_NeedsReview = value;
            }
        }
        private void _defaultSpawnMap()
        {
            if (m_CoreSpawn == true)
            {
                m_Source = "Pub15";                                              // denotes this as pub 15 standard spawn. 
            }
            else
            {
                if (m_Shard.HasFlag(ShardConfig.AngelIsland))
                    Source = "AISpecial";                                      // denotes this as an Angel Island Special - non standard. 
                else if (m_Shard.HasFlag(ShardConfig.Siege))
                    Source = "SPSpecial";                                      // denotes this as a Mortalis Special - non standard. )
                else if (m_Shard.HasFlag(ShardConfig.Mortalis))
                    Source = "MOSpecial";                                      // denotes this as a Mortalis Special - non standard. )
                else if (m_Shard.HasFlag(ShardConfig.Renaissance))
                    Source = "RenSpecial";                                     // denotes this as a Renaissance Special - non standard. )
                else
                    Source = "Unknown";
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string Source
        {
            get { return m_Source; }
            set
            {
                m_Source = value;
            }
        }
        /// <summary>
        /// What shard this spawner is targeting.
        /// AngelIsland is the default, and is the primary spawn config for all shards.
        /// Something like "Mortalis" will only spawn if the shard config is Mortalis
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual ShardConfig Shard
        {
            get { return m_Shard; }
            set
            {
                m_Shard = value;
                UpdateDisplay();
            }
        }
        /// <summary>
        /// Concentric controls how spawn are scattered. We use Concentric when spawning over large areas and we don't want clumping of 
        /// spawn too close to the spawner, or two far away. Concentric attempts to spread the spawn more evenly (could probably use a better algo.)
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Concentric
        {
            get { return (m_SpawnerFlags & SpawnFlags.Concentric) != 0; }
            set
            {
                // adam: Log the change to IsConcentric
                if (Concentric != value)
                {
                    SpawnFlags oldSpawnFlags = m_SpawnerFlags;

                    if (value)
                        m_SpawnerFlags |= SpawnFlags.Concentric;
                    else
                        m_SpawnerFlags &= ~SpawnFlags.Concentric;

                    InvalidateProperties();
                }
            }
        }
        /// <summary>
        /// All the 'core' spawners were derived from the official UO spawn map.
        /// https://uo.stratics.com/hunters/spawn/spawnmap.jpg
        /// if it is not 'core', it is an AngelIsland special spawn
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CoreSpawn
        {
            get { return m_CoreSpawn; }
            set
            {
                m_CoreSpawn = value;
            }
        }
        public void SetFlag(ShardConfig flag, bool value)
        {
            if (value)
                m_Shard |= flag;
            else
                m_Shard &= ~flag;

            UpdateDisplay();
        }
        public bool GetFlag(ShardConfig flag)
        {
            return ((m_Shard & flag) != 0);
        }
        #endregion Shard Config
        public static bool ShouldShardEnable(Spawner s)
        {
            return ShouldShardEnable(s.Shard);
        }
        public static bool ShouldShardEnable(ShardConfig shard)
        {
            if (shard == Spawner.ShardConfig.Core) return true;      // yup, it's exactly a 'core' spawner 
            if (shard == Spawner.ShardConfig.AllShards) return true; // Maybe not 'core', but should be available on all shards (Kin, Prison, etc.)
            else if (Core.RuleSets.SiegeRules() && shard.HasFlag(Spawner.ShardConfig.Siege)) return true;
            else if (Core.RuleSets.AngelIslandRules() && shard.HasFlag(Spawner.ShardConfig.AngelIsland)) return true;
            else if (Core.RuleSets.MortalisRules() && shard.HasFlag(Spawner.ShardConfig.Mortalis)) return true;
            else if (Core.RuleSets.RenaissanceRules() && shard.HasFlag(Spawner.ShardConfig.Renaissance)) return true;
            return false;
        }
        public virtual UInt32 PatchID
        {
            get { return m_PatchID; }
            set { m_PatchID = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual DiceEntry GoldDice
        {
            get { return m_GoldDice; }
            set
            {
                m_GoldDice = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Invulnerable
        {
            get { return m_Invulnerable; }

            set
            {
                m_Invulnerable = value;
            }
        }
        private bool m_Exhibit = false;
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Exhibit
        {
            get { return m_Exhibit; }
            set
            {
                m_Exhibit = value;
            }
        }
        private UInt32 m_QuestCode;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public virtual UInt32 QuestCodeValue
        {
            get { return m_QuestCode; }
            set
            {
                if (Engines.QuestCodes.QuestCodes.CheckQuestCode((ushort)value) || value == 0)
                {
                    m_QuestCode = value;
                }
                else
                {
                    this.SendSystemMessage("That quest code has not been allocated.");
                    this.SendSystemMessage("Use [QuestCodeAlloc to allocate a new quest code.");
                    throw new ApplicationException("That quest code has not been allocated.");
                }

            }
        }
        private double m_QuestCodeChance;
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double QuestCodeChance
        {
            get { return m_QuestCodeChance; }
            set
            {
                m_QuestCodeChance = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int ObjectCount
        {
            get { Defrag(); return (m_Objects == null ? 0 : m_Objects.Count); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsEmpty
        {
            get { return (ObjectCount == 0); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull
        {
            get { return (ObjectCount >= GetSlotCount(index: 0)); }
        }
        public List<Type> ObjectTypes
        {
            get
            {
                List<Type> list = new List<Type>();
                List<string> names = ObjectNamesParsed;
                if (names.Count > 0)
                {
                    foreach (string name in names)
                    {
                        Type type = SpawnerType.GetType(name);
                        if (type != null && list.Contains(type) == false)
                            list.Add(type);
                    }
                }
                return list;
            }
        }
        public List<string> ObjectNamesParsed
        {
            get
            {
                List<string> list = new List<string>();
                foreach (string name in m_ObjectNames)
                {
                    if (name.Contains(':'))
                    {
                        string[] tokens = name.Split(':');
                        foreach (string token in tokens)
                            list.Add(token);
                    }
                    else
                        list.Add(name);
                }

                return list;
            }
        }
        private List<string> GetObjectNames(string name)
        {
            List<string> list = new List<string>();
            if (name.Contains(':'))
            {
                string[] tokens = name.Split(':');
                foreach (string token in tokens)
                    list.Add(token);
            }
            else
                list.Add(name);

            return list;
        }
        public bool Contains(Type item)
        {
            List<Type> objects = ObjectTypes;
            if (objects == null || objects.Count == 0)
                return false;
            if (objects.Contains(item))
                return true;

            return false;
        }
        public ReadOnlyCollection<object> ObjectNames
        {
            get
            {
                List<object> list = new List<object>(ObjectNamesParsed);
                return list.AsReadOnly();
            }
        }
        public ArrayList ObjectNamesRaw
        {
            get { return m_ObjectNames; }
            set
            {
                m_ObjectNames = value;
                if (m_ObjectNames.Count < 1)
                    Stop();

                InvalidateProperties();
            }
        }
        public ArrayList Objects
        {
            get { return m_Objects; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Count
        {   // Nerun's spawners stores count(s) here
            get { return m_SlotCount == null ? 0 : m_SlotCount.Sum(); }
            set
            {
                if (GetSlotCount(index: 0) != value)
                {
                    if ((GetFlag(SpawnerAttribs.ModeNeruns) || GetFlag(SpawnerAttribs.ModeMulti)))
                    {
                        this.SendSystemMessage("Nerun's spawners cannot have their 'count' set in this way");
                        return;
                    }

                    SetSlotCount(index: 0, value: value);

                    InvalidateProperties();
                }
            }
        }
        public void WipeSlots()
        {
            m_SlotCount = new(0);
        }
        private void SlotCountEnsureCapacity(int index)
        {
            if (m_SlotCount == null)
                m_SlotCount = new(0);

            while (m_SlotCount.Count < index + 1)
                m_SlotCount.Add(0);
        }
        public void SetSlotCount(int index, int value)
        {
            SlotCountEnsureCapacity(index);
            m_SlotCount[index] = value;
        }
        public int GetSlotCount(int index)
        {
            SlotCountEnsureCapacity(index);
            return m_SlotCount[index];
        }
        private int GetSlotTableSize()
        {
            if (m_SlotCount == null)
                return 0;

            return m_SlotCount.Count;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string Counts
        {
            get
            {
                string counts = string.Empty;
                int size = GetSlotTableSize();
                if (m_SlotCount == null)
                    m_SlotCount = new(0);
                foreach (int ix in m_SlotCount)
                    counts += ix.ToString() + ", ";
                counts = counts.TrimEnd(new char[] { ',', ' ' });
                return counts;
            }
            set
            {
                if (value != null)
                {
                    string[] tokens = value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    for (int ix = 0; ix < tokens.Length; ix++)
                        SetSlotCount(index: ix, int.Parse(tokens[ix]));
                }
                else
                {
                    WipeSlots();
                }

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual WayPoint WayPoint
        {
            get
            {
                return m_WayPoint;
            }
            set
            {
                m_WayPoint = value;
            }
        }

        public DebugFlags m_DebugMobile = DebugFlags.None;  // not serialized
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual DebugFlags DebugMobile
        {
            get { return m_DebugMobile; }
            set
            {
                m_DebugMobile = value;
                UpdateMobileDebug(value);
            }
        }
        private void UpdateMobileDebug(DebugFlags value)
        {
            foreach (object o in Objects)
                if (o is BaseCreature bc)
                    bc.DebugMode = value;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string NavDestination
        {
            get
            {
                return m_NavDest;
            }
            set
            {
                m_NavDest = value;
                UpdateMobileNav(value);
            }
        }
        private void UpdateMobileNav(string value)
        {
            foreach (object o in Objects)
                if (o is BaseCreature bc)
                    bc.NavDestination = value;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Direction MobileDirection
        {
            get
            {
                return m_MobDirection;
            }
            set
            {
                m_MobDirection = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Running
        {
            get { return base.IsRunning; }
            set
            {
                bool oldRunning = base.IsRunning;
                base.IsRunning = value;
                if (base.IsRunning)
                    Start();
                else
                    Stop();

                InvalidateProperties();
            }
        }

        /// <summary>
        /// HomeRange works differently for Concentric spawners.
        ///     Normally, HomeRange defines both how far the spawner can throw the creatures (items) AND the RangeHome of the creature (how far they can walk)
        ///     This model leads to clumping of creatures. 
        ///     Concentric spawners differ in that HomeRange defines ONLY how far the spawner can throw the creatures, but the creatures RangeHome is set
        ///     to a default value calculated by the 'circle' logic. Additionally, the creatures Home is set to the place where they were spawned.
        ///     This updating happens in OnAfterMobileSpawn(). The result is the creatures are scattered as per usual, but their landing spot becomes their new Home, 
        ///     and their RangeHome is restricted. 
        ///     The theory is that we should see far less clumping of creatures as their RangeHome will keep them near their spawn point.
        /// /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int HomeRange
        {
            get
            {
                return m_HomeRange;
            }
            set
            {
                m_HomeRange = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int WalkRange
        {
            get { return m_WalkRange; }
            set
            {
                m_WalkRange = value;
            }
        }
        private static bool m_WalkRangeCalc = false;
        /// <summary>
        /// Calculates the maximum walking range for a mobile up to 128 tiles from the spawner.
        /// Useful for shopkeepers where you would like them to walk all around the shop without having to set each spawner manually
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool WalkRangeCalc
        {
            get { return m_WalkRangeCalc; }
            set
            {
                int oldWalkRange = m_WalkRange;
                if (value == true)
                {
                    int max = 128;                                                                  // give up at 128 tile radius from the spawner.
                    if (this.Map != null || this.Map != Map.Internal)
                    {
                        if (this.HomeRange > 0)
                        {
                            // can't set when in your backpack, another container, or an invalid map
                            Rectangle2D rect = new Rectangle2D(this.Location.X, this.Location.Y, 1, 1); // spawner sits atop a 1 tile rect
                            // blow up the rect to the max walkable area
                            rect = new Utility.SpawnableRect(rect).MaximumSpawnableRect(this.Map, 128, this.Map.GetAverageZ(rect.X, rect.Y));
                            m_WalkRangeCalc = value;                                                    // show this rect was auto sized
                            m_WalkRange = Math.Max(rect.Width, rect.Height);                            // set the walk range
                            if (rect.Width >= max || rect.Height >= max)                                // caution the user of the clipping
                            {
                                m_WalkRange = max;
                                this.SendSystemMessage("Maximum range reached.");
                            }
                        }
                        else
                        {
                            this.SendSystemMessage("Cannot calc walk range when HomeRange is zero.");
                        }
                    }
                    else
                    {
                        this.SendSystemMessage("This spawner must be placed on the ground before WalkRangeDynamic can be calculated.");
                        m_WalkRangeCalc = false;
                    }
                }
                else
                    m_WalkRangeCalc = false;
            }
        }

        class Circle
        {
            public Vector2 Origin { get; set; }
            public float Radius { get; set; }
            public List<Vector2> Points { get; set; }
            public Circle(float radius, Vector2 origin)
            {
                Radius = radius;
                Origin = origin;
                Points = new List<Vector2>();

                for (int i = 0; i < 360; i++)
                {
                    float x = origin.X + radius * (float)Math.Cos(i * Math.PI / 180.0);
                    float y = origin.Y + radius * (float)Math.Sin(i * Math.PI / 180.0);
                    Points.Add(new Vector2(x, y));
                }
            }

            public int GetConcentricHomeRange()
            {   // 20% of the radius
                return Math.Max(4, (int)Utility.GetDistanceToSqrt(
                    new Point3D((int)Origin.X, (int)Origin.Y, 0),
                    new Point3D((int)Points[0].X, (int)Points[0].Y, 0)) / 4);
            }
            private Point3D Vector2ToPoint3D(Vector2 point)
            {
                return new Point3D((int)point.X, (int)point.Y, 0);
            }
            /// <summary>
            /// Select a RandomPoint within our spawn circle
            /// Results using Gravity 0, and Gravity 1
            /// Gravity set to 0
            /// 614 Objects nearby, 32.32%%
            /// 637 Objects medium distance, 33.53%
            /// 649 Objects far distance, 34.16%
            /// 1900 total objects spawned
            ///  ---
            /// Gravity set to 1
            /// 217 Objects nearby, 11.42%
            /// 646 Objects medium distance, 34.00%
            /// 1037 Objects far distance, 54.58%
            /// 1900 total objects spawned
            /// See Also: nuke SetSpawnerEdgeGravity and TestSpawnerClumping to understand the tuning
            /// </summary>
            /// <param name="spawn_far"></param>
            /// <param name="far_wiggle"></param>
            /// <returns>RandomPoint within our spawn circle</returns>
            public Point3D RandomPointAdjust(bool spawn_far, int far_wiggle)
            {
                // pick a random point around the circumference of our circle
                Vector2 edge = Points[Utility.Random(Points.Count)];
                if (!spawn_far)
                {
                    // Performs a linear interpolation between two vectors based on the given weighting (pull).
                    float pull = (float)Utility.RandomDouble();
                    // Apply gravity. See nuke SetSpawnerEdgeGravity and TestSpawnerClumping to understand the tuning (results in above comment)
                    for (int ix = 0; ix < EdgeGravity; ix++)
                    {   // trying to toss the mobiles farther away from the spawner
                        float temp = (float)Utility.RandomDouble();
                        if (temp > pull)
                            pull = temp;
                    }
                    // pull: A value between 0 and 1 that indicates the weight of the edge. (Edge Gravity)
                    Vector2 newPos = Vector2.Lerp(Origin, edge, pull);
                    return Vector2ToPoint3D(newPos);
                }
                else // (used rarely: Pirate Champ spawn, where the spawner is on the island, but we want all creatures out in the water.)
                {   // here all we are really doing is +/- wiggle
                    // Note: here we could have a gravity of say 100, but it's more expensive and doesn't
                    //  really buy us anything over just going directly to the edge.
                    int x = ((int)edge.X) + ((Utility.RandomList(new int[] { 0, 1 }) * 2 - 1) * far_wiggle);
                    int y = ((int)edge.Y) + ((Utility.RandomList(new int[] { 0, 1 }) * 2 - 1) * far_wiggle);
                    // if we're spawning far, just return the edge point +/- wiggle
                    return new Point3D(x, y, 0);
                }
            }
            // debug
            public void Draw()
            {
                for (int i = 0; i < Points.Count; i++)
                {
                    RecallRune rr = new RecallRune();
                    rr.MoveToWorld(new Point3D((int)Points[i].X, (int)Points[i].Y, 0), Map.Felucca);
                }
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Team
        {
            get { return m_Team; }
            set
            {
                m_Team = value;
                ApplyToCurrent(SpawnerAttribs.Team, m_Team);
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Robot
        {
            get { return GetFlag(SpawnerAttribs.Robot); }
            set
            {
                SetFlag(SpawnerAttribs.Robot, value);
                ApplyToCurrent(SpawnerAttribs.Robot, value);
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool BlockDamage
        {
            get { return GetFlag(SpawnerAttribs.BlockDamage); }
            set
            {
                SetFlag(SpawnerAttribs.BlockDamage, value);
                ApplyToCurrent(SpawnerAttribs.BlockDamage, value);
                InvalidateProperties();
                if (value)
                    SendSystemMessage("The creature will not take damage nor paralyze. It will also dispel fields.");
                else
                    SendSystemMessage("The creature will take normal damage.");
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool MissionCritical
        {
            get { return GetFlag(SpawnerAttribs.MissionCritical); }
            set
            {
                SetFlag(SpawnerAttribs.MissionCritical, value);
                ApplyToCurrent(SpawnerAttribs.MissionCritical, value);
                InvalidateProperties();
                SendSystemMessage("Setting MissionCritical + BlockDamage makes the creature virtually unstoppable.");
                SendSystemMessage("Hulk Smash!");
            }
        }

        private void ApplyToCurrent(SpawnerAttribs attrib, object value)
        {
            if (Objects != null)
                foreach (object o in Objects)
                    if (o is Mobile m)
                        switch (attrib)
                        {
                            case SpawnerAttribs.MissionCritical:
                                {
                                    m.SetMobileBool(Mobile.MobileBoolTable.MissionCritical, (bool)value);
                                    break;
                                }
                            case SpawnerAttribs.BlockDamage:
                                {
                                    m.SetMobileBool(Mobile.MobileBoolTable.BlockDamage, (bool)value);
                                    break;
                                }
                            case SpawnerAttribs.Robot:
                                {
                                    if (m is BaseCreature bc)
                                        bc.Robot = (bool)value;
                                    break;
                                }
                            case SpawnerAttribs.Team:
                                {
                                    if (m is BaseCreature bc)
                                        bc.Team = (int)value;
                                    break;
                                }
                        }

        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan MinDelay
        {
            get { return m_MinDelay; }
            set
            {
                if (value > m_MaxDelay)
                    m_MaxDelay = value * 2;

                m_MinDelay = value;
                ResetTimer();
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan MaxDelay
        {
            get { return m_MaxDelay; }
            set
            {
                m_MaxDelay = value;
                ResetTimer();
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSpawn
        {
            get
            {
                if (Running)
                    return m_End - DateTime.UtcNow < TimeSpan.Zero ? TimeSpan.Zero : m_End - DateTime.UtcNow;
                else
                    return TimeSpan.FromSeconds(0);
            }
            set
            {
                DoTimer(value);
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Group
        {
            get { return m_Group; }
            set
            {
                m_Group = value;
                InvalidateProperties();
            }
        }
        public override Item Dupe(int amount)
        {
            Spawner new_spawner = new Spawner();
            // won't copy the template as it is marked CopyableAttribute(CopyType.DoNotCopy)
            Utility.CopyProperties(new_spawner, this);
            if (TemplateItem != null)
            {
                new_spawner.TemplateItem.SpawnerTempRefCount += 1;
            }
            if (TemplateMobile != null)
            {
                new_spawner.TemplateMobile.SpawnerTempRefCount += 1;
            }
            if (LootPack != null)
            {
                new_spawner.LootPack.SpawnerTempRefCount += 1;
            }
            if (CarvePack != null)
            {
                new_spawner.CarvePack.SpawnerTempRefCount += 1;
            }
            if (ArtifactPack != null)
            {
                new_spawner.ArtifactPack.SpawnerTempRefCount += 1;
            }

            return base.Dupe(new_spawner, amount);
        }
        [Constructable]
        public Spawner(int amount, int minDelay, int maxDelay, int team, int homeRange, string objectName)
            : base(0x1f13)
        {
            ArrayList objectNames = new ArrayList();
            objectNames.Add(objectName.ToLower());
            InitSpawn(amount, TimeSpan.FromMinutes(minDelay), TimeSpan.FromMinutes(maxDelay), team, homeRange, objectNames);
        }
        [Constructable]
        public Spawner(string objectName)
            : base(0x1f13)
        {
            ArrayList objectNames = new ArrayList();
            objectNames.Add(objectName.ToLower());
            InitSpawn(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, 4, objectNames);
        }
        [Constructable]
        public Spawner()
            : base(0x1f13)
        {
            ArrayList objectNames = new ArrayList();
            InitSpawn(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, 4, objectNames);

        }
        public Spawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, ArrayList objectNames)
            : base(0x1f13)
        {
            InitSpawn(amount, minDelay, maxDelay, team, homeRange, objectNames);
        }
        public void InitSpawn(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, ArrayList objectNames)
        {

            Visible = false;
            Movable = false;
            Running = true;
            m_Group = false;
            m_MobDirection = Direction.North;
            Name = "Spawner";
            m_MinDelay = minDelay;
            m_MaxDelay = maxDelay;
            SetSlotCount(index: 0, amount);
            m_Team = team;
            m_HomeRange = homeRange;
            m_ObjectNames = objectNames;
            m_Objects = new ArrayList();
            DoTimer(TimeSpan.FromSeconds(1));

            if (Core.RuleSets.StandardShardRules())
            {
                m_Shard = ShardConfig.Mortalis | ShardConfig.Siege | ShardConfig.Renaissance;
                m_CoreSpawn = false;
                Distro = SpawnerModeAttribs.ModeAI;
                _defaultSpawnMap();
            }
            else if (Core.RuleSets.AngelIslandRules())
            {
                m_Shard = ShardConfig.AngelIsland;
                m_CoreSpawn = false;
                Distro = SpawnerModeAttribs.ModeAI;
                _defaultSpawnMap();
            }
            else
            {   // error
                m_Shard = ShardConfig.None;
                m_CoreSpawn = false;
                Distro = SpawnerModeAttribs.None;
                _defaultSpawnMap();
            }

            // inform our spawner cache this spawner has been created
            try { CacheFactory.UpdateQuickTables(this, CacheFactory.QuickTableUpdate.Changed); }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public Spawner(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel < AccessLevel.GameMaster)
                return;

            SpawnerGump g = new SpawnerGump(this);
            from.SendGump(g);
        }
        public bool CheckTemplate()
        {
            if (!TemplateEnabled)
                return true;

            foreach (string name in ObjectNamesRaw)
            {
                Type t = ScriptCompiler.FindTypeByName(name);
                if (t == null)
                    return false;

                if (t.IsAssignableTo(typeof(Item)) && TemplateItem == null)
                    return false;

                if (t.IsAssignableTo(typeof(Mobile)) && TemplateMobile == null)
                    return false;
            }

            return true;
        }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Running)
            {
                list.Add(1060742); // active

                list.Add(1060656, m_SlotCount.ToString()); // amount to make: ~1_val~
                list.Add(1061169, m_HomeRange.ToString()); // range ~1_val~

                list.Add(1060658, "group\t{0}", m_Group); // ~1_val~: ~2_val~
                list.Add(1060659, "team\t{0}", m_Team); // ~1_val~: ~2_val~
                list.Add(1060660, "speed\t{0} to {1}", m_MinDelay, m_MaxDelay); // ~1_val~: ~2_val~

                List<string> names = ObjectNamesParsed;
                for (int i = 0; i < 3 && i < names.Count; ++i)
                    list.Add(1060661 + i, "{0}\t{1}", names[i], CountObjects((string)names[i]));
            }
            else
            {
                list.Add(1060743); // inactive
            }
        }
        public override void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
                return;

            if (from.AccessLevel == AccessLevel.Player)
            {
                LabelTo(from, string.Format("[{0}]", "Super Secret"));
                return;
            }

            // display the name
            NetState ns = from.NetState;
            ns.Send(new UnicodeMessage(this.Serial, this.ItemID, MessageType.Label, 0x3B2, 3, "ENU", "", (!string.IsNullOrEmpty(m_Source) ? m_Source + " " : "") + this.Name));
            int labels = 0;

            // This is a standard OSI spawn
            //  https://uo.stratics.com/hunters/spawn/spawnmap.jpg
            if (m_CoreSpawn)
            {
                LabelTo(from, (this is EventSpawner) ? "[Event]" : "" + "[Core]" + (Concentric ? " (Concentric)" : ""));
                labels++;
            }

            if (m_Shard.HasFlag(ShardConfig.AllShards))
            {
                if (labels == 0)
                    LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(ShardConfig.AllShards.ToString())));
                else
                    LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(ShardConfig.AllShards.ToString()) + (Running ? " [on]" : " (Off)")));
                labels++;
            }
            else
            {
                // Angel Island special spawn
                if (m_Shard.HasFlag(ShardConfig.AngelIsland))
                {
                    if (labels == 0)
                        LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(ShardConfig.AngelIsland.ToString())));
                    else
                        LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(ShardConfig.AngelIsland.ToString()) + (Running ? " [on]" : " (Off)")));
                    labels++;
                }
                // other shards
                if (m_Shard.HasFlag(ShardConfig.Siege) || m_Shard.HasFlag(ShardConfig.Mortalis) || m_Shard.HasFlag(ShardConfig.Renaissance))
                {
                    if (labels == 0)
                        LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(m_Shard.ToString())).Replace(" ", string.Empty));
                    else
                        LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(m_Shard.ToString())).Replace(" ", string.Empty) + (Running ? " (on)" : " (Off)"));
                }
            }

            if (labels < 2)
                if (Running)
                    LabelTo(from, "[Running]");
                else
                    LabelTo(from, "[Off]");
        }
        public virtual bool OkayStart()
        {
            return m_ObjectNames.Count > 0;
        }
        public virtual void Start()
        {
            if (OkayStart())
                DoTimer();

            UpdateDisplay();
        }
        public void Stop()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer.Flush();
            }

            UpdateDisplay();
        }
        public bool InHouse(object o)
        {
            if (o != null)
            {
                IPoint3D ip = o as IPoint3D;
                Item item = o as Item;
                if (ip != null && item != null)
                {
                    ip = item.GetWorldTop();
                    Point3D p = new Point3D(ip);
                    Region region = Region.Find(p, item.Map);
                    if (region is HouseRegion)
                        return true;
                }
            }

            return false;
        }
        public void UpdateDisplay()
        {
            if (Core.RuleSets.AngelIslandRules())
                return;

            if (Distro == SpawnerModeAttribs.ModeNeruns)
            {
                if (Running)
                    Hue = (int)SpawnerGraphics.NerunRunningHue;
                else
                    Hue = (int)SpawnerGraphics.NerunStoppedHue;
            }
            else if (Shard == ShardConfig.AllShards)
            {
                if (Running)
                    Hue = (int)SpawnerGraphics.AllShardsRunningHue;
                else
                    Hue = (int)SpawnerGraphics.AllShardsStoppedHue;
            }
            else
            {
                if (Running)
                    Hue = (int)SpawnerGraphics.CustomRunningHue;
                else
                    Hue = (int)SpawnerGraphics.CustomStoppedHue;
            }
        }
        private static bool m_worldLoaded = false;
        public override void WorldLoaded()
        {
            m_worldLoaded = true;
            return;
        }
        private bool DefragOk()
        {   // don't allow defragging of things that have not been fully initialized
            return m_worldLoaded;
        }
        public void Defrag()
        {
            #region Spawner placement under a house
            if (this.Map != Map.Internal && this.Map != null && DefragOk())
            {
                BaseHouse house;
                if ((house = BaseHouse.Find(this.Location, this.Map)) != null)
                {

                    // Exhibit spawners are okay inside houses. That's like the demo creatures
                    //  in the Siege Tent in the new player starting area.
                    // If we are spawning a multi like a camp, that's okay too
                    if (!this.Exhibit && !SpawningMulti())
                    {
                        Utility.ConsoleWriteLine(string.Format("Spawner {0} located at {1} in a house. Relocating...", this, this.Location), ConsoleColor.Red);

                        Point3D new_location = this.Location;
                        if (house.BanLocation != Point3D.Zero)
                            new_location = house.BanLocation;

                        this.Location = new_location;

                        if (Running)
                        {   // Defrag is called on server-up on all spawners running or not to do this Relocating, this is why we check 'Running' here
                            this.RemoveObjects();
                            this.Respawn();
                        }
                    }

                }
            }
            #endregion Spawner placement under a house

            #region Normal Defrag
            bool removed = false;

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object o = m_Objects[i];

                if (o is Item)
                {
                    Item item = (Item)o;

                    // Adam:
                    // 1. If GetBool(BoolTable.OnSpawner) == false here, the lock was lifted in Item.OnItemLifted()
                    // 2. Because our Spawner's timer starts before the world is fully loaded, i.e., that thing to defrag
                    //  has not finished Deserialization, we cannot check OnSpawner until the world has finished loading.
                    if (item.Deleted || item.Parent != null || (DefragOk() && !item.GetItemBool(ItemBoolTable.OnSpawner)))
                    {
                        item.SetItemBool(Item.ItemBoolTable.OnSpawner, false);
                        m_Objects.RemoveAt(i);
                        --i;
                        removed = true;
                    }
                }
                else if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    if (m.Deleted)
                    {
                        m_Objects.RemoveAt(i);
                        --i;
                        removed = true;
                    }
                    else if (m is BaseCreature)
                    {
                        if (((BaseCreature)m).Controlled || ((BaseCreature)m).IsAnyStabled || ((BaseCreature)m).GetCreatureBool(CreatureBoolTable.IsTownshipLivestock))
                        {
                            m_Objects.RemoveAt(i);
                            --i;
                            removed = true;
                        }
                    }
                }
                else
                {
                    m_Objects.RemoveAt(i);
                    --i;
                    removed = true;
                }
            }

            if (removed)
                InvalidateProperties();
            #endregion Normal Defrag
        }
        private bool SpawningMulti()
        {
            if (ObjectNamesRaw != null)
                foreach (object o in ObjectNamesRaw)
                    if (o is string name)
                    {
                        Type t = ScriptCompiler.FindTypeByName(name);
                        if (t != null)
                            if (t.IsAssignableTo(typeof(BaseMulti)))
                                return true;
                    }

            return false;
        }
        public virtual void OnTick(bool external = false)
        {   // make virtual so we can get the tick in our derived class EventTimer

            if (external)
                // we are being 'triggered' externally. When this flag is set, we don't cleanup the mobile if this is an event spawner
                SetFlag(SpawnerAttribs.External, external);

            DoTimer();

            if (m_Group)
            {
                Defrag();

                if (m_Objects.Count == 0)
                {
                    Respawn();
                }
                else
                {
                    return;
                }
            }
            else
            {
                Spawn();
            }
        }
        private void ResetTimer()
        {
            if (!Running)
                return;

            int minSeconds = (int)m_MinDelay.TotalSeconds;
            int maxSeconds = (int)m_MaxDelay.TotalSeconds;
            TimeSpan delay = TimeSpan.FromSeconds(Utility.RandomMinMax(minSeconds, maxSeconds));
            DoTimer(delay);
        }
        public void Respawn(bool force = false)
        {
            RemoveObjects(force);

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();

            for (int i = 0; i < Count /*GetSlotCount(index: 0)*/ /*m_SlotCount*/; i++)
                Spawn();

            tc.End();
            if (Debug)
                this.SendSystemMessage(string.Format("Respawn took {0}", tc.TimeTaken));

            // 11/18/22, Adam: after a respawn, recalculate the NextSpawn.
            ResetTimer();
        }

        #region Schedule Respawn
        Timer m_ScheduleRespawnTimer = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool ScheduleRespawn
        {
            get { return m_ScheduleRespawnTimer != null; }
            set
            {
                bool oldScheduleRespawnTimer = ScheduleRespawn;
                if (value == true && m_ScheduleRespawnTimer != null)
                    goto done;
                if (value == true && m_ScheduleRespawnTimer == null)
                {
                    m_ScheduleRespawnTimer = Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(ScheduleRespawnTick), new object[] { });
                    InvalidateProperties();
                    goto done;
                }
                if (value == false && m_ScheduleRespawnTimer != null)
                {
                    m_ScheduleRespawnTimer.Stop();
                    m_ScheduleRespawnTimer.Flush();
                    m_ScheduleRespawnTimer = null;
                    InvalidateProperties();
                    goto done;
                }
                if (value == false && m_ScheduleRespawnTimer == null)
                {
                    goto done;
                }

            done:
                ;
            }
        }
        private void ScheduleRespawnTick(object state)
        {
            RemoveObjects();
            Respawn();
            if (m_ScheduleRespawnTimer != null)
            {
                m_ScheduleRespawnTimer.Stop();
                m_ScheduleRespawnTimer.Flush();
                m_ScheduleRespawnTimer = null;
            }

        }
        #endregion Schedule Respawn
        #region Schedule Despawn
        Timer m_ScheduleDespawnTimer = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool ScheduleDespawn
        {
            get { return m_ScheduleDespawnTimer != null; }
            set
            {
                bool oldScheduleDespawnTimer = ScheduleDespawn;
                if (value == true && m_ScheduleDespawnTimer != null)
                    goto done;
                if (value == true && m_ScheduleDespawnTimer == null)
                {
                    m_ScheduleDespawnTimer = Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(ScheduleDespawnTick), new object[] { });
                    InvalidateProperties();
                    goto done;
                }
                if (value == false && m_ScheduleDespawnTimer != null)
                {
                    m_ScheduleDespawnTimer.Stop();
                    m_ScheduleDespawnTimer.Flush();
                    m_ScheduleDespawnTimer = null;
                    InvalidateProperties();
                    goto done;
                }
                if (value == false && m_ScheduleDespawnTimer == null)
                {
                    goto done;
                }

            done:
                ;
            }
        }
        private void ScheduleDespawnTick(object state)
        {
            RemoveObjects();
            if (m_ScheduleDespawnTimer != null)
            {
                m_ScheduleDespawnTimer.Stop();
                m_ScheduleDespawnTimer.Flush();
                m_ScheduleDespawnTimer = null;
            }

        }
        #endregion Schedule Despawn
        public virtual void Spawn()
        {
            if (GetFlag(SpawnerAttribs.ModeNeruns) || GetFlag(SpawnerAttribs.ModeMulti))
            {
                Defrag();
                int have = 0;
                int need = 0;
                int index = 0;
                // NerunsDistro wants to spawn all entries that are below the spawn threshold Entry Count
                for (int ix = 0; ix < m_ObjectNames.Count; ix++)
                {
                    if (ix >= GetSlotTableSize())
                    {   // error, out of bounds
                        Utility.ConsoleWriteLine(string.Format("Warning: Count index out of bounds: {0}", this), ConsoleColor.Red);
                        // patch it: This is probably dure to an error in original construction of the spawner.
                        //  Since we have an entry in the name table, we wil default it to a count of one.
                        Utility.ConsoleWriteLine(string.Format("Warning: Patching: {0}", ix), ConsoleColor.Green);
                        SetSlotCount(index: ix, value: 1);
                        LogHelper logger = new LogHelper("Nerun Spawner Patch.log", false, true, true);
                        logger.Log(LogType.Item, this, string.Format("Missing 'count' for spawner item: '{0}' at index: {1}", m_ObjectNames[ix], ix));
                        logger.Finish();
                        /* fall through */
                    }

                    need = GetSlotCount(index: ix);             // how many of this creature we need
                    have = 0;                                   // how many of this creature do we have
                    List<string> nameList = GetObjectNames(m_ObjectNames[ix] as string);

                    // count them up!
                    foreach (string name in nameList)
                    {
                        Type type = SpawnerType.GetType(name);
                        foreach (object o in m_Objects)
                            if (o.GetType() == type)
                                have++;
                    }

                    if (have < need)
                    {   // okay, spawn
                        index = ix;
                        break;
                    }
                }

                if (have < need)
                    Spawn(index, Utility.RandomMinMax(1, need - have));
            }
            else
            {
                if (m_ObjectNames.Count > 0)
                    Spawn(Utility.Random(m_ObjectNames.Count));
            }
        }
        public void Spawn(string objectName)
        {
            for (int i = 0; i < m_ObjectNames.Count; i++)
            {
                if ((string)m_ObjectNames[i] == objectName)
                {
                    Spawn(i);
                    break;
                }
            }
        }
        public void Spawn(int index, int count)
        {
            if ((!GetFlag(SpawnerAttribs.ModeNeruns) && !GetFlag(SpawnerAttribs.ModeMulti)) || count == 0)
                return;
            else
                for (int ix = 0; ix < count; ix++)
                    Spawn(index);
        }
        public void Spawn(int index)
        {
            Map map = Map;

            if (map == null || map == Map.Internal || m_ObjectNames.Count == 0 || index >= m_ObjectNames.Count)
                return;

            Defrag();

            if ((!GetFlag(SpawnerAttribs.ModeNeruns) && !GetFlag(SpawnerAttribs.ModeMulti)))
            {
                if (m_Objects.Count >= GetSlotCount(index: 0)/*m_SlotCount*/)
                    return;
            }

            string thingToSpawn = string.Empty;
            string text = ((string)m_ObjectNames[index]).Trim();
            //adam 8/29/22, we now allow ':' delimited lists of creatures
            string[] tokens = text.Split(':', StringSplitOptions.RemoveEmptyEntries);
            List<string> valid = new List<string>();
            int selector = Utility.Random(tokens.Length);
            for (int ix = 0; ix < tokens.Length; ix++)
            {
                string token = tokens[ix];

                Type type = SpawnerType.GetType(token);

                if (type == null)
                {
                    Utility.ConsoleWriteLine("{0} is not a valid type name.", ConsoleColor.Red, token);
                    tokens[ix] = string.Empty;
                }
                else
                    valid.Add(token);
            }
            // select it
            if (selector >= valid.Count || valid.Count == 0)
            {
                Console.WriteLine("No valid object to spawn at selector.", selector);
                return;
            }
            else
                thingToSpawn = valid[selector];

            try
            {
                // create the mobile or item
                object o = CreateRaw(thingToSpawn);

                if (o != null)
                {
                    try
                    {
                        if (o is Mobile)
                        {
                            Mobile m = (Mobile)o;
                            m_Objects.Add(m);
                            InvalidateProperties();
                            Point3D loc = (Utility.IsTownVendor(m) ? this.Location : GetSpawnPosition(o));
                            MoveToWorld(m, loc, map);
                            OnAfterMobileSpawn(m);   //plasma: new "event" allows you to make changes to the mob after spawn
                            m.OnAfterSpawn();
                        }
                        else if (o is Item)
                        {
                            Item item = (Item)o;
                            m_Objects.Add(item);
                            InvalidateProperties();
                            MoveToWorld(item, GetSpawnPosition(o), map);
                            OnAfterItemSpawn(item);
                            item.OnAfterSpawn();

                            // Adam: Warn if we're spawning stuff in a house.
                            //	This is a problem because the item will be orphaned by the spawner (? - not sure this is the case anymore)
                            //	which can lead to excessive item generation.
                            if (InHouse(item) == true)
                            {
                                Console.WriteLine("Warning: House spawn: Item({0}, {1}, {2}), Spawner({3}, {4}, {5})", item.Location.X, item.Location.Y, item.Location.Z, this.Location.X, this.Location.Y, this.Location.Z);
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Warning: Spawner({0}, {1}, {2}) generating object not template compatible", this.Location.X, this.Location.Y, this.Location.Z);
                    }
                }
                else
                    Console.WriteLine("Warning: Spawner({0}, {1}, {2}, {3}) generating object that does not exist: {4}", this.Location.X, this.Location.Y, this.Location.Z, this.Map, thingToSpawn);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public virtual void MoveToWorld(IEntity o, Point3D loc, Map map)
        {
            if (o is Mobile)
                (o as Mobile).MoveToWorld(loc, map);
            if (o is Item)
                (o as Item).MoveToWorld(loc, map);
        }
        protected virtual void OnAfterMobileSpawn(Mobile m)
        {
            if (m is BaseCreature bc)
            {
                // the interesting thing about Concentric spawned creatures is that their home becomes their spawn point
                //  and they get a new default RangeHome based upon the radius of the spawner
                //  Less 'clumping'!
                if (Concentric)
                {
                    bc.Home = m.Location;
                    bc.RangeHome = new Circle(m_HomeRange, new Vector2(this.Location.X, this.Location.Y)).GetConcentricHomeRange();
                }

                // template mobiles may have been constructed at location (0,0,0) and will be moved to there final spawn point
                //      when this happens, they will have been deactivated at (0,0,0) because that sector is not active.
                //      We therefore activate here, then deactivate as appropriate based on their new sector.
                if (bc.AIObject != null && !bc.Active)
                    bc.AIObject.Activate();

                // kill the template to expose the corpse
                if (bc.Spawner is Spawner spawner && spawner.StaticCorpse)
                    Timer.DelayCall(TimeSpan.FromSeconds(2), bc.Kill);

                // set clear mobile debugging
                bc.DebugMode = DebugMobile;

                // Set the EquipmentDisposition for this mobile here
                bc.MutateEquipment = MutateEquipment;

                bc.DeactivateIfAppropriate();
            }
            EventSink.InvokeSpawnedMobileCreated(new SpawnedMobileCreatedEventArgs(m));

            // SetProp 
            if (m_SetProp != null)
                SetObjectProp(m, m_SetProp);

            // SetSkill 
            if (m_SetSkill != null)
                SetObjectSkill(m, m_SetSkill);
        }
        protected virtual void OnAfterItemSpawn(Item item)
        {
            // give the spawned item a ref to this spawner
            item.Spawner = this;
            // QuestCode
            if (m_QuestCode != 0 && m_QuestCodeChance > Utility.RandomDouble())
                item.QuestCode = m_QuestCode;
            //GraphicID
            if (m_GraphicID != 0)
                item.ItemID = m_GraphicID;
            // OnSpawner
            item.SetItemBool(Item.ItemBoolTable.OnSpawner, true);
            // SetProp 
            if (m_SetProp != null)
                SetObjectProp(item, m_SetProp);
            if (item is BaseCamp bc)
            {   // the camp should decay at roughly the rate we have specified.
                //  internally though, camps refresh themselves OnEnter/OnExit
                //  but stop refreshing if corrupted. I.e, some component missing or killed.
                bc.Spawner = this;
            }
        }
        public static void SetObjectProp(object o, string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            try
            {
                foreach (string prop_string in input.Split(";"))
                {
                    var chunks = prop_string.Trim().Split(' ', 2);
                    string name = chunks[0];  // Movable 
                    string value = chunks[1]; // False
                    value = value.Replace("\"", "");

                    if (o is Item item)
                        item.SetObjectProp(name, value, message: false);
                    if (o is Mobile mob)
                        mob.SetObjectProp(name, value, message: false);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public static void SetObjectSkill(object o, string input)
        {
            try
            {
                foreach (string prop_string in input.Split(";"))
                {
                    var chunks = prop_string.Trim().Split(' ', 2);
                    string name = chunks[0];  // Swords 
                    string value = chunks[1]; // 100.3
                    value = value.Replace("\"", "");
                    bool result = SetMobileSkill(o as Mobile, name, value);
                    if (result != true)
                    {
                        if (o is Mobile mob)
                            mob.SendSystemMessage(string.Format("Could not set {0}: {1}", name, value));
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private static bool SetMobileSkill(Mobile m, string name, string value)
        {
            SkillName skill;
            Server.Skills skills = m.Skills;
            if (SkillName.TryParse(name, ignoreCase: true, out skill))
            {
                double result = 0;
                if (!double.TryParse(value, out result))
                    return false;

                skills[skill].Base = result;

                return true;
            }
            else
                return false;
        }
        private bool ThingInList(string text /*text from spawner */, string objToMatch /* what we are to lookup */)
        {
            text = text.ToLower();
            objToMatch = objToMatch.ToLower();
            //adam 8/29/22, we now allow ':' delimited lists of creatures
            string[] tokens = text.Split(':', StringSplitOptions.RemoveEmptyEntries);
            for (int ix = 0; ix < tokens.Length; ix++)
                if (tokens[ix] == objToMatch)
                    return true;
            return false;
        }
        public object CreateRaw(string name)
        {
            Map map = Map;

            if (map == null || map == Map.Internal)
                return null;

            // make sure the thing we are asked to create exists in our list 
            //  of stuff to create.
            bool bFound = false;
            for (int i = 0; i < m_ObjectNames.Count; i++)
            {
                if (ThingInList((string)m_ObjectNames[i], name))
                {
                    bFound = true;
                    break;
                }
            }

            if (bFound == false)
                return null;

            Type type = SpawnerType.GetType(name);
            object o = null;

            if (type != null)
            {
                try
                {
                    o = Activator.CreateInstance(type);

                    if (o != null)
                    {
                        if (o is Mobile)
                        {
                            Mobile m = (Mobile)o;
                            if (m is BaseCreature)
                            {
                                BaseCreature c = (BaseCreature)m;

                                if (TemplateEnabled) //copy our template props to the spawning mobile
                                {
                                    bool templateMobileBroken = m_TemplateMobile.Deleted == true;
                                    if (templateMobileBroken)
                                    {   // something is wing-wang. Our template has been deleted
                                        Utility.ConsoleWriteLine("Error: Our Mobile template has been deleted.", ConsoleColor.Red, m_TemplateMobile);
                                        LogHelper logger = new LogHelper("spawner.CreateRaw.log", false);
                                        logger.Log(LogType.Text, "Our Mobile template has been deleted. Sound the alarm!");
                                        logger.Log(LogType.Mobile, m_TemplateMobile);
                                        logger.Log(LogType.Item, this);
                                        logger.Finish();

                                        // clear the template
                                        m_TemplateMobile = null;
                                    }
                                    else
                                    {
                                        TimeSpan OldLifespan = c.Lifespan;
                                        DateTime OldCreation = c.Created;
                                        Utility.CopyPropertyIntersection(c, m_TemplateMobile);
                                        Utility.CopySkills(c, m_TemplateMobile);
                                        Utility.SwapBackpack(c, TemplateMobile);    // swap backpack

                                        if (!DynamicCopy)
                                            Utility.CopyLayers(c, m_TemplateMobile, CopyLayerFlags.Default);

                                        if (DynamicCopy)
                                        {
                                            //get use a new name/sex/clothing/body layout
                                            c.InitBody();
                                            c.InitOutfit();
                                        }
                                        // restore these
                                        c.Lifespan = OldLifespan;
                                        c.Created = OldCreation;
                                        // these should not carry over to the spawned mobile
                                        c.SpawnerTempMob = false;
                                        c.IsIntMapStorage = false;
                                    }
                                }

                                c.RangeHome = (m_WalkRange == -1) ? m_HomeRange : m_WalkRange;
                                c.CurrentWayPoint = m_WayPoint;
                                c.NavDestination = m_NavDest;
                                if (m_Team > 0) c.Team = m_Team;    // why don't we just set it without checking?
                                c.Home = this.Location;
                                c.Spawner = this;                   //Pix: give the spawned creature a ref to this spawner
                                c.SterlingMin = m_SterlingMin;
                                c.SterlingMax = m_SterlingMax;

                                // tamable override
                                if (Tamable != BoolFlags.Default)
                                    c.Tamable = (Tamable == BoolFlags.True) ? true : false;

                                //if we have a navdestination as soon as we spawn start on it
                                //if (c.NavDestination != NavDestinations.None)
                                if (!string.IsNullOrEmpty(c.NavDestination))
                                    c.AIObject.Think();

                                /////////////////////////////
                                // customize the mob spawned
                                // IsInvulnerable has been deprecated. For spawners, we will use blessed
                                //  10/24/22, Adam: IsInvulnerable is still in RunUO 2.6 and the client still respects it.
                                //  putting it back.
                                c.IsInvulnerable = m_Invulnerable;

                                //  10/24/22, Adam: Exhibit creatures to replace Blessed
                                if (m_Exhibit)
                                    MakeExhibit(c);

                                // 12/28/22, Adam: Should guards ignore this creature? (town invasions)
                                c.GuardIgnore = GuardIgnore;

                                // training dummy. Doesn't move, doesn't take damage, and doesn't fight back
                                if (Dummy)
                                    MakeDummy(c);

                                // robot creature - robot AI, not tame, but only wonders and stays(control orders.)
                                c.Robot = Robot;

                                // should this creature take damage?
                                c.BlockDamage = BlockDamage;

                                // should this creature be unstoppable? (requires BlockDamage)
                                c.MissionCritical = MissionCritical;

                                // not only changes AI to Melee, but also assigns skill, a weapon, and equips it
                                if (m_MorphMode == MorphMode.Melee)
                                    ChampEngine.MakeMelee(c, c.AIObject, boss: false);

                                // this may be a debug spawner, spawning creatures in debug mode
                                //c.DebugMode = Debug ? DebugFlags.AI : DebugFlags.None;

                                // if it's not a template mob, it may be a paragon!
                                if (TemplateEnabled == false && m_Exhibit == false)
                                    if (Utility.RandomChance(10) && HighPowerTameNearby())
                                        c.MakeParagon();
                            }
                        }
                        else if (o is Item)
                        {
                            Item item = (Item)o;

                            if (!Registry.Contains(item.ItemID))
                                Registry.Add(item.ItemID);

                            if (m_TemplateItem != null && m_TemplateItem.Deleted == true)
                            {   // something is wing-wang. Our template has been deleted
                                Console.WriteLine("Our Item template has been deleted. Sound the alarm!", m_TemplateItem);
                                LogHelper logger = new LogHelper("spawner.CreateRaw.log", false);
                                logger.Log(LogType.Text, "Our Item template has been deleted. Sound the alarm!");
                                logger.Log(LogType.Item, m_TemplateItem);
                                logger.Finish();
                            }

                            if (TemplateEnabled)
                            {
                                if (TemplateStyle == TemplateMode.CopyProperties)
                                {
                                    Utility.CopyPropertyIntersection(item, m_TemplateItem);
                                    item.LastMoved = DateTime.UtcNow;    // refresh decay
                                    item.SpawnerTempItem = false;        // Flag this as not a template
                                    item.IsIntMapStorage = false;        // flag this a not intmapstorage			
                                }
                                else if (TemplateStyle == TemplateMode.Dupe)
                                {
                                    item.Delete();
                                    o = Utility.Dupe(m_TemplateItem);
                                    System.Diagnostics.Debug.Assert(o != null);
                                    item = (Item)o;
                                    item.LastMoved = DateTime.UtcNow;    // refresh decay
                                    item.SpawnerTempItem = false;        // Flag this as not a template
                                    item.IsIntMapStorage = false;        // flag this a not intmapstorage			
                                }
                                else
                                    System.Diagnostics.Debug.Assert(false);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {   // kill the damn thing
                    Running = false;
                    if (this is EventSpawner)
                    {
                        (this as EventSpawner).EventStart = DateTime.MinValue.ToString();
                        (this as EventSpawner).EventEnd = (this as EventSpawner).EventStart;
                    }
                    SendSystemMessage("Incompatible types");
                    SendSystemMessage("You may not mix template types, for example Items and Mobiles");
                    Console.WriteLine("Warning: Spawner({0}, {1}, {2}) generating object not template compatible", this.Location.X, this.Location.Y, this.Location.Z);
                    Console.WriteLine("{0}", ex);
                    LogHelper.LogException(ex);
                }
            }

            return o;
        }
        public static void MakeExhibit(BaseCreature bc)
        {
            bc.IsInvulnerable = true;
            bc.Blessed = false;
            bc.BardImmune = true;
            bc.FightMode = FightMode.None;
            if (bc.CanOverrideAI)
                bc.AI = AIType.AI_Animal;
            bc.Tamable = false;
            bc.HerdingImmune = true;
            bc.Fame = bc.Karma = 0;
            bc.IOBAlignment = IOBAlignment.None;
            bc.GuildAlignment = AlignmentType.None;
        }
        public static void MakeDummy(BaseCreature bc)
        {
            bc.BlockDamage = true;
            bc.SetMobileBool(Mobile.MobileBoolTable.TrainingMobile, true);   // necessary if you wish to gain skill off a BlockDamage creature
            bc.FightMode = FightMode.None;
            if (bc.CanOverrideAI)
                bc.AI = AIType.AI_Animal;
            bc.DamageMin = bc.DamageMax = 1;
            bc.Tamable = false;
            bc.HerdingImmune = true;
            bc.Fame = bc.Karma = 0;
            bc.IOBAlignment = IOBAlignment.None;
            bc.GuildAlignment = AlignmentType.None;
        }
        public bool HighPowerTameNearby()
        {// only spawn paragons if we are getting farmed by a highpower tamer/tame
            // the the tamer is closest enough to see the creature spawn, we can see him! (well, his pet)
            IPooledEnumerable eable = Map.GetMobilesInRange(this.Location, Map.MaxLOSDistance);
            foreach (Mobile m in eable)
            {
                // ignore staff
                if (m.AccessLevel > AccessLevel.Player)
                    continue;

                // if a creature 
                if (m is BaseCreature)
                {   // if controlled (summoned or tame)
                    if (((m as BaseCreature).Controlled && (m as BaseCreature).ControlMaster != null) || ((m as BaseCreature).Summoned && (m as BaseCreature).SummonMaster != null))
                    {   // only trap tame dragons and daemons for now
                        if (m is Dragon || m is Daemon)
                        {
                            eable.Free();
                            return true;
                        }
                    }
                }

            }
            eable.Free();

            return false;
        }
        public static bool ClearPathLand(Point3D location, Point3D goal, Map map, bool canOpenDoors = true)
        {
            // can we get there from here?
            // that is, could a human walk from the proposed treasure map location to this nearby spawner.
            Movement.MovementObject obj_start = new Movement.MovementObject(location, goal, map, canOpenDoors: canOpenDoors);
            if (!MovementPath.PathTo(obj_start))
                return false;   // can't get there from here
            else
                return true;
        }
        public static bool ClearPath(Point3D location, List<Point2D> goals, int z, Map map, bool canOpenDoors = true)
        {
            foreach (Point2D p in goals)
            {
                Point3D px = new Point3D(p.X, p.Y, z);
                if (ClearPathLand(location, px, map, canOpenDoors))
                    return true;
            }
            return false;
        }

        public virtual Point3D GetSpawnPosition(object o)
        {
            return GetSpawnPosition(Map, Location, m_HomeRange, m_SpawnerFlags, o, ValidateSpawnPosition);
        }
        public virtual bool ValidateSpawnPosition(Point3D loc, Map map)
        {
            return true;
        }
        private SpawnFlags m_SpawnerFlags = SpawnFlags.None;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
        public SpawnFlags SpawnerFlags
        {
            get { return m_SpawnerFlags; }
            set
            {
                SpawnFlags oldSpawnFlags = m_SpawnerFlags;
                if (Concentric)
                    m_SpawnerFlags = SpawnFlags.Concentric | value;
                else
                    m_SpawnerFlags = value;
            }
        }
        private static int IsWaterTile(Map map, Point3D location)
        {
            if (Utility.IsWater(map, location.X, location.Y, location.Z)) return 1;
            else return 2;
        }
        public delegate bool LocationValidator(Point3D loc, Map map);
        public static Point3D GetSpawnPosition(Map map, Point3D location, int homeRange, SpawnFlags sflags, object o, LocationValidator validator = null)
        {
            #region Flags
            bool allowChangeZ = (sflags & SpawnFlags.AllowChangeZ) != 0;
            bool forceZ = (sflags & SpawnFlags.ForceZ) != 0;
            bool avoidPlayers = (sflags & SpawnFlags.AvoidPlayers) != 0;
            bool boat = (sflags & SpawnFlags.Boat) != 0;
            bool clearPath = (sflags & SpawnFlags.ClearPath) != 0;
            bool noBlock = (sflags & SpawnFlags.NoBlock) != 0;
            #endregion Flags
            Utility.CanFitFlags flags = Utility.CanFitFlags.requireSurface;
            bool waterMob = false;
            bool waterOnlyMob = false;
            if (o is Mobile m)
            {
                if (m != null && m.CanSwim == true) flags |= Utility.CanFitFlags.canSwim;
                if (m != null && m.CantWalkLand == true) flags |= Utility.CanFitFlags.cantWalk;
                waterMob = m.CanSwim;
                waterOnlyMob = m.CanSwim && m.CantWalkLand;
            }
            else if (o == null)
            {   // assume spawn anywhere
                waterMob = true;
            }

            if (map == null)
                return location;

            /* get a reasonable set of points */
            List<Point3D> p3Dlist = new();
            for (int jx = 0; jx < SpawnPointTries; jx++)
            {
                Point3D point = GetPoint(map, location, homeRange, sflags);
                List<int> HardZs = new(Utility.GetHardZs(map, point));
                List<int> UseableZs = new(HardZs);
                if (allowChangeZ)
                {   // allow spawning on different floors/levels. For instance, inside bank, and on the roof
                    Utility.Shuffle(UseableZs);
                }
                else if (forceZ)
                {   // needs to be on the same z as the spawner
                    UseableZs = new List<int>() { location.Z };
                }
                else
                {   // we prefer the level on which the spawner is located, otherwise, average z
                    //  We also include nearby water tiles
                    UseableZs = PreferredZ(map, location.Z, point, HardZs);
                }

                // sometimes, the map lays water tiles over gullies/ditches in dungeons (lakes). Avoid these for land-only mobiles
                //  Example (5228, 911, -45) / (5228, 911, -65) (Destard)
                if (HasWater(map, point, HardZs))
                    if (!waterMob)
                        continue;
                    else
                        // this prevents watermobs from spawning on the landtile under the water
                        RemoveNonWater(map, point, UseableZs);

                foreach (int z in UseableZs)
                {
                    point.Z = z;

                    if (waterOnlyMob && !Utility.IsValidWater(map, point.X, point.Y, z))
                        continue;

                    if (validator != null && !validator(point, map))
                        continue;

                    if (waterMob || waterOnlyMob)
                    {
                        if (Utility.CanSpawnWaterMobile(map, point))
                            if (!p3Dlist.Contains(point))
                                p3Dlist.Add(new Point3D(point.X, point.Y, z));
                    }

                    if (!waterOnlyMob && Utility.CanSpawnLandMobile(map, point, flags))
                        if (!p3Dlist.Contains(point))
                            p3Dlist.Add(new Point3D(point.X, point.Y, z));
                }
            }
#if false
            // if the creature can swim, prefer water tiles
            if (waterMob)
                p3Dlist.Sort((x1, x2) =>
                {
                    return IsWaterTile(map, x1).CompareTo(IsWaterTile(map, x2));
                });
#else
            Utility.Shuffle(p3Dlist);
#endif

            List<Point3D> nearPlayer = new();

            // process point rules
            for (int ix = 0; ix < p3Dlist.Count; ix++)
            {
                Point3D point = p3Dlist[ix];    // pull from the possibly ordered list

                // don't allow spawn from Shame to spill over into Wind for example.
                if (ChangingDungeons(starting_location: location, proposed_location: point))
                    continue;

                if (avoidPlayers && Utility.NearPlayer(map, new Point3D(point.X, point.Y, location.Z), 15))
                {   // don't fail here, instead add to a list and select the best one later
                    if (point != location)
                        nearPlayer.Add(point);
                    continue;
                }
                if (boat && BaseBoat.FindBoatAt(new Point2D(point.X, point.Y), map) == null)
                    continue;
                if (clearPath && ClearPathLand(location, point, map: map) == false)
                    continue;
                if (noBlock && Blocked(location, point, map: map) == true)
                    continue;

                if (waterMob)
                {   // water mobs, just get the point
                    if (Utility.CanSpawnWaterMobile(map, point))
                        return point;
                }
                if (!waterOnlyMob)
                {   // land/water mobs get CanSpawnLandMobile
                    if (Utility.CanSpawnLandMobile(map, point, flags))
                        return point;
                }
            }

            // okay, don't have a perfect point, use one from our nearPlayer list if we have one.
            if (nearPlayer.Count > 0)
            {
                if ((sflags & SpawnFlags.SpawnFar) != 0)
                {
                    nearPlayer.Sort((x1, x2) =>
                    {
                        return Utility.GetDistanceToSqrt(location, x2).CompareTo(Utility.GetDistanceToSqrt(location, x1));
                    });

                    //foreach (var vp in nearPlayer)
                    //Console.WriteLine("{0}", Utility.GetDistanceToSqrt(location, vp));
                    //Console.WriteLine("--------------------------");

                    return nearPlayer[0];
                }
                else
                    return nearPlayer[Utility.Random(nearPlayer.Count)];
            }

            // give up and return the starting location
            SpawnPointFails++;
            return location;
        }
        private static bool HasWater(Map map, Point3D point, List<int> Zs)
        {
            foreach (int z in Zs)
                if (Utility.IsWater(map, point.X, point.Y, z) && Utility.FindOneItemAt(new Point3D(point.X, point.Y, z), map, typeof(Item), 2, false) == null)
                    return true;

            return false;
        }
        private static void RemoveNonWater(Map map, Point3D point, List<int> Zs)
        {
            List<int> toRemove = new List<int>();
            foreach (int z in Zs)
                if (!Utility.IsWater(map, point.X, point.Y, z))
                    toRemove.Add(z);

            foreach (int z in toRemove)
                Zs.Remove(z);

            return;
        }
        private static bool ChangingDungeons(Point3D starting_location, Point3D proposed_location)
        {
            Region r1 = Region.Find(starting_location, Map.Felucca);
            Region r2 = Region.Find(proposed_location, Map.Felucca);
            if (r1 != null && r1.IsDungeonRules && r1 != r2)
                return true;
            return false;
        }
        private static List<int> PreferredZ(Map map, int baseZ, Point3D point, List<int> Zs)
        {
            List<int> list = new();
            if (Zs.Contains(baseZ)) // some GMs put spawners in trees and on rocks. Don't add these
                list.Add(baseZ);

            // get this reasonable z
            int avz = map.GetAverageZ(point.X, point.Y);
            if (!list.Contains(avz))
                list.Add(avz);

            // add in water tiles, usually -5 relative to nearby land at 0
            foreach (int z in Zs)
                if (!list.Contains(z))
                    if (Utility.IsWater(map, point.X, point.Y, z))
                        list.Add(z);

            return list;
        }
        #region SpawnPointTries and analysis 
        /* SpawnPointTries is heuristically derived.
         * The commands below returned the following values.
         * tries 10
         * fails 440
         * tries 100
         * fails 263
         * tries 50
         * fails 261
         * tries 25
         * fails 272
         * tries 15
         * fails 309
         * Because of these results, it looks like 25 (re)tries results in a reasonable
         *  cost benefit payoff
         */
        public static int SpawnPointFails;
        public static int SpawnPointTries = 25;
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }
        public static void Initialize()
        {
            // world building
            CommandSystem.Register("AdjustSpawn", AccessLevel.Administrator, new CommandEventHandler(AdjustSpawn_OnCommand));

            // diagnostics
            CommandSystem.Register("SpawnPointFails", AccessLevel.Owner, new CommandEventHandler(SpawnPointFails_OnCommand));
            CommandSystem.Register("SpawnPointTries", AccessLevel.Owner, new CommandEventHandler(SpawnPointTries_OnCommand));
        }
        public static void SpawnPointTries_OnCommand(CommandEventArgs e)
        {
            try
            {
                SpawnPointTries = int.Parse(e.ArgString);
            }
            catch
            {
                e.Mobile.SendMessage("Usage: SpawnPointTries <number of spawner tries>");
            }
        }
        public static void SpawnPointFails_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("SpawnPointFails: {0}", SpawnPointFails);
            SpawnPointFails = 0;
        }
        #endregion
        public static Point3D GetPoint(Map map, Point3D location, int homeRange, SpawnFlags sflags)
        {
            int x, y, z;
            if ((sflags & SpawnFlags.Concentric) != 0)
            {
                Circle c = new Circle(homeRange, new Vector2(location.X, location.Y));
                if ((sflags & SpawnFlags.SpawnFar) == 0)
                {
                    Point3D rpa = c.RandomPointAdjust(spawn_far: false, far_wiggle: 0);
                    x = rpa.X;
                    y = rpa.Y;
                }
                else
                {   // need to handle 'far' spawns ('5' is just heuristically derived.)
                    Point3D rpa = c.RandomPointAdjust(spawn_far: true, far_wiggle: 5);
                    x = rpa.X;
                    y = rpa.Y;
                }
            }
            else
            {
                if ((sflags & SpawnFlags.SpawnFar) != 0)
                {
                    x = (int)((double)location.X + Spawner.RandomFar() * (double)homeRange);
                    y = (int)((double)location.Y + Spawner.RandomFar() * (double)homeRange);
                }
                else
                {
                    x = location.X + (Utility.Random((homeRange * 2) + 1) - homeRange);
                    y = location.Y + (Utility.Random((homeRange * 2) + 1) - homeRange);
                }
            }

            z = map.GetAverageZ(x, y);

            return new Point3D(x, y, z);
        }
        const int BlockDistance = 10;
        public static bool Blocked(Point3D start, Point3D dest, Map map)
        {
            if (ClearPathLand(dest, new Point3D(dest.X + BlockDistance, dest.Y, dest.Z), map: map))                   // right
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X - BlockDistance, dest.Y, dest.Z), map: map))                   // left
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X - BlockDistance, dest.Y - BlockDistance, dest.Z), map: map))   // upper left
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X + BlockDistance, dest.Y - BlockDistance, dest.Z), map: map))   // upper right
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X, dest.Y - BlockDistance, dest.Z), map: map))                   // top
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X, dest.Y + BlockDistance, dest.Z), map: map))                   // bottom
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X - BlockDistance, dest.Y + BlockDistance, dest.Z), map: map))   // lower left
                return false;
            if (ClearPathLand(dest, new Point3D(dest.X + BlockDistance, dest.Y + BlockDistance, dest.Z), map: map))   // lower right
                return false;
            return true;
        }
        public void DoTimer()
        {
            if (!Running)
                return;

            ResetTimer();
        }
        public void DoTimer(TimeSpan delay)
        {
            if (!Running)
                return;

            m_End = DateTime.UtcNow + delay;

            if (m_Timer != null)
            {
                m_Timer.Stop();         // stop the timer
                m_Timer.Flush();        // remove any queued ticks
            }

            m_Timer = new InternalTimer(this, delay);
            m_Timer.Start();
        }
        private class InternalTimer : Timer
        {
            private Spawner m_Spawner;

            public InternalTimer(Spawner spawner, TimeSpan delay)
                : base(delay)
            {
                if (spawner.IsFull)
                    Priority = TimerPriority.FiveSeconds;
                else
                    Priority = TimerPriority.OneSecond;

                m_Spawner = spawner as Spawner;
            }

            protected override void OnTick()
            {
                if (m_Spawner != null && !m_Spawner.Deleted && m_Spawner.Running == true)
                    m_Spawner.OnTick();
            }
        }
        public int CountObjects(string objectName)
        {
            Defrag();

            int count = 0;
            if (GetFlag(SpawnerAttribs.ModeNeruns) || GetFlag(SpawnerAttribs.ModeMulti) && objectName.Contains(':'))
            {
                List<string> list = GetObjectNames(objectName);
                for (int i = 0; i < m_Objects.Count; ++i)
                    foreach (string name in list)
                        if (Insensitive.Equals(name, m_Objects[i].GetType().Name))
                            ++count;
            }
            else
            {
                for (int i = 0; i < m_Objects.Count; ++i)
                    if (Insensitive.Equals(objectName, m_Objects[i].GetType().Name))
                        ++count;
            }

            return count;
        }
        public void RemoveObjects(string objectName)
        {
            Defrag();

            objectName = objectName.ToLower();

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object o = m_Objects[i];

                if (Insensitive.Equals(objectName, o.GetType().Name))
                {
                    if (o is Item)
                        ((Item)o).Delete();
                    else if (o is Mobile)
                        ((Mobile)o).Delete();
                }
            }

            InvalidateProperties();
        }
        public void RemoveObjects(bool force = false)
        {
            Defrag();

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object o = m_Objects[i];

                if (o is Item item && (!item.GetItemBool(ItemBoolTable.IsLinked) || force))
                    ((Item)o).Delete();
                else if (o is Mobile mobile && (!mobile.GetMobileBool(Mobile.MobileBoolTable.IsLinked) || force))
                    ((Mobile)o).Delete();
            }

            InvalidateProperties();
        }
        public static double RandomFar()
        {
            if (Utility.RandomBool())
                return -Math.Sqrt(Utility.RandomDouble());
            else
                return Math.Sqrt(Utility.RandomDouble());

            // if you want a steeper congregation toward the outside, replace Math.Sqrt(...) with Math.Pow(Utility.RandomDouble(), .33);
            // Pow(x, .33) is cube root, .25 is 4th root etc, etc
            // basically the higher the root, the sharper the dropoff toward center will be
        }
        public void BringToHome()
        {
            Defrag();

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object o = m_Objects[i];

                if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    m.MoveToWorld(Location, Map);
                }
                else if (o is Item)
                {
                    Item item = (Item)o;

                    item.MoveToWorld(Location, Map);
                }
            }
        }

        public override void MoveToWorld(Point3D location, Map map, Mobile responsible = null)
        {
            base.MoveToWorld(location, map);
        }
        private bool CanDeleteTemplate(IEntity o)
        {
            if (o is Mobile m && m.SpawnerTempRefCount == 0) { return true; }
            else if (o is Item i && i.SpawnerTempRefCount == 0) { return true; }
            return false;
        }
        public override void OnDelete()
        {
            base.OnDelete();

            // Remove Templates
            if (m_LootPack != null)
            {
                m_LootPack.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_LootPack))
                    m_LootPack.Delete();
            }

            if (m_CarvePack != null)
            {
                m_CarvePack.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_CarvePack))
                    m_CarvePack.Delete();
            }

            if (m_TemplateMobile != null)
            {
                m_TemplateMobile.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_TemplateMobile))
                    m_TemplateMobile.Delete();
            }

            if (m_TemplateItem != null)
            {
                m_TemplateItem.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_TemplateItem))
                    m_TemplateItem.Delete();
            }

            if (m_ArtifactPack != null)
            {
                m_ArtifactPack.SpawnerTempRefCount--;
                if (CanDeleteTemplate(m_ArtifactPack))
                    m_ArtifactPack.Delete();
            }

            // Remove Creatures
            RemoveObjects();
            if (m_Timer != null)
                m_Timer.Stop();

            // inform our spawner cache this spawner has been deleted
            try { CacheFactory.UpdateQuickTables(this, CacheFactory.QuickTableUpdate.Deleted); }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        /// <summary>
        /// Notifies the spawner of this mobile, of its death.
        /// This is used by our special 'champ level' PushBackSpawners
        /// </summary>
        /// <param name="killed"></param>
        public virtual void SpawnedMobileKilled(Mobile killed)
        {
            #region Trigger System

            // first, process players responsible for the kill
            if (m_TriggerLinkPlayers != null)
            {
                if (TriggerSystem.CheckEvent(this))
                {
                    if (killed is BaseCreature bc)
                    {   // find legit damagers
                        List<DamageStore> list = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);

                        // divvy up points between party members
                        SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(list);

                        // anti-cheezing: Players have found that healing a monster (or allowing it to heal itself,) can yield unlimited damage points.
                        //  We therefore limit the damage points to no more than the creature's HitsMax
                        BaseCreature.ClipDamageStore(ref Results, bc);

                        foreach (Mobile m in Results.Keys)
                            if (m is PlayerMobile pm)
                            {
                                if (TriggerSystem.CanTrigger(pm, m_TriggerLinkPlayers))
                                {
                                    if (pm.KillQueue == null)
                                        pm.KillQueue = new();

                                    pm.KillQueue.Enqueue(bc);

                                    TriggerSystem.CheckTrigger(pm, m_TriggerLinkPlayers);
                                }
                            }
                    }
                }
            }

            // now for the creature
            if (m_TriggerLinkCreature != null)
                if (TriggerSystem.CheckEvent(this))
                    if (killed is BaseCreature bc)
                    {
                        // find legit damagers
                        List<DamageStore> list = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);

                        // divvy up points between party members
                        SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(list);

                        // anti-cheezing: Players have found that healing a monster (or allowing it to heal itself,) can yield unlimited damage points.
                        //  We therefore limit the damage points to no more than the creature's HitsMax
                        BaseCreature.ClipDamageStore(ref Results, bc);

                        foreach (Mobile m in Results.Keys)
                            if (m is PlayerMobile pm)
                            {
                                if (pm.KillQueue == null)
                                    pm.KillQueue = new();

                                if (!pm.KillQueue.Contains(bc))
                                    pm.KillQueue.Enqueue(bc);
                            }

                        TriggerSystem.CheckTrigger(bc, m_TriggerLinkCreature);
                    }

            #endregion Trigger System

            #region Goodies
            if (m_GoodiesRadius >= 0 && m_GoodiesTotalMin >= 0 && m_GoodiesTotalMax > 0 && m_GoodiesTotalMax >= m_GoodiesTotalMin)
            {
                int dropped = Engines.Invasion.GoodiesTimer.DropGoodies(killed.Location, killed.Map, m_GoodiesRadius, m_GoodiesTotalMin, m_GoodiesTotalMax);

                LogHelper logger = new LogHelper("SpawnerGoodies.log", false, true);
                logger.Log(LogType.Item, this, string.Format("Mobile {0} dropped {1:N0} gold in goodies", killed, dropped));
            }
            #endregion

            DistributeArtifacts(killed);
        }
        #region Save Flags
        [Flags]
        enum SaveFlags : UInt32
        {
            None = 0x0,
            Shard = 0x01,
            Source = 0x02,
            NeedsReview = 0x04,
            SpawnerAttribs = 0x08,
            WalkRange = 0x10,
            SlotCount = 0x20,
            PatchID = 0x40,
            Invulnerable = 0x100,
            WalkRangeCalc = 0x200,
            Exhibit = 0x400,
            SpawnFlags = 0x800,
            QuestCode = 0x1000,
            GraphicID = 0x2000,
            __open1 /*LastChange*/ = 0x4000,
            SetProp = 0x8000,
            SetSkill = 0x10000,
            TemplateInternalize = 0x20000,
            CarveOverride = 0x40000,
            TriggerLinkPlayers = 0x80000,
            Goodies = 0x100000,
            Artifacts = 0x200000,
            TemplateMobileDefinition = 0x400000,
            Tamable = 0x800000,
            NavDest = 0x1000000,
            LootString = 0x2000000,
            LootStringProps = 0x4000000,
            MutateEquipment = 0x8000000,
            TriggerLinkCreature = 0x10000000,
            DropsSterling = 0x20000000,
            MorphMode = 0x40000000,
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
            if (version > 16)
                sf = (SaveFlags)reader.ReadInt();
            return sf;
        }

        private SaveFlags WriteSaveFlags(GenericWriter writer)
        {
            m_SaveFlags = SaveFlags.None;
            SetFlag(SaveFlags.Shard, m_Shard != ShardConfig.AngelIsland ? true : false);    // don't save unless the shard config is not AI
            SetFlag(SaveFlags.Source, m_Source != string.Empty ? true : false);             // Sometimes these areas: https://uo.stratics.com/hunters/spawn/spawnmap.jpg 
            SetFlag(SaveFlags.NeedsReview, m_NeedsReview != false ? true : false);          // this spawner needs to be reviewed.
            SetFlag(SaveFlags.SpawnerAttribs, m_SpawnerAttribs != SpawnerAttribs.None ? true : false);
            SetFlag(SaveFlags.WalkRange, m_WalkRange != -1 ? true : false);
            SetFlag(SaveFlags.SlotCount, Count > 0 ? true : false);
            SetFlag(SaveFlags.PatchID, m_PatchID > 0 ? true : false);
            SetFlag(SaveFlags.Invulnerable, m_Invulnerable ? true : false);
            SetFlag(SaveFlags.WalkRangeCalc, m_WalkRangeCalc != false ? true : false);
            SetFlag(SaveFlags.Exhibit, m_Exhibit != false ? true : false);
            SetFlag(SaveFlags.SpawnFlags, m_SpawnerFlags != SpawnFlags.None ? true : false);
            SetFlag(SaveFlags.QuestCode, m_QuestCode != 0 ? true : false);
            SetFlag(SaveFlags.GraphicID, GraphicID != 0 ? true : false);
            //SetFlag(SaveFlags.LastChange, m_LastChange != null ? true : false); - obsolete
            SetFlag(SaveFlags.SetProp, m_SetProp != null ? true : false);
            SetFlag(SaveFlags.SetSkill, m_SetSkill != null ? true : false);
            SetFlag(SaveFlags.TemplateInternalize, m_TemplateInternalize ? true : false);
            SetFlag(SaveFlags.CarveOverride, m_CarveOverride ? true : false);
            SetFlag(SaveFlags.TriggerLinkPlayers, m_TriggerLinkPlayers != null ? true : false);
            SetFlag(SaveFlags.TriggerLinkCreature, m_TriggerLinkCreature != null ? true : false);
            SetFlag(SaveFlags.Goodies, m_GoodiesRadius != 0 || m_GoodiesTotalMin != 0 || m_GoodiesTotalMax != 0);
            SetFlag(SaveFlags.Artifacts, m_ArtifactPack != null || m_ArtifactCount != 0);
            SetFlag(SaveFlags.TemplateMobileDefinition, m_TemplateMobileDefinition != 0);
            SetFlag(SaveFlags.Tamable, Tamable != BoolFlags.Default);
            SetFlag(SaveFlags.NavDest, !string.IsNullOrEmpty(m_NavDest));
            SetFlag(SaveFlags.LootString, !string.IsNullOrEmpty(m_LootString));
            SetFlag(SaveFlags.LootStringProps, !string.IsNullOrEmpty(m_LootStringProps));
            SetFlag(SaveFlags.MutateEquipment, m_EquipmentDisposition != EquipmentDisposition.None);
            SetFlag(SaveFlags.DropsSterling, GetFlag(SpawnerAttribs.DropsSterling));
            SetFlag(SaveFlags.MorphMode, m_MorphMode != MorphMode.Default);

            writer.Write((int)m_SaveFlags);
            return m_SaveFlags;
        }
        #endregion Save Flags
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 47;
            writer.Write(version);                  // version
            m_SaveFlags = WriteSaveFlags(writer);   // always follows version

            // version 47
            if (GetFlag(SaveFlags.MorphMode))
                writer.Write((byte)m_MorphMode);

            // version 46
            if (GetFlag(SaveFlags.DropsSterling))
            {
                writer.Write(m_SterlingMin);
                writer.Write(m_SterlingMax);
            }

            // version 45, eliminate m_LastChange. Now in base class

            // v44
            if (GetFlag(SaveFlags.TriggerLinkCreature))
                writer.Write((Item)m_TriggerLinkCreature);

            // v43
            if (GetFlag(SaveFlags.MutateEquipment))
                writer.Write((int)m_EquipmentDisposition);

            // v42
            if (GetFlag(SaveFlags.LootString))
                writer.Write(m_LootString);

            if (GetFlag(SaveFlags.LootStringProps))
                writer.Write(m_LootStringProps);

            // v41
            if (GetFlag(SaveFlags.NavDest))
                writer.Write(m_NavDest);

            // v40
            if (GetFlag(SaveFlags.Tamable))
                writer.Write((byte)m_Tamable);

            // v39
            if (GetFlag(SaveFlags.TemplateMobileDefinition))
                writer.Write(m_TemplateMobileDefinition);

            // v37
            if (GetFlag(SaveFlags.Artifacts))
            {
                writer.Write((Item)m_ArtifactPack);
                writer.Write((int)m_ArtifactCount);
            }

            // v36
            if (GetFlag(SaveFlags.Goodies))
            {
                writer.Write((int)m_GoodiesRadius);
                writer.Write((int)m_GoodiesTotalMin);
                writer.Write((int)m_GoodiesTotalMax);
            }

            // v35
            if (GetFlag(SaveFlags.TriggerLinkPlayers))
                writer.Write((Item)m_TriggerLinkPlayers);

            // v34
            // added CarveOverride save flag

            // v33
            // added InternalizeTemplates save flag

            // v32
            if (GetFlag(SaveFlags.SetSkill))
                writer.Write(m_SetSkill);

            // v31
            if (GetFlag(SaveFlags.SetProp))
                writer.Write(m_SetProp);

            // v30 - obsolete in version 45
            //if (GetFlag(SaveFlags.LastChange))
            //writer.Write(m_LastChange);

            // v29
            if (GetFlag(SaveFlags.SlotCount))
            {
                writer.Write(GetSlotTableSize());
                foreach (int ix in m_SlotCount)
                    writer.Write(ix);
            }

            // v28
            if (GetFlag(SaveFlags.GraphicID))
            {
                writer.Write(m_GraphicID);
            }

            // v27
            if (GetFlag(SaveFlags.QuestCode))
            {
                writer.Write(m_QuestCode);
                writer.Write(m_QuestCodeChance);
            }

            // v26
            // eliminate v13 m_isConcentric

            // v25
            if (GetFlag(SaveFlags.SpawnFlags))
                writer.Write((int)m_SpawnerFlags);

            // v24 
            if (GetFlag(SaveFlags.Exhibit))
                writer.Write(m_Exhibit);

            // v23
            if (GetFlag(SaveFlags.WalkRangeCalc))
                writer.Write(m_WalkRangeCalc);

            // v22
            if (GetFlag(SaveFlags.Invulnerable))
                writer.Write(m_Invulnerable);

            // v21
            if (GetFlag(SaveFlags.PatchID))
                writer.Write(m_PatchID);

            // v20
            #region OBSOLETE
#if false
            if (GetFlag(SaveFlags.SlotCount))
                for (int ix = 0; ix < GetSlotTableSize(); ix++)
                    writer.Write(GetSlotCount(index: ix));
#endif
            #endregion OBSOLETE
            // v19
            if (GetFlag(SaveFlags.WalkRange))
                writer.Write(m_WalkRange);

            // v18
            if (GetFlag(SaveFlags.SpawnerAttribs))
                writer.Write((UInt32)m_SpawnerAttribs);

            // v17
            if (GetFlag(SaveFlags.Shard))
                writer.Write((int)m_Shard);
            if (GetFlag(SaveFlags.Source))
                writer.Write(m_Source);
            if (GetFlag(SaveFlags.NeedsReview))
                writer.Write(m_NeedsReview);

            // v16
            m_GoldDice.Serialize(writer);

            // v15
            writer.Write((Item)m_CarvePack);
            writer.Write((string)m_CarveMessage);

            // ver 14
            writer.Write(m_CoreSpawn);

            //version 13 - eliminated in version 26
            // writer.Write(m_isConcentric);

            //v12 - obsolete in version 38
            //writer.Write((int)m_OurWorldSize);

            // v11
            writer.Write(m_LootPack);

            // v10
            writer.Write(m_DynamicCopy);
            writer.Write(m_TemplateMobile);
            writer.Write(m_TemplateItem);

            // not used.. just throw away - obsolete in version 38
            //writer.Write((bool)TemplateEnabled);

            if (version <= 21)
                writer.Write((int)0); // 8

            //writer.Write((int)m_NavDest);         // recast as string in v41
            writer.Write((int)m_MobDirection);
            // obsolete in version 38
            //writer.Write(false);                // obsolete: m_FreezeDecay
            writer.Write(m_WayPoint);
            writer.Write(m_Group);
            writer.Write(m_MinDelay);
            writer.Write(m_MaxDelay);
            //writer.Write(m_SlotCount);
            writer.Write(m_Team);
            writer.Write(m_HomeRange);

            // obsolete in version 38
            // writer.Write(Running);

            if (Running)
                writer.WriteDeltaTime(m_End);

            writer.Write(m_ObjectNames.Count);

            for (int i = 0; i < m_ObjectNames.Count; ++i)
                writer.Write((string)m_ObjectNames[i]);

            writer.Write(m_Objects.Count);

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object o = m_Objects[i];

                if (o is Item)
                    writer.Write((Item)o);
                else if (o is Mobile)
                    writer.Write((Mobile)o);
                else
                    writer.Write(Serial.MinusOne);
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_SaveFlags = ReadSaveFlags(reader, version);       // must always follow version

            switch (version)
            {
                case 47:
                    {
                        if (GetFlag(SaveFlags.MorphMode))
                            m_MorphMode = (MorphMode)reader.ReadByte();
                        goto case 46;
                    }
                case 46:
                    {
                        if (GetFlag(SaveFlags.DropsSterling))
                        {
                            m_SterlingMin = reader.ReadUShort();
                            m_SterlingMax = reader.ReadUShort();
                        }
                        goto case 45;
                    }
                case 45:
                    // eliminate m_LastChange - moved to base class
                    goto case 44;
                case 44:
                    {
                        if (GetFlag(SaveFlags.TriggerLinkCreature))
                            m_TriggerLinkCreature = reader.ReadItem();
                        goto case 43;
                    }
                case 43:
                    {
                        if (GetFlag(SaveFlags.MutateEquipment))
                            m_EquipmentDisposition = (EquipmentDisposition)reader.ReadInt();
                        goto case 42;
                    }
                case 42:
                    {
                        if (GetFlag(SaveFlags.LootString))
                            m_LootString = reader.ReadString();

                        if (GetFlag(SaveFlags.LootStringProps))
                            m_LootStringProps = reader.ReadString();

                        goto case 41;
                    }
                case 41:
                    {
                        if (GetFlag(SaveFlags.NavDest))
                            m_NavDest = reader.ReadString();

                        goto case 40;
                    }
                case 40:
                    {
                        if (GetFlag(SaveFlags.Tamable))
                            m_Tamable = (BoolFlags)reader.ReadByte();
                        goto case 39;
                    }
                case 39:
                    {
                        if (GetFlag(SaveFlags.TemplateMobileDefinition))
                            m_TemplateMobileDefinition = reader.ReadInt();
                        goto case 38;
                    }
                case 38:
                    {
                        goto case 37;
                    }
                case 37:
                    {
                        if (GetFlag(SaveFlags.Artifacts))
                        {
                            m_ArtifactPack = reader.ReadItem();
                            m_ArtifactCount = reader.ReadInt();
                        }
                        goto case 36;
                    }

                case 36:
                    {
                        if (GetFlag(SaveFlags.Goodies))
                        {
                            m_GoodiesRadius = reader.ReadInt();
                            m_GoodiesTotalMin = reader.ReadInt();
                            m_GoodiesTotalMax = reader.ReadInt();
                        }
                        goto case 35;
                    }

                case 35:
                    {
                        if (GetFlag(SaveFlags.TriggerLinkPlayers))
                            m_TriggerLinkPlayers = reader.ReadItem();
                        goto case 34;
                    }

                case 34:
                    {
                        m_CarveOverride = GetFlag(SaveFlags.CarveOverride);
                        goto case 33;
                    }

                case 33:
                    {
                        m_TemplateInternalize = GetFlag(SaveFlags.TemplateInternalize);
                        goto case 32;
                    }

                case 32:
                    {
                        if (GetFlag(SaveFlags.SetSkill))
                            m_SetSkill = reader.ReadString();
                        goto case 31;
                    }

                case 31:
                    {
                        if (GetFlag(SaveFlags.SetProp))
                            m_SetProp = reader.ReadString();
                        goto case 30;
                    }

                case 30:
                    {
                        //if (GetFlag(SaveFlags.LastChange))
                        //    m_LastChange = reader.ReadString();
                        if (version < 45)
                            // LastChange == 0x4000
                            if (GetFlag((SaveFlags)0x4000))
                                base.LastChange = reader.ReadString();
                        goto case 29;
                    }

                case 29:
                    {
                        if (GetFlag(SaveFlags.SlotCount))
                        {
                            int count = reader.ReadInt();
                            //m_SlotCount = new(count);
                            for (int ix = 0; ix < count; ix++)
                                SetSlotCount(index: ix, reader.ReadInt());
                        }
                        goto case 28;
                    }
                case 28:
                    {
                        if (GetFlag(SaveFlags.GraphicID))
                            m_GraphicID = reader.ReadInt();
                        goto case 27;
                    }
                case 27:
                    {
                        if (GetFlag(SaveFlags.QuestCode))
                        {
                            m_QuestCode = reader.ReadUInt();
                            m_QuestCodeChance = reader.ReadDouble();
                        }
                        goto case 26;
                    }
                case 26:
                    {
                        // elimination of v13 m_isConcentric
                        goto case 25;
                    }
                case 25:
                    {
                        if (GetFlag(SaveFlags.SpawnFlags))
                            m_SpawnerFlags = (SpawnFlags)reader.ReadInt();
                        goto case 24;
                    }
                case 24:
                    {
                        if (GetFlag(SaveFlags.Exhibit))
                            m_Exhibit = reader.ReadBool();
                        goto case 23;
                    }
                case 23:
                    {
                        if (GetFlag(SaveFlags.WalkRangeCalc))
                            m_WalkRangeCalc = reader.ReadBool();
                        goto case 22;
                    }
                case 22:
                    {
                        if (GetFlag(SaveFlags.Invulnerable))
                            m_Invulnerable = reader.ReadBool();
                        goto case 21;
                    }
                case 21:
                    {
                        if (GetFlag(SaveFlags.PatchID))
                            m_PatchID = reader.ReadUInt();
                        goto case 20;
                    }
                case 20:
                    {
                        if (GetFlag(SaveFlags.SlotCount) && version <= 28)
                        {
                            m_SlotCount = new(6);   // one extra since we will put m_Count at 0
                            for (int ix = 0; ix < 5 /*m_EntryCount.Length*/; ix++)
                                SetSlotCount(index: ix + 1, reader.ReadInt());
                        }
                        goto case 19;
                    }
                case 19:
                    {
                        if (GetFlag(SaveFlags.WalkRange))
                            m_WalkRange = reader.ReadInt();
                        goto case 18;
                    }
                case 18:
                    {
                        if (GetFlag(SaveFlags.SpawnerAttribs))
                            m_SpawnerAttribs = (SpawnerAttribs)reader.ReadUInt();
                        goto case 17;
                    }
                case 17:
                    {
                        if (GetFlag(SaveFlags.Shard))
                            m_Shard = (ShardConfig)reader.ReadInt();
                        if (GetFlag(SaveFlags.Source))
                            m_Source = reader.ReadString();
                        if (GetFlag(SaveFlags.NeedsReview))
                            m_NeedsReview = reader.ReadBool();
                        goto case 16;
                    }
                case 16:
                    {
                        m_GoldDice = new DiceEntry(reader);
                        goto case 15;
                    }
                case 15:
                    {
                        m_CarvePack = reader.ReadItem();
                        m_CarveMessage = reader.ReadString();
                        goto case 14;
                    }
                case 14:
                    {
                        m_CoreSpawn = reader.ReadBool();
                        goto case 13;
                    }
                case 13:
                    {
                        if (version < 26)
                        {
                            bool isConcentric = reader.ReadBool();
                            if (isConcentric)
                                m_SpawnerFlags |= SpawnFlags.Concentric;
                        }
                        goto case 12;
                    }
                case 12:
                    {
                        if (version < 38)
                            /*m_OurWorldSize = (CoreAI.WorldSize)*/
                            reader.ReadInt();
                        goto case 11;
                    }
                case 11:
                    {
                        m_LootPack = reader.ReadItem();
                        goto case 10;
                    }
                case 10:
                    {
                        m_DynamicCopy = reader.ReadBool();
                        goto case 9;
                    }
                case 9:
                    {
                        m_TemplateMobile = reader.ReadMobile();
                        m_TemplateItem = reader.ReadItem();
                        if (version < 38)
                            /*TemplateEnabled =*/
                            reader.ReadBool();

                        goto case 8;
                    }
                case 8:
                    {
                        if (version <= 21)
                            reader.ReadInt();
                        goto case 7;
                    }
                case 7:
                    {
                        if (version < 24)
                        {
                            int dummy = 0;
                            bool b = reader.ReadBool();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                        }
                        goto case 6;
                    }
                case 6:
                    {
                        if (version < 24)
                        {
                            int dummy = 0;
                            string s = reader.ReadString();
                            dummy = reader.ReadInt();
                        }
                        goto case 5;
                    }
                case 5:
                    {   // obsolete in version 41
                        if (version < 41)
                            /*m_NavDest = (NavDestinations)*/
                            reader.ReadInt();
                        goto case 4;
                    }
                case 4:
                    {
                        m_MobDirection = (Direction)reader.ReadInt();
                        goto case 3;
                    }
                case 3:
                    {
                        if (version < 38)
                            // obsolete: m_FreezeDecay
                            reader.ReadBool();
                        goto case 2;
                    }

                case 2:
                    {
                        m_WayPoint = reader.ReadItem() as WayPoint;

                        goto case 1;
                    }

                case 1:
                    {
                        m_Group = reader.ReadBool();

                        goto case 0;
                    }

                case 0:
                    {
                        m_MinDelay = reader.ReadTimeSpan();
                        m_MaxDelay = reader.ReadTimeSpan();
                        if (version <= 28)
                        {
                            int old_count = reader.ReadInt();
                            SetSlotCount(index: 0, old_count);
                        }
                        m_Team = reader.ReadInt();
                        m_HomeRange = reader.ReadInt();

                        if (version < 38)
                            Running = reader.ReadBool();

                        TimeSpan ts = TimeSpan.Zero;

                        if (Running)
                            ts = reader.ReadDeltaTime() - DateTime.UtcNow;

                        int size = reader.ReadInt();

                        m_ObjectNames = new ArrayList(size);

                        for (int i = 0; i < size; ++i)
                        {
                            string typeName = reader.ReadString();
#if true
                            m_ObjectNames.Add(typeName);

                            if (SpawnerType.GetType(typeName) == null)
                            {
                                // this is a bad spawner. It will be picked up elsewhere (OnLoad())
                                // when the GM tries to spawn something we don't have, like "Naturalist" this is the error.
                                //	We track it elsewhere, log it, and can go investigate/delete the spawner
                                ;//debug
                            }
#else
                            if (!string.IsNullOrEmpty(typeName) && SpawnerType.GetType(typeName) != null)
                                m_ObjectNames.Add(typeName);
                            else
                                ;// this is a bad spawner debug point
#endif
                        }

                        int count = reader.ReadInt();

                        m_Objects = new ArrayList(count);

                        for (int i = 0; i < count; ++i)
                        {
                            IEntity e = World.FindEntity(reader.ReadInt());

                            if (e != null)
                                m_Objects.Add(e);
                        }

                        if (Running)
                        {   // NextSpawn is in TS
                            if (ts < TimeSpan.Zero)
                                DoTimer(TimeSpan.Zero);
                            else if (ts > MaxDelay)
                                ResetTimer();
                            else
                                DoTimer(ts);
                        }

                        break;
                    }
            }
        }

        #region Serialization
        public static void Load()
        {
            if (!File.Exists("Saves/Spawner.bin"))
                return;

            Console.WriteLine("Spawner ID Table Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/Spawner.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                int id = reader.ReadInt();
                                if (!Registry.Contains(id))
                                    Registry.Add(id);
                            }

                            break;
                        }

                    default:
                        {
                            throw new Exception("Invalid Spawner.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading Spawner.bin, using default values:");
                Utility.PopColor();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Spawner ID Table Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/Spawner.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(Registry.Count);
                            foreach (var id in Registry)
                                writer.Write(id);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Spawner.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }

    public class EventSpawner : Spawner
    {
        private Event m_event;
        #region Event Properties
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TimerRunning
        {
            get { return m_event.TimerRunning; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EventRunning
        {
            get { return m_event.EventRunning; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventStart
        {
            get { return m_event.EventStart; }
            set
            {
                m_event.EventStart = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventEnd
        {
            get { return m_event.EventEnd; }
            set
            {
                m_event.EventEnd = value;
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public int OffsetFromUTC
        {
            get { return m_event.OffsetFromUTC; }
            set
            {
                m_event.OffsetFromUTC = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Countdown
        {
            get { return m_event.Countdown; }
            set
            {
                m_event.Countdown = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Duration
        {
            get { return m_event.Duration; }
            set
            {
                m_event.Duration = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public bool DurationOverride
        {
            get { return m_event.DurationOverride; }
            set
            {
                m_event.DurationOverride = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Timezone
        {
            get
            { return m_event.Timezone; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EventDebug
        {
            get { return m_event.EventDebug; }
            set
            {
                m_event.EventDebug = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventTimeRemaining
        {
            get { return m_event.EventTimeRemaining; }
        }
        #endregion Event Properties
        #region hidden properties
        // don't make these a command property, we don't want GM's starting/stopping in this way
        public override bool Running
        {
            get { return base.Running; }
            set
            {
                base.Running = value;
                InvalidateProperties();
            }
        }

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                base.Visible = value;
                InvalidateProperties();
            }
        }
        #endregion hidden properties
        [Constructable]
        public EventSpawner()
            : base()
        {
            m_event = new Event(this, null, EventStarted, EventEnded);
            base.Running = false;
        }
        public override void OnSingleClick(Mobile from)
        {
            if (!EventRunning)
            {
                LabelTo(from, "event not running");
                if (Countdown > TimeSpan.Zero)
                    LabelTo(from, string.Format("{0} activating in {1} seconds",
                        this.GetType().Name, string.Format("{0:N2}", this.Countdown.TotalSeconds)));

                LabelTo(from, "(inactive)");
            }
            else
            {
                LabelTo(from, "event running");
                LabelTo(from, "(active)");
            }
        }
        public EventSpawner(Serial serial)
            : base(serial)
        {

        }
        public virtual void EventStarted(object o)
        {
            base.Running = true;
            base.Respawn();
            if (Core.Debug && false)
                Utility.ConsoleWriteLine(string.Format("{0} got 'Event started' event.", this), ConsoleColor.Yellow);
        }
        public virtual void EventEnded(object o)
        {
            base.Running = false;
            if (!GetFlag(SpawnerAttribs.External))
                // when are being 'triggered' externally. we don't cleanup the mobile(s)
                base.RemoveObjects();
            else
                ;// debug
            if (Core.Debug && false)
                Utility.ConsoleWriteLine(string.Format("{0} got 'Event ended' event.", this), ConsoleColor.Yellow);
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(3);   // version
            m_event.Serialize(writer);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_event = new Event(this, null, EventStarted, EventEnded);
                        m_event.Deserialize(reader);
                        break;
                    }
                case 2:
                    {

                        reader.ReadDeltaTime(); // removed m_EventStart in version 3
                        goto case 1;
                    }
                case 1:
                    {
                        reader.ReadDeltaTime(); // removed m_EventEnd in version 3
                        // remove in version 3
                        //m_Timer = new InternalTimer(this, CountDown);
                        //m_Timer.Start();
                        goto case 0;
                    }
                case 0:
                    {
                    }
                    break;
            }
            if (version < 3)
            {
                m_event = new Event(this, null, EventStarted, EventEnded);
                // must kill it immediately
                m_event.Duration = TimeSpan.Zero;
                m_event.Countdown = TimeSpan.Zero;
            }
        }
    }
}