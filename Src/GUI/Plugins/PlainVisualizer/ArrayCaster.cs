using System;

namespace Ripple.Plugins.PlainVisualizer
{
    internal static class ArrayCaster
    {
        public static double[][][] CastToDouble(Array array)
        {
            if (array is double[][][])
            {
                return (double[][][])array;
            }
            else if (array is double[][])
            {
                return new double[][][] { (double[][])array };
            }

            int numDimensions = GetDimensions(array);

            if (numDimensions == 2)
            {
                return new double[][][] { Cast2D((dynamic)array) };
            }
            else if (numDimensions == 3)
            {
                return Cast3D((dynamic)array);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static double[][][] Cast3D<T>(T[][][] array)
        {
            double[][][] ret = new double[array.Length][][];

            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = Cast2D<T>(array[i]);
            }

            return ret;
        }

        private static double[][] Cast2D<T>(T[][] array)
        {
            double[][] ret = new double[array.Length][];

            for (int i = 0; i < array.Length; i++)
            {
                ret[i] = Cast1D<T>(array[i]);
            }

            return ret;
        }

        private static double[] Cast1D<T>(T[] array)
        {
            double[] ret = new double[array.Length];

            if (array is bool[])
            {
                var boolArray = array as bool[];

                for (int i = 0; i < boolArray.Length; i++)
                {
                    ret[i] = boolArray[i] ? 1.0 : 0.0;
                }
            }
            else
            {
                for (int i = 0; i < array.Length; i++)
                {
                    ret[i] = (double)(dynamic)array[i];
                }
            }

            return ret;
        }

        public static int GetDimensions(object array)
        {
            int dimension = 0;
            while (array is Array)
            {
                array = ((Array)array).GetValue(0);
                dimension++;
            }
            return dimension;
        }
    }
}
