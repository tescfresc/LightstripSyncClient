using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Windows.Shapes;
using System.Windows.Interop;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using ColorHelper;

namespace LightstripSyncClient
{
    public class ColorSync
    {
        private bool loop = false;
        private Bitmap bitmap;
        private Graphics graphics;

        private int bitmapRes = 150;
        private double smoothSpeed = 0.8;
        private int refreshRate = 5;
        private int blackFilter = 220;
        private int whiteFilter = 220;
        public void ToggleSync(bool state)
        {
            loop = !loop;
            if (loop) SyncLoop();
        }
        async void SyncLoop()
        {
            var oldColor = System.Drawing.Color.White;
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

                        Globals.bluetoothLEConnectionManager.ChangeColor(newColor);

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
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }

        private System.Drawing.Color FindDominantColour(Bitmap bmp)
        {
            //get initial cluster
            Random random = new Random();
            var cluster = bmp.GetPixel(random.Next(0, bmp.Width), random.Next(0, bmp.Height));

            var n = bmp.Width * bmp.Height;

            double r = 0;
            double g = 0;
            double b = 0;

            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    var color = bmp.GetPixel(x, y);
                    if (GetEuclideanDist(color, System.Drawing.Color.Black) >= blackFilter && GetEuclideanDist(color, System.Drawing.Color.White) >= whiteFilter)
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


            System.Drawing.Color updatedCentre = System.Drawing.Color.FromArgb(
                red,
                green,
                blue
                );
            return updatedCentre;
        }

        private double GetEuclideanDist(System.Drawing.Color c1, System.Drawing.Color c2)
        {
            return Math.Sqrt(
                Math.Pow(c1.R - c2.R, 2) + Math.Pow(c1.G - c2.G, 2) + Math.Pow(c1.B - c2.B, 2)
                );
        }

        private System.Drawing.Color SmoothColor(System.Drawing.Color oldCol, System.Drawing.Color newCol, double time)
        {
            var vector = new Vector3(newCol.R - oldCol.R, newCol.G - oldCol.G, newCol.B - oldCol.B);
            var adjustedVector = new Vector3(vector.x * time, vector.y * time, vector.z * time);

            var SmoothedColorVector = new Vector3(oldCol.R + adjustedVector.x, oldCol.G + adjustedVector.y, oldCol.B + adjustedVector.z);

            var SmoothedColor = System.Drawing.Color.FromArgb((int)SmoothedColorVector.x, (int)SmoothedColorVector.y, (int)SmoothedColorVector.z);
            return SmoothedColor;
        }

        struct Vector3
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
