using System.Collections;
using System;

namespace UAsync.Svelto.Tasks
{
    public delegate void CallbackDelegate (object err = null, object res = null);

    public interface IRunner
    {
        void    StartCoroutine (IEnumerator task, CallbackDelegate onComplete);

        void     StopAllCoroutines ();

        bool    stopped { get; }
    }
}
