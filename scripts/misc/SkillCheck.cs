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

//

///*
// * Scripts/Misc/SkillCheck.cs
// * CHANGELOG
// *	12/07/06 Taran Kain
// *		Added TC-only code to allow 20x stat gain for controlled basecreatures.
// *	11/20/06 Taran Kain
// *		Made logic play nice with new statmax values (StrMax, IntMax, DexMax). -1 prevents logic from running.
// *  11/18,06, Adam
// *      Add: #pragma warning disable 429, 162
// *      The Unreachable code complaint in this file is acceptable
// *      C:\Program Files\RunUO\Scripts\Misc\SkillCheck.cs(330,50): warning CS0429: Unreachable expression code detected
// *      C:\Program Files\RunUO\Scripts\Misc\SkillCheck.cs(331,5): warning CS0162: Unreachable code detected
// *	2/19/05, erlein
// *		Mobile_SkillCheckLocation(), Added code to record last playermobile's skill
// *		used & time of use for [FindSkill command
// *	1/18/05, mith
// *		CheckSkill(), Added flat factor of .5 to intitial gain chance. This replaces the original total skill cap 
// *			formula, and makes it a little easier to gain skills. Without this factor, skill gain works as if the
// *			player gaining the skill is working it at the skill cap. Once the player hits 80 skill, GSGG takes over
// *			as normal.
// *	1/12/05, mith
// *		CheckSkill(), Commented the formula that set used total skill vs. skill cap to determine chance to gain
// *	7/09/04, Pixie
// *		Fixed code I put in... this was causing people to skip stat gains
// *		in int and dex if their str was at 100!
// *	6/29/04, Pixie
// *		Fixed case where player is at 100 stat and gets a "phantom" gain which
// *		forces lowering of one of the other two stats.
// *	6/23/04 smerX
// *		Eased up on Stat gains. (reduced timer and ease)
// *	6/16/04, Pixie
// *		Added GSGG factor.
// */
//#pragma warning disable 429, 162

//using System;
//using Server;
//using Server.Mobiles;
//using Server.Diagnostics;

//namespace Server.Misc
//{
//    public class SkillCheck
//    {


//        public static void Initialize()
//        {
//            //Mobile.SkillCheckLocationHandler = new SkillCheckLocationHandler( Mobile_SkillCheckLocation );
//            //Mobile.SkillCheckDirectLocationHandler = new SkillCheckDirectLocationHandler( Mobile_SkillCheckDirectLocation );

//            //Mobile.SkillCheckTargetHandler = new SkillCheckTargetHandler( Mobile_SkillCheckTarget );
//            //Mobile.SkillCheckDirectTargetHandler = new SkillCheckDirectTargetHandler( Mobile_SkillCheckDirectTarget );


//            /* Pix - the following lists the str/int/dex gain factors for each skill
//            for( int i=0; i<52; i++ )
//            {
//                Console.WriteLine( "{0}: S:{1} D:{2} I:{3}",
//                     SkillInfo.Table[i].Name, 
//                    SkillInfo.Table[i].StrGain, 
//                    SkillInfo.Table[i].DexGain, 
//                    SkillInfo.Table[i].IntGain);
//            }
//            */
//        }

//        /*public static bool Mobile_SkillCheckLocation( Mobile from, SkillName skillName, double minSkill, double maxSkill )
//        {
//            Skill skill = from.Skills[skillName];

//            if ( skill == null )
//                return false;

//            double value = skill.Value;

//            if(from is PlayerMobile) {				// erl: Record skill being "used"

//                PlayerMobile pm = (PlayerMobile) from;

//                pm.LastSkillUsed = skillName;
//                pm.LastSkillTime = DateTime.UtcNow;
//            }


//            if ( value < minSkill )
//                return false; // Too difficult
//            else if ( value >= maxSkill )
//                return true; // No challenge

//            double chance = (value - minSkill) / (maxSkill - minSkill);

//            Point2D loc = new Point2D( from.Location.X / LocationSize, from.Location.Y / LocationSize );
//            return CheckSkill( from, skill, loc, chance );
//        }

//        public static bool Mobile_SkillCheckDirectLocation( Mobile from, SkillName skillName, double chance )
//        {
//            Skill skill = from.Skills[skillName];

//            if ( skill == null )
//                return false;

//            if ( chance < 0.0 )
//                return false; // Too difficult
//            else if ( chance >= 1.0 )
//                return true; // No challenge

//            Point2D loc = new Point2D( from.Location.X / LocationSize, from.Location.Y / LocationSize );
//            return CheckSkill( from, skill, loc, chance );
//        }*/

//        /*public static bool CheckSkill( Mobile from, Skill skill, object amObj, double chance )
//        {
//            if ( from.Skills.Cap == 0 )
//                return false;

//            bool success = ( chance >= Utility.RandomDouble() );
//            //double gc = (double)(from.Skills.Cap - from.Skills.Total) / from.Skills.Cap;
//            double gc = 0.5;
//            gc += ( skill.Cap - skill.Base ) / skill.Cap;
//            gc /= 2;

//            gc += ( 1.0 - chance ) * ( success ? 0.5 : 0.2 );
//            gc /= 2;

//            gc *= skill.Info.GainFactor;

//            if ( gc < 0.01 )
//                gc = 0.01;

//            if ( from is BaseCreature && ((BaseCreature)from).Controled )
//                gc *= 2;

//            if ( from.Alive && ( ( gc >= Utility.RandomDouble() && AllowGain( from, skill, amObj ) ) || skill.Base < 10.0 ) )
//            {
//                bool bGain = true;
//                if( from is PlayerMobile )
//                {
//                    PlayerMobile pm = (PlayerMobile)from;
//                    DateTime lastgain = pm.LastSkillGainTime[skill.SkillID];

//                    TimeSpan totalDesiredMinimum = TimeSpan.FromHours( GSGG );
//                    TimeSpan minTimeBetweenGains = new TimeSpan(0);

//                    //debug message
//                    //pm.SendMessage("Last gain in this skill was " + lastgain.ToLongTimeString() + " " + lastgain.ToLongDateString() );

//                    if( skill.Base > 80.0 && skill.Base < 90.0 )
//                    {
//                        minTimeBetweenGains = TimeSpan.FromSeconds( (totalDesiredMinimum.TotalSeconds/4)/100 );
//                    }
//                    else if( skill.Base >= 90.0 && skill.Base < 95.0 )
//                    {
//                        minTimeBetweenGains = TimeSpan.FromSeconds( (totalDesiredMinimum.TotalSeconds/4)/50 );
//                    }
//                    else if( skill.Base >= 95.0 && skill.Base < 99.0 )
//                    {
//                        minTimeBetweenGains = TimeSpan.FromSeconds( (totalDesiredMinimum.TotalSeconds/4)/40 );
//                    }
//                    else if( skill.Base >= 99.0 )
//                    {
//                        minTimeBetweenGains = TimeSpan.FromSeconds( (totalDesiredMinimum.TotalSeconds/4)/10 );
//                    }
//                    else //skill is <= 80.0, ignore it
//                    {
//                        minTimeBetweenGains = TimeSpan.FromSeconds(0.1);
//                    }

//                    //debug message
//                    //pm.SendMessage("Minimum time is " + minTimeBetweenGains.TotalSeconds + " seconds");
//                    //pm.SendMessage("time since last gain is " + (DateTime.UtcNow - lastgain) );

//                    if( minTimeBetweenGains > (DateTime.UtcNow - lastgain) )
//                    {
//                        bGain = false;
//                        //debug message
//                        //pm.SendMessage("too close to last gain, not going to gain");
//                    }
//                    else
//                    {
//                        pm.LastSkillGainTime[skill.SkillID] = DateTime.UtcNow;
//                    }
//                }

//                if( bGain )
//                {
//                    Gain( from, skill );
//                }
//            }

//            return success;
//        }*/

//        /*public static bool Mobile_SkillCheckTarget( Mobile from, SkillName skillName, object target, double minSkill, double maxSkill )
//        {
//            Skill skill = from.Skills[skillName];

//            if ( skill == null )
//                return false;

//            double value = skill.Value;

//            if ( value < minSkill )
//                return false; // Too difficult
//            else if ( value >= maxSkill )
//                return true; // No challenge

//            double chance = (value - minSkill) / (maxSkill - minSkill);

//            return CheckSkill( from, skill, target, chance );
//        }

//        public static bool Mobile_SkillCheckDirectTarget( Mobile from, SkillName skillName, object target, double chance )
//        {
//            Skill skill = from.Skills[skillName];

//            if ( skill == null )
//                return false;

//            if ( chance < 0.0 )
//                return false; // Too difficult
//            else if ( chance >= 1.0 )
//                return true; // No challenge

//            return CheckSkill( from, skill, target, chance );
//        }*/

//        //private static bool AllowGain( Mobile from, Skill skill, object obj )
//        //{
//        //    if ( from is PlayerMobile && AntiMacroCode && UseAntiMacro[skill.Info.SkillID] )
//        //        return ((PlayerMobile)from).AntiMacroCheck( skill, obj );
//        //    else
//        //        return true;
//        //}

//        //public enum Stat { Str, Dex, Int }

//        //public static void Gain( Mobile from, Skill skill )
//        //{
//        //    if ( from.Region is Regions.Jail )
//        //        return;

//        //    if ( from is BaseCreature && ((BaseCreature)from).IsDeadPet )
//        //        return;

//        //    if ( skill.SkillName == SkillName.Focus && from is BaseCreature )
//        //        return;

//        //    if ( skill.Base < skill.Cap && skill.Lock == SkillLock.Up )
//        //    {
//        //        int toGain = 1;

//        //        if ( skill.Base <= 10.0 )
//        //            toGain = Utility.Random( 4 ) + 1;

//        //        Skills skills = from.Skills;

//        //        if ( ( skills.Total / skills.Cap ) >= Utility.RandomDouble() )//( skills.Total >= skills.Cap )
//        //        {
//        //            for ( int i = 0; i < skills.Length; ++i )
//        //            {
//        //                Skill toLower = skills[i];

//        //                if ( toLower != skill && toLower.Lock == SkillLock.Down && toLower.BaseFixedPoint >= toGain )
//        //                {
//        //                    toLower.BaseFixedPoint -= toGain;
//        //                    break;
//        //                }
//        //            }
//        //        }

//        //        if ( (skills.Total + toGain) <= skills.Cap )
//        //        {
//        //            skill.BaseFixedPoint += toGain;
//        //        }
//        //    }

//        //    if ( skill.Lock == SkillLock.Up )
//        //    {
//        //        SkillInfo info = skill.Info;

//        //        // TC ONLY!
//        //        if (TestCenter.Enabled && from is BaseCreature && ((BaseCreature)from).Controled)
//        //        {
//        //            if (from.StrLock == StatLockType.Up && info.StrGain > Utility.RandomDouble())
//        //                GainStat(from, Stat.Str);
//        //            else if (from.DexLock == StatLockType.Up && info.DexGain > Utility.RandomDouble())
//        //                GainStat(from, Stat.Dex);
//        //            else if (from.IntLock == StatLockType.Up && info.IntGain > Utility.RandomDouble())
//        //                GainStat(from, Stat.Int);
//        //        }
//        //        else
//        //        {
//        //            if (from.StrLock == StatLockType.Up && (info.StrGain / 20.0) > Utility.RandomDouble())
//        //                GainStat(from, Stat.Str);
//        //            else if (from.DexLock == StatLockType.Up && (info.DexGain / 20.0) > Utility.RandomDouble())
//        //                GainStat(from, Stat.Dex);
//        //            else if (from.IntLock == StatLockType.Up && (info.IntGain / 20.0) > Utility.RandomDouble())
//        //                GainStat(from, Stat.Int);
//        //        }
//        //    }
//        //}

//        //public static bool CanLower( Mobile from, Stat stat )
//        //{
//        //    switch ( stat )
//        //    {
//        //        case Stat.Str: return ( from.StrLock == StatLockType.Down && from.RawStr > 10 && from.StrMax != -1);
//        //        case Stat.Dex: return ( from.DexLock == StatLockType.Down && from.RawDex > 10 && from.DexMax != -1);
//        //        case Stat.Int: return ( from.IntLock == StatLockType.Down && from.RawInt > 10 && from.IntMax != -1);
//        //    }

//        //    return false;
//        //}

//        //public static bool AtCap( Mobile from, Stat stat )
//        //{
//        //    bool bReturn = true;

//        //    switch ( stat )
//        //    {
//        //        case Stat.Str: 
//        //            bReturn = (from.RawStr >= from.StrMax);
//        //            break;
//        //        case Stat.Dex: 
//        //            bReturn = (from.RawDex >= from.DexMax);
//        //            break;
//        //        case Stat.Int: 
//        //            bReturn = (from.RawInt >= from.IntMax);
//        //            break;
//        //    }

//        //    return bReturn;
//        //}

//        //public static bool CanRaise( Mobile from, Stat stat )
//        //{
//        //    if ( !(from is BaseCreature && ((BaseCreature)from).Controled) )
//        //    {
//        //        if ( from.RawStatTotal >= from.StatCap )
//        //            return false;
//        //    }

//        //    switch (stat)
//        //    {
//        //        case Stat.Str: return ( from.StrLock == StatLockType.Up && from.RawStr != -1 && !AtCap(from, stat) );
//        //        case Stat.Dex: return ( from.DexLock == StatLockType.Up && from.RawDex != -1 && !AtCap(from, stat) );
//        //        case Stat.Int: return ( from.IntLock == StatLockType.Up && from.RawInt != -1 && !AtCap(from, stat) );
//        //    }

//        //    return false;
//        //}

//        //public static void IncreaseStat( Mobile from, Stat stat, bool atrophy )
//        //{
//        //    atrophy = atrophy || (from.RawStatTotal >= from.StatCap);

//        //    switch ( stat )
//        //    {
//        //        case Stat.Str:
//        //        {
//        //            if( !AtCap( from, Stat.Str ) ) //if we're at cap, then we can skip this gain
//        //            {
//        //                if ( atrophy )
//        //                {
//        //                    if ( CanLower( from, Stat.Dex ) && (from.RawDex < from.RawInt || !CanLower( from, Stat.Int )) )
//        //                        --from.RawDex;
//        //                    else if ( CanLower( from, Stat.Int ) )
//        //                        --from.RawInt;
//        //                }

//        //                if ( CanRaise( from, Stat.Str ) )
//        //                    ++from.RawStr;
//        //            }

//        //            break;
//        //        }
//        //        case Stat.Dex:
//        //        {
//        //            if( !AtCap( from, Stat.Dex ) ) //if we're at cap, then we can skip this gain
//        //            {
//        //                if ( atrophy )
//        //                {
//        //                    if ( CanLower( from, Stat.Str ) && (from.RawStr < from.RawInt || !CanLower( from, Stat.Int )) )
//        //                        --from.RawStr;
//        //                    else if ( CanLower( from, Stat.Int ) )
//        //                        --from.RawInt;
//        //                }

//        //                if ( CanRaise( from, Stat.Dex ) )
//        //                    ++from.RawDex;
//        //            }

//        //            break;
//        //        }
//        //        case Stat.Int:
//        //        {
//        //            if( !AtCap( from, Stat.Int ) ) //if we're at cap, then we can skip this gain
//        //            {
//        //                if ( atrophy )
//        //                {
//        //                    if ( CanLower( from, Stat.Str ) && (from.RawStr < from.RawDex || !CanLower( from, Stat.Dex )) )
//        //                        --from.RawStr;
//        //                    else if ( CanLower( from, Stat.Dex ) )
//        //                        --from.RawDex;
//        //                }

//        //                if ( CanRaise( from, Stat.Int ) )
//        //                    ++from.RawInt;
//        //            }

//        //            break;
//        //        }
//        //    }
//        //}

//        //private static TimeSpan m_StatGainDelay = TimeSpan.FromMinutes( 2.0 );

//        //public static void GainStat( Mobile from, Stat stat )
//        //{
//        //    if ( (from.LastStatGain + m_StatGainDelay) >= DateTime.UtcNow )
//        //        return;

//        //    from.LastStatGain = DateTime.UtcNow;

//        //    bool atrophy = false;//( (from.RawStatTotal / (double)from.StatCap) >= Utility.RandomDouble() );

//        //    IncreaseStat( from, stat, atrophy );
//        //}
//    }
//}