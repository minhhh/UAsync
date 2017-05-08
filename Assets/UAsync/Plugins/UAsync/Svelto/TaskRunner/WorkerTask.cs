using System;
using System.Collections;
using UnityEngine;
using System.Threading;

namespace UAsync.Svelto.Tasks.Internal
{
    public class WorkerTask: IEnumerator
    {
        public object Current
        {
            get {
                if (enumerator != null) {
                    return enumerator.Current;
                }

                return null;
            }
        }

        public bool MoveNext ()
        {
            if (stopped) {
                return false;
            }

            return enumerator.MoveNext ();
        }

        public void Reset ()
        {
            throw new NotSupportedException ();
        }

        internal WorkerTask (IRunner runner)
        {
            this.runner = runner;
        }

        CallbackDelegate onComplete;

        internal void Start (IEnumerator task, CallbackDelegate onComplete = null)
        {
            if (enumerator is WorkerTask) {
                throw new ArgumentException ("Use of incompatible WorkerTask");
            }

            // Warn: IsBusy must be false
            this.onComplete = onComplete;
            enumerator = task;
            stopped = false;
            var currentCbId = cbId;
            runner.StartCoroutine (this, (err, res) => OnTaskCompleted (currentCbId, err, res));
        }

        protected void OnTaskCompleted (long currentCbId, object err, object res)
        {
            if (cbId != currentCbId) {
                return;
            }

            Interlocked.Increment (ref cbId);

            enumerator = null;
            stopped = true;

            if (this.onComplete != null) {
                onComplete (err, res);
                onComplete = null;
            }
        }

        // This should never been called twice or after finishing the job
        // TaskRoutine wraps this function and make sure of it
        internal void Cancel ()
        {
            Interlocked.Increment (ref cbId);

            onComplete = null;
            enumerator = null;
            stopped = true;
        }

        IRunner runner;
        bool stopped = true;
        long cbId = Int64.MinValue;

        public bool IsBusy
        {
            get {
                return !stopped;
            }
        }

        IEnumerator enumerator;
    }
}

