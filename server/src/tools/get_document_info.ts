import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetDocumentInfoTool(server: McpServer) {
  server.tool("get_document_info", "Return current Revit document metadata and active view context.", {}, async () => {
    try {
      const response = await withRevitConnection(async (revitClient) => {
        return await revitClient.sendCommand("get_document_info", {});
      });

      return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
    } catch (error) {
      return {
        content: [{ type: "text", text: `get document info failed: ${error instanceof Error ? error.message : String(error)}` }],
      };
    }
  });
}
