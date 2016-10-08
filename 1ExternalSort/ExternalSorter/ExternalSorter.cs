using System.Diagnostics;

namespace ExternalSorter
{
    public class ExternalSorter
    {
        private Heap<RunInfoFactory.RunInfo> m_runs;


        public ExternalSorter()
        {
        }


        public void DoSorting(string inputFile, string outputFile)
        {
            Debug.Assert(!string.IsNullOrEmpty(inputFile));
            Debug.Assert(!string.IsNullOrEmpty(outputFile));

            while (true)
            {

            }
        }
    }

    public class Heap<T>
    {
        public Heap()
        {

        }
    }
}
