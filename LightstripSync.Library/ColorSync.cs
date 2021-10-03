using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LightstripSyncClient
{
    public class ColorSync
    {
        private bool loop = false;
        private Bitmap bitmap;
        private Graphics graphics;

        private readonly double smoothSpeed = 0.8;
        private readonly int refreshRate = 10;
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
            var random = new Random();
            while (loop)
            {
                using (bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb))
                {
                    using (graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

                        //var newColor = FindDominantColour(bitmap);
                        //newColor = SmoothColor(oldColor, newColor, smoothSpeed);
                        var newColor = GetAverageColor(bitmap, 3);

                        //bluetoothLEConnectionManager.ChangeColor(Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255)));
                        bluetoothLEConnectionManager.ChangeColor(newColor);

                        oldColor = newColor;

                        bitmap.Dispose();
                        graphics.Dispose();
                    }
                }
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
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

        public unsafe Color GetAverageColor(Bitmap image, int sampleStep = 1)
        {
            var data = image.LockBits(
                new Rectangle(Point.Empty, image.Size),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            var row = (int*)data.Scan0.ToPointer();
            var (sumR, sumG, sumB) = (0L, 0L, 0L);
            var stride = data.Stride / sizeof(int) * sampleStep;

            for (var y = 0; y < data.Height; y += sampleStep)
            {
                for (var x = 0; x < data.Width; x += sampleStep)
                {
                    var argb = row[x];
                    sumR += (argb & 0x00FF0000) >> 16;
                    sumG += (argb & 0x0000FF00) >> 8;
                    sumB += argb & 0x000000FF;
                }
                row += stride;
            }

            image.UnlockBits(data);

            var numSamples = data.Width / sampleStep * data.Height / sampleStep;
            var avgR = sumR / numSamples;
            var avgG = sumG / numSamples;
            var avgB = sumB / numSamples;
            return Color.FromArgb((int)avgR, (int)avgG, (int)avgB);
        }
    }
}
