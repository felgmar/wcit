[CMDLetBinding()]
param(
    [Parameter(Mandatory=$false)]
    [String]$OutputPath
)

process {
    [String]$Uri="https://raw.githubusercontent.com/felgmar/isscripts/refs/heads/main/wcit/wcit-setup.iss"
    try {
        Invoke-WebRequest -Uri $Uri -OutFile "$OutputPath" -UseBasicParsing -WarningAction Ignore -ErrorAction Stop
    }
    catch {
        throw $_.Exception.Message
    }
}
