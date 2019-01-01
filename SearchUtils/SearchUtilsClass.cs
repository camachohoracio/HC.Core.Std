#region

using System;
using System.Collections.Generic;
using HC.Core.Exceptions;
using HC.Core.Helpers;

#endregion

namespace HC.Core.SearchUtils
{
    /// <summary>
    /// Search utilities. Find a target value from a given function.
    /// Assumes that the function is monotonic.
    /// Note: This class is not threadsafe. 
    /// RPL values are calculated in different threads.
    /// </summary>
    [Serializable]
    public class SearchUtilsClass
    {
        #region Events

        #region Delegates

        /// <summary>
        /// Event which sets the value to be evaluated by the 
        /// search algorithm given a parameter and a function.
        /// </summary>
        /// <param name="dblParameter">
        /// The parameter to be evaluated
        /// </param>
        /// <param name="function">
        /// The function to be evaluated
        /// </param>
        /// <returns></returns>
        public delegate double EvaluateSearchFunction(
            double dblParameter);

        #endregion

        /// <summary>
        /// Evaluate function event.
        /// </summary>
        public event EvaluateSearchFunction evaluateFunction;

        #endregion

        #region Public

        /// <summary>
        /// Binary search Algorithm
        /// </summary>
        /// <param name="dblParameterMaxValue">
        /// Max value of parameter
        /// </param>
        /// <param name="dblParameterMinValue">
        /// Min value of parameter
        /// </param>
        /// <param name="dblTargetValue">
        /// Target value
        /// </param>
        /// <param name="function">
        /// Function to be evaluated
        /// </param>
        /// <returns>
        /// Search value
        /// </returns>
        public double BinarySearch(
            double dblParameterMaxValue,
            double dblParameterMinValue,
            double dblTargetValue)
        {
            double dblBinarySearchPrecision = CoreConstants.DBL_BINARY_SEARCH_PRECISION;
            int intBinarySearchIterations = CoreConstants.INT_BINARY_SEARCH_ITERATIONS;
            double dblCurrentParameter = BinarySearch(
                dblParameterMaxValue,
                dblParameterMinValue,
                dblTargetValue,
                dblBinarySearchPrecision,
                intBinarySearchIterations);
            return dblCurrentParameter;
        }

        /// <summary>
        /// Binary Sarch Algorithm
        /// </summary>
        /// <param name="dblParameterMaxValue">
        /// Max value parameter
        /// </param>
        /// <param name="dblParameterMinValue">
        /// Min value parameter
        /// </param>
        /// <param name="dblTargetValue">
        /// Target value
        /// </param>
        /// <param name="function">
        /// Function to be evaluated
        /// </param>
        /// <param name="dblBinarySearchPrecision">
        /// Precision
        /// </param>
        /// <param name="intlBinarySearchIterations">
        /// Iterations
        /// </param>
        /// <returns>
        /// Search value
        /// </returns>
        public double BinarySearch(
            double dblParameterMaxValue,
            double dblParameterMinValue,
            double dblTargetValue,
            double dblBinarySearchPrecision,
            int intlBinarySearchIterations)
        {
            double dblCurrentParameter = 0;
            bool blnFlag = true;
            double dblDelta;
            double dblCurrentValue;
            int intCounter = 0;
            while (blnFlag)
            {
                // place the current threshold at the middle of two ranges
                dblCurrentParameter =
                    dblParameterMinValue + (dblParameterMaxValue - dblParameterMinValue)/2.0;
                intCounter++;
                //
                // calculate function
                //
                dblCurrentValue = EvaluateFunction(dblCurrentParameter);

                // calculate the error
                dblDelta = dblTargetValue - dblCurrentValue;

                // end the search if a maximum number of iterations is reached
                if (intCounter > intlBinarySearchIterations ||
                    Math.Abs(dblParameterMaxValue - dblParameterMinValue) <
                    dblBinarySearchPrecision)
                {
                    if (Math.Abs(dblParameterMaxValue - dblParameterMinValue) <
                        dblBinarySearchPrecision &&
                        dblParameterMinValue == 0)
                    {
                        dblCurrentParameter = 0;
                    }
                    PrintToScreen.WriteLine("Binary search iterations: " + intCounter);

                    blnFlag = false;
                }

                // check if the required precision is achieved
                if (Math.Abs(dblDelta) < dblBinarySearchPrecision)
                {
                    PrintToScreen.WriteLine("Binary search iterations: " + intCounter);
                    blnFlag = false;
                }
                else
                {
                    // move to next point in the search
                    if (dblCurrentValue > dblTargetValue)
                    {
                        dblParameterMinValue = dblCurrentParameter;
                    }
                    else
                    {
                        dblParameterMaxValue = dblCurrentParameter;
                    }
                }
            }
            return dblCurrentParameter;
        }

        public double InterpolatedBinarySearch(
            double dblTargetValue)
        {
            return InterpolatedBinarySearch(
                double.MaxValue,
                -double.MaxValue,
                dblTargetValue);
        }

        public double InterpolatedBinarySearch(
            double dblParameterMaxValue,
            double dblParameterMinValue,
            double dblTargetValue)
        {
            double dblBinarySearchPrecision = CoreConstants.DBL_BINARY_SEARCH_PRECISION;
            int intBinarySearchIterations = CoreConstants.INT_BINARY_SEARCH_ITERATIONS;
            return InterpolatedBinarySearch(
                dblParameterMaxValue,
                dblParameterMinValue,
                dblTargetValue,
                dblBinarySearchPrecision,
                intBinarySearchIterations);
        }


        /// <summary>
        /// Interpolated/Binary search algorithm.
        /// Both algorithms compete for accuracy. Since binary search provides large 
        /// jumps at the beginning then use binary search at the beginning.
        /// 
        /// Assume the function is increasing in both axes of the given function.
        /// i.e. if "x" increases, then "y" increases as well.
        /// </summary>
        /// <param name="dblParameterMaxValue">
        /// Max value
        /// </param>
        /// <param name="dblParameterMinValue">
        /// Min value
        /// </param>
        /// <param name="dblTargetValue">
        /// Target value
        /// </param>
        /// <param name="function">
        /// Function. Must be monotonic
        /// </param>
        /// <param name="dblBinarySearchPrecision">
        /// Precision
        /// </param>
        /// <param name="intlBinarySearchIterations">
        /// Maximum number of iterations
        /// </param>
        /// <returns></returns>
        public double InterpolatedBinarySearch(
            double dblParameterMaxValue,
            double dblParameterMinValue,
            double dblTargetValue,
            double dblBinarySearchPrecision,
            int intlBinarySearchIterations)
        {
            double dblCurrentParameter = 0;
            double dblCurrentParameter0 = 0;
            bool blnFlag = true;
            double dblCurrentValue0 = 0.0;
            int intCounter = 0;
            double dblFitnessMinValue =
                EvaluateFunction(dblParameterMinValue);
            double dblFitnessMaxValue =
                EvaluateFunction(dblParameterMaxValue);
            //
            // check if initial values are between the search
            //
            double dblMinTestValue =
                Math.Min(dblFitnessMinValue, dblFitnessMaxValue);
            double dblMaxTestValue =
                Math.Max(dblFitnessMinValue, dblFitnessMaxValue);

            //
            // check if binary seach is worth the try
            //
            if (dblTargetValue < dblMinTestValue ||
                dblTargetValue > dblMaxTestValue)
            {
                if (dblTargetValue < dblMinTestValue)
                {
                    return dblParameterMinValue;
                }
                if (dblTargetValue > dblMaxTestValue)
                {
                    return dblParameterMaxValue;
                }
            }


            while (blnFlag)
            {
                double dblDelta0 = double.MaxValue;
                // do binary seach for the first iterations
                if (intCounter > 5)
                {
                    // compete binary search with interpolated search

                    if (dblFitnessMinValue > dblTargetValue)
                    {
                        return dblParameterMinValue;
                    }

                    double dblVal = ((dblParameterMaxValue - dblParameterMinValue)*
                                     (dblFitnessMaxValue - dblTargetValue)/
                                     (dblFitnessMaxValue - dblFitnessMinValue));
                    if (double.IsInfinity(dblVal))
                    {
                        //
                        // do binary search
                        //
                        dblCurrentParameter0 = 
                            dblParameterMinValue + (dblParameterMaxValue - dblParameterMinValue) / 2.0;
                    }
                    else
                    {
                        // place the current threshold according to the interpolation value
                        dblCurrentParameter0 = dblParameterMaxValue -
                                               dblVal;
                    }
                    dblCurrentValue0 = EvaluateFunction(dblCurrentParameter0);
                    dblDelta0 = dblTargetValue - dblCurrentValue0;
                }
                //
                // place the current threshold at the middle of two ranges
                //
                dblCurrentParameter =
                    dblParameterMinValue + (dblParameterMaxValue - dblParameterMinValue)/2.0;
                double dblCurrentValue = EvaluateFunction(dblCurrentParameter);

                double dblDelta = dblTargetValue - dblCurrentValue;
                if (Math.Abs(dblDelta) > Math.Abs(dblDelta0))
                {
                    // move to next point in the search
                    if (dblCurrentValue > dblTargetValue)
                    {
                        dblParameterMaxValue = dblCurrentParameter;
                        dblFitnessMaxValue = dblCurrentValue;
                    }
                    else
                    {
                        dblParameterMinValue = dblCurrentParameter;
                        dblFitnessMinValue = dblCurrentValue;
                    }

                    // there is less error with interpolation
                    dblCurrentParameter = dblCurrentParameter0;
                    dblCurrentValue = dblCurrentValue0;
                }

                intCounter++;

                // calculate the error
                dblDelta = dblTargetValue - dblCurrentValue;

                // end the search if a maximum number of iterations is reached
                if (intCounter > intlBinarySearchIterations ||
                    Math.Abs(dblParameterMaxValue - dblParameterMinValue) <
                    dblBinarySearchPrecision)
                {
                    if (Math.Abs(dblParameterMaxValue - dblParameterMinValue) <=
                        dblBinarySearchPrecision &&
                        dblParameterMinValue == 0)
                    {
                        dblCurrentParameter = 0;
                    }
                    blnFlag = false;
                }

                // check if the required precision is achieved
                if (Math.Abs(dblDelta) < dblBinarySearchPrecision)
                {
                    blnFlag = false;
                }
                else
                {
                    // move to next point in the search
                    if (dblCurrentValue > dblTargetValue)
                    {
                        // check that the values are different
                        if (dblParameterMinValue == dblCurrentParameter)
                        {
                            blnFlag = false;
                        }
                        dblParameterMaxValue = dblCurrentParameter;
                        dblFitnessMaxValue = dblCurrentValue;
                    }
                    else
                    {
                        // check that the values are different
                        if (dblParameterMaxValue == dblCurrentParameter)
                        {
                            blnFlag = false;
                        }
                        dblParameterMinValue = dblCurrentParameter;
                        dblFitnessMinValue = dblCurrentValue;
                    }
                }
            }

            return dblCurrentParameter;
        }

        #endregion

        #region Protected

        /// <summary>
        /// Evaluate binary search function
        /// </summary>
        /// <param name="dblParameter">
        /// Parameter to be evaluated
        /// </param>
        /// <param name="dblFunction">
        /// Function to be evaluated
        /// </param>
        /// <returns>
        /// Function value
        /// </returns>
        protected double EvaluateFunction(
            double dblParameter)
        {
            if (evaluateFunction != null)
            {
                if (evaluateFunction.GetInvocationList().Length > 0)
                {
                    return evaluateFunction.Invoke(dblParameter);
                }
            }
            throw new HCException("Binary search function not implemented.");
        }

        #endregion

        /// <summary>
        /// Binary search.
        /// </summary>
        /// <param name="dblParameterMaxValue">
        /// Max parameter value
        /// </param>
        /// <param name="dblParameterMinValue">
        /// Min parameter value
        /// </param>
        /// <param name="dblTargetValue">
        /// Target parameter value
        /// </param>
        /// <param name="evaluate">
        /// Evaluation function
        /// </param>
        /// <param name="dblPrecision">
        /// Search precision
        /// </param>
        /// <param name="intIterations">
        /// Maximum number of iterations
        /// </param>
        /// <returns>Parameter value</returns>
        public static double BinarySearch_(
            double dblParameterMaxValue,
            double dblParameterMinValue,
            double dblTargetValue,
            Func<double, double> evaluate,
            double dblPrecision,
            int intIterations)
        {
            double dblCurrentParameter = 0;
            bool blnFlag = true;
            double dblDelta;
            double dblCurrentValue;
            int intCounter = 0;
            while (blnFlag)
            {
                if (intCounter > intIterations)
                {
                    throw new HCException("Binary search failed to converge.");
                }

                // place the current threshold at the middle of two ranges
                dblCurrentParameter =
                    dblParameterMinValue + (dblParameterMaxValue - dblParameterMinValue)/2.0;
                intCounter++;

                //
                // calculate function
                //
                dblCurrentValue = evaluate(dblCurrentParameter);

                // calculate the error
                dblDelta = dblTargetValue - dblCurrentValue;

                // end the search if a maximum number of iterations is reached
                if (intCounter > intIterations ||
                    Math.Abs(dblParameterMaxValue - dblParameterMinValue) <
                    dblPrecision)
                {
                    if (Math.Abs(dblParameterMaxValue - dblParameterMinValue) <
                        dblPrecision &&
                        dblParameterMinValue == 0)
                    {
                        dblCurrentParameter = 0;
                    }
                    PrintToScreen.WriteLine("Binary search iterations: " + intCounter);

                    blnFlag = false;
                }

                // check if the required precision is achieved
                if (Math.Abs(dblDelta) < dblPrecision)
                {
                    PrintToScreen.WriteLine("Binary search iterations: " + intCounter);
                    blnFlag = false;
                }
                else
                {
                    // move to next point in the search
                    if (dblCurrentValue > dblTargetValue)
                    {
                        dblParameterMinValue = dblCurrentParameter;
                    }
                    else
                    {
                        dblParameterMaxValue = dblCurrentParameter;
                    }
                }
            }
            return dblCurrentParameter;
        }


        /// <summary>
        /// Binary search.
        /// </summary>
        /// <param name="dblParameterMaxValue">
        /// Max parameter value
        /// </param>
        /// <param name="dblParameterMinValue">
        /// Min parameter value
        /// </param>
        /// <param name="dblTargetValue">
        /// Target parameter value
        /// </param>
        /// <param name="evaluate">
        /// Evaluation function
        /// </param>
        /// <param name="dblPrecision">
        /// Search precision
        /// </param>
        /// <returns>Parameter value</returns>
        public static double BinarySearch_(
            double dblParameterMaxValue,
            double dblParameterMinValue,
            double dblTargetValue,
            Func<double, double> evaluate,
            double dblPrecision)
        {
            return BinarySearch_(
                dblParameterMaxValue,
                dblParameterMinValue,
                dblTargetValue,
                evaluate,
                dblPrecision,
                1000);
        }

        /// <summary>
        /// Performs a binary search for a double value in an array of
        /// doubles. 
        /// NB. Assumes that the array is in ascending order.
        /// NB. Assumes that the values lies in range.
        /// </summary>
        /// <param name="dblSearchSpace">The search space.</param>
        /// <param name="dblValue">The seach value.</param>
        /// <returns>The index.</returns>
        public static int DoBinarySearch(
            List<double> dblSearchSpace,
            double dblValue)
        {
            int intLow = 0;
            int intMid = 0;
            int intHigh = dblSearchSpace.Count - 1;

            if (dblValue >= dblSearchSpace[dblSearchSpace.Count - 1])
            {
                return dblSearchSpace.Count - 1;
            }

            while (true)
            {
                intMid = ((intLow + intHigh)/2);

                if (dblSearchSpace[intMid] > dblValue)
                {
                    intHigh = intMid;
                }
                else if (dblSearchSpace[intMid + 1] <= dblValue)
                {
                    intLow = intMid + 1;
                }
                else
                {
                    return intMid;
                }
            }
        }

        public static int DoBinarySearch(
            DateTime[] dblSearchSpace,
            DateTime dblValue)
        {
            int intLow = 0;
            int intMid = 0;
            int intHigh = dblSearchSpace.Length - 1;

            if (dblValue >= dblSearchSpace[dblSearchSpace.Length - 1])
            {
                return dblSearchSpace.Length - 1;
            }

            while (true)
            {
                intMid = ((intLow + intHigh)/2);

                if (dblSearchSpace[intMid] > dblValue)
                {
                    intHigh = intMid;
                }
                else if (dblSearchSpace[intMid + 1] <= dblValue)
                {
                    intLow = intMid + 1;
                }
                else
                {
                    return intMid;
                }
            }
        }

        public static int DoBinarySearch(
            double[] dblSearchSpace,
            double dblValue)
        {
            int intLow = 0;
            int intMid = 0;
            int intHigh = dblSearchSpace.Length - 1;

            if (dblValue >= dblSearchSpace[dblSearchSpace.Length - 1])
            {
                return dblSearchSpace.Length - 1;
            }

            while (true)
            {
                intMid = ((intLow + intHigh)/2);

                if (dblSearchSpace[intMid] > dblValue)
                {
                    intHigh = intMid;
                }
                else if (dblSearchSpace[intMid + 1] <= dblValue)
                {
                    intLow = intMid + 1;
                }
                else
                {
                    return intMid;
                }
            }
        }
    }
}


