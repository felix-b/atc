using NUnit.Framework;
using Zero.Serialization.Buffers;

namespace Atc.Data.Tests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            BufferContextScope.UseAsyncLocalScope();            
        }
    }
}