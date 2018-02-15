using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ConsoleApp.SQLite
{
    public class FileHashContext : DbContext
    {
        public DbSet<UbuntuFile> UbuntuFileSet { get; set; }
        public DbSet<DirectoryStatus> DirectoryStatusSet { get; set; }

        public DbSet<GlobalEnvironment> GlobalEnvironment {get; set;}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=Document5.db");
        }
    }

    public class UbuntuFile
    {
        [Key]
        public string filePath { get; set; }
        public long fileSize { get; set; }
        public string fileHash { get; set; }
        public string fileName { get; set; }
        public string fileStatus { get; set; }

        public UbuntuFile()
        {
            
        }
        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4}",filePath, fileSize, fileHash, fileName);
        }
        public UbuntuFile(string filePath, long fileSize, string fileHash, string fileName)
        {
            this.fileHash = fileHash;
            this.filePath = filePath;
            this.fileSize = fileSize;
            this.fileName = fileName;
            this.fileStatus="UnProcessed";
        }
    }
    public enum eDirStatus
    {
        INSIDE, 
        PROCESSING, 
        FINISH
    };
    public class DirectoryStatus
    {
        [Key]
        public string Path { get; set; }
        public string Name { get; set; }
        public string Direction { get; set; }
        public eDirStatus Status { get; set; }
        public long Level { get; set; }
        public DirectoryStatus(string name, string direction,long level)
        {
            Path = name;
            Name = name.Substring(name.LastIndexOf("/")+1);
            Direction = direction;
            Status = eDirStatus.INSIDE;
            Level = level;
        }
        public DirectoryStatus()
        {
            
        }
    }

    public class GlobalEnvironment
    {
        [Key]
        public string key { get; set; }
        public string value { get; set; }

        public GlobalEnvironment(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
        public GlobalEnvironment()
        {
            
        }
    }
    
}