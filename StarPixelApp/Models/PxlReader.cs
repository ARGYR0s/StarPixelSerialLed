using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Models
{
    class PxlReader
    {
        // Типы и порядок цветов.
        // Заполнить согласно актуальным типам !!!
        public enum format_color_t : byte
        {
            COLOR_RGB = 0x00,
            COLOR_RGBA = 0x01,
            COLOR_GRBA = 0x02,
            COLOR_X03 = 0x03,
            COLOR_X04 = 0x04,
            COLOR_X05 = 0x05,
            COLOR_X06 = 0x06,
            COLOR_X07 = 0x07,
            COLOR_X08 = 0x08,
            COLOR_X09 = 0x09,
            COLOR_X0A = 0x0A,
            COLOR_X0B = 0x0B,
            COLOR_X0C = 0x0C,
            COLOR_X0D = 0x0D,
            COLOR_X0E = 0x0E,
            COLOR_X0F = 0x0F
        };

        // Типы и порядок ленты.
        // Заполнить согласно актуальным типам !!!
        public enum format_strip_t : byte
        {
            STRIP_LINE = 0x00,
            STRIP_X01 = 0x01,
            STRIP_ZIGZAG = 0x02,
            STRIP_X03 = 0x03,
            STRIP_X04 = 0x04,
            STRIP_X05 = 0x05,
            STRIP_X06 = 0x06,
            STRIP_X07 = 0x07,
            STRIP_X08 = 0x08,
            STRIP_X09 = 0x09,
            STRIP_X0A = 0x0A,
            STRIP_X0B = 0x0B,
            STRIP_X0C = 0x0C,
            STRIP_X0D = 0x0D,
            STRIP_X0E = 0x0E,
            STRIP_X0F = 0x0F
        };
        /*
                public static List<(int X, int Y, SKColor Color)> ReadPxlFramedList(
                    List<byte> array, int offsetArray, byte size_x, byte size_y,
                    format_color_t color_pxl, format_strip_t strip, float brightnessBoostCoeff)
                {
                    var pixelList = new List<(int X, int Y, SKColor Color)>();

                    int offset = 0;

                    for (int i = offsetArray + 1; i < (size_x * size_y * 3) + offsetArray; i += 3)
                    {
                        byte color1 = array[i];
                        byte color2 = array[i + 1];
                        byte color3 = array[i + 2];

                        ApplyBrightnessBoost(ref color1, ref color2, ref color3, brightnessBoostCoeff);

                        int pos_x, pos_y;

                        switch (strip)
                        {
                            case format_strip_t.STRIP_LINE:
                                pos_x = (offset % size_x);
                                pos_y = (offset / size_x);
                                break;

                            case format_strip_t.STRIP_ZIGZAG:
                                pos_x = (offset / size_y);
                                pos_y = (pos_x % 2 == 0) ? (offset % size_y) : (size_y - (offset % size_y) - 1);
                                break;

                            default:
                                pos_x = 0;
                                pos_y = 0;
                                break;
                        }

                        if (pos_x < size_x && pos_y < size_y)
                        {
                            SKColor color = color_pxl switch
                            {
                                format_color_t.COLOR_RGB => new SKColor(color1, color2, color3),
                                format_color_t.COLOR_RGBA => new SKColor(color1, color2, color3),
                                format_color_t.COLOR_GRBA => new SKColor(color2, color1, color3),
                                _ => SKColors.Transparent
                            };

                            pixelList.Add((pos_x, pos_y, color));
                        }

                        offset++;
                    }

                    return pixelList;
                }
        */
        public static List<(int X, int Y, SKColor Color)> ReadPxlFramedList(
            List<byte> array, int offsetArray, byte size_x, byte size_y,
            format_color_t color_pxl, format_strip_t strip, float brightnessBoostCoeff)
        {
            // Предварительное выделение памяти для списка, учитывая, что нам нужно хранить size_x * size_y элементов
            var pixelList = new List<(int X, int Y, SKColor Color)>(size_x * size_y);

            int offset = 0;

            for (int i = offsetArray + 1; i < (size_x * size_y * 3) + offsetArray; i += 3)
            {
                byte color1 = array[i];
                byte color2 = array[i + 1];
                byte color3 = array[i + 2];

                ApplyBrightnessBoost(ref color1, ref color2, ref color3, brightnessBoostCoeff);

                int pos_x, pos_y;

                // Вычисление pos_x и pos_y в зависимости от типа strip
                if (strip == format_strip_t.STRIP_LINE)
                {
                    pos_x = (offset % size_x);
                    pos_y = (offset / size_x);
                }
                else if (strip == format_strip_t.STRIP_ZIGZAG)
                {
                    pos_x = (offset / size_y);
                    pos_y = (pos_x % 2 == 0) ? (offset % size_y) : (size_y - (offset % size_y) - 1);
                }
                else
                {
                    pos_x = 0;
                    pos_y = 0;
                }

                // Проверка границ
                if (pos_x < size_x && pos_y < size_y)
                {
                    // Создание цвета в зависимости от формата
                    SKColor color = color_pxl switch
                    {
                        format_color_t.COLOR_RGB => new SKColor(color1, color2, color3),
                        format_color_t.COLOR_RGBA => new SKColor(color1, color2, color3),
                        format_color_t.COLOR_GRBA => new SKColor(color2, color1, color3),
                        _ => SKColors.Transparent
                    };

                    // Добавление в список
                    pixelList.Add((pos_x, pos_y, color));
                }

                offset++;
            }

            return pixelList;
        }
        static void ApplyBrightnessBoost(ref byte R, ref byte G, ref byte B, float boostFactor)
        {
            // Преобразуем в float для вычислений
            float r = R;
            float g = G;
            float b = B;

            // Вычисляем максимальное значение
            float maxChannel = Math.Max(r, Math.Max(g, b));

            if (maxChannel == 0) return; // Если цвет черный, ничего не делаем

            // Усиливаем яркость
            r *= boostFactor;
            g *= boostFactor;
            b *= boostFactor;

            // Нормализуем, чтобы не выйти за пределы 255
            maxChannel = Math.Max(r, Math.Max(g, b));
            if (maxChannel > 255.0f)
            {
                float scale = 255.0f / maxChannel;
                r *= scale;
                g *= scale;
                b *= scale;
            }

            // Преобразуем обратно в byte
            R = (byte)Math.Min(r, 255.0f);
            G = (byte)Math.Min(g, 255.0f);
            B = (byte)Math.Min(b, 255.0f);
        }

        public static List<(int X, int Y, SKColor Color)> ScalePixelsToCanvas(
            List<(int X, int Y, SKColor Color)> pixels, int canvasWidth, int canvasHeight)
        {
            if (pixels.Count == 0) return new List<(int, int, SKColor)>();

            SKColor borderColor = SKColors.Black;

            // Определяем границы исходного изображения
            int minX = pixels.Min(p => p.X);
            int minY = pixels.Min(p => p.Y);
            int maxX = pixels.Max(p => p.X);
            int maxY = pixels.Max(p => p.Y);

            int imgWidth = maxX - minX + 1;
            int imgHeight = maxY - minY + 1;

            // Вычисляем коэффициенты масштабирования (размер нового пикселя)
            int pixelSizeX = canvasWidth / imgWidth;
            int pixelSizeY = canvasHeight / imgHeight;
            int pixelSize = Math.Min(pixelSizeX, pixelSizeY) - 1; // Одинаковый размер для X и Y

            if (pixelSize < 1) pixelSize = 1; // Минимальный размер пикселя = 1

            // Центрирование изображения
            int offsetX = (canvasWidth - imgWidth * (pixelSize + 1)) / 2;
            int offsetY = (canvasHeight - imgHeight * (pixelSize + 1)) / 2;

            // Создаём новый список пикселей с масштабированными координатами
            var scaledPixels = new List<(int X, int Y, SKColor Color)>();

            foreach (var (x, y, color) in pixels)
            {
                // Масштабируем координаты верхнего левого угла
                int newX = (x - minX) * (pixelSize + 1) + offsetX;
                int newY = (y - minY) * (pixelSize + 1) + offsetY;

                // Рисуем сам пиксель (цвет)
                for (int dx = 0; dx < pixelSize; dx++)
                {
                    for (int dy = 0; dy < pixelSize; dy++)
                    {
                        scaledPixels.Add((newX + dx, newY + dy, color));
                    }
                }

                // Рисуем границу (по 1 пикселю вокруг)
                for (int dx = -1; dx <= pixelSize; dx++)
                {
                    scaledPixels.Add((newX + dx, newY - 1, borderColor)); // Верхняя граница
                    scaledPixels.Add((newX + dx, newY + pixelSize, borderColor)); // Нижняя граница
                }
                for (int dy = -1; dy <= pixelSize; dy++)
                {
                    scaledPixels.Add((newX - 1, newY + dy, borderColor)); // Левая граница
                    scaledPixels.Add((newX + pixelSize, newY + dy, borderColor)); // Правая граница
                }
            }

            return scaledPixels;
        }

    }
}
