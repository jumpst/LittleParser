using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JVM
{
    public enum LexState
    {
        NoString,
        InString,
        EndString
    }
    struct LexerHelper
    {
       public int index0;
       public int index1;
       public int currLine;
       public LexState lexState;
       public char[] destStr;
    }
    public enum AttributeType
    {
        TYPE_INT,
        TYPE_FLOAT ,
        TYPE_STRING ,
        TYPE_FUNC ,
        TYPE_PARAM ,
        TYPE_VAR ,
        TYPE_SETSTACKSIZE ,
        TYPE_IDENTIFER,
        TYPE_COMMA ,
        TYPE_COLON,
        TYPE_DOUBLE_QUOTA,
        TYPE_OPEN_BRACKET ,
        TYPE_CLOSE_BRACKET,
        TYPE_OPEN_BRACE,
        TYPE_CLOSE_BRACE,
        TYPE_INVALID,
        TYPE_INSTR,
        TYPE_RETVAL,
        TYPE_END_OF_FILE
    }
    class Lexer
    {
        private const int DestStrLen = 20;

        private static Lexer instance;

        public LexerHelper lexerHelper;
        public static Lexer Instance
        {
            get
            {
                if (instance != null)
                {
                    instance = new Lexer();
                }
                return Instance;
            }
        }

        public string[] sourceFile;


        public LexState RetLexState()
        {
            return lexerHelper.lexState;
        }

        public AttributeType GetNextToken()
        {
            lexerHelper.index0 = lexerHelper.index1;
            
            //该行已经读取完毕
            //if (lexerHelper.index1 >= sourceFile[lexerHelper.currLine].Length) 
            //{
            //    if (!SkipNextLine())
            //    {
            //        return AttributeType.TYPE_END_OF_FILE;
            //    }
            //}

            if (lexerHelper.lexState == LexState.EndString) 
            {
                lexerHelper.lexState = LexState.NoString;
            }

            if (lexerHelper.lexState == LexState.NoString) 
            {

                while (true)
                {
                    if (lexerHelper.index0 >= sourceFile[lexerHelper.currLine].Length)
                    {
                        if (!SkipNextLine())
                        {
                            return AttributeType.TYPE_END_OF_FILE;
                        }
                    }

                    if (!IsWhiteSpace(sourceFile[lexerHelper.currLine][lexerHelper.index0]))
                    {
                        break;
                    }
                    lexerHelper.index0++;
                }
                lexerHelper.index1 = lexerHelper.index0;

                while (!IsSeparationChar(sourceFile[lexerHelper.currLine][lexerHelper.index1])) 
                {
                    //
                    if (lexerHelper.index1 >= sourceFile[lexerHelper.currLine].Length)
                    {
                        break;
                    }
                    //如果index1也超过了呢
                    lexerHelper.index1++;

                }
            }

            if (lexerHelper.lexState == LexState.InString) 
            {
                while (true)
                {
                    if (lexerHelper.index0 >= sourceFile[lexerHelper.currLine].Length)
                    {
                        //该字符串没有结尾符
                        return AttributeType.TYPE_INVALID;
                    }

                    if (sourceFile[lexerHelper.currLine][lexerHelper.index0] == '\\')
                    {
                        lexerHelper.index0 += 2;
                        continue;
                    }

                    if (sourceFile[lexerHelper.currLine][lexerHelper.index0] == '\"')
                    {
                        break;
                    }
                    lexerHelper.index1++;
                }
            }

            if (lexerHelper.index1 - lexerHelper.index0 == 0) 
            {
                lexerHelper.index1++;
            }

            int iDestIndex = 0;

            for (int iSourceIndex = lexerHelper.index0; iSourceIndex < lexerHelper.index1; iSourceIndex++)
            {
                if (sourceFile[lexerHelper.currLine][iSourceIndex] == '\\') 
                {
                    iSourceIndex++;
                }
                lexerHelper.destStr[iDestIndex] = sourceFile[lexerHelper.currLine][iSourceIndex];
                iDestIndex++;
            }

            lexerHelper.destStr[iDestIndex] = '\0';
            //已经获得目标字符串， 开始进行判断

            if (lexerHelper.destStr.Length < 1 || lexerHelper.destStr[0] == '"') 
            {
                if (lexerHelper.lexState == LexState.InString) 
                {
                    return AttributeType.TYPE_STRING;
                }
            }

            //单字符长度
            if (lexerHelper.destStr.Length == 1) 
            {
                switch (lexerHelper.destStr[0])
                {
                    case '"':
                        if (lexerHelper.lexState == LexState.NoString) 
                        {
                            lexerHelper.lexState = LexState.InString;
                        }
                        else
                        {
                            lexerHelper.lexState = LexState.EndString;
                        }
                        return AttributeType.TYPE_DOUBLE_QUOTA;

                    case '：': return AttributeType.TYPE_COLON;
                    case ',':   return AttributeType.TYPE_COMMA;
                    case '{': return AttributeType.TYPE_OPEN_BRACE;
                    case '}': return AttributeType.TYPE_CLOSE_BRACE;
                    case '[':  return AttributeType.TYPE_OPEN_BRACKET;
                    case ']': return AttributeType.TYPE_CLOSE_BRACKET;
                }
            }


            //长字符长度
            if (IsIdent(lexerHelper.destStr.ToString())) 
            {
                return AttributeType.TYPE_IDENTIFER;
            }
            if (IsInt(lexerHelper.ToString()))
            {
                return AttributeType.TYPE_INT;
            }
            if (IsFloat(lexerHelper.destStr.ToString()))
            {
                return AttributeType.TYPE_FLOAT;
            }
            if (IsIdent(lexerHelper.destStr.ToString()))
            {
                return AttributeType.TYPE_IDENTIFER;
            }

            //指示符
            if (lexerHelper.destStr.ToString()=="Func")
            {
                return AttributeType.TYPE_FUNC;
            }
            if (lexerHelper.destStr.ToString() == "Var")
            {
                return AttributeType.TYPE_VAR;
            }
            if (lexerHelper.destStr.ToString() == "Param")
            {
                return AttributeType.TYPE_PARAM;
            }
            if (lexerHelper.destStr.ToString() == "SetStackSize")
            {
                return AttributeType.TYPE_SETSTACKSIZE;
            }
            if (lexerHelper.destStr.ToString() == "RetVal")
            {
                return AttributeType.TYPE_RETVAL;
            }

            //指令
            InstrLookUp instr = default(InstrLookUp);
            if (InstrTable.Instance.FindInstr(lexerHelper.destStr.ToString(),out instr))
            {
                return AttributeType.TYPE_INSTR;
            }

            return AttributeType.TYPE_INVALID;
        }

        public char GetLookAheadChar()
        {
            int index1 = lexerHelper.index1;
            int currLine = lexerHelper.currLine;

            if (lexerHelper.lexState != LexState.InString) 
            {
                while (true)
                {
                    if (index1 >= sourceFile[currLine].Length)
                    {
                        if (!SkipNextLine())
                        {
                            return '\0';
                        }
                    }


                    if(!IsWhiteSpace(sourceFile[currLine][index1]))
                    {
                        break;
                    }
                    index1++;
                }
            }
            return sourceFile[lexerHelper.currLine][index1];
        }

        public void ResetLexer()
        {
            lexerHelper.index0 = 0;
            lexerHelper.index1 = 0;
            lexerHelper.lexState = LexState.NoString;
            lexerHelper.currLine = 0;
            lexerHelper.destStr = new char[20];
        }

        public string GetCurrLexeme()
        {
            return CharArrayToString(lexerHelper.destStr);
        }

        public bool SkipNextLine()
        {
            lexerHelper.currLine++;
            lexerHelper.index0 = 0;
            lexerHelper.index1 = 0;
            lexerHelper.lexState = LexState.NoString;

            if (lexerHelper.currLine >= sourceFile.Length) 
            {
                return false;
            }
            return true;
           
        }

        public void LoadFile(string fileName)
        {
            sourceFile = File.ReadAllLines(fileName, Encoding.ASCII);

            for (int i = 0; i < sourceFile.Length; i++)
            {
               sourceFile[i] = TrimComma(sourceFile[i]).Trim();
            }
        }

        #region 判断变量
        public bool IsFloat(string floatStr)
        {
            if (floatStr.Length < 2 || floatStr == null) 
            {
                return false;
            }
            if (floatStr[0] != '-' && !IsIntChar(floatStr[0]))
            {
                return false;
            }

            for (int i = 1; i < floatStr.Length; i++)
            {
                if (!IsIntChar(floatStr[i])&&floatStr[i]!='.')
                {
                    return false;
                }
            }

            return true;
        }

        public string CharArrayToString(char[] array)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; array[i] != '\0'; i++) 
            {
                builder.Append(array[i]);
            }
            return builder.ToString();
        }

        public bool IsIdent(string ident)
        {

            if (IsIntChar(ident[0]) || ident.Length < 1 || ident == null) 
            {
                return false;
            }
            for (int i = 0; i < ident.Length; i++)
            {
                if (!IsIdentChar(ident[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public string TrimComma(string currLineStr)
        {
            if (currLineStr == null) 
            {
                return null;
            }
            bool inStr = false;
            for (int i = 0; i < currLineStr.Length; i++)
            {

                if (currLineStr[i] == '\"')
                {
                    if (inStr)
                    {
                        inStr = false;
                    }
                    else
                    {
                        inStr = true;
                    }
                }

                if (!inStr)
                {
                    if (currLineStr[i] == ';')
                    {
                        return currLineStr.Substring(0, i);
                    }
                }
            }

            return currLineStr;
        }

        public string TrimSpaceWhite(string currLineStr)
        {
           
            if (currLineStr == null) 
            {
                return null;
            }
            if (currLineStr.Length < 1)
            {
                return currLineStr;
            }

            int i = 0, j = currLineStr.Length;
            while (IsWhiteSpace(currLineStr[i]))  { i++; if (i >= currLineStr.Length) break; }
            while (IsWhiteSpace(currLineStr[j])) { j--; if (j < 0) break; }
            if (i<=j)
            {
                return currLineStr.Substring(i, j - i + 1);
            }
            return "";
        }

        public bool IsInt(string num)
        {
            if (num.Length < 1 || num == null) 
            {
                return false;
            }
            if (num[0] != '-' && !IsIntChar(num[0])) 
            {
                return false;
            }   

            for (int i = 1; i < num.Length; i++)
            {
                if (!IsIntChar(num[i])) 
                {
                    return false;
                }
            }
            
            return true;
        }
        #endregion


        #region 判断单个字符
        public bool IsIntChar(Char num)
        {
            if (num >= '0' && num <= '9') 
            {
                return true;
            }
            return false;
        }

        public bool IsIdentChar(char ident)
        {
            if ((ident >= '0' && ident <= '9') ||
                (ident >= 'a' && ident <= 'z') ||
                (ident >= 'A' && ident <= 'Z') ||
                ident == '_' && ident == '$')
            {
                return true;
            }
            return false;
        }

        public bool IsSeparationChar(char c)
        {
            if (c == ',' || c == ':' || c == '\"' || c == '{' || c == '}' || c == '\t' || c == ' ' || c == '\n') 
            {
                return true;
            }
            return false;
        }

        public bool IsWhiteSpace(char c)
        {
            if (c == ' ' || c == '\t' || c == '\n') 
            {
                return true;
            }
            return false;
        }
        #endregion

        
    }
}
