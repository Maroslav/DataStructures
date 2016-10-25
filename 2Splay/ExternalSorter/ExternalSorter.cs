using System.Diagnostics;
using System.IO;
using Utils.DataStructures;

namespace ExternalSorter
{
    public class ExternalSorter
    {
        private RunInfo.RunInfoFactory m_runInfoFactory = new RunInfo.RunInfoFactory();

        private readonly bool m_deleteFilesAfterDone;


        public ExternalSorter(bool deleteFilesAfterDone = false)
        {
            m_deleteFilesAfterDone = deleteFilesAfterDone;
        }


        public void DoSorting(string inputFile, string outputFile)
        {
            Debug.Assert(!string.IsNullOrEmpty(inputFile));
            Debug.Assert(File.Exists(inputFile));
            Debug.Assert(!string.IsNullOrEmpty(outputFile));

            // Split the input file into smaller ordered chunks
            Heap<RunInfo> runs = GatherRuns(inputFile);

        }

        public Heap<RunInfo> GatherRuns(string inputFile)
        {
            BufferedStreamReader reader = new BufferedStreamReader(inputFile);
            Heap<RunInfo> runs = new Heap<RunInfo>();

            while (!reader.EndOfStream)
            {
                runs.Add(GatherNextRun(reader));
            }

            if (m_deleteFilesAfterDone)
                File.Delete(inputFile);
        }

        private RunInfo GatherNextRun(BufferedStreamReader reader)
        {


            int runLength = 0;

            return m_runInfoFactory.GetRunInfo(runLength);
        }
    }

}
