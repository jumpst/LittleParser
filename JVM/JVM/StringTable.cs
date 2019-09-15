using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace JVM
{
   
    class StringTable
    {
      
        private  LinkedList<String> stringTable;
        private StringTable()
        {
            stringTable = new LinkedList<String>();
        }

        public int AddString(String content)
        {
            stringTable.AddLast(content);
            return stringTable.Count - 1;
        }

      
       
    }
}
