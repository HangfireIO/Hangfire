using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HangFire.Web
{
    public class TesteJob2
    {
        public void Run()
        {
            System.Threading.Thread.Sleep(1000 * 60 * 1);
            throw new Exception();
        }
    }
}
