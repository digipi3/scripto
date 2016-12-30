#define TESTING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Scripto
{
    class Program
    {
        static StreamWriter Log = new StreamWriter("log.txt", true);

        static void Main(string[] args)
        {
#if TESTING
            args = new string[3];
            args[0] = "C:\\src\\";
            args[1] = "C:\\des\\";
            args[2] = "C:\\scriptoignore.txt";
#endif

            if (args.Length < 2)
            {
                return;
            }

            if (args[0] == null)
            {
                return;
            }

            if (args[1] == null)
            {
                return;
            }

            string[] directoriesToIgnore = null;

            if( args[2] != null )
            {
                try
                {
                    directoriesToIgnore = ExtractDirectoriesToIgnore(args[2]);
                }
                catch( Exception ex )
                {
                    Log.WriteLine("Error opening ignore file" + ex.ToString());
                }

            }

            String sourceDir = args[0];
            String backUpDir = args[1];

            List<string> sourceDirectories = GetAllTheDirectories(sourceDir);

            if (directoriesToIgnore != null && directoriesToIgnore.Length > 0)
            {
                sourceDirectories = RemoveStringsFromStringist(sourceDirectories, directoriesToIgnore);
            }

            // Do all the directories in the source match the directories in the backup?
            int index = 0;
            System.Collections.ArrayList directoriesThatShouldExist = new System.Collections.ArrayList();

            for (int i = 0; i < sourceDirectories.Count; i++)
            {
                index = sourceDirectories[i].IndexOf(sourceDir);

                if( index == -1)
                {
                    continue;
                }

                string directory = sourceDirectories[i].Remove(index, backUpDir.Length);

                string directoryInBackup = backUpDir + directory;

                directoriesThatShouldExist.Add(directoryInBackup);
            }

            // Okay so now we have all the directories that should exist in the back up.
            // If they don't then they need to be created and files should be copied over.

            string backUp = "";
            string src = "";

            for (int i = 0; i < directoriesThatShouldExist.Count; i++)
            {
                backUp = (string)directoriesThatShouldExist[i];

                if (!System.IO.Directory.Exists(backUp))
                {
                    System.IO.Directory.CreateDirectory(backUp);

                    // This is a new directory so all the files will be new.
                    // Copy time.
                    src = sourceDirectories[i];
                    DirectoryCopy(src, backUp, true);
                    Log.WriteLine("Directory and Files Created: \t\t " + backUp);
                }
            }

            // All new directories and files are done.
            // Well ish, what about new file in the root?

            // TODO

            // Get all the files in the source directory:
            string[] allSourceFiles = System.IO.Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories);

            // Copy new files that don't exist in the back-up because we've only copied new 
            // files that are in new directories.
            string sourceFilePath = "";
            string backUpFilePath = "";

            for (int i = 0; i < allSourceFiles.Length; i++)
            {
                sourceFilePath = allSourceFiles[i];
                backUpFilePath = ConvertSourcePathToBackUpPath(sourceFilePath, sourceDir, backUpDir);

                if (!File.Exists(backUpFilePath))
                {
                    File.Copy(sourceFilePath, backUpFilePath);
                    Log.WriteLine("File Copied:\t\t" + backUpFilePath);
                }
            }

            // Now to check for files that have been modified more recently.

            ArrayList filesToCopy = new ArrayList();

            for (int i = 0; i < allSourceFiles.Length; i++)
            {
                sourceFilePath = allSourceFiles[i];
                backUpFilePath = ConvertSourcePathToBackUpPath(sourceFilePath, sourceDir, backUpDir);

                System.IO.FileInfo sourceFile = new System.IO.FileInfo(sourceFilePath);
                System.IO.FileInfo backUpFile = new System.IO.FileInfo(backUpFilePath);

                if (sourceFile.LastWriteTime > backUpFile.LastWriteTime)
                {
                    filesToCopy.Add(sourceFilePath + "," + backUpFilePath);

                    try
                    {
                        File.Copy(sourceFilePath, backUpFilePath, true);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Failed To Copy: \t\t" + sourceFilePath + "to \t\t" + backUpFilePath);
                        Log.Close();
                        return;
                    }
                }
            }

            Log.Close();
        }

        private static List<string> RemoveStringsFromStringist( List<string> stringList, string[] stringsToRemove)
        {
            // At this point we can remove the source directories that are to be ignored.

            List<string> newList = new List<string>();

            for (int i = 0; i < stringList.Count; i++)
            {
                for (int j = 0; j < stringsToRemove.Length; j++)
                {
                    if (stringList[i] != stringsToRemove[j])
                    {
                        newList.Add(stringList[i]);
                    }
                }
            }

            return newList;
        }

        private static string[] ExtractDirectoriesToIgnore( string ignoreFilePath )
        {
            if( ignoreFilePath == null)
            {
                return null;
            }

            string[] lines = null;

            try
            {
                lines = System.IO.File.ReadAllLines(ignoreFilePath);
            }
            catch
            {
                throw;
            }

            return lines;
        }

        private static string ConvertSourcePathToBackUpPath(string srcPath, string srcDir, string backUpDir)
        {
            int index = srcPath.IndexOf(srcDir);

            string backupPath = srcPath.Remove(index, srcDir.Length);

            backupPath = backUpDir + backupPath;

            return backupPath;
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
