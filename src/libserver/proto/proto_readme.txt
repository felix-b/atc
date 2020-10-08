#--- Generate code from .proto
# C++:
# ../../protobuf/install/bin/protoc --cpp_out=. -I. world.proto
# TypeScript:
# ../../protobuf/install/bin/protoc --plugin=../../../tnc-console/electron-app/node_modules/.bin/protoc-gen-ts_proto --ts_proto_out=. -I. world.proto 
# ..\..\protobuf\install\bin\protoc --plugin="protoc-gen-ts_proto=..\..\..\tnc-console\electron-app\node_modules\.bin\protoc-gen-ts_proto.cmd" --ts_proto_out=. -I. world.proto 
# Copy TypeScript:
# cp world.ts ../../../tnc-console/electron-app/src/proto/
# copy world.ts ..\..\..\tnc-console\electron-app\src\proto
