#region

using System;
using System.Text;
using HC.Core.Io.Serialization.Interfaces;

#endregion

namespace HC.Core.Resources
{
    [Serializable]
    public class DbDataRequest : ASerializable, IDataRequest, IDisposable
    {
        #region Properties

        public string DataSource { get; set; }
        public string DbPath { get; set; }
        public string DbName { get; set; }
        public string TableName { get; private set; }
        public string Name { get; private set; }
        public bool Compress { get; set; }

        #endregion

        #region Constructors
        
        /// <summary>
        /// Used for serialization
        /// </summary>
        public DbDataRequest(){}

        public DbDataRequest(
            string strDataSource) :
            this(
            strDataSource,
            string.Empty,
            string.Empty,
            string.Empty,
            false) { }


        public DbDataRequest(
            string strDataSource,
            string strDbName) :
            this(
            strDataSource,
            strDbName,
            string.Empty,
            string.Empty,
            false) { }

        public DbDataRequest(
            string strDataSource,
            string strDbName,
            string strTableName) :
            this(
            strDataSource,
            strDbName,
            strTableName,
            string.Empty,
            false) { }

        public DbDataRequest(
            string strDataSource,
            string strDbName,
            string strTableName,
            string strDbPath) :
            this(
            strDataSource,
            strDbName,
            strTableName,
            strDbPath,
            false) { }

        public DbDataRequest(
            string strDataSource,
            string strDbName,
            string strTableName,
            string strDbPath,
            bool blnCompress)
        {
            DataSource = strDataSource;
            DbName = strDbName;
            TableName = strTableName;
            DbPath = strDbPath;
            Compress = blnCompress;
            SetResourceName();
        }

        #endregion

        #region Public

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(IDataRequest other)
        {
            return other.Name.Equals(Name);
        }

        public int CompareTo(IDataRequest other)
        {
            return other.Name.CompareTo(Name);
        }

        public int Compare(IDataRequest x, IDataRequest y)
        {
            return x.Name.CompareTo(y.Name);
        }

        public int Compare(object x, object y)
        {
            return Compare((IDataRequest)x,
                           (IDataRequest)y);
        }

        public bool Equals(IDataRequest x, IDataRequest y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(IDataRequest obj)
        {
            return obj.Name.GetHashCode();
        }

        #endregion

        #region Private Methods

        private void SetResourceName()
        {
            StringBuilder sb =
                new StringBuilder();

            if (!string.IsNullOrEmpty(DbName))
            {
                sb.Append("%")
                    .Append(DbName);
            }
            if (!string.IsNullOrEmpty(TableName))
            {
                sb.Append("%")
                    .Append(TableName);
            }
            if (!string.IsNullOrEmpty(DbPath))
            {
                sb.Append("%")
                    .Append(DbPath);
            }
            sb.Append("%")
                .Append(Compress);

            Name = sb.ToString();
        }

        #endregion

        public void Dispose()
        {
            DataSource = null;
            DbPath = null;
            DbName = null;
            TableName = null;
            Name = null;
        }
    }
}



