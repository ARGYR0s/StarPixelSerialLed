using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;
using System.IO;

//namespace StarPixelApp.Models
//{
    public class RawFileCacheReader : IDisposable
    {
        public bool isFileOpened;
        private FileStream _fileStream;
        private byte[] _cacheBuffer;
        private long _cacheOffset;  // Смещение в файле, с которого начинается буфер
        private int _cacheLength;   // Кол-во байт, реально загруженных в буфер
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly int _cacheFactor = 2;

        public void SetFile(string path)
        {
            _fileStream?.Dispose();
            _fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            _cacheBuffer = Array.Empty<byte>();
            _cacheOffset = 0;
            _cacheLength = 0;
            isFileOpened = true;
        }

        public async ValueTask<byte[]> GetDataAsync(long offset, int length, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (!IsInCache(offset, length))
                {
                    var data = await ReadFromFileAsync(offset, length, cancellationToken);
                    _ = PreloadNextChunkAsync(offset + length, length); // фон

                    return data;
                }

                int startIndex = (int)(offset - _cacheOffset);
                byte[] result = new byte[length];
                Buffer.BlockCopy(_cacheBuffer, startIndex, result, 0, length);

                _ = PreloadNextChunkAsync(offset + length, length); // фон

                return result;
            }
            finally
            {
                _lock.Release();
            }
        }

        private bool IsInCache(long offset, int length)
        {
            return offset >= _cacheOffset && (offset + length) <= (_cacheOffset + _cacheLength);
        }

        private async Task<byte[]> ReadFromFileAsync(long offset, int length, CancellationToken cancellationToken)
        {
            int cacheSize = length * _cacheFactor;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(cacheSize);

            _fileStream.Seek(offset, SeekOrigin.Begin);
            int read = await _fileStream.ReadAsync(buffer.AsMemory(0, cacheSize), cancellationToken);

            // Обновляем кэш
            _cacheBuffer = buffer;
            _cacheOffset = offset;
            _cacheLength = read;

            if (length > read)
            {
                length = read;
            }

            byte[] result = new byte[length];
            Buffer.BlockCopy(buffer, 0, result, 0, length);

            return result;
        }

        private async Task PreloadNextChunkAsync(long offset, int length)
        {
            await _lock.WaitAsync();
            try
            {
                if (IsInCache(offset, length)) return;

                int cacheSize = length * _cacheFactor;
                byte[] buffer = ArrayPool<byte>.Shared.Rent(cacheSize);

                _fileStream.Seek(offset, SeekOrigin.Begin);
                int read = await _fileStream.ReadAsync(buffer.AsMemory(0, cacheSize));

                if (read > 0)
                {
                    // Обновляем кэш только если это данные после текущих
                    if (offset > _cacheOffset)
                    {
                        _cacheBuffer = buffer;
                        _cacheOffset = offset;
                        _cacheLength = read;
                    }
                    else
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                else
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch
            {
                // ignore background read errors
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
            _lock?.Dispose();
            if (_cacheBuffer != null && _cacheBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(_cacheBuffer);
            }
            isFileOpened = false;
        }
    }
//}
