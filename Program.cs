#define TESTING

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Scripto
{
    class Program
    {
        static void Main(string[] args)
        {
#if TESTING
            args = new string[2];
            args[0] = "C:\\src\\";
            args[1] = "C:\\des\\";
#endif

            if ( args.Length < 2)
            {
                return;
            }

            if( args[0] == null)
            {
                return;
            }

            if( args[1] == null )
            {
                return;
            }

            String sourceDir = args[0];
            String backUpDir = args[1];

            List<string> sourceDirectories = GetAllTheDirectories(sourceDir);

            // Do all the directories in the source match the directories in the backup?
            int index = 0;
            System.Collections.ArrayList directoriesThatShouldExist = new System.Collections.ArrayList();

            for ( int i = 0; i < sourceDirectories.Count; i++ )
            {
                index = sourceDirectories[i].IndexOf(sourceDir);

                string directory = sourceDirectories[i].Remove(index, backUpDir.Length);

                string directoryInBackup = backUpDir + directory;

                directoriesThatShouldExist.Add(directoryInBackup);
            }

            // Okay so now we have all the directories that should exist in the back up.
            // If they don't then they need to be created and files should be copied over.

            string backUp= "";
            string src = "";

            for( int i = 0; i < directoriesThatShouldExist.Count; i++ )
            {
                backUp = (string ) directoriesThatShouldExist[i];

                if( ! System.IO.Directory.Exists(backUp) )
                {
                    System.IO.Directory.CreateDirectory(backUp);

                    // This is a new directory so all the files will be new.
                    // Copy time.

                    src = sourceDirectories[i];

                    DirectoryCopy(src, backUp, true);
                }
            } 
        }



        private static List<string> GetDirectories(string path)
        {
            try
            {
                return System.IO.Directory.GetDirectories(path).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<string>();
            }
        }

        private static List<string> GetAllTheDirectories(string path)
        {            
            var directories = new List<string>(GetDirectories(path));

            for (var i = 0; i < directories.Count; i++)
            {
                directories.AddRange(GetDirectories(directories[i]));
            }

            return directories;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
