namespace Round
{
    public enum WordType { Name, Profession, Action, Subject }

    [System.Serializable]
    public class WordEntry
    {
        public string id;
        public string title;
        public WordType type;
    }
}