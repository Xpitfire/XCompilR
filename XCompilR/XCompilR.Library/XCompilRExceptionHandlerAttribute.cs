using System;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Patterns.Diagnostics;

namespace XCompilR.Library
{
    /// <summary>
    /// This class uses aspect-oriented concepts to handle exceptions.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class XCompilRExceptionHandlerAttribute : OnExceptionAspect
    {
        private readonly Type _exceptionType;
        
        /// <summary>
        /// The created exception handling aspect uses a specified exception and return type.
        /// Only exceptions of this type will be handled.
        /// The resulting object type will be of the generic implementation type.
        /// </summary>
        /// <param name="exceptionType">Exceptions which will be handled.</param>
        public XCompilRExceptionHandlerAttribute(Type exceptionType)
        {
            _exceptionType = exceptionType;
        }

        /// <summary>
        /// Returns the exception type which has been caught.
        /// </summary>
        /// <param name="method"></param>
        /// <returns>Type of the occured exception</returns>
        public override Type GetExceptionType(MethodBase method) => _exceptionType;

        /// <summary>
        /// Exception handling logic. Implements the base behavior if an exception of type
        /// T occures.
        /// </summary>
        /// <param name="args">Executing method.</param>
        [Log]
        public override void OnException(MethodExecutionArgs args)
        {
            if (_exceptionType ==  typeof(XCompileException))
            {
                // set the method behavior after an exception occurred
                args.FlowBehavior = FlowBehavior.Return;
                
                // handle exception
                Console.WriteLine(args.Exception.StackTrace);
            }
        }
    }
}
