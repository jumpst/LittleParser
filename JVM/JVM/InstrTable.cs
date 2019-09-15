using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace JVM
{
    public struct InstrLookUp
    {
        public string InstrName;
        public int InstCode;
        public int OpCount;
        public List<int> OpFlagList;
    }

    public struct Op
    {
        public Op_Type Type;
        public int index;
        public int offset;
        public float floatLiteral;
    }
    public struct Instr
    {
        public string InstrName;
        public int InstCode;
        public int OpCount;
        public Op[] OpList;
    }

    class InstrTable
    {
        private static InstrTable instance;
        public static InstrTable Instance{
            get
            {
                if (instance == null) 
                {
                    instance = new InstrTable();
                }
                return instance;
            }
        }
        LinkedList<InstrLookUp> instrTable;
        private InstrTable()
        {
            instrTable = new LinkedList<InstrLookUp>();
        }

        public void AddInstr(InstrLookUp instr)
        {
            instrTable.AddLast(instr);
        }

        public bool FindInstr(string instrName, out InstrLookUp instr)
        {
           LinkedList<InstrLookUp>.Enumerator enumerator = instrTable.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.InstrName == instrName)
                {
                    instr = enumerator.Current;
                    return true;
                }
            }
            instr = default(InstrLookUp);
            return false;
        }
       
    }
}
