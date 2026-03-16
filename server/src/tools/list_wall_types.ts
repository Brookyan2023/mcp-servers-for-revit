import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerListWallTypesTool(server: McpServer) {
  server.tool("list_wall_types", "List wall types in the current Revit document.", {}, async () => {
    try {
      const response = await withRevitConnection(async (revitClient) => {
        return await revitClient.sendCommand("list_wall_types", {});
      });

      return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
    } catch (error) {
      return {
        content: [{ type: "text", text: `list wall types failed: ${error instanceof Error ? error.message : String(error)}` }],
      };
    }
  });
}
