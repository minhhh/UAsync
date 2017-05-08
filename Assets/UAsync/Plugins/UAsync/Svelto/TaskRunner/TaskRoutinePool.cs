using System.Collections;
using UAsync.Svelto.DataStructures;
using System;
using UnityEngine;

namespace UAsync.Svelto.Tasks.Internal
{
    internal class TaskRoutinePool
    {
        internal TaskRoutinePool (IRunner runner)
        {
            _runner = runner;
        }

        internal TaskRoutine RetrieveTask ()
        {
            TaskRoutine task = null;

            if (_pool.Count > 0) {
                task = _pool.Dequeue ();
            }

            if (task != null && !task.IsBusy) {
                return task;
            }

            return CreateEmptyTask ();
        }

        internal TaskRoutine Start (IEnumerator enumerator, CallbackDelegate onComplete)
        {
            TaskRoutine task = RetrieveTask ();
            task.Start (enumerator, onComplete);
            return task;
        }

        public void OnRoutineCompleted (TaskRoutine taskRoutine, CallbackDelegate onComplete, object err, object res)
        {
            try {
                if (onComplete != null) {
                    onComplete (err, res);
                } else if (err != null) {
                    Debug.LogWarning (err);
                }
            } catch (Exception e) {
                Debug.LogException (e); // TODO: Add global exception handler
            }

            ReturnTaskToPool (taskRoutine);
        }

        public void OnRoutineCanceled (TaskRoutine taskRoutine)
        {
            ReturnTaskToPool (taskRoutine);
        }

        TaskRoutine CreateEmptyTask ()
        {
            WorkerTask ptask = new WorkerTask (_runner);
            return new TaskRoutine (this, ptask);
        }

        internal bool ReturnTaskToPool (TaskRoutine taskRoutine)
        {
            if (!taskRoutine.IsBusy) {
                _pool.Enqueue (taskRoutine);
                return true;
            }

            return false;
        }

        readonly ThreadSafeQueue<TaskRoutine> _pool = new ThreadSafeQueue<TaskRoutine> ();
        IRunner _runner;
    }
}
