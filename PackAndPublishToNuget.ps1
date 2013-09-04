[xml]$config = Get-Content Packages.xml

$packages = $config.packages.package
$nugetExe = Resolve-Path .nuget\nuget.exe
    
Remove-Item build\*.nupkg

foreach($package in $packages)
{
	$packageName = $package.name

    Write-Host "Building package $packageName..."
    Set-Location -Path "$packageName"
	
	& $nugetExe pack $packageName.csproj -Build -Symbols -Properties Configuration=Release
	if ($package.languages)
	{
		foreach ($language in $package.languages.language)
		{
			$languageName = $language.name
			
			Write-Host "Building $languageName localization package"
			& $nugetExe pack "Resources\$languageName.nuspec"
		}
	}

    Write-Host "Moving nuget package to the build folder..."
    Move-Item .\*.nupkg ..\build\ -Force

    Set-Location ..
}

$packages = Get-ChildItem build\*.nupkg
foreach($package in $packages)
{
	if ($config.packages.server)
	{
		& $nugetExe push $package -s $config.packages.server $config.packages.apiKey
	}
	else
	{
		& $nugetExe push $package
	}
}

Write-Host "Operation complete!"