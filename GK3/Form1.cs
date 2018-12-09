using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GK3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            splitContainer2.SplitterDistance = splitContainer2.Size.Width / 2;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open Image";
                dlg.Filter = "Image Files (*.bmp;*.jpg;*.jpeg,*.png)|*.BMP;*.JPG;*.JPEG;*.PNG"; ;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Bitmap bitmap = new Bitmap(dlg.FileName);
                    if (bitmap.Width > splitContainer2.SplitterDistance)
                    {
                        int width = splitContainer2.SplitterDistance;
                        int height = (bitmap.Height * width) / bitmap.Width;
                        bitmap = ResizeImage(bitmap, width, height);
                    }
                    pictureBox1.Image = bitmap;
                    pictureBox2.Image = null;
                }
            }
        }



        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Bitmap outBmp = new Bitmap(pictureBox1.Image);
            if(!int.TryParse(textBox1.Text,out int tones) || tones < 2 || tones >255)return;
            double stride = 255 / (tones - 1);
            Colors[,] col = new Colors[outBmp.Height, outBmp.Width];
            for (int i = 0; i < outBmp.Height; i++)
            {
                for (int j = 0; j < outBmp.Width; j++)
                {
                    Color color = outBmp.GetPixel(j, i);
                    col[i, j].red = color.R;
                    col[i, j].green = color.G;
                    col[i, j].blue = color.B;
                }
            }

            for (int i = 0; i < outBmp.Height; i++)
            {
                for (int j = 0; j < outBmp.Width; j++)
                {
                    int red = (int)Math.Round((int)Math.Round(col[i, j].red / stride) * stride);
                    int green = (int)Math.Round((int)Math.Round(col[i, j].green / stride) * stride);
                    int blue = (int)Math.Round((int)Math.Round(col[i, j].blue / stride) * stride);
                    CheckConstraints(ref red, ref green, ref blue);
                    outBmp.SetPixel(j, i, Color.FromArgb(red, green, blue));
                    Colors error = new Colors(
                        col[i, j].red - red,
                        col[i, j].green - green,
                        col[i, j].blue - blue);

                    if (j + 1 < outBmp.Width)
                        col[i, j + 1].AddError(error, 7.0 / 16);
                    if (i + 1 < outBmp.Height && j - 1 > 0)
                        col[i + 1, j - 1].AddError(error, 3.0 / 16);
                    if (i + 1 < outBmp.Height)
                        col[i + 1, j].AddError(error, 5.0 / 16);
                    if (i + 1 < outBmp.Height && j + 1 < outBmp.Width)
                        col[i + 1, j + 1].AddError(error, 1.0 / 16);
                }
            }
            pictureBox2.Image = outBmp;

        }

        private static void CheckConstraints(ref int red, ref int green, ref int blue)
        {
            red = red > 255 ? 255 : red;
            green = green > 255 ? 255 : green;
            blue = blue > 255 ? 255 : blue;
            red = red < 0 ? 0 : red;
            green = green < 0 ? 0 : green;
            blue = blue < 0 ? 0 : blue;
        }

        public struct Colors
        {
            public double red;
            public double green;
            public double blue;

            public Colors(double red, double green, double blue)
            {
                this.red = red;
                this.green = green;
                this.blue = blue;
            }

            public void AddError(Colors error, double weight)
            {
                red += error.red * weight;
                green += error.green * weight;
                blue += error.blue * weight;
            }

        }

    }

}
