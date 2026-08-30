interface ApiErrorSource {
  result?: unknown;
  response?: unknown;
}

interface ProblemDetails {
  errors?: Record<string, unknown>;
  detail?: unknown;
  title?: unknown;
  message?: unknown;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object';
}

function asApiErrorSource(value: unknown): ApiErrorSource | undefined {
  return isRecord(value) ? value : undefined;
}

function asProblemDetails(value: unknown): ProblemDetails | undefined {
  return isRecord(value) ? value : undefined;
}

function asMessage(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() ? value : undefined;
}

export function getApiError(error: unknown): string {
  const source = asApiErrorSource(error);

  if (source) {
    let parsedResponse: unknown;
    try {
      parsedResponse = typeof source.response === 'string' ? JSON.parse(source.response) : undefined;
    } catch {
      parsedResponse = undefined;
    }

    const problem = asProblemDetails(source.result ?? parsedResponse ?? error);
    const validationMessages = problem
      ? Object.values(problem.errors ?? {})
        .flatMap((messages) => Array.isArray(messages) ? messages : [messages])
        .map(asMessage)
        .filter((message): message is string => message !== undefined)
      : [];

    if (validationMessages.length > 0) return validationMessages.join(' ');
    const detail = asMessage(problem?.detail);
    if (detail) return detail;
    const title = asMessage(problem?.title);
    if (title) return title;
    const message = asMessage(problem?.message);
    if (message) return message;
  }

  return 'Cane360 could not complete the request. Please try again.';
}
