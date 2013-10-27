namespace HangFire.Tests
{
    public class CustomJob : BackgroundJob
    {
        public static int LastArticleId;
        public static string LastAuthor;

        public int ArticleId { get; set; }
        public string Author { get; set; }
        
        public override void Perform()
        {
            LastArticleId = ArticleId;
            LastAuthor = Author;
        }
    }
}
