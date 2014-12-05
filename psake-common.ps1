Properties {
    ### Directories
    $base_dir = resolve-path .
    $build_dir = "$base_dir\build"
    $src_dir = "$base_dir\src"
    $tests_dir = "$base_dir\tests"
    $package_dir = "$base_dir\packages"
    $nuspec_dir = "$base_dir\nuspecs"
    $temp_dir = "$build_dir\Temp"
    $framework_dir =  $env:windir + "\Microsoft.Net\Framework\v4.0.30319"

    ### Tools
    $nuget = "$base_dir\.nuget\nuget.exe"
    $ilmerge = "$package_dir\ilmerge.*\content\ilmerge.exe"
    $xunit = "$package_dir\xunit.runners*\tools\xunit.console.clr4.exe"
    $7zip = "$package_dir\7-Zip.CommandLine.*\tools\7za.exe"

    ### AppVeyor-related
    $appVeyorConfig = "$base_dir\appveyor.yml"
    $appVeyor = $env:APPVEYOR

    ### Project information
    $solution_path = "$base_dir\$solution"
    $config = "Release"    
    $sharedAssemblyInfo = "$src_dir\SharedAssemblyInfo.cs"
}

## Tasks

Task Clean -Description "Clean up build and project folders." {
    Clean-Directory $build_dir

    "Cleaning up '$solution'..."
    Exec { msbuild $solution_path /target:Clean /nologo /verbosity:minimal }
}

Task Compile -Depends Clean -Description "Compile all the projects in a solution." {
    "Compiling '$solution'..."

    $extra = $null
    if ($appVeyor) {
        $extra = "/logger:C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
    }

    Exec { msbuild $solution_path /p:Configuration=$config /nologo /verbosity:minimal $extra }
}

Task Version -Description "Patch AssemblyInfo and AppVeyor version files." {
    $newVersion = Read-Host "Please enter a new version number (major.minor.patch)"
    Update-SharedVersion $newVersion
    Update-AppVeyorVersion $newVersion
}

## Functions

### Test functions

function Run-XunitTests($project) {
    $assembly = Get-Assembly $tests_dir $project

    if ($appVeyor) {
        Exec { xunit.console.clr4 $assembly /appveyor }
    } else {
        Exec { .$xunit $assembly }
    }
}

### Merge functions

function Merge-Assembly($project, $internalizeAssemblies) {
    "Merging '$project' with $internalizeAssemblies..."

    $internalizePaths = @()

    foreach ($assembly in $internalizeAssemblies) {
        $internalizePaths += Get-SrcAssembly $project $assembly
    }

    $primaryAssemblyPath = Get-SrcAssembly $project

    Create-Directory $temp_dir
    
    Exec { .$ilmerge /targetplatform:"v4,$framework_dir" `
        /out:"$temp_dir\$project.dll" `
        /target:library `
        /internalize `
        $primaryAssemblyPath `
        $internalizePaths `
    }

    Move-Files "$temp_dir\$project.*" (Get-SrcOutputDir $project)
}

### Collect functions

function Collect-Tool($source) {
    "Collecting tool '$source'..."

    $destination = "$build_dir\Tools"

    Create-Directory $destination
    Copy-Files "$base_dir\$source" $destination
}

function Collect-Content($source) {
    "Collecting content '$source'..."

    $destination = "$build_dir\Content"

    Create-Directory $destination
    Copy-Files "$base_dir\$source" $destination
}

function Collect-Assembly($project, $version) {
    "Collecting assembly '$project.dll' into '$version'..."

    $source = (Get-SrcOutputDir $project) + "\$project.*"
    $destination = "$build_dir\$version"

    Create-Directory $destination
    Copy-Files $source $destination
}

### Pack functions

function Create-Package($project, $version) {
    Create-Directory $temp_dir
    Copy-Files "$nuspec_dir\$project.nuspec" $temp_dir

    Try {
        Replace-Content "$nuspec_dir\$project.nuspec" '0.0.0' $version
        Exec { .$nuget pack "$nuspec_dir\$project.nuspec" -OutputDirectory "$build_dir" -BasePath "$build_dir" -Version $version -Symbols }
    }
    Finally {
        Move-Files "$temp_dir\$project.nuspec" $nuspec_dir
    }
}

### Version functions

function Get-BuildVersion {
    $version = Get-SharedVersion
    $buildNumber = $env:APPVEYOR_BUILD_NUMBER

    if ($env:APPVEYOR_REPO_TAG -ne "True" -And $buildNumber -ne $null) {
        $version += "-build-" + $buildNumber.ToString().PadLeft(5, '0')
    }

    return $version
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

### Archive functions

function Create-Zip($file, $dir){
    if (Test-Path -path $file) { Remove-Item $file }
    Create-Directory $dir
    Exec { & $7zip a -mx -tzip $file $dir\* } 
}

### Common functions

function Create-Directory($dir) {
    New-Item -Path $dir -Type Directory -Force > $null
}

function Clean-Directory($dir) {
    If (Test-Path $dir) {
        "Cleaning up '$dir'..."
        Remove-Item "$dir\*" -Recurse -Force
    }
}

function Remove-Directory($dir) {
    if (Test-Path $dir) {
        "Removing $dir"
        Remove-Item $dir -Recurse -Force
    }
}

function Copy-Files($source, $destination) {
    Copy-Item "$source" $destination -Force > $null
}

function Move-Files($source, $destination) {
    Move-Item "$source" $destination -Force > $null
}

function Replace-Content($file, $pattern, $substring) {
    (gc $file) -Replace $pattern, $substring | sc $file
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