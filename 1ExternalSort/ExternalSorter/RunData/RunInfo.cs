using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ExternalSorter
{
    public struct RunInfo
    {
        #region Nested class
        
        internal class RunInfoFactory
        {
            private readonly Random m_rnd = new Random();
            private const string Chars = "abcdefghijklmnopqrstuvwxyz0123456789";

            private readonly string m_randomStringBase;
            private int m_runCount;


            public RunInfoFactory()
            {
                m_randomStringBase = Assembly.GetExecutingAssembly().FullName;
                m_randomStringBase += "__";

                m_randomStringBase += Enumerable.Range(1, 8)
                    .Select(idx => Chars[m_rnd.Next(Chars.Length)]);
            }


            public RunInfo GetRunInfo(int runLength)
            {
                var runCount = Interlocked.Increment(ref m_runCount);
                return new RunInfo(m_randomStringBase + "__" + runCount, runLength);
            }
        }

        #endregion


        private readonly string m_runName;
        private readonly int m_runSize;

        public string RunName
        {
            get { return m_runName; }
        }

        public int RunSize
        {
            get { return m_runSize; }
        }


        private RunInfo(string runName, int runSize)
        {
            m_runName = runName;
            m_runSize = runSize;
        }

    }
}
