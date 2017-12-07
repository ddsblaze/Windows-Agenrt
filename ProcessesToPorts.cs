using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;


// Essentially the same as www.cheynewallace.com/get-active-ports-and-associated-process-names-in-c/
namespace activePorts
{
    public class PortFinder
    {
        public PortFinder()
        {

        }

        // Runs netstat and extracts the useful port information.
        public string getNetStatPorts()
        {
            var ports = new List<Port>();

            try
            {
                Process p = new Process();

                ProcessStartInfo ps = new ProcessStartInfo();
                ps.Arguments = "-a -n -o";
                ps.FileName = "netstat.exe";
                ps.UseShellExecute = false;
                //ps.WindowStyle = ProcessWindowStyle.Hidden;
                ps.CreateNoWindow = true;

                ps.RedirectStandardInput = true;
                ps.RedirectStandardOutput = true;
                ps.RedirectStandardError = true;

                p.StartInfo = ps;
                p.Start();

                StreamReader stdOutput = p.StandardOutput;
                StreamReader stdError = p.StandardError;

                string content = stdOutput.ReadToEnd() + stdError.ReadToEnd();
                string exitStatus = p.ExitCode.ToString();

                if (exitStatus != "0")
                {
                    Console.WriteLine("Netstat command failed!");
                }

                // Extract the relevant information from the netstat output.
                string[] rows = Regex.Split(content, "\r\n");
                foreach (string row in rows)
                {
                    // Split fields on >= 1 whitespace.
                    string[] tokens = Regex.Split(row, "\\s+");
                    // Select where the first word is TCP or UDP and the entire line is well-formed (all columns present).
                    if (tokens.Length > 4 && (tokens[1].Equals("UDP") || tokens[1].Equals("TCP")))
                    {
                        // Replace the IPv6 address, identified by at least one character encased in []'s, with 1.1.1.1 as a placeholder.
                        string localAddress = Regex.Replace(tokens[2], @"\[(.*?)\]", "1.1.1.1");
                        // Create and add a new port.
                        ports.Add(new Port
                        {
                            // Note to self:
                            // variable = (condition) ? (expr if true) : (expr if false)
                            // Used to identify IPv6 connecitons.
                            protocol = localAddress.Contains("1.1.1.1") ? String.Format("{0}v6", tokens[1]) : String.Format("{0}v4", tokens[1]),
                            // Netstat returns the address in form address:port.
                            //port_number = localAddress.Split(':')[1],
                            // TCP shows the state, UDP does not, so control for additional colums in TCP connections.
                            process_name = tokens[1] == "UDP" ? lookupProcess(Convert.ToInt16(tokens[4])) : lookupProcess(Convert.ToInt16(tokens[5])),
                            // Provide the distant connection IP if it is an established conneciotn.
                            //distantIP = tokens[4] == "ESTABLISHED" ? tokens[3] : "---",

                            //Just use the netstat output more directly
                            connectionInfo =  tokens[2] + " --> " + tokens[3]
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            string stringedPorts = "";

            // Convert the port informtion into a string to return.
            foreach (Port i in ports)
            {
                stringedPorts += String.Format("{0}\n", i.getName());
            }

            return stringedPorts;
        }

        // Gets the process name, or ideally the process path, from the PID.
        private static string lookupProcess(int pid)
        {
            string procName;
            try
            { 
                // Try to get the full path of the process, failing that return the process name only.
                procName = Process.GetProcessById(pid).MainModule.FileName;
            }
            catch (Exception)
            {
                try
                {
                    // Failing that, get only the name.
                    procName = Process.GetProcessById(pid).ProcessName;
                }
                catch
                {
                    // Failing that just use a filler.
                    procName = "-";
                }
            }
            return procName;
        }
    }

    // Port class used to store the information to be returned.
    public class Port
    {
        public string getName()
        {
            //return string.Format("{0} ({1} port {2}) --> {3}", this.process_name, this.protocol, this.port_number, this.distantIP);
            return String.Format("{0} / {1} {2}", process_name, protocol, connectionInfo);
        }
        //public string port_number { get; set; }
        public string process_name { get; set; }
        public string protocol { get; set; }
        //public string distantIP { get; set; }

        public string connectionInfo { get; set; }

    }
}