namespace BibleVerseLookupApp
{
    /// <summary>
    /// 查询经文结果
    /// </summary>
    public class QbResult

    {
        public string status;
        public string nstrunv;
        public int record_count;
        public int proc;
        public Record[] record;

        public class Record
        {
            public string chineses;
            public int sec;
            public string bible_text;
        }
    }
}
