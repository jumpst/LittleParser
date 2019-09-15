using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace JVM
{
    public struct Func
    {
        public string FuncName;
        public int EntryPoint;
        public int ParamCount;
        public int LocalSize ;
    }

    class FuncTable
    {
        private static FuncTable instance;
        public static FuncTable Instance
        {
            get
            {
                if (instance == null) 
                {
                    instance = new FuncTable();
                }
                return instance;
            }
        }
        LinkedList<Func> funcTable;
        private FuncTable()
        {
            funcTable = new LinkedList<Func>();
        }

        public bool AddFunc(string funcName, int entryPoint,out int funcIndex)
        {
            LinkedList<Func>.Enumerator enu = funcTable.GetEnumerator();

            while (enu.MoveNext()) 
            {
                if (enu.Current.FuncName == funcName) 
                {
                    funcIndex = -1;
                    return false;
                }
            }
            Func func = default(Func);
            func.FuncName = funcName;
            func.EntryPoint = entryPoint;
            funcTable.AddLast(func);
            funcIndex = funcTable.Count - 1;
            return true;
        }

        public void AddFuncParam( int paramCount, int localSize)
        {
            Func func = funcTable.Last.Value;
            func.ParamCount = paramCount;
            func.LocalSize = localSize;
        }

        public int GetLocalSize(int funcIndex)
        {

            LinkedList<Func>.Enumerator enu = funcTable.GetEnumerator();
            int index = 0;
            while (enu.MoveNext())
            {

                if (index == funcIndex) 
                {
                    return enu.Current.LocalSize;
                }
                index++;
            }

            return -1;
        }

        public bool FindFunc(string funcName, out int funcIndex)
        {
           LinkedList<Func>.Enumerator enumerator = funcTable.GetEnumerator();
            int i = 0;
            while (enumerator.MoveNext())
            {
                i++;
                if (enumerator.Current.FuncName == funcName) 
                {
                    funcIndex = i;
                    return true;
                }
            }
            funcIndex = -1;
            return false;
        }
       
    }
}
