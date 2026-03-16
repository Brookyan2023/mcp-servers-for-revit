import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerSetElementParameterTool(server: McpServer) {
  server.tool(
    "set_element_parameter",
    "Set a writable parameter on a Revit element.",
    {
      element_id: z.number().describe("Element id to modify"),
      parameter_name: z.string().describe("Parameter name"),
      value: z.any().describe("Parameter value"),
      value_unit: z.string().optional().describe("Optional input unit such as mm, m2, m3, deg, or internal"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("set_element_parameter", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `set element parameter failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
