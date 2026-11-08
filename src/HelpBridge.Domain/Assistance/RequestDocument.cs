using HelpBridge.Domain.Common;
using HelpBridge.Domain.Enums;

namespace HelpBridge.Domain.Assistance;

public class RequestDocument : BaseEntity
{
    public Guid RequestId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string BlobPath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Checksum { get; set; } = string.Empty;

    public AssistanceRequest Request { get; set; } = null!;
}

