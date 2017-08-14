using System.Collections;
using UAsync.Svelto.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UAsync
{
    
    class ParallelFunc
    {
        internal string name;
        internal Action<CallbackDelegate> action;
        internal Func<CallbackDelegate, IEnumerator> enumerator;

        private ParallelFunc ()
        {
        }

        public static ParallelFunc FromAction (string name, Action<CallbackDelegate> action)
        {
            if (name == null || action == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.name = name;
            f.action = action;
            f.enumerator = null;
            return f;
        }

        public static ParallelFunc FromEnumerator (string name, Func<CallbackDelegate, IEnumerator> enumerator)
        {
            if (name == null || enumerator == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.name = name;
            f.action = null;
            f.enumerator = enumerator;
            return f;
        }

        static ParallelFunc Create ()
        {
            var f = LeanClassPool <ParallelFunc>.Spawn ();

            if (f == null) {
                f = new ParallelFunc ();
            }

            return f;
        }
    }

    class SeriesFunc
    {
        internal string name;
        internal Action<CallbackDelegate, Dictionary<string, object>> action;
        internal Func<CallbackDelegate, Dictionary<string, object>, IEnumerator> enumerator;

        private SeriesFunc ()
        {
        }

        public static SeriesFunc FromAction (string name, Action<CallbackDelegate, Dictionary<string, object>> action)
        {
            if (name == null || action == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.name = name;
            f.action = action;
            f.enumerator = null;
            return f;
        }

        public static SeriesFunc FromEnumerator (string name, Func<CallbackDelegate, Dictionary<string, object>, IEnumerator> enumerator)
        {
            if (name == null || enumerator == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.name = name;
            f.action = null;
            f.enumerator = enumerator;
            return f;
        }

        static SeriesFunc Create ()
        {
            var f = LeanClassPool <SeriesFunc>.Spawn ();

            if (f == null) {
                f = new SeriesFunc ();
            }

            return f;
        }
    }

    class EachSeriesFunc<T>
    {
        internal Action<T, CallbackDelegate> action;
        internal Func<T, CallbackDelegate, IEnumerator> enumerator;

        private EachSeriesFunc ()
        {
        }

        public static EachSeriesFunc<T> FromAction (Action<T, CallbackDelegate> action)
        {
            if (action == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.action = action;
            f.enumerator = null;
            return f;
        }

        public static EachSeriesFunc<T> FromEnumerator (Func<T, CallbackDelegate, IEnumerator> enumerator)
        {
            if (enumerator == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.action = null;
            f.enumerator = enumerator;
            return f;
        }

        static EachSeriesFunc<T> Create ()
        {
            var f = LeanClassPool <EachSeriesFunc<T>>.Spawn ();

            if (f == null) {
                f = new EachSeriesFunc<T> ();
            }

            return f;
        }
    }

    class EachFinalFunc
    {
        internal Action<object> action;

        internal EachFinalFunc ()
        {
        }

        public static EachFinalFunc From (Action<object> action)
        {
            if (action == null) {
                throw new ArgumentException ();
            }

            var finalFunc = LeanClassPool <EachFinalFunc>.Spawn ();

            if (finalFunc == null) {
                finalFunc = new EachFinalFunc ();
            }

            finalFunc.action = action;
            return finalFunc;
        }
    }

    class UAsyncFinalFunc
    {
        internal Action<object, Dictionary<string, object>> action;

        internal UAsyncFinalFunc ()
        {
        }

        public static UAsyncFinalFunc From (Action<object, Dictionary<string, object>> action)
        {
            if (action == null) {
                throw new ArgumentException ();
            }

            var finalFunc = LeanClassPool <UAsyncFinalFunc>.Spawn ();

            if (finalFunc == null) {
                finalFunc = new UAsyncFinalFunc ();
            }

            finalFunc.action = action;
            return finalFunc;
        }
    }

    static class Async
    {
        /// <summary>
        /// Run a series of functions in parallel
        /// The functions are wrapped in ParallelFunc objects since C# does not
        /// support conversion from method group to object.
        /// The last parameter must be UAsyncFinalFunc
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static IAsyncTask Parallel (params object[] args)
        {
            return new AsyncParallel (args);
        }

        /// <summary>
        /// Run a series of functions in parallel
        /// The functions are wrapped in SeriesFunc objects since C# does not
        /// support conversion from method group to object
        /// The last parameter must be UAsyncFinalFunc
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static IAsyncTask Series (params object[] args)
        {
            return new AsyncSeries (args);
        }

        /// <summary>
        /// Run a series of functions in parallel
        /// The functions are wrapped in SeriesFunc objects since C# does not
        /// support conversion from method group to object
        /// The last parameter must be UAsyncFinalFunc
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static IAsyncTask EachSeries <T> (params object[] args)
        {
            return new AsyncEachSeries <T> (args);
        }
    }

    interface IAsyncTask
    {
        void Cancel ();
    }

    class AsyncParallel : IAsyncTask
    {
        bool isDone;
        int completeNum;
        int total;
        readonly UAsyncFinalFunc uAsyncFinalFunc;
        readonly Dictionary<string, object> result;
        readonly IList <TaskRoutine> taskRoutines;
        readonly IList <ParallelFunc> funcs;

        internal AsyncParallel (object[] args)
        {
            if (args.Length < 2) {
                throw new ArgumentException ();
            }

            for (var i = 0; i < args.Length - 1; i++) {
                if (args [i] == null || !(args [i] is ParallelFunc)) {
                    throw new ArgumentException ();
                }
            }

            if (args [args.Length - 1] == null || !(args [args.Length - 1] is UAsyncFinalFunc)) {
                throw new ArgumentException ();
            }

            isDone = false;
            completeNum = 0;
            total = args.Length - 1;
            uAsyncFinalFunc = args [args.Length - 1] as UAsyncFinalFunc;
            result = new Dictionary <string, object> ();
            taskRoutines = new List <TaskRoutine> ();
            funcs = new List <ParallelFunc> ();

            ParallelFunc func;

            for (var i = 0; i < args.Length - 1; i++) {
                func = args [i] as ParallelFunc;
                funcs.Add (func);
                var name = func.name;

                if (result.ContainsKey (name)) {
                    Cleanup ();
                    throw new ArgumentException ("Same name is already used: " + name);
                } else {
                    result [name] = null;

                    if (func.action != null) {
                        taskRoutines.Add (
                            TaskRunner.Instance.Run (
                                new SingleTask (() => func.action ((err, res) => Callback (name, err, res))),
                                TaskRunnerCallback)
                        );
                    } else {
                        taskRoutines.Add (
                            TaskRunner.Instance.Run (
                                func.enumerator ((err, res) => Callback (name, err, res)),
                                TaskRunnerCallback)
                        );
                    }
                }
            }


        }

        void Callback (string name, object err = null, object res = null)
        {
            if (isDone) {
                return;
            }

            if (err != null) {
                isDone = true;

                for (var i = 0; i < taskRoutines.Count; i++) {
                    taskRoutines [i].Cancel ();
                }

                Action<object, Dictionary<string, object>> action = uAsyncFinalFunc.action;
                Cleanup ();
                action (err, null);
                return;
            }

            result [name] = res;
            completeNum++;

            if (completeNum == total) {
                isDone = true;
                Action<object, Dictionary<string, object>> action = uAsyncFinalFunc.action;
                Cleanup ();
                action (err, null);
            }
        }

        /// <summary>
        /// When TaskRunner finishes, it will callback, we don't care about the result in this case
        /// </summary>
        /// <param name="err">Error.</param>
        /// <param name="res">Res.</param>
        void TaskRunnerCallback (object err = null, object res = null)
        {
            if (isDone) {
                return;
            }

            if (err != null) {
                isDone = true;

                for (var i = 0; i < taskRoutines.Count; i++) {
                    taskRoutines [i].Cancel ();
                }

                Action<object, Dictionary<string, object>> action = uAsyncFinalFunc.action;
                Cleanup ();
                action (err, null);
            } else {
                for (var i = taskRoutines.Count - 1; i >= 0; i--) {
                    if (!taskRoutines [i].IsBusy) {
                        taskRoutines.RemoveAt (i);
                    }
                }
            }
        }

        public void Cancel ()
        {
            if (isDone) {
                return;
            }

            isDone = true;

            for (int i = 0; i < taskRoutines.Count; i++) {
                taskRoutines [i].Cancel ();
            }

            Cleanup ();
        }

        void Cleanup ()
        {
            uAsyncFinalFunc.action = null;
            LeanClassPool <UAsyncFinalFunc>.Despawn (uAsyncFinalFunc);
            taskRoutines.Clear ();

            for (int i = 0; i < funcs.Count; i++) {
                funcs [i].name = null;
                funcs [i].action = null;
                funcs [i].enumerator = null;
                LeanClassPool <ParallelFunc>.Despawn (funcs [i]);
            }

            funcs.Clear ();
        }
    }

    class AsyncSeries : IAsyncTask
    {
        bool isDone;
        int completeNum;
        int total;
        readonly UAsyncFinalFunc uAsyncFinalFunc;
        readonly Dictionary<string, object> result;
        TaskRoutine taskRoutine;
        readonly IList <SeriesFunc> funcs;

        internal AsyncSeries (object[] args)
        {
            if (args.Length < 2) {
                throw new ArgumentException ();
            }

            for (var i = 0; i < args.Length - 1; i++) {
                if (args [i] == null || !(args [i] is SeriesFunc)) {
                    throw new ArgumentException ();
                }
            }

            if (args [args.Length - 1] == null || !(args [args.Length - 1] is UAsyncFinalFunc)) {
                throw new ArgumentException ();
            }

            isDone = false;
            completeNum = 0;
            total = args.Length - 1;
            uAsyncFinalFunc = args [args.Length - 1] as UAsyncFinalFunc;
            result = new Dictionary <string, object> ();
            funcs = new List <SeriesFunc> ();

            SeriesFunc func;

            for (var i = 0; i < args.Length - 1; i++) {
                func = args [i] as SeriesFunc;
                funcs.Add (func);
                var name = func.name;

                if (result.ContainsKey (name)) {
                    Cleanup ();
                    throw new ArgumentException ("Same name is already used: " + name);
                } else {
                    result [name] = null;
                }
            }

            RunOneFunc (args [0] as SeriesFunc);
        }

        void RunOneFunc (SeriesFunc func)
        {
            var name = func.name;

            if (func.action != null) {
                taskRoutine = 
                    TaskRunner.Instance.Run (
                    new SingleTask (
                        () => func.action ((err, res) => Callback (name, err, res), result)),
                    TaskRunnerCallback
                );
            } else {
                taskRoutine = 
                    TaskRunner.Instance.Run (
                    func.enumerator ((err, res) => Callback (name, err, res), result),
                    TaskRunnerCallback);
            }
        }

        void Callback (string name, object err = null, object res = null)
        {
            if (isDone) {
                return;
            }

            if (err != null) {
                isDone = true;
                taskRoutine.Cancel ();
                Action<object, Dictionary<string, object>> action = uAsyncFinalFunc.action;
                Cleanup ();
                action (err, null);
                return;
            }

            result [name] = res;
            completeNum++;

            if (completeNum == total) {
                isDone = true;
                Action<object, Dictionary<string, object>> action = uAsyncFinalFunc.action;
                Cleanup ();
                action (err, null);
            } else {
                RunOneFunc (funcs [completeNum] as SeriesFunc);
            }
        }

        /// <summary>
        /// When TaskRunner finishes, it will callback, we don't care about the result in this case
        /// </summary>
        /// <param name="err">Error.</param>
        /// <param name="res">Res.</param>
        void TaskRunnerCallback (object err = null, object res = null)
        {
            if (isDone) {
                return;
            }

            if (err != null) {
                isDone = true;
                Action<object, Dictionary<string, object>> action = uAsyncFinalFunc.action;
                Cleanup ();
                action (err, null);
            }
        }

        public void Cancel ()
        {
            if (isDone) {
                return;
            }

            isDone = true;
            taskRoutine.Cancel ();

            Cleanup ();
        }

        void Cleanup ()
        {
            uAsyncFinalFunc.action = null;
            LeanClassPool <UAsyncFinalFunc>.Despawn (uAsyncFinalFunc);
            taskRoutine = null;

            for (int i = 0; i < funcs.Count; i++) {
                funcs [i].name = null;
                funcs [i].action = null;
                funcs [i].enumerator = null;
                LeanClassPool <SeriesFunc>.Despawn (funcs [i]);
            }

            funcs.Clear ();
        }
    }

    class AsyncEachSeries<T> : IAsyncTask
    {
        bool isDone;
        int completeNum;
        int total;
        List<T> items;
        readonly EachFinalFunc finalFunc;
        TaskRoutine taskRoutine;
        EachSeriesFunc<T> func;

        internal AsyncEachSeries (object[] args)
        {
            if (args.Length != 3) {
                throw new ArgumentException ();
            }

            if (!(args [0] is IEnumerable <T>)) {
                throw new ArgumentException ();
            }

            if (args [1] == null || !(args [1] is EachSeriesFunc <T>)) {
                throw new ArgumentException ();
            }

            if (args [args.Length - 1] == null || !(args [args.Length - 1] is EachFinalFunc)) {
                throw new ArgumentException ();
            }

            isDone = false;
            completeNum = 0;
            var temp = (IEnumerable<T>)args [0];
            items = temp.ToList ();
            total = items.Count;
            finalFunc = args [args.Length - 1] as EachFinalFunc;

            if (total == 0) {
                finalFunc.action (null);
                return;
            }

            func = args [1] as EachSeriesFunc <T>;
            RunOneFunc ();
        }

        void RunOneFunc ()
        {
            if (func.action != null) {
                taskRoutine = 
                    TaskRunner.Instance.Run (
                    new SingleTask (
                        () => func.action (items [completeNum], (err, res) => Callback (err))
                    ),
                    TaskRunnerCallback
                );
            } else {
                taskRoutine = TaskRunner.Instance.Run (
                    func.enumerator (
                        items [completeNum],
                        (err, res) => Callback (err)),
                    TaskRunnerCallback
                );
            }
        }

        void Callback (object err)
        {
            if (isDone) {
                return;
            }

            if (err != null) {
                isDone = true;
                taskRoutine.Cancel ();
                Action<object> action = finalFunc.action;
                Cleanup ();
                action (err);
                return;
            }

            completeNum++;

            if (completeNum == total) {
                isDone = true;
                Action<object> action = finalFunc.action;
                Cleanup ();
                action (err);
            } else {
                RunOneFunc ();
            }
        }

        /// <summary>
        /// When TaskRunner finishes, it will callback, we don't care about the result in this case
        /// </summary>
        /// <param name="err">Error.</param>
        /// <param name="res">Res.</param>
        void TaskRunnerCallback (object err = null, object res = null)
        {
            if (isDone) {
                return;
            }

            if (err != null) {
                isDone = true;
                Action<object> action = finalFunc.action;
                Cleanup ();
                action (err);
            }
        }

        public void Cancel ()
        {
            if (isDone) {
                return;
            }

            isDone = true;
            taskRoutine.Cancel ();

            Cleanup ();
        }

        void Cleanup ()
        {
            finalFunc.action = null;
            LeanClassPool <EachFinalFunc>.Despawn (finalFunc);
            taskRoutine = null;

            LeanClassPool <EachSeriesFunc<T>>.Despawn (func);
        }
    }

    internal static class LeanClassPool<T>
        where T : class
    {
        private static readonly List<T> cache = new List<T> ();

        public static T Spawn ()
        {
            return Spawn (null, null);
        }

        public static T Spawn (System.Action<T> onSpawn)
        {
            return Spawn (null, onSpawn);
        }

        public static T Spawn (System.Predicate<T> match)
        {
            return Spawn (match, null);
        }

        // This will either return a pooled class instance, or null
        // You can also specify a match for the exact class instance you're looking for
        // You can also specify an action to run on the class instance (e.g. if you need to reset it)
        // NOTE: Because it can return null, you should use it like this: Lean.LeanClassPool<Whatever>.Spawn(...) ?? new Whatever(...)
        public static T Spawn (System.Predicate<T> match, System.Action<T> onSpawn)
        {
            // Get the matched index, or the last index
            var index = match != null ? cache.FindIndex (match) : cache.Count - 1;

            // Was one found?
            if (index >= 0) {
                // Get instance and remove it from cache
                var instance = cache [index];

                cache.RemoveAt (index);

                // Run action?
                if (onSpawn != null) {
                    onSpawn (instance);
                }

                return instance;
            }

            // Return null?
            return null;
        }

        public static void Despawn (T instance)
        {
            Despawn (instance, null);
        }

        // This allows you to desapwn a class instance
        // You can also specify an action to run on the class instance (e.g. if you need to reset it)
        public static void Despawn (T instance, System.Action<T> onDespawn)
        {
            // Does it exist?
            if (instance != null) {
                // Run action on it?
                if (onDespawn != null) {
                    onDespawn (instance);
                }

                // Add to cache
                cache.Add (instance);
            }
        }

        public static void Clear ()
        {
            cache.Clear ();
        }
    }

}