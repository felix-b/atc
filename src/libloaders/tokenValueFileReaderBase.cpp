//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <system_error>
#include "stlhelpers.h"
#include "libworld.h"
#include "libdataxp.h"

using namespace std;
using namespace world;

void TokenValueFileReaderBase::parseInputLines(istream &input, vector<Line> &lines)
{
    while (!input.eof() && !input.bad())
    {
        string text;

        try
        {
            getline(input, text);
        }
        catch(const exception &e)
        {
            break;
        }

        const char *delimiterChars = ",: ";
        const char *whitespaceChars = "\r\n\t ";
        size_t delimitierIndex = text.find_first_of(delimiterChars);
        int lastNonSpaceIndex = delimitierIndex == string::npos ? -1 : (int)text.length() - 1;

        while (lastNonSpaceIndex >= 0 && strchr(whitespaceChars, text.at(lastNonSpaceIndex)))
        {
            lastNonSpaceIndex--;
        }

        if (delimitierIndex != string::npos && delimitierIndex < lastNonSpaceIndex)
        {
            string token = text.substr(0, delimitierIndex);
            string suffix = text.substr(delimitierIndex + 1, lastNonSpaceIndex - delimitierIndex);

            if (!token.empty())
            {
                lines.push_back({ token, suffix, text.at(delimitierIndex) });
            }
        }
    }
}
