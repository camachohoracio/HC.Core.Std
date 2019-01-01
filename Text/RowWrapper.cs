using System;
using System.Text;
using HC.Core.Logging;

namespace HC.Core.Text
{
    public class RowWrapper
    {
        #region Properties

        public TokenWrapper[][] Columns { get; set; }
        public object Handle { get; set; }

        #endregion

        public override string ToString()
        {
            return ToString("_");
        }

        public string ToString(string strDelimiter)
        {
            var parentSb = new StringBuilder();
            //
            // load first row
            //
            var strParentCol =
                GetColumnString(Columns[0]);

            parentSb.Append(strParentCol);

            for (var i = 1; i < Columns.Length; i++)
            {
                strParentCol =
                    GetColumnString(Columns[i]);
                parentSb.Append(strDelimiter + strParentCol);
            }
            var strRowDesc = parentSb.ToString();
            return strRowDesc;
        }

        public static string GetColumnString(TokenWrapper[] tokens)
        {
            try
            {
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
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return string.Empty;
        }
    }
}


