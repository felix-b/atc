@echo off
set repoRootPath=%~dp0..
cd /D "%repoRootPath%"

echo ------ cleaning the plugin ------
cd "%repoRootPath%"
rem here we don't git reset --hard!
if EXIST build rmdir /q/s build || goto :error

echo ------ clean: SUCCESS ------
cd "%repoRootPath%"
goto :EOF

:error
echo ------ clean: FAILED! (exit code %errorlevel%) ------
exit /b %errorlevel%
