#!/bin/bash

cd "${0%/*}"

# protogen is a global .NET Core tool, it should be installed with the following command:
# dotnet tool install --global protobuf-net.Protogen --version 3.0.101

# generate C# for server
protogen --csharp_out=cs +langver=9.0 +oneof=enum atc.proto 

# generate TypeScript for client
../../../../tools/protoc/lin/protoc --plugin="protoc-gen-ts_proto=../../../console/node_modules/.bin/protoc-gen-ts_proto.cmd" --ts_proto_opt=esModuleInterop=true --ts_proto_out=../../../console/src/proto -I. atc.proto
