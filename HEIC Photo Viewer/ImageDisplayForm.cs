using ImageMagick;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HEIC_Photo_Viewer
{
    public partial class ImageDisplayForm : Form
    {
        public ImageDisplayForm()
        {
            InitializeComponent();
        }

        private Image CurrentImage = null;
        private string ImageName = null;
        private int zoomLevel = 100;
        private Point _OldMousePos = new Point(0, 0);

        private Image ConvertHEICtoImage(string Path)
        {
            try
            {
                using var image = new MagickImage(Path);
                image.Format = MagickFormat.Jpeg;
                byte[] data = image.ToByteArray();
                using var ms = new MemoryStream(data);
                image.Dispose();
                return Image.FromStream(ms);
            }
            catch { return null; }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK) { return; }
            OpenImageFromPath(openFileDialog.FileName);
        }

        private void OpenImageFromPath(string path)
        {
            Image ConvertedImage = ConvertHEICtoImage(path);
            if (ConvertedImage is null) { return; }
            pictureBox1.Image = ConvertedImage;
            CurrentImage = ConvertedImage;
            ImageName = Path.GetFileNameWithoutExtension(path);

            this.Text = $"HEIC Image View: {ImageName}";

            SizeObjects();
            zoomInToolStripMenuItem.Visible = true;
            zoomOutToolStripMenuItem.Visible = true;
        }

        private void saveAsJPGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentImage is null) { return; }
            SaveFileDialog openFileDialog = new SaveFileDialog();
            openFileDialog.FileName = $"{ImageName}";
            openFileDialog.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|Png Image|*.png|Tiff Image|*.tiff|Wmf Image|*.wmf";
            openFileDialog.Title = "Saving Image";
            var result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK) { return; }
            saveImage(openFileDialog.FileName);
        }

        public void saveImage(string fileName)
        {
            Bitmap bmp = new Bitmap((Bitmap)CurrentImage.Clone());
            string FileExtention = Path.GetExtension(fileName)??"";
            switch (FileExtention)
            {
                case ".jpg":
                    bmp.Save(fileName, ImageFormat.Jpeg);
                    break;
                case ".png":
                    bmp.Save(fileName, ImageFormat.Png);
                    break;
                case ".bmp":
                    bmp.Save(fileName, ImageFormat.Bmp);
                    break;
                case ".gif":
                    bmp.Save(fileName, ImageFormat.Gif);
                    break;
                case ".tiff":
                    bmp.Save(fileName, ImageFormat.Tiff);
                    break;
                case ".wmf":
                    bmp.Save(fileName, ImageFormat.Wmf);
                    break;
                default:
                    MessageBox.Show($"Failed to Save File!\n{FileExtention} format not recognized", "Failed to Save File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void ImageDisplayForm_ResizeEnd(object sender, EventArgs e)
        {
            SizeObjects();
        }

        private void SizeObjects()
        {
            if (pictureBox1.Image is null) { return; }
            var currentScroll = panel1.AutoScrollPosition;
            panel1.AutoScrollPosition = new Point(0, 0);
            panel1.Width = this.Width - 16;
            panel1.Height = this.Height - menuStrip1.Height - 42;
            int PBX = (pictureBox1.Parent.ClientSize.Width / 2) - (pictureBox1.Image.Width / 2);
            int PBY = (pictureBox1.Parent.ClientSize.Height / 2) - (pictureBox1.Image.Height / 2);
            pictureBox1.Location = new Point(PBX < 0 ? 0 : PBX, PBY < 0 ? 0 : PBY);
            panel1.AutoScrollPosition = new Point(Math.Abs(currentScroll.X), Math.Abs(currentScroll.Y));
            pictureBox1.Refresh();
        }

        private void ImageDisplayForm_Load(object sender, EventArgs e)
        {
            zoomInToolStripMenuItem.Visible = false;
            zoomOutToolStripMenuItem.Visible = false;
            if (Environment.GetCommandLineArgs().Length > 1) { OpenImageFromPath(Environment.GetCommandLineArgs()[1]); }
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            saveAsJPGToolStripMenuItem.Visible = CurrentImage != null;
        }

        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int modifier = 10;
            if ((ModifierKeys & Keys.Control) == Keys.Control) { modifier = 1; }
            if ((ModifierKeys & Keys.Shift) == Keys.Shift) { modifier = 20; }
            zoomLevel = (sender == zoomOutToolStripMenuItem) ? zoomLevel - modifier : zoomLevel + modifier;
            if (zoomLevel < 10) { zoomLevel = 10; }
            DisplayResizedImage();
            SizeObjects();
        }

        private void DisplayResizedImage()
        {
            int widthZoom = (CurrentImage.Width * zoomLevel) / 100;
            int heightZoom = (CurrentImage.Height * zoomLevel) / 100;
            pictureBox1.Image = resizeImage(CurrentImage, new Size(widthZoom, heightZoom));
        }

        public static Image resizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_OldMousePos == e.Location) { return; }
            Point MouseLocation = Cursor.Position;
            if (e.Button == MouseButtons.Left)
            {
                int CurrentHorizontalScroll = panel1.HorizontalScroll.Value;
                int CurrentVErticalScroll = panel1.VerticalScroll.Value;
                int HorizontalScrollAmmount = _OldMousePos.X - MouseLocation.X;
                int VerticalScrollAmmount = _OldMousePos.Y - MouseLocation.Y;
                panel1.AutoScrollPosition = new Point(CurrentHorizontalScroll + HorizontalScrollAmmount, CurrentVErticalScroll + VerticalScrollAmmount);
            }
            _OldMousePos = MouseLocation;
        }
    }
}
