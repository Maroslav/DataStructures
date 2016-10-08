using System.Diagnostics;

namespace ExternalSorter
{
    public class ExternalSorter
    {
        private string m_filePath;

        public ExternalSorter(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));
            m_filePath = filePath;
        }


    }
}
