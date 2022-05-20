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
using System.Drawing.Printing;

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
            if (ConvertedImage is null)
            {
                MessageBox.Show($"{path}\nwas not a valid image!", "Failed to Open File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; 
            }
            CurrentImage = ConvertedImage;
            DisplayResizedImage();
            ImageName = Path.GetFileNameWithoutExtension(path);

            this.Text = $"HEIC Image View: {Path.GetFileName(path)}";

            FormatUI();
        }

        private void saveAsJPGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentImage is null) { return; }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = $"{ImageName}";
            saveFileDialog.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|Png Image|*.png|Tiff Image|*.tiff|Wmf Image|*.wmf";
            saveFileDialog.Title = "Saving Image";
            var result = saveFileDialog.ShowDialog();
            if (result != DialogResult.OK) { return; }
            saveImage(saveFileDialog.FileName);
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
            FormatUI();
        }

        private void FormatUI()
        {
            zoomInToolStripMenuItem.Visible = pictureBox1.Image is not null;
            zoomOutToolStripMenuItem.Visible = pictureBox1.Image is not null;
            saveAsJPGToolStripMenuItem.Visible = CurrentImage is not null;
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
            if (Environment.GetCommandLineArgs().Length > 1) { OpenImageFromPath(Environment.GetCommandLineArgs()[1]); }
            FormatUI();
            printToolStripMenuItem.Visible = false;
        }


        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int modifier = 10;
            if ((ModifierKeys & Keys.Control) == Keys.Control) { modifier = 1; }
            if ((ModifierKeys & Keys.Shift) == Keys.Shift) { modifier = 20; }
            zoomLevel = (sender == zoomOutToolStripMenuItem) ? zoomLevel - modifier : zoomLevel + modifier;
            if (zoomLevel < 10) { zoomLevel = 10; }
            DisplayResizedImage();
            FormatUI();
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

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
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

        private void ImageDisplayForm_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (e.Delta > 0) { zoomToolStripMenuItem_Click(zoomInToolStripMenuItem, e); }
            else if (e.Delta < 0) { zoomToolStripMenuItem_Click(zoomOutToolStripMenuItem, e); }
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintImage();
        }
        private void PrintImage()
        {
            PrintPreviewDialog printPreviewDialog1 = new PrintPreviewDialog();
            printPreviewDialog1.Document = CreatePrintDocument(true);

            ToolStripButton OrientationButton = new ToolStripButton();
            OrientationButton.Text = "LandScape";
            OrientationButton.Checked = printPreviewDialog1.Document.DefaultPageSettings.Landscape;
            OrientationButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            OrientationButton.Click += delegate 
            { 
                printPreviewDialog1.Document = CreatePrintDocument(!printPreviewDialog1.Document.DefaultPageSettings.Landscape);
                OrientationButton.Checked = printPreviewDialog1.Document.DefaultPageSettings.Landscape;
            };

            ToolStripButton PrintButton = new ToolStripButton();
            PrintButton.Text = "Print";
            PrintButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            PrintButton.Click += delegate 
            { 
                PrintDialog(printPreviewDialog1.Document);
                printPreviewDialog1.Close();
            };

            ((ToolStrip)(printPreviewDialog1.Controls[1])).Items.RemoveAt(0);
            ((ToolStrip)(printPreviewDialog1.Controls[1])).Items.Insert(0, OrientationButton);
            ((ToolStrip)(printPreviewDialog1.Controls[1])).Items.Insert(0, PrintButton);
            printPreviewDialog1.ShowDialog();

        }

        private void PrintDialog(PrintDocument PD)
        {
            PrintDialog PrintDialog1 = new PrintDialog();
            PrintDialog1.Document = PD;
            var result = PrintDialog1.ShowDialog();
            if (result != DialogResult.OK) { return; }
            PrintDialog1.Document.Print();
        }

        private PrintDocument CreatePrintDocument(bool Landscape)
        {
            PrintDocument pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = Landscape;
            pd.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);
            pd.PrintPage += PrintPage;
            return pd;  
        }

        private void PrintPage(object o, PrintPageEventArgs e)
        {
            Image i = resizeImage(CurrentImage, new Size(e.MarginBounds.Width, e.MarginBounds.Height));

            e.Graphics.DrawImage(i, e.MarginBounds);
        }
    }

    public class WheelessPanel : Panel
    {
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            //Custom Panel Plass with Mouse wheel scrolling disabled
        }
    }

}
