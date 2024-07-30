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

/* Scripts/Engines/IOBSystem/IOBAlignEnum.cs
 *	01/14/09, plasma
 *		Added all the new guards and description attributes
 *	11/15/08, plasma
 *		Moved speech event sink into KinSystem
 *		Moved KinFactionGuardTypes enum into here
 *  12/3/07, Pix
 *      Added IOBAlignement.Healer for kin-healers instead of overloading Outcast
 *	7/9/06, Pix
 *		Added outcast 'titles' for paperdoll
 *	6/18/06, Pix
 *		Added 'Outcast' IOBAlignment type.
 *	12/20/04, Pix
 *		Added IOB Ranks, changed the namespace to be just Server instead of Server.Items
 *  11/16/04, Froste
 *      Changed "Undead" to "Council" and added a new Undead for the new Alignment
 *  11/05/04, Pigpen
 *	 IOBAlignEnum.cs created for new IOBSystem
 */
//using Server.Engines.IOBSystem.Attributes;

namespace Server
{

    /*public enum KinFactionActivityTypes
	{
		FriendlyVisitor,
		Visitor,
		FriendlySale,
		Sale,
		GuardPostMaint,
		GuardPostHire,
		GuardDeath,
		FriendlyDeath,
		Death,
		GCChampLevel,
		GCDeath
	}


	/// <summary>
	/// Represents all faction-controlable cities
	/// </summary>
	public enum KinFactionCities
	{
		[KinFactionCity(GuardSlots = 10)]
		Cove,
		[KinFactionCity(GuardSlots = 20)]
		Jhelom,
		[KinFactionCity(GuardSlots = 25)]
		Magincia,
		[KinFactionCity(GuardSlots = 20)]
		Minoc,
		[KinFactionCity(GuardSlots = 20)]
		Moonglow,
		[KinFactionCity(GuardSlots = 25)]
		Nujelm,
		[KinFactionCity(GuardSlots = 20)]
		SkaraBrae,
		[KinFactionCity(GuardSlots = 30)]
		Trinsic,
		[KinFactionCity(GuardSlots = 30)]
		Vesper
	}*/

    public enum IOBAlignment
    {
        None,
        Council,
        Pirate,
        Brigand,
        Orcish,
        Savage,
        Undead,
        Good,       // Britannian Militia 
        OutCast,    // PIX's Note: This is a 'special' type for when a player attacks his own NPC kin
        Healer      // Pix's Note: this is another 'special' type for blue-healers
    }

    public enum IOBRank
    {
        None,
        FirstTier,
        SecondTier
    }

    /*
	/// <summary>
	/// Note these values also reflect the slot cost
	/// </summary>
	public enum KinFactionGuardCostTypes
	{
		LowCost = 1,
		MediumCost,
		HighCost
	}*/

    /*
	/// <summary>
	/// This enum is directly used to populate the GuardPost UI and spawn the mobs from the GuardPost.
	/// Add the new enum as the class name of the guard you wish to spawn, like you would the Spawner.
	/// Optionally add a Description attribute to override what is displayed in the UI.
	/// </summary>
	public enum KinFactionGuardTypes
	{
		[Description("Berserker")]
		FactionBerserker = 1,
		[Description("Death Kight")]
		FactionDeathKnight,
		[Description("Dragoon")]
		FactionDragoon,
		[Description("Henchman")]
		FactionHenchman,
		[Description("Knight")]
		FactionKnight,
		[Description("Mercenary")]
		FactionMercenary,
		[Description("Necromancer")]
		FactionNecromancer,
		[Description("Paladin")]
		FactionPaladin,
		[Description("Ranger")]
		FactionRanger,
		[Description("Sorceress")]
		FactionSorceress,
		[Description("Wizard")]
		FactionWizard
	}*/

    public class IOBRankTitle
    {
        public static string[,] rank = new string[,]
            {
                {"", "", ""},									//None
				{"Council", "Council Member", "Council Elder"},	//Council
				{"Pirate", "Deck Hand", "Pirate Captain"},		//Pirate
				{"Brigand", "Bandit", "Brigand Leader"},		//Brigand
				{"Orc", "Orc Gruntee", "Orc Captain"},			//Orcish
				{"Savage", "Savage", "Savage Shaman"},			//Savage
				{"Undead", "Zombie", "Fiend"},					//Undead
				{"Fighter", "Ranger", "Paladin"},				//Good
				{"Outcast", "Outcast", "Outcast"},				//Outcast
                {"Interferer", "Interferer", "Interferer"}      //Kin-Healer
			};

    }
}