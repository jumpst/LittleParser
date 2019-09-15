using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace JVM
{
    public struct Label
    {
        public string LabelName;
        public int InstrIndex;
        public int FuncIndex;
    }

    class LabelTable
    {
        private static LabelTable instance;
        public static LabelTable Instance
        {
            get
            {
                if (instance == null) 
                {
                    instance = new LabelTable();
                }
                return instance;
            }
        }
        LinkedList<Label> labelTable;
        private LabelTable()
        {
            labelTable = new LinkedList<Label>();
        }

        public bool AddLabel(string labelName, int instrIndex, int funcIndex)
        {
            LinkedList<Label>.Enumerator enu = labelTable.GetEnumerator();

            while (enu.MoveNext()) 
            {
                if (enu.Current.LabelName == labelName && enu.Current.FuncIndex == funcIndex) 
                {
                    return false;
                }
            }

            Label label = default(Label);
            label.FuncIndex = funcIndex;
            label.LabelName = labelName;
            label.InstrIndex = instrIndex;

            labelTable.AddLast(label);
            return true;
        }


        public bool FindLabel(string labelName , int funcIndex, out int instrIndex)
        {
            LinkedList<Label>.Enumerator enu = labelTable.GetEnumerator();
            
            while (enu.MoveNext())
            {
                if (enu.Current.LabelName == labelName && enu.Current.FuncIndex == funcIndex)
                {
                    instrIndex = enu.Current.InstrIndex;
                    return true;
                }
            }

            instrIndex = -1;
            return false;
        }

       
    }
}
