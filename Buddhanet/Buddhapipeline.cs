using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Buddhanet
{
    static class Buddhapipeline
    {

         /*
          * Generate random complex within bound.
          */
         public static void RandomComplexGenerator(BlockingCollection<Complex> output, double minRe, double maxRe, double minIm, double maxIm)
         {
            Random rand;
            rand = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));

            while(true)
            {
                lock (MainWindow.pauseLock) { };
                output.Add(
                    new Complex(
                        rand.NextDouble() * (maxRe - minRe) + minRe,
                        rand.NextDouble() * (maxIm - minIm) + minIm
                    )
                );
                MainWindow.randCounter += 1;
            }
         }

        /*
         * quick rejection filter
         */
         public static void quickRejectionFilter(BlockingCollection<Complex> input, BlockingCollection<Complex> output)
         {
            foreach(var item in input.GetConsumingEnumerable())
            {
                if (Complex.Abs(item) > 2.0) continue;
                if ((Complex.Abs(1.0 - Complex.Sqrt(Complex.One - (4 * item))) < 1.0)) continue;
                if (((Complex.Abs(item - new Complex(-1, 0))) < 0.25)) continue;
                if ((((item.Real + 1.309) * (item.Real + 1.309)) + item.Imaginary * item.Imaginary) < 0.00345) continue;
                if ((((item.Real + 0.125) * (item.Real + 0.125)) + (item.Imaginary - 0.744) * (item.Imaginary - 0.744)) < 0.0088) continue;
                if ((((item.Real + 0.125) * (item.Real + 0.125)) + (item.Imaginary + 0.744) * (item.Imaginary + 0.744)) < 0.0088) continue;

                //We tried every known quick filter and didn't reject the item, adding it to next queue.
                output.Add(item);
            }
         }

        /* 
         * iterative rejection filter 
         */
        public static void iterativeRejectionFilter(BlockingCollection<Complex> input, BlockingCollection<Complex> output)
        {
            foreach (var item in input.GetConsumingEnumerable())
            {
                int iter = 0;
                Complex z = Complex.Zero;
                while ((Complex.Abs(z) < 4.0) && (iter < MainWindow.maxIter))
                {
                    iter++;
                    z = z * z + item;
                }
                if (iter == MainWindow.maxIter || iter < MainWindow.minIter) continue; //Reject

                //We tried, couldn't reject it, adding it to next queue
                output.Add(item);
            }
        }


        /* generate orbit from interesting point and add them to buffer */
        public static void complexToBuffer(BlockingCollection<Complex> input, double minRe, double maxRe, double minIm, double maxIm)
        {
            foreach(var item in input.GetConsumingEnumerable())
            {
                int iter = 0;
                Complex z = Complex.Zero;
                int x, y;
                MainWindow.orbitCounter++;
                while((Complex.Abs(z) < 4 && iter < MainWindow.maxIter))
                {
                    iter++;
                    z = z * z + item;
                    x = (int)(MainWindow.imageWidth * ((z.Real - minRe) / (maxRe - minRe)));
                    y = (int)(MainWindow.imageHeight * ((z.Imaginary - minIm) / (maxIm - minIm)));
                    if (x > 0 && y > 0 && x < MainWindow.imageWidth && y < MainWindow.imageHeight && iter > MainWindow.minIter)
                    {
                        //MainWindow.screenBuffer[x, y,0]++;
                        if(iter >= MainWindow.rmin && iter <= MainWindow.rmax) MainWindow.screenBuffer[x, y, 0]++;
                        if (iter >= MainWindow.gmin && iter <= MainWindow.gmax) MainWindow.screenBuffer[x, y, 1]++;
                        if (iter >= MainWindow.bmin && iter <= MainWindow.bmax) MainWindow.screenBuffer[x, y, 2]++;
                    }
                }

            }
        }
    }
}
