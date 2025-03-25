using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Collections.Generic;

namespace StarPixelApp.ViewModels
{
    public class GridCanvasView : SKCanvasView
    {
        private int _rows;
        private int _cols;
        private readonly Dictionary<(int, int), SKColor> _cellColors = new();

        public GridCanvasView()
        {
            // Создаем новый путь (линия)
            var path = new SKPath();
            path.MoveTo(50, 50);
            path.LineTo(150, 150);

            // Отправляем в EventBus
            AsyncEventBus.Publish("canvas1", path);
        }

        public GridCanvasView(int rows, int cols)
        {
            _rows = rows;
            _cols = cols;

            PaintSurface += OnCanvasPaint;
            AsyncEventBus.Subscribe<byte[]>("UpdateCellColor", UpdateCellColor);
            AsyncEventBus.Subscribe<byte[]>("ResizeGrid", ResizeGrid);
        }

        private void OnCanvasPaint(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            var info = e.Info;

            canvas.Clear(SKColors.White);

            float cellWidth = info.Width / (float)_cols;
            float cellHeight = info.Height / (float)_rows;

            using var paint = new SKPaint { StrokeWidth = 1, IsAntialias = true };

            // Заполнение цветами
            foreach (var cell in _cellColors)
            {
                int row = cell.Key.Item1;
                int col = cell.Key.Item2;
                paint.Color = cell.Value;
                canvas.DrawRect(col * cellWidth, row * cellHeight, cellWidth, cellHeight, paint);
            }

            // Отрисовка сетки
            paint.Color = SKColors.Black;
            paint.Style = SKPaintStyle.Stroke;

            for (int i = 0; i <= _rows; i++)
            {
                float y = i * cellHeight;
                canvas.DrawLine(0, y, info.Width, y, paint);
            }

            for (int j = 0; j <= _cols; j++)
            {
                float x = j * cellWidth;
                canvas.DrawLine(x, 0, x, info.Height, paint);
            }
        }

        private async void UpdateCellColor(object data)
        {
            if (data is (int row, int col, string hexColor))
            {
                if (row >= 0 && row < _rows && col >= 0 && col < _cols)
                {
                    _cellColors[(row, col)] = SKColor.Parse(hexColor);
                    MainThread.BeginInvokeOnMainThread(InvalidateSurface);
                }
            }
        }

        private async void ResizeGrid(object data)
        {
            if (data is (int newRows, int newCols))
            {
                _rows = newRows;
                _cols = newCols;
                _cellColors.Clear(); // Очистка цветов при изменении размеров
                MainThread.BeginInvokeOnMainThread(InvalidateSurface);
            }
        }
    }
}
