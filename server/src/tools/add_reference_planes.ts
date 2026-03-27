import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const pointSchema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number().optional().default(0),
});

export function registerAddReferencePlanesTool(server: McpServer) {
  server.tool(
    "add_reference_planes",
    "Create named reference planes in the active family document using line endpoints and a cut vector, typically in the current family editor view.",
    {
      data: z.array(
        z.object({
          name: z.string().optional().default("").describe("Reference plane name"),
          bubbleEnd: pointSchema.describe("Bubble-end point in mm"),
          freeEnd: pointSchema.describe("Free-end point in mm"),
          cutVector: pointSchema.optional().describe("Cut vector in mm. Defaults to +Z"),
        })
      ),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("add_reference_planes", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `add reference planes failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
