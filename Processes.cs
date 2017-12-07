using System;
using System.Collections.Generic;
// System.Diagnostics is needed for PerformanceCounters, but you also need to modify the .csproj file to use net### (in this case 461)
// because .NET CORE does not include PerformanceConters. Since this will only be used on Windows, this is an acceptable workaround.
// Reference: https://dzone.com/articles/how-to-use-performance-counters-withnet-core
using System.Diagnostics;
// Note that to work, System.Management must be added to the project by going to Project > Add References
// then adding System.Management.dll from C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework.NETFramework\<version whatever>\System.Management.dll
// in addition to the using statement.
using System.Management;

namespace Processes
{
    //static void main(string[] args)
    //{
    //    // Should run from there I think?
    //    var watcher = new processInfo();
    //}

    // Used to cause events when processes start/end.
    public class ProcessWatch
    {
        ManagementEventWatcher processStartEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStartTrace");
        ManagementEventWatcher processStopEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStopTrace");

        // The counters, so they can be accessed by the watchers as well as ProcessInfo.
        Dictionary<int, PerformanceCounter> privCounters = new Dictionary<int, PerformanceCounter>();
        Dictionary<int, PerformanceCounter> processorCounters = new Dictionary<int, PerformanceCounter>();
        Dictionary<int, PerformanceCounter> memoryCounters = new Dictionary<int, PerformanceCounter>();

        public ProcessWatch()
        {

        }
        public void startWatching()
        {
            // Admin required to add/remove processes from events. Currently untested.
            processStartEvent.EventArrived += new EventArrivedEventHandler(processStartEvent_EventArrived);
            try
            {
                processStartEvent.Start();
            }
            catch (ManagementException e)
            {
                Console.WriteLine("{0}", e);
            }
            processStopEvent.EventArrived += new EventArrivedEventHandler(processStopEvent_EventArrived);
            try
            {
                processStopEvent.Start();
            }
            catch (ManagementException e)
            {
                Console.WriteLine("{0}", e);
            }

            setupCounters();
            // Wait so that each counter is able to get data, 500ms.
            System.Threading.Thread.Sleep(500);

            //For the processor counters, the first "nextvalue" is always 0, so skip this one

            // It is possible that the dictionaries are modified during iteration, in order to still iterate through them, use a toList.

            // Has issue where the dictionaries will be modified during the iteration.
            lock (processorCounters)
            {
                foreach (KeyValuePair<int, PerformanceCounter> entry in processorCounters)
                {
                    //The lock could mean that a process actually terminated but was not removed from the lsit already
                    try
                    {
                        entry.Value.NextValue();
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine("{0} is no longer running: {1}", Process.GetProcessById(entry.Key), e);
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            lock (privCounters)
            {
                foreach (KeyValuePair<int, PerformanceCounter> entry in privCounters)
                {
                    //The lock could mean that a process actually terminated but was not removed from the lsit already
                    try
                    {
                        entry.Value.NextValue();
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine("{0} is no longer running: {1}", Process.GetProcessById(entry.Key), e);
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        public string getOutput()
        {
            string output = "";

            output += "The memory of each process:\n";
            output += displayCounters(memoryCounters);

            output += "\nThe processor time of each process:\n";
            output += displayCounters(processorCounters);

            output += "\nThe privileged processor time of each process:\n";
            output += displayCounters(privCounters);

            return output;
        }

        // Run when a process starts.
        void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            int pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);

            // Create and add the counters to the dictionaries.
            // Somehow there are duplicate entries attempted.
            lock (privCounters)
            {
                privCounters.Add(pid, new PerformanceCounter("Process", "% Processor Time", processName));
            }
            lock (processorCounters)
            {
                processorCounters.Add(pid, new PerformanceCounter("Process", "% Privileged Time", processName));
            }
            lock (memoryCounters)
            {
                memoryCounters.Add(pid, new PerformanceCounter("Process", "Working Set", processName));
            }
        }

        // Run when a process stops.
        void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();

            int pid = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);

            // Dispose and remove the counters.
            try
            {
                lock (privCounters)
                {
                    privCounters[pid].Dispose();
                    privCounters.Remove(pid);
                }

                lock (processorCounters)
                {
                    processorCounters[pid].Dispose();
                    processorCounters.Remove(pid);
                }

                lock (memoryCounters)
                {
                    memoryCounters[pid].Dispose();
                    memoryCounters.Remove(pid);
                }
            }
            catch
            {
                Console.WriteLine("Failed to remove process ID {0}, it's already dead.", pid);
            }
            //Console.WriteLine("Removed {0}", processName);
        }


    // Create the counters to monitor each process' utilization of privileged and normal proessor time as well as RAM.
    private void setupCounters()
        {
            //It is possible that the process could end between getProcesses and the add calls.
            Process[] theProcs = Process.GetProcesses();
            foreach (Process i in theProcs)
            {
                lock (privCounters)
                {
                    try
                    {
                        privCounters.Add(i.Id, new PerformanceCounter("Process", "% Privileged Time", i.ProcessName));
                    }
                    catch
                    {
                        Console.WriteLine("Cannot add process to dictionary: {0} {1}", i.Id, i.ProcessName);
                    }
                }
                lock (processorCounters)
                {
                    try
                    {
                        processorCounters.Add(i.Id, new PerformanceCounter("Process", "% Processor Time", i.ProcessName));
                    }
                    catch
                    {
                        Console.WriteLine("Cannot add process to dictionary: {0} {1}", i.Id, i.ProcessName);
                    }
                }
                lock (memoryCounters)
                {
                    try
                    {
                        memoryCounters.Add(i.Id, new PerformanceCounter("Process", "Working Set", i.ProcessName));
                    }
                    catch
                    {
                        Console.WriteLine("Cannot add process to dictionary: {0} {1}", i.Id, i.ProcessName);
                    }
                }
            }
        }

        // Updates and prints each counter with its assocaited process.
        private string displayCounters(Dictionary<int, PerformanceCounter> dict)
        {
            string output = "";
            lock (dict)
            {
                List<int> removePIDs = new List<int>();
                foreach (KeyValuePair<int, PerformanceCounter> entry in dict)
                {
                    // Update the counter with the next captured measurement.
                    // Can cause an error when the process dies as this function is executing
                    // and the watchers can't update the dictionary in time.
                    try
                    {
                        output += String.Format("Process: {0} utilizes: {1}\n", Process.GetProcessById(entry.Key).ProcessName, entry.Value.NextValue());
                    }
                    catch
                    {
                        // Can't diaplay the name, because the process is dead.
                        Console.WriteLine("PID {0} does not exist anymore, removing it.\n", entry.Key);
                        // Remove it, because it seems to stick around if and not get handled by the watchers otherwise.
                        removePIDs.Add(entry.Key);
                    }
                }
                // Have to remove it outside the loop, or else it modifies the dictionary while iterating through it.
                foreach (int i in removePIDs)
                {
                    dict.Remove(i);
                }
            }
            return output;
        }
    }
}