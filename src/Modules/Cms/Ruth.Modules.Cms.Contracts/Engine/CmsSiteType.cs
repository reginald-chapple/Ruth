namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Target channel type for a CMS Site.</summary>
public enum CmsSiteType
{
    /// <summary>Standard external or member-portal web site.</summary>
    Web = 0,

    /// <summary>Native mobile shell backing site.</summary>
    Mobile = 1,

    /// <summary>Digital signage / Apple TV / Roku display site.</summary>
    Tv = 2,
}
