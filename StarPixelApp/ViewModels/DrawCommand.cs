using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.ViewModels
{
    public class DrawCommand
    {
        public int X { get; set; }   // Координата X
        public int Y { get; set; }   // Координата Y
        public SKPath Path { get; set; }
        public SKColor Color { get; set; }

        public DrawCommand(SKPath path, SKColor color)
        {
            Path = path;
            Color = color;
        }
    }
}
