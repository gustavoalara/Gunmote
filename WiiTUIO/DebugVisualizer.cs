using System.Windows.Forms;
using System.Drawing;
using WiimoteLib;
using System.Collections.Generic;

public static class DebugVisualizer
{
    private static Form overlayForm;
    private static WiimoteLib.PointF[] currentRhombusPoints;

    // Variables para la Vista del Sensor ---
    private static Form sensorViewForm;
    private static Dictionary<int, WiimoteLib.PointF> currentSensorPoints;

    public static void DrawRhombus(WiimoteLib.PointF[] points)
    {
        currentRhombusPoints = (WiimoteLib.PointF[])points.Clone();

        if (overlayForm == null || overlayForm.IsDisposed)
        {
            overlayForm = new Form();
            overlayForm.FormBorderStyle = FormBorderStyle.None;
            overlayForm.WindowState = FormWindowState.Maximized;
            overlayForm.TopMost = true;
            overlayForm.BackColor = Color.Magenta;
            overlayForm.TransparencyKey = Color.Magenta;
            overlayForm.ShowInTaskbar = false;

            overlayForm.Paint += new PaintEventHandler(OverlayForm_Paint);
            overlayForm.Show();
        }

        overlayForm.Invalidate();
        Application.DoEvents();
    }

    // Método para dibujar la vista del sensor ---
    public static void DrawSensorView(Dictionary<int, WiimoteLib.PointF> points)
    {
        currentSensorPoints = points; // No es necesario clonar una lista pasada por valor si solo la leemos

        if (sensorViewForm == null || sensorViewForm.IsDisposed)
        {
            sensorViewForm = new Form();

            // --- NUEVO: Eliminar la barra de título ---
            sensorViewForm.FormBorderStyle = FormBorderStyle.None;

            sensorViewForm.TopMost = true;
            sensorViewForm.BackColor = Color.Black;
            sensorViewForm.ShowInTaskbar = false;
            sensorViewForm.Text = "Wiimote Sensor View";

            // --- NUEVO: Calcular tamaño y posición ---
            // Obtenemos las dimensiones de la pantalla principal
            int screenW = Screen.PrimaryScreen.Bounds.Width;
            int screenH = Screen.PrimaryScreen.Bounds.Height;

            // Establecemos el tamaño a 1/8 de la pantalla
            sensorViewForm.ClientSize = new Size(screenW / 6, screenH / 6);

            // Posicionamos la ventana en la esquina superior izquierda (0, 0)
            sensorViewForm.Load += SensorViewForm_Load;

            sensorViewForm.Paint += new PaintEventHandler(SensorView_Paint);
            sensorViewForm.Show();
        }
        sensorViewForm.Invalidate();
        Application.DoEvents();
    }

    //Evento Paint para la Vista del Sensor ---
    private static void SensorView_Paint(object sender, PaintEventArgs e)
    {
        if (currentSensorPoints == null) return;

        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int formW = sensorViewForm.ClientSize.Width;
        int formH = sensorViewForm.ClientSize.Height;

        // Usamos el mismo array de colores que en el rombo
        Color[] vertexColors = { Color.Red, Color.Blue, Color.Yellow, Color.Green };

        // Iteramos sobre el diccionario (pares de clave-valor)
        foreach (var pair in currentSensorPoints)
        {
            int index = pair.Key;       // El índice del vértice (0, 1, 2, o 3)
            WiimoteLib.PointF point = pair.Value;  // La posición del punto

            // Asignamos el color que corresponde a su índice
            using (Brush brush = new SolidBrush(vertexColors[index]))
            {
                // Aplicamos inversión
                float x = (point.X) * formW;
                float y = (1.0f - point.Y) * formH; 

                // Dibujamos el círculo del color correcto
                g.FillEllipse(brush, x - 4, y - 4, 8, 8);
            }
        }
    }

    private static void OverlayForm_Paint(object sender, PaintEventArgs e)
    {
        if (currentRhombusPoints == null || currentRhombusPoints.Length != 4) return;
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int screenW = Screen.PrimaryScreen.Bounds.Width;
        int screenH = Screen.PrimaryScreen.Bounds.Height;
        System.Drawing.Point[] screenPoints = new System.Drawing.Point[4];
        for (int i = 0; i < 4; i++)
        {
            screenPoints[i] = new System.Drawing.Point((int)(currentRhombusPoints[i].X * screenW), (int)(currentRhombusPoints[i].Y * screenH));
        }
        using (Pen pen = new Pen(Color.LimeGreen, 3))
        {
            g.DrawPolygon(pen, screenPoints); // DrawPolygon es más eficiente
        }
        Color[] vertexColors = { Color.Red, Color.Blue, Color.Yellow, Color.Green };

        for (int i = 0; i < 4; i++)
        {
            using (Pen vertexPen = new Pen(vertexColors[i], 4))
            {
                // Dibujamos un círculo de 10x10 en cada punto
                g.DrawEllipse(vertexPen, screenPoints[i].X - 5, screenPoints[i].Y - 5, 10, 10);
            }
        }
    }

    private static void SensorViewForm_Load(object sender, System.EventArgs e)
    {
        // Obtenemos una referencia al formulario que ha lanzado el evento
        Form form = sender as Form;

        // Establecemos la posición en la esquina superior izquierda
        form.Location = new System.Drawing.Point(0, 0);
    }

    public static void HideAll()
    {
        if (overlayForm != null && !overlayForm.IsDisposed)
        {
            overlayForm.Close(); // Cierra y libera los recursos del formulario
            overlayForm = null;
        }

        if (sensorViewForm != null && !sensorViewForm.IsDisposed)
        {
            sensorViewForm.Close(); // Cierra y libera los recursos del formulario
            sensorViewForm = null;
        }
    }

}