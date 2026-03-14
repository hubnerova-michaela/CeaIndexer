namespace CeaIndexer
{
    public class Quantity
    {
        public int Id { get; set; }
        public int FileEntryId { get; set; }
        public FileEntry FileEntry { get; set; } = null!;
        public string Name { get; set; } = "";
        public string Archive { get; set; } = "";
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        public double? AverageValue { get; set; }
    }
}
