using UAsync.Svelto.Tasks.Internal;
using System;
using System.Collections;
using UnityEngine;

namespace UAsync.Svelto.Tasks
{
    public class TaskRoutine
    {
        TaskRoutinePool taskRoutinePool;
        CallbackDelegate onComplete;
        WorkerTask task;
        Func<IEnumerator> _taskGenerator;

        internal TaskRoutine (TaskRoutinePool taskRoutinePool, WorkerTask task)
        {
            this.taskRoutinePool = taskRoutinePool;
            this.task = task;
        }

        internal bool Start (IEnumerator taskGenerator, CallbackDelegate onComplete)
        {
            if (IsBusy) {
                return false;
            }

            this.onComplete = onComplete;
            task.Start (taskGenerator, OnWorkerCompleted);

            return true;
        }

        internal void OnWorkerCompleted (object err, object res)
        {
            taskRoutinePool.OnRoutineCompleted (this, this.onComplete, err, res);
        }

        public void Cancel ()
        {
            if (!IsBusy) {
                return;
            }

            task.Cancel ();
            this.onComplete = null;
            taskRoutinePool.OnRoutineCanceled (this);
        }

        public bool IsBusy
        {
            get {
                return task.IsBusy;
            }
        }

    }
}

