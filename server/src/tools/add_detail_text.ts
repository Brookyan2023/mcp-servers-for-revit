import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const pointSchema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number().optional().default(0),
});

export function registerAddDetailTextTool(server: McpServer) {
  server.tool(
    "add_detail_text",
    "Place text notes in the active family view.",
    {
      data: z.array(
        z.object({
          location: pointSchema.describe("Text insertion point in mm"),
          text: z.string().describe("Text content"),
          rotation: z.number().optional().default(0).describe("Rotation in degrees"),
          textNoteTypeId: z.number().optional().default(-1).describe("Optional text note type element id"),
          horizontalAlign: z.number().optional().default(0).describe("0=Left, 1=Center, 2=Right"),
        })
      ),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("add_detail_text", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `add detail text failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
