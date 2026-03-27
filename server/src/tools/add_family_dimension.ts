import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const pointSchema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number().optional().default(0),
});

export function registerAddFamilyDimensionTool(server: McpServer) {
  server.tool(
    "add_family_dimension",
    "Create dimensions between named reference planes in the active family view, with an optional family label parameter.",
    {
      data: z.array(
        z.object({
          referencePlaneNames: z.array(z.string()).min(2).describe("At least two existing reference plane names"),
          line: z.object({
            p0: pointSchema,
            p1: pointSchema,
          }).describe("Dimension line in mm"),
          labelParameterName: z.string().optional().default("").describe("Optional existing family parameter name to use as the family label"),
        })
      ),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("add_family_dimension", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `add family dimension failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
