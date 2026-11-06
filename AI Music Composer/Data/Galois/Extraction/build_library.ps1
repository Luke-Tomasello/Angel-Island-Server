# Builds the local Galois reference library from rsm_all.json (read-only extraction of the world save snapshot).
#  library\NN - <title>.rzr : the exact composition text of each Galois library master (written byte-for-byte, no newline added)
#  variants\...             : distinct composition texts found only on player copies (not library masters)
#  index.csv / index.md     : per-song analysis
param([string]$Dir)
$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$all = Get-Content (Join-Path $Dir 'rsm_all.json') -Raw | ConvertFrom-Json
$gal = @($all | Where-Object { $_.strings -match 'Galois' })
$lib = @($gal | Where-Object inMusicBin | Sort-Object title)
$libComps = @{}; foreach ($r in $lib) { $libComps[$r.composition] = $true }

$notes = 'cl','csl','d','ds','e','f','fs','g','gs','a','as','b','c','cs','dh','dsh','eh','fh','fsh','gh','gsh','ah','ash','bh','ch'
function Safe([string]$s) { if (-not $s) { return 'untitled' }; ($s -replace '[\\/:*?"<>|]', '_').Trim() }

function Analyze($r) {
    $text = [string]$r.composition
    $lines = $text -split "`r?`n"
    $dir = @{}
    foreach ($l in $lines) { if ($l -match '^\s*#\s*([A-Za-z]+)\s*:\s*(.*)$') { $dir[$matches[1].ToLower()] = $matches[2].Trim() } }
    $play = @($lines | Where-Object { $_ -match "^\s*say\s+'\[play" })
    $says = @($lines | Where-Object { $_ -match '^\s*say\s' -and $_ -notmatch "\[play" })
    $waits = @($lines | Where-Object { $_ -match '^\s*wait\b' })
    $tok = foreach ($l in $play) { ($l -replace "(?i)^\s*say\s+'\[play", '' -replace "'", '').Trim() -split '\s+' | Where-Object { $_ } }
    $tok = @($tok)
    $nNote = 0; $nPause = 0; $pauseSum = 0.0; $neg = 0; $sel = 0; $sfx = 0; $vol = 0; $cfg = 0; $macro = 0; $pipe = 0; $back2back = 0; $prevNote = $false
    $selDir = @{}
    foreach ($t in $tok) {
        $lt = $t.ToLower(); $isNote = $false
        if ($lt -match '%') { $macro++ }
        elseif ($lt -match '\|') { $pipe++; $isNote = $true }
        elseif ($notes -contains $lt) { $nNote++; $isNote = $true }
        elseif ($lt -match '^\+\d+$') { $sfx++ }
        elseif ($lt -match '^([+-]{1,2})([a-z]{1,3})$' -and $notes -contains $matches[2]) { $sel++; $nNote++; $isNote = $true; $selDir[$matches[1]] = 1 + [int]$selDir[$matches[1]] }
        elseif ($lt -match '^v\d{1,2}$') { $vol++ }
        elseif ($lt -match '^-?\d*\.?\d+$') { $nPause++; $d = [double]$lt; if ($d -lt 0) { $neg++ }; $pauseSum += [math]::Abs($d) }
        elseif ($lt -in 'instrument','tempo','newtimer','prefetch','harp','lapharp','lute','true','false','concertmode','volume') { $cfg++ }
        if ($isNote -and $prevNote) { $back2back++ }
        $prevNote = $isNote
    }
    $newTimer = if ($dir.ContainsKey('newtimer')) { $dir['newtimer'] } else { '(default false)' }
    $prefetch = if ($dir.ContainsKey('prefetch')) { $dir['prefetch'] } else { '(default false)' }
    $tempo = if ($dir.ContainsKey('tempo')) { $dir['tempo'] } else { '(default 100)' }
    $instr = if ($dir.ContainsKey('instrument')) { $dir['instrument'] } else { 'harp (default)' }
    $other = @(); if ($selDir.Count) { $other += 'selectors ' + (($selDir.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)x$($_.Value)" }) -join ' ') }
    $tech = @()
    if ($sel) { $tech += "instrument interleaving ($sel selector notes)" }
    if ($back2back) { $tech += "stacked notes ($back2back note-to-note, no pause)" }
    if ($neg) { $tech += "negative pauses ($neg)" }
    if ($sfx) { $tech += "sound effects ($sfx)" }
    if ($vol) { $tech += "volume ($vol)" }
    if ($macro) { $tech += "macros ($macro)" }
    if ($pipe) { $tech += "note|ms ($pipe)" }
    if ($says.Count) { $tech += "lyrics/text ($($says.Count))" }
    if ($cfg) { $tech += "inline config ($cfg tokens)" }
    [pscustomobject]@{
        title = $r.title; serial = $r.serial; chars = $text.Length; lines = $lines.Count; playLines = $play.Count; waitLines = $waits.Count
        notes = $nNote; pauses = $nPause; pauseSeconds = [math]::Round($pauseSum, 1); instrument = $instr; otherInstruments = ($other -join '; ')
        newTimer = $newTimer; prefetch = $prefetch; tempo = $tempo; techniques = ($tech -join '; ')
        complexity = $nNote + 3 * $sel + 2 * $back2back + 2 * $neg + $sfx
    }
}

$libDir = Join-Path $Dir 'library'; $varDir = Join-Path $Dir 'variants'
New-Item -ItemType Directory -Force $libDir, $varDir | Out-Null
$rows = @(); $i = 0
foreach ($r in $lib) {
    $i++
    $name = '{0:D2} - {1}.rzr' -f $i, (Safe $r.title)
    [IO.File]::WriteAllText((Join-Path $libDir $name), [string]$r.composition, $utf8)
    $a = Analyze $r; $a | Add-Member file $name; $rows += $a
}
$seen = @{}; $v = 0
foreach ($r in $gal | Where-Object { -not $_.inMusicBin }) {
    if ($libComps.ContainsKey($r.composition) -or $seen.ContainsKey($r.composition)) { continue }
    $seen[$r.composition] = $true; $v++
    [IO.File]::WriteAllText((Join-Path $varDir ('{0:D2} - {1} ({2}).rzr' -f $v, (Safe $r.title), $r.serial)), [string]$r.composition, $utf8)
}
$rows | Export-Csv (Join-Path $Dir 'index.csv') -NoTypeInformation -Encoding utf8
# verify byte-exactness
$bad = 0
foreach ($r in $lib) { $f = Get-ChildItem $libDir | Where-Object { $_.Name -like ('* - ' + (Safe $r.title) + '.rzr') } | Select-Object -First 1; if ([IO.File]::ReadAllText($f.FullName, $utf8) -ne [string]$r.composition) { $bad++ } }
"library {0} files, variants {1}, byte-mismatches {2}" -f $rows.Count, $v, $bad
