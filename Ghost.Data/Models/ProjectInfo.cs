using System.ComponentModel.DataAnnotations.Schema;

namespace Ghost.Data.Models;

internal class ProjectInfo
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID
    {
        get; internal set;
    }

    public required string Name
    {
        get; set;
    }

    public required string MetadataPath
    {
        get; set;
    }
}