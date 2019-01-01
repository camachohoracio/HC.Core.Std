#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace HC.Core.SearchUtils
{
    public class GenericSearchUtils3D<T>
    {
        #region Events

        #region Delegates

        public delegate double EvaluateSearchFunctionObject(
            T value);

        #endregion

        public event EvaluateSearchFunctionObject OnEvaluateSearchFunctionObjectX;
        public event EvaluateSearchFunctionObject OnEvaluateSearchFunctionObjectY;

        #endregion

        public T SearchFunctionValue(
            List<T> functionList,
            double dblX,
            double dblY)
        {
            if (functionList.Count == 0)
            {
                // return null value
                return default(T);
            }
            //
            // check if the sarch value is within the cached values
            //
            if (InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[0]) > dblX)
            {
                return default(T);
            }

            if (InvokeOnEvaluateSearchFunctionObjectY(
                    functionList[0]) > dblY)
            {
                return default(T);
            }

            if (InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[
                        functionList.Count - 1]) < dblX)
            {
                return default(T);
            }

            if (InvokeOnEvaluateSearchFunctionObjectY(
                    functionList[
                        functionList.Count - 1]) < dblY)
            {
                return default(T);
            }

            //
            // the function values may be within the cached values. Therefore, 
            // seach for closest match
            //
            int intFunctionIndex = GetFunctionIndex(
                functionList,
                dblX,
                dblY);
            return functionList[intFunctionIndex];
        }


        public int GetFunctionIndex(
            List<T> functionList,
            double dblX,
            double dblY)
        {
            int intLow = 0;
            int intMid = 0;
            int intHigh = functionList.Count - 1;
            double dblBestMatchXDistance = double.MaxValue;
            double dblBestMatchYDistance = double.MaxValue;

            // get closest value in X
            int intXIndex = GetFunctionIndexX(
                functionList,
                dblX);
            double dblCurrentX = InvokeOnEvaluateSearchFunctionObjectX(
                functionList[intXIndex]);
            double dblCurrentY = InvokeOnEvaluateSearchFunctionObjectY(
                functionList[intXIndex]);

            if (dblCurrentY > dblY)
            {
                intHigh = intXIndex;
            }
            else if (dblCurrentY < dblY)
            {
                intLow = intXIndex;
            }
            double dblCurrentMatchXDistance =
                Math.Abs(
                    dblX -
                    dblCurrentX);
            dblBestMatchXDistance = dblCurrentMatchXDistance;


            double dblCurrentMatchYDistance =
                Math.Abs(
                    dblY -
                    dblCurrentY);
            dblBestMatchYDistance = dblCurrentMatchYDistance;

            while (intLow != intHigh)
            {
                intMid = ((intLow + intHigh)/2);
                dblCurrentX = InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[intMid]);
                dblCurrentMatchXDistance =
                    Math.Abs(
                        dblX -
                        dblCurrentX);
                if (dblCurrentMatchXDistance < dblBestMatchXDistance)
                {
                    //
                    // move in x direction
                    //
                    // reset y distance
                    dblBestMatchYDistance = double.MaxValue;
                    dblBestMatchXDistance = dblCurrentMatchXDistance;
                    if (dblCurrentX > dblX)
                    {
                        intHigh = intMid;
                    }
                    else if (dblCurrentX < dblX)
                    {
                        intLow = intMid;
                    }
                }
                else if (dblCurrentMatchXDistance == dblBestMatchXDistance)
                {
                    //
                    // move in y direction
                    //
                    dblCurrentY = InvokeOnEvaluateSearchFunctionObjectY(
                        functionList[intMid]);

                    dblCurrentMatchYDistance =
                        Math.Abs(
                            dblY -
                            dblCurrentY);

                    //if (dblCurrentMatchYDistance < dblBestMatchYDistance)
                    //{
                    dblBestMatchYDistance = dblCurrentMatchYDistance;
                    if (dblCurrentY > dblY)
                    {
                        intHigh = intMid;
                    }
                    else if (dblCurrentY < dblY)
                    {
                        if (intLow == intMid)
                        {
                            intLow = intMid + 1;
                            intMid++;
                        }
                        else
                        {
                            intLow = intMid;
                        }
                    }
                    else
                    {
                        return intMid;
                    }
                    //}
                    //else
                    //{
                    //    //
                    //    // there is no improvement in y, 
                    //    // therefore the closest value has been found
                    //    //
                    //    return intMid;
                    //}
                }
                else
                {
                    //
                    // move in x direction
                    //
                    // reset y distance
                    dblBestMatchYDistance = double.MaxValue;
                    if (dblCurrentX > dblX)
                    {
                        intHigh = intMid;
                    }
                    else if (dblCurrentX < dblX)
                    {
                        if (intLow == intMid)
                        {
                            intLow = intMid + 1;
                        }
                        else
                        {
                            intLow = intMid;
                        }
                    }
                }
            }

            return intMid;
        }


        public int GetFunctionIndexX(
            List<T> functionList,
            double dblX)
        {
            int intLow = 0;
            int intMid = 0;
            int intHigh = functionList.Count - 1;
            //double dblBestMatchXDistance = double.MaxValue;

            while (intLow != intMid || intHigh != intMid)
            {
                intMid = ((intLow + intHigh)/2);
                double dblCurrentX = InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[intMid]);
                //double dblCurrentMatchXDistance =
                //    Math.Abs(
                //        dblX -
                //        dblCurrentX);
                //if (dblCurrentMatchXDistance < dblBestMatchXDistance)
                //{
                //
                // move in x direction
                //
                //dblBestMatchXDistance = dblCurrentMatchXDistance;
                if (dblCurrentX > dblX)
                {
                    intHigh = intMid;
                }
                else if (dblCurrentX < dblX)
                {
                    if (intLow == intMid)
                    {
                        intLow = intMid + 1;
                    }
                    else
                    {
                        intLow = intMid;
                    }
                }
                else
                {
                    return intMid;
                }
                //}
                //else if (dblCurrentMatchXDistance == dblBestMatchXDistance)
                //{
                //    return intMid;
                //}
                //else
                //{
                //    //
                //    // move in x direction
                //    //
                //    if (dblCurrentX > dblX)
                //    {
                //        intHigh = intMid;
                //    }
                //    else if (dblCurrentX < dblX)
                //    {
                //        if (intLow == intMid)
                //        {
                //            intLow = intMid + 1;
                //        }
                //        else
                //        {
                //            intLow = intMid;
                //        }
                //    }
                //}
            }
            return intMid;
        }


        public T GetNextYValue(
            List<T> functionList,
            int intIndex)
        {
            if (intIndex == functionList.Count() - 1)
            {
                return functionList[intIndex];
            }

            double dblCurrentXValue =
                InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[intIndex]);
            double dblNexXValue =
                InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[intIndex + 1]);

            if (dblCurrentXValue != dblNexXValue)
            {
                return functionList[intIndex];
            }
            return functionList[intIndex + 1];
        }

        public T GetPreviousYValue(
            List<T> functionList,
            int intIndex)
        {
            if (intIndex == 0)
            {
                return functionList[intIndex];
            }

            double dblCurrentXValue =
                InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[intIndex]);
            double dblPrevXValue =
                InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[intIndex - 1]);

            if (dblCurrentXValue != dblPrevXValue)
            {
                return functionList[intIndex];
            }
            return functionList[intIndex - 1];
        }

        public T GetNextXValue(
            List<T> functionList,
            int intIndex)
        {
            int intLow = 0;
            int intMid = intIndex;
            int intHigh = functionList.Count - 1;

            double dblX =
                InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[intIndex]);

            while (true)
            {
                if (intLow + 1 == intHigh ||
                    intLow == intHigh)
                {
                    return functionList[intHigh];
                }
                double dblCurrentXValue = InvokeOnEvaluateSearchFunctionObjectX(
                    functionList[intMid]);

                if (dblCurrentXValue > dblX)
                {
                    intHigh = intMid;
                }
                else if (dblCurrentXValue <= dblX)
                {
                    intLow = intMid;
                }
                intMid = (intLow + intHigh)/2;
            }
        }

        public T GetPreviousXValue(
            List<T> functionList,
            int intIndex)
        {
            int intLow = 0;
            int intMid = intIndex;
            int intHigh = functionList.Count - 1;

            double dblX = InvokeOnEvaluateSearchFunctionObjectX(
                functionList[intIndex]);

            while (true)
            {
                if (intLow + 1 == intHigh ||
                    intLow == intHigh)
                {
                    return functionList[intLow];
                }

                double dblCurrentXValue =
                    InvokeOnEvaluateSearchFunctionObjectX(
                        functionList[intMid]);

                if (dblCurrentXValue >= dblX)
                {
                    intHigh = intMid;
                }
                else if (dblCurrentXValue < dblX)
                {
                    intLow = intMid;
                }
                intMid = (intLow + intHigh)/2;
            }
        }


        //private int GetFunctionIndex_old(
        //    List<T> functionList,
        //    double dblX,
        //    double dblY)
        //{
        //    int intLow = 0;
        //    int intMid = 0;
        //    int intHigh = functionList.Count - 1;

        //    if (dblX >= InvokeOnEvaluateSearchFunctionObjectX(
        //        functionList[functionList.Count - 1]) &&
        //        dblY >= InvokeOnEvaluateSearchFunctionObjectY(
        //        functionList[functionList.Count - 1]))
        //    {
        //        return functionList.Count - 1;
        //    }

        //    // get lower index
        //    GenericSearchUtils2D<T> genericSearchUtils2D = new GenericSearchUtils2D<T>();

        //    //register x values
        //    genericSearchUtils2D.OnEvaluateSearchFunctionObject +=
        //        new GenericSearchUtils2D<T>.EvaluateSearchFunctionObject(
        //            InvokeOnEvaluateSearchFunctionObjectX);

        //    int intLowValue = genericSearchUtils2D.GetFunctionIndexLower(
        //        functionList,
        //        dblX);
        //    int intHighValue = genericSearchUtils2D.GetFunctionIndex(
        //        functionList,
        //        dblX,
        //        intLowValue);

        //    //
        //    // locate between low and high values in Y
        //    //
        //    intHigh = intHighValue;
        //    intLow = intLowValue;
        //    if (dblY >= InvokeOnEvaluateSearchFunctionObjectY(functionList[functionList.Count - 1]))
        //    {
        //        return functionList.Count - 1;
        //    }


        //    while (true)
        //    {
        //        intMid = ((intLow + intHigh) / 2);

        //        if (InvokeOnEvaluateSearchFunctionObjectY(functionList[intMid]) > dblY)
        //        {
        //            intHigh = intMid;
        //        }
        //        else if (InvokeOnEvaluateSearchFunctionObjectY(functionList[intMid + 1]) <= dblY)
        //        {
        //            intLow = intMid + 1;
        //        }
        //        else
        //        {
        //            return intMid;
        //        }
        //    }


        //    //while (true)
        //    //{
        //    //    intMid = ((intLow + intHigh) / 2);
        //    //    double dblLowXValue =
        //    //        InvokeOnEvaluateSearchFunctionObjectX(
        //    //            functionList[intMid]);
        //    //    double dblHighXValue =
        //    //        InvokeOnEvaluateSearchFunctionObjectX(
        //    //            functionList[intMid + 1]);
        //    //    if (!((dblLowXValue > dblX &&
        //    //        dblHighXValue < dblX) ||
        //    //        dblLowXValue == dblX))
        //    //    {
        //    //        if (dblLowXValue > dblX)
        //    //        {
        //    //            intHigh = intMid;
        //    //        }
        //    //        else if (dblHighXValue < dblX)
        //    //        {
        //    //            intLow = intMid + 1;
        //    //        }
        //    //    }
        //    //    else
        //    //    {


        //    //        double dblLowYValue =
        //    //            InvokeOnEvaluateSearchFunctionObjectY(
        //    //                functionList[intMid]);
        //    //        double dblHighYValue =
        //    //            InvokeOnEvaluateSearchFunctionObjectY(
        //    //                functionList[intMid + 1]);


        //    //        if (dblLowYValue > dblY)
        //    //        {
        //    //            intHigh = intMid;
        //    //        }
        //    //        else if (dblHighYValue <= dblY)
        //    //        {
        //    //            intLow = intMid + 1;
        //    //        }
        //    //        else
        //    //        {
        //    //            return intMid;
        //    //        }
        //    //    }
        //    //}
        //}

        private double InvokeOnEvaluateSearchFunctionObjectX(T functionObject)
        {
            if (OnEvaluateSearchFunctionObjectX != null)
            {
                if (OnEvaluateSearchFunctionObjectX.GetInvocationList().Count() > 0)
                {
                    return OnEvaluateSearchFunctionObjectX(functionObject);
                }
            }
            return -1;
        }

        private double InvokeOnEvaluateSearchFunctionObjectY(T functionObject)
        {
            if (OnEvaluateSearchFunctionObjectY != null)
            {
                if (OnEvaluateSearchFunctionObjectY.GetInvocationList().Count() > 0)
                {
                    return OnEvaluateSearchFunctionObjectY(functionObject);
                }
            }
            return -1;
        }
    }
}


