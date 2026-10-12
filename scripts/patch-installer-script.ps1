[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [Parameter(Mandatory = $true)]
    [ValidateSet('AppOutputDir', 'UserName', 'AppLicense', 'RepositoryDir', 'AppVersion', 'SourceDir')]
    [String]$Define,
    [Parameter(Mandatory = $true)]
    [String]$Value
)

process {
    try {
        foreach ($Field in $Define) {
            $ScriptFile = Get-Content -Path $OutputPath -Raw
            if ($Field -eq 'SourceDir') {
                [String]$SourcePattern = 'Source:\s*"\{#RepositoryDir\}\\build\\\*"'
                if ($ScriptFile -notmatch $SourcePattern) {
                    throw 'Could not find the expected Inno Setup file source under RepositoryDir\build.'
                }

                $ScriptFile = $ScriptFile -replace $SourcePattern, 'Source: "{#SourceDir}\*"'
            }

            [String]$Pattern = "(#define\s$Field\s+).*"
            [String]$Replacement = "`$1`"$Value`""
            $Patch = $ScriptFile -replace $Pattern, $Replacement
            Set-Content -Path $OutputPath -Value $Patch -Force
        }
    }
    catch {
        throw $_.Exception.Message
    }
    finally {
        Write-Host "Updated $Define in $OutputPath to '$Value'"
    }
}
