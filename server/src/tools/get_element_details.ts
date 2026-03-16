import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetElementDetailsTool(server: McpServer) {
  server.tool(
    "get_element_details",
    "Return detailed metadata for a single Revit element.",
    {
      element_id: z.number().describe("Element id to inspect"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("get_element_details", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `get element details failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
