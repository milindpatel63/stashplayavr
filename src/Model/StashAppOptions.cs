namespace PlayaApiV2.Model
{
    public class StashAppOptions
    {
        public const string SectionName = "StashApp";
        
        public string Url { get; set; }
        public string GraphQLUrl { get; set; }
        public string ApiKey { get; set; }
    }
}
