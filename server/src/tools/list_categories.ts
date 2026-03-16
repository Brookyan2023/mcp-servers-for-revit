import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerListCategoriesTool(server: McpServer) {
  server.tool("list_categories", "List categories available in the current Revit document.", {}, async () => {
    try {
      const response = await withRevitConnection(async (revitClient) => {
        return await revitClient.sendCommand("list_categories", {});
      });

      return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
    } catch (error) {
      return {
        content: [{ type: "text", text: `list categories failed: ${error instanceof Error ? error.message : String(error)}` }],
      };
    }
  });
}
