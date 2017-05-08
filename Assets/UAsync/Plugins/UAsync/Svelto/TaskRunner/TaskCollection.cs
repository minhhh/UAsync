using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UAsync.Svelto.Tasks
{
    abstract public class TaskCollection: IEnumerable
    {
        protected Queue<IEnumerator>     registeredEnumerators { get; private set; }

        public int                        tasksRegistered { get { return registeredEnumerators.Count; } }

        abstract public                 float progress { get; }

        public TaskCollection ()
        {
            registeredEnumerators = new Queue<IEnumerator> ();
        }

        public void Add (IEnumerable enumerable)
        {
            if (enumerable is TaskCollection) {
                registeredEnumerators.Enqueue (new EnumeratorWithProgress (enumerable.GetEnumerator (),
                                               () => (enumerable as TaskCollection).progress));

                if ((enumerable as TaskCollection).tasksRegistered == 0) {
                    Debug.LogWarning ("Avoid to register zero size collections");
                }
            } else {
                registeredEnumerators.Enqueue (enumerable.GetEnumerator ());
            }

            if (enumerable == null) {
                throw new ArgumentNullException ();
            }
        }

        public void Add (IEnumerator enumerator)
        {
            if (enumerator == null) {
                throw new ArgumentNullException ();
            }

            registeredEnumerators.Enqueue (enumerator);
        }

        public void Add (Action action)
        {
            if (action == null) {
                throw new ArgumentNullException ();
            }

            registeredEnumerators.Enqueue (TaskHelper.ToEnumerator (action));
        }

        public void Reset ()
        {
            throw new NotSupportedException ();
        }

        abstract public IEnumerator GetEnumerator ();
    }
}

