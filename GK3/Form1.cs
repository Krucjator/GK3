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

        private void ErrorDiffusionDithering(object sender, EventArgs e)
        {
            Bitmap outBmp = new Bitmap(pictureBox1.Image);
            if (!int.TryParse(textBox1.Text, out int tones) || tones < 2 || tones > 255) return;
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

            public void Add(Colors error)
            {
                red += error.red ;
                green += error.green;
                blue += error.blue;
            }

            public double distSq(Colors c)
            {
                return (c.red - red) * (c.red - red) + (c.green - green) * (c.green - green) + (c.blue - blue) * (c.blue - blue);
            }

            internal void DivideBy(double v)
            {
                red = red / v;
                green = green / v;
                blue = blue / v;
            }
        }

        private void PopularityAlgorithm(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox1.Text, out int tones) || tones < 2 || tones > 255) return;
            if (!int.TryParse(textBox2.Text, out int cubeDensity) 
                || cubeDensity < 2 
                || cubeDensity > 255 
                || cubeDensity* cubeDensity* cubeDensity<tones) return;
            
            Bitmap outBmp = new Bitmap(pictureBox1.Image);
            //number of cubes equals to cubeDensity cubed
            int[,,] cubes = new int[cubeDensity, cubeDensity, cubeDensity];
            double stride = 256.0 / cubeDensity;
            Colors[,] colors = new Colors[outBmp.Height, outBmp.Width];
            for (int i = 0; i < outBmp.Height; i++)
            {
                for (int j = 0; j < outBmp.Width; j++)
                {
                    Color color = outBmp.GetPixel(j, i);
                    colors[i, j] = new Colors(color.R, color.G, color.B);
                    cubes[(int)(color.R / stride), (int)(color.G / stride), (int)(color.B / stride)]++;
                }
            }
            List<Colors> PopularColors = new List<Colors>();
            for (int i = 0; i < tones; i++)
            {
                Colors col = new Colors();
                int count = -1;
                int r = -1;
                int g = -1;
                int b = -1;
                for (int x = 0; x < cubeDensity; x++)
                {
                    for (int y = 0; y < cubeDensity; y++)
                    {
                        for (int z = 0; z < cubeDensity; z++)
                        {
                            if (cubes[x, y, z] > count)
                            {
                                count = cubes[x, y, z];
                                r = x;
                                g = y;
                                b = z;
                                //center of each cube represents a possible color
                                col = new Colors(x * stride + stride / 2, y * stride + stride / 2, z * stride + stride / 2);
                            }
                        }
                    }
                }
                PopularColors.Add(col);
                cubes[r, g, b] = -1;
            }
            for (int i = 0; i < outBmp.Height; i++)
            {
                for (int j = 0; j < outBmp.Width; j++)
                {
                    double minDist = PopularColors.Min(x => x.distSq(colors[i, j]));
                    Colors closestColor = PopularColors.First(x => x.distSq(colors[i, j]) == minDist);
                    outBmp.SetPixel(j, i, Color.FromArgb((int)closestColor.red, (int)closestColor.green, (int)closestColor.blue));
                }
            }
            pictureBox2.Image = outBmp;
        }

        private void Kmeans(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox3.Text, out int K) || K < 2 || K > 255) return;
            Random random = new Random();
            Bitmap outBmp = new Bitmap(pictureBox1.Image);
            int[] count = new int[K];
            Colors[] means = new Colors[K];
            Colors[] newMeans = new Colors[K];
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

            //starting centers, K random colors from bitmap
            for (int i = 0; i < K; i++)
            {
                int x, y, roll = 0;
                do
                {
                    x = random.Next(0, outBmp.Width);
                    y = random.Next(0, outBmp.Height);
                    roll++;
                }
                while (means.Contains(col[y, x]) && roll < 10);
                means[i] = col[y, x];
            }


            int iterations = 40;
            for (int p = 0; p < iterations; p++)
            {
                for (int i = 0; i < K; i++)
                {
                    newMeans[i] = new Colors(0, 0, 0);
                    count[i] = 0;
                }

                for (int i = 0; i < outBmp.Height; i++)
                {
                    for (int j = 0; j < outBmp.Width; j++)
                    {
                        double min = double.MaxValue;
                        int indx = -1;
                        for (int s = 0; s < K; s++)
                        {
                            double dist = col[i, j].distSq(means[s]);
                            if (dist < min)
                            {
                                min = dist;
                                indx = s;
                            }
                        }
                        newMeans[indx].Add(col[i, j]);
                        count[indx]++;
                    }
                }

                for (int i = 0; i < K; i++)
                {
                    count[i] = Math.Max(count[i], 1);
                    newMeans[i].DivideBy(count[i]);
                    means[i] = newMeans[i];
                }
            }

            //assign points to centers
            for (int i = 0; i < outBmp.Height; i++)
            {
                for (int j = 0; j < outBmp.Width; j++)
                {
                    double min = double.MaxValue;
                    int label = -1;
                    for (int s = 0; s < K; s++)
                    {
                        double dist = col[i, j].distSq(means[s]);
                        if (dist < min)
                        {
                            min = dist;
                            label = s;
                        }
                    }
                    outBmp.SetPixel(j, i, Color.FromArgb((int)means[label].red, (int)means[label].green, (int)means[label].blue));
                }
            }
            pictureBox2.Image = outBmp;
        }
    }
}
