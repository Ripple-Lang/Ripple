using System;

namespace Ripple.Components.Libraries
{
    public static class Randoms
    {
        private static Random randomGenerator = new Random();

        public static void SetSeed(int seed)
        {
            randomGenerator = new Random(seed);
        }

        public static int GetRandomInt(int min, int max)
        {
            return randomGenerator.Next(min, max);
        }

        public static double GetRandomDouble(double min, double max)
        {
            if (min == 0 && max == 1)
            {
                return randomGenerator.NextDouble();
            }
            else
            {
                return randomGenerator.NextDouble() * (max - min) + min;
            }
        }
    }
}
