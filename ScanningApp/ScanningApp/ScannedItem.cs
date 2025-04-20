using System;
using System.Collections.Generic;
using System.Text;

namespace ScanningApp
{
   public class ScannedItem
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }

        public string Code { get; set; }
    }
}
