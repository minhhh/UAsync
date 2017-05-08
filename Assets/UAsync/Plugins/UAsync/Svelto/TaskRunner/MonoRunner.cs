#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_IPHONE || UNITY_ANDROID || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace UAsync.Svelto.Tasks.Internal
{
    internal class MonoRunner: IRunner
    {
        RunnerBehaviour _component = null;

        public bool stopped { private set; get; }

        public MonoRunner ()
        {
            GameObject go = new GameObject ("TaskRunner");
            UnityEngine.Object.DontDestroyOnLoad (go);

            go.hideFlags = HideFlags.HideInHierarchy;

            if ((_component = go.GetComponent<RunnerBehaviour> ()) == null) {
                _component = go.AddComponent<RunnerBehaviour> ();
            }

            _component.gameObject.SetActive (true);
            _component.enabled = true;

            stopped = false;
        }

        public void StopAllCoroutines ()
        {
            stopped = true;
            _component.StopAllCoroutines ();
        }

        public void StartCoroutine (IEnumerator task, CallbackDelegate onComplete)
        {
            if (RunnerBehaviour.isQuitting == true) {
                if (onComplete != null) {
                    onComplete (new CoroutineException ("Application is quitting!"), null);
                }

                return;
            }

            stopped = false;

            _component.StartCoroutine (StartCoroutineInternal (task, onComplete));

        }

        IEnumerator StartCoroutineInternal (IEnumerator coroutine, CallbackDelegate onComplete)
        {
            while (true) {
                try {
                    if (!coroutine.MoveNext ()) {
                        var res = coroutine.Current;
                        onComplete (null, (res is IEnumerator) ? null : res);
                        yield break;
                    }
                } catch (Exception e) {
                    onComplete (e); // Make sure this won't throw
                    yield break;
                }

                if (coroutine.Current is IEnumerator) {
                    yield return StartNestedCoroutineInternal (coroutine.Current as IEnumerator, onComplete);
                } else if (coroutine.Current is IEnumerable) {
                    yield return StartNestedCoroutineInternal ((coroutine.Current as IEnumerable).GetEnumerator (), onComplete);
                } else {
                    yield return coroutine.Current;
                }
            }
        }

        /**
         * We use this so that we can catch exception from nested yield instruction
         **/
        IEnumerator StartNestedCoroutineInternal (IEnumerator coroutine, CallbackDelegate onComplete)
        {
            Stack<IEnumerator> stack = new Stack<IEnumerator> ();
            IEnumerator ce;
            bool yieldCurrent;
            stack.Push (coroutine);

            while (stack.Count > 0) {
                ce = stack.Peek ();
                yieldCurrent = false;

                try {
                    if (!ce.MoveNext ()) {
                        stack.Pop ();
                        yieldCurrent = true;
                    } else {
                        if (ce.Current is IEnumerator) {
                            stack.Push (ce.Current as IEnumerator);
                        } else if (ce.Current is IEnumerable) {
                            stack.Push ((ce.Current as IEnumerable).GetEnumerator ());
                        } else {
                            yieldCurrent = true;
                        }
                    }
                } catch (Exception e) {
                    onComplete (e); // Make sure this won't throw
                    yield break;
                }

                if (yieldCurrent) {
                    yield return ce.Current;
                }
            }
        }
    }
}
#endif
