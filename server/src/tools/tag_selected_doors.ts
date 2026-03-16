import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerTagSelectedDoorsTool(server: McpServer) {
  server.tool(
    "tag_selected_doors",
    "Create tags for selected doors in the current active view. Uses the current Revit selection by default, or explicit door IDs if provided.",
    {
      useLeader: z
        .boolean()
        .optional()
        .default(false)
        .describe("Whether to use a leader line when creating the tags"),
      tagTypeId: z
        .string()
        .optional()
        .describe("Optional door tag family type element id"),
      doorIds: z
        .array(z.number())
        .optional()
        .describe("Optional explicit door element ids. If omitted, uses the current Revit selection."),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("tag_selected_doors", args);
        });

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(response, null, 2),
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Selected door tagging failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
