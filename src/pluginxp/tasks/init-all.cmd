@echo off
set repoRootPath=%~dp0..
cd /D "%repoRootPath%"

echo ------ deep-cloning libs ------
cd "%repoRootPath%"
git submodule update --init --recursive

cd "%repoRootPath%"
call tasks\clean-all.cmd || goto :error

cd "%repoRootPath%"
call tasks\build-all.cmd || goto :error

echo ------ init-all: SUCCESS ------
cd "%repoRootPath%"
goto :EOF

:error
echo ------ init-all: FAILED! (exit code %errorlevel%) ------
exit /b %errorlevel%
