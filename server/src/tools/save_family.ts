import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerSaveFamilyTool(server: McpServer) {
  server.tool(
    "save_family",
    "Save the active family document. If savePath is omitted, the current family path is reused.",
    {
      savePath: z.string().optional().describe("Optional absolute .rfa target path"),
      overwrite: z.boolean().optional().default(false).describe("Allow overwriting when savePath points to an existing file"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("save_family", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `save family failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
