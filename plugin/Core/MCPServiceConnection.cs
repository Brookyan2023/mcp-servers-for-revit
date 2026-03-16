using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Threading;

namespace revit_mcp_plugin.Core
{
    [Transaction(TransactionMode.Manual)]
public class MCPServiceConnection : IExternalCommand
{
        private static int _isBusyFlag = 0;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (Interlocked.Exchange(ref _isBusyFlag, 1) == 1)
                {
                    TaskDialog.Show("revitMCP", "MCP is busy. Please wait and try again.");
                    return Result.Succeeded;
                }

                // 获取socket服务
                // Obtain socket service.
                SocketService service = SocketService.Instance;

                if (service.IsRunning)
                {
                    RibbonUiState.SetStatus("MCP: Closing...", "Stopping MCP server");
                    service.Stop();
                    RibbonUiState.SetStatus("MCP: Closed", "MCP server is closed");
                }
                else
                {
                    RibbonUiState.SetStatus("MCP: Opening...", "Starting MCP server");
                    service.Initialize(commandData.Application);
                    service.Start();
                    if (service.IsRunning)
                        RibbonUiState.SetStatus("MCP: Open", "MCP server is running");
                    else
                        RibbonUiState.SetStatus("MCP: Closed", "MCP server failed to start");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                RibbonUiState.SetStatus("MCP: Error", ex.Message);
                TaskDialog.Show("revitMCP", $"MCP error: {ex.Message}");
                return Result.Failed;
            }
            finally
            {
                Interlocked.Exchange(ref _isBusyFlag, 0);
            }
        }
    }
}
