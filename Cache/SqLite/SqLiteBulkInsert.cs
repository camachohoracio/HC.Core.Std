#region

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using HC.Core.Exceptions;
using HC.Core.Logging;

#endregion

namespace HC.Core.Cache.SqLite
{
    public class SqLiteBulkInsert : IDisposable
    {
        #region Constants

        private const uint COMMIT_MAX = 10000;
        private const string PARAM_DELIM = ":";

        #endregion

        #region Members

        private SQLiteConnection m_dblConn;
        private readonly string m_strFileName;
        private List<KeyValuePair<string, SQLiteParameter>> m_parameters;
        private string m_strBeginInsertText;
        private SQLiteCommand m_cmd;
        private uint m_intCounter;
        private SQLiteTransaction m_trans;
        private bool m_blnIsDisposed;

        #endregion

        #region Properties

        private string CommandText
        {
            get
            {
                try
                {
                    if (m_parameters.Count < 1)
                    {
                        throw new HCException("You must add at least one paramater");
                    }

                    var sb = new StringBuilder(255);
                    sb.Append(m_strBeginInsertText);

                    for (int i = 0; i < m_parameters.Count; i++)
                    {
                        string strParam = m_parameters[i].Key;
                        sb.Append('[');
                        sb.Append(strParam);
                        sb.Append(']');
                        sb.Append(", ");
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(") VALUES (");

                    for (int i = 0; i < m_parameters.Count; i++)
                    {
                        string strParam = m_parameters[i].Key;
                        sb.Append(PARAM_DELIM);
                        sb.Append(strParam);
                        sb.Append(", ");
                    }
                    sb.Remove(sb.Length - 2, 2);
                    sb.Append(")");

                    return sb.ToString();
                }
                catch(Exception ex)
                {
                    Logger.Log(new HCException("Exception in database [" +
                        m_strFileName +
                        "]"));
                    Logger.Log(ex);
                }
                return null;
            }
        }

        #endregion

        #region Constructors

        public SqLiteBulkInsert(
            SQLiteConnection dbConn, 
            string strTableName,
            string strFileName)
        {
            m_dblConn = dbConn;
            m_strFileName = strFileName;
            m_parameters = new List<KeyValuePair<string, SQLiteParameter>>();
            var query = new StringBuilder(255);
            query.Append("INSERT INTO [");
            query.Append(strTableName);
            query.Append("] (");
            m_strBeginInsertText = query.ToString();
        }

        #endregion

        #region Public

        public void Dispose()
        {
            try
            {
                if (m_blnIsDisposed)
                {
                    return;
                }

                EventHandlerHelper.RemoveAllEventHandlers(this);
                CommitTransaction(m_strFileName);
                m_blnIsDisposed = true;

                if (m_cmd != null)
                {
                    m_cmd.Dispose();
                    m_cmd = null;
                }
                m_dblConn = null;
                if (m_parameters != null)
                {
                    m_parameters.Clear();
                    m_parameters = null;
                }
                m_strBeginInsertText = null;

                if(m_trans != null)
                {
                    m_trans.Dispose();
                }
                m_trans = null;
            }
            catch(Exception ex)
            {
                Logger.Log(new HCException("Exception in database [" +
                    m_strFileName +
                    "]"));
                Logger.Log(ex);
            }
        }

        public void AddParameter(string strName, DbType dbType)
        {
            var param = new SQLiteParameter(PARAM_DELIM + strName, dbType);
            m_parameters.Add(new KeyValuePair<string, SQLiteParameter>(strName, param));
        }

        private void CommitTransaction(string strFileName )
        {
            if(m_blnIsDisposed)
            {
                throw new HCException("Connection already disposed");
            }
            var trans = m_trans;
            try
            {
                if (trans != null)
                {
                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new HCException(
                    "Could not commit transation. See inner exception for more details . Exception in database [" +
                    strFileName +
                    "]"));
                Logger.Log(ex);
            }
            finally
            {
                if (trans != null)
                {
                    trans.Dispose();
                }
                m_trans = null;
                m_intCounter = 0;
            }
        }

        public void Insert(
            object[] paramValues,
            string strDbName)
        {
            try
            {
                if (paramValues == null)
                {
                    return;
                }
                if (m_blnIsDisposed)
                {
                    throw new HCException("Connection already disposed");
                }

                if (paramValues.Length != m_parameters.Count)
                {
                    throw new HCException(
                        "The values array count must be equal to the count of the number of parameters");
                }

                m_intCounter++;

                if (m_intCounter == 1)
                {
                    m_trans = m_dblConn.BeginTransaction();
                    m_cmd = m_dblConn.CreateCommand();
                    m_cmd.CommandTimeout = SqliteConstants.TIME_OUT;
                    for (int i = 0; i < m_parameters.Count; i++)
                    {
                        SQLiteParameter sqLiteParameter = m_parameters[i].Value;
                        m_cmd.Parameters.Add(sqLiteParameter);
                    }
                    m_cmd.CommandText = CommandText;
                }

                for (int i = 0; i < m_parameters.Count; i++)
                {
                    SQLiteParameter sqLiteParameter = m_parameters[i].Value;
                    sqLiteParameter.Value = paramValues[i];
                    paramValues[i] = null;
                }
                m_cmd.ExecuteNonQuery();
                if (m_intCounter == COMMIT_MAX)
                {
                    try
                    {
                        if (m_trans != null)
                        {
                            m_trans.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        if (m_trans != null)
                        {
                            m_trans.Dispose();
                            m_trans = null;
                        }

                        m_intCounter = 0;
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Log(new HCException("Error in database [" + 
                    strDbName +
                    "]"));
                Logger.Log(ex);
            }
        }

        #endregion
    }
}