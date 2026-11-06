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

/* Scripts\Engines\Spawner\Types\SpawnerType.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using System;

namespace Server.Mobiles
{
    // NOTE ON TYPE RESOLUTION:
    //
    // ScriptCompiler.FindTypeByName() historically resolved types by simple name
    // across the entire codebase. This caused problems once other systems introduced
    // classes with the same names (eg. "Pirate" in Server.Engines.Alignment), which
    // could result in spawners resolving the wrong type.
    //
    // ScriptCompiler.FindTypeByName() has since been hardened to prefer non-Alignment
    // types when collisions occur. However, spawners operate in a strictly limited
    // domain (Mobiles, Items, Multis), so we continue to resolve types explicitly
    // using fully-qualified names.
    //
    // This approach provides:
    //   - deterministic behavior
    //   - immunity to namespace collisions
    //   - faster resolution than the global type cache
    //
    // Tokens are resolved in the following order:
    //   - Server.Mobiles.<token>
    //   - Server.Items.<token>
    //   - Server.Multis.<token>
    //   - Server.Engines.CrownSterlingSystem.<token>
    //
    // This keeps spawner behavior stable as new systems and namespaces are added.
    public class SpawnerType
    {
        public static Type? GetType(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // Fully qualified name
            if (name.IndexOf('.') >= 0)
                return Type.GetType(name, throwOnError: false, ignoreCase: true);

            // Prefer Mobiles
            Type? t = Type.GetType("Server.Mobiles." + name, throwOnError: false, ignoreCase: true);
            if (t != null)
                return t;

            // interestingly, 'hag' picks up both Grizelda and Blackheart
            t = Type.GetType("Server.Engines.Quests.Hag." + name, throwOnError: false, ignoreCase: true);
            if (t != null)
                return t;

            // Then Items
            t = Type.GetType("Server.Items." + name, throwOnError: false, ignoreCase: true);
            if (t != null)
                return t;

            // Then Multis
            t = Type.GetType("Server.Multis." + name, throwOnError: false, ignoreCase: true);
            if (t != null)
                return t;

            //finally, Server.Engines.CrownSterlingSystem
            t = Type.GetType("Server.Engines.CrownSterlingSystem." + name, throwOnError: false, ignoreCase: true);
            if (t != null)
                return t;

            return null;
        }

    }
}

