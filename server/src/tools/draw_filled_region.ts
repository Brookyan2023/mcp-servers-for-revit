import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const pointSchema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number().optional().default(0),
});

export function registerDrawFilledRegionTool(server: McpServer) {
  server.tool(
    "draw_filled_region",
    "Create filled regions in the active family document using a polygon boundary and an optional filled region type name.",
    {
      data: z.array(
        z.object({
          boundary: z.array(pointSchema).min(3).describe("Polygon boundary points in mm"),
          fillPatternName: z.string().optional().default("").describe("Optional filled region type name"),
        })
      ),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("draw_filled_region", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `draw filled region failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
