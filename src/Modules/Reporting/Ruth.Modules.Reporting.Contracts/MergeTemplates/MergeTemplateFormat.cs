namespace Ruth.Modules.Reporting.Contracts.MergeTemplates;

/// <summary>Output document format for a MergeTemplate.</summary>
public enum MergeTemplateFormat
{
    /// <summary>Standalone rendered HTML document.</summary>
    Html = 0,

    /// <summary>OpenXML WordprocessingML (.docx) document package.</summary>
    Docx = 1,
}
