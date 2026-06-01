namespace api.Interfaces
{
    public class CreateOrUpdatePostInterface
    {
        public string? title { get; set; }
        public string? message { get; set; }
        public string? creator { get; set; }
        public string? selectedFiles { get; set; }

    }

    public class CommentBodyInterface
    {
        public string? postId { get; set; }
        public string? comment { get; set; }
        public string? userId { get; set; }
    }
}
