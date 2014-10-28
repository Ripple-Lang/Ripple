using System;
using System.Diagnostics;
using Ripple.Components;

namespace TestConsole
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            const int maxTime = 100;
            const int width = 480;
            const int wind = 4;
            const double amount = 0.1;
            const double collapse = 0.2;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            dynamic ist = new __N1.__C1();

            ist.width = width;
            ist.wind = wind;
            ist.amount = amount;
            ist.collapse = collapse;

            ((ISimulation)ist).__OnTimeChanged += (_, time) =>
            {
                Console.WriteLine(time);
            };

            ist.__Initialize(maxTime);
            ist.__Run(maxTime);

            sw.Stop();

            Console.WriteLine(sw.Elapsed.TotalSeconds);
        }
    }
}
