//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <cstring>
#include <cctype>
#include <cstdlib>
#include <map>
#include <stdexcept>
#include <string>
#include <vector>

using namespace std;

class SimpleJson
{
public:
    enum class Kind
    {
        Null,
        Bool,
        Number,
        String,
        Array,
        Object
    };

private:
    Kind m_kind = Kind::Null;
    bool m_boolValue = false;
    double m_numberValue = 0.0;
    string m_stringValue;
    vector<SimpleJson> m_arrayValue;
    map<string, SimpleJson> m_objectValue;

public:
    SimpleJson() = default;

    static SimpleJson parse(const string& text)
    {
        Parser parser(text);
        SimpleJson value = parser.parseValue();
        parser.skipWhitespace();
        if (!parser.atEnd())
        {
            throw runtime_error("SimpleJson::parse: trailing content after JSON document");
        }
        return value;
    }

    Kind kind() const { return m_kind; }
    bool isNull() const { return m_kind == Kind::Null; }
    bool isBool() const { return m_kind == Kind::Bool; }
    bool isNumber() const { return m_kind == Kind::Number; }
    bool isString() const { return m_kind == Kind::String; }
    bool isArray() const { return m_kind == Kind::Array; }
    bool isObject() const { return m_kind == Kind::Object; }

    const string& stringValue() const { return m_stringValue; }
    double numberValue() const { return m_numberValue; }
    bool boolValue() const { return m_boolValue; }
    const vector<SimpleJson>& arrayValue() const { return m_arrayValue; }
    const map<string, SimpleJson>& objectValue() const { return m_objectValue; }

    const SimpleJson* tryGet(const string& key) const
    {
        if (!isObject())
        {
            return nullptr;
        }

        auto it = m_objectValue.find(key);
        return it != m_objectValue.end() ? &it->second : nullptr;
    }

    string asString(const string& fallback = "") const
    {
        switch (m_kind)
        {
        case Kind::String:
            return m_stringValue;
        case Kind::Number:
        {
            string result = to_string(m_numberValue);
            while (!result.empty() && result.back() == '0')
            {
                result.pop_back();
            }
            if (!result.empty() && result.back() == '.')
            {
                result.pop_back();
            }
            return result;
        }
        case Kind::Bool:
            return m_boolValue ? "true" : "false";
        default:
            return fallback;
        }
    }

    long long asInteger(long long fallback = 0) const
    {
        switch (m_kind)
        {
        case Kind::Number:
            return static_cast<long long>(m_numberValue);
        case Kind::String:
        {
            char* endPtr = nullptr;
            const long long value = strtoll(m_stringValue.c_str(), &endPtr, 10);
            return endPtr && *endPtr == '\0' ? value : fallback;
        }
        default:
            return fallback;
        }
    }

private:
    class Parser
    {
    private:
        const string& m_text;
        size_t m_index;

    public:
        Parser(const string& text) : m_text(text), m_index(0) {}

        bool atEnd() const { return m_index >= m_text.size(); }

        void skipWhitespace()
        {
            while (!atEnd() && isspace(static_cast<unsigned char>(m_text[m_index])))
            {
                ++m_index;
            }
        }

        SimpleJson parseValue()
        {
            skipWhitespace();
            if (atEnd())
            {
                throw runtime_error("SimpleJson::parse: unexpected end of input");
            }

            const char ch = m_text[m_index];
            if (ch == '"')
            {
                return parseString();
            }
            if (ch == '{')
            {
                return parseObject();
            }
            if (ch == '[')
            {
                return parseArray();
            }
            if (ch == 't')
            {
                return parseLiteral("true", Kind::Bool, true);
            }
            if (ch == 'f')
            {
                return parseLiteral("false", Kind::Bool, false);
            }
            if (ch == 'n')
            {
                return parseNull();
            }

            return parseNumber();
        }

    private:
        SimpleJson parseLiteral(const char* literal, Kind kind, bool boolValue)
        {
            const size_t literalLength = strlen(literal);
            if (m_text.compare(m_index, literalLength, literal) != 0)
            {
                throw runtime_error("SimpleJson::parse: invalid literal");
            }

            m_index += literalLength;
            SimpleJson value;
            value.m_kind = kind;
            value.m_boolValue = boolValue;
            return value;
        }

        SimpleJson parseNull()
        {
            return parseLiteral("null", Kind::Null, false);
        }

        SimpleJson parseString()
        {
            expect('"');
            string result;

            while (!atEnd())
            {
                char ch = m_text[m_index++];
                if (ch == '"')
                {
                    SimpleJson value;
                    value.m_kind = Kind::String;
                    value.m_stringValue = std::move(result);
                    return value;
                }

                if (ch != '\\')
                {
                    result.push_back(ch);
                    continue;
                }

                if (atEnd())
                {
                    throw runtime_error("SimpleJson::parse: unterminated escape sequence");
                }

                char escaped = m_text[m_index++];
                switch (escaped)
                {
                case '"': result.push_back('"'); break;
                case '\\': result.push_back('\\'); break;
                case '/': result.push_back('/'); break;
                case 'b': result.push_back('\b'); break;
                case 'f': result.push_back('\f'); break;
                case 'n': result.push_back('\n'); break;
                case 'r': result.push_back('\r'); break;
                case 't': result.push_back('\t'); break;
                case 'u':
                {
                    // Keep unicode escapes simple and robust for the schedule payloads we parse.
                    if (m_index + 4 > m_text.size())
                    {
                        throw runtime_error("SimpleJson::parse: truncated unicode escape");
                    }
                    m_index += 4;
                    result.push_back('?');
                    break;
                }
                default:
                    result.push_back(escaped);
                    break;
                }
            }

            throw runtime_error("SimpleJson::parse: unterminated string literal");
        }

        SimpleJson parseNumber()
        {
            const char* start = m_text.c_str() + m_index;
            char* endPtr = nullptr;
            const double number = strtod(start, &endPtr);

            if (endPtr == start)
            {
                throw runtime_error("SimpleJson::parse: invalid number literal");
            }

            m_index += static_cast<size_t>(endPtr - start);

            SimpleJson value;
            value.m_kind = Kind::Number;
            value.m_numberValue = number;
            return value;
        }

        SimpleJson parseArray()
        {
            expect('[');
            SimpleJson value;
            value.m_kind = Kind::Array;

            skipWhitespace();
            if (consume(']'))
            {
                return value;
            }

            while (true)
            {
                value.m_arrayValue.push_back(parseValue());
                skipWhitespace();
                if (consume(']'))
                {
                    return value;
                }
                expect(',');
            }
        }

        SimpleJson parseObject()
        {
            expect('{');
            SimpleJson value;
            value.m_kind = Kind::Object;

            skipWhitespace();
            if (consume('}'))
            {
                return value;
            }

            while (true)
            {
                skipWhitespace();
                SimpleJson key = parseString();
                skipWhitespace();
                expect(':');
                value.m_objectValue.emplace(key.m_stringValue, parseValue());
                skipWhitespace();
                if (consume('}'))
                {
                    return value;
                }
                expect(',');
            }
        }

        void expect(char expected)
        {
            if (atEnd() || m_text[m_index] != expected)
            {
                throw runtime_error(string("SimpleJson::parse: expected '") + expected + "'");
            }
            ++m_index;
        }

        bool consume(char expected)
        {
            if (!atEnd() && m_text[m_index] == expected)
            {
                ++m_index;
                return true;
            }
            return false;
        }
    };
};