using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Xceed.Wpf.Toolkit;


namespace Buddhanet
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        WriteableBitmap bmp;
        Random rand = new Random();
        IntPtr pBackBuffer;
        BlockingCollection<Complex> bufferComplex, bufferFiltered, bufferFiltered2;
        Stopwatch stopwatch;

        public static int imageWidth = 2560;
        public static int imageHeight = 1440;
        public static long randCounter = 0;
        public static int orbitCounter = 0;
        public static int[,,] screenBuffer = new int[2560, 1440,3];
        public static double maxRe, minRe, maxIm, minIm;
        public static int minIter = 100;
        public static int maxIter = 2000;

        double contrast, luminosity;
        double fps = 1;

        public MainWindow()
        {
            InitializeComponent();

            stopwatch = new Stopwatch();
            stopwatch.Start();

            //Set Default Values
            FpsSlider.Value = 1;
            RedMin.Value = 10; RedMax.Value = 20;
            GreenMin.Value = 15; GreenMax.Value = 25;
            BlueMin.Value = 20; BlueMax.Value = 30;
            NumThread.Value = 1;

            minRe = -2.0 / 1.2;
            maxRe = 2.0 / 1.2;
            minIm = -1.5 / 1.2;
            maxIm = 1.5 / 1.2;

            //Create image
            bmp = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Bgr32, null);
            bmp.Clear(Colors.Black);
            FractalImage.Source = bmp;
            pBackBuffer = bmp.BackBuffer;
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);

            

            //Pipeline are magic <3
            bufferComplex = new BlockingCollection<Complex>(32000);
            bufferFiltered = new BlockingCollection<Complex>(32000);
            bufferFiltered2 = new BlockingCollection<Complex>(32000);
            var bufferScreen = new BlockingCollection<Complex>(32000);



            // Generation and filtering stages
            var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            /* Generate complex number */
            var stage1 = f.StartNew(() => Buddhapipeline.RandomComplexGenerator(bufferComplex, -2, 1, -1.5, 1.5));

            /* Quick rejection test */
            var stage2 = f.StartNew(() => Buddhapipeline.quickRejectionFilter(bufferComplex, bufferFiltered));
            //var stage2b = f.StartNew(() => Buddhapipeline.quickRejectionFilter(bufferComplex, bufferFiltered));
            //var stage2c = f.StartNew(() => Buddhapipeline.quickRejectionFilter(bufferComplex, bufferFiltered));
            //var stage2d = f.StartNew(() => Buddhapipeline.quickRejectionFilter(bufferComplex, bufferFiltered));

            /* Iterative rejection test */
            var stage3 = f.StartNew(() => Buddhapipeline.iterativeRejectionFilter(bufferFiltered, bufferFiltered2));
            var stage3b = f.StartNew(() => Buddhapipeline.iterativeRejectionFilter(bufferFiltered, bufferFiltered2)); //it's a slow stage so let's add more
            var stage3c = f.StartNew(() => Buddhapipeline.iterativeRejectionFilter(bufferFiltered, bufferFiltered2)); //it's a slow stage so let's add more
            var stage3d = f.StartNew(() => Buddhapipeline.iterativeRejectionFilter(bufferFiltered, bufferFiltered2)); //it's a slow stage so let's add more
            var stage3e = f.StartNew(() => Buddhapipeline.iterativeRejectionFilter(bufferFiltered, bufferFiltered2)); //it's a slow stage so let's add more
            var stage3f = f.StartNew(() => Buddhapipeline.iterativeRejectionFilter(bufferFiltered, bufferFiltered2)); //it's a slow stage so let's add more
            //var stage3g = f.StartNew(() => Buddhapipeline.iterativeRejectionFilter(bufferFiltered, bufferFiltered2)); //it's a slow stage so let's add more
            //var stage3h = f.StartNew(() => Buddhapipeline.iterativeRejectionFilter(bufferFiltered, bufferFiltered2)); //it's a slow stage so let's add more

            // At this point we should only have interesting point now.
            // Time to do the real buddha stuff
            var stage4 = f.StartNew(() => Buddhapipeline.complexToBuffer(bufferFiltered2, minRe, maxRe, minIm, maxIm));

            //Non-pipeline task
            var screenUpdateTask = f.StartNew(() => screenUpdater());

        }

        /*
         * This one is called to update & post process the render view
         */
        void screenUpdater()
        {
            while (true)
            {
                //Find max value
                int max = 0;
                for (int j = 0; j < imageHeight; j++)
                {
                    for (int i = 0; i < imageWidth; i++)
                    {
                        max = Math.Max(max, screenBuffer[i, j,0]);
                    }
                }

                Parallel.For(0, imageHeight, item =>
                {
                    for (int i = 0; i < imageWidth; i++)
                    {
                        int newval = (int)Math.Min(Math.Pow(((screenBuffer[i, item,0] / (float)max) * 255), contrast) + luminosity, 255.0);
                        int c;
                        c = newval << 16;
                        c |= newval << 8;
                        c |= newval;
                        int offset = (i * 4) + (item * MainWindow.imageWidth * 4);
                        unsafe { Marshal.WriteInt32((IntPtr)pBackBuffer, offset, c); }
                    }

                });
                Thread.Sleep((int)(1000/fps));
                //Debug.WriteLine(max);
            }
        }


        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            using (bmp.GetBitmapContext())
            {
                bmp.AddDirtyRect(new Int32Rect(1, 1, imageWidth -1, imageHeight -1));
            }
            RandomBufferProgressBar.Value = bufferComplex.Count;
            BufferFilteredProgressBar.Value = bufferFiltered.Count;
            BufferFiltered2ProgressBar.Value = bufferFiltered2.Count;
            RandGeneratorLabel.Text = $"rand per ms ~= {randCounter / stopwatch.ElapsedMilliseconds :0.##}";
            OrbitCountLabel.Text = $"Orbit per ms ~= {(float)orbitCounter / (float)stopwatch.ElapsedMilliseconds :0.##}";
            RandOrbitRatio.Text = $"Rand/Orbit ratio ~= {(float)randCounter / (float)orbitCounter :0.##}";
        }


        //EVENTS

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.DefaultExt = ".png";
            saveFileDialog.Filter = "PNG (.png)|*.png";
            Nullable<bool> result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                //saveFileDialog.FileName;
                //DO STUFF HERE
                System.Windows.MessageBox.Show($"File saved : {saveFileDialog.FileName}");
            }
            else
            {
            }            
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {

        }


        private void FpsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FpsLabel.Content = $"Frames per second ~= {e.NewValue:0.##}";
            fps = e.NewValue;
        }

        private void NumThread_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //System.Windows.MessageBox.Show($"{e.NewValue}");
        }

        private void FractalImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void FractalImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPoint = e.GetPosition(FractalImage);
            System.Windows.MessageBox.Show($"x : {clickPoint.X}, y : {clickPoint.Y}");
        }

        private void FractalImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void FractalImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            /*
            lock (bmp)
            {
                imageWidth = (int)e.NewSize.Width;
                imageHeight = (int)e.NewSize.Height;
                bmp = bmp.Resize(imageWidth, imageHeight, WriteableBitmapExtensions.Interpolation.NearestNeighbor);
                FractalImage.Source = bmp;
            }
            */
            
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ContrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            contrast = e.NewValue;
        }

        private void LuminositySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            luminosity = e.NewValue;
        }
    }
}
