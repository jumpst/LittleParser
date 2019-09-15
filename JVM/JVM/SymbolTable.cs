using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace JVM
{
    struct Symbol
    {
        public string SymbolName;
        public int StackIndex;
        public int FuncIndex;
        public int Size;
    }

    class SymbolTable
    {
        private static SymbolTable instance;
        public static SymbolTable Instance
        {
            get
            {
                if (instance == null) 
                {
                    instance = new SymbolTable();
                }
                return instance;
            }
        }
        LinkedList<Symbol> symbolTable;
        private SymbolTable()
        {
            symbolTable = new LinkedList<Symbol>();
        }

        public bool AddSymbol(string symbolName, int stackIndex, int funcIndex, int size)
        {
            LinkedList<Symbol>.Enumerator enu = symbolTable.GetEnumerator();

            while (enu.MoveNext())
            {
                if (enu.Current.SymbolName == symbolName && (enu.Current.FuncIndex == funcIndex || enu.Current.StackIndex > 0)) 
                {
                    return false;
                }
            }


            Symbol symbol = default(Symbol);
            symbol.FuncIndex = funcIndex;
            symbol.SymbolName = symbolName;
            symbol.StackIndex = stackIndex;
            symbol.Size = size;
            symbolTable.AddLast(symbol);
            return true;
        }

        public bool FindSymbol(string symbolName, int funcIndex, out int stackIndex)
        {
            LinkedList<Symbol>.Enumerator enu = symbolTable.GetEnumerator();

            while (enu.MoveNext())
            {
                if (enu.Current.SymbolName == symbolName && (enu.Current.FuncIndex == funcIndex || enu.Current.StackIndex > 0))
                {
                    stackIndex = enu.Current.StackIndex;
                    return true;
                }
            }
            stackIndex = -1;
            return false;
        }

        public int FindSymbolSize(string symbolName, int funcIndex)
        {
            LinkedList<Symbol>.Enumerator enu = symbolTable.GetEnumerator();

            while (enu.MoveNext())
            {
                if (enu.Current.SymbolName == symbolName && (enu.Current.FuncIndex == funcIndex || enu.Current.StackIndex > 0))
                {
                     return enu.Current.Size;
                }
            }
            return   -1;
        }
    }
}
