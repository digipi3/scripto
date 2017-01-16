//#define TESTING
#define ACTION

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
        static StreamWriter Log = new StreamWriter("log.txt", false);
#if BATCH
        static StreamWriter Batch = new StreamWriter("batch.txt", true);
#endif 

        static void Main(string[] args)
        {
            List<string> directoriesToIgnore = null;

            if( CheckArguments(args) == false )
            {
                LogMessage("There's an issue with the arguments");
                return;
            }

            if ( ! Directory.Exists( args[0] ) )
            {
                LogMessage("Source directory doesn't exist " + args[0]);
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

            if (args.Length > 2)
            {
                if (args[2] != null)
                {
                    try
                    {
                        directoriesToIgnore = ExtractDirectoriesToIgnore(args[2]);
                        List<string> nonExistDir = GetNonExistentDirectoriesOrFiles(directoriesToIgnore);

                        if( nonExistDir.Count > 0 )
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Error dealing with ignore file" + ex.ToString());
                        return;
                    }
                }
            }

            // Get all the files from source and remove files in the ignore list:
            List<string> files =
                System.IO.Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories)
                .ToList<string>();

            // Get all the directories from source:
            List<string> directories = 
                System.IO.Directory.GetDirectories(sourceDir, "*.*", System.IO.SearchOption.AllDirectories)
                .ToList<string>();

            if ( files.Count < 1 )
            {
                LogMessage("No files in the source directory?!");
                Log.Close();
                return;
            }          

            LogMessage("Start the fans please");

            if ( directoriesToIgnore.Count > 0 )
            {
                // Remove Directories that we want to ignore:
                directories = RemoveStringsFromStringList(directories, directoriesToIgnore);
                files = RemoveStringsFromStringList(files, directoriesToIgnore);
            }

            CreateDirectories(sourceDir, backUpDir, directories); 

            List<string> paths = RemoveStringFromStringList(sourceDir, files);     
                            
            CopyFiles(sourceDir, backUpDir, paths);         
            CopyModifiedFiles(sourceDir, backUpDir, paths );

            LogMessage("Done");

            Log.Close();
#if BATCH
            Batch.Close();
#endif
        }

        private static bool CheckArguments( string [] args )
        {
            if (args == null)
            {
                return false;
            }
            if (args.Length < 2)
            {
                Log.Close();
                return false;
            }

            if (args[0] == null)
            {
                Log.Close();
                return false;
            }

            if (args[1] == null)
            {
                Log.Close();
                return false;
            }

            //Sometimes directory path will be passed to the program using double quotes
            if (args[0].Contains("\""))
            {
                args[0] = args[0].Replace("\"", "");
            }

            //Sometimes a directory path will be passed to the program using single and double quotes
            if (args[1].Contains("\""))
            {
                args[1] = args[1].Replace("\"", "");
            }

            return true;
        }

        private static void CreateDirectories(string sourceDir,string backUpDir, List<string> directories)
        {
            if( directories == null || sourceDir == null || backUpDir == null )
            {
                return;
            }

            if( directories.Count < 1 )
            {
                return;
            }

            // Remove root part of the source directory from each directory path
            List<string> folders = RemoveStringFromStringList(sourceDir, directories);

            // Create Directories
            string directory;

            for (int i = 0; i < folders.Count; i++)
            {
                directory = backUpDir + folders[i];

                // Check that each directory in the source dir exists in the back up directory. 
                if (!System.IO.Directory.Exists(directory))
                {
#if ACTION
                    System.IO.Directory.CreateDirectory(directory);
#endif
                    LogMessage("Create Directory:" + directory);
                }
            }
        }       

        private static List<string> GetNonExistentDirectoriesOrFiles( List<string> directories )
        {
            List<string> nonExist = new List<String>();

            for (int i = 0; i < directories.Count; i++)
            {
                if (! Directory.Exists(directories[i]) )
                {
                    if (! File.Exists(directories[i]) )
                    {
                        nonExist.Add(directories[i]);
                    }
                }
            }

            return nonExist;
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

        private static List<string> RemoveStringsFromStringList(List<string> stringList, List<string> stringsToRemove)
        {
            // At this point we can remove the source directories that are to be ignored.

            List<string> newList = new List<string>();
            bool equalToAny = false;
            List<string> ignored = new List<string>(); // For Testing.

            for (int i = 0; i < stringList.Count; i++)
            {
                equalToAny = false;
                for (int j = 0; j < stringsToRemove.Count; j++)
                {
                    if ( stringList[i].Contains( stringsToRemove[j] ) )
                    {
                        equalToAny = true;
                        ignored.Add(stringList[i]);
                        break;
                    } 
                }
                if (equalToAny == false)
                {
                    newList.Add(stringList[i]);
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
#if ACTION
                    File.Copy(sourceFilePath, backUpFilePath, true );
#endif
                    LogMessage("File Copied: " + backUpFilePath);
                    
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
#if ACTION
                        File.Copy(sourceFilePath, backUpFilePath, true);
#endif
                        LogMessage("Copied: " + sourceFilePath + " to " + backUpFilePath);

                    }
                    catch (Exception ex)
                    {
                        LogMessage("Failed To Copy: " + sourceFilePath + "to " + backUpFilePath);
                        continue;
                    }
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

            Log.WriteLine(DateTime.UtcNow.ToString() + "\t\t" + message);
            Console.WriteLine(DateTime.UtcNow.ToString() + "\t\t" + message);
        }
    }
}
