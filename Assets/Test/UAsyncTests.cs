﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UAsync.Svelto.Tasks;
using UAsync;
using System;

public class UAsyncTests : MonoBehaviour
{
    float time = 0;

    void Update ()
    {
        time += Time.deltaTime;
        if (time > 1) {
            time = 0;
//            TestEachSeriesGC ();
        }
    }

    void Start ()
    {
//        TestParallel ();
        TestSeries ();
//        TestEach ();
//        TestEachSeries ();
    }

    void TestEachSeriesGC ()
    {
        var a = new int [] { 1, 2, 3 };
        var series = 
            UAsync.Async.EachSeries<int> (
                a,
                                ChildFunc<int>.Create (PrintInt),
//                ChildFunc<int>.Create (PrintIntCo),
                UAsyncFinalFunc.Create ((object err) => {
//                    Debug.Log ("Finish " + err);
                })
            );
    }

    void TestEachGC ()
    {
        var a = new int [] { 1, 2, 3 };

        var series = 
            UAsync.Async.Each<int> (
                a,
                                ChildFunc<int>.Create (PrintInt),
//                ChildFunc<int>.Create (PrintIntCo),
                UAsyncFinalFunc.Create ((object err) => {
//                    Debug.Log ("Finish " + err);
                })
            );
    }

    void TestParallelGC ()
    {
        var parallel = 
            UAsync.Async.Parallel (
                ChildFunc.Create ("one", Func1),
//                ChildFunc.Create ("two", Func2),
                ChildFunc.Create ("three", Func3),
                UAsyncFinalFunc.Create ((object err, Dictionary<string, object> res) => {
//                    Debug.Log ("Finish " + err);
                    if (err == null) {
//                        Debug.Log ("res " + res ["one"] + " " + res ["two"] + " " + res ["three"]);
                    }
                })
            );
        //        parallel.Cancel ();
    }

    void TestSeriesGC ()
    {
        var series = 
            UAsync.Async.Series (
//                ChildFunc.Create ("one", SeriesFunc1),
//                ChildFunc.Create ("two", SeriesFunc2),
                ChildFunc.Create ("three", SeriesFunc3),
                UAsyncFinalFunc.Create ((object err, Dictionary<string, object> res) => {
                    // Debug.Log ("Finish " + err);
                    if (err == null) {
                    }
                })
            );
    }

    void TestEach ()
    {
        var a = new int [] { 1, 2, 3 };

        var series = 
            UAsync.Async.Each<int> (
                a,
                ChildFunc<int>.Create (PrintInt),
//                ChildFunc<int>.Create (PrintIntCo),
                UAsyncFinalFunc.Create ((object err) => {
//                    Debug.Log ("Finish " + err);
                })
            );
    }

    void TestEachSeries ()
    {
        var a = new int [] { 1, 2, 3 };
        var series = 
            UAsync.Async.EachSeries<int> (
                a,
                ChildFunc<int>.Create (PrintInt),
//                ChildFunc<int>.Create (PrintIntCo),
                UAsyncFinalFunc.Create ((object err) => {
                    Debug.Log ("Finish " + err);
                })
            );
    }

    IEnumerator PrintIntCo (int i, CallbackDelegate cb)
    {
//        yield return new WaitForSeconds (4 - i);
        yield return null;
//        Debug.Log (i);
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
                ChildFunc.Create ("one", SeriesFunc1),
                ChildFunc.Create ("two", SeriesFunc2),
                ChildFunc.Create ("three", SeriesFunc3),
                UAsyncFinalFunc.Create ((object err, Dictionary<string, object> res) => {
                    Debug.Log ("Finish " + err);
                    if (err == null) {
//                        Debug.Log ("res " + res ["one"] + " " + res ["two"] + " " + res ["three"]);
                    }
                    throw new Exception ();
                 
                })
            );
//        UAsync.Async.CancelTask (series);
    }

    void SeriesFunc1 (CallbackDelegate cb, Dictionary<string, object> res)
    {
        Debug.Log ("SeriesFunc1");

        //        throw new UnityException ("Exception from Func1");
        cb (null, 100);
    }

    IEnumerator SeriesFunc2 (CallbackDelegate cb, Dictionary<string, object> res)
    {
//        Debug.Log ("SeriesFunc2 " + res ["one"] + " " + res ["two"] + " " + res ["three"]);

        yield return new WaitForSeconds (1);
                        throw new UnityException ("Exception from Func2");
//        Debug.Log ("SeriesFunc2 End");
        cb (null, 200);

    }

    void SeriesFunc3 (CallbackDelegate cb, Dictionary<string, object> res)
    {
//        Debug.Log ("SeriesFunc3 " + res ["one"] + " " + res ["two"] + " " + res ["three"]);
        cb (null, 100);
    }

    void TestParallel ()
    {
        var parallel = 
            UAsync.Async.Parallel (
                ChildFunc.Create ("one", Func1),
                ChildFunc.Create ("two", Func2),
                ChildFunc.Create ("three", Func3),
                UAsyncFinalFunc.Create ((object err, Dictionary<string, object> res) => {
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
//        Debug.Log ("Func2");

        yield return new WaitForSeconds (1);
//        throw new UnityException ("Exception from Func2");
//        Debug.Log ("Func2 End");
        cb (null, 200);
    }

    void Func3 (CallbackDelegate cb)
    {
        Debug.Log ("Func3");

        cb (null, 300);
    }

}
