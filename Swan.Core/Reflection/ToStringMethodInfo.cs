﻿using System;
using System.Reflection;

namespace Swan.Reflection
{
    internal sealed class ToStringMethodInfo : CommonMethodInfo
    {
        public ToStringMethodInfo(ExtendedTypeInfo typeInfo)
            : base(typeInfo, nameof(byte.TryParse))
        {
            // placeholder
        }

        protected override MethodInfo? RetriveMethodInfo(ExtendedTypeInfo typeInfo, string methodName) =>
            typeInfo.UnderlyingType.GetMethod(methodName, new[] { typeof(IFormatProvider) }) ??
            typeInfo.UnderlyingType.GetMethod(methodName, Array.Empty<Type>());
    }
}
