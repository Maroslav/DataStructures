using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.DataStructures
{
    public interface ICollection<T>
    {
        void Insert(T item);
        void Delete(T item);
    }
}
