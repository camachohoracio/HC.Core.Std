#region

using System;
using System.Globalization;
using HC.Core.Exceptions;
using HC.Core.Time;
using HC.Core.Logging;
using NUnit.Framework;

#endregion

namespace HC.Core.Helpers
{
    public static class ParserHelper
    {
        private static readonly NumberFormatInfo m_currencyNumberFormat = new NumberFormatInfo
        {
            NegativeSign = "-",
            CurrencyDecimalSeparator = ".",
            CurrencyGroupSeparator = ",",
            CurrencySymbol = "$"
        };

        [Test]
        public static void DoTest()
        {
            Assert.IsTrue(IsNumeric("1.2324532"));
            Assert.IsTrue(IsNumeric("1.2324532%"));
            Assert.IsTrue(IsNumeric("1E10"));
            Assert.IsTrue(IsNumeric("1E10%"));
            Assert.IsTrue(!IsNumeric("a1.2324532"));
            Assert.IsTrue(!IsNumeric("1.2324532b"));
            Assert.IsTrue(IsNumeric("$1'123,421.2324532"));
            Assert.IsTrue(IsNumeric("£1.2324532"));
            Assert.IsTrue(IsNumeric("1.2324532%"));
        }

        public static bool IsNumeric(
            object obj)
        {
            double dblParsedNumber;
            return IsNumeric(obj, out dblParsedNumber);
        }

        private static bool ParseWithAbbrevation(
            string strVal,
            out double dblVal)
        {
            dblVal = 0;
            try
            {
                double dblMultiplier = 1.0;
                if (strVal.EndsWith("B"))
                {
                    strVal = strVal.Replace("B", "");
                    dblMultiplier = 1e9;
                }
                else if (strVal.EndsWith("M"))
                {
                    strVal = strVal.Replace("M", "");
                    dblMultiplier = 1e6;
                }
                else if (strVal.EndsWith("MILL"))
                {
                    strVal = strVal.Replace("MILL", "");
                    dblMultiplier = 1e6;
                }
                else if (strVal.EndsWith("MIO"))
                {
                    strVal = strVal.Replace("MIO", "");
                    dblMultiplier = 1e6;
                }

                if (IsNumeric(strVal, out dblVal))
                {
                    dblVal = dblVal * dblMultiplier;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public static bool ParseNumber(
            string strNumber0,
            out double dblParsedValue,
            out string strNumber)
        {
            return ParseNumber(
                strNumber0,
                true,
                out dblParsedValue,
                out strNumber);
        }

        public static bool ParseNumber(
            string strNumber0,
            bool blnRecurse,
            out double dblParsedValue,
            out string strNumber)
        {
            dblParsedValue = 0;
            strNumber = string.Empty;
            try
            {
                strNumber = strNumber0;
                strNumber = strNumber.Trim().ToLower();
                if (String.IsNullOrEmpty(strNumber))
                {
                    return false;
                }
                strNumber = strNumber
                    .Replace("?", string.Empty)
                    .Replace("€", string.Empty)
                    .Replace("£", string.Empty)
                    .Replace("¥", string.Empty)
                    .Replace("f", string.Empty)
                    .Replace("usd", string.Empty)
                    .Replace("twd", string.Empty)
                    .Replace("php", string.Empty)
                    .Replace("dkk", string.Empty)
                    .Replace("(r)", string.Empty)
                    .Replace("$", string.Empty)
                    .Replace("c", string.Empty)
                    .Replace("nok", string.Empty)
                    .Replace("s", string.Empty)
                    .Replace("chf", string.Empty)
                    .Replace("cf", string.Empty)
                    .Replace("*", string.Empty)
                    .Replace("h", string.Empty)
                    .Replace("brl", string.Empty)
                    .Replace("try", string.Empty)

                    .Replace("sek", string.Empty)
                    .Replace("mxn", string.Empty)
                    .Replace("zar", string.Empty)
                    .Replace("inr", string.Empty)
                    .Trim();

                strNumber = RemoveLetters(strNumber);

                double dblMultiplier = 1.0;
                if (strNumber.EndsWith("b"))
                {
                    strNumber = StringHelper.ReplaceEndsWith(strNumber, "b");
                    dblMultiplier = 1e9;
                }
                else if (strNumber.EndsWith("m"))
                {
                    strNumber = StringHelper.ReplaceEndsWith(strNumber, "m");
                    dblMultiplier = 1e6;
                }
                else if (strNumber.EndsWith("k"))
                {
                    strNumber = StringHelper.ReplaceEndsWith(strNumber, "k");
                    dblMultiplier = 1e3;
                }

                strNumber = strNumber.Trim();

                bool blnResult = IsNumeric(strNumber, out dblParsedValue);
                if (!blnResult && !String.IsNullOrEmpty(strNumber) && blnRecurse)
                {
                    strNumber = CleanString(strNumber);
                    if (!String.IsNullOrEmpty(strNumber))
                    {
                        var toks = strNumber.Split(' ');
                        for (int i = 0; i < toks.Length; i++)
                        {
                            blnResult = ParseNumber(toks[i], false, out dblParsedValue, out strNumber);
                            if (blnResult && dblParsedValue != 0)
                            {
                                break;
                            }
                        }
                    }
                }

                dblParsedValue = dblParsedValue * dblMultiplier;
                return blnResult;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        private static string RemoveLetters(string strItem)
        {
            try
            {
                if (String.IsNullOrEmpty(strItem))
                {
                    return string.Empty;
                }

                string strResult = string.Empty;
                for (int i = 0; i < strItem.Length; i++)
                {
                    if (!Char.IsLetter(strItem[i]))
                    {
                        strResult += strItem[i];
                    }
                }
                return strResult;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }

        public static string CleanString(
            string str)
        {
            try
            {
                if (string.IsNullOrEmpty(str))
                {
                    return string.Empty;
                }
                str = str.Replace("\t", string.Empty)
                         .Replace("\n", string.Empty)
                         .Replace("\r", string.Empty)
                         .Replace("&nbsp;", string.Empty);

                while (str.Contains("  "))
                {
                    str = str.Replace("  ", " ");
                }
                str = str.Trim();
                return str;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }


        public static bool IsNumeric(
            object obj,
            bool blnCheckAbbrevation,
            out double dblParsedNumber)
        {
            if(blnCheckAbbrevation)
            {
                return ParseWithAbbrevation(obj.ToString(), out dblParsedNumber);
            }
            return IsNumeric(obj, out dblParsedNumber);
        }

        public static bool IsNumeric(
            object obj,
            out double dblParsedNumber)
        {
            dblParsedNumber = double.NaN;
            try
            {
                if(obj == null)
                {
                    return false;
                }


                if (IsNumericRaw(obj, out dblParsedNumber))
                {
                    return true;
                }
                string strNum;
                if (!string.IsNullOrEmpty((strNum = obj as string)))
                {
                    decimal dcmParsedNumber;
                    bool blnIsNum = decimal.TryParse(
                        strNum, 
                        NumberStyles.Currency, 
                        m_currencyNumberFormat,
                        out dcmParsedNumber);
                    dblParsedNumber = (double) dcmParsedNumber;
                    if(blnIsNum)
                    {
                        return true;
                    }

                    strNum = StringHelper.RemoveCommonSymbols(strNum);

                    if (!string.IsNullOrEmpty(strNum))
                    {
                        if (IsNumericRaw(strNum, out dblParsedNumber))
                        {
                            return true;
                        }

                    }

                }
                return false;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        private static bool IsNumericRaw(object obj, out double dblParsedNumber)
        {
            bool blnIsNum = Double.TryParse(Convert.ToString(obj),
                                            NumberStyles.Any,
                                            NumberFormatInfo.InvariantInfo,
                                            out dblParsedNumber);
            if (blnIsNum)
            {
                return true;
            }
            return false;
        }

        public static double CastToDouble(object obj)
        {
            if (obj is double)
            {
                return (double) obj;
            }
            if (obj is int)
            {
                return (int) obj;
            }
            if (obj is long)
            {
                return (long) obj;
            }
            throw new NotImplementedException();
        }

        public static T ParseObject<T>(
            object objInput)
        {
            if (objInput.GetType() == typeof (T))
            {
                return (T) objInput;
            }
            return (T) ParseString(
                objInput.ToString(),
                typeof (T));
        }

        public static object ParseString(
            string strInput,
            Type type)
        {
            try
            {
                if (type == null)
                {
                    return null;
                }
                if (type == typeof(string) ||
                    type == typeof(object))
                {
                    return strInput;
                }
                if (type == typeof(double))
                {
                    double dblValue;
                    if (double.TryParse(
                        strInput,
                        out dblValue))
                    {
                        return dblValue;
                    }
                    return null;
                }
                if (type == typeof(int))
                {
                    int intValue;
                    if (int.TryParse(
                        strInput,
                        out intValue))
                    {
                        return intValue;
                    }
                    return null;
                }
                if (type == typeof(bool))
                {
                    bool blnValue;
                    if (bool.TryParse(
                        strInput,
                        out blnValue))
                    {
                        return blnValue;
                    }
                    return null;
                }
                if (type == typeof(long))
                {
                    long lngValue;
                    if (long.TryParse(
                        strInput,
                        out lngValue))
                    {
                        return lngValue;
                    }
                    return null;
                }
                if (type == typeof(DateTime))
                {
                    DateTime dateTime;
                    if (!DateTime.TryParse(strInput, out dateTime))
                    {
                        dateTime = DateHelper.ParseDateTimeString(
                            strInput);
                    }
                    return dateTime;
                }
                if (type.BaseType == typeof(Enum))
                {
                    return Enum.Parse(type, strInput);
                }
                throw new HCException("Type not found [" +
                    type.Name + "]");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static Type GetType(string strToken)
        {
            if (IsInt(strToken))
            {
                return typeof (int);
            }
            if (IsDouble(strToken))
            {
                return typeof (double);
            }
            if (IsDate(strToken))
            {
                return typeof (DateTime);
            }
            return typeof (string);
        }

        public static bool IsInt(string strToken)
        {
            int intValue;
            return int.TryParse(strToken, out intValue);
        }

        public static bool IsDouble(string strToken)
        {
            double dblValue;
            return double.TryParse(strToken, out dblValue);
        }

        public static bool IsDate(string strToken)
        {
            DateTime dateTime;
            return DateTime.TryParse(strToken, out dateTime);
        }
    }
}


