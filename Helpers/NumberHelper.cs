#region

using System;

#endregion

namespace HC.Core.Helpers
{
    public class NumberHelper
    {
        // MANTISSA ROUNDING (TRUNCATING)
        // returns a value of xDouble truncated to trunc double places
        public static double truncate(double xDouble, int trunc)
        {
            var xTruncated = xDouble;
            if (!double.IsNaN(xDouble))
            {
                if (!isPlusInfinity(xDouble))
                {
                    if (!isMinusInfinity(xDouble))
                    {
                        if (xDouble != 0.0D)
                        {
                            var xString = xDouble.ToString().Trim();
                            xTruncated = double.Parse(truncateProcedure(xString, trunc));
                        }
                    }
                }
            }
            return xTruncated;
        }

        // private method for truncating a float or double expressed as a string
        public static string truncateProcedure(string xValue, int trunc)
        {
            var xTruncated = xValue;
            var xWorking = xValue;
            var exponent = " ";
            var first = "+";
            var expPos = xValue.IndexOf('E');
            var dotPos = xValue.IndexOf('.');
            var minPos = xValue.IndexOf('-');

            if (minPos != -1)
            {
                if (minPos == 0)
                {
                    xWorking = xWorking.Substring(1);
                    first = "-";
                    dotPos--;
                    expPos--;
                }
            }
            if (expPos > -1)
            {
                exponent = xWorking.Substring(expPos);
                xWorking = xWorking.Substring(0, expPos);
            }
            string xPreDot = null;
            var xPostDot = "0";
            string xDiscarded = null;
            string tempString = null;
            var tempDouble = 0.0D;
            if (dotPos > -1)
            {
                xPreDot = xWorking.Substring(0, dotPos);
                xPostDot = xWorking.Substring(dotPos + 1);
                var xLength = xPostDot.Length;
                if (trunc < xLength)
                {
                    xDiscarded = xPostDot.Substring(trunc);
                    tempString = xDiscarded.Substring(0, 1) + ".";
                    if (xDiscarded.Length > 1)
                    {
                        tempString += xDiscarded.Substring(1);
                    }
                    else
                    {
                        tempString += "0";
                    }
                    tempDouble = Math.Round(double.Parse(tempString));

                    if (trunc > 0)
                    {
                        if (tempDouble >= 5.0)
                        {
                            var xArray = new int[trunc + 1];
                            xArray[0] = 0;
                            for (var i = 0; i < trunc; i++)
                            {
                                xArray[i + 1] = int.Parse(xPostDot.Substring(i, i + 1));
                            }
                            var test = true;
                            var iCounter = trunc;
                            while (test)
                            {
                                xArray[iCounter] += 1;
                                if (iCounter > 0)
                                {
                                    if (xArray[iCounter] < 10)
                                    {
                                        test = false;
                                    }
                                    else
                                    {
                                        xArray[iCounter] = 0;
                                        iCounter--;
                                    }
                                }
                                else
                                {
                                    test = false;
                                }
                            }
                            var preInt = int.Parse(xPreDot);
                            preInt += xArray[0];
                            xPreDot = preInt.ToString();
                            tempString = "";
                            for (var i = 1; i <= trunc; i++)
                            {
                                tempString += xArray[i].ToString();
                            }
                            xPostDot = tempString;
                        }
                        else
                        {
                            xPostDot = xPostDot.Substring(0, trunc);
                        }
                    }
                    else
                    {
                        if (tempDouble >= 5.0)
                        {
                            var preInt = int.Parse(xPreDot);
                            preInt++;
                            xPreDot = preInt.ToString();
                        }
                        xPostDot = "0";
                    }
                }
                xTruncated = first + xPreDot.Trim() + "." + xPostDot.Trim() + exponent;
            }
            return xTruncated.Trim();
        }


        // Returns true if x is minus infinity
        // x is double
        public static bool isMinusInfinity(double x)
        {
            var test = false;
            if (x == double.NegativeInfinity)
            {
                test = true;
            }
            return test;
        }

        // Returns true if x is minus infinity
        // x is float
        public static bool isMinusInfinity(float x)
        {
            var test = false;
            if (x == double.NegativeInfinity)
            {
                test = true;
            }
            return test;
        }


        // returns a value of xFloat truncated to trunc double places
        public static float truncate(float xFloat, int trunc)
        {
            var xTruncated = xFloat;
            if (!double.IsNaN(xFloat))
            {
                if (!isPlusInfinity(xFloat))
                {
                    if (!isMinusInfinity(xFloat))
                    {
                        if (xFloat != 0.0D)
                        {
                            var xString = xFloat.ToString().Trim();
                            xTruncated = float.Parse(truncateProcedure(xString, trunc));
                        }
                    }
                }
            }
            return xTruncated;
        }

        // Returns true if x is plus infinity
        // x is double
        public static bool isPlusInfinity(double x)
        {
            var test = false;
            if (x == double.PositiveInfinity)
            {
                test = true;
            }
            return test;
        }

        // Returns true if x is plus infinity
        // x is float
        public static bool isPlusInfinity(float x)
        {
            var test = false;
            if (x == double.PositiveInfinity)
            {
                test = true;
            }
            return test;
        }
    }
}


