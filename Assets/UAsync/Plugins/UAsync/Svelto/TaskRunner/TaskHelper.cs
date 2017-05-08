#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_IPHONE || UNITY_ANDROID || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System;

namespace UAsync
{
    public static class TaskHelper
    {
        public static IEnumerator ToEnumerator (YieldInstruction instruction)
        {
            yield return instruction;
        }

        public static IEnumerator ToEnumerator (Action action)
        {
            action ();
            yield return null;
        }
    }
}
#endif
