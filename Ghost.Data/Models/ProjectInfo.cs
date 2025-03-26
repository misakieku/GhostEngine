using System.ComponentModel.DataAnnotations.Schema;

namespace Ghost.Data.Models;

public class ProjectInfo
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID
    {
        get; set;
    }

    public required string Name
    {
        get; set;
    }

    public required string Path
    {
        get; set;
    }

    public required Version EngineVersion
    {
        get; set;
    }

    public required DateTime LastOpened
    {
        get; set;
    }
}