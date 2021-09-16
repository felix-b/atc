@echo off
set repoRootPath=%~dp0..
cd /D "%repoRootPath%"

echo ------ building googletest ------
cd "%repoRootPath%\libs\googletest"
mkdir build 
cd build || goto :error
cmake .. -G "MinGW Makefiles" || goto :error
make || goto :error

echo ------ building PPL ------
cd "%repoRootPath%\libs\PPL"
git apply ..\..\tools\patches\ppl-0001-adapt-cmakelists.patch
mkdir include\glew\include
xcopy ..\..\tools\patches\glew\include include\glew\include\ /s /e /y
mkdir build 
cd build || goto :error
cmake .. -G "MinGW Makefiles" || goto :error
make || goto :error

rem echo ------ building openal ------
rem cd "%repoRootPath%\libs\PPL\include\openal-soft"
rem mkdir build 
rem cd build || goto :error
rem cmake .. -G "MinGW Makefiles" || goto :error
rem make || goto :error

echo ------ building XPMP2 ------
cd "%repoRootPath%\libs\XPMP2"
git apply ..\..\tools\patches\xpmp2-0002-dont-include-mingwstdthreads.patch
mkdir build 
cd build || goto :error
cmake .. -G "MinGW Makefiles" || goto :error
make || goto :error

rem echo ------ building libspeechwin ------
rem cd "%repoRootPath%\libs\libspeechwin"
rem mkdir build 
rem mkdir "%repoRootPath%\build\lib"
rem cd build || goto :error
rem cmake .. || goto :error
rem msbuild ALL_BUILD.vcxproj /property:Configuration=Release || goto :error

echo ------ building the plugin ------
cd "%repoRootPath%"
mkdir build 
cd build || goto :error
cmake .. -G "MinGW Makefiles" || goto :error
make || goto :error
make test || goto :error

echo ------ build-all: SUCCESS ------
cd "%repoRootPath%"
goto :EOF

:error
echo ------ build-all: FAILED! (exit code %errorlevel%) ------
exit /b %errorlevel%
