SET thisScriptPath=%~dp0
CD "%thisScriptPath%\build"
cmake .. -G "MinGW Makefiles"
make
