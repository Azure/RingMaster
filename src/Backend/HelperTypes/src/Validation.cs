// <copyright file="Validation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

#nullable enable
#pragma warning disable SA1615
#pragma warning disable SA1618
#pragma warning disable SA1611
#pragma warning disable SA1402
    /// <summary>
    /// Contains a set of helper methods for validating input arguments or a state of objects.
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.
        /// </summary>
        /// <remarks>
        /// You don't have to specify <paramref name="paramName"/> manually because C# compiler (10 and higher) can do that automatically.
        /// I.e. <code>myArgumentName.ThrowIfNull()</code> will get 'myArgumentName' as <paramref name="paramName"/>.
        /// Plus it can infer any expression that was used for producing the <paramref name="argument"/> even if the expression is fairly complicated,
        /// like <code>GetSomething().ThrowIfNull()'</code> as the <paramref name="paramName"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        [return: NotNull]
        public static T ThrowIfNull<T>(
            [ValidatedNotNull][NotNull] this T? argument,
            [CallerArgumentExpression("argument")] string paramName = "")
            where T : class
        {
            if (argument == null)
            {
                ThrowArgumentNullException(paramName);
            }

            return argument;
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        [return: NotNull]
        public static T ThrowIfNull<T>(
            [ValidatedNotNull][NotNull] this T? argument,
            [CallerArgumentExpression("argument")] string paramName = "",
            string? extraMessage = null)
            where T : struct
        {
            if (argument == null)
            {
                ThrowArgumentNullException(paramName, extraMessage);
            }

            return argument.Value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if a result of a completed <paramref name="task"/> is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        [return: NotNull]
        public static async Task<T> ThrowIfNull<T>(
            [ValidatedNotNull][NotNull] this Task<T?> task,
            [CallerArgumentExpression("task")] string paramName = "")
            where T : class
        {
            var result = await task.ConfigureAwait(false);
            if (result == null)
            {
                ThrowArgumentNullException(paramName);
            }

            return result;
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if a result of a completed <paramref name="task"/> is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        [return: NotNull]
        public static async Task<T> ThrowIfNull<T>(
            [ValidatedNotNull][NotNull] this Task<T?> task,
            [CallerArgumentExpression("task")] string paramName = "",
            string? extraMessage = null)
            where T : struct
        {
            var result = await task.ConfigureAwait(false);
            if (result == null)
            {
                ThrowArgumentNullException(paramName, extraMessage);
            }

            return result.Value;
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/>. Used to make the calling method inlinable by the JIT.
        /// </summary>
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string paramName, string? message = null)
        {
            if (message is null)
            {
                throw new ArgumentNullException(paramName);
            }

            throw new ArgumentNullException(paramName, message);
        }
    }

    /// <summary>
    /// Disables code analysis warnings on arguments requiring validation, e.g.
    /// <list type="bullet">
    /// <item>CA1062: ValidateArgumentsOfPublicMethods</item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}
#pragma warning restore SA1618
#pragma warning restore SA1615
#pragma warning restore SA1611
#pragma warning restore SA1402