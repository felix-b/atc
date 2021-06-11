using NUnit.Framework;

namespace Zero.Serialization.Buffers.Tests
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