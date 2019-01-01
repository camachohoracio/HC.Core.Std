#region

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using HC.Core.Io.Serialization;
using HC.Core.Logging;
using ICSharpCode.SharpZipLib.Zip;

#endregion

namespace HC.Core.Zip
{
    public static class MemoryZipper
    {
        public static MemoryStream ZipWthFastSerializer(object obj)
        {
            byte[] bytes = Serializer.GetBytesFast(obj);
            return ZipInMemoryBytes(bytes);
        }

        public static MemoryStream ZipInMemoryBytes(byte[] bytes)
        {
            try
            {
                ZipOutputStream os;
                GetStreams(out os);

                os.Write(bytes, 0, bytes.Length);
                os.CloseEntry();
                MemoryStream newMs = GetZippedStream(os);
                return newMs;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        public static MemoryStream ZipInMemory(object obj)
        {
            try
            {
                ZipOutputStream os;
                GetStreams(out os);

                SerializeObject(obj, os);
                MemoryStream newMs = GetZippedStream(os);
                return newMs;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private static MemoryStream GetZippedStream(ZipOutputStream os)
        {
            try
            {
                var buff = new byte[1024];

                Stream zippedStream = os;
                var reader = new BinaryReader(zippedStream);
                zippedStream.Position = 0;
                var newMs = new MemoryStream();
                int n;
                while ((n = reader.Read(buff, 0, buff.Length)) > 0)
                {
                    newMs.Write(buff, 0, n);
                }
                return newMs;
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
            return null;
        }

        private static void SerializeObject(object obj, ZipOutputStream os)
        {
            try
            {
                var bf = new BinaryFormatter();
                var fs = new MemoryStream();
                bf.Serialize(fs, obj);
                fs.Position = 0;

                var buff = new byte[1024];
                int n;
                while ((n = fs.Read(buff, 0, buff.Length)) > 0)
                {
                    os.Write(buff, 0, n);
                }
                fs.Close();
                os.CloseEntry();
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void GetStreams(out ZipOutputStream os)
        {
            os = null;
            try
            {
                var ms = new MemoryStream();
                os = new ZipOutputStream(ms);

                var ze = new ZipEntry("")
                             {
                                 CompressionMethod = CompressionMethod.Deflated
                             };
                os.PutNextEntry(ze);
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public static T UnZipMemory<T>(byte[] bytes)
        {
            return (T)UnZipMemory(new MemoryStream(bytes));
        }

        public static T UnZipMemoryFast<T>(byte[] bytes)
        {
            return UnZipMemoryFast<T>(new MemoryStream(bytes));
        }

        public static T UnZipMemoryFast<T>(Stream ms)
        {
            try
            {
                MemoryStream writer = UnzipMemoryStream(ms);
                return Serializer.DeserializeFastFromBytes<T>(writer.GetBuffer());
            }
            catch (Exception e2)
            {
                Console.WriteLine(e2.Message);
            }
            return default(T);
        }

        public static object UnZipMemory(Stream ms)
        {
            try
            {
                MemoryStream writer = UnzipMemoryStream(ms);
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(writer);
            }
            catch (Exception e2)
            {
                Console.WriteLine(e2.Message);
            }
            return null;
        }

        private static MemoryStream UnzipMemoryStream(Stream ms)
        {
            ms.Position = 0;
            var zip = new ZipInputStream(ms);
            zip.GetNextEntry();
            var writer = new MemoryStream();
            var buffer = new Byte[1024];
            int numberread = zip.Read(buffer, 0, buffer.Length);
            while (numberread > 0)
            {
                writer.Write(buffer, 0, numberread);
                numberread = zip.Read(buffer, 0, buffer.Length);
            }
            writer.Position = 0;
            return writer;
        }
    }
}

