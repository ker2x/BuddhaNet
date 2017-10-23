using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Buddhanet
{
    class BuddhaWorker
    {
        int imageWidth;
        int imageHeight;

        public BuddhaWorker(int width, int height)
        {
            imageWidth = width;
            imageHeight = height;
        }

        public void BuddhaCompute(object bmp)
        {
            //TODO: find something better for rand
            Random rand;
            lock (bmp) { rand = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)); }

            Complex c;
            Complex z;

            for (int i = 0; i < 1000; i++)
            {
                //TODO: Generate complex random within meaningful range ([-2, 0.5] [-1.3,1,3])
                c = new Complex(rand.NextDouble() * 2.5 - 2, rand.NextDouble() * 2.6 - 1.3);
                z = Complex.Zero;


                //((ABS(1.0 - SQRT(1 - (4 * c)))) < 1.0)

                //TODO: Do quick rejection test
                if (((Complex.Abs(c - new Complex(-1, 0))) < 0.25)) continue;
                if ((Complex.Abs(1.0 - Complex.Sqrt(Complex.One - (4 * c))) < 1.0)) continue; //may be wrong
                /*
          double q = (cr-0.25)*(cr-0.25) + ci2;
          //Quick rejection check if c is in main cardioid
          if( q*(q+(cr-0.25)) < 0.25*ci2) return -1;

          //Quick rejection check if c is in 2nd order period bulb
          if( (cr+1.0) * (cr+1.0) + ci2 < 0.0625) return -1;

          // test for the smaller bulb left of the period-2 bulb
          if (( ((cr+1.309)*(cr+1.309)) + ci*ci) < 0.00345) return -1;

          // check for the smaller bulbs on top and bottom of the cardioid
          if ((((cr+0.125)*(cr+0.125)) + (ci-0.744)*(ci-0.744)) < 0.0088) return -1;
          if ((((cr+0.125)*(cr+0.125)) + (ci+0.744)*(ci+0.744)) < 0.0088) return -1;
    */

                //TODO: Loop to maxiter
                int iter = 0;
                while(Complex.Abs(z) < 4.0 && iter < 200 ) {
                    iter++;
                    z = z * z + c;
                }


            }
        }


public void RandomPixels(object bmp)
{
int offset;
Random rand;
lock (bmp) { rand = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)); }
int c;
//lock (bmp)
//{



    unsafe
    {

    for (int i = 0; i < 10; )
        {
            c = rand.Next(255) << 16; //R
            c |= rand.Next(255) << 8; //G
            c |= rand.Next(255);      //B
            offset = 0;
            int x = rand.Next(imageWidth);
            int y = rand.Next(imageHeight);
            offset = (x * 4) + (y * imageWidth * 4);


            Marshal.WriteInt32((IntPtr)bmp, offset, c);
        }
    }
//}
}
}
}
