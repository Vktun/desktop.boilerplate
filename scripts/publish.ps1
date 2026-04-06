# 发布脚本 - Desktop Boilerplate
# 用法: .\scripts\publish.ps1 -Configuration Release -Runtime win-x64

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./publish",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet('win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'osx-x64')]
    [string]$Runtime = "win-x64",
    
    [Parameter(Mandatory=$false)]
    [switch]$SelfContained = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$PublishSingleFile = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$PublishTrimmed = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

# 颜色输出函数
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

# 横幅
Write-ColorOutput @"
╔════════════════════════════════════════════════════════════╗
║          Desktop Boilerplate 发布脚本 v1.0                ║
╚════════════════════════════════════════════════════════════╝
"@ "Cyan"

Write-ColorOutput "发布配置:" "Yellow"
Write-Host "  配置:         $Configuration"
Write-Host "  运行时:       $Runtime"
Write-Host "  输出目录:     $OutputPath"
Write-Host "  自包含:       $SelfContained"
Write-Host "  单文件:       $PublishSingleFile"
Write-Host "  裁剪:         $PublishTrimmed"
if ($Version) {
    Write-Host "  版本:         $Version"
}
Write-Host ""

# 清理旧的发布目录
if (Test-Path $OutputPath) {
    Write-ColorOutput "🗑️  清理旧发布目录..." "Yellow"
    Remove-Item $OutputPath -Recurse -Force
    Start-Sleep -Seconds 1
}

# 项目路径
$projectPath = "src/Vk.Dbp.WpfWindow/Vk.Dbp.WpfWindow.csproj"

if (-not (Test-Path $projectPath)) {
    Write-ColorOutput "❌ 错误: 找不到项目文件 $projectPath" "Red"
    exit 1
}

# 构建发布参数
$publishArgs = @(
    "publish",
    $projectPath,
    "--configuration", $Configuration,
    "--runtime", $Runtime,
    "--output", $OutputPath,
    "/p:PublishSingleFile=$PublishSingleFile",
    "/p:PublishTrimmed=$PublishTrimmed",
    "/p:SelfContained=$SelfContained"
)

if ($SelfContained) {
    $publishArgs += "/p:IncludeNativeLibrariesForSelfExtract=true"
}

if ($Version) {
    $publishArgs += "/p:Version=$Version"
}

# 执行发布
Write-ColorOutput "🚀 正在发布..." "Green"
$startTime = Get-Date

try {
    & dotnet $publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ 发布失败 (退出码: $LASTEXITCODE)" "Red"
        exit 1
    }
}
catch {
    Write-ColorOutput "❌ 发布过程中发生错误: $($_.Exception.Message)" "Red"
    exit 1
}

$endTime = Get-Date
$duration = $endTime - $startTime

# 复制配置文件
Write-ColorOutput "📄 复制配置文件..." "Yellow"
$configFiles = @(
    "src/Vk.Dbp.WpfWindow/appsettings.json",
    "src/Vk.Dbp.WpfWindow/appsettings.local.example.json"
)

foreach ($file in $configFiles) {
    if (Test-Path $file) {
        Copy-Item $file $OutputPath -Force
    }
}

# 创建发布信息文件
$releaseInfo = @{
    BuildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Configuration = $Configuration
    Runtime = $Runtime
    SelfContained = $SelfContained
    PublishSingleFile = $PublishSingleFile
    BuildDuration = $duration.ToString("mm\:ss")
    GitCommit = ""
    GitBranch = ""
}

# 获取Git信息
try {
    $releaseInfo.GitCommit = (git rev-parse --short HEAD 2>$null)
    $releaseInfo.GitBranch = (git rev-parse --abbrev-ref HEAD 2>$null)
}
catch {
    # Git信息不可用，忽略
}

$releaseInfo | ConvertTo-Json | Out-File (Join-Path $OutputPath "release-info.json") -Encoding UTF8

# 统计信息
Write-Host ""
Write-ColorOutput "✅ 发布成功！" "Green"
Write-ColorOutput "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" "Cyan"

$files = Get-ChildItem $OutputPath -Recurse
$totalSize = ($files | Measure-Object -Property Length -Sum).Sum
$fileCount = $files.Count

Write-Host "📊 发布统计:" "Yellow"
Write-Host "  文件数量:     $fileCount"
Write-Host "  总大小:       $([math]::Round($totalSize / 1MB, 2)) MB"
Write-Host "  构建耗时:     $($duration.ToString("mm\:ss"))"
Write-Host "  输出目录:     $((Resolve-Path $OutputPath).Path)"
Write-Host ""

# 显示主要文件
Write-ColorOutput "📦 主要文件:" "Yellow"
$mainFiles = Get-ChildItem $OutputPath -Filter "*.exe" | Select-Object -First 3
foreach ($file in $mainFiles) {
    $size = [math]::Round($file.Length / 1MB, 2)
    Write-Host "  $($file.Name) ($size MB)"
}

Write-Host ""
Write-ColorOutput "🎉 完成！" "Green"
Write-ColorOutput "提示: 请记得配置 appsettings.local.json 后再部署" "Cyan"