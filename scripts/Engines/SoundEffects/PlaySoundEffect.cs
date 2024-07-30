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

/* scripts\Engines\SoundEffects\PlaySoundEffect.cs
 * Changelog
 *  3/3/23, Adam
 *		Initial creation.
 */
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Server.Engines
{
    public enum SoundEffect
    {
        None = -1,
        Secret = 0,
    }
    public static class PlaySoundEffect
    {
        private static KeyValuePair<string, string[]>[] SoundEffects = new KeyValuePair<string, string[]>[]
        {
            new KeyValuePair<string, string[]>("fsh 0.1 fh 0.1 dh 0.1 gs 0.1 g 0.1 dsh 0.1 gh 0.1 bh",
                new string[]{
                    "chatter false",
                    "instrument lapharp",
                    "prefetch true",
                    "newtimer true",
                    "tempo 0",
                    "concertmode false",
                })
        };
        /// <summary>
        /// Plays a canned sound effect
        /// </summary>
        /// <param name="m"></param>
        /// <param name="id"></param>
        /// <returns>false if the sound effect id was invalid</returns>
        public static bool Play(Mobile m, string soundEffect)
        {
            try
            {
                SoundEffect id = (SoundEffect)Enum.Parse(typeof(SoundEffect), soundEffect, true);
                Play(m, id);
                return true;
            }
            catch
            {
                m.SendMessage("Could not locate SoundEffect {0}", soundEffect);
            }
            return false;
        }
        /// <summary>
        /// Plays a canned sound effect
        /// </summary>
        /// <param name="m"></param>
        /// <param name="id"></param>
        /// <returns>false if the sound effect id was invalid</returns>
        public static bool Play(Mobile m, SoundEffect id)
        {
            try
            {
                if (id >= 0)
                {
                    foreach (var arg in SoundEffects[(int)id].Value)
                        Commands.SystemMusicPlayer.Player(m, arg, IsCommandLine: false, bSilent: true);
                    Commands.SystemMusicPlayer.Player(m, SoundEffects[(int)id].Key);
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }
        private static PlayerMobile GetNearestPlayer(IEntity entity)
        {
            if (entity is PlayerMobile)
                return entity as PlayerMobile;

            SortedDictionary<double, PlayerMobile> list = new();
            if (entity is Item item)
            {
                IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(item.Location, 13);
                foreach (Mobile m in eable)
                {
                    if (m is PlayerMobile pm)
                        list.Add(pm.GetDistanceToSqrt(item), pm);
                }
                eable.Free();
            }

            if (list.Count > 0)
                return list.FirstOrDefault().Value;// closest player
            return null;
        }
        public static bool Play(IEntity ent, SoundEffect id)
        {
            try
            {
                ;
                PlayerMobile pm = GetNearestPlayer(ent);
                if (pm == null || id == SoundEffect.None)
                    return false;

                foreach (var arg in SoundEffects[(int)id].Value)
                    Commands.SystemMusicPlayer.Player(pm, arg, IsCommandLine: false, bSilent: true);
                Commands.SystemMusicPlayer.Player(pm, SoundEffects[(int)id].Key);
                return true;
            }
            catch
            {
            }
            return false;
        }
        public static bool ValidID(string soundEffect)
        {
            try
            {
                SoundEffect id = (SoundEffect)Enum.Parse(typeof(SoundEffect), soundEffect, true);
                return true;
            }
            catch
            {
            }
            return false;
        }
        public static void Initialize()
        {   // too good to limit to any specific shard.
            if (Core.RuleSets.AllServerRules())
            {
                Server.CommandSystem.Register("SoundEffect", AccessLevel.GameMaster, new CommandEventHandler(SoundEffect_OnCommand));
            }
        }

        [Usage("SoundEffect <SoundID>")]
        [Description("Plays a sound effect.")]
        public static void SoundEffect_OnCommand(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
            {
                e.Mobile.SendMessage("SoundEffect <SoundID>");
                return;
            }
            else
            {
                try
                {
                    SoundEffect id = (SoundEffect)Enum.Parse(typeof(SoundEffect), e.ArgString, true);
                    foreach (var arg in SoundEffects[(int)id].Value)
                        Commands.SystemMusicPlayer.Player(e.Mobile, arg, true);
                    Commands.SystemMusicPlayer.Player(e.Mobile, SoundEffects[(int)id].Key, true);
                }
                catch
                {
                    e.Mobile.SendMessage("Could not locate SoundEffect {0}", e.ArgString);
                }
            }
        }
    }

}