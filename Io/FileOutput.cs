#region

using System;
using System.IO;
using HC.Core.Helpers;

#endregion

namespace HC.Core.Io
{
    /*
    *   Class FileOutput
    *
    *   Methods for writing doubles, floats, BigDecimals,
    *   integers, long integers, short integers, bytes,
    *   Strings, chars, booleans, Complex, ,
    *   ErrorProp and ComplexErrorProp to a text file.
    *
    *   WRITTEN BY: Dr Michael Thomas Flanagan
    *
    *   DATE:    July 2002
    *   UPDATED: 26 April 2004, 22 January 2006, 27 June 2007, 21-23 July 2007, 1 February 2009
    *
    *   DOCUMENTATION:
    *   See Michael Thomas Flanagan's Java library on-line web page:
    *   http://www.ee.ucl.ac.uk/~mflanaga/java/FileOutput.html
    *   http://www.ee.ucl.ac.uk/~mflanaga/java/
    *
    *   Copyright (c) 2002 - 2009
    *
    *   PERMISSION TO COPY:
    *
    *   Redistributions of this source code, or parts of, must retain the above
    *   copyright notice, this list of conditions and the following disclaimer.
    *
    *   Redistribution in binary form of all or parts of this class, must reproduce
    *   the above copyright, this list of conditions and the following disclaimer in
    *   the documentation and/or other materials provided with the distribution.
    *
    *   Permission to use, copy and modify this software and its documentation for
    *   NON-COMMERCIAL purposes is granted, without fee, provided that an acknowledgement
    *   to the author, Michael Thomas Flanagan at www.ee.ucl.ac.uk/~mflanaga, appears in all
    *   copies and associated documentation or publications.
    *
    *   Dr Michael Thomas Flanagan makes no representations about the suitability
    *   or fitness of the software for any or for a particular purpose.
    *   Michael Thomas Flanagan shall not be liable for any damages suffered
    *   as a result of using, modifying or distributing this software or its derivatives.
    *
    ***************************************************************************************/

    public class FileOutput
    {
        #region Members

        // Instance variables
        protected char m_app = 'w'; // 'w' new file - overwrites an existing file of the same name
        protected bool m_append; // true data appended to a file, false new file
        protected bool m_fileExists = true; // = false if file of output filename does not already exist
        protected string m_filename = ""; // output file name
        protected StreamReader m_fwoutput; // instance of StreamReader
        protected StreamWriter m_output; // instance of StreamWriter

        #endregion

        #region Constructors

        // Constructors
        public FileOutput(string filename, char app)
        {
            m_filename = filename;
            m_app = app;
            setFilenames(filename, app);
        }

        public FileOutput(string filename, string apps)
        {
            m_filename = filename;
            m_app = apps[0];
            setFilenames(filename, m_app);
        }

        public FileOutput(string filename)
        {
            m_filename = filename;
            m_app = 'w';
            setFilenames(filename, m_app);
        }

        #endregion

        // Methods

        // Set the file names
        private void setFilenames(string filename, char app)
        {
            StreamReader input0;
            try
            {
                input0 = new StreamReader(filename);
            }
            catch
            {
                m_fileExists = false;
            }

            if (app == 'n')
            {
                var test = true;
                var i = 0;
                StreamReader input;
                var ext = "";
                var filename0 = "";

                var idot = filename.IndexOf('.');
                if (idot != -1)
                {
                    ext += filename.Substring(idot);
                    filename0 += filename.Substring(0, idot);
                }
                else
                {
                    filename0 += filename;
                }

                while (test)
                {
                    i++;
                    filename = filename0 + i + ext;
                    try
                    {
                        input = new StreamReader(filename);
                    }
                    catch
                    {
                        test = false;
                        m_filename = filename;
                    }
                }
            }

            if (app == 'a')
            {
                m_append = true;
            }
            else
            {
                m_append = false;
            }
            try
            {
                m_fwoutput = new StreamReader(filename, m_append);
            }
            catch (IOException e)
            {
                PrintToScreen.WriteLine(e.Message);
            }

            m_output = new StreamWriter(m_fwoutput.BaseStream);
        }

        // Return filename
        public string getFilename()
        {
            return m_filename;
        }

        // Return file already exists check
        // returns true if it does, false if it does not
        public bool checkFileAlreadyExists()
        {
            return m_fileExists;
        }


        // PRINT WITH NO FOLLOWING SPACE OR CHARACTER AND NO LINE RETURN

        // Prints character, no line return
        public void print(char ch)
        {
            m_output.Write(ch);
        }

        // Prints character, no line return, fixed field Length
        public void print(char ch, int f)
        {
            var ss = "";
            ss = ss + ch;
            ss = setField(ss, f);
            m_output.Write(ss);
        }

        // Prints string, no line return
        public void print(string word)
        {
            m_output.Write(word);
        }


        // Prints string, no line return, fixed field Length
        public void print(string word, int f)
        {
            var ss = "";
            ss = ss + word;
            ss = setField(ss, f);
            m_output.Write(ss);
        }

        // Prints double, no line return
        public void print(double dd)
        {
            m_output.Write(dd);
        }

        // Prints double, no line return, fixed field Length
        public void print(double dd, int f)
        {
            var ss = "";
            ss = ss + dd;
            ss = setField(ss, f);
            m_output.Write(ss);
        }

        // Prints float, no line return
        public void print(float ff)
        {
            m_output.Write(ff);
        }

        // Prints float, no line return, fixed field Length
        public void print(float ff, int f)
        {
            var ss = "";
            ss = ss + ff;
            ss = setField(ss, f);
            m_output.Write(ss);
        }


        // Prints int, no line return
        public void print(int big)
        {
            m_output.Write(big.ToString());
        }

        // Prints int big, no line return, fixed field Length
        public void print(int big, int f)
        {
            var ss = "";
            ss = ss + big;
            ss = setField(ss, f);
            m_output.Write(ss);
        }


        // Prints long integer, no line return
        public void print(long ll)
        {
            m_output.Write(ll);
        }

        // Prints long integer, no line return, fixed field Length
        public void print(long ll, int f)
        {
            var ss = "";
            ss = ss + ll;
            ss = setField(ss, f);
            m_output.Write(ss);
        }

        // Prints short integer, no line return
        public void print(short ss)
        {
            m_output.Write(ss);
        }

        // Prints short integer, no line return, fixed field Length
        public void print(short si, int f)
        {
            var ss = "";
            ss = ss + si;
            ss = setField(ss, f);
            m_output.Write(ss);
        }

        // Prints byte integer, no line return
        public void print(byte by)
        {
            m_output.Write(by);
        }

        // Prints short integer, no line return, fixed field Length
        public void print(byte by, int f)
        {
            var ss = "";
            ss = ss + by;
            ss = setField(ss, f);
            m_output.Write(ss);
        }

        // Prints bool, no line return
        public void print(bool bb)
        {
            m_output.Write(bb);
        }

        // Prints bool, no line return, fixed field Length
        public void print(bool bb, int f)
        {
            var ss = "";
            ss = ss + bb;
            ss = setField(ss, f);
            m_output.Write(ss);
        }

        // Prints array of doubles, no line return
        public void print(double[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
            }
        }

        // Prints array of floats, no line return
        public void print(float[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
            }
        }

        // Prints array of int, no line return
        public void print(int[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
            }
        }

        // Prints array of short, no line return
        public void print(short[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
            }
        }

        // Prints array of byte, no line return
        public void print(byte[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
            }
        }

        // Prints array of char, no line return
        public void print(char[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
            }
        }

        // Prints array of bool, no line return
        public void print(bool[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
            }
        }

        // Prints array of Strings, no line return
        public void print(string[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
            }
        }


        // Prints array of doubles, no line return, fixed field Length
        public void print(double[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints array of floats, no line return, fixed field Length
        public void print(float[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints array of int, no line return, fixed field Length
        public void print(int[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints array of long, no line return, fixed field Length
        public void print(long[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints array of short, no line return, fixed field Length
        public void print(short[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints array of byte, no line return, fixed field Length
        public void print(byte[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints array of char, no line return, fixed field Length
        public void print(char[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints array of bool, no line return, fixed field Length
        public void print(bool[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints array of Strings, no line return, fixed field Length
        public void print(string[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints date and time  (no line return);
        public void dateAndTime()
        {
            var d = new DateTime();
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
        }

        // Prints file title (title), date and time  (no line return);
        public void dateAndTime(string title)
        {
            var d = new DateTime();
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file, " + title + ", was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
        }

        // PRINT WITH SPACE (NO LINE RETURN)
        // Prints character plus space, no line return
        public void printsp(char ch)
        {
            m_output.Write(ch);
            m_output.Write(" ");
        }

        // Prints string plus space, no line return
        public void printsp(string word)
        {
            m_output.Write(word + " ");
        }

        // Prints double plus space, no line return
        public void printsp(double dd)
        {
            m_output.Write(dd);
            m_output.Write(" ");
        }

        // Prints float plus space, no line return
        public void printsp(float ff)
        {
            m_output.Write(ff);
            m_output.Write(" ");
        }


        // Prints int plus space, no line return
        public void printsp(int big)
        {
            m_output.Write(big.ToString());
            m_output.Write(" ");
        }

        // Prints long integer plus space, no line return
        public void printsp(long ll)
        {
            m_output.Write(ll);
            m_output.Write(" ");
        }

        // Prints short integer plus space, no line return
        public void printsp(short ss)
        {
            m_output.Write(ss);
            m_output.Write(" ");
        }

        // Prints byte integer plus space, no line return
        public void printsp(byte by)
        {
            m_output.Write(by);
            m_output.Write(" ");
        }

        // Prints bool plus space, no line return
        public void printsp(bool bb)
        {
            m_output.Write(bb);
            m_output.Write(" ");
        }

        // Prints  space, no line return
        public void printsp()
        {
            m_output.Write(" ");
        }

        // Prints array of doubles, separated by spaces
        public void printsp(double[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }

        // Prints array of floats, separated by spaces
        public void printsp(float[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }

        // Prints array of int, separated by spaces
        public void printsp(int[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }

        // Prints array of long, separated by spaces
        public void printsp(long[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }

        // Prints array of char, separated by spaces
        public void printsp(char[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }

        // Prints array of short, separated by spaces
        public void printsp(short[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }

        // Prints array of byte, separated by spaces
        public void printsp(byte[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }

        // Prints array of bool, separated by spaces
        public void printsp(bool[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }

        // Prints array of Strings, separated by spaces
        public void printsp(string[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }


        // Prints date and time (plus space, no line return);
        public void dateAndTimesp()
        {
            var d = new DateTime();
            var day = d.DayOfWeek.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
            m_output.Write(" ");
        }

        // Prints file title (title), date and time  (no line return);
        public void dateAndTimesp(string title)
        {
            var d = DateTime.Today;
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file, " + title + ", was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
            m_output.Write(" ");
        }

        // PRINT WITH LINE RETURN
        // Prints character with line return
        public void println(char ch)
        {
            m_output.WriteLine(ch);
        }

        // Prints string with line return
        public void println(string word)
        {
            m_output.WriteLine(word);
        }

        // Prints double with line return
        public void println(double dd)
        {
            m_output.WriteLine(dd);
        }

        // Prints float with line return
        public void println(float ff)
        {
            m_output.WriteLine(ff);
        }

        // Prints int with line return
        public void println(int big)
        {
            m_output.WriteLine(big.ToString());
        }

        // Prints long integer with line return
        public void println(long ll)
        {
            m_output.WriteLine(ll);
        }

        // Prints short integer with line return
        public void println(short ss)
        {
            m_output.WriteLine(ss);
        }

        // Prints byte integer with line return
        public void println(byte by)
        {
            m_output.WriteLine(by);
        }

        // Prints bool with line return
        public void println(bool bb)
        {
            m_output.WriteLine(bb);
        }

        // Prints  line return
        public void println()
        {
            m_output.WriteLine("");
        }

        // Prints array of doubles, each followed by a line return
        public void println(double[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }

        // Prints array of floats, each followed by a line return
        public void println(float[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }

        // Prints array of int, each followed by a line return
        public void println(int[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }

        // Prints array of long, each followed by a line return
        public void println(long[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }

        // Prints array of short, each followed by a line return
        public void println(short[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }

        // Prints array of byte, each followed by a line return
        public void println(byte[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }

        // Prints array of char, each followed by a line return
        public void println(char[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }

        // Prints array of bool, each followed by a line return
        public void println(bool[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }

        // Prints array of Strings, each followed by a line return
        public void println(string[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }


        // Prints date and time as date-month-year hour:minute:second (with line return);
        public void dateAndTimeln()
        {
            var d = new DateTime();
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file, " + m_filename + ", was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.WriteLine(day);
        }

        // Prints file title (title), date and time (with line return);
        public void dateAndTimeln(string title)
        {
            var d = new DateTime();
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file, " + title + ", was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.WriteLine(day);
        }

        // PRINT WITH FOLLOWING TAB, NO LINE RETURN
        // Prints character plus tab, no line return
        public void printtab(char ch)
        {
            m_output.Write(ch);
            m_output.Write("\t");
        }

        // Prints character plus tab, no line return, fixed field Length
        public void printtab(char ch, int f)
        {
            var ss = "";
            ss = ss + ch;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints string plus tab, no line return
        public void printtab(string word)
        {
            m_output.Write(word + "\t");
        }

        // Prints string plus tab, no line return, fixed field Length
        public void printtab(string word, int f)
        {
            var ss = "";
            ss = ss + word;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints double plus tab, no line return
        public void printtab(double dd)
        {
            m_output.Write(dd);
            m_output.Write("\t");
        }

        // Prints double plus tab, fixed field Length, fixed field Length
        public void printtab(double dd, int f)
        {
            var ss = "";
            ss = ss + dd;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints float plus tab, no line return
        public void printtab(float ff)
        {
            m_output.Write(ff);
            m_output.Write("\t");
        }

        // Prints float plus tab, no line return, fixed field Length
        public void printtab(float ff, int f)
        {
            var ss = "";
            ss = ss + ff;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints int plus tab, no line return
        public void printtab(int big)
        {
            m_output.Write(big.ToString());
            m_output.Write("\t");
        }

        // Prints int plus tab, no line return, fixed field Length
        public void printtab(int big, int f)
        {
            var ss = "";
            ss = ss + big;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints long integer plus tab, no line return
        public void printtab(long ll)
        {
            m_output.Write(ll);
            m_output.Write("\t");
        }

        // Prints long integer plus tab, no line return, fixed field Length
        public void printtab(long ll, int f)
        {
            var ss = "";
            ss = ss + ll;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints short integer plus tab, no line return
        public void printtab(short ss)
        {
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints short integer plus tab, no line return, fixed field Length
        public void printtab(short si, int f)
        {
            var ss = "";
            ss = ss + si;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints byte integer plus tab, no line return
        public void printtab(byte by)
        {
            m_output.Write(by);
            m_output.Write("\t");
        }

        // Prints byte integer plus tab, no line return, fixed field Length
        public void printtab(byte by, int f)
        {
            var ss = "";
            ss = ss + by;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints bool plus tab, no line return
        public void printtab(bool bb)
        {
            m_output.Write(bb);
            m_output.Write("\t");
        }

        // Prints bool plus tab, no line return, fixed field Length
        public void printtab(bool bb, int f)
        {
            var ss = "";
            ss = ss + bb;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints tab, no line return
        public void printtab()
        {
            m_output.Write("\t");
        }

        // Prints array of doubles, tab, no line return, fixed field Length
        public void printtab(double[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }

        // Prints array of floats, tab, no line return, fixed field Length
        public void printtab(float[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }

        // Prints array of int, tab, no line return, fixed field Length
        public void printtab(int[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }


        // Prints array of long, tab, no line return, fixed field Length
        public void printtab(long[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }


        // Prints array of short, tab, no line return, fixed field Length
        public void printtab(short[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }

        // Prints array of byte, tab, no line return, fixed field Length
        public void printtab(byte[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }

        // Prints array of char, tab, no line return, fixed field Length
        public void printtab(char[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }

        // Prints array of bool, tab, no line return, fixed field Length
        public void printtab(bool[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }

        // Prints array of Strings, tab, no line return, fixed field Length
        public void printtab(string[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }

        // Prints array of doubles, tab, no line return
        public void printtab(double[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write("\t");
            }
        }

        // Prints array of floats, tab, no line return
        public void printtab(float[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write("\t");
            }
        }

        // Prints array of int, tab, no line return
        public void printtab(int[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i].ToString());
                m_output.Write("\t");
            }
        }

        // Prints array of long, tab, no line return
        public void printtab(long[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write("\t");
            }
        }

        // Prints array of char, tab, no line return
        public void printtab(char[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write("\t");
            }
        }

        // Prints array of bool, tab, no line return
        public void printtab(bool[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write("\t");
            }
        }

        // Prints array of Strings, tab, no line return
        public void printtab(string[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write("\t");
            }
        }

        // Prints date and time (plus tab, no line return);
        public void dateAndTimetab()
        {
            var d = new DateTime();
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
            m_output.Write("\t");
        }

        // Prints file title (title), date and time (plus tab, no line return);
        public void dateAndTimetab(string title)
        {
            var d = new DateTime();
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file, " + title + ", was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
            m_output.Write("\t");
        }

        // PRINT FOLLOWED BY A COMMA, NO LINE RETURN
        // Prints character plus comma, no line return
        public void printcomma(char ch)
        {
            m_output.Write(ch);
            m_output.Write(",");
        }

        // Prints string plus comma, no line return
        public void printcomma(string word)
        {
            m_output.Write(word + ",");
        }

        // Prints double plus comma, no line return
        public void printcomma(double dd)
        {
            m_output.Write(dd);
            m_output.Write(",");
        }

        // Prints float plus comma, no line return
        public void printcomma(float ff)
        {
            m_output.Write(ff);
            m_output.Write(",");
        }


        // Prints int plus comma, no line return
        public void printcomma(int big)
        {
            m_output.Write(big.ToString());
            m_output.Write(",");
        }

        // Prints long integer plus comma, no line return
        public void printcomma(long ll)
        {
            m_output.Write(ll);
            m_output.Write(",");
        }

        // Prints bool plus comma, no line return
        public void printcomma(bool bb)
        {
            m_output.Write(bb);
            m_output.Write(",");
        }

        // Prints short integer plus comma, no line return
        public void printcomma(short ss)
        {
            m_output.Write(ss);
            m_output.Write(",");
        }

        // Prints byte integer plus comma, no line return
        public void printcomma(byte by)
        {
            m_output.Write(by);
            m_output.Write(",");
        }

        // Prints comma, no line return
        public void printcomma()
        {
            m_output.Write(",");
        }

        // Prints array of doubles, each separated by a comma
        public void printcomma(double[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(",");
            }
        }

        // Prints array of floats, each separated by a comma
        public void printcomma(float[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(",");
            }
        }

        // Prints array of int, each separated by a comma
        public void printcomma(int[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i].ToString());
                m_output.Write(",");
            }
        }

        // Prints array of long, each separated by a comma
        public void printcomma(long[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(",");
            }
        }

        // Prints array of short, each separated by a comma
        public void printcomma(short[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(",");
            }
        }

        // Prints array of byte, each separated by a comma
        public void printcomma(byte[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(",");
            }
        }

        // Prints array of char, each separated by a comma
        public void printcomma(char[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(",");
            }
        }

        // Prints array of bool, each separated by a comma
        public void printcomma(bool[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(",");
            }
        }

        // Prints array of Strings, each separated by a comma
        public void printcomma(string[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(",");
            }
        }


        // Prints date and time (plus comma, no line return);
        public void dateAndTimecomma()
        {
            var d = new DateTime();

            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
            m_output.Write(",");
        }

        // Prints file title (title), date and time (plus comma, no line return);
        public void dateAndTimecomma(string title)
        {
            var d = new DateTime();
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file, " + title + ", was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
            m_output.Write(",");
        }

        // PRINT FOLLOWED BY A SEMICOLON, NO LINE RETURN
        // Prints character plus semicolon, no line return
        public void printsc(char ch)
        {
            m_output.Write(ch);
            m_output.Write(";");
        }

        // Prints string plus semicolon, no line return
        public void printsc(string word)
        {
            m_output.Write(word + ";");
        }

        // Prints double plus semicolon, no line return
        public void printsc(double dd)
        {
            m_output.Write(dd);
            m_output.Write(";");
        }

        // Prints float plus semicolon, no line return
        public void printsc(float ff)
        {
            m_output.Write(ff);
            m_output.Write(";");
        }

        // Prints int plus semicolon, no line return
        public void printsc(int big)
        {
            m_output.Write(big.ToString());
            m_output.Write(";");
        }

        // Prints long integer plus semicolon, no line return
        public void printsc(long ll)
        {
            m_output.Write(ll);
            m_output.Write(";");
        }

        // Prints short integer plus semicolon, no line return
        public void printsc(short ss)
        {
            m_output.Write(ss);
            m_output.Write(";");
        }

        // Prints byte integer plus semicolon, no line return
        public void printsc(byte by)
        {
            m_output.Write(by);
            m_output.Write(";");
        }

        // Prints bool plus semicolon, no line return
        public void printsc(bool bb)
        {
            m_output.Write(bb);
            m_output.Write(";");
        }

        // Prints  semicolon, no line return
        public void printsc()
        {
            m_output.Write(";");
        }

        // Prints array of doubles, each separated by a semicolon
        public void printsc(double[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(";");
            }
        }

        // Prints array of floats, each separated by a semicolon
        public void printsc(float[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(";");
            }
        }

        // Prints array of int, each separated by a semicolon
        public void printsc(int[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i].ToString());
                m_output.Write(";");
            }
        }

        // Prints array of long, each separated by a semicolon
        public void printsc(long[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(";");
            }
        }

        // Prints array of short, each separated by a semicolon
        public void printsc(short[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(";");
            }
        }

        // Prints array of byte, each separated by a semicolon
        public void printsc(byte[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(";");
            }
        }

        // Prints array of char, each separated by a semicolon
        public void printsc(char[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(";");
            }
        }

        // Prints array of bool, each separated by a semicolon
        public void printsc(bool[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(";");
            }
        }

        // Prints array of Strings, each separated by a semicolon
        public void printsc(string[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(";");
            }
        }

        // Prints date and time (plus semicolon, no line return);
        public void dateAndTimesc()
        {
            var d = new DateTime();
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
            m_output.Write(";");
        }

        // Prints file title (title), date and time (plus semicolon, no line return);
        public void dateAndTimesc(string title)
        {
            var d = new DateTime();
            var day = d.Day.ToString();
            var tim = d.TimeOfDay.ToString();

            m_output.Write("This file, " + title + ", was created at ");
            m_output.Write(tim);
            m_output.Write(" on ");
            m_output.Write(day);
            m_output.Write(";");
        }

        // Close file
        public void close()
        {
            m_output.Close();
        }

        // Print a 2-D array of doubles to a text file, no file title provided
        public static void printArrayToText(double[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }

        // Print a 1-D array of doubles to a text file, no file title provided
        public static void printArrayToText(double[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 1-D array of doubles to a text file, file title provided
        public static void printArrayToText(string title, double[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i]);
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }

        // Print a 2-D array of floats to a text file, no file title provided
        public static void printArrayToText(float[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 2-D array of floats to a text file, file title provided
        public static void printArrayToText(string title, float[,] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.GetLength(0);
            var ncol = 0;
            for (var i = 0; i < nrow; i++)
            {
                ncol = array.GetLength(1);
                for (var j = 0; j < ncol; j++)
                {
                    fo.printtab(array[i, j]);
                }
                fo.println();
            }
            fo.println("End of file.");
            fo.close();
        }


        // Print a 1-D array of floats to a text file, no file title provided
        public static void printArrayToText(float[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 1-D array of float to a text file, file title provided
        public static void printArrayToText(string title, float[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i]);
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }

        // Print a 2-D array of double to a text file, file title provided
        public static void printArrayToText(string title, double[,] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.GetLength(0);
            var ncol = 0;
            for (var i = 0; i < nrow; i++)
            {
                ncol = array.GetLength(1);
                for (var j = 0; j < ncol; j++)
                {
                    fo.printtab(array[i, j].ToString());
                }
                fo.println();
            }
            fo.println("End of file.");
            fo.close();
        }

        // Print a 2-D array of int to a text file, no file title provided
        public static void printArrayToText(int[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }

        // Print a 1-D array of int to a text file, no file title provided
        public static void printArrayToText(int[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }

        // Print a 1-D array of int to a text file, file title provided
        public static void printArrayToText(string title, int[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i].ToString());
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }


        // Print a 2-D array of ints to a text file, file title provided
        public static void printArrayToText(string title, int[,] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.GetLength(0);
            var ncol = 0;
            for (var i = 0; i < nrow; i++)
            {
                ncol = array.GetLength(1);
                for (var j = 0; j < ncol; j++)
                {
                    fo.printtab(array[i, j]);
                }
                fo.println();
            }
            fo.println("End of file.");
            fo.close();
        }

        // Print a 2-D array of longs to a text file, no file title provided
        public static void printArrayToText(long[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 2-D array of longs to a text file, file title provided
        public static void printArrayToText(string title, long[,] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.GetLength(0);
            var ncol = 0;
            for (var i = 0; i < nrow; i++)
            {
                ncol = array.GetLength(1);
                for (var j = 0; j < ncol; j++)
                {
                    fo.printtab(array[i, j]);
                }
                fo.println();
            }
            fo.println("End of file.");
            fo.close();
        }


        // Print a 1-D array of longs to a text file, no file title provided
        public static void printArrayToText(long[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 1-D array of long to a text file, file title provided
        public static void printArrayToText(string title, long[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i]);
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }

        // Print a 2-D array of shorts to a text file, no file title provided
        public static void printArrayToText(short[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 2-D array of shorts to a text file, file title provided
        public static void printArrayToText(string title, short[,] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.GetLength(0);
            var ncol = 0;
            for (var i = 0; i < nrow; i++)
            {
                ncol = array.GetLength(1);
                for (var j = 0; j < ncol; j++)
                {
                    fo.printtab(array[i, j]);
                }
                fo.println();
            }
            fo.println("End of file.");
            fo.close();
        }

        // Print a 1-D array of shorts to a text file, no file title provided
        public static void printArrayToText(short[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 1-D array of short to a text file, file title provided
        public static void printArrayToText(string title, short[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i]);
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }


        // Print a 2-D array of bytes to a text file, no file title provided
        public static void printArrayToText(byte[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 2-D array of bytes to a text file, file title provided
        public static void printArrayToText(string title, byte[,] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.GetLength(0);
            var ncol = 0;
            for (var i = 0; i < nrow; i++)
            {
                ncol = array.GetLength(1);
                for (var j = 0; j < ncol; j++)
                {
                    fo.printtab(array[i, j]);
                }
                fo.println();
            }
            fo.println("End of file.");
            fo.close();
        }


        // Print a 1-D array of bytes to a text file, no file title provided
        public static void printArrayToText(byte[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 1-D array of byte to a text file, file title provided
        public static void printArrayToText(string title, byte[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i]);
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }


        // Print a 2-D array of Strings to a text file, no file title provided
        public static void printArrayToText(string[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 2-D array of Strings to a text file, file title provided
        public static void printArrayToText(string title, string[,] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.GetLength(0);
            var ncol = 0;
            for (var i = 0; i < nrow; i++)
            {
                ncol = array.GetLength(1);
                for (var j = 0; j < ncol; j++)
                {
                    fo.printtab(array[i, j]);
                }
                fo.println();
            }
            fo.println("End of file.");
            fo.close();
        }


        // Print a 1-D array of Strings to a text file, no file title provided
        public static void printArrayToText(string[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 1-D array of string to a text file, file title provided
        public static void printArrayToText(string title, string[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i]);
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }

        // Print a 2-D array of chars to a text file, no file title provided
        public static void printArrayToText(char[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 2-D array of chars to a text file, file title provided
        public static void printArrayToText(string title, char[,] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.GetLength(0);
            var ncol = 0;
            for (var i = 0; i < nrow; i++)
            {
                ncol = array.GetLength(1);
                for (var j = 0; j < ncol; j++)
                {
                    fo.printtab(array[i, j]);
                }
                fo.println();
            }
            fo.println("End of file.");
            fo.close();
        }


        // Print a 1-D array of chars to a text file, no file title provided
        public static void printArrayToText(char[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 1-D array of char to a text file, file title provided
        public static void printArrayToText(string title, char[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i]);
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }

        // Print a 2-D array of booleans to a text file, no file title provided
        public static void printArrayToText(bool[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }

        private static void printArrayToText(string title, bool[,] array)
        {
            throw new NotImplementedException();
        }


        // Print a 1-D array of booleans to a text file, no file title provided
        public static void printArrayToText(bool[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }


        // Print a 1-D array of bool to a text file, file title provided
        public static void printArrayToText(string title, bool[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i]);
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }

        protected static string setField(string ss, int f)
        {
            var sp = ' ';
            var n = ss.Length;
            if (f > n)
            {
                for (var i = n + 1; i <= f; i++)
                {
                    ss = ss + sp;
                }
            }
            return ss;
        }

        // Prints object, no line return, fixed field Length
        public void print(object ff, int f)
        {
            var ss = "";
            ss = ss + ff;
            ss = setField(ss, f);
            m_output.Write(ss);
        }

        // Prints object, no line return
        public void print(object ff)
        {
            m_output.Write(ff.ToString());
        }

        // Prints array of object, no line return
        public void print(object[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
            }
        }

        // Prints array of object, no line return, fixed field Length
        public void print(object[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
            }
        }

        // Prints object plus space, no line return
        public void printsp(object ff)
        {
            m_output.Write(ff.ToString());
            m_output.Write(" ");
        }

        // Prints array of object, separated by spaces
        public void printsp(object[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(" ");
            }
        }

        // Prints object with line return
        public void println(object ff)
        {
            m_output.WriteLine(ff.ToString());
        }

        // Prints array of object, each followed by a line return
        public void println(object[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.WriteLine(array[i]);
            }
        }


        // Prints object plus tab, no line return
        public void printtab(object ff)
        {
            m_output.Write(ff.ToString());
            m_output.Write("\t");
        }

        // Prints object plus tab, no line return, fixed field Length
        public void printtab(object ff, int f)
        {
            var ss = "";
            ss = ss + ff;
            ss = setField(ss, f);
            m_output.Write(ss);
            m_output.Write("\t");
        }

        // Prints array of object, tab, no line return, fixed field Length
        public void printtab(object[] array, int f)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                var ss = "";
                ss = ss + array[i];
                ss = setField(ss, f);
                m_output.Write(ss);
                m_output.Write("\t");
            }
        }

        // Prints array of object, tab, no line return
        public void printtab(object[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write("\t");
            }
        }

        // Prints array of object, tab, no line return
        // Prints object plus comma, no line return
        public void printcomma(object ff)
        {
            m_output.Write(ff.ToString());
            m_output.Write(",");
        }

        // Prints array of object, each separated by a comma
        public void printcomma(object[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(",");
            }
        }

        // Prints object plus semicolon, no line return
        public void printsc(object ff)
        {
            m_output.Write(ff.ToString());
            m_output.Write(";");
        }

        // Prints array of object, each separated by a semicolon
        public void printsc(object[] array)
        {
            var n = array.Length;
            for (var i = 0; i < n; i++)
            {
                m_output.Write(array[i]);
                m_output.Write(";");
            }
        }

        // Print a 2-D array of object to a text file, no file title provided
        public static void printArrayToText(object[,] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }

        // Print a 1-D array of object to a text file, no file title provided
        public static void printArrayToText(object[] array)
        {
            var title = "ArrayToText.txt";
            printArrayToText(title, array);
        }

        // Print a 2-D array of object to a text file, file title provided
        public static void printArrayToText(string title, object[,] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.GetLength(0);
            var ncol = 0;
            for (var i = 0; i < nrow; i++)
            {
                ncol = array.GetLength(1);
                for (var j = 0; j < ncol; j++)
                {
                    fo.printtab(array[i, j].ToString());
                }
                fo.println();
            }
            fo.println("End of file.");
            fo.close();
        }


        // Print a 1-D array of object to a text file, file title provided
        public static void printArrayToText(string title, object[] array)
        {
            var fo = new FileOutput(title, 'n');
            fo.dateAndTimeln(title);
            var nrow = array.Length;
            for (var i = 0; i < nrow; i++)
            {
                fo.printtab(array[i].ToString());
            }
            fo.println();
            fo.println("End of file.");
            fo.close();
        }
    }
}


