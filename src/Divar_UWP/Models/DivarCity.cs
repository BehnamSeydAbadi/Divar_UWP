namespace Divar_UWP.Models
{
    public sealed class DivarCity
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Slug { get; set; }

        public string ParentId { get; set; }

        public bool IsTopCity { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
