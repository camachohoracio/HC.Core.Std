#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace HC.Core.SearchUtils
{
    [Serializable]
    public class GenericSearchUtils2D<T>
    {
        #region Events

        #region Delegates

        public delegate double EvaluateSearchFunctionObject(
            T value);

        #endregion

        public event EvaluateSearchFunctionObject OnEvaluateSearchFunctionObject;

        #endregion

        public T SearchFunctionValue(
            List<T> functionList,
            double dblX)
        {
            if (functionList.Count == 0)
            {
                // return null value
                return default(T);
            }

            if (InvokeOnEvaluateSearchFunctionObject(functionList[0]) > dblX)
            {
                return default(T);
            }

            if (InvokeOnEvaluateSearchFunctionObject(
                    functionList[
                        functionList.Count - 1]) < dblX)
            {
                return default(T);
            }

            int intFunctionIndex = GetFunctionIndex(
                functionList,
                dblX);
            return functionList[intFunctionIndex];
        }


        public int GetFunctionIndex(
            List<T> functionList,
            double dblX)
        {
            int intLow = 0;
            return GetFunctionIndex(
                functionList,
                dblX,
                intLow);
        }

        public int GetFunctionIndex(
            List<T> functionList,
            double dblX,
            int intLow)
        {
            int intMid = 0;
            int intHigh = functionList.Count - 1;

            if (dblX >= InvokeOnEvaluateSearchFunctionObject(functionList[functionList.Count - 1]))
            {
                return functionList.Count - 1;
            }


            while (true)
            {
                intMid = ((intLow + intHigh)/2);

                if (InvokeOnEvaluateSearchFunctionObject(functionList[intMid]) > dblX)
                {
                    intHigh = intMid;
                }
                else if (InvokeOnEvaluateSearchFunctionObject(functionList[intMid + 1]) <= dblX)
                {
                    intLow = intMid + 1;
                }
                else
                {
                    return intMid;
                }
            }
        }

        public int GetFunctionIndexLower(
            List<T> functionList,
            double dblX)
        {
            int intLow = 0;
            int intMid = 0;
            int intHigh = functionList.Count - 1;

            if (dblX >= InvokeOnEvaluateSearchFunctionObject(functionList[functionList.Count - 1]))
            {
                return functionList.Count - 1;
            }


            while (true)
            {
                intMid = ((intLow + intHigh)/2);

                if (InvokeOnEvaluateSearchFunctionObject(functionList[intMid]) >= dblX)
                {
                    intHigh = intMid;
                }
                else if (InvokeOnEvaluateSearchFunctionObject(functionList[intMid + 1]) < dblX)
                {
                    intLow = intMid + 1;
                }
                else
                {
                    // return the closest value
                    if (Math.Abs(
                            InvokeOnEvaluateSearchFunctionObject(functionList[intMid + 1]) - dblX) <
                        Math.Abs(
                            InvokeOnEvaluateSearchFunctionObject(functionList[intMid]) - dblX))
                    {
                        return intMid + 1;
                    }
                    else
                    {
                        return intMid;
                    }
                }
                if (intLow == intHigh)
                {
                    return intMid;
                }
            }
        }

        private double InvokeOnEvaluateSearchFunctionObject(T functionObject)
        {
            if (OnEvaluateSearchFunctionObject != null)
            {
                if (OnEvaluateSearchFunctionObject.GetInvocationList().Count() > 0)
                {
                    return OnEvaluateSearchFunctionObject(functionObject);
                }
            }
            return -1;
        }
    }
}


