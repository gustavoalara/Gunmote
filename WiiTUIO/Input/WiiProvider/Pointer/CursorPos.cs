using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiimoteLib;

namespace WiiTUIO.Provider
{
    public class CursorPos
    {
        public int X;
        public int Y;
        public double RelativeX;
        public double RelativeY;
        public double Rotation;
        public bool OutOfReach;
        public double MarginX;
        public double MarginY;
        public double LightbarX;
        public double LightbarY;
        public double Width;
        public double Height;
        public bool OffScreen;

        public PointF[] OnScreenPoints;

        public CursorPos(int x, int y, double relativeX, double relativeY, double rotation,
            double marginX = 0.0, double marginY = 0.0, double lightbarX = 0.0, double lightbarY = 0.0, double lightbarWidth = 0.0, double lightbarHeight = 0.0)
        {
            this.X = x;
            this.Y = y;
            this.RelativeX = relativeX;
            this.RelativeY = relativeY;
            this.Rotation = rotation;
            this.OutOfReach = false;
            this.MarginX = marginX;
            this.MarginY = marginY;
            this.LightbarX = lightbarX;
            this.LightbarY = lightbarY;
            this.Width = lightbarWidth;
            this.Height = lightbarHeight;

            this.OnScreenPoints = new PointF[4];
        }
    }
}
