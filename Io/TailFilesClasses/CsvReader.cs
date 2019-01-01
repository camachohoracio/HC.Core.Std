#region

using System;
using System.Collections;
using System.IO;
using System.Text;

#endregion

namespace HC.Core.Io.TailFilesClasses
{
    /// <summary>
    ///   A data-reader style interface for reading CSV files.
    /// </summary>
    /// <remark>
    ///   Adapted from http://www.heikniemi.net/hc/archives/files/CsvReader.cs
    /// </remark>
    public class CSVReader : IDisposable
    {
        #region Private variables

        private readonly StreamReader reader;
        private readonly char sep = ',';
        private readonly Stream stream;

        #endregion

        /// <summary>
        ///   Create a new reader for the given stream.
        /// </summary>
        /// <param name = "s">The stream to read the CSV from.</param>
        public CSVReader(Stream s) : this(s, null, ',')
        {
        }

        /// <summary>
        ///   Create a new reader for the given stream.
        /// </summary>
        /// <param name = "s">The stream to read the CSV from.</param>
        public CSVReader(Stream s, char sep) : this(s, null, sep)
        {
        }

        /// <summary>
        ///   Create a new reader for the given stream and encoding.
        /// </summary>
        /// <param name = "s">The stream to read the CSV from.</param>
        /// <param name = "enc">The encoding used.</param>
        public CSVReader(Stream s, Encoding enc, char sep)
        {
            stream = s;
            if (!s.CanRead)
            {
                throw new CsvReaderException("Could not read the given CSV stream!");
            }
            reader = (enc != null) ? new StreamReader(s, enc) : new StreamReader(s);
            this.sep = sep;
        }

        /// <summary>
        ///   Creates a new reader for the given text file path.
        /// </summary>
        /// <param name = "filename">The name of the file to be read.</param>
        public CSVReader(string filename)
            : this(new FileStream(filename, FileMode.Open), null, ',')
        {
        }

        /// <summary>
        ///   Creates a new reader for the given text file path and seperator.
        /// </summary>
        /// <param name = "filename">The name of the file to be read.</param>
        public CSVReader(string filename, char sep)
            : this(new FileStream(filename, FileMode.Open), null, sep)
        {
        }

        /// <summary>
        ///   Creates a new reader for the given text file path and encoding.
        /// </summary>
        /// <param name = "filename">The name of the file to be read.</param>
        /// <param name = "enc">The encoding used.</param>
        public CSVReader(string filename, Encoding enc)
            : this(new FileStream(filename, FileMode.Open), enc, ',')
        {
        }

        #region IDisposable Members

        /// <summary>
        ///   Disposes the CsvReader. The underlying stream is closed.
        /// </summary>
        public void Dispose()
        {
            // Closing the reader closes the underlying stream, too
            if (reader != null)
            {
                reader.Close();
            }
            else if (stream != null)
            {
                stream.Close(); // In case we failed before the reader was constructed
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        ///   Returns the fields for the next row of CSV data (or null if at eof)
        /// </summary>
        /// <returns>A string array of fields or null if at the end of file.</returns>
        public string[] GetCSVLine()
        {
            var data = reader.ReadLine();
            if (data == null)
            {
                return null;
            }
            if (data.Length == 0)
            {
                return new string[0];
            }

            var result = new ArrayList();

            ParseCSVFields(result, data);

            return (string[]) result.ToArray(typeof (string));
        }

        // Parses the CSV fields and pushes the fields into the result arraylist
        private void ParseCSVFields(ArrayList result, string data)
        {
            var pos = -1;
            while (pos < data.Length)
            {
                result.Add(ParseCSVField(data, ref pos));
            }
        }

        // Parses the field at the given position of the data, modified pos to match
        // the first unparsed position and returns the parsed field
        private string ParseCSVField(string data, ref int startSeparatorPosition)
        {
            if (startSeparatorPosition == data.Length - 1)
            {
                startSeparatorPosition++;
                // The last field is empty
                return "";
            }

            var fromPos = startSeparatorPosition + 1;

            // Determine if this is a quoted field
            if (data[fromPos] == '"')
            {
                // If we're at the end of the string, let's consider this a field that
                // only contains the quote
                if (fromPos == data.Length - 1)
                {
                    fromPos++;
                    return "\"";
                }

                // Otherwise, return a string of appropriate Length with double quotes collapsed
                // Note that FSQ returns data.Length if no single quote was found
                var nextSingleQuote = FindSingleQuote(data, fromPos + 1);
                startSeparatorPosition = nextSingleQuote + 1;
                return data.Substring(fromPos + 1, nextSingleQuote - fromPos - 1).Replace("\"\"", "\"");
            }

            // The field ends in the next comma or EOL
            var nextComma = data.IndexOf(sep, fromPos);
            if (nextComma == -1)
            {
                startSeparatorPosition = data.Length;
                return data.Substring(fromPos);
            }
            else
            {
                startSeparatorPosition = nextComma;
                return data.Substring(fromPos, nextComma - fromPos);
            }
        }

        // Returns the index of the next single quote mark in the string 
        // (starting from startFrom)
        private int FindSingleQuote(string data, int startFrom)
        {
            var i = startFrom - 1;
            while (++i < data.Length)
            {
                if (data[i] == '"')
                {
                    // If this is a double quote, bypass the chars
                    if (i < data.Length - 1 && data[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }
                    else
                    {
                        return i;
                    }
                }
            }
            // If no quote found, return the end value of i (data.Length)
            return i;
        }
    }


    /// <summary>
    ///   HCException class for CsvReader exceptions.
    /// </summary>
    public class CsvReaderException : ApplicationException
    {
        /// <summary>
        ///   Constructs a new HCException object with the given message.
        /// </summary>
        /// <param name = "message">The exception message.</param>
        public CsvReaderException(string message) : base(message)
        {
        }
    }
}


