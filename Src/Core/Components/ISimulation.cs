
namespace Ripple.Components
{
    public delegate void __OnTimeChangedEventHandler(object sender, int time);

    public interface ISimulation
    {
        event __OnTimeChangedEventHandler __OnTimeChanged;
        void __Initialize(System.Int32 __maxtime);
        void __Run(System.Int32 __maxtime);
    }
}
