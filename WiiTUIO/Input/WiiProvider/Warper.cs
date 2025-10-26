using WiiTUIO.Properties;

namespace WiiTUIO.Provider
{
    /// <summary>
    /// This class is responsible for transforming a 2D-coordinate on a source rectangle onto a that of a destination rectangle.
    /// The transformation is linear and will not take into account bent or curved surfaces (the transformations are affine!).
    /// This is based on the work done by Johnny Lee and can be found here: http://johnnylee.net/projects/wii/
    /// </summary>
    internal class Warper
    {
        private float[] srcX = new float[4];
        private float[] srcY = new float[4];
        private float[] dstX = new float[4];
        private float[] dstY = new float[4];

        private float[] srcMat = new float[16];
        private float[] dstMat = new float[16];
        private float[] warpMat = new float[16];
        private float[] center = new float[2];
        private bool dirty;

        public CalibrationSettings settings;

        /// <summary>
        /// Construct a new warper class.
        /// </summary>
        public Warper(CalibrationSettings settings)
        {
            this.settings = settings;
            this.settings.PropertyChanged += SettingsChanged;
            setIdentity();
        }

        public void setIdentity()
        {
            center[0] = (float)this.settings.CenterX;
            center[1] = (float)this.settings.CenterY;
            if (Settings.Default.pointer_4IRMode == "square")
                setDestination(this.settings.TRled, this.settings.OffsetYBottom,
                               this.settings.TLled, this.settings.OffsetYBottom,
                               this.settings.TLled, this.settings.OffsetYTop,
                               this.settings.TRled, this.settings.OffsetYTop);
            else if (Settings.Default.pointer_4IRMode == "diamond")
                setDestination(this.settings.Right, 0.5f,  //Right-Center 
                               0.5f, this.settings.Bottom,  //Bottom-Center Led
                               this.settings.Left, 0.5f,  //Left-Center Led 
                               0.5f, this.settings.Top); //Top-Center Led 
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Center" || e.PropertyName == "TLled" || e.PropertyName == "TRled" || e.PropertyName == "Top" || e.PropertyName == "Bottom" || e.PropertyName == "Left" || e.PropertyName == "Right")
            {
                setIdentity();
            }
        }

        /// <summary>
        /// Set the source rectangle we are transforming coordinates from.
        /// </summary>
        /// <param name="x0">Top Left X</param>
        /// <param name="y0">Top Left Y</param>
        /// <param name="x1">Top Right X</param>
        /// <param name="y1">Top Right Y</param>
        /// <param name="x2">Bottom Left X</param>
        /// <param name="y2">Bottom Left Y</param>
        /// <param name="x3">Bottom Right X</param>
        /// <param name="y3">Bottom Right Y</param>
        public void setSource(float x0, float y0,
                        float x1, float y1,
                        float x2, float y2,
                        float x3, float y3)
        {
            // Set the data.
            srcX[0] = x0;
            srcY[0] = y0;
            srcX[1] = x1;
            srcY[1] = y1;
            srcX[2] = x2;
            srcY[2] = y2;
            srcX[3] = x3;
            srcY[3] = y3;
        }

        /// <summary>
        /// Set the destination rectangle we are transforming coordinates onto.
        /// </summary>
        /// <param name="x0">Top Left X</param>
        /// <param name="y0">Top Left Y</param>
        /// <param name="x1">Top Right X</param>
        /// <param name="y1">Top Right Y</param>
        /// <param name="x2">Bottom Left X</param>
        /// <param name="y2">Bottom Left Y</param>
        /// <param name="x3">Bottom Right X</param>
        /// <param name="y3">Bottom Right Y</param>
        public void setDestination(float x0, float y0,
                        float x1, float y1,
                        float x2, float y2,
                        float x3, float y3)
        {
            // Set data.
            dstX[0] = x0;
            dstY[0] = y0;
            dstX[1] = x1;
            dstY[1] = y1;
            dstX[2] = x2;
            dstY[2] = y2;
            dstX[3] = x3;
            dstY[3] = y3;

            // Flag we need to change.
            dirty = true;
        }

        /// <summary>
        /// Compute the new 'warp' matrix based on the source and destination rectangles.
        /// </summary>
        public void computeWarp()
        {
            computeQuadToSquare(srcX[0], srcY[0], srcX[1], srcY[1], srcX[2], srcY[2], srcX[3], srcY[3], srcMat);
            computeSquareToQuad(dstX[0], dstY[0], dstX[1], dstY[1], dstX[2], dstY[2], dstX[3], dstY[3], dstMat);
            multMats(srcMat, dstMat, warpMat);

            // Remove our flag so that we are not in need of update again until something changes.
            dirty = false;
        }

        /// <summary>
        /// A helper function to multiply two matracies.
        /// </summary>
        /// <param name="srcMat">Source matrix as a 16-float flattened 4x4 matrix.</param>
        /// <param name="dstMat">Destination matrix as a 16-float flattened 4x4 matrix.</param>
        /// <param name="resMat">Result matrix as a 16-float flattened 4x4 matrix.</param>
        public void multMats(float[] srcMat, float[] dstMat, float[] resMat)
        {
            // DSTDO/CBB: could be faster, but not called often enough to matter
            for (int r = 0; r < 4; r++)
            {
                int ri = r * 4;
                for (int c = 0; c < 4; c++)
                {
                    resMat[ri + c] = (srcMat[ri] * dstMat[c] +
                              srcMat[ri + 1] * dstMat[c + 4] +
                              srcMat[ri + 2] * dstMat[c + 8] +
                              srcMat[ri + 3] * dstMat[c + 12]);
                }
            }
        }

        public void computeSquareToQuad(float x0,
                                            float y0,
                                            float x1,
                                            float y1,
                                            float x2,
                                            float y2,
                                            float x3,
                                            float y3,
                                            float[] mat)
        {

            float dx1 = x1 - x2, dy1 = y1 - y2;
            float dx2 = x3 - x2, dy2 = y3 - y2;
            float sx = x0 - x1 + x2 - x3;
            float sy = y0 - y1 + y2 - y3;
            float g = (sx * dy2 - dx2 * sy) / (dx1 * dy2 - dx2 * dy1);
            float h = (dx1 * sy - sx * dy1) / (dx1 * dy2 - dx2 * dy1);
            float a = x1 - x0 + g * x1;
            float b = x3 - x0 + h * x3;
            float c = x0;
            float d = y1 - y0 + g * y1;
            float e = y3 - y0 + h * y3;
            float f = y0;

            mat[0] = a; mat[1] = d; mat[2] = 0; mat[3] = g;
            mat[4] = b; mat[5] = e; mat[6] = 0; mat[7] = h;
            mat[8] = 0; mat[9] = 0; mat[10] = 1; mat[11] = 0;
            mat[12] = c; mat[13] = f; mat[14] = 0; mat[15] = 1;
        }

        public void computeQuadToSquare(float x0,
                                            float y0,
                                            float x1,
                                            float y1,
                                            float x2,
                                            float y2,
                                            float x3,
                                            float y3,
                                            float[] mat)
        {
            computeSquareToQuad(x0, y0, x1, y1, x2, y2, x3, y3, mat);

            // invert through adjoint

            float a = mat[0], d = mat[1],   /* ignore */        g = mat[3];
            float b = mat[4], e = mat[5],   /* 3rd col*/        h = mat[7];
            /* ignore 3rd row */
            float c = mat[12], f = mat[13];

            float A = e - f * h;
            float B = c * h - b;
            float C = b * f - c * e;
            float D = f * g - d;
            float E = a - c * g;
            float F = c * d - a * f;
            float G = d * h - e * g;
            float H = b * g - a * h;
            float I = a * e - b * d;

            // Probably unnecessary since 'I' is also scaled by the determinant,
            //   and 'I' scales the homogeneous coordinate, which, in turn,
            //   scales the X,Y coordinates.
            // Determinant  =   a * (e - f * h) + b * (f * g - d) + c * (d * h - e * g);
            float idet = 1.0f / (a * A + b * D + c * G);

            mat[0] = A * idet; mat[1] = D * idet; mat[2] = 0; mat[3] = G * idet;
            mat[4] = B * idet; mat[5] = E * idet; mat[6] = 0; mat[7] = H * idet;
            mat[8] = 0; mat[9] = 0; mat[10] = 1; mat[11] = 0;
            mat[12] = C * idet; mat[13] = F * idet; mat[14] = 0; mat[15] = I * idet;
        }

        /// <summary>
        /// Return a reference to the array which is the warp matrix.
        /// </summary>
        /// <returns>An array reference of a 4x4 matrix represented as a flattened 16-float array.</returns>
        public float[] getWarpMatrix()
        {
            return warpMat;
        }

        /// <summary>
        /// Transform a point from one rectangle onto another using this class's warp matrix.
        /// </summary>
        /// <param name="srcX">Source point, X coordinate.</param>
        /// <param name="srcY">Source point, Y coordinate.</param>
        /// <param name="dstX">Destination point, X coordinate.</param>
        /// <param name="dstY">Destination point, Y coordinate.</param>
        public float[] warp()
        {
            float[] warpedPos = new float[2];

            // If our matrix is out of date, recompute it.
            if (dirty)
            {
                computeSquareToQuad(dstX[0], dstY[0], dstX[1], dstY[1], dstX[2], dstY[2], dstX[3], dstY[3], dstMat);
                dirty = false;
            }

            computeQuadToSquare(srcX[0], srcY[0], srcX[1], srcY[1], srcX[2], srcY[2], srcX[3], srcY[3], srcMat);
            multMats(srcMat, dstMat, warpMat);

            // Compute the coordinate transform.
            float[] result = new float[4];
            float z = 0;
            result[0] = center[0] * warpMat[0] + center[1] * warpMat[4] + z * warpMat[8] + 1 * warpMat[12];
            result[1] = center[0] * warpMat[1] + center[1] * warpMat[5] + z * warpMat[9] + 1 * warpMat[13];
            result[3] = center[0] * warpMat[3] + center[1] * warpMat[7] + z * warpMat[11] + 1 * warpMat[15];

            warpedPos[0] = result[0] / result[3];
            warpedPos[1] = 1 - result[1] / result[3];
            return warpedPos;

        }
        public void setCenter(float x, float y)
        {
            center[0] = x;
            center[1] = y;
        }
    }

}
