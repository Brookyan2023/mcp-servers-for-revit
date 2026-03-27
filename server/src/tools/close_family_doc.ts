import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCloseFamilyDocTool(server: McpServer) {
  server.tool(
    "close_family_doc",
    "Close the active family document and switch Revit back to an open project document.",
    {
      saveBeforeClose: z.boolean().optional().default(false).describe("Save the family before closing it"),
      targetProjectTitle: z.string().optional().describe("Optional open project title to activate before closing the family"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("close_family_doc", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `close family doc failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
