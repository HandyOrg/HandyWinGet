namespace WinGet_GUI.Models
{
    public class ApplicationData
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }

        public override string ToString()
        {
            return $"{Name} {Version} ({Id})";
        }
    }
}
