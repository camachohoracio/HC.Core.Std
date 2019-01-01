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
using System.Collections.Generic;
using HC.Core.Reflection.Managed.Reflection.Metadata;

namespace HC.Core.Reflection.Managed.Reflection.Reader
{
    sealed class MethodDefImpl : MethodInfo
    {
        private readonly ModuleReader module;
        private readonly int index;
        private readonly TypeDefImpl declaringType;
        private MethodSignature lazyMethodSignature;
        private ParameterInfo returnParameter;
        private ParameterInfo[] parameters;
        private Type[] typeArgs;

        internal MethodDefImpl(ModuleReader module, TypeDefImpl declaringType, int index)
        {
            this.module = module;
            this.index = index;
            this.declaringType = declaringType;
        }

        public override MethodBody GetMethodBody()
        {
            return GetMethodBody(this);
        }

        internal MethodBody GetMethodBody(IGenericContext context)
        {
            if ((GetMethodImplementationFlags() & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL)
            {
                // method is not IL
                return null;
            }
            int rva = module.MethodDef.records[index].RVA;
            return rva == 0 ? null : new MethodBody(module, rva, context);
        }

        public override int __MethodRVA
        {
            get { return module.MethodDef.records[index].RVA; }
        }

        public override CallingConventions CallingConvention
        {
            get { return this.MethodSignature.CallingConvention; }
        }

        public override MethodAttributes Attributes
        {
            get { return (MethodAttributes)module.MethodDef.records[index].Flags; }
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return (MethodImplAttributes)module.MethodDef.records[index].ImplFlags;
        }

        public override ParameterInfo[] GetParameters()
        {
            PopulateParameters();
            return (ParameterInfo[])parameters.Clone();
        }

        private void PopulateParameters()
        {
            if (parameters == null)
            {
                MethodSignature methodSignature = this.MethodSignature;
                parameters = new ParameterInfo[methodSignature.GetParameterCount()];
                int parameter = module.MethodDef.records[index].ParamList - 1;
                int end = module.MethodDef.records.Length > index + 1 ? module.MethodDef.records[index + 1].ParamList - 1 : module.Param.records.Length;
                for (; parameter < end; parameter++)
                {
                    int seq = module.Param.records[parameter].Sequence - 1;
                    if (seq == -1)
                    {
                        returnParameter = new ParameterInfoImpl(this, seq, parameter);
                    }
                    else
                    {
                        parameters[seq] = new ParameterInfoImpl(this, seq, parameter);
                    }
                }
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] == null)
                    {
                        parameters[i] = new ParameterInfoImpl(this, i, -1);
                    }
                }
                if (returnParameter == null)
                {
                    returnParameter = new ParameterInfoImpl(this, -1, -1);
                }
            }
        }

        internal override int ParameterCount
        {
            get { return this.MethodSignature.GetParameterCount(); }
        }

        public override ParameterInfo ReturnParameter
        {
            get
            {
                PopulateParameters();
                return returnParameter;
            }
        }

        public override Type ReturnType
        {
            get
            {
                return this.MethodSignature.GetReturnType(this);
            }
        }

        public override Type DeclaringType
        {
            get { return declaringType.IsModulePseudoType ? null : declaringType; }
        }

        public override string Name
        {
            get { return module.GetString(module.MethodDef.records[index].Name); }
        }

        public override int MetadataToken
        {
            get { return (MethodDefTable.Index << 24) + index + 1; }
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                PopulateGenericArguments();
                return typeArgs.Length > 0;
            }
        }

        public override bool IsGenericMethod
        {
            get { return IsGenericMethodDefinition; }
        }

        public override Type[] GetGenericArguments()
        {
            PopulateGenericArguments();
            return Util.Copy(typeArgs);
        }

        private void PopulateGenericArguments()
        {
            if (typeArgs == null)
            {
                int token = this.MetadataToken;
                int first = module.GenericParam.FindFirstByOwner(token);
                if (first == -1)
                {
                    typeArgs = Type.EmptyTypes;
                }
                else
                {
                    List<Type> list = new List<Type>();
                    int len = module.GenericParam.records.Length;
                    for (int i = first; i < len && module.GenericParam.records[i].Owner == token; i++)
                    {
                        list.Add(new GenericTypeParameter(module, i, Signature.ELEMENT_TYPE_MVAR));
                    }
                    typeArgs = list.ToArray();
                }
            }
        }

        internal override Type GetGenericMethodArgument(int index)
        {
            PopulateGenericArguments();
            return typeArgs[index];
        }

        internal override int GetGenericMethodArgumentCount()
        {
            PopulateGenericArguments();
            return typeArgs.Length;
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            if (this.IsGenericMethodDefinition)
            {
                return this;
            }
            throw new InvalidOperationException();
        }

        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            return new GenericMethodInstance(declaringType, this, typeArguments);
        }

        public override Module Module
        {
            get { return module; }
        }

        internal override MethodSignature MethodSignature
        {
            get { return lazyMethodSignature ?? (lazyMethodSignature = MethodSignature.ReadSig(module, module.GetBlob(module.MethodDef.records[index].Signature), this)); }
        }

        internal override int ImportTo(Emit.ModuleBuilder module)
        {
            return module.ImportMethodOrField(declaringType, this.Name, this.MethodSignature);
        }

        public override MethodInfo[] __GetMethodImpls()
        {
            Type[] typeArgs = null;
            List<MethodInfo> list = null;
            foreach (int i in module.MethodImpl.Filter(declaringType.MetadataToken))
            {
                if (module.MethodImpl.records[i].MethodBody == this.MetadataToken)
                {
                    if (typeArgs == null)
                    {
                        typeArgs = declaringType.GetGenericArguments();
                    }
                    if (list == null)
                    {
                        list = new List<MethodInfo>();
                    }
                    list.Add((MethodInfo)module.ResolveMethod(module.MethodImpl.records[i].MethodDeclaration, typeArgs, null));
                }
            }
            return Util.ToArray(list, Empty<MethodInfo>.Array);
        }

        internal override int GetCurrentToken()
        {
            return this.MetadataToken;
        }

        internal override bool IsBaked
        {
            get { return true; }
        }
    }

    sealed class ParameterInfoImpl : ParameterInfo
    {
        private readonly MethodDefImpl method;
        private readonly int position;
        private readonly int index;

        internal ParameterInfoImpl(MethodDefImpl method, int position, int index)
        {
            this.method = method;
            this.position = position;
            this.index = index;
        }

        public override string Name
        {
            get { return index == -1 ? null : ((ModuleReader)this.Module).GetString(this.Module.Param.records[index].Name); }
        }

        public override Type ParameterType
        {
            get { return position == -1 ? method.MethodSignature.GetReturnType(method) : method.MethodSignature.GetParameterType(method, position); }
        }

        public override ParameterAttributes Attributes
        {
            get { return index == -1 ? ParameterAttributes.None : (ParameterAttributes)this.Module.Param.records[index].Flags; }
        }

        public override int Position
        {
            get { return position; }
        }

        public override object RawDefaultValue
        {
            get
            {
                if ((this.Attributes & ParameterAttributes.HasDefault) != 0)
                {
                    return this.Module.Constant.GetRawConstantValue(this.Module, this.MetadataToken);
                }
                Universe universe = this.Module.universe;
                if (this.ParameterType == universe.System_Decimal)
                {
                    Type attr = universe.System_Runtime_CompilerServices_DecimalConstantAttribute;
                    if (attr != null)
                    {
                        foreach (CustomAttributeData cad in CustomAttributeData.__GetCustomAttributes(this, attr, false))
                        {
                            IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
                            if (args.Count == 5)
                            {
                                if (args[0].ArgumentType == universe.System_Byte
                                    && args[1].ArgumentType == universe.System_Byte
                                    && args[2].ArgumentType == universe.System_Int32
                                    && args[3].ArgumentType == universe.System_Int32
                                    && args[4].ArgumentType == universe.System_Int32)
                                {
                                    return new Decimal((int)args[4].Value, (int)args[3].Value, (int)args[2].Value, (byte)args[1].Value != 0, (byte)args[0].Value);
                                }
                                else if (args[0].ArgumentType == universe.System_Byte
                                    && args[1].ArgumentType == universe.System_Byte
                                    && args[2].ArgumentType == universe.System_UInt32
                                    && args[3].ArgumentType == universe.System_UInt32
                                    && args[4].ArgumentType == universe.System_UInt32)
                                {
                                    return new Decimal(unchecked((int)(uint)args[4].Value), unchecked((int)(uint)args[3].Value), unchecked((int)(uint)args[2].Value), (byte)args[1].Value != 0, (byte)args[0].Value);
                                }
                            }
                        }
                    }
                }
                if ((this.Attributes & ParameterAttributes.Optional) != 0)
                {
                    return Missing.Value;
                }
                return null;
            }
        }

        public override CustomModifiers __GetCustomModifiers()
        {
            return position == -1
                ? method.MethodSignature.GetReturnTypeCustomModifiers(method)
                : method.MethodSignature.GetParameterCustomModifiers(method, position);
        }

        public override bool __TryGetFieldMarshal(out FieldMarshal fieldMarshal)
        {
            return FieldMarshal.ReadFieldMarshal(this.Module, this.MetadataToken, out fieldMarshal);
        }

        public override MemberInfo Member
        {
            get
            {
                // return the right ConstructorInfo wrapper
                return method.Module.ResolveMethod(method.MetadataToken);
            }
        }

        public override int MetadataToken
        {
            get
            {
                // for parameters that don't have a row in the Param table, we return 0x08000000 (because index is -1 in that case),
                // just like .NET
                return (ParamTable.Index << 24) + index + 1;
            }
        }

        internal override Module Module
        {
            get { return method.Module; }
        }
    }
}
