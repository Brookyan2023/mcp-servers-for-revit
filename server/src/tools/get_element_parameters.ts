import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetElementParametersTool(server: McpServer) {
  server.tool(
    "get_element_parameters",
    "List parameters for a Revit element.",
    {
      element_id: z.number().describe("Element id to inspect"),
      name_filter: z.string().optional().describe("Optional case-insensitive parameter name filter"),
      include_read_only: z.boolean().optional().default(true).describe("Include read-only parameters"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("get_element_parameters", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `get element parameters failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
