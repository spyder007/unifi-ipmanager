namespace unifi.ipmanager.Models.Unifi
{
    public class UniNote
    {
        public bool? Set_on_device { get; set; }
        public string Dns_hostname { get; set; }

        public bool? Sync_dnshostname { get; set; }

        public void Update(UniNote notes)
        {
            if (notes?.Set_on_device != null)
            {
                Set_on_device = notes.Set_on_device;
            }

            if (notes?.Sync_dnshostname != null)
            {
                Sync_dnshostname = notes.Sync_dnshostname;
            }

            if (notes?.Dns_hostname != null)
            {
                Dns_hostname = notes.Dns_hostname;
            }
        }
    }
}
