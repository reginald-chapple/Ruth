namespace Ruth.Providers.LiveKit;

/// <summary>Strongly-typed configuration options for the Apache-2.0 LiveKit server and RustFS S3 egress provider.</summary>
public sealed class LiveKitOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "VirtualRooms:LiveKit";

    /// <summary>Gets or sets whether real HTTP Twirp calls to LiveKit server are enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the internal LiveKit Twirp HTTP base URL.</summary>
    public string ServerUrl { get; set; } = "http://localhost:7880";

    /// <summary>Gets or sets the browser-reachable WebSocket signaling URL.</summary>
    public string PublicWebSocketUrl { get; set; } = "ws://localhost:7880";

    /// <summary>Gets or sets the LiveKit API key.</summary>
    public string ApiKey { get; set; } = "devkey";

    /// <summary>Gets or sets the LiveKit HMAC-SHA256 signing secret (minimum 32 characters recommended).</summary>
    public string ApiSecret { get; set; } =
        "replace-with-a-unique-random-32-plus-character-livekit-secret";

    /// <summary>Gets or sets the RustFS S3 endpoint passed to LiveKit Egress.</summary>
    public string S3Endpoint { get; set; } = "http://rustfs:9000";

    /// <summary>Gets or sets the RustFS S3 bucket name for archived MP4 recordings.</summary>
    public string S3Bucket { get; set; } = "ruth";

    /// <summary>Gets or sets the RustFS S3 access key.</summary>
    public string S3AccessKey { get; set; } = "rustfsadmin";

    /// <summary>Gets or sets the RustFS S3 secret key.</summary>
    public string S3SecretKey { get; set; } = "rustfsadmin";
}
