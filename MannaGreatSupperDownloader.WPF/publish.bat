@echo off
REM -------------------------------
REM 一键发布单文件，依赖 .NET 8 Runtime
REM 输出到项目根目录的 publish 文件夹
REM -------------------------------

echo 正在发布...
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true -o "%~dp0publish"

if %errorlevel% neq 0 (
    echo 发布失败！
    pause
    exit /b %errorlevel%
)

echo 发布完成！输出目录: %~dp0publish
pause
