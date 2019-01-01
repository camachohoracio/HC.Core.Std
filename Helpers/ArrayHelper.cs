using System;
using HC.Core.Exceptions;
using HC.Core.Logging;
using NUnit.Framework;

namespace HC.Core.Helpers
{
    public static class ArrayHelper
    {
        public static void SplitArray<T>(
            T[] array, 
            out T[] arr1, 
            out T[] arr2,
            double dblPrcArr1)
        {
            arr1 = null;
            arr2 = null;
            try
            {
                if (array == null ||
                    array.Length == 0)
                {
                    return;
                }

                if (dblPrcArr1 < 0 || dblPrcArr1 > 1)
                {
                    throw new HCException("Invalid array prc size");
                }
                int intArrSize = array.Length;
                int intArr1Size = Math.Min(
                    intArrSize,
                    (int) (Math.Round(
                        dblPrcArr1*intArrSize,
                        0)));
                int intArr2Size = intArrSize - intArr1Size;
                if (intArr1Size < 0 || intArr1Size > intArrSize)
                {
                    throw new HCException("Invalid array2 size");
                }
                if (intArr2Size < 0 || intArr2Size > intArrSize)
                {
                    throw new HCException("Invalid array2 size");
                }


                if (intArr1Size == 0)
                {
                    arr1 = array;
                    return;
                }
                if (intArr2Size == 0)
                {
                    arr2 = array;
                    return;
                }
                arr1 = new T[intArr1Size];
                arr2 = new T[intArr2Size];
                for (int i = 0; i < intArrSize; i++)
                {
                    if(i < intArr1Size)
                    {
                        arr1[i] = array[i];
                    }
                    else
                    {
                        arr2[i - intArr1Size] = array[i];
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        /// <summary>
        ///   Copy a row from a given array. The array structure is defined as [row,column]
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "inputArray"></param>
        /// <param name = "intRow"></param>
        /// <returns></returns>
        public static T[] GetRowCopy<T>(T[,] inputArray, int intRow)
        {
            var newArr = new T[inputArray.GetLength(1)];
            for (var i = 0; i < inputArray.GetLength(1); i++)
            {
                newArr[i] = inputArray[intRow, i];
            }
            return newArr;
        }

        public static T[,] GetSliceCopy<T>(T[,,] inputArray, int intSlice)
        {
            var newArr = new T[
                inputArray.GetLength(1),
                inputArray.GetLength(2)];

            for (var i = 0; i < inputArray.GetLength(1); i++)
            {
                for (var j = 0; j < inputArray.GetLength(2); j++)
                {
                    newArr[i, j] = inputArray[intSlice, i, j];
                }
            }
            return newArr;
        }

        public static void SetRow<T>(
            T[,] inputArray,
            T[] rowArray,
            int intRow)
        {
            for (var i = 0; i < inputArray.GetLength(1); i++)
            {
                inputArray[intRow, i] = rowArray[i];
            }
        }

        [Test]
        public static void TestArrSplit()
        {
            const int intArrSize = 177;
            const double dblPrc = 0.3;
            var array = new double[intArrSize];
            var intArrSize1 = (int) Math.Round(intArrSize*dblPrc, 0);
            int intArrSize2 = intArrSize - intArrSize1;
            double[] arr1;
            double[] arr2;
            SplitArray(
                array,
                out arr1,
                out arr2,
                dblPrc);
            Assert.IsTrue(arr1.Length == intArrSize1);
            Assert.IsTrue(arr2.Length == intArrSize2);
        }

        /// <summary>
        ///   Swap the rows from a given array
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "inputArray"></param>
        /// <param name = "intRow1"></param>
        /// <param name = "intRow2"></param>
        /// <returns></returns>
        public static T[,] SwapRowFromArray<T>(
            T[,] inputArray,
            int intRow1,
            int intRow2)
        {
            var temp =
                GetRowCopy(
                    inputArray,
                    intRow1);
            var intColumns = inputArray.GetLength(1);
            for (var i = 0; i < intColumns; i++)
            {
                inputArray[intRow1, i] = inputArray[intRow2, i];
                inputArray[intRow2, i] = temp[i];
            }
            return inputArray;
        }

        public static double[] GetRowCopy(double[,] dblInputArray, int intRow)
        {
            var intDimensions = dblInputArray.GetLength(1);
            var dblOutArray = new double[intDimensions];
            for (var i = 0; i < intDimensions; i++)
            {
                dblOutArray[i] = dblInputArray[intRow, i];
            }
            return dblOutArray;
        }
    }
}


