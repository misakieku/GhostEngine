using Ghost.Database.Models.Projects;
using System.Data.SQLite;

namespace Ghost.Database.DataContext;

public class ProjectRepository
{
    private const string _CONNECTION_STRING = "Data Source={0}/projects.db;Version=3;";
    private const string _CREATE_PROJECT_TABLE_STRING = "CREATE TABLE IF NOT EXISTS Projects (ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Path TEXT, EngineVersion TEXT, LastOpened DATETIME);";
    private const string _SELECT_PROJECT_STRING = "SELECT * FROM Projects";
    private const string _INSERT_PROJECT_STRING = "INSERT INTO Projects (Name, Path, EngineVersion, LastOpened) VALUES (@Name, @Path, @EngineVersion, @LastOpened);";

    private static string GetConnectionString() => string.Format(_CONNECTION_STRING, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    private static void EnsureTableCreated(SQLiteConnection connection)
    {
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = _CREATE_PROJECT_TABLE_STRING;
        createCommand.ExecuteNonQuery();
    }

    public static IEnumerable<ProjectInfo> LoadProjects()
    {
        using var connection = new SQLiteConnection(GetConnectionString());
        connection.Open();

        EnsureTableCreated(connection);

        using var command = connection.CreateCommand();
        command.CommandText = _SELECT_PROJECT_STRING;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var project = new ProjectInfo
            {
                Name = reader.GetString(0),
                Path = reader.GetString(1),
                EngineVersion = new Version(reader.GetString(2)),
                LastOpened = reader.GetDateTime(3)
            };

            yield return project;
        }
    }

    public static void AddProject(ProjectInfo project)
    {
        using var connection = new SQLiteConnection(GetConnectionString());
        connection.Open();

        EnsureTableCreated(connection);

        using var command = connection.CreateCommand();
        command.CommandText = _INSERT_PROJECT_STRING;

        command.Parameters.AddWithValue("@Name", project.Name);
        command.Parameters.AddWithValue("@Path", project.Path);
        command.Parameters.AddWithValue("@EngineVersion", project.EngineVersion.ToString());
        command.Parameters.AddWithValue("@LastOpened", project.LastOpened);
        command.ExecuteNonQuery();
    }
}