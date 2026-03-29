param(
    [int]$Trials = 40,
    [string]$InputPattern = "input*.txt",
    [int]$Seed = 12345,
    [string]$Deadline = ""
)

$ErrorActionPreference = "Stop"

if (!(Test-Path "Program.cpp")) {
    Write-Error "Program.cpp not found in current directory."
}

$inputs = Get-ChildItem -File -Path $InputPattern | Sort-Object FullName
if ($inputs.Count -eq 0) {
    if (Test-Path "input.txt") {
        $inputs = @(Get-Item "input.txt")
    } else {
        Write-Error "No input files found. (pattern: $InputPattern)"
    }
}

$wslWorkDir = "/tmp/ahc_tune"
wsl bash -lc "rm -rf $wslWorkDir && mkdir -p $wslWorkDir"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to prepare WSL work directory."
}

Get-Content "Program.cpp" -Raw | wsl bash -lc "cat > $wslWorkDir/Program.cpp"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to copy Program.cpp to WSL."
}

$inputNames = @()
foreach ($f in $inputs) {
    $name = $f.Name
    Get-Content $f.FullName -Raw | wsl bash -lc "cat > $wslWorkDir/$name"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to copy input to WSL: $name"
    }
    $inputNames += $name
}

$rng = [System.Random]::new($Seed)

$deadlineDt = $null
if (![string]::IsNullOrWhiteSpace($Deadline)) {
    $today = Get-Date
    $parsed = New-Object DateTime
    if (![DateTime]::TryParseExact($Deadline, "HH:mm", [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::None, [ref]$parsed)) {
        Write-Error "Deadline must be HH:mm format, e.g. 18:40"
    }
    $deadlineDt = Get-Date -Year $today.Year -Month $today.Month -Day $today.Day -Hour $parsed.Hour -Minute $parsed.Minute -Second 0
    if ($deadlineDt -le (Get-Date)) {
        Write-Error "Deadline is already past: $($deadlineDt.ToString('yyyy-MM-dd HH:mm:ss'))"
    }
}

function Sample-LogUniform([double]$minValue, [double]$maxValue, [System.Random]$r) {
    $lnMin = [Math]::Log($minValue)
    $lnMax = [Math]::Log($maxValue)
    $x = $lnMin + ($lnMax - $lnMin) * $r.NextDouble()
    return [Math]::Exp($x)
}

$bestAvgV = [double]::NegativeInfinity
$bestParams = $null

Write-Host "Inputs: $($inputNames -join ', ')"
Write-Host "Trials: $Trials"
if ($deadlineDt -ne $null) {
    Write-Host "Deadline: $($deadlineDt.ToString('yyyy-MM-dd HH:mm:ss'))"
}

$completedTrials = 0

for ($t = 1; $t -le $Trials; $t++) {
    if ($deadlineDt -ne $null -and (Get-Date) -ge $deadlineDt) {
        Write-Host "Reached deadline before trial $t. Stop tuning."
        break
    }

    # 現在値を基準に探索
    # SA_START_TEMP baseline: 2e8
    # SA_LEN_POW baseline: 3.0
    # SA_OROPT_RATE baseline: 70
    $startTemp = [Math]::Round((Sample-LogUniform 5e7 8e8 $rng), 3)
    $lenPow = [Math]::Round((2.0 + 2.5 * $rng.NextDouble()), 3)
    $orOptRate = $rng.Next(50, 86)

    $compileCmd = "cd $wslWorkDir && g++ -std=gnu++17 -O2 Program.cpp -o solver " +
                  "-DSA_START_TEMP=$startTemp -DSA_LEN_POW=$lenPow -DSA_OROPT_RATE=$orOptRate"

    wsl bash -lc $compileCmd
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[$t/$Trials] compile failed"
        continue
    }

    [double]$sumV = 0
    $okCount = 0
    $deadlineHitDuringTrial = $false

    foreach ($name in $inputNames) {
        if ($deadlineDt -ne $null -and (Get-Date) -ge $deadlineDt) {
            $deadlineHitDuringTrial = $true
            break
        }

        $vText = wsl bash -lc "cd $wslWorkDir && ./solver < '$name' > /dev/null 2> run.log && grep '^V=' run.log | tail -n1 | cut -d= -f2"
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($vText)) {
            continue
        }

        [double]$vVal = 0
        if (![double]::TryParse($vText.Trim(), [ref]$vVal)) {
            continue
        }

        $sumV += $vVal
        $okCount++
    }

    if ($deadlineHitDuringTrial) {
        Write-Host "Reached deadline during trial $t. Discard this partial trial and stop."
        break
    }

    if ($okCount -eq 0) {
        Write-Host "[$t/$Trials] run failed"
        continue
    }

    $avgV = $sumV / $okCount
    $completedTrials++
    Write-Host "[$t/$Trials] avgV=$([Math]::Round($avgV, 2)) start=$startTemp lenPow=$lenPow orOpt=$orOptRate"

    if ($avgV -gt $bestAvgV) {
        $bestAvgV = $avgV
        $bestParams = [PSCustomObject]@{
            SA_START_TEMP = $startTemp
            SA_LEN_POW = $lenPow
            SA_OROPT_RATE = $orOptRate
            AVG_V = [Math]::Round($avgV, 2)
        }
    }
}

if ($null -eq $bestParams) {
    Write-Error "No successful trial."
}

$bestParams | Format-List | Out-String | Set-Content best_params.txt

$flags = "-DSA_START_TEMP=$($bestParams.SA_START_TEMP) -DSA_LEN_POW=$($bestParams.SA_LEN_POW) -DSA_OROPT_RATE=$($bestParams.SA_OROPT_RATE)"
"BEST_FLAGS: $flags" | Add-Content best_params.txt

Write-Host "Best AVG_V=$($bestParams.AVG_V)"
Write-Host "Completed Trials: $completedTrials"
Write-Host "Saved: best_params.txt"
Write-Host "BEST_FLAGS: $flags"
