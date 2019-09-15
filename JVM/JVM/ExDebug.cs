using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVM
{
   
    class ExDebug
    {
        private static ExDebug instance;

        public static ExDebug Instance
        {
            get
            {
                if (instance == null) 
                {
                    instance = new ExDebug();
                }
                return instance;
            }
        }
        
        public void ExitOnCode(string msg)
        {

        }
        
    }
}
