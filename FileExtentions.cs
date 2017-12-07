using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;

namespace Files
{
    public class FileExtentions
    {
        public FileExtentions()
        {

        }
        public string determineExecutingPrograms()
        {
            Dictionary<string, string> applicationPaths;

            //Go through the C:\ drive and get unique extentions and corresponding example files
            List<string> testFilePaths = getFilePaths();
            //Need the example files to determine which applicaitons open them
            applicationPaths = pairExtentionsToApplications(testFilePaths);

            return displayDict(applicationPaths);
        }

        // Get all the extentions on the C:\ drive.
        private List<string> getFilePaths()
        {
            HashSet<string> temp = new HashSet<string>();
            List<string> filePaths = new List<string>();
            // Only the user directory, should get the likely files to be exploited.
            string[] fileArray = enumerateFilesRecursive("C:\\Users\\", "*").ToArray();

            // Will enumerate files in the filesystem and allows for error handling.
            // From https://stackoverflow.com/questions/7756626/enumerating-files-throwing-exception
            IEnumerable<string> enumerateFilesRecursive(string root, string pattern = "*")
            {
                var todo = new Queue<string>();
                todo.Enqueue(root);
                while (todo.Count > 0)
                {
                    string dir = todo.Dequeue();
                    string[] subdirs = new string[0];
                    string[] files = new string[0];
                    try
                    {
                        subdirs = Directory.GetDirectories(dir);
                        files = Directory.GetFiles(dir, pattern);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        // Do nothing, just ignore the directories we cannpt access
                        Console.WriteLine("Cannot access directory! {0}", dir.ToString());
                    }

                    foreach (string subdir in subdirs)
                    {
                        todo.Enqueue(subdir);
                    }
                    foreach (string filename in files)
                    {
                        yield return filename;
                    }
                }
            }


            foreach (string file in fileArray)
            {
                // Just in case there are multiple .'s in the filename we want the last one for the extention.
                if (file.Contains("."))
                {
                    string[] theExtention = file.Split('.');

                    // Done so that we can run FindExecutable, as it requires an actual filepath to work
                    // not just an extention: only add if the HashSet returns true, meaning that it is a
                    // successful add due to a new item (string in this case).
                    if (temp.Add(theExtention[theExtention.Length - 1]))
                    {
                        filePaths.Add(file);
                    }
                }
                // Ignore files without extentions, determined by the presence of a "." somewhere in the path.
                else
                {
                    continue;
                }
            }

            // The HashSet was only needed to ensure that no duplicate entries are entered
            // I imagine this is more efficient than splitting the current entry and iterating 
            // through the filePaths array doing string.contains each time.
            return filePaths;
        }

        // Pair the extentions with their default applications.
        private Dictionary<string, string> pairExtentionsToApplications(List<string> filePaths)
        {
            Dictionary<string, string> applications = new Dictionary<string, string>();
            Finder find = new Finder();

            foreach (string item in filePaths)
            {
                string[] temp = item.Split('.');
                // The extention, then the full path to the application used to open the file.
                applications.Add(temp[temp.Length - 1], find.FindExecutable(item));
            }

            // So returns along the lines of < pdf, /path/Adobe.exe >, < doc, /path/MicrosoftWord.exe > etc
            return applications;
        }

        //Print a dictionary, only works on <string, string> types
        private string displayDict(Dictionary<string, string> dict)
        {
            string output = "";
            foreach (KeyValuePair<string, string> i in dict)
            {
                //Many "Error 31"'s due to specific files that aren't normally opened by user programs, filter these out
                if (!i.Value.Contains("Error"))
                {
                    output += String.Format("Extention: {0} is opened with applicaiton: {1}\n", i.Key, i.Value);
                }
            }
            return output;
        }
    }

    // This should allow executable finding. Copied from 
    // http://it.toolbox.com/blogs/paytonbyrd/how-to-use-findexecutable-from-c-3324
    // and looks right.
    public class Finder
    {
        [DllImport("shell32.dll", EntryPoint = "FindExecutable")]
        public static extern long FindExecutableA(string lpFile, string lpDirectory, StringBuilder lpResult);

        public string FindExecutable(string pv_strFilename)
        {
            StringBuilder objResultBuffer = new StringBuilder(1024);
            long lngResult = 0;

            lngResult = FindExecutableA(pv_strFilename, string.Empty, objResultBuffer);

            if (lngResult >= 32)
            {
                return objResultBuffer.ToString();
            }

            return string.Format("Error: ({0})", lngResult);
        }

        // Gets the index in an array of strings where that string contains the given extention.
        private int getIndex(string[] stringArray, string extention)
        {
            int index = -1;

            for (int i = 0; i < stringArray.Length; i++)
            {
                // Need to ensure that the extention found is actually the extention and not some absurd filename
                // like blahpdf.pdf.doc, so can't match on just the extention or the extention prepended with a "."
                // I'm also pretty sure you can't use regex in "contains"
                // so this ensures that we're matching on the actual extention part.
                string[] elementSplit = stringArray[i].Split('.');
                string element = elementSplit[elementSplit.Length - 1];

                // "Equals" should also work because only the extention itself was stored.
                // This works because only the firt encountered isntance of a filetype has its name stored in the
                // array passed to stringArray: filePaths.
                if (element.Contains(extention))
                {
                    return i;
                }
            }
            // Error return -1
            return index;
        }
    }
}
