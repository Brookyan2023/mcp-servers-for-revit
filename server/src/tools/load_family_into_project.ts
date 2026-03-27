import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerLoadFamilyIntoProjectTool(server: McpServer) {
  server.tool(
    "load_family_into_project",
    "Load a family into a Revit project. Works from an active family document or from a project document plus a familyPath.",
    {
      familyPath: z.string().optional().describe("Absolute path to a .rfa file when loading from a project document"),
      targetProjectTitle: z.string().optional().describe("Optional open project title to load into when a family editor is active"),
      overwriteParameters: z.boolean().optional().default(true).describe("Overwrite parameter values when the family already exists"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("load_family_into_project", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `load family into project failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
