import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const pointSchema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number().optional().default(0),
});

export function registerAddSymbolicLineTool(server: McpServer) {
  server.tool(
    "add_symbolic_line",
    "Create symbolic lines in the active family view. This first pass focuses on line geometry and style assignment.",
    {
      data: z.array(
        z.object({
          line: z.object({
            p0: pointSchema,
            p1: pointSchema,
          }).describe("Symbolic line segment in mm"),
          lineStyle: z.string().optional().default("").describe("Optional line style name"),
        })
      ),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("add_symbolic_line", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `add symbolic line failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
