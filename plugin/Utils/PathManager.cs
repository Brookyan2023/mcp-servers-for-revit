using Newtonsoft.Json;
using System;
using System.IO;

namespace revit_mcp_plugin.Utils
{
    public static class PathManager
    {
        /// <summary>
        /// Gets the root application data directory
        /// </summary>
        public static string GetAppDataDirectoryPath()
        {
            string applicationPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string applicationDirectory = Path.GetDirectoryName(applicationPath);

            return applicationDirectory;
        }
        /// <summary>
        /// Gets the path to the Commands directory
        /// </summary>
        public static string GetCommandsDirectoryPath()
        {
            string appDataDirectory = GetAppDataDirectoryPath();
            string commandsDirectory = Path.Combine(appDataDirectory, "Commands");

            EnsureDirectoryExists(commandsDirectory);

            return commandsDirectory;
        }
        /// <summary>
        /// Gets the path to the Logs directory
        /// </summary>
        public static string GetLogsDirectoryPath()
        {
            string appDataDirectory = GetAppDataDirectoryPath();
            string logsDirectory = Path.Combine(appDataDirectory, "Logs");

            EnsureDirectoryExists(logsDirectory);

            return logsDirectory;
        }
        /// <summary>
        /// Gets the shared state directory for data accessed by both the plugin and MCP server.
        /// </summary>
        public static string GetSharedStateDirectoryPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string sharedDirectory = Path.Combine(appData, "RevitMCP");

            EnsureDirectoryExists(sharedDirectory);

            return sharedDirectory;
        }
        /// <summary>
        /// Gets the path to the port state file shared with the MCP server.
        /// </summary>
        public static string GetPortStateFilePath()
        {
            string sharedDirectory = GetSharedStateDirectoryPath();
            return Path.Combine(sharedDirectory, "revit-mcp-port.json");
        }
        /// <summary>
        /// Gets the path to the command registry file.
        /// If the file doesn't exist, creates it with default content.
        /// </summary>
        /// <param name="createIfNotExists">Whether to create a default file if it doesn't exist (default: true)</param>
        /// <returns>Path to the command registry file</returns>
        public static string GetCommandRegistryFilePath(bool createIfNotExists = true)
        {
            string commandsDirectory = GetCommandsDirectoryPath();
            string registryFilePath = Path.Combine(commandsDirectory, "commandRegistry.json");

            if (createIfNotExists && !File.Exists(registryFilePath))
            {
                CreateDefaultCommandRegistryFile(registryFilePath);
            }

            return registryFilePath;
        }
        /// <summary>
        /// Creates a default command registry file with empty commands array
        /// </summary>
        /// <param name="filePath">Path where to create the file</param>
        private static void CreateDefaultCommandRegistryFile(string filePath)
        {
            try
            {
                var defaultRegistry = new
                {
                    commands = new object[] { },
                    settings = new { logLevel = "Info", port = 0 }
                };
                string jsonContent = JsonConvert.SerializeObject(defaultRegistry, Formatting.Indented);

                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating default command registry file: {ex.Message}");
            }
        }
        /// <summary>
        /// Ensures that the specified directory exists
        /// </summary>
        /// <param name="directoryPath">The path to check and create if needed</param>
        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}
