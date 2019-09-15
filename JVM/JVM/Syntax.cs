using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVM
{

    public enum Op_Type_Flag
    {
        OP_FLAG_INT = 0,
        OP_FLAG_FLOAT = 2,
        OP_FLAG_STRING = 4,
        OP_FLAG_MEM = 8,
        OP_FLAG_FUNC = 16,
        OP_FLAG_RETVAL = 32,
        OP_FLAG_LABEL = 64,
        OP_FLAG_HOSTFUNC = 128
    }

    public enum Op_Type
    {
        OP_TYPE_INT,
        OP_TYPE_FLOAT,
        OP_TYPE_STRING,
        OP_TYPE_REL_STACKINDEX,
        OP_TYPE_ABS_STACKINDEX,
        OP_TYPE_FUNC_INDEX,
        OP_TYPE_LABEL_INDEX,
        OP_TYPE_HOST_FUNC,
        OP_TYPE_RETVAL
    }

    struct MainHeader
    {
        public bool FoundMainFunc;
        public bool FoundStackSize;
        public int StackSize;
        public int MainEntryPoint;
        public int GlobalLocalSize;
    }

    struct SyntaxHelper
    {
        
        public bool IsAliveFunc;
        public int CurrFuncIndex;
        
        public int CurrFuncParam;
        public int CurrFuncLocalSize;
        public int InstrStreamSize;
        public int CurrInstrIndex;
    }

    
    class Syntax
    {

        private SyntaxHelper SyntaxHelper;
        private MainHeader MainHeader;
        private Instr[] instrStream;
        private StringTable stringTable;
        private StringTable hostApiTable;

        public void DealFunc()
        {
            if (SyntaxHelper.IsAliveFunc)
            {
                ExDebug.Instance.ExitOnCode("函数嵌套");
            }
            if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_IDENTIFER)
            {
                ExDebug.Instance.ExitOnCode("没有标识符");
            }
            string funcName = Lexer.Instance.GetCurrLexeme();

            if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_OPEN_BRACKET)
            {
                ExDebug.Instance.ExitOnCode("没有开放括号");
            }
            if (!FuncTable.Instance.AddFunc(funcName, SyntaxHelper.InstrStreamSize, out SyntaxHelper.CurrFuncIndex))
            {
                ExDebug.Instance.ExitOnCode("函数重复定义");
            }

            SyntaxHelper.IsAliveFunc = true;
            SyntaxHelper.CurrFuncLocalSize = 0;
            SyntaxHelper.CurrFuncParam = 0;

            SyntaxHelper.InstrStreamSize++; //每个函数都要加一个ret语句
            if (funcName == "_Main")
            {
                MainHeader.FoundMainFunc = true;
                MainHeader.MainEntryPoint = SyntaxHelper.CurrFuncIndex;
            }
        }

        public void DealCloseBracket()
        {
            if (!SyntaxHelper.IsAliveFunc)
            {
                ExDebug.Instance.ExitOnCode("不在函数内部");
            }
            FuncTable.Instance.AddFuncParam(SyntaxHelper.CurrFuncParam, SyntaxHelper.CurrFuncLocalSize);
        }

        public void DealParam()
        {
            if (!SyntaxHelper.IsAliveFunc)
            {
                ExDebug.Instance.ExitOnCode("Param只能在函数里面定义");
            }
            if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_IDENTIFER)
            {
                ExDebug.Instance.ExitOnCode("Var后面不是标识符");
            }
            SyntaxHelper.CurrFuncParam++;
        }

        public void DealParamTwice()
        {

        }

        public void DealRowLabel()
        {
            string labelName = Lexer.Instance.GetCurrLexeme();
            if (!SyntaxHelper.IsAliveFunc)
            {
                ExDebug.Instance.ExitOnCode("行标签不能被定义在函数外面");
            }
            if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_COLON)
            {
                ExDebug.Instance.ExitOnCode("行标签没有冒号");
            }

            //这里要减一因为函数里面的返回指令加了一
            if (!LabelTable.Instance.AddLabel(labelName, SyntaxHelper.InstrStreamSize - 1, SyntaxHelper.CurrFuncIndex))
            {
                ExDebug.Instance.ExitOnCode("重复定义行标签");
            }
        }
        public void DealVar()
        {
            if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_IDENTIFER)
            {
                ExDebug.Instance.ExitOnCode("声明变量不是标识符");
            }

            char nextChar = Lexer.Instance.GetLookAheadChar();
            int size = 1;
            if (nextChar == '[') 
            {
                if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_OPEN_BRACE)
                {
                    ExDebug.Instance.ExitOnCode("数组没有中括号");
                }
                if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_INT) 
                {
                    ExDebug.Instance.ExitOnCode("数组长度应该是整数");
                }
                size = int.Parse(Lexer.Instance.GetCurrLexeme());
                if (size <= 0)
                {
                    ExDebug.Instance.ExitOnCode("数组的长度不能小于0");
                }
                if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_CLOSE_BRACE) 
                {
                    ExDebug.Instance.ExitOnCode("数组没有关闭括号");
                }
            }

            string varName = Lexer.Instance.GetCurrLexeme();

            int stackIndex = 0;
            if (SyntaxHelper.IsAliveFunc)
            {
                stackIndex = -(SyntaxHelper.CurrFuncLocalSize + 2);
                SyntaxHelper.CurrFuncLocalSize += size;
            }
            else
            {
                stackIndex = MainHeader.GlobalLocalSize;
                MainHeader.GlobalLocalSize += size;
            }

            if (SymbolTable.Instance.AddSymbol(varName, stackIndex, SyntaxHelper.CurrFuncIndex, size))
            {
                ExDebug.Instance.ExitOnCode("变量重复命名");
            }
        }

        public void DealStackSize()
        {
            if (MainHeader.FoundStackSize)
            {
                ExDebug.Instance.ExitOnCode("重复设定堆栈大小");
            }
            if (SyntaxHelper.IsAliveFunc)
            {
                ExDebug.Instance.ExitOnCode("堆栈不能定义在函数当中");
            }
            if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_INT)
            {
                ExDebug.Instance.ExitOnCode("设定的堆栈大小不是整数");
            }
            int stackSize = int.Parse(Lexer.Instance.GetCurrLexeme());
            if (stackSize < 0)
            {
                ExDebug.Instance.ExitOnCode("堆栈大小不能设定为负数");
            }
            MainHeader.StackSize = stackSize;
            MainHeader.FoundStackSize = true;
        }

        public void DealInstr()
        {
            if (!SyntaxHelper.IsAliveFunc)
            {
                ExDebug.Instance.ExitOnCode("指令只能出现在函数里面");
            }
            SyntaxHelper.InstrStreamSize++;
        }

       
        public void Parser()
        {
            while (true)
            {
                AttributeType currAttribute = Lexer.Instance.GetNextToken();
                if (currAttribute == AttributeType.TYPE_END_OF_FILE)
                {
                    break;
                }

                switch (currAttribute)
                {
                    case AttributeType.TYPE_FUNC:
                        DealFunc();
                        break;
                    case AttributeType.TYPE_PARAM:
                        DealParam();
                        break;
                    case AttributeType.TYPE_VAR:
                        DealVar();
                        break;
                    case AttributeType.TYPE_SETSTACKSIZE:
                        DealStackSize();
                        break;
                    case AttributeType.TYPE_IDENTIFER:
                        DealRowLabel();
                        break;
                    case AttributeType.TYPE_CLOSE_BRACE:
                        DealCloseBracket();
                        break;
                    case AttributeType.TYPE_INSTR:
                        DealInstr();
                        break;
                    default:
                        ExDebug.Instance.ExitOnCode("出现异常情况");
                        break;
                }
                //此刻完成了对一条指令的解析 应该还需要检验其后是否有其他杂乱字符串  Mov vxa, 20 dsdsds    这样的情况也应该报错
                //其实对于
                if (Lexer.Instance.GetLookAheadChar() != '')
                {
                    ExDebug.Instance.ExitOnCode("出现杂乱码");
                }

                //这一句代码仅仅是为了让指令只能在一行内
                if (!Lexer.Instance.SkipNextLine())
                {
                    break;
                }
            }

            instrStream = new Instr[SyntaxHelper.InstrStreamSize];
            Lexer.Instance.ResetLexer();
            SyntaxHelper.CurrInstrIndex = 0;

            while (true)
            {
                AttributeType currAttribute = Lexer.Instance.GetNextToken();

                switch (currAttribute)
                {
                    case AttributeType.TYPE_FUNC:
                        SyntaxHelper.IsAliveFunc = true;
                        SyntaxHelper.CurrFuncParam = 0;
                        FuncTable.Instance.FindFunc(Lexer.Instance.GetCurrLexeme(), out SyntaxHelper.CurrFuncIndex);
                        break;
                    case AttributeType.TYPE_PARAM:
                        if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_IDENTIFER)
                        {
                            ExDebug.Instance.ExitOnCode("Param 后面不是字符串");
                        }
                        //堆栈索引等于
                        int stackIndex = -(FuncTable.Instance.GetLocalSize(SyntaxHelper.CurrFuncIndex) + 3 + SyntaxHelper.CurrFuncParam);
                        SymbolTable.Instance.AddSymbol(Lexer.Instance.GetCurrLexeme(), SyntaxHelper.CurrFuncIndex, stac);
                        
                        break;
                 
                    case AttributeType.TYPE_INSTR:
                        string instrName = Lexer.Instance.GetCurrLexeme();
                        InstrLookUp instrLookUp = default(InstrLookUp);
                        if(!InstrTable.Instance.FindInstr(instrName, out instrLookUp))
                        {
                            ExDebug.Instance.ExitOnCode("找不到对应指令");
                        }
                        instrStream[SyntaxHelper.CurrInstrIndex].OpList = new Op[instrLookUp.OpCount];
                        instrStream[SyntaxHelper.CurrInstrIndex].InstCode = instrLookUp.InstCode;

                        for (int i = 0; i < instrLookUp.OpCount; i++) 
                        {
                            AttributeType type = Lexer.Instance.GetNextToken();
                            int  opFlag = instrLookUp.OpFlagList[i];
                            string opContent = Lexer.Instance.GetCurrLexeme();

                            if (i != instrLookUp.OpCount - 1) 
                            {
                                if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_COMMA)
                                {
                                    ExDebug.Instance.ExitOnCode("操作数之间没有逗号分隔");
                                }
                            }
                            
                            switch (type)
                            {
                                case AttributeType.TYPE_INT:
                                    #region 立即数
                                    if ((opFlag & (int)Op_Type_Flag.OP_FLAG_INT) == 0) 
                                    {
                                        ExDebug.Instance.ExitOnCode("当前操作数类型不符合");
                                    }
                                    instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_INT;
                                    int intValue = 0;
                                    if (int.TryParse(opContent, out intValue)) 
                                    {
                                        ExDebug.Instance.ExitOnCode("解析数字出现错误");
                                    }
                                    instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = intValue;
                                    break;
                                #endregion

                                case AttributeType.TYPE_FLOAT:
                                    #region 立即数
                                    if ((opFlag & (int)Op_Type_Flag.OP_FLAG_FLOAT) == 0)
                                    {
                                        ExDebug.Instance.ExitOnCode("当前操作数类型不符合");
                                    }
                                    instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_FLOAT;
                                    float floatValue = 0;
                                    if (float.TryParse(opContent, out floatValue)) 
                                    {
                                        ExDebug.Instance.ExitOnCode("解析数字出现错误");
                                    }
                                    instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].floatLiteral = floatValue;
                                    break;
                                #endregion

                                case AttributeType.TYPE_DOUBLE_QUOTA:
                                    #region 字符串类型
                                    //这里会正常输出空字符串吗
                                    if ((opFlag & (int)Op_Type_Flag.OP_FLAG_STRING) == 0)
                                    {
                                        ExDebug.Instance.ExitOnCode("当前操作数类型不符合");
                                    }

                                    AttributeType token = Lexer.Instance.GetNextToken();

                                    if (token == AttributeType.TYPE_DOUBLE_QUOTA 
                                        && Lexer.Instance.RetLexState() == LexState.EndString)
                                    {
                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_STRING;
                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = stringTable.AddString("");
                                    }
                                    else if (token==AttributeType.TYPE_STRING)
                                    {
                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_STRING;
                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = 
                                            stringTable.AddString(Lexer.Instance.GetCurrLexeme());
                                    }
                                    else
                                    {
                                        ExDebug.Instance.ExitOnCode("双引号后面出现其他字符");
                                    }
                                    break;
                                #endregion

                                case AttributeType.TYPE_RETVAL:
                                    #region 寄存器类型
                                    if ((opFlag & (int)Op_Type_Flag.OP_FLAG_RETVAL) == 0)
                                    {
                                        ExDebug.Instance.ExitOnCode("当前操作数类型不符合");
                                    }
                                    instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_RETVAL;
                                    instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = 0;
                                    break;
                                #endregion

                                case AttributeType.TYPE_IDENTIFER:
                                    //对于数组和变量是不能重名的
                                    //保持简单 所以函数不支持复杂的寻址能力
                                    #region 标识符
                                    if ((opFlag & (int)Op_Type_Flag.OP_FLAG_MEM) == 0) 
                                    {
                                        int baseIndex = 0;
                                        if (!SymbolTable.Instance.FindSymbol(opContent, SyntaxHelper.CurrFuncIndex, out baseIndex))
                                        {
                                            ExDebug.Instance.ExitOnCode("没有对应变量 ");
                                        }

                                        int size = SymbolTable.Instance.FindSymbolSize(opContent, SyntaxHelper.CurrFuncIndex);
                                            //如果是数组，那么会将其覆盖
                                        if (Lexer.Instance.GetLookAheadChar() == '[')
                                        {

                                            if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_OPEN_BRACKET)
                                            {
                                                ExDebug.Instance.ExitOnCode("数组没有中括号");
                                            }
                                            if (size < 1) 
                                            {
                                                ExDebug.Instance.ExitOnCode("数组尺寸小于1");
                                            }
                                            AttributeType token = Lexer.Instance.GetNextToken();

                                            if (token == AttributeType.TYPE_INT)
                                            {
                                                int offset = int.Parse(Lexer.Instance.GetCurrLexeme());
                                                    
                                                //绝对堆栈索引在原程序里面没有判断正负
                                                if (baseIndex > 0)
                                                {
                                                    baseIndex += offset;
                                                }
                                                else
                                                {
                                                    baseIndex -= offset;
                                                }

                                                instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_ABS_STACKINDEX;
                                                instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = baseIndex;
                                            }
                                            else if (token == AttributeType.TYPE_IDENTIFER)
                                            {
                                                string identName = Lexer.Instance.GetCurrLexeme();
                                                int offsetStack = 0;
                                                if (!SymbolTable.Instance.FindSymbol(identName, SyntaxHelper.CurrFuncIndex, out offsetStack))
                                                {
                                                    ExDebug.Instance.ExitOnCode("没有定义的变量");
                                                }

                                                int identSize = SymbolTable.Instance.FindSymbolSize(identName, SyntaxHelper.CurrFuncIndex);
                                                if (identSize > 1 || identSize < 0) 
                                                {
                                                    ExDebug.Instance.ExitOnCode("数组的索引不是变量");
                                                }

                                                instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_REL_STACKINDEX;
                                                instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = baseIndex;
                                                instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].offset = offsetStack;
                                            }
                                            else
                                            {
                                                ExDebug.Instance.ExitOnCode("数组出现不合法的序号");
                                            }

                                            if (Lexer.Instance.GetNextToken() != AttributeType.TYPE_CLOSE_BRACKET)
                                            {
                                                ExDebug.Instance.ExitOnCode("数组没有闭合");
                                            }
                                           
                                        }
                                        else
                                        {
                                            if (size > 1)
                                            {
                                                ExDebug.Instance.ExitOnCode("变量的size不能大于1");
                                            }
                                            instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_ABS_STACKINDEX;
                                            instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = baseIndex;
                                        }
                                    }

                                    if ((opFlag & (int)Op_Type_Flag.OP_FLAG_FUNC) == 0)
                                    {
                                        int index = 0;
                                        if (!FuncTable.Instance.FindFunc(opContent, out index)) 
                                        {
                                            ExDebug.Instance.ExitOnCode("没有定义对应函数");
                                        }

                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_FUNC_INDEX;
                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = index;
                      
                                    }

                                    if ((opFlag & (int)Op_Type_Flag.OP_FLAG_HOSTFUNC) == 0)
                                    {
                                        //如果这个API不存在呢;
                                        //所以还应该有一个主程序API的表
                                        int index = hostApiTable.AddString(opContent);
                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_HOST_FUNC;
                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = index;
                                      
                                    }

                                    //要是标签
                                    if ((opFlag & (int)Op_Type_Flag.OP_FLAG_LABEL) == 0)
                                    {
                                        int index = 0;
                                        if (!LabelTable.Instance.FindLabel(opContent, SyntaxHelper.CurrFuncIndex, out index))
                                        {
                                            ExDebug.Instance.ExitOnCode("没有定义该标签");
                                        }
                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].Type = Op_Type.OP_TYPE_LABEL_INDEX;
                                        instrStream[SyntaxHelper.CurrInstrIndex].OpList[i].index = index;
                                    }

                                    break;
                                #endregion

                                case AttributeType.TYPE_END_OF_FILE:
                                    break;
                                default:
                                    ExDebug.Instance.ExitOnCode("指令错误");
                                    break;
                            }
                        }
                        break;
                    default:
                        ExDebug.Instance.ExitOnCode("发现以其他字符进行开头");
                        break;
                }

                if (!Lexer.Instance.SkipNextLine())
                {
                    break;
                }
            }
        }
       
           

        
    }
}
