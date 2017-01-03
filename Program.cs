#define TESTING
//#define BATCH

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Scripto
{
    /* Important notes:
     * 
     * Directories are dealth with in the following format:
     * C:\\Movies and not C:\\Movies\\ so the last slash will be removed.
     * 
     */
    class Program
    {
        static StreamWriter Log = new StreamWriter("log.txt", true);
#if BATCH
        static StreamWriter Batch = new StreamWriter("batch.txt", true);
#endif 

        static void Main(string[] args)
        {
#if TESTING
            args = new string[3];
            //args[0] = "C:\\src";
            //args[1] = "C:\\des";

            args[0] = "'C:\\Users\\mark\\Documents\\test'";
            args[1] = "'E:\\test backup'";


            args[2] = "C:\\scriptoignore.txt";
#endif

            if (args.Length < 2)
            {
                Log.Close();
                return;
            }

            if (args[0] == null)
            {
                Log.Close();
                return;
            }

            if (args[1] == null)
            {
                Log.Close();
                return;
            }

            List<string> directoriesToIgnore = null;

            //Sometimes directory path will be passed to the program using single and double quotes
            if (args[0].Contains("'") )
            {
                args[0] = args[0].Replace("'", "");
            }

            if (args[0].Contains("\""))
            {
                args[0] = args[0].Replace("\"", "");
            }

            //Sometimes a directory path will be passed to the program using single and double quotes
            if (args[1].Contains("'") )
            {
                args[1] = args[1].Replace("'", "");
            }


            if (args[1].Contains("\""))
            {
                args[1] = args[1].Replace("\"", "");
            }

            if ( ! Directory.Exists( args[0] ) )
            {
                LogMessage("Source directory doesn't exist");
                Log.Close();
                return;
            }

            if ( ! Directory.Exists( args[1] ) )
            {
                LogMessage("Backup directory doesn't exist: " + args[1].ToString() );
                Log.Close();
                return;
            }

            String sourceDir = args[0];
            String backUpDir = args[1];

            // Get all the files from source and remove files in the ignore list:
            List<string> files =
                System.IO.Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories)
                .ToList<string>();

            if ( args[2] != null )
            {
                try
                {
                    directoriesToIgnore = ExtractDirectoriesToIgnore(args[2]);
                }
                catch( Exception ex )
                {
                    LogMessage("Error opening ignore file" + ex.ToString());
                    Log.Close();
                    return;
                }
            }
           

            if( files.Count < 1 )
            {
                LogMessage("No files in the source directory?!");
                Log.Close();
                return;
            }

            LogMessage("Start the fans please");

            // Get all the directories in the source directory
            List<string> sourceDirectories = GetAllTheDirectories(sourceDir);

            // if there aren't any directories then this stuff doesn't need to happen:
            if (sourceDirectories.Count > 0)
            {
                // Ignore any directories that have been specified by the user
                if (directoriesToIgnore != null && directoriesToIgnore.Count > 0)
                {
                    sourceDirectories = RemoveStringsIfMatchesStringInList(sourceDirectories, directoriesToIgnore);
                }

                // Remove root part of the source directory from each directory path
                List<string> folders = RemoveStringFromStringList(sourceDir, sourceDirectories);

                LogMessage("Creating any directories that do not exist and any files within them.\n\n");

                // Next the directories that don't exist in back-up need to be created and their files copied?!
                CreateDirectoriesAndCopyFiles(sourceDir, backUpDir, folders);
            }            

            int numFilesSource = files.Count;

            files = RemoveStringIfContainsStringInList(files, directoriesToIgnore);
            List<string> paths = RemoveStringFromStringList(sourceDir, files);

            int numFilesAfterIgnore = files.Count;

            int numFilesIgnored = numFilesSource - numFilesAfterIgnore;

            LogMessage("Copying Files that are new.\n\n");
            // Copy files that are new.
            CopyFiles(sourceDir, backUpDir, paths);

            LogMessage("Copying Files that have been modified more recently.\n\n");
            // Copy Files that have been modified more recently:
            CopyModifiedFiles(sourceDir, backUpDir, paths );

            LogMessage("Number of files in source directory: " + numFilesSource.ToString() );
            LogMessage("Number of files ignored: " + numFilesIgnored.ToString());

            Log.Close();
#if BATCH
            Batch.Close();
#endif
        }

        private static List<string> ExtractDirectoriesToIgnore(string ignoreFilePath)
        {
            if (ignoreFilePath == null)
            {
                return null;
            }

            string[] lines = null;

            try
            {
                lines = System.IO.File.ReadAllLines(ignoreFilePath);

                RemoveAnyLastSlashes(ref lines);
            }
            catch
            {
                throw;
            }

            return lines.ToList<string>();
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

        private static List<string> RemoveStringsIfMatchesStringInList(List<string> stringList, List<string> stringsToRemove)
        {
            // At this point we can remove the source directories that are to be ignored.

            List<string> newList = new List<string>();

            for (int i = 0; i < stringList.Count; i++)
            {
                for (int j = 0; j < stringsToRemove.Count; j++)
                {
                    if (stringList[i] != stringsToRemove[j])
                    {
                        newList.Add(stringList[i]);
                    }
                }
            }
            return newList;
        }

        private static List<string> RemoveStringFromStringList( string removeString, List<string> list )
        {
            int index = 0;

            if( list.Count < 1)
            {
                return null;
            }

            List<string> folders = new List<string>(list.Count);

            for( int i = 0; i < list.Count; i++ )
            {
                if (list[i].Contains(removeString))
                {
                    index = list[i].IndexOf(removeString);
                    folders.Add( list[i].Remove(index, removeString.Length) );
                 }
            }
            return folders;
        }

        private static List<string> RemoveStringIfContainsStringInList(List<string> stringList, List<string> stringsToRemove)
        {
            // At this point we can remove the source directories that are to be ignored.

            List<string> newList = new List<string>();

            for (int i = 0; i < stringList.Count; i++)
            {
                for (int j = 0; j < stringsToRemove.Count; j++)
                {
                    if (!stringList[i].Contains(stringsToRemove[j]))
                    {
                        newList.Add(stringList[i]);
                    }
                }
            }
            return newList;
        }

        // here

        private static void CopyFiles(string sourceDir, string backUpDir, List<string> files)
        {
            if (files == null || sourceDir == null || backUpDir == null)
            {
                return;
            }

            string sourceFilePath = "";
            string backUpFilePath = "";

            for (int i = 0; i < files.Count; i++)
            {
                sourceFilePath = sourceDir + files[i];
                backUpFilePath = backUpDir + files[i];

                if (!File.Exists(backUpFilePath))
                {
#if BATCH
                    Batch.WriteLine("copy \"" + sourceFilePath + "\" \"" + backUpFilePath + "\"");
#else
                    File.Copy(sourceFilePath, backUpFilePath);
                    LogMessage("File Copied:\t\t\t\t\t\t" + backUpFilePath);
#endif
                    
                }
            }
        }

        private static void CopyModifiedFiles(string sourceDir, string backUpDir, List<string> files)
        {
            string sourceFilePath = "";
            string backUpFilePath = "";

            for (int i = 0; i < files.Count; i++)
            {
                sourceFilePath = sourceDir + files[i];
                backUpFilePath = backUpDir + files[i];

                System.IO.FileInfo sourceFile = new System.IO.FileInfo(sourceFilePath);
                System.IO.FileInfo backUpFile = new System.IO.FileInfo(backUpFilePath);

                if (sourceFile.LastWriteTime > backUpFile.LastWriteTime)
                {
                    try
                    {
#if BATCH
                        Batch.WriteLine("copy \"" + sourceFilePath + "\" \"" + backUpFilePath + "\"");
#else
                        File.Copy(sourceFilePath, backUpFilePath, true);
                        LogMessage("Copied: \t\t" + sourceFilePath + "to \t\t" + backUpFilePath);
#endif
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Failed To Copy: \t\t" + sourceFilePath + "to \t\t" + backUpFilePath);
                        continue;
                    }
                }
            }
        }

        private static void CreateDirectoriesAndCopyFiles(string sourceDirectory, string backUpDirectory, List<string> folders)
        {
            string bDir = "";
            string sDir = "";

            for (int i = 0; i < folders.Count; i++)
            {
                bDir = backUpDirectory + folders[i];

                // Check that each directory in the source dir exists in the back up directory. 
                if (!System.IO.Directory.Exists(bDir))
                {
#if BATCH
                    Batch.WriteLine("mkdir \"" + bDir + "\"");
#else
                    System.IO.Directory.CreateDirectory(bDir);
#endif

                    sDir = sourceDirectory + folders[i];
#if BATCH
                    string line = "copy \"" + sDir + "\\*.*\" \"" + bDir + "\\\"";
                    Batch.WriteLine(line);
#else
                    DirectoryCopy(sDir, bDir, true);
                    LogMessage("Directory and Files Created: \t\t" + bDir);
#endif                    
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
#if BATCH
                Batch.WriteLine("mkdir \"" + destDirName + "\"");
#else
                Directory.CreateDirectory(destDirName);
                LogMessage("Directory Created: \t\t\t\t\t" + destDirName);
#endif
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);

#if BATCH
                string line = "copy \"" + file.FullName + "\\*.*\" \"" + temppath + "\\\"";
                Batch.WriteLine(line);
#else
                file.CopyTo(temppath, false);
                LogMessage("File copied: \t\t\t\t\t\t" + temppath);

#endif
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
#if BATCH
                    string line = "copy \"" + subdir.FullName + "\\*.*\" \"" + temppath + "\"";
                    Batch.WriteLine(line);
#else
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                    LogMessage("Directory and Files Created: \t\t" + temppath);
#endif
                }
            }
        }

        private static void RemoveAnyLastSlashes(ref string[] listOfDirectories)
        {
            if (listOfDirectories == null)
            {
                return;
            }

            string line;
            int indexSlash;
            char slash = '\\';

            // Strip that last slash.
            for (int i = 0; i < listOfDirectories.Length; i++)
            {
                line = listOfDirectories[i];
                indexSlash = line.LastIndexOf(slash);

                if (indexSlash > 0)
                {
                    if ((line.Length - 1) == indexSlash)
                    {
                        listOfDirectories[i] = line.Remove(indexSlash);
                    }
                }
            }
        }   

        private static void LogMessage( string message )
        {
            if( Log == null )
            {
                return;
            }

            Log.WriteLine(DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString() + "\t\t" + message);
        }
    }
}
