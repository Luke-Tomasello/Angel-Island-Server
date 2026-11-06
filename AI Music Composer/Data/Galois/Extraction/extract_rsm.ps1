# Read-only: extract every RolledUpSheetMusic composition from a snapshot of the world item save.
#  Items.tdb (type names), Items.idx (typeRef, serial, pos, len), Items.bin (item data).
#  Each RSM's data ends with: int version(4) | bool napster | int hash | int owner serial | encoded int price |
#  string title | string composition   (strings: 0/1 prefix byte, encoded length, UTF-8).
#  The tail is decoded backwards from the end of the item and validated (version must be 4).
#  Also lists the other prefixed strings in the item (author is among them) and whether Music.bin references the serial.
param([string]$Snap, [string]$OutJson)
$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @"
using System; using System.IO; using System.Text; using System.Collections.Generic;
public class RsmRec { public int Serial; public int Hash; public int Owner; public int Price; public bool Napster; public string Title; public string Composition; public List<string> Strings = new List<string>(); public string Error; }
public static class RsmReader {
    static bool Var(byte[] b, int p, int end, out int val, out int len) {
        val = 0; len = 0; int shift = 0;
        while (p + len < end && len < 5) { int x = b[p + len]; len++; val |= (x & 0x7F) << shift; shift += 7; if ((x & 0x80) == 0) return true; }
        return false;
    }
    // a prefixed string that ends exactly at 'end'; returns its start or -1
    static int StrEndingAt(byte[] b, int lo, int end, out string s) {
        s = null;
        for (int st = end - 2; st >= lo; st--) {
            if (b[st] != 1) continue;
            int v, l;
            if (!Var(b, st + 1, end, out v, out l)) continue;
            if (st + 1 + l + v == end) { s = Encoding.UTF8.GetString(b, st + 1 + l, v); return st; }
        }
        return -1;
    }
    public static List<RsmRec> Read(string snap, string typeName) {
        var res = new List<RsmRec>();
        var types = new List<string>();
        using (var tdb = new BinaryReader(File.OpenRead(Path.Combine(snap, "Items.tdb")))) { int n = tdb.ReadInt32(); for (int i = 0; i < n; i++) types.Add(tdb.ReadString()); }
        int tref = types.IndexOf(typeName);
        if (tref < 0) throw new Exception("type not found: " + typeName);
        using (var idx = new BinaryReader(File.OpenRead(Path.Combine(snap, "Items.idx"))))
        using (var bin = File.OpenRead(Path.Combine(snap, "Items.bin"))) {
            int count = idx.ReadInt32();
            for (int i = 0; i < count; i++) {
                int t = idx.ReadInt32(), serial = idx.ReadInt32(); long pos = idx.ReadInt64(); int len = idx.ReadInt32();
                if (t != tref) continue;
                byte[] b = new byte[len]; bin.Position = pos; bin.Read(b, 0, len);
                var r = new RsmRec { Serial = serial };
                try {
                    string comp; int cs = StrEndingAt(b, 0, len, out comp);
                    bool nullComp = false;
                    if (cs < 0 && len > 0 && b[len - 1] == 0) { cs = len - 1; nullComp = true; }
                    if (cs < 0) throw new Exception("no composition string at the end");
                    string title; int ts = StrEndingAt(b, 0, cs, out title);
                    bool nullTitle = false;
                    if (ts < 0 && b[cs - 1] == 0) { ts = cs - 1; nullTitle = true; }
                    if (ts < 0) throw new Exception("no title string");
                    bool ok = false;
                    for (int k = 1; k <= 5 && !ok; k++) {
                        int ps = ts - k; int v, l;
                        if (ps < 13 || !Var(b, ps, ts, out v, out l) || l != k) continue;
                        int ver = BitConverter.ToInt32(b, ps - 13);
                        byte nap = b[ps - 9];
                        if (ver != 4 || nap > 1) continue;
                        r.Price = v; r.Owner = BitConverter.ToInt32(b, ps - 4); r.Hash = BitConverter.ToInt32(b, ps - 8); r.Napster = nap == 1;
                        ok = true;
                        // other prefixed printable strings before the RSM fields (author, song name, ...)
                        for (int p = 0; p < ps - 13; p++) {
                            if (b[p] != 1) continue; int sv, sl;
                            if (!Var(b, p + 1, ps - 13, out sv, out sl) || sv < 2 || sv > 60 || p + 1 + sl + sv > ps - 13) continue;
                            string s = Encoding.UTF8.GetString(b, p + 1 + sl, sv); bool pr = true;
                            foreach (char c in s) if (c < 32 || c > 126) { pr = false; break; }
                            if (pr) r.Strings.Add(s);
                        }
                    }
                    if (!ok) throw new Exception("RSM header (version 4) not found before the title");
                    r.Title = nullTitle ? null : title; r.Composition = nullComp ? null : comp;
                } catch (Exception ex) { r.Error = ex.Message; }
                res.Add(r);
            }
        }
        return res;
    }
}
"@

$recs = [RsmReader]::Read($Snap, 'Server.Misc.RolledUpSheetMusic')
$music = [IO.File]::ReadAllBytes((Join-Path $Snap 'Music.bin'))
$inMusic = @{}
for ($i = 0; $i -le $music.Length - 4; $i++) { $inMusic[[BitConverter]::ToInt32($music, $i)] = $true }
$out = foreach ($r in $recs) {
    [pscustomobject]@{ serial = ('0x{0:X8}' -f $r.Serial); inMusicBin = [bool]$inMusic[$r.Serial]; owner = ('0x{0:X8}' -f $r.Owner); hash = $r.Hash; price = $r.Price; napster = $r.Napster
        title = $r.Title; strings = ($r.Strings -join ' | '); error = $r.Error; composition = $r.Composition }
}
$out | ConvertTo-Json -Depth 3 | Set-Content $OutJson -Encoding utf8
"RSM items: {0}; decoded {1}; errors {2}; referenced by Music.bin {3}" -f $recs.Count, @($recs | Where-Object { -not $_.Error }).Count, @($recs | Where-Object { $_.Error }).Count, @($out | Where-Object inMusicBin).Count
