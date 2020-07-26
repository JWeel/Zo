using System;

namespace Zo
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var core = new Core())
                core.Run();
        }
    }
}
