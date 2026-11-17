@echo off
rem Atalho de dois cliques: roda scripts\setup.ps1 sem esbarrar na politica de execucao do PowerShell.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\setup.ps1"
