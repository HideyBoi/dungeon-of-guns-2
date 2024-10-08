using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Godot;

/// <summary>
/// This class lets you safely call some code to access the Node/Scene tree from a Task / worker thread.  For example updating UI labels etc to tell the user some work is complete.
/// Godot 4.1 is more strict than 4.0 around threads.  It throws errors if you access nodes from threads other then the main thread running _Process().
/// </summary>
public partial class MainThreadInvoker: Node
{
    private static ConcurrentQueue<Action> deferredActionQueue = new ConcurrentQueue<Action>();

    private static double deltaTotal = 0;

    // This is the interval in seconds that we check the concurrent queue. For example 0.1 will check 10 times per second.
    public static double CheckIntervalSeconds = 0.1;

    /// <summary>
    /// Call this passing an action from your Task or worker Thread.  The action will be run on the main Process thread (eg: updating UI etc).
    /// Therefore you should avoid doing too much processing in this action to ensure the scene remains responsive.
    /// Note: no parameters are needed because you can use the usual C# 'capture' feature to access any variables in scope before calling this.
    /// </summary>
    /// <param name="action"></param>
    public static void InvokeOnMainThread(Action action)
    {
        deferredActionQueue.Enqueue(action);
    }

    public override void _Process(double delta)
    {
        ProcessMainThreadQueue(delta);
    }

    /// <summary>
    /// This method should be called from the your scene _Process() method, passing it the delta value. 
    /// It only checks the queue every so often to avoid wasting processing time.  Interval is configurable by changing the value of the CheckIntervalSeconds field
    /// </summary>
    /// <param name="delta"></param>
    public static void ProcessMainThreadQueue(double delta)
    {
        deltaTotal += delta;

        if (deltaTotal > CheckIntervalSeconds)
        {
            deltaTotal = deltaTotal - CheckIntervalSeconds;

            // NB: All actions in the queue will be processed now on the main Process thread, so you should avoid big processing task here to keep responsiveness.
            while (deferredActionQueue.TryDequeue(out Action action))
            {
                action();
            };
        }
    }
}