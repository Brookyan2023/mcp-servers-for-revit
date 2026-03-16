import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
import {
  getDeveloperModeBlockReason,
  getDeveloperModeStatePath,
  validateCodeExecutionRequest,
} from "../utils/developerMode.js";

export function registerSendCodeToRevitTool(server: McpServer) {
  const parameterValueSchema = z.union([
    z.string(),
    z.number(),
    z.boolean(),
    z.null(),
    z.record(z.string(), z.union([z.string(), z.number(), z.boolean(), z.null()])),
    z.object({}).passthrough(),
  ]);

  server.tool(
    "send_code_to_revit",
    "Send C# code to Revit for execution. The code will be inserted into a template with access to the Revit Document and parameters. Your code should be written to work within the Execute method of the template.",
    {
      code: z
        .string()
        .describe(
          "The C# code to execute in Revit. This code will be inserted into the Execute method of a template with access to Document and parameters."
        ),
      parameters: z
        .array(parameterValueSchema)
        .optional()
        .describe(
          "Optional execution parameters that will be passed to your code"
        ),
    },
    async (args, extra) => {
      const modeBlockReason = getDeveloperModeBlockReason();
      if (modeBlockReason) {
        return {
          content: [
            {
              type: "text",
              text: `Code execution blocked: ${modeBlockReason}`,
            },
          ],
        };
      }

      const requestBlockReason = validateCodeExecutionRequest(args.code);
      if (requestBlockReason) {
        return {
          content: [
            {
              type: "text",
              text: `Code execution blocked: ${requestBlockReason}`,
            },
          ],
        };
      }

      const params = {
        code: args.code,
        parameters: args.parameters || [],
      };

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("send_code_to_revit", params);
        });

        return {
          content: [
            {
              type: "text",
              text: `Code execution successful!\nResult: ${JSON.stringify(
                response,
                null,
                2
              )}`,
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Code execution failed: ${
                error instanceof Error ? error.message : String(error)
              }\nDeveloper mode state file: ${getDeveloperModeStatePath()}`,
            },
          ],
        };
      }
    }
  );
}
