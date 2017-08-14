using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UAsync.Svelto.Tasks;
using UAsync;

public class UAsyncTests : MonoBehaviour
{
    void Start ()
    {
//        TestParallel ();
//        TestSeries ();
        TestEachSeries ();
    }

    void TestEachSeries ()
    {
        var a = new int [] { 1, 2, 3 };
        var series = 
            UAsync.Async.EachSeries<int> (
                a,
//                EachSeriesFunc<int>.FromAction (PrintInt),
                EachSeriesFunc<int>.FromEnumerator (PrintIntCo),
                EachFinalFunc.From ((object err) => {
                    Debug.Log ("Finish " + err);
                })
            );
    }

    IEnumerator PrintIntCo (int i, CallbackDelegate cb)
    {
        yield return new WaitForSeconds (1);
        Debug.Log (i);
        cb ();
    }

    void PrintInt (int i, CallbackDelegate cb)
    {
        Debug.Log (i);
        cb ();
    }

    void TestSeries ()
    {
        var series = 
            UAsync.Async.Series (
                SeriesFunc.FromAction ("one", SeriesFunc1),
                SeriesFunc.FromEnumerator ("two", SeriesFunc2),
                SeriesFunc.FromAction ("three", SeriesFunc3),
                UAsyncFinalFunc.From ((object err, Dictionary<string, object> res) => {
                    Debug.Log ("Finish " + err);
                    if (err == null) {
                        Debug.Log ("res " + res ["one"] + " " + res ["two"] + " " + res ["three"]);
                    }
                })
            );
//        series.Cancel ();
    }

    void SeriesFunc1 (CallbackDelegate cb, Dictionary<string, object> res)
    {
        Debug.Log ("SeriesFunc1");

        //        throw new UnityException ("Exception from Func1");
        cb (null, 100);
    }

    IEnumerator SeriesFunc2 (CallbackDelegate cb, Dictionary<string, object> res)
    {
        Debug.Log ("SeriesFunc2 " + res ["one"] + " " + res ["two"] + " " + res ["three"]);

        yield return new WaitForSeconds (1);
        throw new UnityException ("Exception from Func2");
        Debug.Log ("SeriesFunc2 End");
        cb (null, 200);
    }

    void SeriesFunc3 (CallbackDelegate cb, Dictionary<string, object> res)
    {
        Debug.Log ("SeriesFunc3 " + res ["one"] + " " + res ["two"] + " " + res ["three"]);
        cb (null, 100);
    }

    void TestParallel ()
    {
        var parallel = 
            UAsync.Async.Parallel (
                ParallelFunc.FromAction ("one", Func1),
                ParallelFunc.FromEnumerator ("two", Func2),
                ParallelFunc.FromAction ("three", Func3),
                UAsyncFinalFunc.From ((object err, Dictionary<string, object> res) => {
                    Debug.Log ("Finish " + err);
                    if (err == null) {
                        Debug.Log ("res " + res ["one"] + " " + res ["two"] + " " + res ["three"]);
                    }
                })
            );
//        parallel.Cancel ();
    }

    void Func1 (CallbackDelegate cb)
    {
        Debug.Log ("Func1");

//        throw new UnityException ("Exception from Func1");
        cb (null, 100);
    }

    IEnumerator Func2 (CallbackDelegate cb)
    {
        Debug.Log ("Func2");

        yield return new WaitForSeconds (1);
//        throw new UnityException ("Exception from Func2");
        Debug.Log ("Func2 End");
        cb (null, 200);
    }

    void Func3 (CallbackDelegate cb)
    {
        Debug.Log ("Func3");

        cb (null, 300);
    }

}
