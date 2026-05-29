$exe = "F:\code\ReligionSupport\WeChatPublisher\bin\Release\net8.0-windows\publish\微讯创作发布工具.exe"
$ico = "F:\code\ReligionSupport\WeChatPublisher\Assets\app.ico"
$folder = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\微讯创作发布工具"
New-Item -ItemType Directory -Force -Path $folder | Out-Null
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut("$folder\微讯创作发布工具.lnk")
$sc.TargetPath = $exe
$sc.IconLocation = "$ico,0"
$sc.WorkingDirectory = Split-Path $exe -Parent
$sc.Save()
Write-Host "Shortcut created"
