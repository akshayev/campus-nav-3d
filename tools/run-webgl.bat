@echo off
REM Starts the local server and opens the game in the default browser.
cd /d "%~dp0"
start "" http://localhost:8080
node serve-webgl.js
