
using UtilsTests.FibHeap;

namespace Runner
{
    class Program
    {
        private const string LogFolderName = "Log";

        static void Main(string[] args)
        {
            var test = new FibGeneratorTests(LogFolderName);
            test.BalancedTest();
            test.Dispose();

            test = new FibGeneratorTests(LogFolderName);
            test.ImbalancedTest();
            test.Dispose();

            test = new FibGeneratorTests(LogFolderName);
            test.MaliciousTest();
            test.Dispose();
        }
    }
}
