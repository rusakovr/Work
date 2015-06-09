using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sig_crt
{
    class mean_forward
    {
        public static double mean_of_forward(char pre)
        {
            switch (pre)
            {
                // Положительные
                case 'h': return Math.Pow(10.0, 2.0);
                case 'k': return Math.Pow(10.0, 3.0);
                case 'M': return Math.Pow(10.0, 6.0);
                case 'G': return Math.Pow(10.0, 9.0);
                case 'Т': return Math.Pow(10.0, 12.0);
                case 'П': return Math.Pow(10.0, 15.0);
                case 'E': return Math.Pow(10.0, 18.0);
                case 'З': return Math.Pow(10.0, 21.0);
                case 'И': return Math.Pow(10.0, 24.0);

                // Отрицательные
                case 'd': return Math.Pow(10.0, -1.0);
                case 'c': return Math.Pow(10.0, -2.0);
                case 'm': return Math.Pow(10.0, -3.0);
                case 'µ': return Math.Pow(10.0, -6.0);
                case 'n': return Math.Pow(10.0, -9.0);
                case 'p': return Math.Pow(10.0, -12.0);
                case 'f': return Math.Pow(10.0, -15.0);
                case 'a': return Math.Pow(10.0, -18.0);
                case 'z': return Math.Pow(10.0, -21.0);
                case 'y': return Math.Pow(10.0, -24.0);
            }
            return 1.0;
        }
    }
}