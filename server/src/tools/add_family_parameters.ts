import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const familyParameterSchema = z.object({
  name: z.string().describe("Parameter name"),
  group: z.string().optional().default("Data").describe("Parameter group, e.g. Data, Geometry, Text, Graphics"),
  dataType: z.string().optional().default("Text").describe("Parameter type, e.g. Text, Length, Number, Integer, YesNo, Area"),
  isInstance: z.boolean().optional().default(false).describe("True for instance parameter, false for type parameter"),
  defaultValue: z.union([z.string(), z.number(), z.boolean()]).optional().describe("Optional default value for the current family type"),
  formula: z.string().optional().describe("Optional family formula. If provided, it takes precedence over defaultValue"),
});

export function registerAddFamilyParametersTool(server: McpServer) {
  server.tool(
    "add_family_parameters",
    "Batch add family parameters to the active family document. Supports type or instance parameters with optional defaults or formulas.",
    {
      data: z.array(familyParameterSchema).describe("Parameters to add to the active family"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("add_family_parameters", args);
        });

        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return {
          content: [{ type: "text", text: `add family parameters failed: ${error instanceof Error ? error.message : String(error)}` }],
        };
      }
    }
  );
}
