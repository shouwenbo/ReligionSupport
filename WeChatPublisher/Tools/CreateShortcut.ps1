$exe = "f:\code\ReligionSupport\WeChatPublisher\bin\Release\net8.0-windows\publish\公众号视频号发布工具.exe"
$ico = "f:\code\ReligionSupport\WeChatPublisher\Assets\app.ico"
$startMenu = [Environment]::GetFolderPath('StartMenu') + '\Programs\公众号视频号发布工具'
New-Item -ItemType Directory -Force -Path $startMenu | Out-Null

$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$startMenu\公众号视频号发布工具.lnk")
$Shortcut.TargetPath = $exe
$Shortcut.IconLocation = "$ico,0"
$Shortcut.WorkingDirectory = Split-Path $exe -Parent
$Shortcut.Save()

Write-Host "Shortcut created: $startMenu\公众号视频号发布工具.lnk"
Write-Host "You can now find it in Start Menu, right-click -> Pin to Start"
