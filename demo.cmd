@echo off
rem SmartGrao em Docker: dois cliques sobem tudo e abrem o navegador; Enter na janela desliga.
rem Para comecar do zero: demo.cmd reset
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\demo.ps1" %*
