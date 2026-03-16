import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerListLevelsTool(server: McpServer) {
  server.tool("list_levels", "List levels in the current Revit document.", {}, async () => {
    try {
      const response = await withRevitConnection(async (revitClient) => {
        return await revitClient.sendCommand("list_levels", {});
      });

      return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
    } catch (error) {
      return {
        content: [{ type: "text", text: `list levels failed: ${error instanceof Error ? error.message : String(error)}` }],
      };
    }
  });
}
