namespace Configuration.Models
{
    public class TaskMessage
    {
        public int Id { get; set; }

        public string Task { get; set; }

        public int Precision { get; set; }

        public bool IsFirst { get; set; }
    }
}
