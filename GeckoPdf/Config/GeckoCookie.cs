namespace GeckoPdf.Config
{
    public class GeckoCookie
    {
        public string Path { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public bool Secure { get; set; }

        public bool HttpOnly { get; set; }

        public long ExpiresUnix { get; set; }
    }
}
