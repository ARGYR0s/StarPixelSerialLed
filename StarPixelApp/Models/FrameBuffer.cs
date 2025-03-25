using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Models
{
    class FrameBuffer
    {
        private ConcurrentQueue<ImageData> _frames = new();
        //private ImageData? _lastFrame = null;
        private int _frameCount = 0;
        private readonly object _cleanupLock = new(); // Блокировка только для удаления
        private const int MaxFrames = 60;
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion); // Блокировка чтения-записи

        /// <summary>
        /// Возвращает текущее количество кадров в буфере.
        /// </summary>
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _frameCount;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Добавляет новый кадр в очередь.
        /// </summary>
        public void AddFrame(ImageData frame)
        {
            _lock.EnterWriteLock(); // Блокируем все чтения во время добавления
            try
            {
                _frames.Enqueue(frame);
                Interlocked.Increment(ref _frameCount);

                if (_frameCount > MaxFrames)
                {
                    lock (_cleanupLock) // Блокируем только удаление
                    {
                        if (_frames.TryDequeue(out _))
                        {
                            Interlocked.Decrement(ref _frameCount);
                        }
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock(); // Разблокируем после завершения записи
            }
        }

        /// <summary>
        /// Получает кадр: если очередь не пуста — берет новый, иначе возвращает последний доступный.
        /// </summary>
        public ImageData? GetFrame()
        {
            _lock.EnterReadLock(); // Блокируем, чтобы не читать в момент добавления
            try
            {
                if (_frames.TryDequeue(out var frame))
                {
                    Interlocked.Decrement(ref _frameCount);
                    //_lastFrame = frame;
                    return frame;
                }

                return null;// _lastFrame;
            }
            finally
            {
                _lock.ExitReadLock(); // Разблокируем после чтения
            }
        }
    }
}
