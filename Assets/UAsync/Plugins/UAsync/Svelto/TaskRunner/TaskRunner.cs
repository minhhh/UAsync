using System.Collections;
using UAsync.Svelto.Tasks;
using UAsync.Svelto.Tasks.Internal;
using System;

namespace UAsync
{
    public class TaskRunner
    {
        static TaskRunner _instance;
        static readonly object _locker = new object ();

        static public TaskRunner Instance
        {
            get {
                lock (_locker) {
                    if (_instance == null) {
                        InitInstance ();
                    }
                }

                return _instance;
            }
        }

        public TaskRoutine Run (IEnumerator task, CallbackDelegate onComplete = null)
        {
            if (task == null) {
                return null;
            }

            return _taskRoutinePool.Start (task, onComplete);
        }

        public void CancelAllTasks ()
        {
            if (_runner != null) {
                _runner.StopAllCoroutines ();
            }
        }

        static void InitInstance ()
        {
            _instance = new TaskRunner ();
            #if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_IPHONE || UNITY_ANDROID || UNITY_EDITOR
            _instance._runner = new MonoRunner ();
            #else
            //        _instance._runner = new MultiThreadRunner();
            _instance._runner = new MonoRunner ();
            #endif
            _instance._taskRoutinePool = new TaskRoutinePool (_instance._runner);
        }

        TaskRoutinePool _taskRoutinePool;
        IRunner _runner;
    }
}
