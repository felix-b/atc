using System.IO;
using NUnit.Framework;

namespace Atc.Data.Sources.Tests
{
    public static class TestContextExtensions
    {
        public static string GetTestInputPath(this TestContext testContext, params string[] parts)
        {
            return Path.Combine(
                testContext.TestDirectory,
                "..",
                "..",
                "..",
                "TestInputs",
                Path.Combine(parts)
            );
        }

        public static string GetTestOutputPath(this TestContext testContext, params string[] parts)
        {
            return Path.Combine(
                testContext.TestDirectory,
                "..",
                "..",
                "..",
                "TestOutputs",
                Path.Combine(parts)
            );
        }
    }
}