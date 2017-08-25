using System.Collections;
using UAsync.Svelto.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Lean;

namespace UAsync
{
    
    class ChildFunc
    {
        internal string name;
        internal Action<CallbackDelegate> action1;
        internal Action<CallbackDelegate, Dictionary<string, object>> action2;
        internal Func<CallbackDelegate, IEnumerator> enumerator1;
        internal Func<CallbackDelegate, Dictionary<string, object>, IEnumerator> enumerator2;

        private ChildFunc ()
        {
        }

        public static ChildFunc FromAction (string name, Action<CallbackDelegate> action)
        {
            if (name == null || action == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.name = name;
            f.action1 = action;
            f.enumerator1 = null;
            return f;
        }

        public static ChildFunc FromEnumerator (string name, Func<CallbackDelegate, IEnumerator> enumerator)
        {
            if (name == null || enumerator == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.name = name;
            f.action1 = null;
            f.enumerator1 = enumerator;
            return f;
        }

        public static ChildFunc FromAction (string name, Action<CallbackDelegate, Dictionary<string, object>> action)
        {
            if (name == null || action == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.name = name;
            f.action2 = action;
            f.enumerator1 = null;
            return f;
        }

        public static ChildFunc FromEnumerator (string name, Func<CallbackDelegate, Dictionary<string, object>, IEnumerator> enumerator)
        {
            if (name == null || enumerator == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.name = name;
            f.action1 = null;
            f.enumerator2 = enumerator;
            return f;
        }

        static ChildFunc Create ()
        {
            var f = LeanClassPool <ChildFunc>.Spawn ();

            if (f == null) {
                f = new ChildFunc ();
            }

            return f;
        }

        public void OnDespawn ()
        {
            name = null;
            action1 = null;
            action2 = null;
            enumerator1 = null;
            enumerator2 = null;
        }
    }

    class ChildFunc<T>
    {
        internal Action<T, CallbackDelegate> action1;
        internal Func<T, CallbackDelegate, IEnumerator> enumerator1;

        private ChildFunc ()
        {
        }

        public static ChildFunc<T> FromAction (Action<T, CallbackDelegate> action)
        {
            if (action == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.action1 = action;
            f.enumerator1 = null;
            return f;
        }

        public static ChildFunc<T> FromEnumerator (Func<T, CallbackDelegate, IEnumerator> enumerator)
        {
            if (enumerator == null) {
                throw new ArgumentException ();
            }

            var f = Create ();
            f.action1 = null;
            f.enumerator1 = enumerator;
            return f;
        }

        static ChildFunc<T> Create ()
        {
            var f = LeanClassPool <ChildFunc<T>>.Spawn ();

            if (f == null) {
                f = new ChildFunc<T> ();
            }

            return f;
        }

        public void OnDespawn ()
        {
            action1 = null;
            enumerator1 = null;
        }
    }

    class UAsyncFinalFunc
    {
        internal Action<object, Dictionary<string, object>> action1;
        internal Action<object> action2;

        internal UAsyncFinalFunc ()
        {
        }

        public static UAsyncFinalFunc Create (Action<object, Dictionary<string, object>> action)
        {
            if (action == null) {
                throw new ArgumentException ();
            }

            var finalFunc = LeanClassPool <UAsyncFinalFunc>.Spawn ();

            if (finalFunc == null) {
                finalFunc = new UAsyncFinalFunc ();
            }

            finalFunc.action1 = action;
            return finalFunc;
        }

        public static UAsyncFinalFunc Create (Action<object> action)
        {
            if (action == null) {
                throw new ArgumentException ();
            }

            var finalFunc = LeanClassPool <UAsyncFinalFunc>.Spawn ();

            if (finalFunc == null) {
                finalFunc = new UAsyncFinalFunc ();
            }

            finalFunc.action2 = action;
            return finalFunc;
        }

        public void OnDespawn ()
        {
            action1 = null;
            action2 = null;
        }
    }

    static class Async
    {
        /// <summary>
        /// Run a series of functions in parallel
        /// The functions are wrapped in ChildFunc objects since C# does not
        /// support conversion from method group to object.
        /// The last parameter must be UAsyncFinalFunc
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static IAsyncTask Parallel (params object[] args)
        {
            return new AsyncParallel (args);
        }

        /// <summary>
        /// Run a series of functions in sequence
        /// The functions are wrapped in ChildFunc objects since C# does not
        /// support conversion from method group to object
        /// The last parameter must be UAsyncFinalFunc
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static IAsyncTask Series (params object[] args)
        {
            return new AsyncSeries (args);
        }

        /// <summary>
        /// Applies a function to each item in coll, in parallel
        /// The function is wrapped in ChildFunc objects since C# does not
        /// support conversion from method group to object
        /// The last parameter must be UAsyncFinalFunc
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static IAsyncTask Each <T> (params object[] args)
        {
            return new AsyncEach <T> (args);
        }

        /// <summary>
        /// Applies a function to each item in coll, in sequence
        /// The functions are wrapped in ChildFunc objects since C# does not
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
        readonly UAsyncFinalFunc finalFunc;
        readonly Dictionary<string, object> result;
        readonly IList <TaskRoutine> taskRoutines;
        readonly IList <ChildFunc> funcs;

        internal AsyncParallel (object[] args)
        {
            if (args.Length < 2) {
                throw new ArgumentException ();
            }

            for (var i = 0; i < args.Length - 1; i++) {
                if (args [i] == null || !(args [i] is ChildFunc)) {
                    throw new ArgumentException ();
                }
            }

            if (args [args.Length - 1] == null || !(args [args.Length - 1] is UAsyncFinalFunc)) {
                throw new ArgumentException ();
            }

            isDone = false;
            completeNum = 0;
            total = args.Length - 1;
            finalFunc = args [args.Length - 1] as UAsyncFinalFunc;
            result = new Dictionary <string, object> ();
            taskRoutines = new List <TaskRoutine> ();
            funcs = new List <ChildFunc> ();

            ChildFunc func;

            for (var i = 0; i < args.Length - 1; i++) {
                func = args [i] as ChildFunc;
                funcs.Add (func);
                var name = func.name;

                if (result.ContainsKey (name)) {
                    Cleanup ();
                    throw new ArgumentException ("Same name is already used: " + name);
                } else {
                    result [name] = null;

                    if (func.action1 != null) {
                        taskRoutines.Add (
                            TaskRunner.Instance.Run (
                                new SingleTask (() => func.action1 ((err, res) => Callback (name, err, res))),
                                TaskRunnerCallback)
                        );
                    } else {
                        taskRoutines.Add (
                            TaskRunner.Instance.Run (
                                func.enumerator1 ((err, res) => Callback (name, err, res)),
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

                Action<object, Dictionary<string, object>> action = finalFunc.action1;
                Cleanup ();
                action (err, null);
                return;
            }

            result [name] = res;
            completeNum++;

            if (completeNum == total) {
                isDone = true;
                Action<object, Dictionary<string, object>> action = finalFunc.action1;
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

                Action<object, Dictionary<string, object>> action = finalFunc.action1;
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
            finalFunc.action1 = null;
            LeanClassPool <UAsyncFinalFunc>.Despawn (finalFunc);
            taskRoutines.Clear ();

            for (int i = 0; i < funcs.Count; i++) {
                funcs [i].OnDespawn ();
                LeanClassPool <ChildFunc>.Despawn (funcs [i]);
            }

            funcs.Clear ();
        }
    }

    class AsyncSeries : IAsyncTask
    {
        bool isDone;
        int completeNum;
        int total;
        readonly UAsyncFinalFunc finalFunc;
        readonly Dictionary<string, object> result;
        TaskRoutine taskRoutine;
        readonly IList <ChildFunc> funcs;

        internal AsyncSeries (object[] args)
        {
            if (args.Length < 2) {
                throw new ArgumentException ();
            }

            for (var i = 0; i < args.Length - 1; i++) {
                if (args [i] == null || !(args [i] is ChildFunc)) {
                    throw new ArgumentException ();
                }
            }

            if (args [args.Length - 1] == null || !(args [args.Length - 1] is UAsyncFinalFunc)) {
                throw new ArgumentException ();
            }

            isDone = false;
            completeNum = 0;
            total = args.Length - 1;
            finalFunc = args [args.Length - 1] as UAsyncFinalFunc;
            result = new Dictionary <string, object> ();
            funcs = new List <ChildFunc> ();

            ChildFunc func;

            for (var i = 0; i < args.Length - 1; i++) {
                func = args [i] as ChildFunc;
                funcs.Add (func);
                var name = func.name;

                if (result.ContainsKey (name)) {TaskRoutine taskRoutine;
                    Cleanup ();
                    throw new ArgumentException ("Same name is already used: " + name);
                } else {
                    result [name] = null;
                }
            }

            RunOneFunc (args [0] as ChildFunc);
        }

        void RunOneFunc (ChildFunc func)
        {
            var name = func.name;

            if (func.action2 != null) {
                taskRoutine = 
                    TaskRunner.Instance.Run (
                    new SingleTask (
                        () => func.action2 ((err, res) => Callback (name, err, res), result)),
                    TaskRunnerCallback
                );
            } else {
                taskRoutine = 
                    TaskRunner.Instance.Run (
                    func.enumerator2 ((err, res) => Callback (name, err, res), result),
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
                Action<object, Dictionary<string, object>> action = finalFunc.action1;
                Cleanup ();
                action (err, null);
                return;
            }

            result [name] = res;
            completeNum++;

            if (completeNum == total) {
                isDone = true;
                Action<object, Dictionary<string, object>> action = finalFunc.action1;
                Cleanup ();
                action (err, null);
            } else {
                RunOneFunc (funcs [completeNum] as ChildFunc);
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
                Action<object, Dictionary<string, object>> action = finalFunc.action1;
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
            finalFunc.action1 = null;
            LeanClassPool <UAsyncFinalFunc>.Despawn (finalFunc);
            taskRoutine = null;

            for (int i = 0; i < funcs.Count; i++) {
                funcs [i].OnDespawn ();
                LeanClassPool <ChildFunc>.Despawn (funcs [i]);
            }

            funcs.Clear ();
        }
    }

    class AsyncEach<T> : IAsyncTask
    {
        bool isDone;
        int completeNum;
        int total;
        List<T> items;
        readonly UAsyncFinalFunc finalFunc;
        readonly IList <TaskRoutine> taskRoutines;
        ChildFunc<T> func;

        internal AsyncEach (object[] args)
        {
            if (args.Length != 3) {
                throw new ArgumentException ();
            }

            if (!(args [0] is IEnumerable <T>)) {
                throw new ArgumentException ();
            }

            if (args [1] == null || !(args [1] is ChildFunc <T>)) {
                throw new ArgumentException ();
            }

            if (args [args.Length - 1] == null || !(args [args.Length - 1] is UAsyncFinalFunc)) {
                throw new ArgumentException ();
            }

            isDone = false;
            completeNum = 0;
            var temp = (IEnumerable<T>)args [0];
            items = temp.ToList ();
            total = items.Count;
            finalFunc = args [args.Length - 1] as UAsyncFinalFunc;

            if (total == 0) {
                finalFunc.action2 (null);
                return;
            }

            taskRoutines = new List <TaskRoutine> ();
            func = args [1] as ChildFunc <T>;

            for (var i = 0; i < total; i++) {
                var item = items [i];

                if (func.action1 != null) {
                    taskRoutines.Add (
                        TaskRunner.Instance.Run (
                            new SingleTask (() => func.action1 (item, Callback)),
                            TaskRunnerCallback)
                    );
                } else {
                    taskRoutines.Add (
                        TaskRunner.Instance.Run (
                            func.enumerator1 (item, Callback),
                            TaskRunnerCallback)
                    );
                }
            }
        }

        void Callback (object err = null, object res = null)
        {
            if (isDone) {
                return;
            }

            if (err != null) {
                isDone = true;

                for (var i = 0; i < taskRoutines.Count; i++) {
                    taskRoutines [i].Cancel ();
                }

                Action<object> action = finalFunc.action2;
                Cleanup ();
                action (err);
                return;
            }

            completeNum++;

            if (completeNum == total) {
                isDone = true;
                Action<object> action = finalFunc.action2;
                Cleanup ();
                action (err);
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

                Action<object> action = finalFunc.action2;
                Cleanup ();
                action (err);
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
            finalFunc.OnDespawn ();
            LeanClassPool <UAsyncFinalFunc>.Despawn (finalFunc);
            taskRoutines.Clear ();

            func.OnDespawn ();
            LeanClassPool <ChildFunc<T>>.Despawn (func);
        }
    }

    class AsyncEachSeries<T> : IAsyncTask
    {
        bool isDone;
        int completeNum;
        int total;
        List<T> items;
        readonly UAsyncFinalFunc finalFunc;
        TaskRoutine taskRoutine;
        ChildFunc<T> func;

        internal AsyncEachSeries (object[] args)
        {
            if (args.Length != 3) {
                throw new ArgumentException ();
            }

            if (!(args [0] is IEnumerable <T>)) {
                throw new ArgumentException ();
            }

            if (args [1] == null || !(args [1] is ChildFunc <T>)) {
                throw new ArgumentException ();
            }

            if (args [args.Length - 1] == null || !(args [args.Length - 1] is UAsyncFinalFunc)) {
                throw new ArgumentException ();
            }

            isDone = false;
            completeNum = 0;
            var temp = (IEnumerable<T>)args [0];
            items = temp.ToList ();
            total = items.Count;
            finalFunc = args [args.Length - 1] as UAsyncFinalFunc;

            if (total == 0) {
                finalFunc.action2 (null);
                return;
            }

            func = args [1] as ChildFunc <T>;
            RunOneFunc ();
        }

        void RunOneFunc ()
        {
            if (func.action1 != null) {
                taskRoutine = 
                    TaskRunner.Instance.Run (
                    new SingleTask (
                        () => func.action1 (items [completeNum], (err, res) => Callback (err))
                    ),
                    TaskRunnerCallback
                );
            } else {
                taskRoutine = TaskRunner.Instance.Run (
                    func.enumerator1 (
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
                Action<object> action = finalFunc.action2;
                Cleanup ();
                action (err);
                return;
            }

            completeNum++;

            if (completeNum == total) {
                isDone = true;
                Action<object> action = finalFunc.action2;
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
                Action<object> action = finalFunc.action2;
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
            finalFunc.OnDespawn ();
            LeanClassPool <UAsyncFinalFunc>.Despawn (finalFunc);
            taskRoutine = null;

            func.OnDespawn ();
            LeanClassPool <ChildFunc<T>>.Despawn (func);
        }
    }
}