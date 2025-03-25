using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Models
{
    class ImageData
    {
        public List <byte> PixelData { get; set; } // Массив байт (кадр)
        public int Width { get; set; }        // Ширина кадра
        public int Height { get; set; }       // Высота кадра

        public ImageData(List <byte> pixelData, int width, int height)
        {
            PixelData = pixelData;
            Width = width;
            Height = height;
        }
    }
}
