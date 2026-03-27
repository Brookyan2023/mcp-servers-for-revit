import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateDetailFamilyTool(server: McpServer) {
  server.tool(
    "create_detail_family",
    "Create a new Revit family document from a .rft template, save it to disk, and activate it so follow-up family tools operate on the new family.",
    {
      templatePath: z.string().describe("Absolute path to the .rft family template"),
      savePath: z.string().describe("Absolute path where the new .rfa family should be saved"),
      overwrite: z.boolean().optional().default(false).describe("Overwrite the target .rfa if it already exists"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_detail_family", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `create detail family failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
