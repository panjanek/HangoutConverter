using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangoutConverter
{
    public static class ImageUtil
    {
        public static void ResizeImage(string inputFile, string outputFile, int desiredWidth)
        {
            using (var input = new Bitmap(inputFile))
            {
                int desiredHeight = (input.Height * desiredWidth) / input.Width;
                using (var result = new Bitmap(desiredWidth, desiredHeight, PixelFormat.Format32bppArgb))
                {
                    result.SetResolution(72, 72);
                    using (Graphics graph = Graphics.FromImage(result))
                    {
                        graph.CompositingQuality = CompositingQuality.HighQuality;
                        graph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graph.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graph.SmoothingMode = SmoothingMode.HighQuality;
                        graph.DrawImage(input, 0, 0, desiredWidth, desiredHeight);
                    }

                    result.Save(outputFile);
                }
            }
        }
    }
}
