param(
    [Parameter(Mandatory=$true)]
    [string]$Path
)

$supportedExtensions = @('.md', '.sh', '.yml', '.yaml', '.json', '.cs', '.csproj', '.slnx', '.json', '.razor', '.props', '.ps1', '.sh', '.tf', '.sql', '.proto', '.txt', '.tt', '.nuspec', '.js', '.sql'. '.cshtml', '.bat', '.xml', '.manifest', '.css')

# Resolve the input path
$item = Get-Item -LiteralPath $Path -ErrorAction Stop

# Collect files to process
if ($item.PSIsContainer) {
    # It's a directory → recurse
    $files = Get-ChildItem -Path $item.FullName -Recurse -File | Where-Object {
        $supportedExtensions -contains $_.Extension.ToLower() -and $_.FullName -notmatch '(?i)[\\/](node_modules|obj|bin|\.git|graphify-out|\.idea|\.vs)[\\/]'
    }
}
else {
    # It's a file → check extension
    if ($supportedExtensions -notcontains $item.Extension.ToLower()) {
        Write-Error "File '$Path' does not have a supported extension."
        exit 1
    }
    $files = $item
}

# Process each file
foreach ($file in $files) {
    $content = Get-Content -Raw -Path $file.FullName
    if ($content -match "\r\n") {
        $normalized = $content -replace "\r\n", "`n"
        [System.IO.File]::WriteAllText($file.FullName, $normalized)
        Write-Host "Normalized CRLF → LF: $($file.FullName)"
    }
    else {
        Write-Host "Already uses LF or no line endings to change: $($file.FullName)"
    }
}