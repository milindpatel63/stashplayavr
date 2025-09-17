namespace PlayaApiV2.Model
{
    public class AppOptions
    {
        public const string SectionName = "App";

        /// <summary>
        /// If true, shows all actors regardless of scene count. If false, only shows actors with at least 1 scene.
        /// Default: false (only actors with scenes)
        /// </summary>
        public bool ShowAllActors { get; set; } = false;

        /// <summary>
        /// If true, sorts by rating for popularity. If false, sorts by o_counter for popularity.
        /// Default: false (sort by o_counter)
        /// </summary>
        public bool SortPopularityByRating { get; set; } = false;

        /// <summary>
        /// Host address for the application to bind to.
        /// Default: "0.0.0.0" (bind to all interfaces)
        /// </summary>
        public string Host { get; set; } = "0.0.0.0";

        /// <summary>
        /// Port number for the application to listen on.
        /// Default: 8890
        /// </summary>
        public int Port { get; set; } = 8890;

        /// <summary>
        /// External URL for the application when behind a reverse proxy (like Traefik).
        /// This is used to generate proper URLs for clients.
        /// Default: null (uses internal host/port)
        /// </summary>
        public string ExternalUrl { get; set; } = null;
    }
}
