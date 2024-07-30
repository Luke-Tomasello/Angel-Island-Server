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

/* http://www.runuo.com/forums/general-discussion/66314-body-values.html
 * 0 = invisible  1 = Ogre  2 = Ettin  3 = zombi  4 = gargoyle  5 = eagle
 * 6 = sparrow  7 = Orc Lord  8 = corpser  9 = daemon  10 = sm daemon
 * 11 = dread spider  12 = dragon  13 = Air elemental  14 = Earth Elemental
 * 15 = Fire Elemental  16 = Water Elemental  17 = orc  18 = ettin (club)
 * 19 = Dread spider  20 = Ice Spider  21 = L. Serpent  22 =gazer  23 = wolf
 * 24 = lich  25 = wolf  26 = ghoul  27 = wolf  28 = g. Spider  29 = gorilla
 * 30 = harpy  31 = headless  32 = null  33 = lizardman  34 = wolf
 * 35 = lizardman(spear)  36 = lizardman(mace)  37 = wolf  38 = Balrog Lord
 * 39 = mongbat  40 = Balrog  41 = orc(club)  42 = ratman  43 = Ice Fiend
 * 44 = ratman(club)  45 = ratman(sword)  46 = Ancient Dragon  47 = reaper
 * 48 = g. scorpian  49 = dragon  50 = skeleton  51 = slime  52 = snake
 * 53 = troll(axe)  54 = troll  55 = troll(axe)  56 = skeleton(axe)
 * 57 = skeleton(sword & shield)  58 = sprite  59 = dragon  60 = drake
 * 61 = drake  62 = wyvern  63 = panther  64 = panther  65 = panther
 * 66 = corpser  67 = stone gargoyle  68 = gazer  69 = gazer
 * 70 = terathan warrior  71 = terathan drone  72 = terthan  73 = harpy
 * 74 = imp  75 = cyclops  76 = titan  77 = kraken  78 = lich  79 = lich
 * 80 = g. toad  81 = toad  82 = lich  83 = ogre  84 = ogre
 * 85 = ophidan enforcer  86 = ophidan avenger  87 = ophidan queen
 * 88 = ram  89 = g. serpent  90 = g. serpent  91 = g. serpent
 * 92 = g. serpent  93 = g. serpent  94 = slime  95 = null  96 = slime
 * 97 = wolf  98 = wolf  99 = wolf  100 = wolf  101 = centaur  102 = daemon	
 * 103 = serpent dragon  104 = bone dragon  105 = dragon  106 = dragon
 * 107 = earth elemental  108 = earth elemental  109 = earth elemental
 * 110 = earth elemental  111 = earth elemental  112 = earth elemental
 * 113 = earth elemental  114 = horse  115 = horse  116 = horse  117 = horse	
 * 118 = horse  119 = horse  120 = horse  121 = horse  122 = unicorn
 * 123 = angel  124 = evil mage  125 = evil mage lord  126 = evil mage lord
 * 127 = panther  128 = pixie  129 = corpser  130 = flame gargoyle
 * 131 = air elemental  132 = kirin  133 = alligator  134 = crocodile  135 = ogre
 * 136 = ophidain  137 = ophidian avenger  138 = orc lord  139 = orc Lord
 * 140 = orc  141 = naked man  142 = ratman(sword)  143 = ratman(sword)
 * 144 = horse  145 = sea serpent  146 = Harrower (1st)
 * 147 = skeleton (sword & shield)  148 = skeletal mage  149 = succubus
 * 150 = sea serpent  151 = dolphin  152 = new terathan?  153 = ghast
 * 154 = mummy  155 = zombi  156 = null  157 = black widow
 * 158 = water elemental  159 = water elemental  160 = water elemental
 * 161 = earth elemental  162 = air elemental  163 = air elemental
 * 164 = air elemental  165 = sprite  166 = earth elemental  167 = brown bear
 * 168 = water elemental  169 = g. beetle  170 = llama  171 = ostard
 * 172 = Ancient Dragon  173 = Mephitis  174 = Semidar  175 = Lord Oaks
 * 176 = Silvani  177 = horse  178 = horse  179 = horse  180 = dragon
 * 181 = new orc mage  182 = orc bomber  183 = normal  184 = normal
 * 185 = normal  186 = normal  187 = ridgeback  188 = new ridgeback?
 * 189 = orc brute  190 = horse  191 = kirin  192 = unicorn  193 = ridgeback
 * 194 = swamp dragon 195 = g. beetle  196 = kaze kemono  197 = null
 * 198 = null  199 = rai-ju  200 = horse  201 = cat  202 = alligator  203 = pig
 * 204 = horse  205 = rabbit  206 = crocodile  207 = sheep  208 = chicken
 * 209 = ram  210 = ostard  211 = brown bear  212 = grizzly  213 = polar bear	
 * 214 = panther  215 = g. rat  216 = cow  217 = dog  218 = ostard
 * 219 = ostard  220 = llama  221 = walrus  222 = Lizardman(spear)
 * 223 = sheep  224 = invisible  225 = wolf  226 = horse  227 = invisible
 * 228 = horse  229 = invisible  230 = invisible  231 = cow  232 = bull
 * 233 = bull  234 = deer(buck)  235 = invisible  236 = broke skelly
 * 237 = deer (doe)  238 = rat  239 = invisible  240 =kappa  241 = oni
 * 242 = death beetle  243 = hiryu  244 = rune beetle  245 = Yomotsu War
 * 246 = Bake Kitsune  247 = fan dancer  248 = gaman  249 = Yamandon
 * 250 = tsuki wolf  251 = revenant lion  252 = lady of the snow
 * 253 = Yomotsu priest  254 = crane  255 = yomotsu elder
 * 256 = Chief Paroxysmus  257 = dread horn  258 = Lady M
 * 259 = Monstrous Interred Grizzle  260 = shimmering Effusion
 * 261 = shimmering Effusion  262 = Tormented Minotaur  263 = minotaur
 * 264 = thorn bat  265 = hydra  267 = troglodyte  268 = invisible
 * 269 = broke satyr  270 = broke satyr  271 = satyr  272 = fetid essence
 * 273 = fetid essence  274 = invisible  275 = invisible  276 = chimera
 * 277 = cu sidhe  278 = squirrel  279 = ferret  280 = armored minotaur
 * 281 = minotaur  282 = parrot  283 = crow  284 = mondain�s steed
 * 285 = reaper redux  286 = invisible  287 = invisible  288 = invisible
 * 289 = invisible  290 = pig  291 = pack horse  292 = pack llama
 * 293 = invisible  294 = invisible  295 = dolphin  296 = invisible
 * 297 = invisible  298 = invisible  299 = invisible  300 = crystal elemental
 * 301 = tree fellow  302 = skittering hopper 303 = devourer of souls
 * 304 = flesh golem  305 = gore fiend  306 = impaler  307 = gibberling
 * 308 = bone daemon 309 = patchwork skeleton  310 = wail banshee
 * 311 = shadow knight  312 = abysmal horror  313 = Darknight creeper
 * 314 = ravager  315 = flesh renderer  316 = wanderer of the void
 * 317 = vampire bat 318 = deamon knight  319 = mound of maggots
 * 320 = invisible
 * 
 * 321 and on are broken or invisible.
 * 400 normal
 */
using System;
using System.IO;

namespace Server
{
    public enum BodyType : byte
    {
        Empty,
        Monster,
        Sea,
        Animal,
        Human,
        Equipment
    }

    public struct Body
    {
        private int m_BodyID;

        private static BodyType[] m_Types;

        public static BodyType GetBodyType(int bodyID)
        {
            // we use this in nServer.Movement.MovementObject to determing if the mobile can CanOpenDoors or CanMoveOverObstacles
            //	 we don't care about the other body types now, but they could always be added.

            if (bodyID >= 0
                    && bodyID < m_Types.Length
                    && m_Types[bodyID] == BodyType.Human
                    && bodyID != 402
                    && bodyID != 404
                    && bodyID != 970) return BodyType.Human;

            if (bodyID >= 0
                && bodyID < m_Types.Length
                && m_Types[bodyID] == BodyType.Animal) return BodyType.Animal;

            if (bodyID >= 0
                    && bodyID < m_Types.Length
                    && m_Types[bodyID] == BodyType.Sea) return BodyType.Sea;

            if (bodyID >= 0
                    && bodyID < m_Types.Length
                    && m_Types[bodyID] == BodyType.Monster) return BodyType.Monster;

            return BodyType.Empty;
        }

        static Body()
        {
            if (File.Exists(Path.Combine(Core.DataDirectory, "Binary", "BodyTypes.bin")))
            {
                using (BinaryReader bin = new BinaryReader(new FileStream(Path.Combine(Core.DataDirectory, "Binary", "BodyTypes.bin"), FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    m_Types = new BodyType[(int)bin.BaseStream.Length];

                    for (int i = 0; i < m_Types.Length; ++i)
                        m_Types[i] = (BodyType)bin.ReadByte();
                }
            }
            else
            {
                Console.WriteLine("Warning: {0} does not exist", Path.Combine(Core.DataDirectory, "Binary", "BodyTypes.bin"));

                m_Types = new BodyType[0];
            }
        }

        public Body(int bodyID)
        {
            m_BodyID = bodyID;
        }

        public BodyType Type
        {
            get
            {
                if (m_BodyID >= 0 && m_BodyID < m_Types.Length)
                    return m_Types[m_BodyID];
                else
                    return BodyType.Empty;
            }
        }

        public bool IsHuman
        {
            get
            {
                return m_BodyID >= 0
                    && m_BodyID < m_Types.Length
                    && m_Types[m_BodyID] == BodyType.Human
                    && m_BodyID != 402
                    && m_BodyID != 404
                    && m_BodyID != 970;
            }
        }

        public bool IsMale
        {
            get
            {
                return m_BodyID == 183
                    || m_BodyID == 185
                    || m_BodyID == 400
                    || m_BodyID == 402
                    || m_BodyID == 750;
            }
        }

        public bool IsFemale
        {
            get
            {
                return m_BodyID == 184
                    || m_BodyID == 186
                    || m_BodyID == 401
                    || m_BodyID == 403
                    || m_BodyID == 751;
            }
        }

        public bool IsGhost
        {
            get
            {
                return m_BodyID == 402
                    || m_BodyID == 403
                    || m_BodyID == 970;
            }
        }

        public bool IsMonster
        {
            get
            {
                return m_BodyID >= 0
                    && m_BodyID < m_Types.Length
                    && m_Types[m_BodyID] == BodyType.Monster;
            }
        }

        public bool IsAnimal
        {
            get
            {
                return m_BodyID >= 0
                    && m_BodyID < m_Types.Length
                    && m_Types[m_BodyID] == BodyType.Animal;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return m_BodyID >= 0
                    && m_BodyID < m_Types.Length
                    && m_Types[m_BodyID] == BodyType.Empty;
            }
        }

        public bool IsSea
        {
            get
            {
                return m_BodyID >= 0
                    && m_BodyID < m_Types.Length
                    && m_Types[m_BodyID] == BodyType.Sea;
            }
        }

        public bool IsEquipment
        {
            get
            {
                return m_BodyID >= 0
                    && m_BodyID < m_Types.Length
                    && m_Types[m_BodyID] == BodyType.Equipment;
            }
        }

        public int BodyID
        {
            get
            {
                return m_BodyID;
            }
        }

        public static implicit operator int(Body a)
        {
            return a.m_BodyID;
        }

        public static implicit operator Body(int a)
        {
            return new Body(a);
        }

        public override string ToString()
        {
            return string.Format("0x{0:X}", m_BodyID);
        }

        public override int GetHashCode()
        {
            return m_BodyID;
        }

        public override bool Equals(object o)
        {
            if (o == null || !(o is Body)) return false;

            return ((Body)o).m_BodyID == m_BodyID;
        }

        public static bool operator ==(Body l, Body r)
        {
            return l.m_BodyID == r.m_BodyID;
        }

        public static bool operator !=(Body l, Body r)
        {
            return l.m_BodyID != r.m_BodyID;
        }

        public static bool operator >(Body l, Body r)
        {
            return l.m_BodyID > r.m_BodyID;
        }

        public static bool operator >=(Body l, Body r)
        {
            return l.m_BodyID >= r.m_BodyID;
        }

        public static bool operator <(Body l, Body r)
        {
            return l.m_BodyID < r.m_BodyID;
        }

        public static bool operator <=(Body l, Body r)
        {
            return l.m_BodyID <= r.m_BodyID;
        }
    }
}