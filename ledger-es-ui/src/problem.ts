export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}
