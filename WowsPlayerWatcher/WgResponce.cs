namespace WowsPlayerWatcher
{
    public class WgResponce<T>
    {
        public T Data { get; set; }
        public string status { get; set; }
        public Meta meta { get; set; }
    }
    public class Meta
    {
        public int count { get; set; }
        public object? hidden { get; set; }
    }
}
