using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Diagnostics;

public static class AsyncEventBus
{
    private static readonly Dictionary<string, Delegate> _events = new();
    private static readonly Dictionary<string, ConcurrentQueue<object>> _eventQueues = new();
    private static readonly Dictionary<string, ManualResetEventSlim> _eventSignals = new();
    private static readonly Dictionary<string, Thread> _eventThreads = new();
    private static readonly object _lock = new();

    public static void Subscribe<T>(string eventName, Action<T> callback)
    {
        if (eventName == null || callback == null) return;

        lock (_lock)
        {
            if (_events.ContainsKey(eventName))
                _events[eventName] = Delegate.Combine(_events[eventName], callback);
            else
                _events[eventName] = callback;

            if (!_eventQueues.ContainsKey(eventName))
                _eventQueues[eventName] = new ConcurrentQueue<object>();

            if (!_eventSignals.ContainsKey(eventName))
                _eventSignals[eventName] = new ManualResetEventSlim(false);

            if (!_eventThreads.ContainsKey(eventName))
            {
                var thread = new Thread(() => ProcessEventQueue<T>(eventName));
                thread.IsBackground = true;
                _eventThreads[eventName] = thread;
                thread.Start();
            }
        }
    }

    public static void Unsubscribe<T>(string eventName, Action<T> callback)
    {
        if (eventName == null || callback == null) return;

        lock (_lock)
        {
            if (_events.ContainsKey(eventName))
            {
                _events[eventName] = Delegate.Remove(_events[eventName], callback);
                if (_events[eventName] == null)
                    _events.Remove(eventName);
            }
        }
    }

    // Проверка наличия подписки для страницы
    public static bool HasSubscription(string pageId)
    {
        return _events.ContainsKey(pageId);
    }

    public static void Publish<T>(string eventName, T data)
    {
        if (eventName == null) return;

        lock (_lock)
        {
            if (_events.ContainsKey(eventName))
            {
                _eventQueues[eventName].Enqueue(data!);
                _eventSignals[eventName].Set(); // Будим поток обработки
            }
        }
    }
    /*
    private static void ProcessEventQueue<T>(string eventName)
    {
        while (_events.ContainsKey(eventName))
        {
            _eventSignals[eventName].Wait(); // Ждем появления данных
            _eventSignals[eventName].Reset();

            while (_eventQueues[eventName].TryDequeue(out var data))
            {
                if (_events[eventName] is Action<T> action)
                    action((T)data);
            }
        }
    }
    */

    private static void ProcessEventQueue<T>(string eventName)
    {
        try
        {
            while (true)
            {
                lock (_lock)
                {
                    if (!_events.ContainsKey(eventName) || !_eventSignals.ContainsKey(eventName) || !_eventQueues.ContainsKey(eventName))
                        break; // Выходим из цикла, если событие удалено
                }

                _eventSignals[eventName].Wait(); // Ждем появления данных

                lock (_lock)
                {
                    if (!_eventQueues.ContainsKey(eventName)) break; // Проверяем наличие очереди
                    _eventSignals[eventName].Reset();
                }

                while (true)
                {
                    object data;
                    lock (_lock)
                    {
                        if (!_eventQueues.ContainsKey(eventName) || !_eventQueues[eventName].TryDequeue(out data))
                            break; // Если очередь пропала или пуста, выходим
                    }

                    if (_events.TryGetValue(eventName, out var del) && del is Action<T> action)
                    {
                        action((T)data);
                    }
                }
            }
        }
        catch (ThreadInterruptedException)
        {
            // Поток прерван при отписке — корректно завершаем выполнение
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка в обработке событий {eventName}: {ex.Message}");
        }
    }
    public static void UnsubscribeId(string eventName)
    {
        if (eventName == null) return;

        lock (_lock)
        {
            if (_events.ContainsKey(eventName))
            {
                _events.Remove(eventName);
            }

            if (_eventQueues.ContainsKey(eventName))
            {
                _eventQueues.Remove(eventName);
            }

            if (_eventSignals.ContainsKey(eventName))
            {
                _eventSignals[eventName].Set();
                _eventSignals[eventName].Dispose();
                _eventSignals.Remove(eventName);
            }

            if (_eventThreads.ContainsKey(eventName))
            {
                if (_eventThreads[eventName].IsAlive)
                {
                    _eventThreads[eventName].Interrupt();
                }
                _eventThreads.Remove(eventName);
            }
        }
    }

    public static void UnsubscribeAll()
    {
        lock (_lock)
        {
            // Очистка всех подписчиков
            _events.Clear();

            // Остановка всех потоков
            foreach (var thread in _eventThreads.Values)
            {
                if (thread.IsAlive)
                {
                    thread.Interrupt(); // Прерываем поток
                }
            }

            _eventThreads.Clear();

            // Очистка очередей и сигналов
            foreach (var signal in _eventSignals.Values)
            {
                signal.Set(); // Разблокируем, если кто-то ждет
                signal.Dispose();
            }

            _eventSignals.Clear();
            _eventQueues.Clear();
        }
    }
}
