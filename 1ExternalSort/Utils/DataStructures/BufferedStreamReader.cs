using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.DataStructures
{
    public class BufferedStreamReader : StreamReader
    {
        public BufferedStreamReader(string inputFile)
            : base(inputFile)
        { }


        
    }
}
