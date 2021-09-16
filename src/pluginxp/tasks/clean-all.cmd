@echo off
set repoRootPath=%~dp0..
cd /D "%repoRootPath%"

echo ------ cleaning googletest ------
cd "%repoRootPath%\libs\googletest"
git reset --hard || goto :error
if EXIST build rmdir /q/s build || goto :error

echo ------ cleaning PPL ------
cd "%repoRootPath%\libs\PPL"
git reset --hard || goto :error
if EXIST build rmdir /q/s build || goto :error

rem echo ------ cleaning openal ------
rem cd "%repoRootPath%\libs\PPL\include\openal-soft"
rem git reset --hard || goto :error
rem if EXIST build rmdir /q/s build || goto :error

echo ------ cleaning XPMP2 ------
cd "%repoRootPath%\libs\XPMP2"
git reset --hard || goto :error
if EXIST build rmdir /q/s build || goto :error

rem echo ------ cleaning libspeechwin ------
rem cd "%repoRootPath%\libs\libspeechwin"
rem git reset --hard || goto :error
rem if EXIST build rmdir /q/s build || goto :error

echo ------ cleaning the plugin ------
cd "%repoRootPath%"
rem here we don't git reset --hard!
if EXIST build rmdir /q/s build || goto :error

echo ------ clean-all: SUCCESS ------
cd "%repoRootPath%"
goto :EOF

:error
echo ------ clean-all: FAILED! (exit code %errorlevel%) ------
exit /b %errorlevel%
