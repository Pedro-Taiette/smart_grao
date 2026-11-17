@echo off
rem Atalho de dois cliques: roda scripts\stop.ps1 sem esbarrar na politica de execucao do PowerShell.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\stop.ps1"
