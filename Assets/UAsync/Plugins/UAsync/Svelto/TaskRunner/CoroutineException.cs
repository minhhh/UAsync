using System;
using System.Text;

namespace UAsync
{
    class CoroutineException : Exception
    {
        public CoroutineException (string message, Exception innerException) : base (message, innerException)
        {
        }

        public CoroutineException (string message) : base (message)
        {
        }

        public override string StackTrace
        {
            get {
                return "STACK TRACE" + Environment.NewLine + Environment.NewLine + base.StackTrace;
            }
        }

        public override string ToString ()
        {
            return Message + Environment.NewLine + Environment.NewLine + StackTrace;
        }
    }

}
