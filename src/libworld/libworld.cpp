// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#include <cstring>
#include "libworld.h"

int libWorldFunc()
{
	return 123;
}

namespace world
{
    IMPLEMENT_ENUM_BITWISE_OP(Aircraft::Category, |)
    IMPLEMENT_ENUM_BITWISE_OP(Aircraft::Category, &)
    IMPLEMENT_ENUM_BITWISE_OP(Aircraft::OperationType, |)
    IMPLEMENT_ENUM_BITWISE_OP(Aircraft::OperationType, &)
    IMPLEMENT_ENUM_BITWISE_OP(Aircraft::LightBits, |)
    IMPLEMENT_ENUM_BITWISE_OP(Aircraft::LightBits, &)
}