using UAsync.Svelto.Tasks.Internal;
using System;
using System.Collections;
using UnityEngine;

namespace UAsync.Svelto.Tasks
{
    public class SingleTask: IEnumerator
    {
        public object Current
        {
            get {
                return _enumerator.Current;
            }
        }

        public SingleTask (IEnumerator enumerator)
        {
            if (enumerator is SingleTask || enumerator is WorkerTask) {
                throw new ArgumentException ("Use of incompatible Enumerator, cannot be SingleTask/PausableTask/AsyncTask");
            }

            if (enumerator == null) {
                throw new ArgumentNullException ();
            }

            _enumerator = enumerator;
        }

        public SingleTask (YieldInstruction yieldInstruction)
        {
            if (yieldInstruction == null) {
                throw new ArgumentNullException ();
            }

            _enumerator = TaskHelper.ToEnumerator (yieldInstruction);
        }

        public SingleTask (Action action)
        {
            if (action == null) {
                throw new ArgumentNullException ();
            }

            _enumerator = TaskHelper.ToEnumerator (action);
        }

        public bool MoveNext ()
        {
            return _enumerator.MoveNext ();
        }

        public void Reset ()
        {
            throw new NotSupportedException ();
        }

        IEnumerator _enumerator;
    }
}

