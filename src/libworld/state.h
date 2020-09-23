// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#include <string>

using namespace std;

namespace world
{
    template<class T>
    class StateVariable
    {
    private:
        const string m_qualifiedName;
        T m_value;
    public:
        StateVariable(const T& _value)
        {
            m_value = _value;
        }

        const operator T&() const 
        { 
            return m_value; 
        }
    };
}
