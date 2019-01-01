#region

using System;
using System.Diagnostics;
using System.Text;

#endregion

namespace HC.Core.Helpers
{
    /*
    *   Class   PrintToScreen
    *
    *   USAGE:  Methods for writing one and two dimensional arrays to the sceen
    *
    *   WRITTEN BY: Dr Michael Thomas Flanagan
    *
    *   DATE:       13  April 2008  (Most methods taken from existing classes to make a separate print to screen class)
    *   AMENDED:    11 August 2008, 14 September 2008
    *
    *   DOCUMENTATION:
    *   See Michael Thomas Flanagan's Java library on-line web pages:
    *   http://www.ee.ucl.ac.uk/~mflanaga/java/PrintToScreen.html
    *   http://www.ee.ucl.ac.uk/~mflanaga/java/
    *
    *   Copyright (c) 2008 Michael Thomas Flanagan
    *
    *   PERMISSION TO COPY:
    *
    * Permission to use, copy and modify this software and its documentation for NON-COMMERCIAL purposes is granted, without fee,
    * provided that an acknowledgement to the author, Dr Michael Thomas Flanagan at www.ee.ucl.ac.uk/~mflanaga, appears in all copies
    * and associated documentation or publications.
    *
    * Redistributions of the source code of this source code, or parts of the source codes, must retain the above copyright notice, this list of conditions
    * and the following disclaimer and requires written permission from the Michael Thomas Flanagan:
    *
    * Redistribution in binary form of all or parts of this class must reproduce the above copyright notice, this list of conditions and
    * the following disclaimer in the documentation and/or other materials provided with the distribution and requires written permission from the Michael Thomas Flanagan:
    *
    * Dr Michael Thomas Flanagan makes no representations about the suitability or fitness of the software for any or for a particular purpose.
    * Dr Michael Thomas Flanagan shall not be liable for any damages suffered as a result of using, modifying or distributing this software
    * or its derivatives.
    *
    ***************************************************************************************/

    public class PrintToScreen
    {
        #region Members

        private static readonly StringBuilder m_sb = new StringBuilder();

        #endregion

        // 1D ARRAYS

        // print an array of doubles to screen
        // No line returns except at the end
        public static void print(double[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        public static void print(decimal[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of doubles to screen with truncation
        // No line returns except at the end
        public static void print(double[] aa, int trunc)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(NumberHelper.truncate(aa[i], trunc) + "   ");
            }
            WriteLine();
        }

        // print an array of doubles to screen
        // with line returns
        public static void println(double[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }

        public static void println(decimal[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }

        // print an array of doubles to screen with truncation
        // with line returns
        public static void println(double[] aa, int trunc)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(NumberHelper.truncate(aa[i], trunc) + "   ");
            }
        }

        // print an array of floats to screen
        // No line returns except at the end
        public static void print(float[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of floats to screen with truncation
        // No line returns except at the end
        public static void print(float[] aa, int trunc)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(NumberHelper.truncate(aa[i], trunc) + "   ");
            }
            WriteLine();
        }

        // print an array of floats to screen
        // with line returns
        public static void println(float[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }

        // print an array of floats to screen with truncation
        // with line returns
        public static void println(float[] aa, int trunc)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(NumberHelper.truncate(aa[i], trunc) + "   ");
            }
        }

        // print an array of ints to screen
        // No line returns except at the end
        public static void print(int[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of ints to screen
        // with line returns
        public static void println(int[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }

        // print an array of longs to screen
        // No line returns except at the end
        public static void print(long[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of longs to screen
        // with line returns
        public static void println(long[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }

        // print an array of shorts to screen
        // No line returns except at the end
        public static void print(short[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of shorts to screen
        // with line returns
        public static void println(short[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }

        // print an array of char to screen
        // No line returns except at the end
        public static void print(char[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of char to screen
        // with line returns
        public static void println(char[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }


        // print an array of bytes to screen
        // No line returns except at the end
        public static void print(byte[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of bytes to screen
        // with line returns
        public static void println(byte[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }


        // print an array of String to screen
        // No line returns except at the end
        public static void print(string[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of Strings to screen
        // with line returns
        public static void println(string[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }

        // print an array of object to screen
        // No line returns except at the end
        public static void print(object[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of object to screen
        // with line returns
        public static void println(object[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }


        // print an array of bool to screen
        // No line returns except at the end
        public static void print(bool[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                Write(aa[i] + "   ");
            }
            WriteLine();
        }

        // print an array of bool to screen
        // with line returns
        public static void println(bool[] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                WriteLine(aa[i] + "   ");
            }
        }


        // 2D ARRAYS

        // print a 2D array of doubles to screen
        public static void print(double[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(
                    ArrayHelper.GetRowCopy(aa, i));
            }
        }

        // print a 2D array of doubles to screen with truncation
        public static void print(double[,] aa, int trunc)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(
                    ArrayHelper.GetRowCopy(aa, i), trunc);
            }
        }

        // print a 2D array of floats to screen
        public static void print(float[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i));
            }
        }

        // print a 2D array of floats to screen with truncation
        public static void print(float[,] aa, int trunc)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i), trunc);
            }
        }

        // print a 2D array of ints to screen
        public static void print(int[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i));
            }
        }

        // print a 2D array of longs to screen
        public static void print(long[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i));
            }
        }

        // print a 2D array of chars to screen
        public static void print(char[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i));
            }
        }

        // print a 2D array of bytes to screen
        public static void print(byte[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i));
            }
        }

        // print a 2D array of shorts to screen
        public static void print(short[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i));
            }
        }


        // print a 2D array of Strings to screen
        public static void print(string[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i));
            }
        }

        // print a 2D array of object to screen
        public static void print(object[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                for (var j = 0; j < aa.GetLength(1); j++)
                {
                    print(aa);
                }
            }
        }

        // print a 2D array of object to screen with truncation
        public static void print(object[,] aa, int trunc)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i));
            }
        }

        // print a 2D array of Phasor to screen

        // print a 2D array of bool to screen
        public static void print(bool[,] aa)
        {
            for (var i = 0; i < aa.GetLength(0); i++)
            {
                PrintToScreen.print(ArrayHelper.GetRowCopy(aa, i));
            }
        }

        public static void Write()
        {
            Write("");
        }

        public static void Write(string strLine)
        {
            Console.Write(strLine);
            Debug.Write(strLine);
            m_sb.Append(strLine);
        }

        public static void WriteLine(
            string strLine1,
            string strLine2)
        {
            Write(strLine1);
            Write(" ");
            Write(strLine2);
            WriteLine();
        }

        public static void WriteLine(string strLine, object arg)
        {
            Console.WriteLine(strLine, arg);
            Debug.WriteLine(strLine, arg.ToString());
            m_sb.AppendLine(strLine);
        }

        public static void WriteLine(object oLine)
        {
            WriteLine(oLine.ToString());
        }

        public static void WriteLine(string strLine)
        {
            Console.WriteLine(strLine);
            Debug.WriteLine(strLine);
            m_sb.AppendLine(strLine);
        }

        public static void WriteLine()
        {
            WriteLine("");
        }
    }
}


