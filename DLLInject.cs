using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;


namespace DLLFinder
{
    public class DLLInject
    {
        public DLLInject()
        {

        }
        public string hashLoadedDlls()
        {
            // Idea is to export each proess and its loaded modules along with their hashes in an attempt to detect anomolous DLLs.

            Dictionary<int, ProcessModuleCollection> modulesByProcess = getModules();
            string output = "";

            // Hash each module, current output is just to console.
            foreach (KeyValuePair<int, ProcessModuleCollection> entry in modulesByProcess)
            {
                foreach (ProcessModule i in entry.Value)
                {
                    // Does not hash the memory, only the location of the module.
                    // Would catch custom DLLs but won't do much if the memory was modified.
                    // Also possible that the process dies between collection and output, in that case only display the PID.
                    string name;
                    try
                    {
                        name = Process.GetProcessById(entry.Key).ProcessName;
                    }
                    catch
                    {
                        name = entry.Key.ToString();
                    }
                    output += String.Format("Process: {0}, Module: {1}, Hash: {2}\n", name, i.ModuleName, getSHA256(i.FileName));
                }
            }
            return output;
        }

        // Returns each process as a key in a dictionary with the array of modules ProcessModuleCollection as the value.
        private Dictionary<int, ProcessModuleCollection> getModules()
        {
            Dictionary<int, ProcessModuleCollection> modulesByProcess = new Dictionary<int, ProcessModuleCollection>();

            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    //Needs to run as administrator to get modules for several processes.
                    modulesByProcess.Add(proc.Id, proc.Modules);
                }
                catch
                {
                    Console.WriteLine("Cannot add modules for process {0}", proc.ProcessName);
                }
            }
            return modulesByProcess;
        }

        // Hashes the input.
        private string getSHA256(string path)
        {
            SHA256 sha = SHA256.Create();
            FileStream stream = File.OpenRead(path);
            byte[] hash = sha.ComputeHash(stream);
            // Convert it from a byte array to a string.
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
