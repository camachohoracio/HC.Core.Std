#region

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using HC.Core.Exceptions;

#endregion

namespace HC.Core.Io
{
    public class FileTransferHelper : IDisposable
    {
        #region Events

        #region Delegates

        public delegate CopyFileCallbackAction CopyFileCallback(
            FileInfo source, FileInfo destination, object state,
            long totalFileSize, long totalBytesTransferred);

        public delegate void ProgressBarEventHandler(
            string message, int percentage);

        #endregion

        public event ProgressBarEventHandler progressBarEventHandler;

        public event CopyFileCallback CopyFileCallbackEventHandler;

        #endregion

        #region Memebers

        public bool m_blnRunTransfer = true;
        private StreamReader m_br;
        private BinaryWriter m_bw;
        private long m_intFileSize;
        private long m_intTransfered;
        private DateTime m_startTime;
        private string m_strDestinationFileName;
        private string m_strSourceFileName;

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            EventHandlerHelper.RemoveAllEventHandlers(this);
            m_blnRunTransfer = false;
            if (m_br != null)
            {
                try
                {
                    m_br.Close();
                }
                catch
                {
                }
            }
            if (m_bw != null)
            {
                try
                {
                    m_bw.Close();
                }
                catch
                {
                }
            }
        }

        #endregion

        ~FileTransferHelper()
        {
            Dispose();
        }

        public void CancelTransfer()
        {
            m_blnRunTransfer = false;
        }

        public void MoveFile(
            string strSourceFileName,
            string strDestinationFileName)
        {
            var fiSource = new FileInfo(
                strSourceFileName);
            var fiDestination = new FileInfo(
                strDestinationFileName);
            if (!fiSource.DirectoryName.ToLower().Equals(fiDestination.DirectoryName.ToLower()))
            {
                CopyFile(
                    strSourceFileName,
                    fiDestination.DirectoryName,
                    false);
            }
            if (!fiSource.Name.Equals(fiDestination.Name))
            {
                File.Move(
                    fiDestination.DirectoryName + @"\" +
                    fiSource.Name,
                    fiDestination.DirectoryName + @"\" +
                    fiDestination.Name);
            }
            FileHelper.Delete(strSourceFileName);
        }

        public void CopyFile(
            string strSourceFileName,
            string strDestinationFileName)
        {
            if (!FileHelper.Exists(strSourceFileName))
            {
                throw new HCException("File not found: " + strSourceFileName);
            }

            var fiSource = new FileInfo(strSourceFileName);
            var strSourceDirName = fiSource.DirectoryName;
            var strSourceFileName2 = fiSource.Name;

            var fiDest = new FileInfo(strDestinationFileName);
            var strDestDirName = fiDest.DirectoryName;

            //string strSourceDirName = fiSource.DirectoryName;

            CopyFile(strSourceFileName,
                     strDestDirName,
                     false);

            // rename file
            File.Move(
                strDestDirName + @"\" + strSourceFileName2,
                strDestinationFileName);
        }

        public void CopyFile(
            string strFileName,
            string strDestinationPath,
            bool blnCutIntoVolumes)
        {
            m_blnRunTransfer = true;
            if (!FileHelper.Exists(strFileName))
            {
                Console.WriteLine(
                    "File transfer error. File not found: " + strFileName);
                return;
            }
            m_strSourceFileName = strFileName;
            m_startTime = DateTime.Now;
            var fi = new FileInfo(strFileName);
            m_intFileSize = fi.Length;
            m_intTransfered = 0;
            m_strDestinationFileName = strDestinationPath + @"\" +
                                       fi.Name;

            // Delete file, if exists.
            if (FileHelper.Exists(m_strDestinationFileName))
            {
                FileHelper.Delete(m_strDestinationFileName);
            }

            if (blnCutIntoVolumes)
            {
                TransferVolumesFiles();
            }
            else
            {
                TransferFile();
            }
        }

        private void TransferFile()
        {
            var fr = new FileTransferHelper();
            fr.CopyFileCallbackEventHandler += fr_CopyFileCallbackEventHandler;
            var fiSoruce = new FileInfo(m_strSourceFileName);
            var fiDestination = new FileInfo(m_strDestinationFileName);
            CopyFileCallback copuFileCallback = fr_CopyFileCallbackEventHandler;
            fr.CopyFile(fiSoruce, fiDestination, CopyFileOptions.None, copuFileCallback);
        }

        private CopyFileCallbackAction fr_CopyFileCallbackEventHandler(
            FileInfo source,
            FileInfo destination,
            object state,
            long totalFileSize,
            long totalBytesTransferred)
        {
            m_intTransfered = totalBytesTransferred;
            UpdateProgress();
            if (m_blnRunTransfer)
            {
                return CopyFileCallbackAction.Continue;
            }
            else
            {
                return CopyFileCallbackAction.Cancel;
            }
        }


        private void CheckFileProgress()
        {
            while (true)
            {
                if (FileHelper.Exists(m_strDestinationFileName))
                {
                    Thread.Sleep(5000);
                    var br = new BinaryReader(File.Open(
                        m_strDestinationFileName,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read));
                    m_intTransfered = m_br.BaseStream.Length;
                    br.Close();
                    UpdateProgress();
                }
            }
        }

        private void TransferVolumesFiles()
        {
            m_bw = new BinaryWriter(File.Open(
                m_strDestinationFileName,
                FileMode.Append));
            m_br = new StreamReader(m_strSourceFileName);
            var chunksize = 10*2048*2048;

            var buffer = new byte[chunksize];
            var intCounter = 0;
            while (m_br.BaseStream.Length > ((chunksize)*((long) intCounter)))
            {
                var intTest = chunksize*intCounter;
                if (m_br.BaseStream.Length > ((chunksize)*((long) (intCounter + 1))))
                {
                    m_br.BaseStream.Read(buffer, 0, chunksize);
                    m_intTransfered += chunksize;
                }
                else
                {
                    var remainLen = (int) (m_br.BaseStream.Length - (((long) chunksize)*(intCounter)));
                    buffer = new byte[remainLen];
                    m_br.BaseStream.Read(buffer, 0, remainLen);
                    m_intTransfered += remainLen;
                }
                m_bw.Write(buffer);
                UpdateProgress();
                intCounter++;
            }
            m_br.Close();
            m_bw.Close();
        }


        private void UpdateProgress()
        {
            if (progressBarEventHandler != null)
            {
                var intPercentage = (m_intTransfered*100)/m_intFileSize;
                if (progressBarEventHandler.GetInvocationList().Length > 0)
                {
                    var intElapsedTime =
                        (int) intPercentage == 0
                            ? 0
                            : (int) (1 + ((100 - intPercentage)*
                                          (DateTime.Now - m_startTime).TotalMinutes)/intPercentage);
                    intElapsedTime = intElapsedTime < 0 ? 0 : intElapsedTime;
                    var fi = new FileInfo(m_strSourceFileName);
                    progressBarEventHandler(
                        "Transfering file: " + fi.Name +
                        ", " + intElapsedTime +
                        " mins remaining", (int) intPercentage);
                }
            }
        }

        public void CopyFile(FileInfo source, FileInfo destination)
        {
            CopyFile(source, destination, CopyFileOptions.None);
        }

        public void CopyFile(FileInfo source, FileInfo destination,
                             CopyFileOptions options)
        {
            CopyFile(source, destination, options, null);
        }

        public void CopyFile(FileInfo source, FileInfo destination,
                             CopyFileOptions options, CopyFileCallback callback)
        {
            CopyFile(source, destination, options, callback, null);
        }

        public void CopyFile(FileInfo source, FileInfo destination,
                             CopyFileOptions options, CopyFileCallback callback, object state)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            if ((options & ~CopyFileOptions.All) != 0)
            {
                throw new ArgumentOutOfRangeException("options");
            }

            new FileIOPermission(
                FileIOPermissionAccess.Read, source.FullName).Demand();
            new FileIOPermission(
                FileIOPermissionAccess.Write, destination.FullName).Demand();

            var cpr = callback == null
                          ? null
                          : new CopyProgressRoutine(new CopyProgressData(
                                                        source, destination, callback, state).
                                                        CallbackHandler);

            var cancel = false;
            if (!CopyFileEx(source.FullName, destination.FullName, cpr,
                            IntPtr.Zero, ref cancel, (int) options))
            {
                throw new IOException(new Win32Exception().Message);
            }
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CopyFileEx(
            string lpExistingFileName, string lpNewFileName,
            CopyProgressRoutine lpProgressRoutine,
            IntPtr lpData, ref bool pbCancel, int dwCopyFlags);

        #region Nested type: CopyProgressData

        private class CopyProgressData
        {
            private readonly CopyFileCallback _callback;
            private readonly FileInfo _destination;
            private readonly FileInfo _source;
            private readonly object _state;

            public CopyProgressData(FileInfo source, FileInfo destination,
                                    CopyFileCallback callback, object state)
            {
                _source = source;
                _destination = destination;
                _callback = callback;
                _state = state;
            }

            public int CallbackHandler(
                long totalFileSize, long totalBytesTransferred,
                long streamSize, long streamBytesTransferred,
                int streamNumber, int callbackReason,
                IntPtr sourceFile, IntPtr destinationFile, IntPtr data)
            {
                return (int) _callback(_source, _destination, _state,
                                       totalFileSize, totalBytesTransferred);
            }
        }

        #endregion

        #region Nested type: CopyProgressRoutine

        private delegate int CopyProgressRoutine(
            long totalFileSize, long TotalBytesTransferred, long streamSize,
            long streamBytesTransferred, int streamNumber, int callbackReason,
            IntPtr sourceFile, IntPtr destinationFile, IntPtr data);

        #endregion
    }


    public enum CopyFileCallbackAction
    {
        Continue = 0,
        Cancel = 1,
        Stop = 2,
        Quiet = 3
    }

    [Flags]
    public enum CopyFileOptions
    {
        None = 0x0,
        FailIfDestinationExists = 0x1,
        Restartable = 0x2,
        AllowDecryptedDestination = 0x8,
        All = FailIfDestinationExists | Restartable | AllowDecryptedDestination
    }
}


