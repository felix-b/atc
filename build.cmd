SET thisScriptPath=%~dp0
docker run --rm -v "%thisScriptPath%:/src" atcbuild:v2 win lin mac
