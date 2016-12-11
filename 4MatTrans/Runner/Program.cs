
using UtilsTests.FibHeap;
using UtilsTests.Matrix;

namespace Runner
{
    class Program
    {
        private const string LogFolderName = "Log";

        static void Main(string[] args)
        {
            var test = new MatrixGeneratorTests();
            test.TestCache();
        }
    }
}
