#region

using System;
using System.Collections.Generic;
using HC.Core.Helpers;
using HC.Core.Logging;
using NUnit.Framework;

#endregion

namespace HC.Core.Text
{
    public static class Tokeniser
    {
        [Test]
        public static void DoTest()
        {
            const string strTextExample = @"This £1.234, $1.5E6 is_a sentence-a Million    £%^£ assdf
                        word1 word2 /t";

            string[] substringsList = Tokenise(strTextExample);
            Assert.IsTrue("this".Equals(substringsList[0]));
            Assert.IsTrue(1.234 ==  double.Parse(substringsList[1]));
            Assert.IsTrue(1.5E6 == double.Parse(substringsList[2]));
            Assert.IsTrue("is".Equals(substringsList[3]));
            Assert.IsTrue("a".Equals(substringsList[4]));
            Assert.IsTrue("sentence".Equals(substringsList[5]));
            Assert.IsTrue("a".Equals(substringsList[6]));
            Assert.IsTrue("million".Equals(substringsList[7]));
            Assert.IsTrue("assdf".Equals(substringsList[8]));
            Assert.IsTrue("word".Equals(substringsList[9]));
            Assert.IsTrue("1".Equals(substringsList[10]));
            Assert.IsTrue("word".Equals(substringsList[11]));
            Assert.IsTrue("2".Equals(substringsList[12]));
            Console.WriteLine(substringsList);
        }

        public static string[] Tokenise(
            string str,
            bool blnIgnoreDigits)
        {
            try
            {
                if(string.IsNullOrEmpty(str))
                {
                    return new string[0];
                }

                string[] substrings = str.Split(new[]
                    {
                        '\t',
                        '\b',
                        '\n',
                        ' '
                    });

                var substringsList = new List<string>(substrings.Length);
                for (int i = 0; i < substrings.Length; i++)
                {
                    string strCurrStr = substrings[i];
                    if (!string.IsNullOrEmpty(substrings[i]))
                    {
                        strCurrStr = StringHelper.RemoveCommonSymbols(strCurrStr);
                        if (!string.IsNullOrEmpty(strCurrStr))
                        {
                            double dblParsedNumber;
                            if (char.IsDigit(strCurrStr[0]) &&
                                ParserHelper.IsNumeric(strCurrStr, out dblParsedNumber))
                            {
                                substringsList.Add(dblParsedNumber.ToString());
                            }
                            else
                            {
                                KeyValuePair<string, bool>[] digitLeters = SplitDigitLetters(strCurrStr);
                                if (digitLeters != null &&
                                    digitLeters.Length > 0)
                                {
                                    for (int j = 0; j < digitLeters.Length; j++)
                                    {
                                        var kvp = digitLeters[j];
                                        if (kvp.Value)
                                        {
                                            if (ParserHelper.IsNumeric(kvp.Key, out dblParsedNumber))
                                            {
                                                substringsList.Add(dblParsedNumber.ToString());
                                            }
                                            else
                                            {
                                                TokeniseAndAdd(kvp.Key, substringsList, blnIgnoreDigits);
                                            }
                                        }
                                        else
                                        {
                                            TokeniseAndAdd(kvp.Key, substringsList, blnIgnoreDigits);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return substringsList.ToArray();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new string[0];
        }

        private static void TokeniseAndAdd(
            string str, 
            List<string> substringsList,
            bool blnIgnoreDigits)
        {
            string[] tokens = Tokenise0(str, blnIgnoreDigits);
            if(tokens != null &&
                tokens.Length > 0 &&
                !(tokens.Length == 1 &&
                string.IsNullOrEmpty(tokens[0])))
            {
                substringsList.AddRange(tokens);
            }
        }

        private static string[] GetEmptyTokenSet()
        {
            try
            {
                var arr = new string[1];
                arr[0] = "";
                return arr;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new string[0];
        }

        public static TokenWrapper[] TokeniseAndWrap(
            string strInput,
            string[] strStopWordsArr)
        {
            try
            {
                if (strStopWordsArr == null)
                {
                    return TokeniseAndWrap(
                        strInput);
                }
                string[] tokens = Tokenise(strInput,
                                           strStopWordsArr,
                                           false);
                return WrapTokens(tokens);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new TokenWrapper[0];
        }

        public static string[] Tokenise(
            string strInput,
            string[] strStopWordsArr)
        {
            return Tokenise(
                strInput,
                strStopWordsArr,
                false);
        }

        public static string[] Tokenise(
            string strInput,
            string[] strStopWordsArr,
            bool blnIgnoreNumbers)
        {
            try
            {
                if (strStopWordsArr == null)
                {
                    return Tokenise(strInput,
                                    blnIgnoreNumbers);
                }

                string[] tokenArr = Tokenise(
                    strInput, 
                    blnIgnoreNumbers);

                var tokenList =
                    new List<string>(tokenArr.Length);

                foreach (string strToken in tokenArr)
                {
                    var blnAddToken = true;
                    foreach (string strStopWord in strStopWordsArr)
                    {
                        if (!strStopWord.Equals(string.Empty))
                        {
                            if (strStopWord.Equals(strToken))
                            {
                                blnAddToken = false;
                            }
                        }
                    }
                    if (blnAddToken)
                    {
                        tokenList.Add(strToken);
                    }
                }
                tokenArr = tokenList.ToArray();
                return tokenArr;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new string[0];
        }


        public static TokenWrapper[] TokeniseAndWrap(string strInput)
        {
            string[] tokens = Tokenise(strInput, false);
            TokenWrapper[] tokenWraps = WrapTokens(tokens);
            return tokenWraps;
        }

        public static TokenWrapper[] WrapTokens(string[] tokens)
        {
            try
            {
                var tokenWraps = new TokenWrapper[tokens.Length];
                for (var i = 0; i < tokens.Length; i++)
                {
                    var strToken = tokens[i];
                    tokenWraps[i] = new TokenWrapper(strToken);
                }
                return tokenWraps;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new TokenWrapper[0];
        }

        public static string[] Tokenise(
            string strInput)
        {
            return Tokenise(strInput, false);
        }

        public static KeyValuePair<string,bool>[] SplitDigitLetters(
            string strInput)
        {
            try
            {
                if (string.IsNullOrEmpty(strInput))
                {
                    return new KeyValuePair<string, bool>[0];
                }
                strInput = strInput.ToLower();
                var tokens = new List<KeyValuePair<string, bool>>();
                var cursor = 0;
                var length = strInput.Length;
                while (cursor < length)
                {
                    var ch = strInput[cursor];
                    if (char.IsWhiteSpace(ch))
                    {
                        cursor++;
                    }
                    else if (!char.IsDigit(ch))
                    {
                        string word = "";
                        while (cursor < length &&
                               !char.IsDigit(strInput[cursor]))
                        {
                            word += strInput[cursor];
                            cursor++;
                        }
                        tokens.Add(new KeyValuePair<string, bool>(word, false));
                    }
                    else if (!char.IsLetter(ch))
                    {
                        var word = "";
                        while (cursor < length &&
                               !char.IsLetter(strInput[cursor]))
                        {
                            word += strInput[cursor];
                            cursor++;
                        }
                        tokens.Add(new KeyValuePair<string, bool>(word, true));
                    }
                    else
                    {
                        cursor++;
                    }
                }
                if (tokens.Count == 0)
                {
                    return new KeyValuePair<string, bool>[0];
                }
                return tokens.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return new KeyValuePair<string, bool>[0];
        }


        private static string[] Tokenise0(
            string strInput,
            bool blnIgnoreNumbers)
        {
            try
            {
                if (string.IsNullOrEmpty(strInput))
                {
                    var tokenArray = new string[1];
                    tokenArray[0] = string.Empty;
                    return tokenArray;
                }
                strInput = strInput.ToLower();
                var tokens = new List<string>();
                var cursor = 0;
                var length = strInput.Length;
                while (cursor < length)
                {
                    var ch = strInput[cursor];
                    if (char.IsWhiteSpace(ch))
                    {
                        cursor++;
                    }
                    else if (char.IsLetter(ch))
                    {
                        var word = "";
                        while (cursor < length &&
                               char.IsLetter(strInput[cursor]))
                        {
                            word += strInput[cursor];
                            cursor++;
                        }
                        tokens.Add(word);
                    }
                    else if (char.IsDigit(ch))
                    {
                        var word = "";
                        while (cursor < length &&
                               char.IsDigit(strInput[cursor]))
                        {
                            word += strInput[cursor];
                            cursor++;
                        }
                        tokens.Add(word);
                    }
                    else
                    {
                        cursor++;
                    }
                }
                if (tokens.Count == 0)
                {
                    return GetEmptyTokenSet();
                }
                var outTokens = new List<string>();
                for (int i = 0; i < tokens.Count; i++)
                {
                    string strToken = tokens[i];
                    if (!string.IsNullOrEmpty(strToken))
                    {
                        if ((blnIgnoreNumbers &&
                             StringHelper.AllLetters(strToken)) ||
                            !blnIgnoreNumbers)
                        {
                            outTokens.Add(strToken);
                        }
                    }
                }
                return outTokens.ToArray();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return new string[0];
        }
    }
}