namespace HandyWinget.Database.Tables
{
    public class PathPartCte
    {
        public long rowid { get; set; }
        public long? parent { get; set; }
        public string path { get; set; }
    }
}
