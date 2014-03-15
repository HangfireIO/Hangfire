param($logFile)

$reader = [System.IO.File]::OpenText($logFile)
try {
    for(;;) {
        $line = $reader.ReadLine()
        if ($line -eq $null) { break }
        
        if ($line -match '^[\s]*(?<FileName>.+)\((?<Line>[\d]+),(?<Column>[\d]+)\): (?<Severity>.+) (?<Code>[A-Z0-9]+): (?<Message>.*) \[(?<ProjectDir>.+)\\(?<ProjectName>.+)\.(?<ProjectExt>.+)\]$') {
            $projectFile = $matches.ProjectName + "." + $matches.ProjectExt
            
            Add-AppveyorCompilationMessage `
              -Message $matches.Message `
              -Category $matches.Severity `
              -FileName $matches.FileName `
              -Line $matches.Line `
              -Column $matches.Column `
              -ProjectName $matches.ProjectName `
              -ProjectFile $projectFile
        }
    }
}
finally {
    $reader.Close()
}