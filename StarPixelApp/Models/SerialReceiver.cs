//using Java.Nio;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SkiaSharp;
using StarPixelApp.Models;
using static System.Net.Mime.MediaTypeNames;
//using static CoreFoundation.DispatchSource;
using static StarPixelApp.Models.PxlReader;

namespace StarPixelApp.Models
{
    public class SerialReceiver
    {
        private const char DEV_AT = ',';
        private const char END_ATr = '\r';
        private const char END_ATn = '\n';
        private static readonly byte[] PXL_AT_STR = Encoding.ASCII.GetBytes("+PXL=");
        private const int SCR_COLOR_SIZE = 3;

        private List<byte> dataBuf = new();
        private List<byte> relocateBuf = new();
        private List<byte> pixelBuf = new();

        private FrameBuffer frames = new FrameBuffer();
        private FrameBuffer framesSerial = new FrameBuffer();
        private FrameBuffer framesGenerate = new FrameBuffer();

        //private List<int> pixelBufSize = new();
        //private List<int> relocatePixelSize = new();

        //List<ImageData> frames = new List<ImageData>();

        //private SerialPort serialPort;

        private int scrWidth, scrHeight, scrColor, scrDataSize;
        private int posAt = -1, posAtWidth = -1, posAtHeight = -1, posAtColor = -1, posAtEnd = -1;
        private bool isAtFind = false, isAtParam = false, isAtReady = false, isCorruptedAtParam;

        private int posProcessedData;

        private uint iSerialUpd;

        private TaskCompletionSource<bool>? _dataReceivedSignal = new();

        // Создаем экземпляр Stopwatch
        Stopwatch stopwatch = new Stopwatch();

        public SerialReceiver()
        {
            //serialPort = port;
            //serialPort.DataReceived += async (s, e) => await DataReceivedAsync();
            AsyncEventBus.Subscribe<byte[]>("deviceDataReceived", SeriaReceived);
            //Debug.WriteLine($"SerialReceivers: {SeriaReceived}");
        }

        private static int GetParamOffset(ReadOnlySpan<byte> data, int offset)
        {
            for (int i = offset + 1; i < data.Length; i++)
            {
                if (data[i] == DEV_AT || (data[i] == END_ATn && i > 0/* && data[i - 1] == END_ATr*/))
                    return i;
            }
            return -1;
        }

        private static int GetAtOffsetParam(ReadOnlySpan<byte> data, int offset)
        {
            int val = 0;
            //для проверки что END_ATr была
            int isATrWas = 0;

            for (int i = offset + 1; i < data.Length; i++)
            {
                if (data[i] == DEV_AT || (data[i] == END_ATn && i > 0/* && data[i - 1] == END_ATr*/))
                    return val;
                if (char.IsDigit((char)data[i]))
                    val = (val * 10) + (data[i] - '0');
            }
            return -1;
        }

        private static int GetAtPosEnd(ReadOnlySpan<byte> data, int offset)
        {
            //int index = data.IndexOf(PXL_AT_STR,  10);
            // Проверка, что offset находится в пределах длины data
            if (offset < 0 || offset >= data.Length)
            {
                return -1;
            }

            int index = data.Slice(offset).IndexOf(PXL_AT_STR);

            return index >= 0 ? index + offset + PXL_AT_STR.Length : -1;

        }

        private static int CalcAt(ReadOnlySpan<byte> data, int offset)
        {
            int count = 0;
            int position = 0;

            while (position < data.Length)
            {
                int index = data.Slice(offset+position).IndexOf(PXL_AT_STR);
                if (index < 0) // Больше вхождений не найдено
                    break;

                count++;
                position += index + PXL_AT_STR.Length;
            }

            return count;
        }


        private const int BufferSize = 1048576; // Размер буфера
        private readonly byte[] buffer = new byte[BufferSize];
        private int head = 0; // Начало данных
        private int tail = 0; // Конец данных (новые данные записываются сюда)
        private int count = 0; // Количество байтов в буфере
        private readonly object bufferLock = new();

        private void SaveHexToFile(byte[] data, string filePath)
        {
            try
            {
                string hexData = Convert.ToHexString(data);
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {hexData}{Environment.NewLine}";
                File.AppendAllText(filePath, logEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving to file: {ex.Message}");
            }
        }

        private async void SeriaReceived(object data)
        {
            try
            {
                if (data is byte[] bytes)
                {

                    // Путь к файлу (можно настроить)
                    //string logFilePath =  "SerialDataHex_250322.txt";

                    // Сохраняем данные в hex-формате в файл
                    //SaveHexToFile(bytes, logFilePath);

                    lock (bufferLock)
                    {
                        int bytesToWrite = bytes.Length;

                        // Проверяем, есть ли место в буфере
                        if (bytesToWrite > BufferSize)
                        {
                            // Если приходят данные больше размера буфера — оставляем только последние `BufferSize` байт
                            bytesToWrite = BufferSize;
                            Array.Copy(bytes, bytes.Length - BufferSize, buffer, 0, BufferSize);
                            head = 0;
                            tail = 0;
                            count = BufferSize;
                        }
                        else
                        {
                            // Если не хватает места, сдвигаем указатель головы (удаляем старые данные)
                            while (count + bytesToWrite > BufferSize)
                            {
                                head = (head + 1) % BufferSize;
                                if (count > 0) count--;
                            }

                            // Записываем новые данные в буфер
                            for (int i = 0; i < bytesToWrite; i++)
                            {
                                buffer[tail] = bytes[i];
                                tail = (tail + 1) % BufferSize;
                            }
                            
                            //Array.Copy(bytes, 0, buffer, tail, bytesToWrite);

                            count += bytesToWrite;
                        }
                    }

                    //Debug.WriteLine($"SerialBufferSize: {count}");
                }
                else
                {
                    Debug.WriteLine("Error: Expected byte[] data, but received: " + data.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        private int _currentX = 0;
        private bool _movingRight = true;

        //private int _currentX = 0;
        private int _direction = 1;

        private List<(int X, int Y, SKColor Color)> ConvertToPixelList(ImageData imageData)
        {
            var pixels = new List<(int X, int Y, SKColor Color)>();
            for (int y = 0; y < imageData.Height; y++)
            {
                for (int x = 0; x < imageData.Width; x++)
                {
                    int index = (y * imageData.Width + x) * 3;
                    byte r = imageData.PixelData[index];
                    byte g = imageData.PixelData[index + 1];
                    byte b = imageData.PixelData[index + 2];
                    pixels.Add((x, y, new SKColor(r, g, b)));
                }
            }
            return pixels;
        }
        private ImageData GenerateFrameImage(int width, int height, int squareWidth, int squareHeight)
        {
            var pixelData = new List<byte>(width * height * 3); // RGB по 3 байта на пиксель

            // Заполняем фон черным цветом (RGB: 0, 0, 0)
            for (int i = 0; i < width * height; i++)
            {
                pixelData.Add(0); // R
                pixelData.Add(0); // G
                pixelData.Add(0); // B
            }

            // Вычисляем вертикальный центр для квадрата
            int startY = (height - squareHeight) / 2;
            int maxX = width - squareWidth; // Максимальная позиция X

            // Обновляем положение квадрата
            _currentX += _direction;
            if (_currentX < 0 || _currentX > maxX)
            {
                _direction *= -1; // Меняем направление
                _currentX += _direction;
            }

            // Рисуем зеленый квадрат (RGB: 0, 255, 0)
            for (int y = startY; y < startY + squareHeight; y++)
            {
                for (int x = _currentX; x < _currentX + squareWidth; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        int index = (y * width + x) * 3;
                        pixelData[index] = 0;   // R
                        pixelData[index + 1] = 255; // G
                        pixelData[index + 2] = 0;   // B
                    }
                }
            }

            return new ImageData(pixelData, width, height);
        }


        private List<(int X, int Y, SKColor Color)> GenerateFrame(int width, int height, int squareWidth, int squareHeight)
        {
            var pixels = new List<(int X, int Y, SKColor Color)>();

            // Заполняем фон черным цветом
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels.Add((x, y, SKColors.Black));
                }
            }

            // Вычисляем вертикальный центр для квадрата
            int startY = (height - squareHeight) / 2;
            int maxX = width - squareWidth; // Максимальная позиция X

            // Рисуем зеленый квадрат
            for (int y = startY; y < startY + squareHeight; y++)
            {
                for (int x = _currentX; x < _currentX + squareWidth; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        pixels.Add((x, y, SKColors.Green));
                    }
                }
            }

            // Обновляем позицию и направление
            if (_movingRight)
            {
                _currentX++;
                if (_currentX >= maxX) // Если достиг правого края
                {
                    _movingRight = false; // Меняем направление
                }
            }
            else
            {
                _currentX--;
                if (_currentX <= 0) // Если достиг левого края
                {
                    _movingRight = true; // Меняем направление
                }
            }

            return pixels;
        }

        public async Task ProcessDataExternally()
        {
            ProcessData();
        }
  
        //чтоб не пересматривать весь массив, ищем только в возможных 3 кадрах
        const int SERIAL_FRAME_SIZE = 3*16*128*3; //18 432 = 3 фрагмента 16 строк 128 столбцов по 3 байта на цвет
        int lastSizeBuf;
        int commandFound;
        public async void ProcessData()
        {

            byte[] bufferCopy;
            int bufferSize;
            int localProcessedData;


            lock (bufferLock)
            {
                if (count <= 0) return; // Нет новых данных
                //Debug.WriteLine("Continue");

                bufferSize = Math.Min(count, SERIAL_FRAME_SIZE);

                bufferCopy = new byte[bufferSize];

                // Копируем только необработанные данные
                for (int i = 0; i < bufferSize; i++)
                {
                    int index = (head + i) % BufferSize;
                    bufferCopy[i] = buffer[index];
                }

                //lastSizeBuf = count;
                localProcessedData = 0; // Сохраняем локальное значение
            }

            ReadOnlySpan<byte> spanData = bufferCopy;

            if (!isAtFind)
            {
                posAt = GetAtPosEnd(spanData, 0);

                if (posAt > -1)
                {
                    isAtFind = true;
                    commandFound++;

                }
            }

            if (isAtFind && !isAtParam)
            {
                ProcessAtParams(spanData);
            }

            if (isAtParam)
            {
                ProcessImageData(spanData);
                localProcessedData = posAtEnd; // Все данные обработаны
            }
            else if (isCorruptedAtParam)
            {
                localProcessedData = posAtEnd;
            }
            else if (bufferSize >= SERIAL_FRAME_SIZE)
            {
                localProcessedData = bufferSize;
            }

            if (localProcessedData > 0)
            {
                lock (bufferLock)
                {
                    head = (head + localProcessedData) % BufferSize; // Обновляем указатель
                    count -= localProcessedData; // Уменьшаем количество данных
                                                 //posProcessedData = 0; // Сбрасываем, так как обработали все
                }
            }

            ResetState();

        }

        private void ProcessAtParams(ReadOnlySpan<byte> spanData)
        {
            if (posAtWidth < 0)
            {
                scrWidth = GetAtOffsetParam(spanData, posAt - 1);
                posAtWidth = GetParamOffset(spanData, posAt - 1);
            }
            if (posAtWidth > 0 && posAtHeight < 0)
            {
                scrHeight = GetAtOffsetParam(spanData, posAtWidth);
                posAtHeight = GetParamOffset(spanData, posAtWidth);
            }
            if (posAtHeight > 0)
            {
                scrColor = GetAtOffsetParam(spanData, posAtHeight);
                posAtColor = GetParamOffset(spanData, posAtHeight);
            }
            if (posAtColor > 0)
            {
                int requiredSize = scrWidth * scrHeight * SCR_COLOR_SIZE;
                if (spanData.Length > (requiredSize + posAtColor+1))
                {
                    if (spanData[posAtColor + requiredSize + 1] == END_ATn)
                    {
                        isAtParam = true;
                        
                    }
                    else
                    {
                        isCorruptedAtParam = true;
                    }
                    posAtEnd = posAtColor + requiredSize + 1;

                }
            }
        }

        private void ProcessBuffer() 
        {
            int sizeArray = (scrWidth * scrHeight * SCR_COLOR_SIZE);
            int posNextFrame = sizeArray + posAtColor;
            if (dataBuf.Count <= posNextFrame * 100) //защита от переполнения буфера на 100 кадров
            {
                relocateBuf.AddRange(dataBuf.Skip(posNextFrame));
            }

            pixelBuf.AddRange(dataBuf.GetRange(posAtColor, sizeArray));

            ResetState();
        }

        int counter1;
        private void ProcessImageData(ReadOnlySpan<byte> spanData)
        {
            int sizeFrame = scrWidth * scrHeight * SCR_COLOR_SIZE;


            // Взять подмножество данных от n до k
            ReadOnlySpan<byte> subSpan = spanData.Slice(posAtColor+1, sizeFrame);

            // Копируем данные в List<byte>
            List<byte> pixelData = new List<byte>(sizeFrame);
            pixelData.AddRange(subSpan.ToArray()); // Преобразуем span в массив и добавляем в список

            // Добавляем кадр
            frames.AddFrame(new ImageData(pixelData, scrWidth, scrHeight));
        }



        private void ResetState()
        {
            isAtFind = isAtParam = isAtReady = isCorruptedAtParam = false;
            scrWidth = scrHeight = scrColor = 0;
            posAt = posAtWidth = posAtHeight = posAtColor = posAtEnd = scrDataSize = - 1;
        }

        int frameCnt;
        public async Task ScreenUpdater()
        {
            AsyncEventBus.Publish("readyFrames", $"Ready frames: {framesSerial.Count}");

            frameCnt++;

            var firstFrame = frames.GetFrame();

            if (firstFrame != null)
            {
                List<(int X, int Y, SKColor Color)> imageSerial2 =  ReadPxlFramedList(firstFrame.PixelData, -1, (byte)firstFrame.Width, (byte)firstFrame.Height,
                    format_color_t.COLOR_GRBA, format_strip_t.STRIP_ZIGZAG, 10);

                AsyncEventBus.Publish("canvas1", imageSerial2);
            }

            AsyncEventBus.Publish("frames", "frames: " + frameCnt);

        }

    }

}
