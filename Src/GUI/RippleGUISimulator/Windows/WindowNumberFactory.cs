
namespace Ripple.GUISimulator.Windows
{
    static class WindowNumberFactory
    {
        private static int num = 0;

        public static int GetNextNo()
        {
            return num++;
        }
    }
}
