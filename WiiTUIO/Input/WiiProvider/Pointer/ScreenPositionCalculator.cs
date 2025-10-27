using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using WiimoteLib;
using WiiTUIO.Filters;
using WiiTUIO.Properties;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Point = WiimoteLib.Point;

namespace WiiTUIO.Provider
{
    public class ScreenPositionCalculator
    {
        private int wiimoteId = 0;

        private int minXPos;
        private int maxXPos;
        private int maxWidth;

        private uint[] see = new uint[4];

        private PointF median;

        private PointF[] finalPos = new PointF[4];

        private float xDistTop;
        private float xDistBottom;
        private float yDistLeft;
        private float yDistRight;

        float angleTop;
        float angleBottom;
        float angleLeft;
        float angleRight;

        double angle;
        float height;
        float width;

        private float[] angleOffset = new float[4];

        private int minYPos;
        private int maxYPos;
        private int maxHeight;
        private int SBPositionOffset;
        private double CalcMarginOffsetY;

        private double midMarginX;
        private double midMarginY;
        private double marginBoundsX;
        private double marginBoundsY;

        private PointF topLeftPt = new PointF();
        private PointF bottomRightPt = new PointF();
        private PointF trueTopLeftPt = new PointF();
        private PointF trueBottomRightPt = new PointF();
        private double boundsX;
        private double boundsY;

        private double targetAspectRatio = 0.0;

        private double smoothedX, smoothedZ;
        private int orientation;

        private int leftPoint = -1;

        private Warper pWarper;

        private CursorPos lastPos;

        private Screen primaryScreen;

        private RadiusBuffer smoothingBuffer;
        private CoordFilter coordFilter;

        private int lastIrPoint1 = -1;
        private int lastIrPoint2 = -1;

        public CalibrationSettings settings;

        // Variables para la geometría del Rombo (Diamond)
        private float DistTR, DistBR, DistBL, DistTL; // Distancias
        private float offsetTR, offsetBR, offsetBL, offsetTL; // Offsets de ángulo
        private float angleTR, angleBR, angleBL, angleTL; // Ángulos de los lados
        private bool hasIdealGeometry = false;

        private float DistTB, DistRL;                 // Distancias de las diagonales

        // Ángulos
        private float idealAngleAtTop, idealAngleAtRight, idealAngleAtBottom, idealAngleAtLeft;

        // Puntos de referencia
        private PointF[] idealPoints = new PointF[4];


        private float NormalizeAngle(float angle)
        {
            while (angle > MathF.PI) angle -= 2 * MathF.PI;
            while (angle < -MathF.PI) angle += 2 * MathF.PI;
            return angle;
        }

        public ScreenPositionCalculator(int id, CalibrationSettings settings)
        {
            this.wiimoteId = id;
            this.settings = settings;

            this.pWarper = new Warper(this.settings);

            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            this.recalculateScreenBounds(this.primaryScreen);

            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            this.settings.PropertyChanged += SettingsChanged;

            lastPos = new CursorPos(0, 0, 0, 0, 0);

            coordFilter = new CoordFilter();
            this.smoothingBuffer = new RadiusBuffer(Settings.Default.pointer_positionSmoothing);
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "primaryMonitor")
            {
                this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
                Console.WriteLine("Setting primary monitor for screen position calculator to " + this.primaryScreen.Bounds);
                this.recalculateScreenBounds(this.primaryScreen);
            }
            
            else if (e.PropertyName == "Left" || e.PropertyName == "Right" || e.PropertyName == "Top" || e.PropertyName == "Bottom")
            {
                trueTopLeftPt.X = topLeftPt.X = this.settings.Left;
                trueTopLeftPt.Y = topLeftPt.Y = this.settings.Top;
                trueBottomRightPt.X = bottomRightPt.X = this.settings.Right;
                trueBottomRightPt.Y = bottomRightPt.Y = this.settings.Bottom;
                recalculateLightgunCoordBounds();
            }
            else if (e.PropertyName == "CalibrationMarginX" || e.PropertyName == "CalibrationMarginY")
            {
                recalculateLightgunCoordBounds();
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            recalculateScreenBounds(this.primaryScreen);
        }

        private void recalculateScreenBounds(Screen screen)
        {
            Console.WriteLine("Setting primary monitor for screen position calculator to " + this.primaryScreen.Bounds);
            minXPos = -(int)(screen.Bounds.Width * Settings.Default.pointer_marginsLeftRight);
            maxXPos = screen.Bounds.Width + (int)(screen.Bounds.Width * Settings.Default.pointer_marginsLeftRight);
            maxWidth = maxXPos - minXPos;
            minYPos = -(int)(screen.Bounds.Height * Settings.Default.pointer_marginsTopBottom);
            maxYPos = screen.Bounds.Height + (int)(screen.Bounds.Height * Settings.Default.pointer_marginsTopBottom);
            maxHeight = maxYPos - minYPos;
            SBPositionOffset = (int)(screen.Bounds.Height * Settings.Default.pointer_sensorBarPosCompensation);
            CalcMarginOffsetY = Settings.Default.pointer_sensorBarPosCompensation;

            midMarginX = Settings.Default.pointer_marginsLeftRight * 0.5;
            midMarginY = Settings.Default.pointer_marginsTopBottom * 0.5;
            marginBoundsX = 1 / (1 - Settings.Default.pointer_marginsLeftRight);
            marginBoundsY = 1 / (1 - Settings.Default.pointer_marginsTopBottom);
            
            trueTopLeftPt.X = topLeftPt.X = this.settings.Left;
            trueTopLeftPt.Y = topLeftPt.Y = this.settings.Top;
            trueBottomRightPt.X = bottomRightPt.X = this.settings.Right;
            trueBottomRightPt.Y = bottomRightPt.Y = this.settings.Bottom;
            
            if (Settings.Default.pointer_4IRMode == "diamond")
            {
                // 1. Vértices del rombo ideal en coordenadas normalizadas (0.0 a 1.0)
                PointF pTop = new PointF { X = 0.5f, Y = this.settings.Top };
                PointF pRight = new PointF { X = this.settings.Right, Y = 0.5f };
                PointF pBottom = new PointF { X = 0.5f, Y = this.settings.Bottom };
                PointF pLeft = new PointF { X = this.settings.Left, Y = 0.5f };

                // 2. Guardamos las 4 posiciones "perfectas"
                idealPoints[0] = pTop;
                idealPoints[1] = pRight;
                idealPoints[2] = pBottom;
                idealPoints[3] = pLeft;

                // 3. Calculamos y guardamos las distancias ideales de lados y diagonales
                DistTR = MathF.Hypot(pTop.Y - pRight.Y, pTop.X - pRight.X);
                DistBR = MathF.Hypot(pRight.Y - pBottom.Y, pRight.X - pBottom.X);
                DistBL = MathF.Hypot(pBottom.Y - pLeft.Y, pLeft.X - pBottom.X);
                DistTL = MathF.Hypot(pLeft.Y - pTop.Y, pLeft.X - pTop.X);
                DistTB = MathF.Hypot(pTop.Y - pBottom.Y, pTop.X - pBottom.X);
                DistRL = MathF.Hypot(pRight.Y - pLeft.Y, pRight.X - pLeft.X);

                // 4. Pre-calculamos y normalizamos los ángulos internos del rombo ideal
                float vec_TL_angle = MathF.Atan2(pLeft.Y - pTop.Y, pLeft.X - pTop.X);
                float vec_TR_angle = MathF.Atan2(pRight.Y - pTop.Y, pRight.X - pTop.X);
                idealAngleAtTop = NormalizeAngle(vec_TR_angle - vec_TL_angle);

                float vec_RT_angle = MathF.Atan2(pTop.Y - pRight.Y, pTop.X - pRight.X);
                float vec_RB_angle = MathF.Atan2(pBottom.Y - pRight.Y, pBottom.X - pRight.X);
                idealAngleAtRight = NormalizeAngle(vec_RB_angle - vec_RT_angle);

                float vec_BL_angle = MathF.Atan2(pLeft.Y - pBottom.Y, pLeft.X - pBottom.X);
                float vec_BR_angle = MathF.Atan2(pRight.Y - pBottom.Y, pRight.X - pBottom.X);
                idealAngleAtBottom = NormalizeAngle(vec_BL_angle - vec_BR_angle);

                float vec_LT_angle = MathF.Atan2(pTop.Y - pLeft.Y, pTop.X - pLeft.X);
                float vec_LB_angle = MathF.Atan2(pBottom.Y - pLeft.Y, pBottom.X - pLeft.X);
                idealAngleAtLeft = NormalizeAngle(vec_LT_angle - vec_LB_angle);

                hasIdealGeometry = true;
            } 

            if (targetAspectRatio == 0.0)
            {
                recalculateLightgunCoordBounds();
            }
            else
            {
                RecalculateLightgunAspect(targetAspectRatio);
            }
        }

        private void recalculateLightgunCoordBounds()
        {
            
            if (Settings.Default.pointer_4IRMode == "diamond")
            {
                double calibratedWidth = Math.Abs(this.settings.Right - this.settings.Left);
                double calibratedHeight = Math.Abs(this.settings.Bottom - this.settings.Top);

                boundsX = (Math.Abs(this.settings.Right - this.settings.Left) > double.Epsilon) ? 1.0 / (this.settings.Right - this.settings.Left) : 0;
                boundsY = (Math.Abs(this.settings.Bottom - this.settings.Top) > double.Epsilon) ? 1.0 / (this.settings.Bottom - this.settings.Top) : 0;


            }
            /*else if (Settings.Default.pointer_4IRMode == "none")
            {
                boundsX = (1 - Settings.Default.CalibrationMarginX * 2) / (bottomRightPt.X - topLeftPt.X);
                boundsY = (1 - Settings.Default.CalibrationMarginY * 2) / (bottomRightPt.Y - topLeftPt.Y);
            }*/
            else if (Settings.Default.pointer_4IRMode == "square" || Settings.Default.pointer_4IRMode == "none")
            {
                boundsX = 1 / (bottomRightPt.X - topLeftPt.X);
                boundsY = 1 / (bottomRightPt.Y - topLeftPt.Y);
                //boundsX = (Math.Abs(bottomRightPt.X - topLeftPt.X) > double.Epsilon) ? 1.0 / (bottomRightPt.X - topLeftPt.X) : 0;
                //boundsY = (Math.Abs(bottomRightPt.Y - topLeftPt.Y) > double.Epsilon) ? 1.0 / (bottomRightPt.Y - topLeftPt.Y) : 0;
            }
        }

        public CursorPos CalculateCursorPos(WiimoteState wiimoteState)
        {
            bool reconstructionSuccess = false;
            int x = 0;
            int y = 0;
            double marginX, marginY = 0.0;
            double lightbarX = 0.0;
            double lightbarY = 0.0;
            int offsetY = 0;
            double marginOffsetY = 0.0;
            PointF resultPos = new PointF();

            IRState irState = wiimoteState.IRState;

            if (Settings.Default.pointer_4IRMode == "none")
            {
                int irPoint1 = 0;
                int irPoint2 = 0;
                bool foundMidpoint = false;
                // First check if previously found points are still detected.
                // Prefer those points first
                if (lastIrPoint1 != -1 && lastIrPoint2 != -1)
                {
                    if (irState.IRSensors[lastIrPoint1].Found &&
                        irState.IRSensors[lastIrPoint2].Found)
                    {
                        foundMidpoint = true;
                        irPoint1 = lastIrPoint1;
                        irPoint2 = lastIrPoint2;
                    }
                }

                // If no midpoint found from previous points, check all available
                // IR points for a possible midpoint
                for (int i = 0; !foundMidpoint && i < irState.IRSensors.Count(); i++)
                {
                    if (irState.IRSensors[i].Found)
                    {
                        for (int j = i + 1; j < irState.IRSensors.Count() && !foundMidpoint; j++)
                        {
                            if (irState.IRSensors[j].Found)
                            {
                                foundMidpoint = true;

                                irPoint1 = i;
                                irPoint2 = j;
                            }
                        }
                    }
                }

                if (foundMidpoint)
                {
                    int i = irPoint1;
                    int j = irPoint2;
                    median.X = (irState.IRSensors[i].Position.X + irState.IRSensors[j].Position.X) / 2.0f;
                    median.Y = (irState.IRSensors[i].Position.Y + irState.IRSensors[j].Position.Y) / 2.0f;

                    smoothedX = smoothedX * 0.9f + wiimoteState.AccelState.RawValues.X * 0.1f;
                    smoothedZ = smoothedZ * 0.9f + wiimoteState.AccelState.RawValues.Z * 0.1f;

                    int l = leftPoint, r;
                    if (leftPoint == -1)
                    {
                        double absx = Math.Abs(smoothedX - 128), absz = Math.Abs(smoothedZ - 128);

                        if (orientation == 0 || orientation == 2) absx -= 5;
                        if (orientation == 1 || orientation == 3) absz -= 5;

                        if (absz >= absx)
                        {
                            if (absz > 5)
                                orientation = (smoothedZ > 128) ? 0 : 2;
                        }
                        else
                        {
                            if (absx > 5)
                                orientation = (smoothedX > 128) ? 3 : 1;
                        }

                        switch (orientation)
                        {
                            case 0: l = (irState.IRSensors[i].RawPosition.X < irState.IRSensors[j].RawPosition.X) ? i : j; break;
                            case 1: l = (irState.IRSensors[i].RawPosition.Y > irState.IRSensors[j].RawPosition.Y) ? i : j; break;
                            case 2: l = (irState.IRSensors[i].RawPosition.X > irState.IRSensors[j].RawPosition.X) ? i : j; break;
                            case 3: l = (irState.IRSensors[i].RawPosition.Y < irState.IRSensors[j].RawPosition.Y) ? i : j; break;
                        }
                    }
                    leftPoint = l;
                    r = l == i ? j : i;

                    double dx = irState.IRSensors[r].RawPosition.X - irState.IRSensors[l].RawPosition.X;
                    double dy = irState.IRSensors[r].RawPosition.Y - irState.IRSensors[l].RawPosition.Y;

                    double d = Math.Sqrt(dx * dx + dy * dy);

                    dx /= d;
                    dy /= d;

                    angle = Math.Atan2(dy, dx);


                    median.X = median.X - 0.5F;
                    median.Y = median.Y - 0.5F;

                    median = this.rotatePoint(median, angle);

                    median.X = median.X + 0.5F;
                    median.Y = median.Y + 0.5F;

                    lastIrPoint1 = irPoint1;
                    lastIrPoint2 = irPoint2;
                }
                else if (!foundMidpoint)
                {
                    CursorPos err = lastPos;
                    err.OutOfReach = true;
                    err.OffScreen = true;
                    leftPoint = -1;
                    lastIrPoint1 = -1;
                    lastIrPoint2 = -1;

                    return err;
                }

                if (Properties.Settings.Default.pointer_sensorBarPos == "top")
                {
                    offsetY = -SBPositionOffset;
                    marginOffsetY = CalcMarginOffsetY;
                }
                else if (Properties.Settings.Default.pointer_sensorBarPos == "bottom")
                {
                    offsetY = SBPositionOffset;
                    marginOffsetY = -CalcMarginOffsetY;
                }
                if (Settings.Default.Debug)
                {
                    PointF[] debugDisplayPos = new PointF[4];
                    var sensorPointsDict = new Dictionary<int, WiimoteLib.PointF>();

                    // 2. Copiar los datos invirtiendo el eje X.
                    for (int i = 0; i < 4; i++)
                    {
                        debugDisplayPos[i] = new PointF
                        {
                            // Invertimos el eje X (asumiendo coordenadas normalizadas de 0.0 a 1.0)
                            X = 1.0f - finalPos[i].X,
                            // El eje Y se mantiene igual.
                            Y = finalPos[i].Y
                        };
                        
                            // ...lo añadimos al diccionario para dibujarlo.
                            sensorPointsDict.Add(i, finalPos[i]);
                        
                    }

                    // 3. Dibujar el rombo de debug con la copia invertida.
                    //DebugVisualizer.DrawRhombus(debugDisplayPos);
                    DebugVisualizer.DrawSensorView(sensorPointsDict);
                }
                resultPos = median;
            }
            else if (Settings.Default.pointer_4IRMode == "diamond")
            {
                byte seenFlags = 0;
                int foundCount = 0;
                var visiblePoints = new List<PointF>();

                // --- FASE 1: IDENTIFICACIÓN DE PUNTOS ---
                for (int i = 0; i < 4; i++)
                {
                    if (irState.IRSensors[i].Found)
                    {
                        visiblePoints.Add(irState.IRSensors[i].Position);
                        foundCount++;
                    }
                }

                if (foundCount >= 3)
                {
                    double Roll = Math.Atan2(wiimoteState.AccelState.Values.X, wiimoteState.AccelState.Values.Z);

                    median = new PointF();
                    foreach (var p in visiblePoints) { median.X += p.X; median.Y += p.Y; }
                    median.X /= foundCount;
                    median.Y /= foundCount;
                    foreach (var p in visiblePoints)
                    {
                        double point_angle = Math.Atan2(p.Y - median.Y, p.X - median.X) - Roll;
                        point_angle += (MathF.PI / 4);
                        if (point_angle < 0) point_angle += 2 * MathF.PI;
                        if (point_angle > 2 * MathF.PI) point_angle -= 2 * MathF.PI;
                        int index = (int)(point_angle / (MathF.PI / 2));
                        int finalIndex = 0;
                        switch (index)
                        {
                            case 0: finalIndex = 1; break; // Right
                            case 1: finalIndex = 2; break; // Bottom
                            case 2: finalIndex = 3; break; // Left
                            case 3: finalIndex = 0; break; // Top
                        }
                        finalPos[finalIndex] = p;
                        seenFlags |= (byte)(1 << finalIndex);
                    }
                }
                else if (foundCount == 2)
                {
                    // --- Identificación Stateful (CON MEMORIA) usando el último fotograma conocido ---
                    if (lastPos.OnScreenPoints != null && (lastPos.OnScreenPoints[0].X != 0 || lastPos.OnScreenPoints[0].Y != 0))
                    {
                        PointF p1 = visiblePoints[0];
                        PointF p2 = visiblePoints[1];

                        // Buscamos cuál de los 4 puntos ANTERIORES está más cerca de p1
                        int bestIdx_p1 = -1;
                        float minDist_p1 = float.MaxValue;
                        for (int i = 0; i < 4; i++)
                        {
                            float dist = MathF.Hypot(p1.Y - lastPos.OnScreenPoints[i].Y, p1.X - lastPos.OnScreenPoints[i].X);
                            if (dist < minDist_p1)
                            {
                                minDist_p1 = dist;
                                bestIdx_p1 = i;
                            }
                        }

                        // Buscamos cuál de los 4 puntos ANTERIORES está más cerca de p2
                        int bestIdx_p2 = -1;
                        float minDist_p2 = float.MaxValue;
                        for (int i = 0; i < 4; i++)
                        {
                            // Nos aseguramos de no asignar el mismo punto anterior a ambos puntos actuales
                            if (i == bestIdx_p1) continue;
                            float dist = MathF.Hypot(p2.Y - lastPos.OnScreenPoints[i].Y, p2.X - lastPos.OnScreenPoints[i].X);
                            if (dist < minDist_p2)
                            {
                                minDist_p2 = dist;
                                bestIdx_p2 = i;
                            }
                        }

                        // Si hemos encontrado una correspondencia válida, la usamos
                        if (bestIdx_p1 != -1 && bestIdx_p2 != -1)
                        {
                            // Limpiamos finalPos para no arrastrar datos viejos
                            Array.Clear(finalPos, 0, finalPos.Length);

                            // Asignamos los puntos visibles a sus posiciones correctas en finalPos
                            finalPos[bestIdx_p1] = p1;
                            finalPos[bestIdx_p2] = p2;
                            seenFlags = (byte)((1 << bestIdx_p1) | (1 << bestIdx_p2));
                        }
                    }
                    else
                    {
                        // Si no hay fotograma anterior, nos la jugamos con la identificación stateless original como último recurso.
                        // (Este bloque es el código que estás eliminando, pegado aquí como fallback)
                        PointF p1 = visiblePoints[0];
                        PointF p2 = visiblePoints[1];
                        int bestIdx_p1 = -1, bestIdx_p2 = -1;
                        float minDist_p1 = float.MaxValue, minDist_p2 = float.MaxValue;
                        for (int i = 0; i < 4; i++) { float dist = MathF.Hypot(p1.Y - idealPoints[i].Y, p1.X - idealPoints[i].X); if (dist < minDist_p1) { minDist_p1 = dist; bestIdx_p1 = i; } }
                        for (int i = 0; i < 4; i++) { if (i == bestIdx_p1) continue; float dist = MathF.Hypot(p2.Y - idealPoints[i].Y, p2.X - idealPoints[i].X); if (dist < minDist_p2) { minDist_p2 = dist; bestIdx_p2 = i; } }
                        if (bestIdx_p1 != -1 && bestIdx_p2 != -1) { finalPos[bestIdx_p1] = p1; finalPos[bestIdx_p2] = p2; seenFlags = (byte)((1 << bestIdx_p1) | (1 << bestIdx_p2)); }
                    }
                }

                // --- FASE 2: RECONSTRUCCIÓN ---
                if (hasIdealGeometry && (foundCount == 3 || (foundCount == 2 && seenFlags != 0)))
                {
                    if (foundCount == 3)
                    {
                        // Tu lógica perfecta para 3 puntos
                        int missingIdx = -1;
                        for (int i = 0; i < 4; i++) { if ((seenFlags & (1 << i)) == 0) { missingIdx = i; break; } }
                        int prevIdx = (missingIdx + 3) % 4;
                        int nextIdx = (missingIdx + 1) % 4;
                        int oppositeIdx = (missingIdx + 2) % 4;
                        PointF center = new PointF { X = (finalPos[prevIdx].X + finalPos[nextIdx].X) / 2, Y = (finalPos[prevIdx].Y + finalPos[nextIdx].Y) / 2 };
                        finalPos[missingIdx] = new PointF { X = 2 * center.X - finalPos[oppositeIdx].X, Y = 2 * center.Y - finalPos[oppositeIdx].Y };

                        seenFlags = 0x0F;
                    }
                    else if (foundCount == 2)
                    {
                        // Lógica de vectores escalados (la que mantiene la forma correcta)
                        int idx1 = -1, idx2 = -1;
                        for (int i = 0; i < 4; i++) { if ((seenFlags & (1 << i)) != 0) { if (idx1 == -1) idx1 = i; else idx2 = i; } }

                        bool isAdjacent = ((idx1 + 1) % 4 == idx2 || (idx2 + 1) % 4 == idx1);
                        if (isAdjacent)
                        {
                            // --- PASO A: Reconstrucción por Vectores (Tu código, no cambia) ---
                            if (seenFlags == 3)
                            { // Vemos Arriba(0) y Derecha(1)
                                PointF pTop = finalPos[0], pRight = finalPos[1];
                                float current_DistTR = MathF.Hypot(pRight.Y - pTop.Y, pRight.X - pTop.X);
                                float scale = (DistTR > 0) ? current_DistTR / DistTR : 1.0f;
                                float angle_vec_TR = MathF.Atan2(pRight.Y - pTop.Y, pRight.X - pTop.X);
                                float angle_vec_TL = NormalizeAngle(angle_vec_TR - idealAngleAtTop);
                                PointF pLeft = new PointF { X = pTop.X + (DistTL * scale) * MathF.Cos(angle_vec_TL), Y = pTop.Y + (DistTL * scale) * MathF.Sin(angle_vec_TL) };
                                PointF vec_TL = new PointF { X = pLeft.X - pTop.X, Y = pLeft.Y - pTop.Y };
                                finalPos[3] = pLeft;
                                finalPos[2] = new PointF { X = pRight.X + vec_TL.X, Y = pRight.Y + vec_TL.Y };
                            }
                            else if (seenFlags == 6)
                            { // Vemos Derecha(1) y Abajo(2)
                                PointF pRight = finalPos[1], pBottom = finalPos[2];
                                float current_DistRB = MathF.Hypot(pBottom.Y - pRight.Y, pBottom.X - pRight.X);
                                float scale = (DistBR > 0) ? current_DistRB / DistBR : 1.0f;
                                float angle_vec_RB = MathF.Atan2(pBottom.Y - pRight.Y, pBottom.X - pRight.X);
                                float angle_vec_RT = NormalizeAngle(angle_vec_RB - idealAngleAtRight);
                                PointF pTop = new PointF { X = pRight.X + (DistTR * scale) * MathF.Cos(angle_vec_RT), Y = pRight.Y + (DistTR * scale) * MathF.Sin(angle_vec_RT) };
                                PointF vec_RT = new PointF { X = pTop.X - pRight.X, Y = pTop.Y - pRight.Y };
                                finalPos[0] = pTop;
                                finalPos[3] = new PointF { X = pBottom.X + vec_RT.X, Y = pBottom.Y + vec_RT.Y };
                            }
                            else if (seenFlags == 12)
                            { // Vemos Abajo(2) e Izquierda(3)
                                PointF pBottom = finalPos[2], pLeft = finalPos[3];
                                float current_DistBL = MathF.Hypot(pLeft.Y - pBottom.Y, pLeft.X - pBottom.X);
                                float scale = (DistBL > 0) ? current_DistBL / DistBL : 1.0f;
                                float angle_vec_BL = MathF.Atan2(pLeft.Y - pBottom.Y, pLeft.X - pBottom.X);
                                float angle_vec_BR = NormalizeAngle(angle_vec_BL - idealAngleAtBottom);
                                PointF pRight = new PointF { X = pBottom.X + (DistBR * scale) * MathF.Cos(angle_vec_BR), Y = pBottom.Y + (DistBR * scale) * MathF.Sin(angle_vec_BR) };
                                PointF vec_BR = new PointF { X = pRight.X - pBottom.X, Y = pRight.Y - pBottom.Y };
                                finalPos[1] = pRight;
                                finalPos[0] = new PointF { X = pLeft.X + vec_BR.X, Y = pLeft.Y + vec_BR.Y };
                            }
                            else if (seenFlags == 9)
                            { // Vemos Izquierda(3) y Arriba(0)
                                PointF pLeft = finalPos[3], pTop = finalPos[0];
                                float current_DistLT = MathF.Hypot(pTop.Y - pLeft.Y, pTop.X - pLeft.X);
                                float scale = (DistTL > 0) ? current_DistLT / DistTL : 1.0f;
                                float angle_vec_LT = MathF.Atan2(pTop.Y - pLeft.Y, pTop.X - pLeft.X);
                                float angle_vec_LB = NormalizeAngle(angle_vec_LT - idealAngleAtLeft);
                                PointF pBottom = new PointF { X = pLeft.X + (DistBL * scale) * MathF.Cos(angle_vec_LB), Y = pLeft.Y + (DistBL * scale) * MathF.Sin(angle_vec_LB) };
                                PointF vec_LB = new PointF { X = pBottom.X - pLeft.X, Y = pBottom.Y - pLeft.Y };
                                finalPos[2] = pBottom;
                                finalPos[1] = new PointF { X = pTop.X + vec_LB.X, Y = pTop.Y + vec_LB.Y };
                            }

                            // --- PASO B: Corrección de Posición por Extrapolación (ARREGLO FINAL) ---
                            // Solo si tenemos una referencia válida del fotograma anterior.
                            if (lastPos.OnScreenPoints != null && (lastPos.OnScreenPoints[0].X != 0 || lastPos.OnScreenPoints[0].Y != 0))
                            {
                                // 1. Calculamos el centro del rombo del fotograma anterior.
                                PointF previousCenter = new PointF { X = (lastPos.OnScreenPoints[0].X + lastPos.OnScreenPoints[2].X) / 2f, Y = (lastPos.OnScreenPoints[0].Y + lastPos.OnScreenPoints[2].Y) / 2f };

                                // 2. Calculamos cómo se ha movido el centro de los dos puntos que SÍ vemos.
                                PointF p1_visible = finalPos[idx1];
                                PointF p2_visible = finalPos[idx2];
                                PointF last_p1_visible = lastPos.OnScreenPoints[idx1];
                                PointF last_p2_visible = lastPos.OnScreenPoints[idx2];

                                PointF currentMidpoint = new PointF { X = (p1_visible.X + p2_visible.X) / 2f, Y = (p1_visible.Y + p2_visible.Y) / 2f };
                                PointF previousMidpoint = new PointF { X = (last_p1_visible.X + last_p2_visible.X) / 2f, Y = (last_p1_visible.Y + last_p2_visible.Y) / 2f };

                                PointF delta = new PointF { X = currentMidpoint.X - previousMidpoint.X, Y = currentMidpoint.Y - previousMidpoint.Y };

                                // 3. Extrapolamos la nueva posición del centro del rombo completo.
                                PointF predictedCenter = new PointF { X = previousCenter.X + delta.X, Y = previousCenter.Y + delta.Y };

                                // 4. Calculamos el "empujón" necesario para alinear el rombo reconstruido con el centro extrapolado.
                                PointF reconstructedCenter = new PointF { X = (finalPos[0].X + finalPos[2].X) / 2f, Y = (finalPos[0].Y + finalPos[2].Y) / 2f };
                                PointF nudge = new PointF { X = predictedCenter.X - reconstructedCenter.X, Y = predictedCenter.Y - reconstructedCenter.Y };

                                // 5. Aplicamos el empujón a los 4 puntos para eliminar el salto y el anclaje.
                                for (int i = 0; i < 4; i++)
                                {
                                    finalPos[i].X += nudge.X;
                                    finalPos[i].Y += nudge.Y;
                                }
                            }
                            seenFlags = 0x0F;
                        }
                    }
                }
                // 3. APRENDIZAJE CONTINUO (Lógica OpenFIRE)
                if (hasIdealGeometry && seenFlags != 0)
                {
                    const uint STABILITY_CHECK = (1 << 5);
                    for (int i = 0; i < 4; i++)
                    {
                        if ((seenFlags & (1 << i)) != 0) { see[i] = (see[i] << 1) | 1; }
                        else { see[i] = 0; }
                    }
                    if ((seenFlags & 0x0F) == 0x0F)
                    {
                        angleTR = MathF.Atan2(finalPos[0].Y - finalPos[1].Y, finalPos[1].X - finalPos[0].X);
                        angleBR = MathF.Atan2(finalPos[1].Y - finalPos[2].Y, finalPos[2].X - finalPos[1].X);
                        angleBL = MathF.Atan2(finalPos[2].Y - finalPos[3].Y, finalPos[3].X - finalPos[2].X);
                        angleTL = MathF.Atan2(finalPos[3].Y - finalPos[0].Y, finalPos[0].X - finalPos[3].X);
                        double current_global_angle = MathF.Atan2(finalPos[3].Y - finalPos[1].Y, finalPos[1].X - finalPos[3].X);
                        offsetTR = NormalizeAngle(angleTR - (float)current_global_angle);
                        offsetBR = NormalizeAngle(angleBR - (float)current_global_angle);
                        offsetBL = NormalizeAngle(angleBL - (float)current_global_angle);
                        offsetTL = NormalizeAngle(angleTL - (float)current_global_angle);
                    }
                    if ((see[0] & see[1] & STABILITY_CHECK) == STABILITY_CHECK)
                    {
                        angle = MathF.Atan2(finalPos[0].Y - finalPos[1].Y, finalPos[1].X - finalPos[0].X) - offsetTR;
                    }
                    else if ((see[1] & see[2] & STABILITY_CHECK) == STABILITY_CHECK)
                    {
                        angle = MathF.Atan2(finalPos[1].Y - finalPos[2].Y, finalPos[2].X - finalPos[1].X) - offsetBR;
                    }
                    else if ((see[2] & see[3] & STABILITY_CHECK) == STABILITY_CHECK)
                    {
                        angle = MathF.Atan2(finalPos[2].Y - finalPos[3].Y, finalPos[3].X - finalPos[2].X) - offsetBL;
                    }
                    else if ((see[3] & see[0] & STABILITY_CHECK) == STABILITY_CHECK)
                    {
                        angle = MathF.Atan2(finalPos[3].Y - finalPos[0].Y, finalPos[0].X - finalPos[3].X) - offsetTL;
                    }
                }
                // --- FASE 3: WARPER Y SALIDA ---
                if ((seenFlags & 0x0F) == 0x0F)
                {
                    reconstructionSuccess = true;

                    if (Settings.Default.Debug)
                    {
                        PointF[] debugDisplayPos = new PointF[4];
                        var sensorPointsDict = new Dictionary<int, WiimoteLib.PointF>();

                        // 2. Copiar los datos invirtiendo el eje X.
                        for (int i = 0; i < 4; i++)
                        {
                            debugDisplayPos[i] = new PointF
                            {
                                // Invertimos el eje X (asumiendo coordenadas normalizadas de 0.0 a 1.0)
                                X = 1.0f - finalPos[i].X,
                                // El eje Y se mantiene igual.
                                Y = finalPos[i].Y
                            };
                            // Si el punto 'i' ha sido visto o reconstruido en este frame...
                            if ((seenFlags & (1 << i)) != 0)
                            {
                                // ...lo añadimos al diccionario para dibujarlo.
                                sensorPointsDict.Add(i, finalPos[i]);
                            }
                        }

                        // 3. Dibujar el rombo de debug con la copia invertida.
                        //DebugVisualizer.DrawRhombus(debugDisplayPos);
                        DebugVisualizer.DrawSensorView(sensorPointsDict);
                    }
                    width = MathF.Hypot(finalPos[1].Y - finalPos[3].Y, finalPos[1].X - finalPos[3].X);
                    height = MathF.Hypot(finalPos[2].Y - finalPos[0].Y, finalPos[2].X - finalPos[0].X);


                    pWarper.setSource(finalPos[1].X, finalPos[1].Y, finalPos[2].X, finalPos[2].Y, finalPos[3].X, finalPos[3].Y, finalPos[0].X, finalPos[0].Y);
                    float[] fWarped = pWarper.warp();
                    resultPos.X = fWarped[0];
                    resultPos.Y = fWarped[1];
                    if (double.IsNaN(resultPos.X) || double.IsNaN(resultPos.Y))
                    {
                        CursorPos err = lastPos;
                        err.OutOfReach = true;
                        err.OffScreen = true;

                        return err;
                    }
                }
                else
                {
                    CursorPos err = lastPos;
                    err.OutOfReach = true;
                    err.OffScreen = true;

                    return err;
                }
            }
            else if (Settings.Default.pointer_4IRMode == "square")
            {
                byte seenFlags = 0;
                double Roll = Math.Atan2(wiimoteState.AccelState.Values.X, wiimoteState.AccelState.Values.Z);

                for (int i = 0; i < 4; i++)
                {
                    if (irState.IRSensors[i].Found)
                    {
                        double point_angle = Math.Atan2(irState.IRSensors[i].Position.Y - median.Y, irState.IRSensors[i].Position.X - median.X) - Roll;
                        if (point_angle < 0) point_angle += 2 * Math.PI;

                        int index = (int)(point_angle / (Math.PI / 2));

                        finalPos[index] = irState.IRSensors[i].Position;
                        see[index] = (see[index] << 1) | 1;
                        seenFlags |= (byte)(1 << index);
                    }
                    else
                        see[i] = 0;
                }
                if (Settings.Default.Debug)
                {
                    var sensorPointsDict = new Dictionary<int, WiimoteLib.PointF>();
                    for (int i = 0; i < 4; i++)
                    {
                        // Si el punto 'i' ha sido visto o reconstruido en este frame...
                        if ((seenFlags & (1 << i)) != 0)
                        {
                            // ...lo añadimos al diccionario para dibujarlo.
                            sensorPointsDict.Add(i, finalPos[i]);
                        }
                    }
                    DebugVisualizer.DrawSensorView(sensorPointsDict);
                }
                while ((seenFlags & 15) != 0 && (seenFlags & 15) != 15)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if ((seenFlags & (1 << i)) == 0)
                        {
                            see[i] = 0;
                            int[] neighbors;
                            switch (i)
                            {
                                case 0:
                                    neighbors = new[] { 3, 1 };
                                    break;
                                case 1:
                                    neighbors = new[] { 2, 0 };
                                    break;
                                case 2:
                                    neighbors = new[] { 1, 3 };
                                    break;
                                case 3:
                                    neighbors = new[] { 0, 2 };
                                    break;
                                default:
                                    neighbors = Array.Empty<int>();
                                    break;
                            }

                            foreach (int neighbor in neighbors)
                            {
                                float f = 0;
                                if ((seenFlags & (1 << neighbor)) != 0) // Check if the bit for the neighbor is set
                                {
                                    switch (i)
                                    {
                                        case 0:
                                            f = angleBottom - angleOffset[neighbor];
                                            break;
                                        case 1:
                                            f = angleBottom + (angleOffset[neighbor] - MathF.PI);
                                            break;
                                        case 2:
                                            f = angleTop + angleOffset[neighbor];
                                            break;
                                        case 3:
                                            f = angleTop - (angleOffset[neighbor] - MathF.PI);
                                            break;
                                    }
                                }

                                float distance = 0;
                                switch (i)
                                {
                                    case 0:
                                        distance = (neighbor == 3) ? yDistRight : xDistBottom;
                                        break;
                                    case 1:
                                        distance = (neighbor == 2) ? yDistLeft : xDistBottom;
                                        break;
                                    case 2:
                                        distance = (neighbor == 1) ? yDistLeft : xDistTop;
                                        break;
                                    case 3:
                                        distance = (neighbor == 0) ? yDistRight : xDistTop;
                                        break;
                                }

                                finalPos[i].X = finalPos[neighbor].X + distance * MathF.Cos(f);
                                finalPos[i].Y = finalPos[neighbor].Y + distance * -MathF.Sin(f);
                                seenFlags |= (byte)(1 << i);
                                break;
                            }
                        }
                    }
                    if ((seenFlags & 15) == 15) break;

                }

                pWarper.setSource(finalPos[0].X, finalPos[0].Y, finalPos[1].X, finalPos[1].Y, finalPos[2].X, finalPos[2].Y, finalPos[3].X, finalPos[3].Y);
                float[] fWarped = pWarper.warp();
                resultPos.X = fWarped[0];
                resultPos.Y = fWarped[1];

                if (irState.IRSensors[0].Found == true && irState.IRSensors[1].Found == true && irState.IRSensors[2].Found == true && irState.IRSensors[3].Found == true)
                {
                    median.Y = (irState.IRSensors[0].Position.Y + irState.IRSensors[1].Position.Y + irState.IRSensors[2].Position.Y + irState.IRSensors[3].Position.Y + 0.002f) / 4;
                    median.X = (irState.IRSensors[0].Position.X + irState.IRSensors[1].Position.X + irState.IRSensors[2].Position.X + irState.IRSensors[3].Position.X + 0.002f) / 4;
                }
                else
                {
                    median.Y = (finalPos[0].Y + finalPos[1].Y + finalPos[2].Y + finalPos[3].Y + 0.002f) / 4;
                    median.X = (finalPos[0].X + finalPos[1].X + finalPos[2].X + finalPos[3].X + 0.002f) / 4;
                }

                // If 4 LEDS can be seen and loop has run through 5 times update offsets and height      
                if (((1 << 5) & see[0] & see[1] & see[2] & see[3]) != 0)
                {
                    angleOffset[0] = angleTop - (angleLeft - MathF.PI);
                    angleOffset[1] = -(angleTop - angleRight);
                    angleOffset[2] = -(angleBottom - angleLeft);
                    angleOffset[3] = angleBottom - (angleRight - MathF.PI);
                    height = (yDistLeft + yDistRight) / 2.0f;
                    width = (xDistTop + xDistBottom) / 2.0f;
                }

                // If 2 LEDS can be seen and loop has run through 5 times update angle and distances
                if (((1 << 5) & see[2] & see[1]) != 0)
                {
                    angleLeft = MathF.Atan2(finalPos[1].Y - finalPos[2].Y, finalPos[2].X - finalPos[1].X);
                    yDistLeft = MathF.Hypot((finalPos[2].Y - finalPos[1].Y), (finalPos[2].X - finalPos[1].X));
                }

                if (((1 << 5) & see[0] & see[3]) != 0)
                {
                    angleRight = MathF.Atan2(finalPos[0].Y - finalPos[3].Y, finalPos[3].X - finalPos[0].X);
                    yDistRight = MathF.Hypot((finalPos[0].Y - finalPos[3].Y), (finalPos[0].X - finalPos[3].X));
                }

                if (((1 << 5) & see[2] & see[3]) != 0)
                {
                    angleTop = MathF.Atan2(finalPos[2].Y - finalPos[3].Y, finalPos[3].X - finalPos[2].X);
                    xDistTop = MathF.Hypot((finalPos[2].Y - finalPos[3].Y), (finalPos[2].X - finalPos[3].X));
                }

                if (((1 << 5) & see[0] & see[1]) != 0)
                {
                    angleBottom = MathF.Atan2(finalPos[1].Y - finalPos[0].Y, finalPos[0].X - finalPos[1].X);
                    xDistBottom = MathF.Hypot((finalPos[1].Y - finalPos[0].Y), (finalPos[1].X - finalPos[0].X));
                }


                // Add tilt correction
                angle = -(MathF.Atan2(finalPos[0].Y - finalPos[1].Y, finalPos[1].X - finalPos[0].X) + MathF.Atan2(finalPos[2].Y - finalPos[3].Y, finalPos[3].X - finalPos[2].X)) / 2;
                if (angle < 0) angle += MathF.PI * 2;

                if (see.Count(seen => seen == 0) >= 3 || Double.IsNaN(resultPos.X) || Double.IsNaN(resultPos.Y))
                {
                    CursorPos err = lastPos;
                    err.OutOfReach = true;
                    err.OffScreen = true;

                    return err;
                }
            }

            if (Settings.Default.pointer_4IRMode == "diamond")
            {
                x = Convert.ToInt32((float)maxWidth * (resultPos.X) + minXPos);
                y = Convert.ToInt32((float)maxHeight * resultPos.Y + minYPos) + offsetY;
            }
            else
            {
                x = Convert.ToInt32((float)maxWidth * (1 - resultPos.X) + minXPos);
                y = Convert.ToInt32((float)maxHeight * resultPos.Y + minYPos) + offsetY;
            }
            marginX = Math.Min(1.0, Math.Max(0.0, (1 - resultPos.X - midMarginX) * marginBoundsX));
            marginY = Math.Min(1.0, Math.Max(0.0, (resultPos.Y - (marginOffsetY + midMarginY)) * marginBoundsY));

            double finalMarginX = 0;
            double finalMarginY = 0;
            /*if (Settings.Default.pointer_4IRMode == "none")
            {       finalMarginX = Settings.Default.CalibrationMarginX;
                    finalMarginY = Settings.Default.CalibrationMarginY;
            }*/

            lightbarX = (resultPos.X - topLeftPt.X) * boundsX;
            lightbarY = (resultPos.Y - topLeftPt.Y) * boundsY;
            

            
            if (x <= 0) { x = 0; }
            else if (x >= primaryScreen.Bounds.Width) { x = primaryScreen.Bounds.Width - 1; }
            if (y <= 0) { y = 0; }
            else if (y >= primaryScreen.Bounds.Height) { y = primaryScreen.Bounds.Height - 1; }

            CursorPos result = new CursorPos(x, y, median.X, median.Y, angle,
                lightbarX, lightbarY, lightbarX, lightbarY, width, height); // Pasamos lightbarX/Y

            if (lightbarX < 0.0 || lightbarX > 1.0 || lightbarY < 0.0 || lightbarY > 1.0)
            {
                result.OffScreen = true;
                result.LightbarX = lightbarX; // Mantenemos el valor sin acotar aquí para posible debug
                result.LightbarY = lightbarY;
            }

            if (reconstructionSuccess) { result.OnScreenPoints = (PointF[])finalPos.Clone(); }
            else if (lastPos.OnScreenPoints != null) { result.OnScreenPoints = (PointF[])lastPos.OnScreenPoints.Clone(); }

            lastPos = result;
            return result;
        }

        private PointF rotatePoint(PointF point, double angle)
        {
            double sin = Math.Sin(angle * -1);
            double cos = Math.Cos(angle * -1);

            double xnew = point.X * cos - point.Y * sin;
            double ynew = point.X * sin + point.Y * cos;

            PointF result;

            xnew = Math.Min(0.5, Math.Max(-0.5, xnew));
            ynew = Math.Min(0.5, Math.Max(-0.5, ynew));

            result.X = (float)xnew;
            result.Y = (float)ynew;

            return result;
        }

        public void RecalculateFullLightgun()
        {
            targetAspectRatio = 0.0;

            topLeftPt = trueTopLeftPt;
            bottomRightPt = trueBottomRightPt;

            recalculateLightgunCoordBounds();
        }

        public void RecalculateLightgunAspect(double targetAspect)
        {
            this.targetAspectRatio = targetAspect;

            int outputWidth = (int)(targetAspect * primaryScreen.Bounds.Height);
            double scaleFactor = outputWidth / (double)primaryScreen.Bounds.Width;
            double target_topLeftX = ((trueBottomRightPt.X + trueTopLeftPt.X) / 2) - ((trueBottomRightPt.X - trueTopLeftPt.X) * scaleFactor / 2);
            double target_bottomRightY = trueBottomRightPt.X - (target_topLeftX - trueTopLeftPt.X);

            topLeftPt = new PointF()
            {
                X = (float)target_topLeftX,
                Y = trueTopLeftPt.Y
            };

            bottomRightPt = new PointF()
            {
                X = (float)target_bottomRightY,
                Y = trueBottomRightPt.Y
            };

            recalculateLightgunCoordBounds();
        }
    }

    public static class MathF
    {
        public const float PI = (float)Math.PI;
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Cos(float d) => (float)Math.Cos(d);
        public static float Round(float a) => (float)Math.Round(a);
        public static float Sin(float a) => (float)Math.Sin(a);
        public static float Hypot(float p, float b) => (float)Math.Sqrt(Math.Pow(p, 2) + Math.Pow(b, 2));
        public static float Sqrt(float d) => (float)Math.Sqrt(d);
        public static float Max(float val1, float val2) => (float)Math.Max(val1, val2);
        public static float Min(float val1, float val2) => (float)Math.Min(val1, val2);
    }
}