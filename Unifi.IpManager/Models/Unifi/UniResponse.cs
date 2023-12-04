namespace unifi.ipmanager.Models.Unifi
{
    public class UniResponse<TReturnType>
    {
        public UniMeta Meta { get; set; }

        public TReturnType Data { get; set; }
    }
}
