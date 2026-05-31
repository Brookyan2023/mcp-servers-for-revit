import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

type RuleSeverity = "error" | "warning";
type ValidationMode = "strict" | "lenient";

type VersionRange = {
  min?: number;
  max?: number;
};

type ValidationRule = {
  id: string;
  description: string;
  severity: RuleSeverity;
  category: string;
  versionRange?: VersionRange;
  patterns?: string[];
  specialCheck?: "missingRevitNamespace";
  message: string;
  suggestion?: string;
};

type ValidatorConfig = {
  defaultValidationMode: ValidationMode;
  defaultRevitVersion: number;
  rules: ValidationRule[];
};

export type CodeValidationIssue = {
  ruleId: string;
  severity: RuleSeverity;
  category: string;
  message: string;
  suggestion?: string;
  line?: number;
  column?: number;
  evidence?: string;
};

export type CodeValidationResult = {
  ok: boolean;
  validationMode: ValidationMode;
  revitVersion: number;
  errors: CodeValidationIssue[];
  warnings: CodeValidationIssue[];
};

let cachedConfig: ValidatorConfig | null = null;

function getConfigPath(): string {
  const __filename = fileURLToPath(import.meta.url);
  const __dirname = path.dirname(__filename);
  const buildConfigPath = path.resolve(__dirname, "../config/code-execution-rules.json");
  if (fs.existsSync(buildConfigPath)) {
    return buildConfigPath;
  }

  return path.resolve(__dirname, "../../src/config/code-execution-rules.json");
}

function loadConfig(): ValidatorConfig {
  if (cachedConfig) return cachedConfig;
  const raw = fs.readFileSync(getConfigPath(), "utf8");
  cachedConfig = JSON.parse(raw) as ValidatorConfig;
  return cachedConfig;
}

function appliesToVersion(range: VersionRange | undefined, revitVersion: number): boolean {
  if (!range) return true;
  if (range.min !== undefined && revitVersion < range.min) return false;
  if (range.max !== undefined && revitVersion > range.max) return false;
  return true;
}

function indexToLineColumn(source: string, index: number): { line: number; column: number } {
  const before = source.slice(0, index);
  const lines = before.split(/\r?\n/);
  return {
    line: lines.length,
    column: lines[lines.length - 1].length + 1,
  };
}

function buildIssue(
  rule: ValidationRule,
  source: string,
  index: number,
  evidence?: string
): CodeValidationIssue {
  const position = index >= 0 ? indexToLineColumn(source, index) : undefined;
  return {
    ruleId: rule.id,
    severity: rule.severity,
    category: rule.category,
    message: rule.message,
    suggestion: rule.suggestion,
    line: position?.line,
    column: position?.column,
    evidence,
  };
}

function runSpecialCheck(rule: ValidationRule, source: string): CodeValidationIssue[] {
  if (rule.specialCheck === "missingRevitNamespace") {
    const hasRevitNamespace = /Autodesk\.Revit\.(DB|UI|ApplicationServices)/.test(source);
    if (!hasRevitNamespace) {
      return [buildIssue(rule, source, -1)];
    }
  }

  return [];
}

export function validateDynamicCode(
  code: string,
  options?: { revitVersion?: number; validationMode?: ValidationMode }
): CodeValidationResult {
  const config = loadConfig();
  const revitVersion = options?.revitVersion ?? config.defaultRevitVersion;
  const validationMode = options?.validationMode ?? config.defaultValidationMode;

  const errors: CodeValidationIssue[] = [];
  const warnings: CodeValidationIssue[] = [];

  for (const rule of config.rules) {
    if (!appliesToVersion(rule.versionRange, revitVersion)) continue;

    const specialIssues = runSpecialCheck(rule, code);
    for (const issue of specialIssues) {
      if (issue.severity === "error") errors.push(issue);
      else warnings.push(issue);
    }

    for (const pattern of rule.patterns ?? []) {
      const regex = new RegExp(pattern, "g");
      let match: RegExpExecArray | null = null;

      while ((match = regex.exec(code)) !== null) {
        const issue = buildIssue(rule, code, match.index, match[0]);
        if (issue.severity === "error") errors.push(issue);
        else warnings.push(issue);
      }
    }
  }

  const ok = validationMode === "strict" ? errors.length === 0 : errors.length === 0;
  return { ok, validationMode, revitVersion, errors, warnings };
}
