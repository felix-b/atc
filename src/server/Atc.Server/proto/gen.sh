#!/bin/bash
cd "${0%/*}"
protogen --csharp_out=cs +langver=9.0 +oneof=enum atc.proto 
