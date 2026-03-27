import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerListLineStylesTool(server: McpServer) {
  server.tool(
    "list_line_styles",
    "List the available line styles in the current Revit document or active family document.",
    {},
    async () => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("list_line_styles", {});
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `list line styles failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
