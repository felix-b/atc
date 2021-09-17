cd /D "%~dp0"

rem protogen is a global .NET Core tool, it should be installed with the following command:
rem dotnet tool install --global protobuf-net.Protogen --version 3.0.101

rem generate C# for server
protogen --csharp_out=cs +langver=9.0 +oneof=enum atc.proto

rem generate TypeScript for client
..\..\..\..\tools\protoc\win\protoc.exe --plugin="protoc-gen-ts_proto=..\..\..\console\node_modules\.bin\protoc-gen-ts_proto.cmd" --ts_proto_opt=esModuleInterop=true --ts_proto_out=..\..\..\console\src\proto -I. atc.proto

rem generate C++ for plugin
..\..\..\..\tools\protoc\win\protoc.exe --cpp_out=..\..\..\pluginxp\plugin\proto -I. atc.proto
