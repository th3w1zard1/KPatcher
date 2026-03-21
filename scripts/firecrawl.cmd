@echo off
REM Proxy for firecrawl.ps1 — loads KPatcher repo .env, then runs the real npm firecrawl.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0firecrawl.ps1" %*
