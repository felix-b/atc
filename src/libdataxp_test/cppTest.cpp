// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include "gtest/gtest.h"
#include <sstream>

using namespace std;

TEST(CppTest, extractFromStreamTillEndOfLine) {
    stringstream str;
    str << " aaa bbb ccc ddd " << endl << " eee fff " << endl << " zzz ";
    str.seekg(0);

    string word1, word2, word3, word4;
    string rest1, rest2;

    str >> word1 >> word2;
    getline(str, rest1);
    str >> word3 >> word4;
    getline(str, rest2);

    EXPECT_EQ(word1, "aaa");
    EXPECT_EQ(word2, "bbb");
    EXPECT_EQ(rest1, " ccc ddd ");
    EXPECT_EQ(word3, "eee");
    EXPECT_EQ(word4, "fff");
    EXPECT_EQ(rest2, " ");
}

TEST(CppTest, extractFromStream_8_decimalDigits) {
    stringstream strIn;
    stringstream strOut;
    strIn << " 32.00029334 ";
    strIn.seekg(0);

    strIn.precision(11);
    strOut.precision(11);

    double value;
    strIn >> value;
    strOut << value;

    EXPECT_EQ(strOut.str(), "32.00029334");
    EXPECT_GT(value, 32.00029333);
    EXPECT_LT(value, 32.00029335);
}
