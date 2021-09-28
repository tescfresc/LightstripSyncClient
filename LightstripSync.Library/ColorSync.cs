using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LightstripSyncClient
{
    public class ColorSync
    {
        private bool loop = false;
        private Bitmap bitmap;
        private Graphics graphics;

        private readonly int bitmapRes = 150;
        private readonly double smoothSpeed = 0.8;
        private readonly int refreshRate = 5;
        private readonly int blackFilter = 220;
        private readonly int whiteFilter = 220;
        public void ToggleSync(bool state, BluetoothLEConnectionManager bluetoothLEConnectionManager)
        {
            loop = state;
            if (loop)
            {
                SyncLoop(bluetoothLEConnectionManager);
            }
        }

        private async void SyncLoop(BluetoothLEConnectionManager bluetoothLEConnectionManager)
        {
            var oldColor = Color.White;
            while (loop)
            {
                using (bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
                {
                    using (graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
                        bitmap = ResizeBitmap(bitmap, bitmapRes, bitmapRes);

                        var newColor = FindDominantColour(bitmap);

                        newColor = SmoothColor(oldColor, newColor, smoothSpeed);

                        bluetoothLEConnectionManager.ChangeColor(newColor);

                        oldColor = newColor;

                        bitmap.Dispose();
                        graphics.Dispose();
                    }
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(refreshRate);
            }

        }

        public Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            var result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }

        private Color FindDominantColour(Bitmap bmp)
        {
            //get initial cluster
            var random = new Random();
            _ = bmp.GetPixel(random.Next(0, bmp.Width), random.Next(0, bmp.Height));

            var n = bmp.Width * bmp.Height;

            double r = 0;
            double g = 0;
            double b = 0;

            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    var color = bmp.GetPixel(x, y);
                    if (GetEuclideanDist(color, Color.Black) >= blackFilter && GetEuclideanDist(color, Color.White) >= whiteFilter)
                    {
                        r += color.R;
                        g += color.G;
                        b += color.B;
                    }
                    else
                    {
                        n--;
                    }

                }
            }

            ////clamp values
            var red = (int)Math.Round(r / n);
            var green = (int)Math.Round(g / n);
            var blue = (int)Math.Round(b / n);

            red = Math.Min(255, Math.Max(0, red));
            green = Math.Min(255, Math.Max(0, green));
            blue = Math.Min(255, Math.Max(0, blue));


            var updatedCentre = Color.FromArgb(
                red,
                green,
                blue
                );
            return updatedCentre;
        }

        private double GetEuclideanDist(Color c1, Color c2)
        {
            return Math.Sqrt(
                Math.Pow(c1.R - c2.R, 2) + Math.Pow(c1.G - c2.G, 2) + Math.Pow(c1.B - c2.B, 2)
                );
        }

        private Color SmoothColor(Color oldCol, Color newCol, double time)
        {
            var vector = new Vector3(newCol.R - oldCol.R, newCol.G - oldCol.G, newCol.B - oldCol.B);
            var adjustedVector = new Vector3(vector.x * time, vector.y * time, vector.z * time);

            var SmoothedColorVector = new Vector3(oldCol.R + adjustedVector.x, oldCol.G + adjustedVector.y, oldCol.B + adjustedVector.z);

            var SmoothedColor = Color.FromArgb((int)SmoothedColorVector.x, (int)SmoothedColorVector.y, (int)SmoothedColorVector.z);
            return SmoothedColor;
        }

        private struct Vector3
        {
            public double x, y, z;
            public Vector3(double x, double y, double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
        }
    }
}
