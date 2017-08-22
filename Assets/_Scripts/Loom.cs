using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class Loom : MonoBehaviour
{
    public static int maxThreads = 8;
    private static int numThreads;

    private static Loom _current;
    private int _count;

    public static Loom Current {
        get {
            Initialize();
            return _current;
        }
    }

    private void Awake()
    {
        _current = this;
        initialized = true;
    }

    private static bool initialized;

    private static void Initialize()
    {
        if (!initialized)
        {
            if (!Application.isPlaying)
                return;
            initialized = true;
            var g = new GameObject("Loom");
            _current = g.AddComponent<Loom>();
        }
    }
    /// <summary>
    /// 执行列表
    /// </summary>
    private List<Action> _actions = new List<Action>();
    /// <summary>
    /// 延迟列队
    /// </summary>
    public struct DelayedQueueItem
    {
        public float time;
        public Action action;
    }
    /// <summary>
    /// 延迟列表
    /// </summary>
    private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();
    /// <summary>
    /// 当前延迟列表
    /// </summary>
    private List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

    //3 ：要主线程运行的内容列队
    public static void QueueOnMainThread(Action action)
    {
        QueueOnMainThread(action, 0f);
    }
    //4：
    public static void QueueOnMainThread(Action action, float time)
    {
        if (time != 0)
        {
            lock (Current._delayed)
            {
                //将主线程需要运行的内容加入延迟列队 并存入加入的时间
                Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action });
            }
        }
        else 
        {   //如果time 等于 0 的时候直接加入执行列表
            lock (Current._actions)
            {
                //添加入执行列表
                Current._actions.Add(action);
            }
        }
    }
    //1：运行异步
    public static Thread RunAsync(Action a)     //起始点 添加内容进入列表
    {
        Initialize();                           //单例
        while (numThreads >= maxThreads)
        {
            Thread.Sleep(1);
        }
        Interlocked.Increment(ref numThreads);  //原子增量 锁
        ThreadPool.QueueUserWorkItem(RunAction, a);     //启动线程 并传入参数 a 要运行的方法内容
        return null;
    }
    //2：运行动作 异步执行了发送的参数 参数内容是包括的方法函数等
    private static void RunAction(object action)
    {
        try
        {
            ((Action)action)();
        }
        catch
        {
        }
        finally
        {
            Interlocked.Decrement(ref numThreads);  //原子减去 锁
        }
    }

    private void OnDisable()
    {
        if (_current == this)
        {
            _current = null;
        }
    }


    private List<Action> _currentActions = new List<Action>();

    private void Update()
    {
        lock (_actions)//运行执行列表
        {
            _currentActions.Clear();  
            _currentActions.AddRange(_actions);//执行列表的内容，添加入目前执行的列表末尾
            _actions.Clear();//清空执行列表
        }
        foreach (var a in _currentActions)//循环目前执行列表，并在主线程执行其中程序
        {
            a();
        }
        lock (_delayed)//延迟列表
        {
            _currentDelayed.Clear();//清理当前延迟列表
            _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));//从延迟执行的列表，选择记录时间小于等于当前时间的延迟内容添加入
            foreach (var item in _currentDelayed)
                _delayed.Remove(item);//剔除延迟列表中被转移的内容
        }
        foreach (var delayed in _currentDelayed)
        {
            delayed.action();//执行延迟列表的内容
        }
    }
}