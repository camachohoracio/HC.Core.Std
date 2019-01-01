/*
  The MIT License (MIT) 
  Copyright (C) 2009-2012 Jeroen Frijters
  
  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:
  
  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.
  
  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HC.Core.Reflection.Managed.Reflection.Reader;

namespace HC.Core.Reflection.Managed.Reflection
{
    public sealed class AssemblyName
    {
        private string name;
        private string culture;
        private Version version;
        private byte[] publicKeyToken;
        private byte[] publicKey;
        private StrongNameKeyPair keyPair;
        private AssemblyNameFlags flags;
        private AssemblyHashAlgorithm hashAlgorithm;
        private AssemblyVersionCompatibility versionCompatibility = AssemblyVersionCompatibility.SameMachine;
        private string codeBase;
        internal byte[] hash;

        public AssemblyName()
        {
        }

        public AssemblyName(string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException("assemblyName");
            }
            if (assemblyName == "")
            {
                throw new ArgumentException();
            }
            ParsedAssemblyName parsed;
            switch (Fusion.ParseAssemblyName(assemblyName, out parsed))
            {
                case ParseAssemblyResult.GenericError:
                case ParseAssemblyResult.DuplicateKey:
                    throw new FileLoadException();
            }
            if (!ParseVersion(parsed.Version, parsed.Retargetable.HasValue, out version))
            {
                throw new FileLoadException();
            }
            name = parsed.Name;
            if (parsed.Culture != null)
            {
                if (parsed.Culture.Equals("neutral", StringComparison.OrdinalIgnoreCase))
                {
                    culture = "";
                }
                else if (parsed.Culture == "")
                {
                    throw new FileLoadException();
                }
                else
                {
                    culture = new CultureInfo(parsed.Culture).Name;
                }
            }
            if (parsed.PublicKeyToken != null)
            {
                if (parsed.PublicKeyToken.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    publicKeyToken = Empty<byte>.Array;
                }
                else if (parsed.PublicKeyToken.Length != 16)
                {
                    throw new FileLoadException();
                }
                else
                {
                    publicKeyToken = ParseKey(parsed.PublicKeyToken);
                }
            }
            if (parsed.Retargetable.HasValue)
            {
                if (parsed.Culture == null || parsed.PublicKeyToken == null || version == null)
                {
                    throw new FileLoadException();
                }
                if (parsed.Retargetable.Value)
                {
                    flags |= AssemblyNameFlags.Retargetable;
                }
            }
            ProcessorArchitecture = parsed.ProcessorArchitecture;
            if (parsed.WindowsRuntime)
            {
                ContentType = AssemblyContentType.WindowsRuntime;
            }
        }

        private static byte[] ParseKey(string key)
        {
            if ((key.Length & 1) != 0)
            {
                throw new FileLoadException();
            }
            byte[] buf = new byte[key.Length / 2];
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte)(ParseHexDigit(key[i * 2]) * 16 + ParseHexDigit(key[i * 2 + 1]));
            }
            return buf;
        }

        private static int ParseHexDigit(char digit)
        {
            if (digit >= '0' && digit <= '9')
            {
                return digit - '0';
            }
            else
            {
                digit |= (char)0x20;
                if (digit >= 'a' && digit <= 'f')
                {
                    return 10 + digit - 'a';
                }
                else
                {
                    throw new FileLoadException();
                }
            }
        }

        public override string ToString()
        {
            return FullName;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public CultureInfo CultureInfo
        {
            get { return culture == null ? null : new CultureInfo(culture); }
            set { culture = value == null ? null : value.Name; }
        }

        public string CultureName
        {
            get { return culture; }
            set { culture = value; }
        }

        public Version Version
        {
            get { return version; }
            set { version = value; }
        }

        public StrongNameKeyPair KeyPair
        {
            get { return keyPair; }
            set { keyPair = value; }
        }

        public string CodeBase
        {
            get { return codeBase; }
            set { codeBase = value; }
        }

        public ProcessorArchitecture ProcessorArchitecture
        {
            get { return (ProcessorArchitecture)(((int)flags & 0x70) >> 4); }
            set
            {
                if (value >= ProcessorArchitecture.None && value <= ProcessorArchitecture.Arm)
                {
                    flags = (flags & ~(AssemblyNameFlags)0x70) | (AssemblyNameFlags)((int)value << 4);
                }
            }
        }

        public AssemblyNameFlags Flags
        {
            get { return flags & (AssemblyNameFlags)~0xEF0; }
            set { flags = (flags & (AssemblyNameFlags)0xEF0) | (value & (AssemblyNameFlags)~0xEF0); }
        }

        public AssemblyVersionCompatibility VersionCompatibility
        {
            get { return versionCompatibility; }
            set { versionCompatibility = value; }
        }

        public AssemblyContentType ContentType
        {
            get { return (AssemblyContentType)(((int)flags & 0xE00) >> 9); }
            set
            {
                if (value >= AssemblyContentType.Default && value <= AssemblyContentType.WindowsRuntime)
                {
                    flags = (flags & ~(AssemblyNameFlags)0xE00) | (AssemblyNameFlags)((int)value << 9);
                }
            }
        }

        public byte[] GetPublicKey()
        {
            return publicKey;
        }

        public void SetPublicKey(byte[] publicKey)
        {
            this.publicKey = publicKey;
            flags = (flags & ~AssemblyNameFlags.PublicKey) | (publicKey == null ? 0 : AssemblyNameFlags.PublicKey);
        }

        public byte[] GetPublicKeyToken()
        {
            if (publicKeyToken == null && publicKey != null)
            {
                // note that GetPublicKeyToken() has a side effect in this case, because we retain this token even after the public key subsequently gets changed
                publicKeyToken = ComputePublicKeyToken(publicKey);
            }
            return publicKeyToken;
        }

        public void SetPublicKeyToken(byte[] publicKeyToken)
        {
            this.publicKeyToken = publicKeyToken;
        }

        public AssemblyHashAlgorithm HashAlgorithm
        {
            get { return hashAlgorithm; }
            set { hashAlgorithm = value; }
        }

        public byte[] __Hash
        {
            get { return hash; }
        }

        public string FullName
        {
            get
            {
                if (name == null)
                {
                    return "";
                }
                ushort versionMajor = 0xFFFF;
                ushort versionMinor = 0xFFFF;
                ushort versionBuild = 0xFFFF;
                ushort versionRevision = 0xFFFF;
                if (version != null)
                {
                    versionMajor = (ushort)version.Major;
                    versionMinor = (ushort)version.Minor;
                    versionBuild = (ushort)version.Build;
                    versionRevision = (ushort)version.Revision;
                }
                byte[] publicKeyToken = this.publicKeyToken;
                if ((publicKeyToken == null || publicKeyToken.Length == 0) && publicKey != null)
                {
                    publicKeyToken = ComputePublicKeyToken(publicKey);
                }
                return GetFullName(name, versionMajor, versionMinor, versionBuild, versionRevision, culture, publicKeyToken, (int)flags);
            }
        }

        internal static string GetFullName(string name, ushort versionMajor, ushort versionMinor, ushort versionBuild, ushort versionRevision, string culture, byte[] publicKeyToken, int flags)
        {
            StringBuilder sb = new StringBuilder();
            bool doubleQuotes = name.StartsWith(" ") || name.EndsWith(" ") || name.IndexOf('\'') != -1;
            bool singleQuotes = name.IndexOf('"') != -1;
            if (singleQuotes)
            {
                sb.Append('\'');
            }
            else if (doubleQuotes)
            {
                sb.Append('"');
            }
            if (name.IndexOf(',') != -1 || name.IndexOf('\\') != -1 || name.IndexOf('=') != -1 || (singleQuotes && name.IndexOf('\'') != -1))
            {
                for (int i = 0; i < name.Length; i++)
                {
                    char c = name[i];
                    if (c == ',' || c == '\\' || c == '=' || (singleQuotes && c == '\''))
                    {
                        sb.Append('\\');
                    }
                    sb.Append(c);
                }
            }
            else
            {
                sb.Append(name);
            }
            if (singleQuotes)
            {
                sb.Append('\'');
            }
            else if (doubleQuotes)
            {
                sb.Append('"');
            }
            if (versionMajor != 0xFFFF)
            {
                sb.Append(", Version=").Append(versionMajor);
                if (versionMinor != 0xFFFF)
                {
                    sb.Append('.').Append(versionMinor);
                    if (versionBuild != 0xFFFF)
                    {
                        sb.Append('.').Append(versionBuild);
                        if (versionRevision != 0xFFFF)
                        {
                            sb.Append('.').Append(versionRevision);
                        }
                    }
                }
            }
            if (culture != null)
            {
                sb.Append(", Culture=").Append(culture == "" ? "neutral" : culture);
            }
            if (publicKeyToken != null)
            {
                sb.Append(", PublicKeyToken=");
                if (publicKeyToken.Length == 0)
                {
                    sb.Append("null");
                }
                else
                {
                    AppendPublicKey(sb, publicKeyToken);
                }
            }
            if ((flags & (int)AssemblyNameFlags.Retargetable) != 0)
            {
                sb.Append(", Retargetable=Yes");
            }
            if ((AssemblyContentType)((flags & 0xE00) >> 9) == AssemblyContentType.WindowsRuntime)
            {
                sb.Append(", ContentType=WindowsRuntime");
            }
            return sb.ToString();
        }

        internal static byte[] ComputePublicKeyToken(byte[] publicKey)
        {
            if (publicKey.Length == 0)
            {
                return publicKey;
            }
            byte[] hash;
            using (SHA1 sha1 = SHA1.Create())
            {
                hash = sha1.ComputeHash(publicKey);
            }
            byte[] token = new byte[8];
            for (int i = 0; i < token.Length; i++)
            {
                token[i] = hash[hash.Length - 1 - i];
            }
            return token;
        }

        internal static string ComputePublicKeyToken(string publicKey)
        {
            StringBuilder sb = new StringBuilder(16);
            AppendPublicKey(sb, ComputePublicKeyToken(ParseKey(publicKey)));
            return sb.ToString();
        }

        private static void AppendPublicKey(StringBuilder sb, byte[] publicKey)
        {
            for (int i = 0; i < publicKey.Length; i++)
            {
                sb.Append("0123456789abcdef"[publicKey[i] >> 4]);
                sb.Append("0123456789abcdef"[publicKey[i] & 0x0F]);
            }
        }

        public override bool Equals(object obj)
        {
            AssemblyName other = obj as AssemblyName;
            return other != null && other.FullName == this.FullName;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        public object Clone()
        {
            AssemblyName copy = (AssemblyName)MemberwiseClone();
            copy.publicKey = Copy(publicKey);
            copy.publicKeyToken = Copy(publicKeyToken);
            return copy;
        }

        private static byte[] Copy(byte[] b)
        {
            return b == null || b.Length == 0 ? b : (byte[])b.Clone();
        }

        public static AssemblyName GetAssemblyName(string path)
        {
            try
            {
                path = Path.GetFullPath(path);
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ModuleReader module = new ModuleReader(null, null, fs, path, false);
                    if (module.Assembly == null)
                    {
                        throw new BadImageFormatException("Module does not contain a manifest");
                    }
                    return module.Assembly.GetName();
                }
            }
            catch (IOException x)
            {
                throw new FileNotFoundException(x.Message, x);
            }
            catch (UnauthorizedAccessException x)
            {
                throw new FileNotFoundException(x.Message, x);
            }
        }

        internal AssemblyNameFlags RawFlags
        {
            get { return flags; }
            set { flags = value; }
        }

        private static bool ParseVersion(string str, bool mustBeComplete, out Version version)
        {
            if (str == null)
            {
                version = null;
                return true;
            }
            string[] parts = str.Split('.');
            if (parts.Length < 2 || parts.Length > 4)
            {
                version = null;
                ushort dummy;
                // if the version consists of a single integer, it is invalid, but not invalid enough to fail the parse of the whole assembly name
                return parts.Length == 1 && ushort.TryParse(parts[0], NumberStyles.Integer, null, out dummy);
            }
            if (parts[0] == "" || parts[1] == "")
            {
                // this is a strange scenario, the version is invalid, but not invalid enough to fail the parse of the whole assembly name
                version = null;
                return true;
            }
            ushort major, minor, build = 65535, revision = 65535;
            if (ushort.TryParse(parts[0], NumberStyles.Integer, null, out major)
                && ushort.TryParse(parts[1], NumberStyles.Integer, null, out minor)
                && (parts.Length <= 2 || parts[2] == "" || ushort.TryParse(parts[2], NumberStyles.Integer, null, out build))
                && (parts.Length <= 3 || parts[3] == "" || (parts[2] != "" && ushort.TryParse(parts[3], NumberStyles.Integer, null, out revision))))
            {
                if (mustBeComplete && (parts.Length < 4 || parts[2] == "" || parts[3] == ""))
                {
                    version = null;
                }
                else if (major == 65535 || minor == 65535)
                {
                    version = null;
                }
                else
                {
                    version = new Version(major, minor, build, revision);
                }
                return true;
            }
            version = null;
            return false;
        }
    }
}
