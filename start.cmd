@echo off
rem Atalho de dois cliques: roda scripts\start.ps1 sem esbarrar na politica de execucao do PowerShell.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\start.ps1"
