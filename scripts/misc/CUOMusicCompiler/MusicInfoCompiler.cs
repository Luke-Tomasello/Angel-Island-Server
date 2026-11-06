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

/* Purpose:
 * CUO and AI/Sp use a different approach for locating music.
 * It was assumed that client software would only look in Music and or Music/Digital.
 * CUO however recurses all directories under Music looking for music files. This is problematic since original AI had all of Digital, Digitial.new, and Digitial.old directories.
 *  (It's impossible to know which one CUO is going to pick unless we use the same algo to find the music.)
 * So this routine uses CUO's logic for locating usable music files. We then take exactly those files and, with the help of NAudio, compile timing information
 *  for each file. We use this timing (duration) information in the Music Controller to allow tight looping of music not otherwise looped by the client.
 */

/* Scripts\misc\CUOMusicCompiler\MusicInfoCompiler.cs
 * CHANGELOG:
 *  4/29/2024, Adam 
 *      first time checkin. 
 */
