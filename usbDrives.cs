using System;
using System.IO;
using System.Management;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace usbDrives
{
    // Used to cause events when usbs are pluggged in or taken out.
    public class usbWatch
    {
        // Current drive letters and their corresponding watchers.
        Dictionary<String, FileSystemWatcher> driveWatchers = new Dictionary<string, FileSystemWatcher>();
        List<string> hashesOfFilesCopied = new List<string>();

        ManagementEventWatcher usbPluggedInEvent = new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
        ManagementEventWatcher usbRemovedEvent = new ManagementEventWatcher("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");

        public usbWatch()
        {

        }
        public void startWatching()
        {
            getRemovableDrives();

            //Admin privileges required in order to detect inserted/removed usb drives
            usbPluggedInEvent.EventArrived += new EventArrivedEventHandler(usbPluggedInEvent_EventArrived);
            try
            {
                usbPluggedInEvent.Start();
            }
            catch (ManagementException e)
            {
                Console.WriteLine("Error plugged in start: {0}", e);
            }
            usbRemovedEvent.EventArrived += new EventArrivedEventHandler(usbRemovedEvent_EventArrived);
            try
            {
                usbRemovedEvent.Start();
            }
            catch (ManagementException e)
            {
                Console.WriteLine("Error removed start: {0}", e);
            }
        }
        // Triggers whenever a usb device is plugged in.
        void usbPluggedInEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            string driveName = e.NewEvent.Properties["DriveName"].Value.ToString();
            DriveType type = (DriveType)e.NewEvent.Properties["DriveType"].Value;
            // Add the drive letter and watcher.
            if (type == DriveType.Removable)
            {
                driveWatchers.Add(driveName, new FileSystemWatcher());
                driveWatchers[driveName].Path = driveName;
                driveWatchers[driveName].NotifyFilter = NotifyFilters.LastWrite;
                driveWatchers[driveName].Filter = "*";
                driveWatchers[driveName].Changed += new FileSystemEventHandler(onChanged);
                // Created event does not trigger, but changed event works and triggers 2ish times per copy
                //driveWatchers[driveName].Created += new FileSystemEventHandler(onChanged);
                driveWatchers[driveName].EnableRaisingEvents = true;
                driveWatchers[driveName].IncludeSubdirectories = true;
            }
        }

        // Triggers whenever a usb device is removed.
        void usbRemovedEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            // Dispose of the watcher and remove from the dictionary.
            // Delay just in case a hash needs to finish (I assumed that the
            // file data would already be loaded into memory).
            string driveName = e.NewEvent.Properties["DriveName"].Value.ToString();

            System.Threading.Thread.Sleep(250);
            driveWatchers[driveName].Dispose();
            driveWatchers.Remove(driveName);
        }


        // Finds all the removable drives and starts their watchers.
        private void getRemovableDrives()
        {
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                
                //Console.WriteLine("Found {0} drive", driveInfo.Name);
                if (driveInfo.DriveType == DriveType.Removable)
                {
                    driveWatchers.Add(driveInfo.Name, new FileSystemWatcher());
                    // The name is the drive letter, like E:\
                    // Have to ensure that you escape the '\' by adding one... which itself must be escaped!
                    driveWatchers[driveInfo.Name].Path = driveInfo.Name + "\\";
                    driveWatchers[driveInfo.Name].NotifyFilter = NotifyFilters.LastWrite;
                    driveWatchers[driveInfo.Name].Filter = "*";
                    driveWatchers[driveInfo.Name].EnableRaisingEvents = true;
                    driveWatchers[driveInfo.Name].IncludeSubdirectories = true;
                    driveWatchers[driveInfo.Name].Changed += new FileSystemEventHandler(onChanged);
                    // Created event does not trigger, but changed event works and triggers 2ish times per copy
                    //driveWatchers[driveInfo.Name].Created += new FileSystemEventHandler(onChanged);
                    
                }
            }
        }

        private void onChanged(object source, FileSystemEventArgs e)
        {
            // Copies file to another directory.
            // Helper funciton that waits until the file has been completely copied before taking action.
            bool isFileReady(String sFilename)
            {
                // If the file can be opened for exclusive access it means that the file is no longer locked by another process.
                try
                {
                    using (FileStream inputStream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return (inputStream.Length > 0);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            // First wait in case the file is still copying.
            // The event triggers when the write starts, not ends, so this could 
            // be a long wait for larger files.
            while (!isFileReady(e.FullPath))
            {
                continue;
            }

            // Rather than copy the file again, just hash it. Can compare to existing file hashes, the
            // only issue is that it won't provide much information with an unknown hash.

            string temp;

            using (SHA256 sha = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(e.FullPath))
                {
                    byte[] hash = sha.ComputeHash(stream);
                    // Convert it from a byte array to a string.
                    temp = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }

            // Write the new file hash as it's calculated.
            //Console.WriteLine("New file written to usb with SHA256: {0}", temp);
            hashesOfFilesCopied.Add(temp);
        }

        // Output all the hashes we found to the screen.
        public string getCopiedHashes()
        {
            string output = "";
            foreach (string hash in hashesOfFilesCopied)
            {
               output += String.Format("{0}", hash);
            }
            return output;
        }
    }
}