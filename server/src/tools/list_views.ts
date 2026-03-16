import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerListViewsTool(server: McpServer) {
  server.tool(
    "list_views",
    "List views in the current Revit document.",
    {
      view_type: z.string().optional().describe("Optional Revit view type filter, e.g. FloorPlan or ThreeD"),
      include_templates: z.boolean().optional().default(false).describe("Include view templates"),
      max_items: z.number().optional().default(500).describe("Maximum number of views to return"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("list_views", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `list views failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
