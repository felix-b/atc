SET thisScriptPath=%~dp0
docker run --rm -v "%thisScriptPath%:/src" atcbuild win lin mac
