param($logFile)

"Start analyzing the build log..."

$reader = [System.IO.File]::OpenText($logFile)

Add-Type -AssemblyName System.Core
$h = new-object 'System.Collections.Generic.HashSet[string]'

try {
    for(;;) {
        $line = $reader.ReadLine()
        if ($line -eq $null) { break }
        
        if ($line -match '^[\s]*(?<FileName>(?![\s]*[\d]+\>).+)\((?<Line>[\d]+),(?<Column>[\d]+)\): (?<Severity>warning|error|information) (?<Code>[A-Z0-9]+): (?<Message>.*) \[(?<ProjectDir>.+)\\(?<ProjectName>.+)\.(?<ProjectExt>.+)\]$') {
            $projectFile = $matches.ProjectName + "." + $matches.ProjectExt
            $category = $matches.Severity.substring(0,1).toupper() + $matches.Severity.substring(1).tolower()
            
            $key = $matches.ProjectName + '|' + $matches.FileName + '|' + $matches.Line + '|' + $matches.Column + '|' + $matches.Message
            
            if (-Not ($h.Contains($key))) {
              $h.Add($key)
              
              appveyor AddCompilationMessage `
                -Message $matches.Message `
                -Details $matches.Message `
                -Category $category `
                -FileName $matches.FileName `
                -Line $matches.Line `
                -Column $matches.Column `
                -ProjectName $matches.ProjectName `
                -ProjectFile $projectFile
            }
        }
    }
}
finally {
    $reader.Close()
}