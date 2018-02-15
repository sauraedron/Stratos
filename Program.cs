using System;
using System.IO;
using ConsoleApp.SQLite;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Stratos_alpha
{
    class Program
    {
        static string whiteList;

        static void Stage1_Map_Directories_pass(long globalLevel, string entryPoint)
        {
            try
            {
                //Get Root Dirs and Persist them
                string[] dirs = Directory.GetDirectories(entryPoint);
                using (var db = new FileHashContext())
                {
                    for (int i = 0; i < dirs.Length; ++i)
                    {
                        db.DirectoryStatusSet.Add(new DirectoryStatus(dirs[i].ToString(), "Inwards", globalLevel));
                    }
                    var count = db.SaveChanges();
                    /*End Level 1 */

                    /* For each level Map all Dirs */
                    long DIRSREAD = 1;
                    bool moreDirs = true;

                    while (moreDirs)
                    {
                        while (DIRSREAD != 0)
                        {
                            DIRSREAD = 0;
                            var CurrentTraversingDir = db.DirectoryStatusSet.Where(p => p.Level == globalLevel && p.Path.Contains(entryPoint));
                            moreDirs = false;
                            foreach (var SubTraversingDir in CurrentTraversingDir)
                            {
                                moreDirs = true;
                                string[] subDirs = Directory.GetDirectories(SubTraversingDir.Path);
                                for (int i = 0; i < subDirs.Length; ++i)
                                {
                                    db.DirectoryStatusSet.Add(new DirectoryStatus(subDirs[i].ToString(), "Inwards", SubTraversingDir.Level + 1));
                                    DIRSREAD++;
                                }
                                if (SubTraversingDir == CurrentTraversingDir.Last())
                                    db.SaveChangesAsync();
                                if (!moreDirs)
                                    break;
                            }
                            /*Save every 50K entries */
                            if (DIRSREAD > 50000)
                                db.SaveChangesAsync();
                            globalLevel++;
                        }
                        if (DIRSREAD == 0)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message.ToString());
            }
        }
        private static void Stage2_GetFileInfo()
        {
            long FILESREAD = 0;
            using (var db = new FileHashContext())
            {

                var MappedDirectories = db.DirectoryStatusSet;
                foreach (var localDirectory in MappedDirectories.OrderBy(p => p.Level))
                {
                    localDirectory.Status = eDirStatus.PROCESSING;
                    localDirectory.Direction = "Processing";
                    System.IO.DriveInfo locldri = new DriveInfo(localDirectory.Path);
                    var filesUnderLocalDirectory = locldri.RootDirectory.GetFiles();
                    foreach (var singleFile in filesUnderLocalDirectory)
                    {
                        if (db.UbuntuFileSet.Where(p => p.fileName != singleFile.FullName).Count() == 0)
                            db.UbuntuFileSet.Add(new UbuntuFile(singleFile.FullName, singleFile.Length, "XX", singleFile.Name));

                    }
                    localDirectory.Direction = "Outwards";
                    FILESREAD++;
                    if (localDirectory == MappedDirectories.Last())
                        db.SaveChanges();
                }
                /*Save every 50K to avoid DB update overhead */
                if (FILESREAD > 50000)
                {
                    FILESREAD = 0;
                    db.SaveChanges();
                }
            }
        }
        private static void Stage3_calculateHash()
        {
            using (var db = new FileHashContext())
            {
                //For all those files where duplicate exists. Queue them up for Hashing.
                var hashValueCandidates = db.UbuntuFileSet.FromSql("select * from ubuntufileSet where fileSize in (select fileSize from ubuntufileset group by filesize having count(*) >1) and filesize>0 and fileStatus!='Hashed' order by  filesize");
                int fileUpdated = 0;
                //using (StreamWriter sw = new StreamWriter("asdf"))
                {
                    foreach (var fileCandidate in hashValueCandidates)
                    {
                        //sw.WriteLine(a.fileName + " : " + a.fileStatus);
                        string ComputedHash;
                        using (var md5obj = MD5.Create())
                        {
                            using (var b = File.OpenRead(fileCandidate.filePath))
                            {
                                ComputedHash = BitConverter.ToString(md5obj.ComputeHash(b));
                            }
                        }
                        fileCandidate.fileHash = ComputedHash;
                        fileCandidate.fileStatus = "Hashed";
                        fileUpdated++;
                        /*Save every 500 files. Hash Calculation is costly. Hence need more grainer updates */
                        if (fileUpdated > 500 || fileCandidate == hashValueCandidates.Last())
                            db.SaveChanges();
                    }

                }
            }

        
        }
        private static void Stage4_Generate_ShellFile()
        {
            using (var db = new FileHashContext())
            {
                string scratchDisk = @"/home/livestream/Documents/ScratchDisk";
                var hashedFile = db.UbuntuFileSet.FromSql("select filepath, (fileHash), fileName, fileSize,fileStatus from ubuntufileset where fileHash!='XX' and fileStatus='Hashed' group by fileHash");
                StringBuilder sb = new StringBuilder("");
                foreach (var singleEntry in hashedFile)
                {
                    if (!isWhiteListedExtension(singleEntry.fileName))
                        continue;
                    var entiresMatchingAParticularHash = db.UbuntuFileSet.Where(p => p.fileHash == singleEntry.fileHash);
                    if (entiresMatchingAParticularHash.Count() > 1)
                    {
                        /*Todo: reduce the size of paths */
                        string[] jj = singleEntry.filePath.Split(@"/");
                        StringBuilder path = new StringBuilder();
                        for (int i = 3; i < jj.Length - 1; ++i)
                            path.Append(@"/" + jj[i]);
                        foreach (var d in entiresMatchingAParticularHash)
                        {

                            if (d == entiresMatchingAParticularHash.First())
                            {
                                d.fileStatus = "Kept";
                                sb.Append("mkdir -p \"" + scratchDisk + path.ToString() + "\"");

                                //System.Console.WriteLine(d.filePath.Substring(0, d.filePath.LastIndexOf(@"/"))+"\"");
                                sb.Append("\nmv \"" + d.filePath + "\" " + " \"" + scratchDisk + "" + path.ToString() + "/" + d.fileName + "\"\n");
                            }

                            else
                            {
                                d.fileStatus = "Removed";
                                sb.Append("rm \"" + d.filePath + "\"\n");
                            }
                        }
                    }
                }
                db.SaveChanges();
                /*Generate a Shell file for copying to Scratch disk */
                using (StreamWriter sw = new StreamWriter("fileOps.sh"))
                {
                    sw.WriteLine(sb.ToString());
                }
            }
        }
        static void Main(string[] args)
        {
            var startTime = DateTime.Now;
            string entryPoint = @"/home/livestream/Documents";
            /*Set WhiteList at start */
            using (var db = new FileHashContext())
            {
                var results = db.GlobalEnvironment.Where(p => p.key == "whiteList");
                foreach (var a in results)
                {
                    if (a.key == "whiteList")
                        whiteList = a.value;
                }
            }
            /*Insert once */
            using (var db = new FileHashContext())
            {
                if (db.GlobalEnvironment.Where(p => p.key == "whiteList").Count() == 0)
                {
                    db.GlobalEnvironment.Add(new GlobalEnvironment("whiteList", "mp4,webm,mp3"));
                    db.SaveChangesAsync();
                }
            }
            /*string entryPoint = @"/media/livestream/9CBEB71EBEB6F03E"; --Probelmatic*/
            /*/media/livestream/B0AEADADAEAD6D12 - 2nd drive 215Gb */
            using (StreamWriter sw = new StreamWriter(@"logger.txt", true))
            {
                sw.WriteLine(DateTime.Now.ToString());
            }
            long globalLevel = 0;
            /*Level 1 */
            Stage1_Map_Directories_pass(globalLevel, entryPoint);
            using (StreamWriter sw = new StreamWriter(@"logger.txt", true))
            {
                sw.WriteLine(DateTime.Now.ToString());
            }
            /*Level 2 */
            Stage2_GetFileInfo();

            Stage3_calculateHash();

            Stage4_Generate_ShellFile();
            var endTime = DateTime.Now;
            System.Console.WriteLine("{0}", endTime - startTime);
        }
        

        private static bool isWhiteListedExtension(string fileName)
        {
            string[] extension = fileName.Split(".");
            if (extension.Length == 0)
                return false;
            string ext = extension[extension.Length - 1];

            if (whiteList.Contains(ext))
                return true;
            return false;


        }

    }
}

