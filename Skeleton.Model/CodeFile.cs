namespace Skeleton.Model
{
    public class CodeFile
    {
        public string Name { get; set; }

        public string Contents { get; set; }

        public string RelativePath { get; set; }

        public bool IsFragment { get; set; }
        
        public string Template { get; set; }
    }
}
