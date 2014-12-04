Properties {
    $base_dir = resolve-path .
    $build_dir = "$base_dir\build"
    $src_dir = "$base_dir\src"
    $tests_dir = "$base_dir\tests"
    $package_dir = "$base_dir\packages"
    $nuspec_dir = "$base_dir\nuspecs"
    $framework_dir =  $env:windir + "\Microsoft.Net\Framework\v4.0.30319"
    $temp_dir = "$build_dir\Temp"
    $solution_path = "$base_dir\$solution"
    $config = "Release"
    $buildNumber = $null
    $xunit = "$package_dir\xunit.runners*\tools\xunit.console.clr4.exe"
    $ilmerge = "$package_dir\ilmerge.*\content\ilmerge.exe"
    $nuget = "$base_dir\.nuget\nuget.exe"
}

### Tasks

Task Clean {
    If (Test-Path $build_dir) {
        "Cleaning up '$build_dir'..."
        Remove-Item "$build_dir\*" -Recurse -Force
    }

    "Cleaning up '$solution'..."
    Exec { msbuild $solution_path /target:Clean /nologo /verbosity:minimal }
}

Task Compile -depends Clean {
    "Compiling '$solution'..."
    Exec { msbuild $solution_path /p:Configuration=$config /nologo /verbosity:minimal }
}

### Functions

function Run-Tests($project) {
    $assembly = Get-Assembly $tests_dir $project $assembly
    Exec { .$xunit $assembly }
}

function Get-SharedVersion {
    $line = Get-Content "$src_dir\SharedAssemblyInfo.cs" | where {$_.Contains("AssemblyVersion")}
    $line.Split('"')[1]
}

function Create-Package($project) {
    $version = Get-SharedVersion

    if ($buildNumber -ne $null) {
        $version += "-build-" + $buildNumber.ToString().PadLeft(5, '0')
    }

    New-Item $temp_dir -Type Directory -Force > $null

    Copy-Item "$nuspec_dir\$project.nuspec" $temp_dir -Force > $null
    Try {
        (gc "$nuspec_dir\$project.nuspec") -Replace '0.0.0', $version | sc "$nuspec_dir\$project.nuspec"
        Exec { .$nuget pack "$nuspec_dir\$project.nuspec" -OutputDirectory "$build_dir" -BasePath "$build_dir" -Version $version -Symbols }
    }
    Finally {
        Move-Item "$temp_dir\$project.nuspec" $nuspec_dir -Force > $null
    }
}

function Collect-Tool($source) {
    "Collecting tool '$source'..."

    $destination = "$build_dir\Tools"

    New-Item $destination -Type Directory -Force > $null
    Copy-Item "$base_dir\$source" $destination -Force > $null
}

function Collect-Content($source) {
    "Collecting content '$source'..."

    $destination = "$build_dir\Content"

    New-Item $destination -Type Directory -Force > $null
    Copy-Item "$base_dir\$source" $destination -Force > $null
}

function Collect-Assembly($project, $version) {
    "Collecting assembly '$project.dll' into '$version'..."

    $source = (Get-SrcOutputDir $project) + "\$project.*"
    $destination = "$build_dir\$version"

    New-Item $destination -Type Directory -Force > $null
    Copy-Item $source $destination -Force > $null
}

function Merge-Assembly($project, $internalizeAssemblies) {
    "Merging '$project' with $internalizeAssemblies..."

    $internalizePaths = @()

    foreach ($assembly in $internalizeAssemblies) {
        $internalizePaths += Get-SrcAssembly $project $assembly
    }

    $primaryAssemblyPath = Get-SrcAssembly $project
    New-Item -Path "$temp_dir" -Type Directory -Force > $null

    Exec { .$ilmerge /targetplatform:"v4,$framework_dir" `
        /out:"$temp_dir\$project.dll" `
        /target:library `
        /internalize `
        $primaryAssemblyPath `
        $internalizePaths `
    }

    Move-Item -Force "$temp_dir\$project.*" (Get-SrcOutputDir $project)
}

function Get-SrcAssembly($project, $assembly) {
    return Get-Assembly $src_dir $project $assembly
}

function Get-SrcOutputDir($project) {
    return Get-OutputDir $src_dir $project
}

function Get-Assembly($dir, $project, $assembly) {
    if (!$assembly) { 
        $assembly = $project 
    }
    return (Get-OutputDir $dir $project) + "\$assembly.dll"
}

function Get-OutputDir($dir, $project) {
    return "$dir\$project\bin\$config"
}