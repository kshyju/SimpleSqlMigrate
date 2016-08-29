using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSqlMigrate
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter the script location path");
            var path = Console.ReadLine();
            Console.WriteLine("Please enter the connection string");
            var connectionString = Console.ReadLine();
            Console.WriteLine("Starting migration...");
            SimpleSqlMigrate.Run(path, connectionString);
            Console.WriteLine("Finished migration.");
            Console.ReadLine();
        }
    }

    public class SimpleSqlMigrate
    {
        const string MigrationTblName = "__DbMigrations";
        const string CreateMigrationTableSql =
            @"if OBJECT_ID('{0}') is null 
begin

CREATE TABLE [dbo].[{0}](
	[Id] [int] IDENTITY(1,1) primary key NOT NULL,
	[Filename] [nvarchar](250) NULL,
	[Content] [nvarchar](max) NULL,
	[ExecutionStartTime] [datetime] NULL,
	[ExecutionEndTime] [datetime] NULL,
)
end";


        public static void Run(string scriptLocationPath, string connectionString)
        {

            CreateMigrationTable(connectionString);

            if (!Directory.Exists(scriptLocationPath))
                throw new ArgumentException("script location does not exist!");

            string[] fileEntries = Directory.GetFiles(scriptLocationPath);
            var sql = new List<MigrationItem>();
            foreach (var fileName in fileEntries.OrderBy(g => g))
            {
                var fileContent = File.ReadAllText(fileName);

                var c = new MigrationItem { FileContent = fileContent, FileName = Path.GetFileName(fileName) };
                sql.Add(c);
            }
            ExecuteSql(connectionString, sql);

        }

        private class MigrationItem
        {
            public string FileName { get; internal set; }
            public string FileContent { get; internal set; }
            public DateTime ExecutionStartTime { get; internal set; }
            public DateTime ExecutionEndTime { get; internal set; }
        }

        private static void SaveMigration(MigrationItem item, string connectionString)
        {
            using (var con = new SqlConnection(connectionString))
            {
                con.Open();
                var insertQry = String.Format("INSERT INTO {0} (FileName,Content,ExecutionStartTime,ExecutionEndTime) VALUES (@fileName,@content,@startTime,@endTime);", MigrationTblName);

                using (var cmd = new SqlCommand(insertQry, con))
                {
                    cmd.Parameters.AddWithValue("@fileName", item.FileName);
                    cmd.Parameters.AddWithValue("@content", item.FileContent);
                    cmd.Parameters.AddWithValue("@startTime", item.ExecutionStartTime);
                    cmd.Parameters.AddWithValue("@endTime", item.ExecutionEndTime);

                    cmd.ExecuteNonQuery();
                }

            }
        }

        private static void CreateMigrationTable(string connectionString)
        {
            var c = new MigrationItem { FileContent = String.Format(CreateMigrationTableSql, MigrationTblName) };
            ExecuteSql(connectionString, new List<MigrationItem> { c });
        }

        private static void ExecuteSql(string connectionString, IEnumerable<MigrationItem> sqlList)
        {
            SqlTransaction trans = null;
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    trans = con.BeginTransaction();
                    foreach (var item in sqlList)
                    {
                        item.ExecutionStartTime = DateTime.UtcNow;
                        using (var cmd = new SqlCommand(item.FileContent, con, trans))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        item.ExecutionEndTime = DateTime.UtcNow;
                        if (!String.IsNullOrEmpty(item.FileName))
                            SaveMigration(item, connectionString);
                    }
                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                if (trans != null)
                    trans.Rollback();
                Trace.TraceError(ex.Message);
                throw;
            }

        }

    }
}
