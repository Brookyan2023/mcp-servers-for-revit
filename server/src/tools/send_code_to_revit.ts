import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
import {
  getDeveloperModeBlockReason,
  getDeveloperModeStatePath,
  validateCodeExecutionRequest,
} from "../utils/developerMode.js";
import { validateDynamicCode } from "../utils/codeExecutionValidator.js";

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
      revit_version: z
        .number()
        .int()
        .min(2019)
        .max(2026)
        .optional()
        .describe("Optional explicit Revit target version for compatibility validation."),
      validation_mode: z
        .enum(["strict", "lenient"])
        .optional()
        .describe("Validation mode. strict blocks on validation errors."),
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

      const validation = validateDynamicCode(args.code, {
        revitVersion: args.revit_version,
        validationMode: args.validation_mode,
      });

      if (!validation.ok) {
        return {
          content: [
            {
              type: "text",
              text: `Code execution blocked by validator.\n${JSON.stringify(
                {
                  can_retry: true,
                  phase: "validation",
                  validation,
                },
                null,
                2
              )}`,
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
              text: `Code execution successful!\n${JSON.stringify(
                {
                  validation: {
                    revitVersion: validation.revitVersion,
                    warnings: validation.warnings,
                  },
                  result: response,
                },
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
