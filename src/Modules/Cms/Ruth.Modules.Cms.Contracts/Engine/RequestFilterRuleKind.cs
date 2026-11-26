namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Kind of HTTP or visitor signal inspected by a RequestFilterRule.</summary>
public enum RequestFilterRuleKind
{
    /// <summary>Matches a query string parameter value.</summary>
    QueryString = 0,

    /// <summary>Matches a request cookie value.</summary>
    Cookie = 1,

    /// <summary>Matches an HTTP request header value.</summary>
    Header = 2,

    /// <summary>Matches a client IP address or CIDR prefix.</summary>
    IpAddressRange = 3,

    /// <summary>Matches a normalized device class derived from User-Agent (Desktop, Mobile, Tablet, Tv).</summary>
    UserAgentDevice = 4,

    /// <summary>Matches current UTC time of day against a HH:mm-HH:mm window.</summary>
    TimeOfDay = 5,

    /// <summary>Matches current UTC day of week (e.g. Monday, Tuesday).</summary>
    DayOfWeek = 6,

    /// <summary>Matches a visitor environment/host domain attribute.</summary>
    HostDomain = 7,
}
