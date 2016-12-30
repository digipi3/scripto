# scripto


A script for backing up only new and modified files


This script requires a source directory and a backup directory.

The source directory is checked for any new directories and new files. These are created in the backup directory if they don't already exist.

The source directory is also checked for files that have been written to more recently than files in the backup directory.The backup directory files are replaced with the more recent source directory files.