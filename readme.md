# Unity Async

Nowadays, if you want to use a structured way for your flow control in Unity, you basically have 4 options:

* Write your own `Task` library (which might use coroutines)
* Use coroutines. This means that you `StartCoroutine` in a lot of places and insert try catch code when errors occur. This works for small games. For larger games, not being able to catch nested exception is a big NO NO.
* Use [C-Sharp-Promise](https://github.com/Real-Serious-Games/C-Sharp-Promise). If you're familiar with JS promises, this comes natural to you. It handles exceptions pretty well. You can try combining this with coroutine, but the API is probably verbose.
* Use [UniRx](https://github.com/neuecc/UniRx). This is simply the best choice because it supports control flow, exception handling, progress report and coroutine.

So we should always use `UniRx`, right? Unfortunately, sometimes the efforts to use `UniRx` is just too much that we can't afford. In that case, it's better to use existing solution, but with more robust code. (`C-Sharp-Promise` is ofcourse another option, but it is not compatible with coroutine and existing coroutine code without some custom modifications).

`UAsync` (Unity Async) is a library that helps you write Unity code using callback style of `Node.js` and `async` library. The `TaskRunner` part is taken from [Svelto.Tasks](https://github.com/sebas77/Svelto.Tasks) with some modifications to make it support catching exceptions and returning errors. The `UAsync` class adds several functions on top of `TaskRunner` to support execution of tasks in parallel or serial with returned results at the end of the execution. For the moment, it does not support `Thread` because it focuses on control flow, not enhancing performance by distributing work to multiple cores.

To include UAsync into your project, you can use `npm` method of unity package management described [here](https://github.com/minhhh/UBootstrap).

## Usage

**TaskRunner**

First of all, it's quite well-known that 2 main disadvantages of coroutine are: 1) it cannot return value and 2) it cannot handle nested exception. There's a simple way to wrap coroutine so we can support those 2 features, as detailed in [this article](http://www.zingweb.com/blog/2013/02/05/unity-coroutine-wrapper). `TaskRunner` also supports returning value and catching exceptions using callback style. You use it like so:

```
using UAsync;
...
TaskRunner.Instance.Run (task, onComplete);
// public TaskRoutine Run (IEnumerator task, CallbackDelegate onComplete = null)
```

In the code above, `task` is a `IEnumerator` and `onComplete` is a delegate of type `CallbackDelegate (object err = null, object res = null)`. Any exceptions occur will be passed via `err`. The last `yield` in `task` will be passed to `res`. You might want to use `TaskRunner.Instance.Run` when you have a sequence of actions to be performed in a fixed order.

**UAsync**

`UAsync` is a port of Node's `async` module to Unity environment. It can be used to turn a set of synchronous functions or coroutine to run sequentially or concurrently. Even though you can already run a set of tasks sequentially using coroutine, passing values between these tasks are proven to be difficult. You have to use external variables to hold the return values which creates coupling between functions, and it's not convenient. `UAsync` can solve this problem by allowing coroutine to return value, as well as catching exceptions if any.

Let's look at an example:

```
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

void SeriesFunc1 (CallbackDelegate cb, Dictionary<string, object> res)
{
    cb (null, 100);
}

IEnumerator SeriesFunc2 (CallbackDelegate cb, Dictionary<string, object> res)
{
    yield return new WaitForSeconds (1);
    cb (null, 200);
}

void SeriesFunc3 (CallbackDelegate cb, Dictionary<string, object> res)
{
    cb (null, 300);
}
```

Here, `SeriesFunc.FromAction` and `SeriesFunc.FromEnumerator` are just convenient functions to wrap synchronous functions and coroutines. Each of the functions `SeriesFunc1`, `SeriesFunc2` and `SeriesFunc3` will receive a callback parameter and a `res` parameter. To complete the execution of each function, you must call `cb` with 2 parameters: `err` representing the error, and `result` representing the returned value. In the code above, there is no error. If any of the code in those function throws exception, cb will also be called automatically with the exception as the first parameter.

The second parameter passed to each of the functions `SeriesFunc1`, `SeriesFunc2` and `SeriesFunc3` is quite important. It is a dicionary which contains all the results from previous functions, so `SeriesFunc2` will receive result from `SeriesFunc1`, `SeriesFunc3` will receive results from `SeriesFunc1` and `SeriesFunc2`. The key of the dictionary are declared when creating the series, e.g. SeriesFunc.FromAction ("one", SeriesFunc1) means the result of SeriesFunc1 will have key "one". This is a powerful way to pass results between functions without creating high coupling between them.

After all the functions have been executed, there is a final function `UAsyncFinalFunc` which will receive all the results and execute some logic accordingly. If any of the functions above throws exceptions or calls callback with a `err` parameter, the error will be passed to the final function to deal with.

The `UAsync.Async.Parallel` function is similar to the `Series` function, except that there will be no results from previous functions since they're executed concurrently.

Finally, you can cancel a running sequence once it's started. This is not obvious with coroutine because even though you can call `MonoBehaviour.StopCoroutine`


## Changelog

**0.0.2**

* Add `Each` and `EachSeries`

**0.0.1**

* Initial commit

<br/>

