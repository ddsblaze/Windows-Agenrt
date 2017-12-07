using System;
using Microsoft.Win32;

namespace RegistryKeys
{
    class RunKeys
    {
        public RunKeys()
        {
            
        }

        public string displayRunKeys()
        {
            string output = "";
            output += "HKLM Run keys:\n";
            output += writeArray(Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run").GetValueNames());
            output += "HKCU Run keys:\n";
            output += writeArray(Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run").GetValueNames());
            output += "HKLM RunOnce keys:\n";
            output += writeArray(Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce").GetValueNames());
            output += "HKCU RunOnce keys:\n";
            output += writeArray(Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce").GetValueNames());

            return output;
        }

        private string writeArray(string[] arr)
        {
            string output = "";
            for (int i = 0; i < arr.Length; i++)
            {
                output += String.Format("{0}", arr[i]);
            }
            return output;
        }
    }
}