using DLLFinder;
using Processes;
using Files;
using usbDrives;
using RegistryKeys;
using activePorts;
using System;
using System.Threading.Tasks;

namespace Windows_Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            var dllFinder = new DLLInject();
            var extentions = new FileExtentions();
            // Process and usb watchers need to begin watching and will trigger events
            // The below loop only queries their results.
            var procWatch = new ProcessWatch();
            procWatch.startWatching();
            var usbWatcher = new usbWatch();
            usbWatcher.startWatching();
            var getsKeys = new RunKeys();
            var getPorts = new PortFinder();

            string[] results = new String[6];

            while (true)
            {
                Parallel.Invoke(() =>
                {
                    results[0] = dllFinder.hashLoadedDlls();
                },
                () =>
                {
                    results[1] = extentions.determineExecutingPrograms();
                },
                () =>
                {
                    results[2] = (procWatch.getOutput());
                },
                () =>
                {
                    results[3] = (usbWatcher.getCopiedHashes());
                },
                () =>
                {
                    results[4] = getsKeys.displayRunKeys();
                },
                () =>
                {
                    results[5] = getPorts.getNetStatPorts();
                });


                foreach (string str in results)
                {
                    Console.Write(str);
                }
                System.Threading.Thread.Sleep(60000);
            }

            //Console.WriteLine("All done!");
            //Console.ReadLine();
        }
    }
}
