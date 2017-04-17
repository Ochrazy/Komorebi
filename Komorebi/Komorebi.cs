using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komorebi
{
    class Komorebi
    {
        static void Main()
        {
            using (Window window = new Window())
            {
                // Maximum of 58 fps
                window.Run(58.0, 58.0);
            }
        }
    }
}
