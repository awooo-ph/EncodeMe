// Source: Somewhere in www.codeproject.com

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace NORSU.EncodeMe
{
    static class ImageProcessor
    {

        public static Image Resize(Image imgPhoto, int size)
        {
            var sourceWidth = imgPhoto.Width;
            var sourceHeight = imgPhoto.Height;
            var sourceX = 0;
            var sourceY = 0;
            var destX = 0;
            var destY = 0;
            var nPercent = 0.0f;

            if (sourceWidth > sourceHeight)
                nPercent = (size / (float)sourceWidth);

            else
                nPercent = (size / (float)sourceHeight);



            var destWidth = (int)(sourceWidth * nPercent);
            var destHeight = (int)(sourceHeight * nPercent);

            var bmPhoto = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            var grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }
        
        [DllImport("shell32.dll", EntryPoint = "#261",
            CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void GetUserTilePath(
            string username,
            UInt32 whatever, // 0x80000000
            StringBuilder picpath, int maxLength);

        public static byte[] GetRandomLego()
        {
            var rnd = new Random();
            var info = Application.GetResourceStream(new Uri($"pack://application:,,,/Lego/{rnd.Next(1, 9)}.jpg"));
            if (info == null) return null;
            using (var mem = new MemoryStream())
            {
                info.Stream.CopyTo(mem);
                return mem.ToArray();
            }
        }

        public static string GetUserTilePath(string username)
        {
            // username: use null for current user
            var sb = new StringBuilder(1000);
            GetUserTilePath(username, 0x80000000, sb, sb.Capacity);
            return sb.ToString();
        }

        public static byte[] GetUserTile(string username)
        {
            return File.ReadAllBytes(GetUserTilePath(username));
        }
}
}
