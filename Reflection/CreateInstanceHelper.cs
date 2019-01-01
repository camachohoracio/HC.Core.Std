#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

#endregion

namespace HC.Core.Reflection
{
    public class CreateInstanceHelper<T>
    {
        #region Delegate & events

        private delegate object CreateCtor(
            object[] args);

        #endregion

        #region Members

        private readonly IReflector m_reflector;
        private readonly ConstructorInfo m_constructorInfo;
        private CreateCtor m_createdCtorDelegate;
        private readonly List<string> m_readWriteProperties;

        #endregion

        #region Constructors

        public CreateInstanceHelper()
        {
            ConstructorInfo[] constructors = 
                typeof (T).GetConstructors();

            ConstructorInfo constructor = constructors[0];
            if (constructors.Length > 1)
            {
                int intMinParams = (from n in constructors
                                    select n.GetParameters().Count()).Min();
                constructor = (from n in constructors
                               where n.GetParameters().Count() ==
                               intMinParams
                               select n).First();
            }

            m_constructorInfo = constructor;
            CreateCtorDelegate();
            m_reflector = ReflectorCache.GetReflector(typeof (T));
            m_readWriteProperties = new List<string>();
            foreach(string strProperty in m_reflector.GetPropertyNames())
            {
                if(m_reflector.CanWriteProperty(strProperty))
                {
                    m_readWriteProperties.Add(strProperty);
                }
            }
        }

        #endregion

        #region Public

        public T CreateInstance(
            object[] constructorObjects,
            object[] propertyObjects)
        {
            //
            // build object
            //
            var tobj = (T) m_createdCtorDelegate(
                constructorObjects);

            //
            // add properties
            //
            if (propertyObjects != null)
            {
                //var propertyNames = m_binderObj.GetPropertyNames();
                if (propertyObjects.Length != m_readWriteProperties.Count)
                {
                    throw new Exception("Invalid property size");
                }
                for (var i = 0; i < propertyObjects.Length; i++)
                {
                    m_reflector.SetPropertyValue(
                        tobj,
                        m_readWriteProperties[i],
                        propertyObjects[i]);
                }
            }
            return tobj;
        }

        #endregion

        #region Private

        private void CreateCtorDelegate()
        {
            var method = new DynamicMethod(
                "CreateInstance",
                typeof (T),
                new[] {typeof (object[])});
            var gen = method.GetILGenerator();

            var intParamCount = 0;
            ConstructorInfo[] constructors = typeof (T).GetConstructors();
            ConstructorInfo constructor = constructors[0];
            if (constructors.Length > 1)
            {
                int intMinParams = (from n in constructors
                    select n.GetParameters().Count()).Min();
                constructor = (from n in constructors
                 where n.GetParameters().Count() ==
                 intMinParams
                 select n).First();
            }
            foreach (ParameterInfo param in constructor.GetParameters())
            {
                gen.Emit(OpCodes.Ldarg_0); //arr
                var paramType = param.ParameterType;
                gen.Emit(OpCodes.Ldc_I4, intParamCount);
                gen.Emit(OpCodes.Ldelem_Ref);
                if (paramType == typeof (string))
                {
                    gen.Emit(OpCodes.Castclass, paramType);
                }
                else
                {
                    gen.Emit(OpCodes.Unbox_Any, paramType);
                }
                intParamCount++;
            }
            gen.Emit(OpCodes.Newobj, m_constructorInfo); // ne created
            gen.Emit(OpCodes.Ret);
            m_createdCtorDelegate = (CreateCtor) method.CreateDelegate(typeof (CreateCtor));
        }

        #endregion
    }
}


