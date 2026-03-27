import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetFamilyViewInfoTool(server: McpServer) {
  server.tool(
    "get_family_view_info",
    "Return the active Revit document and current view context, with extra family-editor metadata when the active document is a family.",
    {},
    async () => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("get_family_view_info", {});
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `get family view info failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
