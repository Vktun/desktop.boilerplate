param(
    [string]$ConnectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DabpCore;Integrated Security=True;TrustServerCertificate=True;",
    [string]$AdminPassword,
    [switch]$FirstRun
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command sqllocaldb -ErrorAction SilentlyContinue)) {
    throw "sqllocaldb is not installed or not in PATH."
}

sqllocaldb start MSSQLLocalDB | Out-Null

$env:ConnectionStrings__Default = $ConnectionString

if ($FirstRun) {
    if ([string]::IsNullOrWhiteSpace($AdminPassword) -and [string]::IsNullOrWhiteSpace($env:DBP_INITIAL_ADMIN_PASSWORD)) {
        throw "First run requires DBP_INITIAL_ADMIN_PASSWORD. Use -AdminPassword or set the environment variable in this session."
    }
}

if (-not [string]::IsNullOrWhiteSpace($AdminPassword)) {
    $env:DBP_INITIAL_ADMIN_PASSWORD = $AdminPassword
}

dotnet run --project src\Vk.Dbp.WpfWindow\Vk.Dbp.WpfWindow.csproj
