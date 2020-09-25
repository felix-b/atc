// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/COPYING
// 
#pragma once

#include <string>
#include <sstream>
#include <vector>
#include <unordered_map>
#include <unordered_set>
#include <algorithm>
#include <functional>
#include <time.h>

#ifdef WIN32
    #define timegm _mkgmtime
#endif

using namespace std;

#define DECLARE_ENUM_BITWISE_OP(Enum, OP)     \
    Enum operator OP(Enum lhs, Enum rhs);     \

#define IMPLEMENT_ENUM_BITWISE_OP(Enum, OP)   \
    Enum operator OP(Enum lhs, Enum rhs)      \
    {                                         \
        static_assert(std::is_enum<Enum>::value,                    \
            "Type must be an enum");                                \
        using underlying = typename underlying_type<Enum>::type;    \
        return static_cast<Enum> (           \
            static_cast<underlying>(lhs) OP  \
            static_cast<underlying>(rhs)     \
        );                                   \
    }                                        \


template <class TKey, class TValue>
const bool tryGetValue(const unordered_map<TKey, TValue>& source, const TKey& key, TValue& value)
{
    auto found = source.find(key);
    if (found == source.end())
    {
        return false;
    }
    value = found->second;
    return true;
}   

template <class TKey, class TValue>
const TValue& getValueOrThrow(const unordered_map<TKey, TValue>& source, const TKey& key)
{
    auto found = source.find(key);
    if (found == source.end())
    {
        stringstream error;
        error << "Key not found in map: " << key;
        throw runtime_error(error.str());
    }
    return found->second;
}   

template <class T>
bool hasKey(const unordered_set<T>& source, const T& key)
{
    return (source.find(key) != source.end());
}   

template <class K, class V>
bool hasKey(const unordered_map<K, V>& source, const K& key)
{
    return (source.find(key) != source.end());
}   

template <class T>
bool tryInsertKey(unordered_set<T>& source, const T& key)
{
    return source.insert(key).second;
} 

template <class T>
bool hasAny(const vector<T>& source, function<bool(const T& item)> predicate)
{
    return (find_if(source.begin(), source.end(), predicate) != source.end());
}   

template <class T>
const T tryFindFirst(const vector<T>& source, function<bool(const T& item)> predicate)
{
    auto found = find_if(source.begin(), source.end(), predicate);
    return (found != source.end()
        ? *found
        : nullptr);
}   

bool stringStartsWith(const string& s, const string& prefix);

time_t initTime(int year, int month, int day, int hour, int min, int sec);
