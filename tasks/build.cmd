@echo off
set repoRootPath=%~dp0..
cd /D "%repoRootPath%"

echo ------ building the plugin ------
cd "%repoRootPath%"
mkdir build 
cd build || goto :error
cmake .. -G "MinGW Makefiles" || goto :error
make || goto :error

echo ------ build: SUCCESS ------
cd "%repoRootPath%"
goto :EOF

:error
echo ------ build: FAILED! (exit code %errorlevel%) ------
exit /b %errorlevel%
