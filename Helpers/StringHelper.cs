using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HC.Core.Exceptions;
using HC.Core.Logging;
using HC.Core.Text;
using NUnit.Framework;

namespace HC.Core.Helpers
{
    public static class StringHelper
    {
        public static readonly string[] EnglishAlphabet = new []
                                                              {
                                                                  "a",
                                                                  "b",
                                                                  "c",
                                                                  "d",
                                                                  "e",
                                                                  "f",
                                                                  "g",
                                                                  "h",
                                                                  "i",
                                                                  "j",
                                                                  "k",
                                                                  "l",
                                                                  "m",
                                                                  "n",
                                                                  "o",
                                                                  "p",
                                                                  "q",
                                                                  "r",
                                                                  "s",
                                                                  "t",
                                                                  "u",
                                                                  "v",
                                                                  "w",
                                                                  "x",
                                                                  "y",
                                                                  "z"
                                                              };


        public static string[,] LoadArr(List<string[]> list)
        {
            var intCols = list[0].Length;
            var result = new string[list.Count, intCols];
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < intCols; j++)
                {
                    result[i, j] = list[i][j];
                }
            }
            return result;
        }

        public static void FormatNumbers(string[,] rows)
        {
            try
            {
                for (int i = 0; i < rows.GetLength(0); i++)
                {
                    for (int j = 0; j < rows.GetLength(1); j++)
                    {
                        double dblParsedNumber;
                        string strCurr = rows[i, j];
                        if (ParserHelper.IsNumeric(strCurr, out dblParsedNumber))
                        {
                            rows[i, j] = GetStrNum(dblParsedNumber) + (strCurr.EndsWith("%") ? "%" : string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static string GetStrNum(double dblParsedNumber)
        {
            if (Math.Abs(dblParsedNumber) >= 100000)
            {
                return String.Format("{0:#,##0}", dblParsedNumber);
            }
            if (Math.Abs(dblParsedNumber) >= 100)
            {
                return String.Format("{0:#,##0.##}", dblParsedNumber);
            }
            if (dblParsedNumber == 0)
            {
                return "-";
            }
            if ((dblParsedNumber / (int)dblParsedNumber) > 1e-15)
            {
                if (dblParsedNumber < 10)
                {
                    return String.Format("{0:#,##0.####}", dblParsedNumber);
                }
                return String.Format("{0:#,##0.##}", dblParsedNumber);
            }
            return String.Format("{0:#,##0}", dblParsedNumber);
        }

        public static string GetPxNum(double dblParsedNumber)
        {
            return String.Format("{0:#,##0.####}", dblParsedNumber);
        }


        public static string RemoveCommonSymbols(string str)
        {
            str = str
                .Replace("£", " ")
                .Replace("$", " ")
                .Replace("€", " ")
                .Replace("%", " ")
                .Replace("'", " ")
                .Replace("#", " ")
                .Replace("#", " ")
                .Replace("=", " ")
                .Replace("+", " ")
                .Replace("_", " ")
                .Replace("`", " ")
                .Replace("@", " ")
                .Replace("[", " ")
                .Replace("]", " ")
                .Replace("{", " ")
                .Replace("}", " ")
                .Replace("&", " ")
                .Replace("^", " ")
                .Replace("<", " ")
                .Replace(">", " ")
                .Replace("?", " ")
                .Replace("~", " ")
                .Replace("|", " ")
                .Replace(@"\", " ")
                .Replace(@"/", " ")
                .Replace(@"*", " ")
                .Replace(@"!", " ")
                .Replace(@"(", " ")
                .Replace(@")", " ")
                .Trim();
            if(string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            char firstChar = str[0];
            if (!char.IsLetterOrDigit(firstChar) &&
                !(firstChar == '-' ||
                firstChar == '+' ||
                firstChar == '.'))
            {
                str = str.Substring(1);
            }
            int intStrSize = str.Length;
            if (intStrSize > 1)
            {
                char lastChar = str[intStrSize - 1];
                if (!char.IsLetterOrDigit(lastChar))
                {
                    str = str.Substring(0, str.Length - 1);
                }
            }
            return str;
        }

        [Test]
        public static void TestTwoWords()
        {
            string str = "Hello_World13";
            str = CleanString(str);
            Assert.IsTrue(str.Equals("hello world 13"));
        }

        [Test]
        public static void TestReplaceEndsWith()
        {
            String str = "holaHolahola";
            var str1 = ReplaceEndsWith(str, "hola");
            var str2 = ReplaceEndsWith(str, "Hola");
            Assert.IsTrue(str1.Equals("holaHola"));
            Assert.IsTrue(str1.Equals(str2));
        }

        [Test]
        public static void TestReplaceStarsWith()
        {
            string strSymbol = "5EGOOG";
            strSymbol = ReplaceStartsWith(strSymbol,
                "5E",
            string.Empty);
            Console.WriteLine(strSymbol);

            if(strSymbol.StartsWith("5E"))
            {
                throw new HCException("Invalid strReplacement");
            }

            strSymbol = "GOOG5E";
            strSymbol = ReplaceStartsWith(strSymbol,
                "5E",
            string.Empty);
            Console.WriteLine(strSymbol);
            
            if (!strSymbol.Contains("5E"))
            {
                throw new HCException("Invalid strReplacement");
            }
        }

        public static string ReplaceEndsWith(
            string s, 
            string suffix)
        {
            if (s.EndsWith(suffix))
            {
                return s.Substring(0, s.Length - suffix.Length);
            }
            return s;
        }

        public static string ReplaceStartsWith(
            string original,
            string pattern,
            string replacement)
        {
            if(original.StartsWith(pattern))
            {
                return replacement + original.Substring(pattern.Length);
            }
            return original;
        }


        public static string ReplaceWholeWord(
            string strOriginal,
            string strWord,
            string strReplacement)
        {
            return ReplaceWholeWord(
                strOriginal,
                strWord,
                strReplacement,
                false);
        }

        public static string ReplaceWholeWord(
            string strOriginal,
            string strWord, 
            string strReplacement,
            bool blnCaseInsensitive)
        {
            try
            {
                string strPattern = String.Format(@"\b{0}\b", strWord);
                if (blnCaseInsensitive)
                {
                    strOriginal = Regex.Replace(
                        strOriginal,
                        strPattern,
                        strReplacement,
                        RegexOptions.IgnoreCase);
                }
                else
                {
                    strOriginal = Regex.Replace(
                        strOriginal,
                        strPattern,
                        strReplacement);
                }
                while (strOriginal.Contains("  "))
                {
                    strOriginal = strOriginal.Replace("  ", " ");
                }
                return strOriginal.Trim();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        public static bool ContainsWord(
            string str,
            string strWord)
        {
            strWord = strWord.ToLower();
            var toks = Tokeniser.Tokenise(str);
            return toks.Contains(strWord);
        }

        public static string ToTitleCase(string str)
        {
            var strAsTitleCase = new string(CharsToTitleCase(str).ToArray());
            return strAsTitleCase;
        }

        private static IEnumerable<char> CharsToTitleCase(string s)
        {
            bool newWord = true;
            foreach (char c in s)
            {
                if (newWord) { yield return Char.ToUpper(c); newWord = false; }
                else yield return Char.ToLower(c);
                if (c == ' ') newWord = true;
            }
        }

        [Test]
        public static void TestTextReplace()
        {
            const string strText = "Hello Big World!";

            TestWord(strText, "hello", "Big World!");
            TestWord(strText, "big", "Hello World!");
            TestWord(strText, "world", "Hello Big !");
        }

        public static string CleanString(
            string strInput)
        {
            return CleanString(strInput, false);
        }

        public static string CleanString(
            string strInput,
            bool blnIgnoreNumbers)
        {
            return CleanString(strInput,
                        null,
                        blnIgnoreNumbers);
        }

        public static string CleanString(
            string strInput,
            string[] stopWords)
        {
            return CleanString(strInput, stopWords, false);
        }

        public static string CleanString(
            string strInput,
            string[] stopWords,
            bool blnIgnoreNumbers)
        {
            try
            {
                if (String.IsNullOrEmpty(strInput))
                {
                    return string.Empty;
                }
                string[] tokens = Tokeniser.Tokenise(
                    strInput,
                    stopWords,
                    blnIgnoreNumbers);

                if (tokens == null || tokens.Length == 0)
                {
                    return string.Empty;
                }
                var sb = new StringBuilder();
                sb.Append(tokens[0]);
                for (var i = 1; i < tokens.Length; i++)
                {
                    sb.Append(" " + tokens[i]);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        private static void TestWord(
            string strText,
            string strWord,
            string strRest)
        {
            if (strRest == null) 
                throw new ArgumentNullException("strRest");
            try
            {
                string strText2 = ReplaceWholeWord(strText, strWord.ToLower(), string.Empty);
                Assert.IsTrue(strText.Equals(strText2));

                string strText3 = ReplaceWholeWord(strText, ToTitleCase(strWord), string.Empty);
                Assert.IsTrue(strRest.Equals(strText3));

                string strText4 = ReplaceWholeWord(strText, strWord.ToLower(), string.Empty, true);
                Assert.IsTrue(strRest.Equals(strText4));
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static string ReplaceCaseInsensitive(
            string strOriginal,
            string strPattern,
            string strReplacement)
        {
            try
            {
                int position0, position1;
                int count = position0 = 0;
                string upperString = strOriginal.ToUpper();
                string upperPattern = strPattern.ToUpper();
                int inc = (strOriginal.Length/strPattern.Length)*
                          (strReplacement.Length - strPattern.Length);
                var chars = new char[strOriginal.Length + Math.Max(0, inc)];
                while ((position1 = upperString.IndexOf(upperPattern,
                                                        position0)) != -1)
                {
                    for (int i = position0; i < position1; ++i)
                        chars[count++] = strOriginal[i];
                    for (int i = 0; i < strReplacement.Length; ++i)
                        chars[count++] = strReplacement[i];
                    position0 = position1 + strPattern.Length;
                }
                if (position0 == 0) return strOriginal;
                for (int i = position0; i < strOriginal.Length; ++i)
                    chars[count++] = strOriginal[i];
                return new string(chars, 0, count).Trim();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return strOriginal;
        }

        /// <summary>
        ///   Check if this token consists of all Unicode letters to eliminate
        ///   other bizarre tokens
        /// </summary>
        /// <param name = "token"></param>
        /// <returns></returns>
        public static bool AllLetters(string token)
        {
            try
            {
                for (var i = 0; i < token.Length; i++)
                {
                    if (!Char.IsLetter(token[i]))
                        return false;
                }
                return true;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return true;
        }

        public static string RemoveNonLettersNumbers(
            string strPropertyName,
            char replacement)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < strPropertyName.Length; i++)
            {
                if (Char.IsLetterOrDigit(strPropertyName[i]))
                {
                    sb.Append(strPropertyName[i]);
                }
                else
                {
                    sb.Append(replacement);
                }
            }
            return sb.ToString();
        }
    }
}
