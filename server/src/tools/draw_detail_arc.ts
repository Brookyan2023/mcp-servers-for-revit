import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const pointSchema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number().optional().default(0),
});

export function registerDrawDetailArcTool(server: McpServer) {
  server.tool(
    "draw_detail_arc",
    "Draw detail arcs in the active family document view using center point, radius, and start/end angles in degrees.",
    {
      data: z.array(
        z.object({
          center: pointSchema.describe("Arc center in mm"),
          radius: z.number().describe("Arc radius in mm"),
          startAngle: z.number().describe("Start angle in degrees"),
          endAngle: z.number().describe("End angle in degrees"),
          lineStyle: z.string().optional().default("").describe("Optional line style name"),
        })
      ),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("draw_detail_arc", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `draw detail arc failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
