/***************************************************************************
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

/* Scripts/Misc/DiscordHerald.cs
 * ChangeLog:
 *  11/11/2026, Adam
 *      First version. The Herald's Discord subscriber: events go to Discord channels through webhooks, by
 *      route. Data\discord.json (the same on every shard; in the private SVN, and "never discord.json" in every
 *      GitHub sync list: a webhook URL lets anyone post to its channel):
 *        name       who the posts are from (left out: "<shard> Herald", so one file serves every shard)
 *        webhooks   channel -> webhook URL, or -> { shard name -> URL } for a channel each shard has its own of
 *                   (a shard's General); the same file is copied to every shard and into the Installer.
 *                   Shard names are Core.Server's, with " Test Center" added on Test Center (which runs as
 *                   Upheaval): Angel Island, Siege Perilous, Mortalis, Seasons, Upheaval, Upheaval Test Center,
 *                   Login Server
 *        private    channels only staff can read: the only ones Staff events (exceptions) may go to
 *        routes     interest -> channels ("Conquest": ["general", "main"])
 *      Missing file: a template is written, with a placeholder URL, and nothing is posted.
 *      Rules: a Player event (one player's private message) never leaves the game; a Staff event only goes to
 *      a private channel. Posting is in the background (the game never waits on Discord); Discord's "too many"
 *      (429) is waited out as Discord asks; at most 10 posts a minute per channel, the rest counted and said in
 *      the next post. Mentions are never parsed, so no text can ping @everyone.
 *      [herald status | test <text> | echo on|off | reload (Administrator). echo: every post is also shown in
 *      game to Administrators and up ("[Discord -> staff] ..."), for testing.
 *      [announce <text> (Administrator): a broadcast in game to everyone and an Announcement to Discord (event
 *      starts: "The Pumpkin King is starting now!"). Announce(text) is the same thing for code (cron events).
 *      A development machine (-nosaves: every local launch, never production) posts only to private channels, so
 *      local testing can't reach the players' live channels; with echo on, what would have gone is shown as "not
 *      posted, development machine". [herald status says so.
 */

using Server.Commands;
using Server.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Misc
{
    public sealed class DiscordHerald : IHeraldSubscriber
    {
        // this shard's name for Discord: Test Center runs as Upheaval (Core.Server), so it says so
        public static string ShardName => Core.Server + (Core.GMNTC_CFG ? " Test Center" : "");

        public static string ConfigPath => Path.Combine(Core.DataDirectory, "discord.json");
        private const string Placeholder = "<paste the channel's webhook URL here>";
        private const int PerMinute = 10;
        private const int MaxText = 1900;   // Discord's limit is 2000

        private static DiscordHerald? s_Instance;
        private static readonly HttpClient s_Http = new() { Timeout = TimeSpan.FromSeconds(15) };

        // [herald echo on: posts are also shown in game to Administrators and up
        public static bool Echo { get; private set; }

        private string m_Name = ShardName + " Herald";
        private Dictionary<string, string> m_Webhooks = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> m_Private = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<string>> m_Routes = new(StringComparer.OrdinalIgnoreCase);
        private string m_LoadResult = "";

        private readonly BlockingCollection<(string Channel, string Url, string Text)> m_Outbox = new(200);
        private readonly ConcurrentDictionary<string, string> m_LastResult = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Queue<DateTime>> m_Sent = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> m_Dropped = new(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            s_Instance = new DiscordHerald();
            s_Instance.Load();
            Herald.Subscribe(s_Instance);
            new Thread(s_Instance.Worker) { IsBackground = true, Name = "DiscordHerald" }.Start();

            CommandSystem.Register("Herald", AccessLevel.Administrator, new CommandEventHandler(OnHerald));
            CommandSystem.Register("Announce", AccessLevel.Administrator, new CommandEventHandler(OnAnnounce));
        }

        // a broadcast in game to everyone, and an Announcement to Discord (for code: cron events and the like)
        public static void Announce(string text)
        {
            World.Broadcast(0x482, true, text);
            Herald.Publish(Interest.Announcement, Audience.World, text);
        }

        // ---- config ----

        private void Load()
        {
            try
            {
                bool wroteTemplate = false;
                if (!File.Exists(ConfigPath))
                {
                    WriteTemplate();
                    wroteTemplate = true;
                }

                JsonNode? root = JsonNode.Parse(File.ReadAllText(ConfigPath),
                    documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

                string? name = root?["name"]?.GetValue<string>();
                m_Name = string.IsNullOrWhiteSpace(name) ? ShardName + " Herald" : name!;

                // a channel's webhook is one URL, or one per shard ({ "Upheaval": "https://...", ... }), so the same
                // file serves every shard; a shard without its own entry has no such channel
                Dictionary<string, string> webhooks = new(StringComparer.OrdinalIgnoreCase);
                if (root?["webhooks"] is JsonObject hooks)
                    foreach (var (channel, value) in hooks)
                    {
                        JsonNode? url = value;
                        if (value is JsonObject perShard)
                            url = perShard.FirstOrDefault(kv => kv.Key.Equals(ShardName, StringComparison.OrdinalIgnoreCase)).Value;
                        string u = url is JsonValue v && v.TryGetValue(out string? s) ? s : "";
                        if (u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            webhooks[channel] = u;          // the placeholder (or anything else) is left out
                    }

                HashSet<string> priv = new(StringComparer.OrdinalIgnoreCase);
                if (root?["private"] is JsonArray p)
                    foreach (JsonNode? c in p)
                        if (c != null)
                            priv.Add(c.GetValue<string>());

                Dictionary<string, List<string>> routes = new(StringComparer.OrdinalIgnoreCase);
                if (root?["routes"] is JsonObject r)
                    foreach (var (interest, channels) in r)
                        routes[interest] = channels is JsonArray a ? a.Where(x => x != null).Select(x => x!.GetValue<string>()).ToList() : new();

                m_Webhooks = webhooks;
                m_Private = priv;
                m_Routes = routes;
                m_LoadResult = wroteTemplate
                    ? "no discord.json: a template was written; paste a webhook URL into it, then [herald reload"
                    : string.Format("loaded: {0} channel{1} with a webhook", webhooks.Count, webhooks.Count == 1 ? "" : "s");
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is InvalidOperationException || ex is UnauthorizedAccessException)
            {
                m_LoadResult = "discord.json couldn't be read: " + ex.Message;   // nothing is posted until it can
                m_Webhooks = new(StringComparer.OrdinalIgnoreCase);
            }
            Console.WriteLine("Discord: {0}", m_LoadResult);
        }

        private static void WriteTemplate()
        {
            Directory.CreateDirectory(Core.DataDirectory);
            File.WriteAllText(ConfigPath,
@"{
  // Discord (Scripts/Misc/DiscordHerald.cs). Private: a webhook URL lets anyone post to its channel (never on GitHub).
  // The same file serves every shard: each posts as ""<its name> Herald"" (""name"" here would override that).
  // A channel's webhook: channel settings -> Integrations -> Webhooks -> New Webhook -> Copy Webhook URL.
  // One URL for a shared channel, or one per shard: ""general"": { ""Upheaval"": ""https://..."", ""Mortalis"": ""https://..."" }
  // Shard names: Angel Island, Siege Perilous, Mortalis, Seasons, Upheaval, Upheaval Test Center, Login Server
  ""webhooks"": {
    ""staff"": """ + Placeholder + @"""
  },
  // channels only staff can read: the only ones staff events (exceptions) may go to
  ""private"": [ ""staff"" ],
  // interest -> channels (Exception, Conquest, Announcement, Test)
  ""routes"": {
    ""Exception"": [ ""staff"" ],
    ""Conquest"": [ ""staff"" ],
    ""Announcement"": [ ""staff"" ],
    ""Test"": [ ""staff"" ]
  }
}
");
        }

        // ---- delivery (game thread) ----

        public void Deliver(HeraldEvent e)
        {
            if (e.Audience == Audience.Player)
                return;     // one player's private message never leaves the game
            if (!m_Routes.TryGetValue(e.Interest.ToString(), out List<string>? channels))
                return;

            foreach (string channel in channels)
            {
                if (!m_Webhooks.TryGetValue(channel, out string? url))
                    continue;
                if (e.Audience == Audience.Staff && !m_Private.Contains(channel))
                    continue;   // staff events only to channels only staff can read

                // a development machine (-nosaves) never posts where players read: a test death, conquest or
                //  announcement on a local shard must not reach the live channels (echo still shows what would go)
                if (Core.NoSaves && !m_Private.Contains(channel))
                {
                    if (Echo)
                        CommandHandlers.BroadcastMessage(AccessLevel.Administrator, 0x482,
                            string.Format("[Discord -> {0}: not posted, development machine] {1}", channel, e.Text));
                    continue;
                }

                if (!m_Outbox.TryAdd((channel, url, e.Text)))
                    m_LastResult[channel] = DateTime.Now.ToString("HH:mm:ss") + " outbox full: dropped";

                if (Echo)
                    CommandHandlers.BroadcastMessage(AccessLevel.Administrator, 0x482, string.Format("[Discord -> {0}] {1}", channel, e.Text));
            }
        }

        // ---- posting (background) ----

        private void Worker()
        {
            foreach (var (channel, url, text) in m_Outbox.GetConsumingEnumerable())
            {
                try
                {
                    if (!MayPost(channel))
                        continue;
                    PostAsync(channel, url, text).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    // never LogException here: that would tell the Herald, which would post again
                    m_LastResult[channel] = DateTime.Now.ToString("HH:mm:ss") + " failed: " + ex.Message;
                    Console.WriteLine("Discord: post to {0} failed: {1}", channel, ex.Message);
                }
            }
        }

        // at most PerMinute posts a minute per channel; the rest are counted
        private bool MayPost(string channel)
        {
            if (!m_Sent.TryGetValue(channel, out Queue<DateTime>? sent))
                m_Sent[channel] = sent = new Queue<DateTime>();
            DateTime now = DateTime.UtcNow;
            while (sent.Count > 0 && now - sent.Peek() > TimeSpan.FromMinutes(1))
                sent.Dequeue();
            if (sent.Count >= PerMinute)
            {
                m_Dropped[channel] = m_Dropped.GetValueOrDefault(channel) + 1;
                return false;
            }
            sent.Enqueue(now);
            return true;
        }

        private async Task PostAsync(string channel, string url, string text)
        {
            int dropped = m_Dropped.GetValueOrDefault(channel);
            if (dropped > 0)
            {
                text = string.Format("({0} more not posted: over {1} a minute)\n{2}", dropped, PerMinute, text);
                m_Dropped[channel] = 0;
            }
            if (text.Length > MaxText)
                text = text.Substring(0, MaxText) + "...";

            JsonObject body = new()
            {
                ["content"] = text,
                ["username"] = m_Name,
                ["allowed_mentions"] = new JsonObject { ["parse"] = new JsonArray() },  // no @everyone, ever
            };

            for (int attempt = 0; attempt < 2; ++attempt)
            {
                using StringContent content = new(body.ToJsonString(), Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await s_Http.PostAsync(url, content);
                if ((int)response.StatusCode == 429 && attempt == 0)
                {
                    double wait = 2.0;
                    try
                    {
                        JsonNode? r = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                        wait = r?["retry_after"]?.GetValue<double>() ?? wait;
                    }
                    catch (JsonException) { }
                    await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(wait, 0.5, 60)));
                    continue;
                }
                m_LastResult[channel] = string.Format("{0} {1} {2}", DateTime.Now.ToString("HH:mm:ss"), (int)response.StatusCode,
                    response.IsSuccessStatusCode ? "posted" : response.ReasonPhrase);
                return;
            }
        }

        // ---- commands ----

        [Usage("Herald status | test <text> | echo on|off | reload")]
        [Description("Discord through the Herald: what's configured, a test post, showing posts in game, rereading discord.json.")]
        private static void OnHerald(CommandEventArgs e)
        {
            DiscordHerald? h = s_Instance;
            if (h == null)
                return;
            string verb = e.Length > 0 ? e.GetString(0).ToLowerInvariant() : "status";

            switch (verb)
            {
                case "test":
                {
                    string text = e.ArgString.Length > 4 ? e.ArgString.Substring(4).Trim() : "";
                    if (text.Length == 0)
                        text = string.Format("Test from {0} ({1}).", e.Mobile.Name, ShardName);
                    Herald.Publish(Interest.Test, Audience.World, text);
                    e.Mobile.SendMessage("Herald: test sent: {0}", text);
                    break;
                }
                case "echo":
                    if (e.Length > 1)
                        Echo = e.GetString(1).Equals("on", StringComparison.OrdinalIgnoreCase);
                    e.Mobile.SendMessage("Herald: echo {0}.", Echo ? "on (Discord posts are shown to Administrators and up)" : "off");
                    break;
                case "reload":
                    h.Load();
                    e.Mobile.SendMessage("Herald: {0}", h.m_LoadResult);
                    break;
                default:
                    e.Mobile.SendMessage("Herald: {0} ({1})", h.m_LoadResult, ConfigPath);
                    e.Mobile.SendMessage("Herald: posts as '{0}'; echo {1}", h.m_Name, Echo ? "on" : "off");
                    if (Core.NoSaves)
                        e.Mobile.SendMessage("Herald: development machine (-nosaves): private channels only; public ones are never posted to");
                    foreach (string channel in h.m_Webhooks.Keys)
                        e.Mobile.SendMessage("Herald: channel {0}{1}: last {2}", channel, h.m_Private.Contains(channel) ? " (private)" : "",
                            h.m_LastResult.TryGetValue(channel, out string? last) ? last : "nothing posted yet");
                    foreach (var (interest, channels) in h.m_Routes)
                        e.Mobile.SendMessage("Herald: {0} -> {1}", interest, channels.Count == 0 ? "nowhere" : string.Join(", ", channels));
                    break;
            }
        }

        [Usage("Announce <text>")]
        [Description("Broadcasts <text> in game to everyone and posts it to Discord (an Announcement).")]
        private static void OnAnnounce(CommandEventArgs e)
        {
            string text = e.ArgString.Trim();
            if (text.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [announce <text>");
                return;
            }
            Announce(text);
        }
    }
}
