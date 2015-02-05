using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SE_lab
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Run as:\n1 Task Manager\n2 Computational Node\n3 Computational Client");
            string choise = Console.ReadLine();
            switch (choise[0])
            {
                case '1':
                    TaskManager tm = new TaskManager();
                    Console.WriteLine("TASK MANAGER:");
                    tm.Start();
                    Console.ReadKey();
                    break;

                case '2':
                    ComputationalNode c = new ComputationalNode();
                    Console.WriteLine("COMPUTATIONAL NODE:");
                    c.Start();
                    Console.ReadKey();
                    break;

                case '3':
                    ComputationalClient computationalClient = new ComputationalClient();
                    Console.WriteLine("COMPUTATIONAL CLIENT:");
                    computationalClient.Start();
                    break;

                default:
                    Console.WriteLine(choise[0] + " is not an option");
                    break;
            }
            //ComputationalNode c = new ComputationalNode();
            //Console.WriteLine("NODE:");
            //Console.ReadKey();
            //c.RegisterComponent();
            //Console.ReadKey();
            //    while (true)
            //   {
            //        c.ReceivePartialProblem();
            //    }
            //  Console.WriteLine("Otrzymano odpowiedź {0} ", Encoding.ASCII.GetString(c.Receive()));
            //   Console.ReadKey();

         
        }
    }
}