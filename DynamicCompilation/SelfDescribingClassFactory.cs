#region

using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using HC.Core.ConfigClasses;
using HC.Core.Io.Serialization.Interfaces;
using HC.Core.Io.Serialization.Readers;
using HC.Core.Io.Serialization.Writers;
using Microsoft.CSharp;
using HC.Core.Exceptions;
using HC.Core.Helpers;
using HC.Core.Io;
using HC.Core.Io.Serialization.Types;
using HC.Core.Logging;
using HC.Core.Reflection;

#endregion

namespace HC.Core.DynamicCompilation
{
    [Serializable]
    public class SelfDescribingClassFactory : IDisposable
    {
        #region Properties

        [XmlArray("UsingList")]
        [XmlArrayItem("Using", typeof (string))]
        public List<string> UsingList { get; set; }

        [XmlArray("Properties")]
        [XmlArrayItem("Property", typeof (PropertyPair))]
        public List<PropertyPair> Properties
        {
            get
            {
                return m_properties;
            }
            set
            {
                m_properties = value;
                ResetPropertyLookups();
            }
        }

        private void ResetPropertyLookups()
        {
            m_propertyLookup = new HashSet<string>();
            foreach (PropertyPair propertyPair in m_properties)
            {
                m_propertyLookup.Add(propertyPair.Name.ToLower());
            }
        }

        [XmlArray("Assemblies")]
        [XmlArrayItem("Assembly", typeof (string))]
        public List<string> ReferencedAssembly { get; set; }

        [NonSerialized]
        [XmlIgnore]
        private readonly Dictionary<string, string > m_events;

        [XmlArray("Interfaces")]
        [XmlArrayItem("Interface", typeof (string))]
        public List<string> Interfaces { get; set; }

        [XmlArray("Methods")]
        [XmlArrayItem("Method", typeof (string))]
        public List<string> Methods { get; set; }

        [XmlArray("Members")]
        [XmlArrayItem("Member", typeof(string))]
        public List<string> Members { get; set; }

        [XmlArray("MethodNamess")]
        [XmlArrayItem("MethodName", typeof (string))]
        public List<string> MethodNames { get; set; }

        [XmlArray("Classes")]
        [XmlArrayItem("Classes", typeof(string))]
        public List<string> Classes { get; set; }

        public string NameSpace { get; set; }

        public string ClassName
        {
            get
            {
                return m_strClassName;
            }
            set
            {
                //
                // normalize class name
                //
                string strClassName = value
                    .Replace("-", "_")
                    .Replace(".", "_")
                    .Replace("`", "_")
                    .Replace("%", "_")
                    .Replace("^","_");
                strClassName = char.IsNumber(strClassName[0])
                                   ? "a" + strClassName
                                   : strClassName;

                m_strClassName = strClassName;
            }
        }

        #endregion

        #region Members

        private static Dictionary<string, SelfDescribingClassFactory> m_classFactories;
        private static readonly object m_lockObject = new object();
        private Assembly m_assembly;
        private Type m_type;
        private string m_strClassName;
        private HashSet<string> m_propertyLookup;
        private List<PropertyPair> m_properties;
        private static readonly CompilerParameters m_compilerParameters;
        private static readonly CSharpCodeProvider m_compiler;
        private static readonly object m_compilerLock = new object();
        private static readonly ConcurrentDictionary<string, object> m_assemblyValidator;
        private static readonly object m_assemblyLock = new object();

        #endregion

        #region Constructors

        static SelfDescribingClassFactory()
        {
            m_assemblyValidator = new ConcurrentDictionary<string, object>();
            m_compilerParameters = new CompilerParameters
                                     {
                                         GenerateExecutable = false,
                                         GenerateInMemory = true,
                                         IncludeDebugInformation = false,
                                         WarningLevel = 3,
                                         TreatWarningsAsErrors = false,
                                         CompilerOptions = "/optimize"
                                     };
            m_compiler = new CSharpCodeProvider();
        }

        /// <summary>
        ///   Used for xml serialization
        /// </summary>
        public SelfDescribingClassFactory()
        {
            m_events = new Dictionary<string, string>();
            m_propertyLookup = new HashSet<string>();
            UsingList = new List<string>();
            Interfaces = new List<string>();
            Properties = new List<PropertyPair>();
            ReferencedAssembly = new List<string>();
            Methods = new List<string>();
            Members = new List<string>();
            MethodNames = new List<string>();
            Classes = new List<string>();

            SetDefaults();
        }

        public SelfDescribingClassFactory(
            string className,
            string strNameSpace) : this()
        {
            ClassName = className;
            NameSpace = strNameSpace.Replace( "^",string.Empty );
        }

        #endregion

        #region Public

        public void SaveSchema()
        {
            SelfDescribingClassHelper.SaveSchema(
                ClassName,
                this);
        }

        public static ASelfDescribingClass BuildSelfDecrigingClassFromString(
            string strDescr,
            Enum enumClassName)
        {
            return BuildSelfDecrigingClassFromString(
                strDescr,
                enumClassName.ToString());
        }

        public static ASelfDescribingClass BuildSelfDecrigingClassFromString(
            string strDescr,
            string strClassName)
        {
            var constDict =
                HCConfig.GetConfigConstantsFromString(
                    strDescr).ConstantDict;

            var selfDescribingClass = GetSelfDescribingClassFromConstants(strClassName, constDict);
            return selfDescribingClass;
        }

        public static ASelfDescribingClass BuildSelfDescribingClassFromXml(
            string strClassName,
            string strXmlFileName)
        {
            var constDict =
                HCConfig.GetConfigConstants(
                    strXmlFileName,
                    true).ConstantDict;

            ASelfDescribingClass selfDescribingClass = GetSelfDescribingClassFromConstants(
                strClassName, 
                constDict);
            return selfDescribingClass;
        }

        private static ASelfDescribingClass GetSelfDescribingClassFromConstants(
            string strClassName,
            Dictionary<string, object> constDict)
        {
            var selfDescribingClassFactory = CreateFactory(
                strClassName,
                typeof (SelfDescribingClassFactory).Namespace);

            //
            // add all properties as strings
            //
            foreach (KeyValuePair<string, object> keyValuePair in constDict)
            {
                selfDescribingClassFactory.AddProperty(
                    keyValuePair.Key,
                    keyValuePair.Value.GetType());
            }

            var selfDescribingClass = selfDescribingClassFactory.CreateInstance();

            // 
            // set property values
            //
            foreach (KeyValuePair<string, object> keyValuePair in constDict)
            {
                selfDescribingClass.SetValueToDictByType(
                    keyValuePair.Key,
                    keyValuePair.Value);
            }
            return selfDescribingClass;
        }

        public static SelfDescribingClassFactory CreateFactory(
            string strClassName,
            string strNameSpace)
        {
            //
            // make factory thread-safe
            //
            lock (m_lockObject)
            {
                if (m_classFactories == null)
                {
                    m_classFactories =
                        new Dictionary<string, SelfDescribingClassFactory>();
                }

                SelfDescribingClassFactory selfDescribingClassFactory;
                if (!m_classFactories.TryGetValue(strClassName,
                                                  out selfDescribingClassFactory))
                {
                    //
                    // get class factory from xml schema file
                    //
                    selfDescribingClassFactory = SelfDescribingClassHelper.GetSerializedClassFactory(
                        strClassName);
                    if (selfDescribingClassFactory == null)
                    {
                        //
                        // there is no schema, therefore, 
                        // create an empty factory
                        //
                        selfDescribingClassFactory = new SelfDescribingClassFactory(
                            strClassName,
                            strNameSpace);
                    }
                    else 
                    {
                        //
                        // there was a schema. We need to update property lookups
                        //
                        selfDescribingClassFactory.ResetPropertyLookups();
                    }
                    m_classFactories.Add(strClassName,
                                         selfDescribingClassFactory);
                }
                return selfDescribingClassFactory;
            }
        }

        public void AddInterface(
            Type type)
        {
            AddUsingStatement(type);
            AddInterface(type.Name);
        }

        public void AddInterface(
            string strInterfaceName)
        {
            if (!Interfaces.Contains(strInterfaceName))
            {
                Interfaces.Add(strInterfaceName);
            }
        }

        public void AddUsingStatement(Type type)
        {
            AddUsingStatement(type.Namespace);
        }

        public void AddUsingStatement(string strUsing)
        {
            try
            {
                if (string.IsNullOrEmpty(strUsing))
                {
                    return;
                }
                if (!UsingList.Contains(strUsing))
                {
                    UsingList.Add(strUsing);
                    ResetClassFactory();
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public bool ContainsPropertyName(string strPropertyName)
        {
            return m_propertyLookup.Contains(strPropertyName.ToLower());
        }

        public void AddMethod(
            bool blnIsPublic,
            bool blnIsOverride,
            string strMethodName,
            [Optional] Type returnType,
            List<KeyValuePair<string, Type>> methodParams,
            string strBody)
        {
            //
            // do not repeat method names
            //
            if (MethodNames.Contains(strMethodName))
            {
                return;
            }

            MethodNames.Add(strMethodName);

            var strMethodBody = GetMethodBody(
                blnIsPublic, 
                blnIsOverride, 
                strMethodName, 
                returnType, 
                methodParams, 
                strBody);

            Methods.Add(strMethodBody);
        }

        private static string GetMethodBody(bool blnIsPublic, 
            bool blnIsOverride, 
            string strMethodName, 
            Type returnType,
            List<KeyValuePair<string, Type>> methodParams, 
            string strBody)
        {
            var sb = new StringBuilder();
            sb.Append(
                blnIsPublic ? "public" : "private")
                .Append(" ")
                .Append(blnIsOverride ? "override " : string.Empty)
                .Append(
                    returnType == null ? "void" : returnType.Name)
                .Append(" ")
                .Append(strMethodName)
                .Append("(");

            //
            // add params
            //
            if (methodParams != null)
            {
                bool blnIsFirst = true;
                foreach (KeyValuePair<string, Type> keyValuePair in methodParams)
                {
                    if (!blnIsFirst)
                    {
                        sb.Append(",");
                    }
                    else
                    {
                        blnIsFirst = false;
                    }
                    sb.AppendLine(keyValuePair.Value.Name)
                        .Append(" ")
                        .Append(keyValuePair.Key);
                }
            }
            sb.AppendLine(")")
                .AppendLine("{")
                .AppendLine("try{")
                .AppendLine(strBody)
                .AppendLine("} catch(Exception ex){Logger.Log(ex);}");

                if(returnType != null)
                {
                    sb.Append(GetDefaultReturnValue(returnType));
                }

                sb.Append("}");
            string strMethodBody =
                sb.ToString();
            return strMethodBody;
        }

        private static string GetDefaultReturnValue(Type type)
        {
            if(type == typeof(bool))
            {
                return "return false;";
            }
            if (type == typeof(string))
            {
                return "return null;";
            }
            if (type == typeof(int) ||
                type == typeof(double) ||
                type == typeof(long))
            {
                return "return 0;";
            }
            if (type == typeof(DateTime))
            {
                return "return new DateTime();";
            }
            return "return null;";
        }

        public void AddProperty(string strPropertyName, Type type)
        {
            AddProperty(strPropertyName, ComplexTypeParser.ToStringType(type));
        }

        public void AddProperty(string strPropertyName, string strTypeName)
        {
            try
            {
                strPropertyName = ValidatePoperty(strPropertyName);
                if (!ContainsPropertyName(strPropertyName))
                {
                    lock (Properties)
                    {
                        strPropertyName = strPropertyName
                            .Replace(".", "_");
                        if (!ContainsPropertyName(strPropertyName))
                        {
                            Properties.Add(
                                new PropertyPair
                                    {
                                        Name = strPropertyName,
                                        Type = strTypeName
                                    });
                            m_propertyLookup.Add(strPropertyName.ToLower());
                            ResetClassFactory();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private string ValidatePoperty(string strPropertyName)
        {
            if(!char.IsLetter(strPropertyName[0]))
            {
                strPropertyName = "_" + strPropertyName;
            }
            strPropertyName = StringHelper.RemoveNonLettersNumbers(strPropertyName, '_');
            if(strPropertyName.Length > 100)
            {
                strPropertyName = strPropertyName.Substring(0, 99);
            }
            return strPropertyName;
        }

        public void AddReferencedAssembly(Assembly assembly)
        {
            AddReferencedAssembly(assembly.FullName);
        }

        public void AddReferencedAssembly(Type type)
        {
            if (!typeof(ASelfDescribingClass).IsAssignableFrom(type))
            {
                AddReferencedAssembly(FileHelper.GetAssemblyFullFileName(type));
            }
        }

        public void AddReferencedAssembly(string strReferencedAssembly)
        {
            //
            // allow unique file names
            //
            strReferencedAssembly = strReferencedAssembly.ToLower();
            string strAssemblyName =
                new FileInfo(strReferencedAssembly).Name.ToLower().Trim();

            foreach (string strAssembly in ReferencedAssembly)
            {
                if (new FileInfo(strAssembly).Name.ToLower().Trim().Equals(strAssemblyName))
                {
                    //
                    // an assembly with the same name has already been added
                    //
                    return;
                }
            }

            if (!ReferencedAssembly.Contains(strReferencedAssembly) &&
                (strReferencedAssembly.EndsWith(".dll") || strReferencedAssembly.EndsWith(".exe")))
            {
                ReferencedAssembly.Add(strReferencedAssembly);
                ResetClassFactory();
            }
        }

        public ASelfDescribingClass CreateInst(
            ASelfDescribingClass selfDescribingClass)
        {
            return CreateInstance(selfDescribingClass,
                                  this);
        }

        public static ASelfDescribingClass CreateInstance(
            ASelfDescribingClass selfDescribingClass)
        {
            string strClassName = selfDescribingClass.GetClassName();

            if (string.IsNullOrEmpty(strClassName))
            {
                throw new HCException("Invalid class name");
            }

            SelfDescribingClassFactory selfDescribingClassFactory = CreateFactory(
                strClassName,
                typeof (SelfDescribingClassFactory).Namespace);
            return CreateInstance(
                selfDescribingClass,
                selfDescribingClassFactory);
        }

        public static ASelfDescribingClass CreateInstance(
            ASelfDescribingClass selfDescribingClass,
            SelfDescribingClassFactory selfDescribingClassFactory)
        {
            var strClassName = selfDescribingClass.GetClassName();

            if (string.IsNullOrEmpty(strClassName))
            {
                throw new HCException("Invalid class name");
            }

            var valuesToSet =
                new Dictionary<string, object>();


            Type givenType = selfDescribingClass.GetType();
            //if (givenType != typeof(ASelfDescribingClass))
            //{
                var expressionCache = ReflectorCache.GetReflector(givenType);

                foreach (string strPropertyName in selfDescribingClass.GetHardPropertyNames())
                {
                    var objValue = expressionCache.GetPropertyValue(
                        selfDescribingClass,
                        strPropertyName);
                    objValue = (objValue ?? string.Empty);
                    selfDescribingClassFactory.AddProperty(
                        strPropertyName,
                        expressionCache.GetPropertyType(strPropertyName));
                    valuesToSet[strPropertyName] =
                        objValue;
                }
            //}

            //
            // set dbl values
            //
            var dblValues = selfDescribingClass.GetDblValues();
            lock (dblValues)
            {
                foreach (KeyValuePair<string, double> keyValuePair in dblValues)
                {
                    var strKey = CleanPropertyName(keyValuePair.Key);
                    selfDescribingClassFactory.AddProperty(
                        strKey,
                        typeof (double));
                    valuesToSet[strKey] = keyValuePair.Value;
                }
            }

            //
            // set int values
            //
            var intValues = selfDescribingClass.GetIntValues();
            lock (intValues)
            {
                foreach (KeyValuePair<string, int> keyValuePair in intValues)
                {
                    var strKey = CleanPropertyName(keyValuePair.Key);
                    selfDescribingClassFactory.AddProperty(
                        strKey,
                        typeof (int));
                    valuesToSet[strKey] = keyValuePair.Value;
                }
            }

            //
            // set bln values
            //
            var blnValues = selfDescribingClass.GetBlnValues();
            lock (blnValues)
            {
                foreach (KeyValuePair<string, bool> keyValuePair in blnValues)
                {
                    var strKey = CleanPropertyName(keyValuePair.Key);
                    selfDescribingClassFactory.AddProperty(
                        strKey,
                        typeof (bool));
                    valuesToSet[strKey] = keyValuePair.Value;
                }
            }

            //
            // set long values
            //
            var lngValues = selfDescribingClass.GetLngValues();
            lock (lngValues)
            {
                foreach (KeyValuePair<string, long> keyValuePair in lngValues)
                {
                    var strKey = CleanPropertyName(keyValuePair.Key);
                    selfDescribingClassFactory.AddProperty(
                        strKey,
                        typeof (long));
                    valuesToSet[strKey] = keyValuePair.Value;
                }
            }

            //
            // set date values
            //
            var dateValues = selfDescribingClass.GetDateValues();
            lock (dateValues)
            {
                foreach (KeyValuePair<string, DateTime> keyValuePair in dateValues)
                {
                    var strKey = CleanPropertyName(keyValuePair.Key);
                    selfDescribingClassFactory.AddProperty(
                        strKey,
                        typeof (DateTime));
                    valuesToSet[strKey] = keyValuePair.Value;
                }
            }

            //
            // set string values
            //
            var strValues = selfDescribingClass.GetStringValues();
            lock (strValues)
            {
                foreach (KeyValuePair<string, string> keyValuePair in strValues)
                {
                    var strKey = CleanPropertyName(keyValuePair.Key);
                    selfDescribingClassFactory.AddProperty(
                        strKey,
                        typeof (string));
                    valuesToSet[strKey] = keyValuePair.Value;
                }
            }

            var newSelfDescribingClass =
                selfDescribingClassFactory.CreateInstance();

            //
            // set property values
            //
            var binder = ReflectorCache.GetReflector(newSelfDescribingClass.GetType());
            foreach (string strPropertyName in binder.GetPropertyNames())
            {
                if(!binder.CanWriteProperty(strPropertyName))
                {
                    continue;
                }
                object objPropertyValue;
                if (valuesToSet.TryGetValue(
                    strPropertyName,
                    out objPropertyValue))
                {
                    newSelfDescribingClass.SetHardPropertyValue(
                        strPropertyName,
                        objPropertyValue);
                }
            }

            return newSelfDescribingClass;
        }

        private static string CleanPropertyName(
            string strKey)
        {
            return strKey
                .Replace("/", "_")
                .Replace("/", "_")
                .Replace("-", "_")
                .Replace("*", "_");
        }

        public ASelfDescribingClass CreateInstance()
        {
            if (m_assembly == null)
            {
                CompileAssembly();
            }
            //
            // if this is a new type, then we add the type to the known types cache
            //
            IReflector reflector = ReflectorCache.GetReflector(m_type);

            object obj = reflector.CreateInstance();
            if (obj == null)
            {
                throw new HCException("Couldn't load class.");
            }

            var selfDescribingClass = (ASelfDescribingClass) obj;
            selfDescribingClass.SetClassName(ClassName);
            
            SetDefaultPropertyValues(obj, reflector);
            return (ASelfDescribingClass) obj;
        }

        private static void SetDefaultPropertyValues(
            object obj, 
            IReflector reflector)
        {
            IEnumerable<string> propertyNames = from n in reflector.GetPropertyNames()
                                                where reflector.CanWriteProperty(n)
                                                select n;
            foreach (string strPropertyName in propertyNames)
            {
                Type propertyType = reflector.GetPropertyType(strPropertyName);
                if (!typeof (Enum).IsAssignableFrom(propertyType))
                {
                    object objValue = propertyType.IsValueType
                                          ? ReflectionHelper.GetDefaultValueType(propertyType)
                                          : null;
                    reflector.SetPropertyValue(obj, strPropertyName, objValue);
                }
            }
        }

        #endregion

        #region Private

        private void CompileAssembly()
        {
            string strCode = ParseClass();

            CompileAssembly(strCode);
        }

        private void CompileAssembly(string strCode)
        {
            //
            // Add referenced assemblies
            //
            foreach (string strCurrAssembly in ReferencedAssembly)
            {
                string strAssembly = strCurrAssembly;
                if (!FileHelper.Exists(
                    strAssembly,
                    false))
                {
                    var strTmpAssembly = Path.Combine(
                        FileHelper.GetExecutingAssemblyDir(),
                        new FileInfo(strAssembly).Name);
                    if (!FileHelper.ExistsFileLocal(
                        strTmpAssembly))
                    {
                        Logger.Log("Assembly file not found in path: " +
                                   strTmpAssembly);
                    }
                    else
                    {
                        strAssembly = strTmpAssembly;
                    }
                }

                string strAssemblyName = new FileInfo(strAssembly).Name.ToLower();
                if (!m_assemblyValidator.ContainsKey(strAssemblyName))
                {
                    lock (m_assemblyLock)
                    {
                        if (!m_assemblyValidator.ContainsKey(strAssemblyName))
                        {
                            m_assemblyValidator[strAssemblyName] = null;

                            if (!m_compilerParameters.ReferencedAssemblies.Contains(strAssembly))
                            {
                                m_compilerParameters.ReferencedAssemblies.Add(strAssembly);
                            }
                        }
                    }
                }
            }

            //
            // Compile the whole class
            //
            CompilerResults compilerResults;
            lock (m_compilerLock)
            {
                compilerResults =
                    m_compiler.CompileAssemblyFromSource(
                        m_compilerParameters,
                        strCode);
            }

            if (compilerResults.Errors.HasErrors)
            {
                // *** Create Error String
                string strErrorMsg = "Dynamic class runtime compile error. Num of errors = " +
                                     compilerResults.Errors.Count + ". Errors:";
                for (var x = 0; x < compilerResults.Errors.Count; x++)
                {
                    strErrorMsg = strErrorMsg + "\r\nLine: " +
                                  compilerResults.Errors[x].Line + " - " +
                                  compilerResults.Errors[x].ErrorText;
                }
                throw new HCException(strErrorMsg + "\r\n\r\n" + strCode);
            }
            m_assembly = compilerResults.CompiledAssembly;
            m_type = m_assembly.GetType(NameSpace + "." + ClassName);
        }

        public Type GetType(
            string strClassName)
        {
            return m_assembly.GetType(
                            NameSpace + "." +
                            strClassName);            
        }

        private void SetDefaults()
        {
            //
            // add default referenced assemblies
            //
            ReferencedAssembly.Add("System.dll");
            ReferencedAssembly.Add("netstandard.dll");
            //ReferencedAssembly.Add("mscorlib.xmlserializers.dll");
            string strAssemblyFileName = FileHelper.GetAssemblyFullFileName(
                typeof (ASelfDescribingClass));
            ReferencedAssembly.Add(strAssemblyFileName);
            strAssemblyFileName = FileHelper.GetAssemblyFullFileName(
                typeof(IXmlSerializable));
            ReferencedAssembly.Add(strAssemblyFileName);
            //
            // add default namespace
            //
            UsingList.Add("System");
            UsingList.Add("System.Collections.Generic");
            UsingList.Add("System.Text");
            UsingList.Add("System.Reflection");
            UsingList.Add("System.Collections.Generic");
            UsingList.Add(GetType().Namespace);
            UsingList.Add(typeof(ComplexTypeSerializer).Namespace);
            UsingList.Add(typeof(ISerializerReader).Namespace);
            UsingList.Add(typeof(ISerializerWriter).Namespace);
            UsingList.Add(typeof(ISerializable).Namespace);
            UsingList.Add(typeof(EnumSerializedType).Namespace);
            UsingList.Add(typeof(ComplexTypeSerializer).Namespace);
            UsingList.Add(typeof(Logger).Namespace);
        }

        private void ResetClassFactory()
        {
            //
            // the structure of the class has changed. 
            // Therefore we need to create a new assembly
            //
            m_assembly = null;
        }

        public string ParseClass()
        {
            var sb = new StringBuilder();
            //
            // add using statements
            //
            foreach (string strUsing in UsingList.Distinct())
            {
                sb.AppendLine("using " + strUsing + ";");
            }

            string strParsedClass = ParseClassWithoutUsiung();
            sb.Append(strParsedClass);
            return sb.ToString();
        }

        public string ParseClassWithoutUsiung()
        {
            var sb = new StringBuilder();
            //
            // namespace
            //
            if (string.IsNullOrEmpty(NameSpace))
            {
                NameSpace = GetType().Namespace;
            }
            sb.AppendLine("namespace " + NameSpace);
            sb.AppendLine("{");

            //
            // class name
            //
            sb.AppendLine("[Serializable]");
            sb.AppendLine("public class " + ClassName +
                          ": " + typeof (ASelfDescribingClass).Name);

            //
            // add interfaces
            //
            foreach (string strInterface in Interfaces)
            {
                sb.Append("," + strInterface);
            }

            //
            // start of class
            //
            sb.AppendLine("{");

            //
            // add events
            //
            foreach (KeyValuePair<string, string> keyValuePair in m_events)
            {
                sb.AppendLine("public event " +
                              keyValuePair.Value + " " +
                              keyValuePair.Key + ";");
            }

            //
            // add properties
            //
            lock (Properties)
            {
                foreach (PropertyPair keyValuePair in Properties)
                {
                    sb.AppendLine(
                        "public " +
                        keyValuePair.Type + " " +
                        keyValuePair.Name +
                        " { get; set; }");
                }
            }

            //
            // add members
            //
            foreach (string strMembers in Members)
            {
                sb.AppendLine(strMembers);
            }

            //
            // add methods
            //
            foreach (string strMethod in Methods)
            {
                sb.AppendLine(strMethod);
            }

            //
            // end of class
            //
            sb.AppendLine("}");

            //
            // end of namespace
            //
            sb.AppendLine("}");

            foreach (string strClass in Classes)
            {
                sb.AppendLine(strClass);
            }
            return sb.ToString();
        }

        #endregion

        public void AddMember(string strMember)
        {
            Members.Add(strMember);
        }

        public void AddEvent(
            string strEventName, 
            string strEventType)
        {
            if (!m_events.ContainsKey(strEventName))
            {
                m_events[strEventName] = strEventType;
            }
        }

        public void AddClass(string strParsedClass)
        {
            Classes.Add(strParsedClass);
        }

        public void Dispose()
        {
        }

        public void AddUsingStatement(List<Type> strUsing)
        {
            try
            {
                if (strUsing == null)
                {
                    return;
                }
                foreach (Type type in strUsing)
                {
                    AddUsingStatement(type);
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex);
            }
        }
    }
}


