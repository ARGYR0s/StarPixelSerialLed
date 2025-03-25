using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class PixelRenderer
{
    private readonly SKCanvasView _canvasView;
    private SKBitmap _bitmap;
    private bool _isDirty = true;

    private Stopwatch _stopwatch;
    private int _frameCount = 0;
    private double _fps = 0;

    public PixelRenderer(SKCanvasView canvasView, int width, int height)
    {
        _canvasView = canvasView;
        _bitmap = new SKBitmap(width, height);
        _canvasView.PaintSurface += OnPaintSurface;

        // Инициализация Stopwatch для замера времени
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public void SetPixel(int x, int y, SKColor color)
    {
        if (x >= 0 && x < _bitmap.Width && y >= 0 && y < _bitmap.Height)
        {
            _bitmap.SetPixel(x, y, color);
            _isDirty = true;
        }
    }

    public void SetPixels(SKColor[] pixels)
    {
        if (pixels.Length != _bitmap.Width * _bitmap.Height)
            throw new ArgumentException("Invalid pixel array size");

        IntPtr ptr = _bitmap.GetPixels(); // Получаем указатель на пиксели
        int[] pixelData = pixels.Select(c => (c.Alpha << 24) | (c.Red << 16) | (c.Green << 8) | c.Blue).ToArray();
        Marshal.Copy(pixelData, 0, ptr, pixels.Length);

        _isDirty = true;
    }

    public void Clear(SKColor color)
    {
        _bitmap.Erase(color);
        _isDirty = true;
    }

    public void UpdateCanvas()
    {
        if (_isDirty)
        {
            _canvasView.InvalidateSurface();
            _isDirty = false;
        }
    }

    public void OnPaintSurface(object sender, SKPaintSurfaceEventArgs args)
    {
        // Измерение времени между кадрами
        _frameCount++;
        if (_stopwatch.ElapsedMilliseconds >= 1000)
        {
            // Вычисление FPS каждую секунду
            _fps = _frameCount / (_stopwatch.ElapsedMilliseconds / 1000.0);
            _frameCount = 0;
            _stopwatch.Restart();
            //Console.WriteLine($"FPS: {_fps:F2}");  // Выводим FPS в консоль
            AsyncEventBus.Publish("labelFps", "FPS: " + _fps.ToString("F2"));
        }

        var canvas = args.Surface.Canvas;
        var width = args.Info.Width;
        var height = args.Info.Height;

        canvas.Clear(SKColors.White);
        //canvas.DrawBitmap(_bitmap, 0, 0);

        // Масштабируем изображение на весь Canvas
        //canvas.DrawBitmap(_bitmap, new SKRect(0, 0, args.Info.Width, args.Info.Height));

        // Масштабируемая матрица преобразования для растягивания изображения
        var scaleX = (float)width / _bitmap.Width;
        var scaleY = (float)height / _bitmap.Height;

        // Очищаем фон канвы
        canvas.Clear(SKColors.White);

        // Рисуем изображение с учетом масштаба
        canvas.Save();
        canvas.Scale(scaleX, scaleY);
        canvas.DrawBitmap(_bitmap, 0, 0);
        canvas.Restore();

        // Устанавливаем кисть для рисования сетки
        var gridPaint = new SKPaint
        {
            Color = SKColors.Black,   // Цвет сетки
            StrokeWidth = 1,          // Толщина линии сетки
            IsAntialias = true        // Антиалиасинг для сглаживания
        };

        // Рисуем вертикальные линии сетки
        for (int x = 1; x < _bitmap.Width; x++)
        {
            float scaledX = x * scaleX;  // Масштабируем X-координату
            canvas.DrawLine(scaledX, 0, scaledX, height, gridPaint);
        }

        // Рисуем горизонтальные линии сетки
        for (int y = 1; y < _bitmap.Height; y++)
        {
            float scaledY = y * scaleY;  // Масштабируем Y-координату
            canvas.DrawLine(0, scaledY, width, scaledY, gridPaint);
        }
    }

    public SKBitmap CreateBitmapFromPixels(List<(int X, int Y, SKColor Color)> pixels)
    {
        if (pixels.Count == 0)
            return null;

        // Определяем границы переданных пикселей
        int minX = pixels.Min(p => p.X);
        int minY = pixels.Min(p => p.Y);
        int maxX = pixels.Max(p => p.X);
        int maxY = pixels.Max(p => p.Y);

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        // Создаем временный битмап
        SKBitmap newBitmap = new SKBitmap(width, height);

        foreach (var pixel in pixels)
        {
            newBitmap.SetPixel(pixel.X - minX, pixel.Y - minY, pixel.Color);
        }

        return newBitmap;
    }

    public void SetBitmap(SKBitmap newBitmap)
    {
        _bitmap.Dispose();  // Освобождаем старый битмап
        _bitmap = newBitmap;
        _isDirty = true;
        UpdateCanvas();  // Инициализируем перерисовку
    }

    public async Task StartRenderingAsync(int targetFps = 10)
    {
        int delay = 1000 / targetFps;
        while (true)
        {
            /*
            // Измерение времени между кадрами
            _frameCount++;
            if (_stopwatch.ElapsedMilliseconds >= 1000)
            {
                // Вычисление FPS каждую секунду
                _fps = _frameCount / (_stopwatch.ElapsedMilliseconds / 1000.0);
                _frameCount = 0;
                _stopwatch.Restart();
                //Console.WriteLine($"FPS: {_fps:F2}");  // Выводим FPS в консоль
                EventBus.Publish("labelFps", _fps.ToString());
            }
            */

            UpdateCanvas();
            await Task.Delay(delay);
        }
    }
}