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
    $xunit = "$package_dir\xunit.runners*\tools\xunit.console.clr4.exe"
    $ilmerge = "$package_dir\ilmerge.*\content\ilmerge.exe"
    $nuget = "$base_dir\.nuget\nuget.exe"
    $sharedAssemblyInfo = "$src_dir\SharedAssemblyInfo.cs"
    $appVeyorConfig = "$base_dir\appveyor.yml"
    $appVeyor = $env:APPVEYOR
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

    $extra = $null
    if ($appVeyor) {
        $extra = "/logger:C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
    }

    Exec { msbuild $solution_path /p:Configuration=$config /nologo /verbosity:minimal $extra }
}

Task Version {
    $newVersion = Read-Host "Please enter a new version number (major.minor.patch)"
    Update-SharedVersion $newVersion
    Update-AppVeyorVersion $newVersion
}

### Functions

function Run-Tests($project) {
    $assembly = Get-Assembly $tests_dir $project $assembly

    if ($appVeyor) {
        Exec { xunit.console.clr4 $assembly /appveyor }
    } else {
        Exec { .$xunit $assembly }
    }
}

function Get-SharedVersion {
    $line = Get-Content "$sharedAssemblyInfo" | where {$_.Contains("AssemblyVersion")}
    $line.Split('"')[1]
}

function Update-SharedVersion($version) {
    Check-Version($version)
    
    $versionPattern = 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
    $versionAssembly = 'AssemblyVersion("' + $version + '")';

    if (Test-Path $sharedAssemblyInfo) {
        "Patching $sharedAssemblyInfo..."
        Replace-Content "$sharedAssemblyInfo" $versionPattern $versionAssembly
    }
}

function Update-AppveyorVersion($version) {
    Check-Version($version)

    $versionPattern = "version: [0-9]+(\.([0-9]+|\*)){1,3}"
    $versionReplace = "version: $version"

    if (Test-Path $appVeyorConfig) {
        "Patching $appVeyorConfig..."
        Replace-Content "$appVeyorConfig" $versionPattern $versionReplace
    }
}

function Check-Version($version) {
    if ($version -notmatch "[0-9]+(\.([0-9]+|\*)){1,3}") {
        Write-Error "Version number incorrect format: $version"
    }
}

function Create-Package($project) {
    $version = Get-SharedVersion
    $buildNumber = $env:APPVEYOR_BUILD_NUMBER

    if ($buildNumber -ne $null) {
        $version += "-build-" + $buildNumber.ToString().PadLeft(5, '0')
    }

    New-Item $temp_dir -Type Directory -Force > $null

    Copy-Item "$nuspec_dir\$project.nuspec" $temp_dir -Force > $null
    Try {
        Replace-Content "$nuspec_dir\$project.nuspec" '0.0.0' $version
        Exec { .$nuget pack "$nuspec_dir\$project.nuspec" -OutputDirectory "$build_dir" -BasePath "$build_dir" -Version $version -Symbols }
    }
    Finally {
        Move-Item "$temp_dir\$project.nuspec" $nuspec_dir -Force > $null
    }
}

function Replace-Content($file, $pattern, $substring) {
    (gc $file) -Replace $pattern, $substring | sc $file
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