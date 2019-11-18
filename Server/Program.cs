using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace WIRC
{
    class Program
    {
        static int Main(string[] args)
        {
            var bindAddress = IPEndPoint.Parse("0.0.0.0:2424");
            if (args.Length >= 1)
                bindAddress = IPEndPoint.Parse(args[0]);

            var server = new Server(bindAddress, Console.Out);
            server.Start().Wait();

            return 0;
        }
    }
}
