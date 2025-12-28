@echo off
chcp 65001 >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& {[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & '%~dp0发布脚本.ps1'}"
pause
