import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const pointSchema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number().optional().default(0),
});

export function registerDrawDetailLinesTool(server: McpServer) {
  server.tool(
    "draw_detail_lines",
    "Draw detail lines in the active family document view. Each line can optionally target a named line style from list_line_styles.",
    {
      data: z.array(
        z.object({
          line: z.object({
            p0: pointSchema,
            p1: pointSchema,
          }),
          lineStyle: z.string().optional().default("").describe("Optional line style name"),
        })
      ),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("draw_detail_lines", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `draw detail lines failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
