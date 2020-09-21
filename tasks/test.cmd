@echo off
set repoRootPath=%~dp0..
cd /D "%repoRootPath%"

call tasks\build.cmd || goto :error
echo ------ running unit tests ------
cd "%repoRootPath%\build"
call make test || goto :error

echo ------ test: SUCCESS ------
cd "%repoRootPath%"
goto :EOF

:error
echo ------ test: FAILED! (exit code %errorlevel%) ------
exit /b %errorlevel%
