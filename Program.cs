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
    /* Important notes:
     * 
     * Directories are dealth with in the following format:
     * C:\\Movies and not C:\\Movies\\ so the last slash will be removed.
     */
    class Program
    {
        static StreamWriter Log = new StreamWriter("log.txt", true);

        static void Main(string[] args)
        {
#if TESTING
            args = new string[3];
            args[0] = "C:\\src";
            args[1] = "C:\\des";
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
                    LogMessage("Error opening ignore file" + ex.ToString());
                    Log.Close();
                    return;
                }

            }


            String sourceDir = args[0];
            String backUpDir = args[1];

            LogMessage("Backup is about to begin");

            // Get all the directories in the source directory
            List<string> sourceDirectories = GetAllTheDirectories(sourceDir);

            // Ignore any directories that have been specified by the user

            if (directoriesToIgnore != null && directoriesToIgnore.Length > 0)
            {
                sourceDirectories = RemoveStringsFromStringist(sourceDirectories, directoriesToIgnore);
            }

            // Generate backup directory paths using the source paths
            List<string> directoriesThatShouldExist = GenerateBackupDirFromSourceDir(sourceDirectories, sourceDir, backUpDir);

            // Next the directories that don't exist in back-up need to be created and their files copied?!
            CreateDirectoriesAndCopyFiles(sourceDirectories, directoriesThatShouldExist);

            // Copy all new files           
            // Get all the files in the source directory:          

            //CopyNewFiles(sourceDir, backUpDir);
            CopyNewFilesToBackup(sourceDir, backUpDir, directoriesToIgnore.ToList<string>() );

            // Copy Files that have been modified more recently:
            CopyModifiedFiles(sourceDir, backUpDir);

            Log.Close();
        }

        private static void CopyModifiedFiles(string sourceDir, string backUpDir)
        {
            string sourceFilePath = "";
            string backUpFilePath = "";

            // Now to check for files that have been modified more recently.
            string[] sourceFiles = System.IO.Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories);
            ArrayList filesToCopy = new ArrayList();

            for (int i = 0; i < sourceFiles.Length; i++)
            {
                sourceFilePath = sourceFiles[i];
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
                        LogMessage("Failed To Copy: \t\t" + sourceFilePath + "to \t\t" + backUpFilePath);
                        continue;
                    }
                }
            }
        }

        private static List<string> RemoveFilesFromExemptDirectories( List<string> files, List<string> exemptList )
        {
            ArrayList newFiles = new ArrayList();

            for( int i = 0; i < files.Count; i++ )
            {
                for (int j = 0; j < exemptList.Count; j++)
                {
                    if (files[i].Contains( exemptList[j] ) == false)
                    {
                        newFiles.Add(files[i]);
                    }
                }
            }

            List<string> filesCleaned = new List<string>();

            filesCleaned = newFiles.Cast<string>().ToList();

            return filesCleaned;
        }

        private static void CopyNewFilesToBackup( string sourceDir, string backUpDir, List<string> exemptList )
        {
            List<string> allSourceFiles = System.IO.Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories).ToList<string>();

            allSourceFiles = RemoveFilesFromExemptDirectories(allSourceFiles, exemptList);

            // Copy new files that don't exist in the back-up because we've only copied new 
            // files that are in new directories.
            string sourceFilePath = "";
            string backUpFilePath = "";

            for (int i = 0; i < allSourceFiles.Count; i++)
            {
                sourceFilePath = allSourceFiles[i];
                backUpFilePath = ConvertSourcePathToBackUpPath(sourceFilePath, sourceDir, backUpDir);

                if (!File.Exists(backUpFilePath))
                {
                    File.Copy(sourceFilePath, backUpFilePath);
                    LogMessage("File Copied:\t\t" + backUpFilePath);
                }
            }
        }

        private static void CopyNewFiles( string sourceDir, string backUpDir )
        {
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
                    LogMessage("File Copied:\t\t" + backUpFilePath);
                }
            }
        }

        private static void CreateDirectoriesAndCopyFiles( List<string> srcDir, List<string> backUpDir )
        {
            string backUp = "";
            string src = "";

            for (int i = 0; i < backUpDir.Count; i++)
            {
                backUp = backUpDir[i];

                if (!System.IO.Directory.Exists(backUp))
                {
                    System.IO.Directory.CreateDirectory(backUp);

                    // This is a new directory so all the files will be new.
                    // Copy time.
                    src = srcDir[i];
                    DirectoryCopy(src, backUp, true);
                    LogMessage("Directory and Files Created: \t\t " + backUp);
                }
            }
        }

        private static List<string> GenerateBackupDirFromSourceDir(List<string> sourceDirectories, string sourceDir, string backUpDir )
        {
            int index = 0;
            System.Collections.ArrayList directoriesThatShouldExist = new System.Collections.ArrayList();

            for (int i = 0; i < sourceDirectories.Count; i++)
            {
                index = sourceDirectories[i].IndexOf(sourceDir);

                if (index == -1)
                {
                    continue;
                }

                string directory = sourceDirectories[i].Remove(index, backUpDir.Length);

                string directoryInBackup = backUpDir + directory;

                directoriesThatShouldExist.Add(directoryInBackup);
            }

            return directoriesThatShouldExist.Cast<string>().ToList();
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

                RemoveAnyLastSlashes( ref lines );
            }
            catch
            {
                throw;
            }

            return lines;
        }

        private static void RemoveAnyLastSlashes(ref string[] listOfDirectories)
        {
            if( listOfDirectories == null )
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
                    if ( ( line.Length - 1) == indexSlash)
                    {
                        listOfDirectories[i] = line.Remove(indexSlash);
                    }
                }
            }
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
