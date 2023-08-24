cov-build.exe --dir cov-int build.bat build

# Compress results.
"Compressing Coverity results..."
$zipEncoderDef = @'
    namespace AnalyseCode {
        public class PortableFileNameEncoder: System.Text.UTF8Encoding {
            public PortableFileNameEncoder() {}
            public override byte[] GetBytes(string entry) {
                return base.GetBytes(entry.Replace("\\", "/"));
            }
        }
    }
'@
Add-Type -TypeDefinition $zipEncoderDef
[IO.Compression.ZipFile]::CreateFromDirectory(
    "$env:APPVEYOR_BUILD_FOLDER\cov-int",
    "$env:APPVEYOR_BUILD_FOLDER\$env:APPVEYOR_PROJECT_NAME.zip",
    [IO.Compression.CompressionLevel]::Optimal,
    $true,  # include root directory
    (New-Object AnalyseCode.PortableFileNameEncoder))

# Upload results to Coverity server.
"Uploading Coverity results..."
Add-Type -AssemblyName "System.Net.Http"
$client = New-Object Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromMinutes(20)
$form = New-Object Net.Http.MultipartFormDataContent

# Fill token field.
[Net.Http.HttpContent]$formField =
    New-Object Net.Http.StringContent($env:COVERITY_TOKEN)
$form.Add($formField, '"token"')

# Fill email field.
$formField = New-Object Net.Http.StringContent($env:COVERITY_EMAIL)
$form.Add($formField, '"email"')

# Fill file field.
$fs = New-Object IO.FileStream(
    "$env:APPVEYOR_BUILD_FOLDER\$env:APPVEYOR_PROJECT_NAME.zip",
    [IO.FileMode]::Open,
    [IO.FileAccess]::Read)
$formField = New-Object Net.Http.StreamContent($fs)
$form.Add($formField, '"file"', "$env:APPVEYOR_PROJECT_NAME.zip")

# Fill version field.
$formField = New-Object Net.Http.StringContent($env:APPVEYOR_BUILD_VERSION)
$form.Add($formField, '"version"')

# Fill description field.
$formField = New-Object Net.Http.StringContent("AppVeyor scheduled build.")
$form.Add($formField, '"description"')

# Submit form.
$url = "https://scan.coverity.com/builds?project=$env:APPVEYOR_REPO_NAME"
$task = $client.PostAsync($url, $form)
try {
    $task.Wait()  # throws AggregateException on timeout
} catch [AggregateException] {
    throw $_.Exception.InnerException
}
$task.Result
$fs.Close()