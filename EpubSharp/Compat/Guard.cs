using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EpubSharp
{
    internal static class Guard
    {
        public static void NotNull([NotNull] object argument, [CallerArgumentExpression("argument")] string paramName = null)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(argument, paramName);
#else
            if (argument is null) throw new System.ArgumentNullException(paramName);
#endif
        }

        public static void NotNullOrWhiteSpace([NotNull] string argument, [CallerArgumentExpression("argument")] string paramName = null)
        {
#if NET8_0_OR_GREATER
            ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
#else
            if (string.IsNullOrWhiteSpace(argument))
            {
                if (argument is null) throw new System.ArgumentNullException(paramName);
                throw new System.ArgumentException("The value cannot be empty or whitespace.", paramName);
            }
#endif
        }

        public static void IsTrue(bool condition, string message, [CallerArgumentExpression("condition")] string paramName = null)
        {
            if (!condition)
            {
                throw new System.ArgumentException(message, paramName);
            }
        }
    }
}

#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute { }
}
#endif

#if !NETCOREAPP3_0_OR_GREATER && !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}
#endif
