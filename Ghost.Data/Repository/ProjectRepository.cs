using Ghost.Data.Models;
using System.Data;
using System.Data.SQLite;

namespace Ghost.Data.Repository;

internal class ProjectRepository : IDisposable
{
    private readonly SQLiteConnection _connection;

    public ProjectRepository(string sourceDirectory)
    {
        _connection = new SQLiteConnection(string.Format(Command.CONNECTION_STRING, sourceDirectory));
        _connection.Open();
    }

    private static class Command
    {
        public const string CONNECTION_STRING = "Data Source={0}\\projects.db;Version=3;";
        public const string CREATE_PROJECT_TABLE_STRING = "CREATE TABLE IF NOT EXISTS Projects (ID INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, MetadataPath TEXT);";
        public const string SELECT_PROJECT_STRING = "SELECT * FROM Projects";
        public const string INSERT_PROJECT_STRING = "INSERT INTO Projects (Name, MetadataPath) VALUES (@Name, @MetadataPath);";
        public const string REMOVE_PROJECT_STRING = "DELETE FROM Projects WHERE ID = @ID;";
        public const string UPDATE_PROJECT_STRING = "UPDATE Projects SET Name = @Name, MetadataPath = @MetadataPath WHERE ID = @ID;";
    }

    private async Task EnsureTableCreatedAsync()
    {
        using var createCommand = _connection.CreateCommand();
        createCommand.CommandText = Command.CREATE_PROJECT_TABLE_STRING;
        await createCommand.ExecuteNonQueryAsync();
    }

    public async IAsyncEnumerable<ProjectInfo> LoadProjectsAsync()
    {
        await EnsureTableCreatedAsync();

        using var command = _connection.CreateCommand();
        command.CommandText = Command.SELECT_PROJECT_STRING;

        using var reader = command.ExecuteReader();
        while (await reader.ReadAsync())
        {
            var project = new ProjectInfo
            {
                ID = reader.GetInt32(0),
                Name = reader.GetString(1),
                MetadataPath = reader.GetString(2),
            };

            yield return project;
        }
    }

    public async Task AddProjectAsync(ProjectInfo project)
    {
        await EnsureTableCreatedAsync();

        using var command = _connection.CreateCommand();
        command.CommandText = Command.INSERT_PROJECT_STRING;

        command.Parameters.AddWithValue("@Name", project.Name);
        command.Parameters.AddWithValue("@MetadataPath", project.MetadataPath);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveProjectAsync(ProjectInfo project)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = Command.REMOVE_PROJECT_STRING;

        command.Parameters.AddWithValue("@ID", project.ID);

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateProjectAsync(ProjectInfo project)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = Command.UPDATE_PROJECT_STRING;

        command.Parameters.AddWithValue("@Name", project.Name);
        command.Parameters.AddWithValue("@MetadataPath", project.MetadataPath);
        command.Parameters.AddWithValue("@ID", project.ID);

        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        if (_connection.State == ConnectionState.Open)
        {
            _connection.Close();
        }

        _connection.Dispose();
    }
}