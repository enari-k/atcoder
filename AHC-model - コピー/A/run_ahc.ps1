param(
    [string]$InputFile = "input.txt",
    [string]$OutputFile = "output.txt"
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $InputFile)) {
    Write-Error "Input file not found: $InputFile"
}

if (!(Test-Path "Program.cpp")) {
    Write-Error "Program.cpp not found in current directory."
}

$wslWorkDir = "/tmp/ahc_run"

wsl bash -lc "rm -rf $wslWorkDir && mkdir -p $wslWorkDir"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to prepare WSL work directory."
}

Get-Content "Program.cpp" -Raw | wsl bash -lc "cat > $wslWorkDir/Program.cpp"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to copy Program.cpp to WSL."
}

Get-Content $InputFile -Raw | wsl bash -lc "cat > $wslWorkDir/input.txt"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to copy input file to WSL."
}

wsl bash -lc "cd $wslWorkDir && g++ -std=gnu++17 -O2 Program.cpp -o run && ./run < input.txt > output.txt"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build or execution failed in WSL."
}

wsl bash -lc "cat $wslWorkDir/output.txt" | Set-Content $OutputFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to copy output from WSL."
}

$lineCount = (Get-Content $OutputFile | Measure-Object -Line).Lines
Write-Host "Done: $OutputFile ($lineCount lines)"
