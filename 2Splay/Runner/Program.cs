using UtilsTests.SplayTree;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            GeneratorTests tests = new GeneratorTests();

            int[] ts = { 10, 100, 1000, 10000, 100000, 1000000 };

            foreach (var t in ts)
                tests.RunSubset(t);

            tests.RunSequential();
        }
    }
}
