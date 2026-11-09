export interface AnalyzeRequest {
  text: string;
}

export interface CreateShareRequest {
  text: string;
}

export interface CreateShareResponse {
  slug: string;
  url: string;
  expiresAt: string;
  deleteToken: string;
}

export interface SharedReport {
  report: AnalysisReport;
  expiresAt: string;
}

export interface AnalysisReport {
  analysisId: string;
  engineVersion: string;
  verdict: Verdict;
  fix: Fix | null;
  location: BlameLocation | null;
  evidence: string[];
  extended: ExtendedInfo;
}

export interface Verdict {
  title: string;
  summary: string;
  exceptionType: string;
  exceptionMessage: string;
}

export interface Fix {
  explanation: string;
  code: string | null;
  docsUrl: string | null;
}

export interface BlameLocation {
  method: string;
  file: string | null;
  line: number | null;
}

export interface ExtendedInfo {
  exceptionChain: ExceptionNode[];
  frames: StackFrame[];
  findings: Finding[];
  warnings: string[];
  stats: AnalysisStats;
  redactions: string[];
}

export interface ExceptionNode {
  type: string;
  message: string;
  isWrapper: boolean;
  frames: StackFrame[];
  children: ExceptionNode[];
  truncated: boolean;
}

export interface StackFrame {
  method: string;
  parameters: string | null;
  file: string | null;
  line: number | null;
  kind: string;
  isCompilerGenerated: boolean;
  asyncBoundaryBefore: boolean;
}

export interface Finding {
  ruleId: string;
  title: string;
  explanation: string;
  evidence: string[];
}

export interface AnalysisStats {
  totalFrames: number;
  userFrames: number;
  frameworkFrames: number;
  thirdPartyFrames: number;
  unparsedLines: number;
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
