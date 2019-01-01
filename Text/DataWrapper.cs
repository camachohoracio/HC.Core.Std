#region

using System;
using System.Collections.Generic;

#endregion

namespace HC.Core.Text
{
    public class DataWrapper : IDisposable
    {
        #region Properties

        public RowWrapper[] Data { get; set; }

        #endregion

        #region Constructors

        public DataWrapper(List<string> dataList,
                           char charColumnDelimiter) : this(dataList,
                                                    charColumnDelimiter,
                                                    null) { }

        public DataWrapper(List<string> dataList,
                           char charColumnDelimiter,
                            string[] stopWords)
        {
            LoadTokens(dataList,
                       charColumnDelimiter,
                       stopWords);
        }

        public DataWrapper(TokenWrapper[] data)
        {
            Data = new RowWrapper[1];
            Data[0] = new RowWrapper();
            Data[0].Columns = new TokenWrapper[1][];
            Data[0].Columns[0] = data;
        }

        public DataWrapper(RowWrapper[] data)
        {
            Data = data;
        }

        public DataWrapper(List<string> nameList) : this(nameList, '\0')
        {
        }

        public DataWrapper()
        {
        }

        #endregion

        public int Length
        {
            get
            {
                return Data.Length;
            }
        }

        private void LoadTokens(
            List<string> dataList,
            char charColumnDelimiter,
            string[] stopWords)
        {
            if (dataList == null || dataList.Count == 0)
            {
                Data = new RowWrapper[0];
                return;
            }
            var intCols = dataList[0].Split(charColumnDelimiter).Length;
            Data = new RowWrapper[dataList.Count];
            for (var intRow = 0; intRow < dataList.Count; intRow++)
            {
                Data[intRow] = new RowWrapper();
                Data[intRow].Columns = new TokenWrapper[intCols][];
                var strLine = dataList[intRow];
                var cols = strLine.Split(',');
                for (var intCol = 0; intCol < intCols; intCol++)
                {
                    var strCol = cols[intCol];
                    var tokens = Tokeniser.TokeniseAndWrap(strCol, stopWords);
                    Data[intRow].Columns[intCol] = tokens;
                }
            }
        }

        public void Dispose()
        {
            Data = null;
        }
    }
}


