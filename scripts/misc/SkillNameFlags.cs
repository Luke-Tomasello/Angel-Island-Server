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

/* Misc/SkillNameFlags.cs
 * CHANGELOG:
 *  10/28/21, Yoar
 *		Initial version.
 */

using System;

namespace Server
{
    [Flags]
    public enum SkillNameFlags : ulong
    {
        _None = 0x0000000000000000,
        _Mask = 0xFFFFFFFFFFFFFFFF,

        _Misc =
            ArmsLore | Begging | Camping | Cartography | Forensics | ItemID |
            TasteID,
        _Combat =
            Anatomy | Archery | Fencing | Healing | Macing | Parry |
            Swords | Tactics | Wrestling,
        _Trade =
            Alchemy | Blacksmith | Fletching | Carpentry | Cooking | Inscribe |
            Lumberjacking | Mining | Tailoring | Tinkering,
        _Magic =
            EvalInt | Magery | Meditation | Necromancy | MagicResist | SpiritSpeak,
        _Wilderness =
            AnimalLore | AnimalTaming | Fishing | Herding | Tracking | Veterinary,
        _Thieving =
            DetectHidden | Hiding | Lockpicking | Poisoning | RemoveTrap | Snooping |
            Stealing | Stealth,
        _Bard = Discordance | Musicianship | Peacemaking | Provocation,

        Alchemy = 0x0000000000000001,
        Anatomy = 0x0000000000000002,
        AnimalLore = 0x0000000000000004,
        ItemID = 0x0000000000000008,
        ArmsLore = 0x0000000000000010,
        Parry = 0x0000000000000020,
        Begging = 0x0000000000000040,
        Blacksmith = 0x0000000000000080,
        Fletching = 0x0000000000000100,
        Peacemaking = 0x0000000000000200,
        Camping = 0x0000000000000400,
        Carpentry = 0x0000000000000800,
        Cartography = 0x0000000000001000,
        Cooking = 0x0000000000002000,
        DetectHidden = 0x0000000000004000,
        Discordance = 0x0000000000008000,
        EvalInt = 0x0000000000010000,
        Healing = 0x0000000000020000,
        Fishing = 0x0000000000040000,
        Forensics = 0x0000000000080000,
        Herding = 0x0000000000100000,
        Hiding = 0x0000000000200000,
        Provocation = 0x0000000000400000,
        Inscribe = 0x0000000000800000,
        Lockpicking = 0x0000000001000000,
        Magery = 0x0000000002000000,
        MagicResist = 0x0000000004000000,
        Tactics = 0x0000000008000000,
        Snooping = 0x0000000010000000,
        Musicianship = 0x0000000020000000,
        Poisoning = 0x0000000040000000,
        Archery = 0x0000000080000000,
        SpiritSpeak = 0x0000000100000000,
        Stealing = 0x0000000200000000,
        Tailoring = 0x0000000400000000,
        AnimalTaming = 0x0000000800000000,
        TasteID = 0x0000001000000000,
        Tinkering = 0x0000002000000000,
        Tracking = 0x0000004000000000,
        Veterinary = 0x0000008000000000,
        Swords = 0x0000010000000000,
        Macing = 0x0000020000000000,
        Fencing = 0x0000040000000000,
        Wrestling = 0x0000080000000000,
        Lumberjacking = 0x0000100000000000,
        Mining = 0x0000200000000000,
        Meditation = 0x0000400000000000,
        Stealth = 0x0000800000000000,
        RemoveTrap = 0x0001000000000000,
        Necromancy = 0x0002000000000000,
        //Focus         = 0x0004000000000000,
        //Chivalry      = 0x0008000000000000,
        //Bushido       = 0x0010000000000000,
        //Ninjitsu      = 0x0020000000000000,
        //Spellweaving  = 0x0040000000000000,
    }

    public static class SkillNameFlagsHelper
    {
        public static SkillNameFlags GetFlag(this SkillName skill)
        {
            return (SkillNameFlags)((ulong)1 << (int)skill);
        }

        public static SkillName GetSkill(this SkillNameFlags flag)
        {
            ulong mask = (uint)flag;

            for (int i = 0; i < 64 && mask != 0; i++, mask >>= 1)
            {
                if ((mask & 0x1) != 0)
                    return (SkillName)i;
            }

            return (SkillName)0;
        }

        public static SkillName[] GetSkills(this SkillNameFlags flag)
        {
            ulong mask = (uint)flag;
            int count = 0;

            for (int i = 0; i < 64 && mask != 0x0; i++, mask >>= 1)
            {
                if ((mask & 0x1) != 0)
                    count++;
            }

            SkillName[] skills = new SkillName[count];

            mask = (uint)flag;
            count = 0;

            for (int i = 0; i < 64 && mask != 0x0; i++, mask >>= 1)
            {
                if ((mask & 0x1) != 0)
                    skills[count++] = (SkillName)i;
            }

            return skills;
        }
    }
}